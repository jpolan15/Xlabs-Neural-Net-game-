using System;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    public enum DefenseOutcome
    {
        Vaporized = 0,
        FriendlyFire = 1,
        Docked = 2,
        Breach = 3
    }

    /// <summary>
    /// Tick-driven point defense for Chamber 01. The neuron decides. This class only moves objects and
    /// applies the outcome <see cref="PuzzleEvaluator"/> already computed.
    /// </summary>
    public class AsteroidDefenseDirector : MonoBehaviour
    {
        enum Mode
        {
            Idle = 0,
            Lead = 1,
            Combat = 2,
            Gap = 3,
            Drain = 4,
            SolvedWait = 5,
            Victory = 6,
            Lesson = 7,
            Pause = 8,
            Done = 9
        }

        sealed class Flight
        {
            public DataTargetReceptor receptor;
            public float elapsed;
            public float duration;
            public bool active;
            public bool scanned;
            public bool dockAtEnd;
            public bool breachAtEnd;
        }

        [SerializeField] private ChamberController chamber;
        [SerializeField] private GatewayController gateway;
        [SerializeField] private ChamberOnboardingController onboarding;

        [SerializeField] private float travelSeconds = 8f;
        [SerializeField] private float launchSpacing = 3f;
        [SerializeField] private float waveGap = 4f;
        [SerializeField] private float briefingLead = 18.24f;
        [SerializeField] private float scanProgress = 0.65f;
        [SerializeField] private float breachDamage = 12f;
        [SerializeField] private float friendlyDamage = 6f;
        [SerializeField] private float dockRestore = 4f;
        [SerializeField] private float reroutePause = 4f;
        [SerializeField] private float solvedWait = 4.5f;
        [SerializeField] private int victoryWaveCount = 3;
        [SerializeField] private float victoryTravel = 3.5f;
        [SerializeField] private float victorySpacing = 0.8f;
        [SerializeField] private float lessonDelay = 9.5f;
        [SerializeField] private int shuffleSeed = 7;

        readonly Flight[] _pool = new Flight[8];
        readonly int[] _queue = new int[4];
        int _queueCount;
        System.Random _rng;
        PuzzleDefinition _defensePuzzle;
        Mode _mode = Mode.Idle;
        Mode _pauseReturn = Mode.Combat;
        bool _booted;
        bool _briefed;
        bool _solved;
        bool _rerouteRequested;
        float _timer;
        float _launchWait;
        float _activeTravel;
        float _activeSpacing;
        int _waveIndex;
        int _victoryLeft;

        public bool Solved => _solved;

        public event Action<int, bool> OnWaveStarted;
        public event Action<DataTargetReceptor> OnObjectLaunched;
        public event Action<DataTargetReceptor, CaseDiagnostic, bool> OnScan;
        public event Action<DataTargetReceptor, DefenseOutcome> OnOutcome;
        public event Action OnVictoryStarted;
        public event Action OnVictoryComplete;
        public event Action OnRestored;

        void Awake()
        {
            for (int i = 0; i < _pool.Length; i++) _pool[i] = new Flight();
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            if (gateway == null) gateway = FindAnyObjectByType<GatewayController>();
            if (onboarding == null) onboarding = FindAnyObjectByType<ChamberOnboardingController>();
        }

        void OnEnable()
        {
            if (onboarding != null) onboarding.OnOpeningBriefing += NotifyOpeningBriefing;
            if (chamber != null)
            {
                chamber.OnPuzzleSolved += HandleSolved;
                chamber.OnHullRerouted += HandleReroute;
                chamber.OnChamberReset += HandleReset;
            }
        }

        void OnDisable()
        {
            if (onboarding != null) onboarding.OnOpeningBriefing -= NotifyOpeningBriefing;
            if (chamber != null)
            {
                chamber.OnPuzzleSolved -= HandleSolved;
                chamber.OnHullRerouted -= HandleReroute;
                chamber.OnChamberReset -= HandleReset;
            }
        }

        void Start()
        {
            Boot();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Boot()
        {
            if (_booted) return;
            _booted = true;
            _rng = new System.Random(shuffleSeed);
            if (chamber != null)
            {
                _defensePuzzle = chamber.Puzzle;
                chamber.SetExternalWaveDirector(true);
                IReadOnlyList<DataTargetReceptor> list = chamber.TargetReceptors;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null) list[i].Retire();
                }
            }
        }

        public void NotifyOpeningBriefing()
        {
            if (_briefed) return;
            Boot();
            _briefed = true;
            _mode = Mode.Lead;
            _timer = briefingLead;
        }

        public void Tick(float dt)
        {
            if (dt < 0f) dt = 0f;
            Boot();

            if (_mode == Mode.Lead || _mode == Mode.Gap || _mode == Mode.Pause || _mode == Mode.SolvedWait || _mode == Mode.Lesson)
            {
                _timer -= dt;
                if (_timer <= 0f) AdvanceTimerMode();
            }

            if (_mode == Mode.Combat || _mode == Mode.Victory)
            {
                _launchWait -= dt;
                if (_launchWait <= 0f && _queueCount > 0)
                {
                    LaunchNext();
                    _launchWait = _activeSpacing;
                }
            }

            if (_mode == Mode.Combat || _mode == Mode.Drain || _mode == Mode.Victory)
            {
                AdvanceFlights(dt);
            }

            if (_rerouteRequested)
            {
                _rerouteRequested = false;
                ClearFlights();
                if (_mode == Mode.Combat || _mode == Mode.Drain || _mode == Mode.Gap || _mode == Mode.Victory || _mode == Mode.Lead)
                {
                    _pauseReturn = _mode == Mode.Victory ? Mode.Victory : (_solved ? Mode.Drain : Mode.Combat);
                    _mode = Mode.Pause;
                    _timer = reroutePause;
                }
            }

            if (_mode == Mode.Combat && _queueCount == 0 && !AnyFlight())
            {
                _mode = Mode.Gap;
                _timer = waveGap;
            }
            else if (_mode == Mode.Drain && !AnyFlight())
            {
                _mode = Mode.SolvedWait;
                _timer = solvedWait;
            }
            else if (_mode == Mode.Victory && _queueCount == 0 && !AnyFlight())
            {
                FinishVictoryWave();
            }
        }

        void AdvanceTimerMode()
        {
            switch (_mode)
            {
                case Mode.Lead:
                case Mode.Gap:
                    if (_solved)
                    {
                        _mode = Mode.Drain;
                        return;
                    }
                    BeginWave(false);
                    _mode = Mode.Combat;
                    break;
                case Mode.Pause:
                    _mode = _pauseReturn;
                    if (_mode == Mode.Victory && _queueCount == 0 && !AnyFlight())
                    {
                        BeginWave(true);
                    }
                    else if (_mode == Mode.Combat && _queueCount == 0 && !AnyFlight())
                    {
                        BeginWave(false);
                    }
                    break;
                case Mode.SolvedWait:
                    BeginVictory();
                    break;
                case Mode.Lesson:
                    OnRestored?.Invoke();
                    if (gateway != null) gateway.Open();
                    _mode = Mode.Done;
                    break;
            }
        }

        void BeginVictory()
        {
            _victoryLeft = victoryWaveCount;
            OnVictoryStarted?.Invoke();
            BeginWave(true);
            _mode = Mode.Victory;
        }

        void FinishVictoryWave()
        {
            _victoryLeft--;
            if (_victoryLeft > 0)
            {
                BeginWave(true);
                return;
            }
            OnVictoryComplete?.Invoke();
            _mode = Mode.Lesson;
            _timer = lessonDelay;
        }

        void BeginWave(bool victory)
        {
            _queue[0] = 0;
            _queue[1] = 1;
            _queue[2] = 2;
            _queue[3] = 3;
            for (int i = 3; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                int tmp = _queue[i];
                _queue[i] = _queue[j];
                _queue[j] = tmp;
            }
            _queueCount = 4;
            _launchWait = 0f;
            _activeTravel = victory ? victoryTravel : travelSeconds;
            _activeSpacing = victory ? victorySpacing : launchSpacing;
            if (!victory && _waveIndex == 0)
            {
                for (int i = 1; i < 4; i++)
                {
                    if (_queue[i] != 2) continue;
                    int swap = _queue[0];
                    _queue[0] = _queue[i];
                    _queue[i] = swap;
                    break;
                }
                _activeTravel = travelSeconds * 1.15f;
                _activeSpacing = launchSpacing * 1.1f;
            }
            OnWaveStarted?.Invoke(_waveIndex, victory);
            _waveIndex++;
        }

        void LaunchNext()
        {
            if (_queueCount <= 0 || chamber == null) return;
            int caseIndex = _queue[0];
            for (int i = 1; i < _queueCount; i++) _queue[i - 1] = _queue[i];
            _queueCount--;

            DataTargetReceptor receptor = FindReceptor(caseIndex);
            if (receptor == null) return;

            Flight flight = Rent();
            if (flight == null) return;

            receptor.Launch();
            flight.receptor = receptor;
            flight.elapsed = 0f;
            flight.duration = Mathf.Max(0.1f, _activeTravel);
            flight.active = true;
            flight.scanned = false;
            flight.dockAtEnd = false;
            flight.breachAtEnd = false;
            OnObjectLaunched?.Invoke(receptor);
        }

        void AdvanceFlights(float dt)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                Flight flight = _pool[i];
                if (!flight.active || flight.receptor == null) continue;

                flight.elapsed += dt;
                float progress = flight.elapsed / flight.duration;
                if (progress > 1f) progress = 1f;
                flight.receptor.SetApproachProgress(progress);

                if (!flight.scanned && progress >= scanProgress)
                {
                    Scan(flight);
                }

                if (!flight.active) continue;

                if (flight.elapsed >= flight.duration)
                {
                    Arrive(flight);
                }
            }
        }

        void Scan(Flight flight)
        {
            flight.scanned = true;
            if (chamber == null) return;

            int caseIndex = flight.receptor.CaseIndex;
            CaseDiagnostic diag = _solved
                ? chamber.ReplaySingleCase(caseIndex, _defensePuzzle)
                : chamber.TriggerSingleCasePass(caseIndex);

            if (diag == null) return;

            bool fired = diag.ActualOutput >= 0.5;
            bool expectFire = diag.ExpectedOutput >= 0.5;
            OnScan?.Invoke(flight.receptor, diag, fired);

            if (fired && expectFire)
            {
                flight.receptor.NotifyVaporized();
                flight.active = false;
                OnOutcome?.Invoke(flight.receptor, DefenseOutcome.Vaporized);
            }
            else if (fired && !expectFire)
            {
                flight.receptor.NotifyVaporized();
                flight.active = false;
                if (!_solved)
                {
                    chamber.ApplyShieldDamage(friendlyDamage);
                    chamber.ReportFriendlyCasualty(caseIndex, "The laser burned the repair drone.");
                }
                OnOutcome?.Invoke(flight.receptor, DefenseOutcome.FriendlyFire);
            }
            else if (!expectFire)
            {
                flight.dockAtEnd = true;
            }
            else
            {
                flight.breachAtEnd = true;
            }
        }

        void Arrive(Flight flight)
        {
            flight.active = false;
            if (flight.receptor == null) return;

            if (flight.dockAtEnd)
            {
                flight.receptor.NotifyDocked();
                if (!_solved && chamber != null) chamber.RestoreShield(dockRestore);
                OnOutcome?.Invoke(flight.receptor, DefenseOutcome.Docked);
            }
            else if (flight.breachAtEnd)
            {
                flight.receptor.NotifyBreached();
                if (!_solved && chamber != null)
                {
                    chamber.ApplyShieldDamage(breachDamage);
                    chamber.ReportPerimeterBreach(flight.receptor.CaseIndex, flight.receptor.TargetTitle + " struck the hull.");
                }
                OnOutcome?.Invoke(flight.receptor, DefenseOutcome.Breach);
            }
        }

        void HandleSolved()
        {
            if (_solved) return;
            _solved = true;
            _queueCount = 0;
            if (_mode != Mode.Victory && _mode != Mode.Lesson && _mode != Mode.Done && _mode != Mode.SolvedWait)
            {
                _mode = Mode.Drain;
            }
        }

        void HandleReroute()
        {
            _rerouteRequested = true;
        }

        void HandleReset()
        {
            _solved = false;
            _victoryLeft = 0;
            _queueCount = 0;
            _waveIndex = 0;
            ClearFlights();
            if (chamber != null) _defensePuzzle = chamber.Puzzle;
            if (_briefed)
            {
                _mode = Mode.Lead;
                _timer = waveGap;
            }
            else
            {
                _mode = Mode.Idle;
            }
        }

        void ClearFlights()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].active && _pool[i].receptor != null) _pool[i].receptor.Retire();
                _pool[i].active = false;
                _pool[i].receptor = null;
            }
        }

        bool AnyFlight()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].active) return true;
            }
            return false;
        }

        Flight Rent()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].active) return _pool[i];
            }
            return null;
        }

        DataTargetReceptor FindReceptor(int caseIndex)
        {
            if (chamber == null) return null;
            IReadOnlyList<DataTargetReceptor> list = chamber.TargetReceptors;
            for (int i = 0; i < list.Count; i++)
            {
                DataTargetReceptor receptor = list[i];
                if (receptor != null && receptor.CaseIndex == caseIndex) return receptor;
            }
            return null;
        }
    }
}
