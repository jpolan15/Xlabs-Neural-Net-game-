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
        [SerializeField] private bool showMath;

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

        /// <summary>Formula stays behind this toggle. Default is off.</summary>
        public void ToggleMath()
        {
            SetShowMath(!showMath);
        }

        public void SetShowMath(bool value)
        {
            showMath = value;
            Refresh();
        }

        private void Refresh()
        {
            if (objectiveText != null)
            {
                var board = ChamberPromptBoard.Instance;
                if (board != null && !string.IsNullOrEmpty(board.Current)) objectiveText.text = board.Current;
                else if (onboardingController != null) objectiveText.text = onboardingController.GetCurrentStepPrompt();
            }

            if (readoutText != null)
            {
                readoutText.gameObject.SetActive(showMath);
                if (showMath && neuralState != null)
                {
                    _builder.Clear();
                    string in1 = "In1";
                    string in2 = "In2";
                    string output = "Out";
                    if (chamberController != null && chamberController.Curriculum != null)
                    {
                        in1 = chamberController.Curriculum.InputNames[0];
                        in2 = chamberController.Curriculum.InputNames[1];
                        output = chamberController.Curriculum.OutputName;
                    }

                    _builder.Append(output);
                    _builder.Append(" = step(");
                    _builder.Append(neuralState.Weight1.ToString("0.0"));
                    _builder.Append("·");
                    _builder.Append(in1);
                    _builder.Append(" + ");
                    _builder.Append(neuralState.Weight2.ToString("0.0"));
                    _builder.Append("·");
                    _builder.Append(in2);
                    _builder.Append(" + ");
                    _builder.Append(neuralState.Bias.ToString("0.0"));
                    _builder.Append(')');
                    readoutText.text = _builder.ToString();
                }
            }

            if (resultText != null)
            {
                resultText.text = BuildResultLine();
            }
        }

        private string BuildResultLine()
        {
            PuzzleEvaluation eval = chamberController != null ? chamberController.LastEvaluation : null;
            if (eval == null || eval.Diagnostics == null) return "Not yet.";
            if (eval.Passed) return "Handled.";
            for (int i = 0; i < eval.Diagnostics.Count; i++)
            {
                if (eval.Diagnostics[i].IsCorrect && eval.ActivationMatches) continue;
                return FirstWords(eval.Diagnostics[i].Explanation, 12);
            }

            return "Not yet.";
        }

        static string FirstWords(string line, int max)
        {
            if (string.IsNullOrWhiteSpace(line)) return "Not yet.";
            string[] parts = line.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= max) return string.Join(" ", parts);
            return string.Join(" ", parts, 0, max);
        }
    }
}
