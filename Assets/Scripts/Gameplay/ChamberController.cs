using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Central gameplay state machine for Level 1 — The Awakening Gate.
    /// Orchestrates phase transitions, receives interaction commands, invokes pure Core evaluation,
    /// and fires events for Presentation and GatewayController. Never fakes completion.
    /// </summary>
    public class ChamberController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private PerformanceTracker performanceTracker;

        [Header("Starting Preset")]
        [SerializeField] private BrokenConfigurationSO initialPreset;

        private PuzzleDefinition _puzzle;
        private bool _hasSolved;

        public ChamberPhase Phase { get; private set; } = ChamberPhase.Arrival;
        public PuzzleDefinition Puzzle => _puzzle;
        public PuzzleEvaluation LastEvaluation { get; private set; }
        public bool HasSolved => _hasSolved;

        // Gameplay events consumed by Presentation and Gateway
        public event Action<ChamberPhase> OnPhaseChanged;
        public event Action OnForwardPassTriggered;
        public event Action<PuzzleEvaluation> OnEvaluationComplete;
        public event Action OnPuzzleSolved;
        public event Action OnChamberReset;

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
        }

        public void SetPhase(ChamberPhase newPhase)
        {
            if (Phase == newPhase) return;
            Phase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        /// <summary>
        /// Command received from XR tools or desktop fallback to fire a test pulse and evaluate the circuit.
        /// </summary>
        public PuzzleEvaluation TriggerForwardPass()
        {
            if (Phase == ChamberPhase.Arrival)
            {
                SetPhase(ChamberPhase.NeuralRepair);
            }

            OnForwardPassTriggered?.Invoke();
            performanceTracker?.RecordPulseFired();

            // Construct effective network reflecting physical cable connections
            NetworkModel evalModel;
            if (!neuralState.Cable1Connected || !neuralState.Cable2Connected)
            {
                // Disconnected conduit drops that input connection to zero
                double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
                double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
                evalModel = NetworkModel.CreateSingleNeuronNetwork(2, new[] { w1, w2 }, neuralState.Bias, neuralState.Activation);
            }
            else
            {
                evalModel = neuralState.Network;
            }

            // Real neural evaluation through pure Core
            PuzzleEvaluation evaluation = PuzzleEvaluator.Evaluate(evalModel, _puzzle);
            LastEvaluation = evaluation;

            performanceTracker?.RecordEvaluation(evaluation);
            OnEvaluationComplete?.Invoke(evaluation);

            // One-way idempotent unlock authority
            if (evaluation.Passed)
            {
                if (!_hasSolved)
                {
                    _hasSolved = true;
                    SetPhase(ChamberPhase.Awakening);
                    OnPuzzleSolved?.Invoke();
                }
            }

            return evaluation;
        }

        /// <summary>
        /// Internal chamber reset invoked by LevelResetter.
        /// </summary>
        internal void NotifyReset()
        {
            _hasSolved = false;
            LastEvaluation = null;
            SetPhase(ChamberPhase.Arrival);
            OnChamberReset?.Invoke();
        }
    }
}
