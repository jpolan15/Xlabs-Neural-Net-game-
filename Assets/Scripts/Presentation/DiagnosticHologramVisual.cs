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
    /// Presents real-time double-loop pedagogical feedback across the 4 facility hazard scenarios:
    /// - Case 1: Clean Room (Radiation=0, BioLeak=0) -> Safe (0)
    /// - Case 2: Bio-Hazard Leak (Radiation=0, BioLeak=1) -> Quarantine Alarm (1)
    /// - Case 3: Radiation Spike (Radiation=1, BioLeak=0) -> Quarantine Alarm (1)
    /// - Case 4: Dual Hazard Breach (Radiation=1, BioLeak=1) -> Quarantine Alarm (1)
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
        private string _statusHeader = "SENSOR CHECK — run the test to see results";
        private string _pedagogicalHint = "Turn the sensitivity dial until all four rows show green, then pull the lever.";

        private readonly StringBuilder _rowBuffer = new StringBuilder(256);
        private readonly StringBuilder _hintBuffer = new StringBuilder(512);

        private static readonly string[] CaseNames = new string[]
        {
            "Safe Room    ",
            "Bio Leak     ",
            "Rad Flare    ",
            "Dual Breach  "
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
                ? "All 4 scenarios correct — sentry is calibrated!"
                : $"{eval.PassedCases} of 4 correct — adjust the dial and try again";

            _hintBuffer.Length = 0;
            if (!eval.ActivationMatches)
            {
                _hintBuffer.Append($"Wrong crystal type: '{eval.ActiveActivation}'. The sentry needs a Step crystal for binary (fire/hold) decisions.");
            }
            else if (eval.Passed)
            {
                _hintBuffer.Append("Perfect! All four scenarios verified. Blast doors are unsealing.");
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
            _statusHeader = "SENSOR CHECK — connect the cables and tune the dial";
            _pedagogicalHint = "Turn the sensitivity dial until all four rows go green, then pull the lever.";

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
                string targetLabel = target > 0.5 ? "ALARM" : "SAFE ";
                
                // Visual energy bar
                string energyBar = FormatEnergyBar(z);

                if (_latestEvaluation != null && _latestEvaluation.Diagnostics != null && i < _latestEvaluation.Diagnostics.Count)
                {
                    bool verifiedPass = _latestEvaluation.Diagnostics[i].IsCorrect && _latestEvaluation.ActivationMatches;
                    _rowBuffer.Append($"{CaseNames[i]} | Expected: {targetLabel} | Energy: {energyBar} | {(verifiedPass ? "✓ VERIFIED" : "✗ MISMATCH")}");
                }
                else
                {
                    string liveState = (z >= 0.0) ? "⚡ ALERT " : "○ QUIET ";
                    _rowBuffer.Append($"{CaseNames[i]} | Expected: {targetLabel} | Energy: {energyBar} | {liveState}");
                }

                _caseRowTexts[i] = _rowBuffer.ToString();
            }

            if (_latestEvaluation == null)
            {
                _statusHeader = "HAZARD MATRIX TELEMETRY (Pull Clock Lever [Space] to verify)";
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
                _hintBuffer.Append("Cable not plugged in! Grab the glowing cable and snap it into the console.");
                return;
            }

            if (neuralState.Activation != ActivationType.Step)
            {
                _hintBuffer.Append($"Wrong crystal type ('{neuralState.Activation}'). The sentry needs a Step crystal to make a clear fire/hold decision.");
                return;
            }

            double w1 = neuralState.Weight1;
            double w2 = neuralState.Weight2;
            double b  = neuralState.Bias;

            // Case 1: (0,0) -> Target 0 (Requires b < 0)
            if (b >= 0.0)
            {
                _hintBuffer.Append("The sentry is triggering in a quiet room — try turning the sensitivity down a little.");
                return;
            }

            // Case 2: (0,1) -> Target 1 (Requires w2 + b >= 0)
            if (w2 + b < 0.0)
            {
                _hintBuffer.Append("Bio-leak alarm not reaching the sentry — try turning the sensitivity up.");
                return;
            }

            // Case 3: (1,0) -> Target 1 (Requires w1 + b >= 0)
            if (w1 + b < 0.0)
            {
                _hintBuffer.Append("Radiation alert not reaching the sentry — try turning the sensitivity up.");
                return;
            }

            _hintBuffer.Append("Looks balanced — pull the lever to run the final test!");
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
