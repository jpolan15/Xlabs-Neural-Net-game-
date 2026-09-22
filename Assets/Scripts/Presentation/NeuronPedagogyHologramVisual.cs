using System;
using System.Text;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// In-World Educational 3D Hologram Station for Level 1.
    /// Teaches the foundational architecture of the simplest Artificial Neuron (Perceptron)
    /// through the concrete lens of the Sector 01 Hazard Classifier:
    /// - Inputs: Radiation Detector (x1) & Bio-Leak Detector (x2)
    /// - Synaptic Weights (w1, w2): Sensor Sensitivity / Volume Knobs
    /// - Noise Bias (b): Background Noise Filter / Threshold Offset
    /// - Summation Core: Total Hazard Energy z = (w1 × x1) + (w2 × x2) + b
    /// - Activation Function: Step Decision Gate y = Step(z) [1 = Lockdown Alarm, 0 = Safe]
    /// Strictly Presentation layer: observes state and never evaluates or decides puzzle correctness.
    /// </summary>
    [ExecuteAlways]
    public class NeuronPedagogyHologramVisual : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("3D Text Displays")]
        [SerializeField] private TextMesh headerTextMesh;
        [SerializeField] private TextMesh formulaTextMesh;
        [SerializeField] private TextMesh explanationTextMesh;
        [SerializeField] private TextMesh statusBadgeTextMesh;

        [Header("Backlight & Frame")]
        [SerializeField] private Light stationLight;
        [SerializeField] private Renderer panelRenderer;

        [Header("Color Palette")]
        [SerializeField] private Color neonCyan = new Color(0.25f, 0.80f, 1.0f, 1.0f);
        [SerializeField] private Color neonEmerald = new Color(0.10f, 0.90f, 0.50f, 1.0f);
        [SerializeField] private Color neonAmber = new Color(0.98f, 0.65f, 0.10f, 1.0f);
        [SerializeField] private Color neonCoral = new Color(0.95f, 0.35f, 0.35f, 1.0f);

        private readonly StringBuilder _formulaBuilder = new StringBuilder(256);
        private readonly StringBuilder _explanationBuilder = new StringBuilder(512);

        private double _prevW1;
        private double _prevW2;
        private double _prevBias;
        private ActivationType _prevActivation;
        private bool _prevC1;
        private bool _prevC2;
        private string _dynamicInsight = "Connect both sensor conduits [1] & [2] to restore AI perception.";

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        private void OnEnable()
        {
            if (neuralState != null)
            {
                CacheCurrentState();
                neuralState.OnStateMutated += HandleStateChanged;
            }
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
        }

        private void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleStateChanged;
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
        }

        private void Start()
        {
            CacheCurrentState();
            UpdateEducationalDisplay();
        }

        private void Update()
        {
            // Subtle ambient floating hover
            float floatOffset = Mathf.Sin(Time.time * 1.8f) * 0.025f;
            transform.localPosition += new Vector3(0, floatOffset * Time.deltaTime, 0);

            UpdateEducationalDisplay();
        }

        private void CacheCurrentState()
        {
            if (neuralState == null) return;
            _prevW1 = neuralState.Weight1;
            _prevW2 = neuralState.Weight2;
            _prevBias = neuralState.Bias;
            _prevActivation = neuralState.Activation;
            _prevC1 = neuralState.Cable1Connected;
            _prevC2 = neuralState.Cable2Connected;
        }

        private void HandleStateChanged()
        {
            if (neuralState == null) return;

            if (neuralState.Cable1Connected != _prevC1 || neuralState.Cable2Connected != _prevC2)
            {
                _dynamicInsight = neuralState.Cable1Connected && neuralState.Cable2Connected
                    ? "Both sensor lines active. Adjust sensitivity weights to respond to incoming hazards."
                    : "Warning: Disconnected conduits zero out incoming sensor telemetry.";
            }
            else if (Math.Abs(neuralState.Weight1 - _prevW1) > 0.01 || Math.Abs(neuralState.Weight2 - _prevW2) > 0.01)
            {
                double maxW = Math.Max(neuralState.Weight1, neuralState.Weight2);
                double minW = Math.Min(neuralState.Weight1, neuralState.Weight2);
                if (minW <= 0.0)
                {
                    _dynamicInsight = "Low sensor sensitivity: Weak signals may fail to breach the lockdown threshold.";
                }
                else
                {
                    _dynamicInsight = "Sensors amplified: Hazard detections will generate strong positive energy.";
                }
            }
            else if (Math.Abs(neuralState.Bias - _prevBias) > 0.01)
            {
                _dynamicInsight = neuralState.Bias < 0
                    ? "Noise barrier active: Suppresses false alarms during clean background states."
                    : "Warning: Positive bias causes false alarms even when all rooms are safe.";
            }
            else if (neuralState.Activation != _prevActivation)
            {
                _dynamicInsight = neuralState.Activation == ActivationType.Step
                    ? "Step Crystal active: Outputs a decisive binary 0 (Safe) or 1 (Lockdown Alarm)."
                    : $"Activation is {neuralState.Activation}. Requires a binary step switch for quarantine control.";
            }

            CacheCurrentState();
            UpdateEducationalDisplay();
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (eval != null && eval.Passed)
            {
                if (stationLight != null) stationLight.color = neonEmerald;
                if (statusBadgeTextMesh != null)
                {
                    statusBadgeTextMesh.text = "★ 100% HARMONIZED — QUARANTINE PROTOCOL RESTORED ★";
                    statusBadgeTextMesh.color = neonEmerald;
                }
            }
            else
            {
                if (stationLight != null) stationLight.color = neonCyan;
                if (statusBadgeTextMesh != null)
                {
                    statusBadgeTextMesh.text = "CALIBRATION IN PROGRESS...";
                    statusBadgeTextMesh.color = neonAmber;
                }
            }
        }

        private void HandleChamberReset()
        {
            if (stationLight != null) stationLight.color = neonCyan;
            if (statusBadgeTextMesh != null)
            {
                statusBadgeTextMesh.text = "AWAITING SENSOR CALIBRATION";
                statusBadgeTextMesh.color = neonCyan;
            }
            _dynamicInsight = "Connect both sensor conduits [1] & [2] to restore AI perception.";
            UpdateEducationalDisplay();
        }

        public void UpdateEducationalDisplay()
        {
            if (neuralState == null) return;

            double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
            double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
            double b = neuralState.Bias;
            string actName = neuralState.Activation.ToString();

            // Header
            if (headerTextMesh != null)
            {
                headerTextMesh.text = "HAZARD CLASSIFIER NEURON";
                headerTextMesh.color = new Color(0.35f, 0.85f, 1.0f);
            }

            // Live Mathematical Formula Breakdown
            if (formulaTextMesh != null)
            {
                _formulaBuilder.Length = 0;
                _formulaBuilder.AppendLine("1. SENSOR SUMMATION (Total Energy z):");
                _formulaBuilder.AppendLine($"   z = ({w1:+0.0;-0.0;0.0} × x1) + ({w2:+0.0;-0.0;0.0} × x2) + ({b:+0.0;-0.0;0.0})");
                _formulaBuilder.AppendLine();
                _formulaBuilder.AppendLine("2. ACTIVATION GATE (Quarantine Switch):");
                _formulaBuilder.AppendLine($"   y = {actName}(z)  [Alarm=1 if z >= 0; Safe=0 if z < 0]");
                formulaTextMesh.text = _formulaBuilder.ToString();
                formulaTextMesh.color = Color.white;
            }

            // Educational Explanations with Live Dynamic Insight
            if (explanationTextMesh != null)
            {
                _explanationBuilder.Length = 0;
                _explanationBuilder.AppendLine(">> LIVE NEURAL DIAGNOSTIC:");
                _explanationBuilder.AppendLine($"   {_dynamicInsight}");
                _explanationBuilder.AppendLine();
                _explanationBuilder.AppendLine("• ALLY RULE  : Friendly Drone (0,0) -> Total Energy z < 0 -> Hold Fire (0) [SPARE ALLY]");
                _explanationBuilder.AppendLine("• THREAT RULE: Any Hazard (Rad OR Bio) -> Total Energy z >= 0 -> Plasma Intercept (1) [DESTROY THREAT]");
                explanationTextMesh.text = _explanationBuilder.ToString();
                explanationTextMesh.color = new Color(0.85f, 0.90f, 0.98f);
            }
        }
    }
}
