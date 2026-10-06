using System;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Central gameplay state machine for Level 1 — The Sentry Intercept Crisis.
    /// Orchestrates live purge countdown timer, station shield integrity, advancing threat waves,
    /// breach penalties, and invokes pure Core evaluation.
    /// Never fakes completion.
    /// </summary>
    public class ChamberController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private PerformanceTracker performanceTracker;

        [Header("Starting Preset")]
        [SerializeField] private BrokenConfigurationSO initialPreset;

        [Header("Data Target Receptors")]
        [SerializeField] private List<DataTargetReceptor> targetReceptors = new List<DataTargetReceptor>();

        [Header("Live Purge & Wave Settings")]
        [SerializeField] private float maxPurgeTime = 90.0f;
        [SerializeField] private float purgeTimer = 90.0f;
        [SerializeField] private float maxShieldIntegrity = 100.0f;
        [SerializeField] private float shieldIntegrity = 100.0f;
        [SerializeField] private float waveApproachDuration = 22.0f; // Seconds for wave to reach perimeter
        [SerializeField] private bool enableLiveCountdown = false;  // OFF by default — Level 1 is a sandbox

        [Header("Sandbox / Difficulty")]
        [Tooltip("Sandbox mode: disables timer countdown, shield damage, and wave perimeter penalties. Default ON for Level 1.")]
        [SerializeField] private bool enableSandboxMode = true;

        [Tooltip("When true, AsteroidDefenseDirector owns waves and scans. The built-in wave loop stays off.")]
        [SerializeField] private bool externalWaveDirector = false;

        [Tooltip("At 0 hull, restore a fraction and keep the player's settings instead of purging the chamber.")]
        [SerializeField] private bool enableSoftReroute = false;

        [SerializeField] private float rerouteRestoreFraction = 0.6f;

        private PuzzleDefinition _puzzle;
        private bool _hasSolved;
        private float _waveTimer;
        private bool _isPurged;

        public ChamberPhase Phase { get; private set; } = ChamberPhase.Arrival;
        public PuzzleDefinition Puzzle => _puzzle;
        public PuzzleEvaluation LastEvaluation { get; private set; }
        public bool HasSolved => _hasSolved;
        public IReadOnlyList<DataTargetReceptor> TargetReceptors => targetReceptors;
        public int PulseCount => performanceTracker != null ? performanceTracker.PulsesFired : 0;
        public PerformanceTracker Tracker => performanceTracker;

        public float PurgeTimer => purgeTimer;
        public float MaxPurgeTime => maxPurgeTime;
        public float ShieldIntegrity => shieldIntegrity;
        public float MaxShieldIntegrity => maxShieldIntegrity;
        public float WaveProgress => Mathf.Clamp01(_waveTimer / waveApproachDuration);

        // Gameplay events consumed by Presentation, XR, and Gateway
        public event Action<ChamberPhase> OnPhaseChanged;
        public event Action OnForwardPassTriggered;
        public event Action<int, CaseDiagnostic, bool> OnSingleCaseEvaluated;
        public event Action<PuzzleEvaluation> OnEvaluationComplete;
        public event Action OnPuzzleSolved;
        public event Action OnChamberReset;

        public event Action<float, float> OnTimerUpdated;
        public event Action<float, float> OnShieldUpdated;
        public event Action<float> OnShieldDamaged; // Damage amount (for screen shake / alarm)
        public event Action OnHullRerouted;
        public event Action<int, string> OnPerimeterBreached;
        public event Action<int, string> OnFriendlyCasualty;
        public event Action OnEmergencyPurge;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = GetComponent<NeuralState>() ?? gameObject.AddComponent<NeuralState>();
            }
            if (performanceTracker == null)
            {
                performanceTracker = GetComponent<PerformanceTracker>() ?? gameObject.AddComponent<PerformanceTracker>();
            }

            _puzzle = PuzzleDefinition.CreateORGatePuzzle();
        }

        private void Start()
        {
            if (initialPreset != null)
            {
                initialPreset.ApplyTo(neuralState);
            }
            SetPhase(ChamberPhase.Arrival);
            ResetPurgeState();
        }

        private void Update()
        {
            if (externalWaveDirector) return;
            if (_hasSolved || _isPurged || Phase == ChamberPhase.Arrival) return;

            // 1. Tick Purge Countdown Timer
            if (enableLiveCountdown && purgeTimer > 0f)
            {
                purgeTimer -= Time.deltaTime;
                OnTimerUpdated?.Invoke(purgeTimer, maxPurgeTime);

                if (purgeTimer <= 0f)
                {
                    purgeTimer = 0f;
                    TriggerEmergencyPurge("SECTOR EMERGENCY PURGE: TIME EXPIRED");
                    return;
                }
            }

            // 2. Advance Live Wave Along Approach Corridor
            _waveTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_waveTimer / waveApproachDuration);

            for (int i = 0; i < targetReceptors.Count; i++)
            {
                var r = targetReceptors[i];
                if (r != null && !r.IsVaporized && !r.IsDocked)
                {
                    r.SetApproachProgress(progress);
                }
            }

            // 3. Perimeter Breach Check — skipped in sandbox mode
            if (!enableSandboxMode && _waveTimer >= waveApproachDuration)
            {
                HandleWavePerimeterReached();
                _waveTimer = 0f; // Loop wave
            }
            else if (enableSandboxMode && _waveTimer >= waveApproachDuration)
            {
                _waveTimer = 0f; // Just loop the wave visually, no penalty
            }
        }

        public void SetExternalWaveDirector(bool enabled)
        {
            externalWaveDirector = enabled;
        }

        public void ReportFriendlyCasualty(int index, string message)
        {
            OnFriendlyCasualty?.Invoke(index, message);
        }

        public void ReportPerimeterBreach(int index, string message)
        {
            OnPerimeterBreached?.Invoke(index, message);
        }

        public void SetPhase(ChamberPhase newPhase)
        {
            if (Phase == newPhase) return;
            Phase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        public NetworkModel GetEffectiveNetwork()
        {
            if (neuralState == null) return null;

            if (neuralState.Network == null) return null;

            if (neuralState.Cable1Connected && neuralState.Cable2Connected)
            {
                return neuralState.Network;
            }

            NetworkModel copy = neuralState.Network.DeepCopy();
            var first = copy.Layers[0];
            for (int n = 0; n < first.NeuronCount; n++)
            {
                var neuron = first.Neurons[n];
                if (!neuralState.Cable1Connected && neuron.WeightCount > 0) neuron.SetWeight(0, 0.0);
                if (!neuralState.Cable2Connected && neuron.WeightCount > 1) neuron.SetWeight(1, 0.0);
            }

            return copy;
        }

        /// <summary>
        /// Switches the live puzzle without pretending the previous one is still solved.
        /// Does not reset the gateway.
        /// </summary>
        public void BeginPuzzle(PuzzleDefinition puzzle)
        {
            _puzzle = puzzle ?? throw new System.ArgumentNullException(nameof(puzzle));
            _hasSolved = false;
            LastEvaluation = null;
            SetPhase(ChamberPhase.NeuralRepair);
        }

        /// <summary>
        /// Command to fire sentry trial across all approaching targets. (Now Asteroid targets)
        /// </summary>
        public PuzzleEvaluation TriggerForwardPass()
        {

            if (Phase == ChamberPhase.Arrival)
            {
                SetPhase(ChamberPhase.NeuralRepair);
            }

            OnForwardPassTriggered?.Invoke();
            performanceTracker?.RecordPulseFired();

            NetworkModel evalModel = GetEffectiveNetwork();
            PuzzleEvaluation evaluation = PuzzleEvaluator.Evaluate(evalModel, _puzzle);
            LastEvaluation = evaluation;

            DispatchDiagnosticsToReceptors(evaluation);
            if (!externalWaveDirector)
            {
                EvaluateTacticalOutcomes(evaluation);
            }

            performanceTracker?.RecordEvaluation(evaluation);
            OnEvaluationComplete?.Invoke(evaluation);

            if (evaluation.Passed && !_hasSolved)
            {
                _hasSolved = true;
                SetPhase(ChamberPhase.Awakening);
                RestoreShield(35f);
                OnPuzzleSolved?.Invoke();
                UnityEngine.Debug.Log("Sensor array passed. The ship is not clear to jump.");
            }

            return evaluation;
        }

        public CaseDiagnostic TriggerSingleCasePass(int caseIndex)
        {

            if (Phase == ChamberPhase.Arrival)
            {
                SetPhase(ChamberPhase.NeuralRepair);
            }

            performanceTracker?.RecordPulseFired();

            NetworkModel evalModel = GetEffectiveNetwork();
            PuzzleEvaluation evaluation = PuzzleEvaluator.Evaluate(evalModel, _puzzle);
            LastEvaluation = evaluation;

            DispatchDiagnosticsToReceptors(evaluation);

            CaseDiagnostic diag = null;
            if (evaluation.Diagnostics != null && caseIndex >= 0 && caseIndex < evaluation.Diagnostics.Count)
            {
                diag = evaluation.Diagnostics[caseIndex];
                if (caseIndex < targetReceptors.Count && targetReceptors[caseIndex] != null)
                {
                    targetReceptors[caseIndex].NotifyPulseHit();
                }
                OnSingleCaseEvaluated?.Invoke(caseIndex, diag, evaluation.ActivationMatches);
            }

            OnEvaluationComplete?.Invoke(evaluation);

            if (evaluation.Passed && !_hasSolved)
            {
                _hasSolved = true;
                SetPhase(ChamberPhase.Awakening);
                RestoreShield(35f);
                OnPuzzleSolved?.Invoke();
            }

            return diag;
        }

        /// <summary>
        /// Evaluates one case of a captured puzzle for the victory swarm.
        /// Does not change solve state, the live puzzle, the phase, or last evaluation.
        /// </summary>
        public CaseDiagnostic ReplaySingleCase(int caseIndex, PuzzleDefinition puzzle)
        {
            if (puzzle == null) return null;

            NetworkModel evalModel = GetEffectiveNetwork();
            if (evalModel == null) return null;

            PuzzleEvaluation evaluation = PuzzleEvaluator.Evaluate(evalModel, puzzle);
            if (evaluation == null || evaluation.Diagnostics == null) return null;
            if (caseIndex < 0 || caseIndex >= evaluation.Diagnostics.Count) return null;

            CaseDiagnostic diag = evaluation.Diagnostics[caseIndex];
            if (caseIndex < targetReceptors.Count && targetReceptors[caseIndex] != null)
            {
                targetReceptors[caseIndex].ApplyDiagnostic(diag, evaluation.ActivationMatches);
                targetReceptors[caseIndex].NotifyPulseHit();
            }

            OnSingleCaseEvaluated?.Invoke(caseIndex, diag, evaluation.ActivationMatches);
            return diag;
        }

        private void DispatchDiagnosticsToReceptors(PuzzleEvaluation eval)
        {
            if (eval == null || eval.Diagnostics == null) return;

            for (int i = 0; i < targetReceptors.Count && i < eval.Diagnostics.Count; i++)
            {
                if (targetReceptors[i] != null)
                {
                    targetReceptors[i].ApplyDiagnostic(eval.Diagnostics[i], eval.ActivationMatches);
                }
            }
        }

        private void EvaluateTacticalOutcomes(PuzzleEvaluation eval)
        {
            if (eval == null || eval.Diagnostics == null) return;

            for (int i = 0; i < targetReceptors.Count && i < eval.Diagnostics.Count; i++)
            {
                var r = targetReceptors[i];
                var d = eval.Diagnostics[i];
                if (r == null || d == null) continue;

                if (i == 0)
                {
                    // Case 0: Friendly Drone
                    if (d.ActualOutput >= 0.5)
                    {
                        // False Positive: Sentry shot the friendly drone!
                        r.NotifyVaporized();
                        ApplyShieldDamage(15.0f);
                        OnFriendlyCasualty?.Invoke(0, "FALSE ALARM: quiet sensors were marked as a hit.");
                    }
                    else
                    {
                        r.NotifyDocked();
                    }
                }
                else
                {
                    // Cases 1, 2, 3: Hazard Canisters / Drones
                    if (d.ActualOutput >= 0.5)
                    {
                        // Intercepted threat mid-flight
                        r.NotifyVaporized();
                    }
                }
            }
        }

        private void HandleWavePerimeterReached()
        {
            for (int i = 0; i < targetReceptors.Count; i++)
            {
                var r = targetReceptors[i];
                if (r == null) continue;

                if (i > 0 && !r.IsVaporized)
                {
                    // Hazard breached the perimeter!
                    r.NotifyBreached();
                    ApplyShieldDamage(20.0f);
                    OnPerimeterBreached?.Invoke(i, $"CONTAINMENT BREACH: {r.TargetTitle} struck perimeter shield!");
                }

                // Reset for next wave pass if not permanently harmonized
                if (!r.IsHarmonized)
                {
                    r.SetApproachProgress(0f);
                }
            }
        }

        public void ApplyShieldDamage(float damage)
        {
            // In sandbox mode players cannot lose — no shield damage applied
            if (_hasSolved || _isPurged || enableSandboxMode) return;

            shieldIntegrity = Mathf.Clamp(shieldIntegrity - damage, 0f, maxShieldIntegrity);
            OnShieldDamaged?.Invoke(damage);
            OnShieldUpdated?.Invoke(shieldIntegrity, maxShieldIntegrity);

            if (shieldIntegrity <= 0f)
            {
                if (enableSoftReroute)
                {
                    float restored = maxShieldIntegrity * Mathf.Clamp01(rerouteRestoreFraction);
                    shieldIntegrity = restored;
                    OnShieldUpdated?.Invoke(shieldIntegrity, maxShieldIntegrity);
                    OnHullRerouted?.Invoke();
                }
                else
                {
                    TriggerEmergencyPurge("CRITICAL FAILURE: CONTAINMENT SHIELD COLLAPSED");
                }
            }
        }

        public void RestoreShield(float amount)
        {
            shieldIntegrity = Mathf.Clamp(shieldIntegrity + amount, 0f, maxShieldIntegrity);
            OnShieldUpdated?.Invoke(shieldIntegrity, maxShieldIntegrity);
        }

        private void TriggerEmergencyPurge(string reason)
        {
            _isPurged = true;
            OnEmergencyPurge?.Invoke();
        }

        public void ResetPurgeState()
        {
            _isPurged = false;
            _hasSolved = false;
            purgeTimer = maxPurgeTime;
            shieldIntegrity = maxShieldIntegrity;
            _waveTimer = 0f;
            LastEvaluation = null;

            foreach (var r in targetReceptors)
            {
                if (r != null) r.ResetReceptor();
            }

            OnTimerUpdated?.Invoke(purgeTimer, maxPurgeTime);
            OnShieldUpdated?.Invoke(shieldIntegrity, maxShieldIntegrity);
            OnChamberReset?.Invoke();
        }

        public void RegisterTargetReceptor(DataTargetReceptor receptor)
        {
            if (receptor != null && !targetReceptors.Contains(receptor))
            {
                targetReceptors.Add(receptor);
            }
        }

        internal void NotifyReset()
        {
            ResetPurgeState();
            SetPhase(ChamberPhase.Arrival);
        }
    }
}
