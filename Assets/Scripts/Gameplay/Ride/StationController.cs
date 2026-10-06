using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// One stop on the ride. Loads the Chamber 01 catalog row, decides which dash controls are live, re-evaluates
    /// through <see cref="PuzzleEvaluator"/> on every lever move, feeds the data stream one case at a time, and raises
    /// Solved when the evaluator passes. The Learns stop runs <see cref="BatchGradientDescent"/> step by step and moves
    /// the weight levers itself. Presentation only listens; it never evaluates.
    /// </summary>
    public sealed class StationController : MonoBehaviour
    {
        [SerializeField] private StationKind kind = StationKind.OneSignal;
        [SerializeField] private float feedInterval = 2.6f;
        [SerializeField] private float solveHoldSeconds = 1.0f;
        [SerializeField] private float hintAfterSeconds = 8f;
        [SerializeField] private float trainStepSeconds = 0.4f;
        [SerializeField] private int maxTrainSteps = 60;
        [SerializeField] private float convergedGradient = 0.02f;
        [SerializeField] private float divergeLimit = 4.5f;
        [SerializeField] private float landscapeMin = -4f;
        [SerializeField] private float landscapeMax = 4f;
        [SerializeField] private int landscapeResolution = 33;
        [SerializeField] private Vector2 startWeights = new Vector2(-1.5f, 1.5f);
        [SerializeField] private float[] learningRates = { 0.05f, 0.5f, 1.6f };

        private RideControlSet _controls;
        private RideControlChannel _rock;
        private RideControlChannel _ice;
        private RideControlChannel _trigger;
        private RideControlChannel _action;
        private RideControlChannel _eta;
        private PuzzleDefinition _puzzle;
        private List<double[]> _trainInputs;
        private List<double[]> _trainTargets;
        private readonly Dictionary<int, bool> _passCache = new Dictionary<int, bool>();
        private CaseOutcome[] _board = new CaseOutcome[0];
        private double[,] _grid;
        private bool _active;
        private bool _solved;
        private bool _training;
        private bool _oopsSaid;
        private bool _inOops;
        private int _feedCursor;
        private float _feedTimer;
        private float _passTimer;
        private Coroutine _hint;
        private Coroutine _train;

        public StationKind Kind => kind;
        public bool IsActive => _active;
        public bool IsSolved => _solved;
        public bool IsTraining => _training;
        public RideControlSet Controls => _controls;
        public IReadOnlyList<CaseOutcome> Board => _board;
        public double[,] LossGrid => _grid;
        public float LandscapeMin => landscapeMin;
        public float LandscapeMax => landscapeMax;
        public Vector2 StartWeights => startWeights;
        public float SolveHoldSeconds { get => solveHoldSeconds; set => solveHoldSeconds = value; }

        /// <summary>Raised when the stop becomes live.</summary>
        public event Action<StationKind> Began;

        /// <summary>Raised when the stop's phase changes.</summary>
        public event Action<StationKind, StationPhase> PhaseChanged;

        /// <summary>A narration line should play.</summary>
        public event Action<RideLine> Said;

        /// <summary>One case should travel the stream now, with its result at the current settings.</summary>
        public event Action<CaseOutcome> CaseFed;

        /// <summary>Every case re-evaluated after a control change.</summary>
        public event Action<IReadOnlyList<CaseOutcome>> BoardChanged;

        /// <summary>The evaluator passed and held.</summary>
        public event Action<StationSummary> Solved;

        /// <summary>The trainer took a step.</summary>
        public event Action<TrainingFrame> TrainingStepped;

        /// <summary>The loss grid for the landscape is ready.</summary>
        public event Action LandscapeReady;

        /// <summary>The stop was ended and its controls released.</summary>
        public event Action Ended;

        /// <summary>Which lever to nudge and which way (+1 up, -1 down). A null id means no hint.</summary>
        public event Action<string, int> HintChanged;

        /// <summary>Makes this stop live. Looks up the dash channels by id and sets which ones the player can touch.</summary>
        public void Begin(RideControlSet controls)
        {
            if (controls == null) throw new ArgumentNullException(nameof(controls));
            if (!isActiveAndEnabled) return;
            End();

            _controls = controls;
            _rock = controls.Get(RideControlIds.Rock);
            _ice = controls.Get(RideControlIds.Ice);
            _trigger = controls.Get(RideControlIds.Trigger);
            _action = controls.Get(RideControlIds.Action);
            _eta = controls.Get(RideControlIds.Eta);
            if (_rock == null || _ice == null || _trigger == null || _action == null || _eta == null)
            {
                throw new InvalidOperationException("The pod is missing a control channel.");
            }

            _solved = false;
            _training = false;
            _oopsSaid = false;
            _inOops = false;
            _passTimer = 0f;
            _feedCursor = 0;
            _feedTimer = 0.8f;
            _passCache.Clear();
            BuildPuzzle();
            ConfigureControls();

            _rock.Changed += OnWeightOrTrigger;
            _ice.Changed += OnWeightOrTrigger;
            _trigger.Changed += OnWeightOrTrigger;
            _action.Changed += OnAction;
            _active = true;

            Began?.Invoke(kind);
            Evaluate();
            SetPhase(StationPhase.Tuning);
            if (kind == StationKind.Learns)
            {
                _grid = LossLandscape.SampleGrid(TrainingNetwork(0f, 0f), _trainInputs, _trainTargets,
                    LossKind.MeanSquaredError, landscapeMin, landscapeMax, landscapeResolution);
                LandscapeReady?.Invoke();
            }

            Say(kind == StationKind.OneSignal ? RideLine.NeuronIntro
                : kind == StationKind.TwoSignals ? RideLine.TwoSensors
                : RideLine.PullLearn);
            _hint = StartCoroutine(HintRoutine());
            if (kind == StationKind.Learns) SetHint(RideControlIds.Action, 1);
        }

        /// <summary>Stops the stop. Safe to call twice.</summary>
        public void End()
        {
            if (_rock != null) _rock.Changed -= OnWeightOrTrigger;
            if (_ice != null) _ice.Changed -= OnWeightOrTrigger;
            if (_trigger != null) _trigger.Changed -= OnWeightOrTrigger;
            if (_action != null) _action.Changed -= OnAction;
            if (_hint != null) StopCoroutine(_hint);
            if (_train != null) StopCoroutine(_train);
            _hint = null;
            _train = null;
            bool wasActive = _active;
            _active = false;
            _training = false;
            if (wasActive) Ended?.Invoke();
        }

        /// <summary>Sends the next case down the stream with its result at the current settings.</summary>
        public void FeedNextCase()
        {
            if (!_active || _board.Length == 0) return;
            _feedCursor %= _board.Length;
            CaseFed?.Invoke(_board[_feedCursor]);
            _feedCursor++;
        }

        private void Update()
        {
            if (!_active) return;

            _feedTimer -= Time.deltaTime;
            if (_feedTimer <= 0f)
            {
                _feedTimer = feedInterval;
                FeedNextCase();
            }

            if (kind != StationKind.Learns && !_solved)
            {
                if (_lastPassed) _passTimer += Time.deltaTime; else _passTimer = 0f;
                if (_passTimer >= solveHoldSeconds) Solve();
            }
        }

        private bool _lastPassed;

        private void BuildPuzzle()
        {
            PuzzleDefinition full = CurriculumCatalog.Chamber01().Definition;
            _trainInputs = new List<double[]>();
            _trainTargets = new List<double[]>();
            var oneSignal = new List<TestCase>();
            for (int i = 0; i < full.TestCases.Count; i++)
            {
                TestCase tc = full.TestCases[i];
                _trainInputs.Add(tc.Inputs);
                _trainTargets.Add(tc.ExpectedOutputs);
                if (tc.GetInput(1) < 0.5) oneSignal.Add(tc);
            }

            _puzzle = kind == StationKind.OneSignal
                ? new PuzzleDefinition(oneSignal, 2, 1, ActivationType.Step, 1.0, "One signal")
                : full;
        }

        private void ConfigureControls()
        {
            switch (kind)
            {
                case StationKind.OneSignal:
                    _rock.Configure(-2f, 2f, 0.5f, 1f, true, false);
                    _ice.Configure(-2f, 2f, 0.5f, 0f, true, false);
                    _trigger.Configure(-1f, 2f, 0.5f, 2f, false, true);
                    _action.Configure(0f, 1f, 1f, 0f, true, false);
                    _eta.Configure(0f, 2f, 1f, 1f, true, false);
                    break;
                case StationKind.TwoSignals:
                    _rock.Configure(-2f, 2f, 0.5f, 0.5f, false, true);
                    _ice.Configure(-2f, 2f, 0.5f, -1f, false, true);
                    _trigger.Configure(-1f, 2f, 0.5f, 1f, false, true);
                    _action.Configure(0f, 1f, 1f, 0f, true, false);
                    _eta.Configure(0f, 2f, 1f, 1f, true, false);
                    break;
                default:
                    _rock.Configure(-2f, 2f, 0.5f, startWeights.x, true, true);
                    _ice.Configure(-2f, 2f, 0.5f, startWeights.y, true, true);
                    _trigger.Configure(-1f, 2f, 0.5f, 0.5f, true, true);
                    _action.Configure(0f, 1f, 1f, 0f, false, true);
                    _eta.Configure(0f, 2f, 1f, 1f, false, true);
                    break;
            }
        }

        private void OnWeightOrTrigger(RideControlChannel channel, bool byUser)
        {
            if (!byUser || !_active || _solved) return;
            Evaluate();
        }

        private void OnAction(RideControlChannel channel, bool byUser)
        {
            if (!byUser || kind != StationKind.Learns || _solved || _training) return;
            if (channel.Value >= 0.5f) _train = StartCoroutine(TrainRoutine());
        }

        /// <summary>Evaluates the decision neuron at the current lever values through the Core evaluator.</summary>
        private void Evaluate()
        {
            double trigger = _trigger.Value;
            NetworkModel network = NetworkModel.CreateSingleNeuronNetwork(
                2, new[] { (double)_rock.Value, (double)_ice.Value }, -trigger, ActivationType.Step);
            PuzzleEvaluation result = PuzzleEvaluator.Evaluate(network, _puzzle);

            var board = new CaseOutcome[result.Diagnostics.Count];
            bool falsePositive = false;
            for (int i = 0; i < board.Length; i++)
            {
                CaseDiagnostic d = result.Diagnostics[i];
                double[] x = d.Inputs;
                bool fired = d.ActualOutput >= 0.5;
                bool should = d.ExpectedOutput >= 0.5;
                board[i] = new CaseOutcome(i, KindOf(x), x[0], x[1], d.CalculatedZ + trigger, fired, should, d.IsCorrect);
                if (fired && !should) falsePositive = true;
            }

            _board = board;
            _lastPassed = result.Passed;
            BoardChanged?.Invoke(_board);

            if (falsePositive && !_inOops)
            {
                _inOops = true;
                SetPhase(StationPhase.Oops);
                if (!_oopsSaid && kind == StationKind.OneSignal)
                {
                    _oopsSaid = true;
                    Say(RideLine.DroneOops);
                }
            }
            else if (!falsePositive && _inOops)
            {
                _inOops = false;
                SetPhase(StationPhase.Tuning);
            }

            if (kind != StationKind.Learns) PublishHint();
        }

        /// <summary>
        /// Points at the lever to move next, along the shortest route of single detent moves to a setting that passes.
        /// A breadth-first search over the lever positions; every setting is judged by <see cref="PuzzleEvaluator"/>.
        /// No answer is stored, so it works from any starting position.
        /// </summary>
        private void PublishHint()
        {
            if (_lastPassed) { SetHint(null, 0); return; }

            RideControlChannel[] levers = { _rock, _ice, _trigger };
            var counts = new int[3];
            var start = new int[3];
            for (int k = 0; k < 3; k++)
            {
                counts[k] = Mathf.RoundToInt((levers[k].Max - levers[k].Min) / levers[k].Step) + 1;
                start[k] = Mathf.Clamp(Mathf.RoundToInt((levers[k].Value - levers[k].Min) / levers[k].Step), 0, counts[k] - 1);
            }

            var seen = new HashSet<int> { Key(start) };
            var queue = new Queue<(int[] at, int lever, int dir)>();
            queue.Enqueue((start, -1, 0));
            while (queue.Count > 0)
            {
                (int[] at, int lever, int dir) node = queue.Dequeue();
                for (int k = 0; k < 3; k++)
                {
                    if (levers[k].Locked || !levers[k].Visible) continue;
                    for (int step = -1; step <= 1; step += 2)
                    {
                        int next = node.at[k] + step;
                        if (next < 0 || next >= counts[k]) continue;
                        var moved = (int[])node.at.Clone();
                        moved[k] = next;
                        if (!seen.Add(Key(moved))) continue;
                        int firstLever = node.lever < 0 ? k : node.lever;
                        int firstDir = node.lever < 0 ? step : node.dir;
                        if (PassesAt(moved, levers))
                        {
                            SetHint(levers[firstLever].Id, firstDir);
                            return;
                        }

                        queue.Enqueue((moved, firstLever, firstDir));
                    }
                }
            }

            SetHint(null, 0);
        }

        private static int Key(int[] at) => at[0] * 10000 + at[1] * 100 + at[2];

        private bool PassesAt(int[] at, RideControlChannel[] levers)
        {
            int key = Key(at);
            if (_passCache.TryGetValue(key, out bool known)) return known;
            double rock = levers[0].Min + at[0] * levers[0].Step;
            double ice = levers[1].Min + at[1] * levers[1].Step;
            double trigger = levers[2].Min + at[2] * levers[2].Step;
            NetworkModel trial = NetworkModel.CreateSingleNeuronNetwork(2, new[] { rock, ice }, -trigger, ActivationType.Step);
            bool passes = PuzzleEvaluator.Evaluate(trial, _puzzle).Passed;
            _passCache[key] = passes;
            return passes;
        }

        private void SetHint(string id, int direction) => HintChanged?.Invoke(id, direction);

        private static CaseKind KindOf(double[] x)
        {
            bool rock = x[0] >= 0.5;
            bool ice = x[1] >= 0.5;
            if (rock && ice) return CaseKind.Chunk;
            if (rock) return CaseKind.Rock;
            return ice ? CaseKind.Comet : CaseKind.Drone;
        }

        private static NetworkModel TrainingNetwork(float w1, float w2)
        {
            return NetworkModel.CreateSingleNeuronNetwork(2, new[] { (double)w1, (double)w2 }, 0.0, ActivationType.Linear);
        }

        private IEnumerator TrainRoutine()
        {
            _training = true;
            SetHint(null, 0);
            SetPhase(StationPhase.Learning);
            Say(RideLine.HillIsError);
            NetworkModel net = TrainingNetwork(_rock.Value, _ice.Value);
            bool diverged = false;
            bool converged = false;

            for (int step = 1; step <= maxTrainSteps; step++)
            {
                yield return new WaitForSeconds(trainStepSeconds);
                float lr = learningRates[Mathf.Clamp(Mathf.RoundToInt(_eta.Value), 0, learningRates.Length - 1)];
                BatchGradient g = BatchGradientDescent.Step(net, _trainInputs, _trainTargets, lr, LossKind.MeanSquaredError);
                double w1 = net.SingleNeuron.GetWeight(0);
                double w2 = net.SingleNeuron.GetWeight(1);
                TrainingStepped?.Invoke(new TrainingFrame(step, w1, w2, g.Loss, g.WeightGradients[0], g.WeightGradients[1], lr));
                _rock.Drive((float)w1);
                _ice.Drive((float)w2);

                if (double.IsNaN(w1) || double.IsNaN(w2) || Math.Abs(w1) > divergeLimit || Math.Abs(w2) > divergeLimit)
                {
                    diverged = true;
                    break;
                }

                double next = BatchGradientDescent.ComputeGradient(net, _trainInputs, _trainTargets, LossKind.MeanSquaredError).WeightGradientNorm;
                if (next < convergedGradient)
                {
                    converged = true;
                    break;
                }
            }

            if (diverged)
            {
                SetPhase(StationPhase.Overshoot);
                Say(RideLine.LrTooHigh);
                yield return new WaitForSeconds(2.5f);
                _rock.Drive(startWeights.x);
                _ice.Drive(startWeights.y);
                _action.Drive(0f);
                _training = false;
                SetPhase(StationPhase.Tuning);
                SetHint(RideControlIds.Eta, -1);
                yield break;
            }

            _action.Drive(0f);
            if (!converged)
            {
                _training = false;
                SetPhase(StationPhase.Stalled);
                SetHint(RideControlIds.Eta, 1);
                yield break;
            }

            Evaluate();
            _training = false;
            if (_lastPassed)
            {
                Say(RideLine.GradientDescent);
                Solve();
            }
            else
            {
                SetPhase(StationPhase.Tuning);
            }
        }

        private IEnumerator HintRoutine()
        {
            yield return new WaitForSeconds(hintAfterSeconds);
            if (!_active || _solved) yield break;
            // The arrows on the levers do the prompting; no extra narration.
        }

        private void Solve()
        {
            if (_solved) return;
            _solved = true;
            SetHint(null, 0);
            SetPhase(StationPhase.Solved);
            if (kind == StationKind.OneSignal) Say(RideLine.WeightTimesInput);
            else if (kind == StationKind.TwoSignals) Say(RideLine.OrBuilt);

            float lr = learningRates[Mathf.Clamp(Mathf.RoundToInt(_eta.Value), 0, learningRates.Length - 1)];
            Solved?.Invoke(new StationSummary(kind, _rock.Value, _ice.Value, _trigger.Value, lr));
        }

        private void SetPhase(StationPhase phase) => PhaseChanged?.Invoke(kind, phase);

        private void Say(RideLine line) => Said?.Invoke(line);

        private void OnDisable() => End();
    }
}
