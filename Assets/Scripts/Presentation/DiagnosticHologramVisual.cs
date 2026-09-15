using System;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Holographic floating truth-table display in Level 1.
    /// Strictly observes ChamberController evaluation events and presents per-case
    /// diagnostic telemetry to the player. Never initiates or decides evaluation.
    /// </summary>
    public class DiagnosticHologramVisual : MonoBehaviour
    {
        [Header("State Listener")]
        [SerializeField] private ChamberController chamberController;

        [Header("Visual Elements")]
        [SerializeField] private Transform displayRoot;
        [SerializeField] private Light hologramBacklight;
        [SerializeField] private Color passColor = new Color(0.2f, 1.0f, 0.4f, 1.0f);
        [SerializeField] private Color failColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        [SerializeField] private Color neutralColor = new Color(0.3f, 0.7f, 1.0f, 1.0f);
        [SerializeField] private bool showScreenOverlay = false;

        private PuzzleEvaluation _latestEvaluation;
        private string[] _caseTexts = new string[4];
        private string _statusHeader = "OR GATE AWAITING SIGNAL PULSE";

        public PuzzleEvaluation LatestEvaluation => _latestEvaluation;

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }

            for (int i = 0; i < 4; i++)
            {
                _caseTexts[i] = $"CASE {i + 1}: --";
            }
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            _latestEvaluation = eval;
            if (eval == null) return;

            if (hologramBacklight != null)
            {
                hologramBacklight.color = eval.Passed ? passColor : failColor;
            }

            _statusHeader = eval.Passed
                ? "HARMONIC CONVERGENCE: 100% ACCURACY"
                : $"CALIBRATION INCOMPLETE: {eval.PassedCases}/{eval.TotalCases} ({eval.Accuracy * 100:F0}%)";

            for (int i = 0; i < eval.Diagnostics.Count && i < _caseTexts.Length; i++)
            {
                var d = eval.Diagnostics[i];
                string resultStr = d.IsCorrect ? "[PASS]" : "[FAIL]";
                _caseTexts[i] = $"{d.Label,-14} | z: {d.CalculatedZ:+0.00;-0.00;0.00} -> {d.ActualOutput:F0} {resultStr}";
            }
        }

        private void HandleChamberReset()
        {
            _latestEvaluation = null;
            _statusHeader = "OR GATE CALIBRATION STANDBY";
            if (hologramBacklight != null)
            {
                hologramBacklight.color = neutralColor;
            }
            for (int i = 0; i < _caseTexts.Length; i++)
            {
                _caseTexts[i] = $"CASE {i + 1}: STANDBY";
            }
        }

        private void OnGUI()
        {
            if (!showScreenOverlay || chamberController == null) return;

            int screenWidth = Screen.width;
            Rect boxRect = new Rect(screenWidth - 420, 10, 410, 240);
            GUI.Box(boxRect, "AWAKENING GATE — PERCEPTRON DIAGNOSTIC MATRIX");

            GUI.Label(new Rect(boxRect.x + 15, boxRect.y + 30, 380, 25), _statusHeader);

            for (int i = 0; i < _caseTexts.Length; i++)
            {
                Rect rowRect = new Rect(boxRect.x + 15, boxRect.y + 60 + (i * 28), 380, 25);
                GUI.Label(rowRect, _caseTexts[i]);
            }

            if (_latestEvaluation != null)
            {
                GUI.Label(new Rect(boxRect.x + 15, boxRect.y + 180, 380, 50), _latestEvaluation.Summary);
            }
            else
            {
                GUI.Label(new Rect(boxRect.x + 15, boxRect.y + 180, 380, 50), "Adjust weights & bias, insert Step crystal, fire pulse to test.");
            }
        }
    }
}
