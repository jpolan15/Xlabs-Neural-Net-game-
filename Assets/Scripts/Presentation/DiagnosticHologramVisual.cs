using System;
using System.Text;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// High-clarity 3D world-space and on-screen holographic telemetry matrix display for Level 1.
    /// Presents real-time double-loop pedagogical feedback across the 4 Earth recognition vectors:
    /// - Case 1: Deep Space Void (Land=0, Atmos=0) -> Filtered / Standby (0)
    /// - Case 2: Earth Atmosphere (Land=0, Atmos=1) -> Warp Vector Lock (1)
    /// - Case 3: Earth Continents (Land=1, Atmos=0) -> Warp Vector Lock (1)
    /// - Case 4: Earth Orbital Fix (Land=1, Atmos=1) -> Warp Vector Lock (1)
    /// Strictly observes ChamberController and NeuralState events. Never makes puzzle decisions.
    /// </summary>
    [ExecuteAlways]
    public class DiagnosticHologramVisual : MonoBehaviour
    {
        [Header("State Listeners")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;

        [Header("Visual Elements")]
        [SerializeField] private Transform displayRoot;
        [SerializeField] private Light hologramBacklight;
        [SerializeField] private Renderer displayScreenRenderer;
        [SerializeField] private TextMesh headerTextMesh;
        [SerializeField] private TextMesh[] rowTextMeshes = new TextMesh[4];
        [SerializeField] private TextMesh diagnosticFeedbackTextMesh;

        [Header("Color Palette")]
        [SerializeField] private Color passColor = new Color(0.10f, 0.90f, 0.50f, 1.0f); // Mint Emerald
        [SerializeField] private Color failColor = new Color(0.95f, 0.35f, 0.35f, 1.0f); // Coral Rose
        [SerializeField] private Color standbyColor = new Color(0.25f, 0.80f, 1.0f, 1.0f); // Ice Cyan
        [SerializeField] private Color amberWarningColor = new Color(0.98f, 0.65f, 0.10f, 1.0f); // Solar Amber

        [Header("Screen Overlay")]
        [SerializeField] private bool showScreenOverlay = false;

        private PuzzleEvaluation _latestEvaluation;
        private readonly string[] _caseRowTexts = new string[4];
        private string _statusHeader = "WARP VECTOR SCANNER — initiate scan pulse to verify";
        private string _pedagogicalHint = "Adjust sensor sensitivity dials until all 4 telemetry vectors lock onto Earth, then initiate jump pulse.";

        private readonly StringBuilder _rowBuffer = new StringBuilder(256);
        private readonly StringBuilder _hintBuffer = new StringBuilder(512);

        private static readonly string[] CaseNames = new string[]
        {
            "Deep Space Void ",
            "Earth Atmosphere",
            "Earth Continents",
            "Earth Orbital Fix"
        };

        public PuzzleEvaluation LatestEvaluation => _latestEvaluation;

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }

            for (int i = 0; i < 4; i++)
            {
                _caseRowTexts[i] = $"{CaseNames[i]}: STANDBY";
            }
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
            if (neuralState != null)
            {
                neuralState.OnStateMutated += HandleNeuralStateChanged;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
            if (neuralState != null)
            {
                neuralState.OnStateMutated -= HandleNeuralStateChanged;
            }
        }

        private void Start()
        {
            UpdateLiveMatrix();
        }

        private void HandleNeuralStateChanged()
        {
            UpdateLiveMatrix();
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            _latestEvaluation = eval;
            if (eval == null) return;

            if (hologramBacklight != null)
            {
                hologramBacklight.color = eval.Passed ? passColor : (eval.Accuracy > 0.5f ? amberWarningColor : failColor);
            }

            _statusHeader = eval.Passed
                ? "All 4 telemetry vectors locked — Warp AI stabilized!"
                : $"{eval.PassedCases} of 4 vectors locked — adjust sensor weights and retry pulse";

            _hintBuffer.Length = 0;
            if (!eval.ActivationMatches)
            {
                _hintBuffer.Append($"Incompatible matrix module: '{eval.ActiveActivation}'. Warp AI requires a Step module for decisive warp lock.");
            }
            else if (eval.Passed)
            {
                _hintBuffer.Append("Hyperspace coordinates locked onto Earth! Emergency jump corridor open.");
            }
            else
            {
                // Actionable double-loop pedagogical hint based on state
                BuildDetailedDiagnosticHint();
            }
            _pedagogicalHint = _hintBuffer.ToString();

            Update3DTextDisplays(eval.Passed);
        }

        private void HandleChamberReset()
        {
            _latestEvaluation = null;
            _statusHeader = "WARP VECTOR SCANNER — connect sensor conduits and tune sensitivity";
            _pedagogicalHint = "Adjust sensor sensitivity dials until all 4 telemetry vectors lock onto Earth, then initiate jump pulse.";

            if (hologramBacklight != null)
            {
                hologramBacklight.color = standbyColor;
            }

            UpdateLiveMatrix();
        }

        /// <summary>
        /// Real-time live matrix preview calculating energy levels as dials rotate.
        /// Shows energy bars and directional feedback without giving away explicit numerical cheat sheets.
        /// </summary>
        private void UpdateLiveMatrix()
        {
            if (neuralState == null) return;

            bool c1 = neuralState.Cable1Connected;
            bool c2 = neuralState.Cable2Connected;
            double w1 = c1 ? neuralState.Weight1 : 0.0;
            double w2 = c2 ? neuralState.Weight2 : 0.0;
            double b = neuralState.Bias;
            bool isStep = neuralState.Activation == ActivationType.Step;

            // 4 Hazard Cases: (x1: Rad, x2: Bio, Target: 0 or 1)
            double[][] cases = new double[][]
            {
                new double[] { 0.0, 0.0, 0.0 }, // Clean Room -> 0
                new double[] { 0.0, 1.0, 1.0 }, // Bio-Leak -> 1
                new double[] { 1.0, 0.0, 1.0 }, // Radiation -> 1
                new double[] { 1.0, 1.0, 1.0 }  // Dual Breach -> 1
            };

            int passedCases = 0;

            for (int i = 0; i < 4; i++)
            {
                double rawX1 = cases[i][0];
                double rawX2 = cases[i][1];
                double target = cases[i][2];

                double x1 = c1 ? rawX1 : 0.0;
                double x2 = c2 ? rawX2 : 0.0;

                double z = (x1 * w1) + (x2 * w2) + b;
                double y = isStep ? (z >= 0.0 ? 1.0 : 0.0) : z;
                bool pass = (Math.Abs(y - target) < 1e-4) && isStep;

                if (pass) passedCases++;

                _rowBuffer.Length = 0;
                string targetLabel = target > 0.5 ? "LOCK " : "VOID ";
                
                // Visual energy bar
                string energyBar = FormatEnergyBar(z);

                if (_latestEvaluation != null && _latestEvaluation.Diagnostics != null && i < _latestEvaluation.Diagnostics.Count)
                {
                    bool verifiedPass = _latestEvaluation.Diagnostics[i].IsCorrect && _latestEvaluation.ActivationMatches;
                    _rowBuffer.Append($"{CaseNames[i]} | Target: {targetLabel} | Energy: {energyBar} | {(verifiedPass ? "✓ LOCKED" : "✗ MISMATCH")}");
                }
                else
                {
                    string liveState = (z >= 0.0) ? "⚡ LOCK " : "○ VOID ";
                    _rowBuffer.Append($"{CaseNames[i]} | Target: {targetLabel} | Energy: {energyBar} | {liveState}");
                }

                _caseRowTexts[i] = _rowBuffer.ToString();
            }

            if (_latestEvaluation == null)
            {
                _statusHeader = "WARP NAVIGATION MATRIX (Initiate Jump Pulse [Space] to verify)";
                BuildDetailedDiagnosticHint();
            }

            Update3DTextDisplays(_latestEvaluation != null && _latestEvaluation.Passed);
        }

        private string FormatEnergyBar(double z)
        {
            // Visual energy bar centered at 0: [-4 .. 0 .. +4]
            int clamped = (int)Mathf.Clamp((float)z * 2.5f, -6f, 6f);
            if (clamped < 0)
            {
                int filled = -clamped;
                return $"[{new string('◄', filled).PadLeft(6, '·')}|······]";
            }
            else if (clamped > 0)
            {
                int filled = clamped;
                return $"[······|{new string('►', filled).PadRight(6, '·')}]";
            }
            else
            {
                return "[······|······]";
            }
        }

        private void BuildDetailedDiagnosticHint()
        {
            _hintBuffer.Length = 0;

            if (neuralState == null) return;

            if (!neuralState.Cable1Connected || !neuralState.Cable2Connected)
            {
                _hintBuffer.Append("Sensor conduits unlinked! Plug both sensor conduits into the command console.");
                return;
            }

            if (neuralState.Activation != ActivationType.Step)
            {
                _hintBuffer.Append($"Incompatible matrix module ('{neuralState.Activation}'). Warp AI requires a Step module for decisive warp lock.");
                return;
            }

            double w1 = neuralState.Weight1;
            double w2 = neuralState.Weight2;
            double b  = neuralState.Bias;

            // Case 1: (0,0) -> Target 0 (Requires b < 0)
            if (b >= 0.0)
            {
                _hintBuffer.Append("Deep space noise triggering false warp lock — lower the cosmic noise filter (Bias < 0).");
                return;
            }

            // Case 2: (0,1) -> Target 1 (Requires w2 + b >= 0)
            if (w2 + b < 0.0)
            {
                _hintBuffer.Append("Atmospheric O2/N2 signature too faint — boost Atmosphere Sensitivity (W2).");
                return;
            }

            // Case 3: (1,0) -> Target 1 (Requires w1 + b >= 0)
            if (w1 + b < 0.0)
            {
                _hintBuffer.Append("Continental landmass telemetry too weak — boost Landmass Sensitivity (W1).");
                return;
            }

            _hintBuffer.Append("Warp vectors aligned with Earth! Pull the jump lever to initiate recovery jump.");
        }

        private void Update3DTextDisplays(bool allPassed)
        {
            if (headerTextMesh != null)
            {
                headerTextMesh.text = _statusHeader;
                headerTextMesh.color = allPassed ? passColor : standbyColor;
            }

            for (int i = 0; i < rowTextMeshes.Length && i < _caseRowTexts.Length; i++)
            {
                if (rowTextMeshes[i] != null)
                {
                    rowTextMeshes[i].text = _caseRowTexts[i];
                    bool rowPass = _caseRowTexts[i].Contains("[PASS]");
                    rowTextMeshes[i].color = rowPass ? passColor : failColor;
                }
            }

            if (diagnosticFeedbackTextMesh != null)
            {
                diagnosticFeedbackTextMesh.text = string.IsNullOrEmpty(_pedagogicalHint) ? _hintBuffer.ToString() : _pedagogicalHint;
                diagnosticFeedbackTextMesh.color = allPassed ? passColor : amberWarningColor;
            }
        }
    }
}
