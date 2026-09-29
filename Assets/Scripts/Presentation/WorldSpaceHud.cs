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
        [SerializeField] private string[] caseLabels = { "Quiet", "Radio", "Light", "Both" };

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
                _builder.Append("W1 ").Append(neuralState.Weight1.ToString("+0.0;-0.0;0.0"));
                _builder.Append("   W2 ").Append(neuralState.Weight2.ToString("+0.0;-0.0;0.0"));
                _builder.Append("   BIAS ").Append(neuralState.Bias.ToString("+0.0;-0.0;0.0"));
                _builder.Append("   CRYSTAL ").Append(neuralState.Activation.ToString().ToUpperInvariant());
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
            if (eval == null)
            {
                return "NO TEST RUN YET";
            }

            _builder.Clear();
            for (int i = 0; i < caseLabels.Length; i++)
            {
                bool known = eval.Diagnostics != null && i < eval.Diagnostics.Count;
                bool pass = known && eval.Diagnostics[i].IsCorrect && eval.ActivationMatches;
                _builder.Append(caseLabels[i]).Append(pass ? " OK" : " X");
                if (i < caseLabels.Length - 1) _builder.Append("   |   ");
            }

            _builder.Append(eval.Passed ? "\nALL CASES PASS" : "\n" + eval.PassedCases + "/" + caseLabels.Length + " PASS");
            return _builder.ToString();
        }
    }
}
