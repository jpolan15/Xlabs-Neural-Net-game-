using System;
using System.Collections;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Re-evaluates on every control change through PuzzleEvaluator, and picks a one-control hint.
    /// Presentation does not call Evaluate.
    /// </summary>
    public sealed class LiveEvaluationRelay : MonoBehaviour
    {
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private float hintIdleSeconds = 45f;
        [SerializeField] private float engageCaseSeconds = 6f;

        bool _busy;
        float _idle;
        bool _engaging;
        Coroutine _engage;

        public event Action<string> OnHintControl;
        public event Action<int> OnEngageCase;

        void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        void OnEnable()
        {
            if (neuralState != null) neuralState.OnStateMutated += HandleMutated;
        }

        void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleMutated;
        }

        void Update()
        {
            if (chamberController == null) return;
            PuzzleEvaluation last = chamberController.LastEvaluation;
            if (last != null && last.Passed)
            {
                _idle = 0f;
                return;
            }

            _idle += Time.deltaTime;
            if (_idle < hintIdleSeconds) return;
            _idle = 0f;
            PublishHint();
        }

        void HandleMutated()
        {
            _idle = 0f;
            if (_busy || chamberController == null) return;
            _busy = true;
            chamberController.TriggerForwardPass();
            _busy = false;
        }

        public void BeginEngage()
        {
            if (_engaging || chamberController == null) return;
            if (_engage != null) StopCoroutine(_engage);
            _engage = StartCoroutine(Engage());
        }

        IEnumerator Engage()
        {
            _engaging = true;
            int count = chamberController.Puzzle != null ? chamberController.Puzzle.TestCases.Count : 0;
            for (int i = 0; i < count; i++)
            {
                chamberController.TriggerSingleCasePass(i);
                OnEngageCase?.Invoke(i);
                yield return new WaitForSeconds(engageCaseSeconds);
            }

            _engaging = false;
        }

        void PublishHint()
        {
            if (neuralState == null || neuralState.Network == null || chamberController.Puzzle == null) return;
            NetworkModel baseline = chamberController.GetEffectiveNetwork();
            if (baseline == null || baseline.SingleNeuron == null) return;
            PuzzleEvaluation now = PuzzleEvaluator.Evaluate(baseline.DeepCopy(), chamberController.Puzzle);
            int best = now.PassedCases;
            string bestId = null;
            double bestDelta = double.MaxValue;
            Consider(baseline, 0, neuralState.Weight1, ref best, ref bestId, ref bestDelta);
            Consider(baseline, 1, neuralState.Weight2, ref best, ref bestId, ref bestDelta);
            ConsiderBias(baseline, neuralState.Bias, ref best, ref bestId, ref bestDelta);
            if (bestId != null) OnHintControl?.Invoke(bestId);
        }

        void Consider(NetworkModel baseline, int index, double current, ref int best, ref string bestId, ref double bestDelta)
        {
            TryStep(baseline, index, current, -0.5, ref best, ref bestId, ref bestDelta);
            TryStep(baseline, index, current, 0.5, ref best, ref bestId, ref bestDelta);
        }

        void TryStep(NetworkModel baseline, int index, double current, double delta, ref int best, ref string bestId, ref double bestDelta)
        {
            double next = current + delta;
            if (next < -2.0 || next > 2.0) return;
            NetworkModel trial = baseline.DeepCopy();
            NeuronModel neuron = trial.SingleNeuron;
            if (neuron == null || index >= neuron.WeightCount) return;
            neuron.SetWeight(index, next);
            int score = PuzzleEvaluator.Evaluate(trial, chamberController.Puzzle).PassedCases;
            double abs = System.Math.Abs(delta);
            if (score > best || (bestId != null && score == best && abs < bestDelta))
            {
                best = score;
                bestDelta = abs;
                bestId = index == 0 ? "ROCK" : "ICE";
            }
        }

        void ConsiderBias(NetworkModel baseline, double current, ref int best, ref string bestId, ref double bestDelta)
        {
            TryBias(baseline, current, -0.5, ref best, ref bestId, ref bestDelta);
            TryBias(baseline, current, 0.5, ref best, ref bestId, ref bestDelta);
        }

        void TryBias(NetworkModel baseline, double current, double delta, ref int best, ref string bestId, ref double bestDelta)
        {
            double next = current + delta;
            if (next < -2.0 || next > 2.0) return;
            NetworkModel trial = baseline.DeepCopy();
            if (trial.SingleNeuron == null) return;
            trial.SingleNeuron.Bias = next;
            int score = PuzzleEvaluator.Evaluate(trial, chamberController.Puzzle).PassedCases;
            double abs = System.Math.Abs(delta);
            if (score > best || (bestId != null && score == best && abs < bestDelta))
            {
                best = score;
                bestDelta = abs;
                bestId = "BIAS";
            }
        }
    }
}
