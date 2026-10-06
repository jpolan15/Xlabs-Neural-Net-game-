using System.Text;
using TMPro;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// World-space replacement for the OnGUI HUD (ADR-007). Shows the current objective, live dial readout and
    /// per-case results on a canvas that XRI ray interactors can point at. Observes gameplay state only.
    /// </summary>
    public sealed class WorldSpaceHud : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private ChamberOnboardingController onboardingController;

        [Header("Text Targets")]
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text readoutText;
        [SerializeField] private TMP_Text resultText;

        [Header("Behaviour")]
        [SerializeField] private float refreshInterval = 0.2f;
        [SerializeField] private bool yawTowardHead = true;
        [SerializeField] private string[] caseLabels = { "Drone", "Comet", "Rock", "Both" };

        private readonly StringBuilder _builder = new StringBuilder(160);
        private float _nextRefresh;
        private Camera _camera;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (onboardingController == null) onboardingController = FindAnyObjectByType<ChamberOnboardingController>();
        }

        private void LateUpdate()
        {
            if (yawTowardHead)
            {
                if (_camera == null) _camera = Camera.main;
                if (_camera != null)
                {
                    Vector3 toHead = _camera.transform.position - transform.position;
                    toHead.y = 0f;
                    if (toHead.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
                    }
                }
            }

            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            if (objectiveText != null && onboardingController != null)
            {
                objectiveText.text = onboardingController.GetCurrentStepPrompt();
            }

            if (readoutText != null && neuralState != null)
            {
                _builder.Clear();
                _builder.Append("FIRE = step(");
                _builder.Append(neuralState.Weight1.ToString("0.0"));
                _builder.Append("·ROCK + ");
                _builder.Append(neuralState.Weight2.ToString("0.0"));
                _builder.Append("·ICE + ");
                _builder.Append(neuralState.Bias.ToString("0.0"));
                _builder.Append(")  ");
                _builder.Append(neuralState.Activation.ToString().ToUpperInvariant());
                _builder.Append("\nFires when the sum reaches 0.");
                readoutText.text = _builder.ToString();
            }

            if (resultText != null)
            {
                resultText.text = BuildResultLine();
            }
        }

        private string BuildResultLine()
        {
            PuzzleEvaluation eval = chamberController != null ? chamberController.LastEvaluation : null;
            if (eval == null || eval.Diagnostics == null)
            {
                return "Drone slips past. Ice, rock, and both should burn.";
            }

            _builder.Clear();
            int shown = caseLabels.Length < eval.Diagnostics.Count ? caseLabels.Length : eval.Diagnostics.Count;
            for (int i = 0; i < shown; i++)
            {
                CaseDiagnostic diag = eval.Diagnostics[i];
                bool pass = diag.IsCorrect && eval.ActivationMatches;
                _builder.Append(caseLabels[i]).Append(' ');
                if (!pass) _builder.Append("missed");
                else if (diag.ExpectedOutput >= 0.5) _builder.Append("burned");
                else _builder.Append("safe");
                if (i < shown - 1) _builder.Append("   ");
            }

            _builder.Append(eval.Passed ? "\nThe rule holds." : "\nChange one dial.");
            return _builder.ToString();
        }
    }
}
