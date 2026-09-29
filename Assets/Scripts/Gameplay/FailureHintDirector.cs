using UnityEngine;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Three cause hints after repeated failed tests. The text names the failing case.
    /// It does not state a weight or a bias value.
    /// </summary>
    public class FailureHintDirector : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;
        private int _failures;
        private int _hintsGiven;

        /// <summary>Spoken when a new hint is earned.</summary>
        public event System.Action<string> OnHint;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        private void OnEnable()
        {
            if (chamberController != null) chamberController.OnEvaluationComplete += HandleEvaluation;
        }

        private void OnDisable()
        {
            if (chamberController != null) chamberController.OnEvaluationComplete -= HandleEvaluation;
        }

        private void HandleEvaluation(PuzzleEvaluation eval)
        {
            if (eval == null || eval.Passed)
            {
                return;
            }

            _failures++;
            if (_hintsGiven >= 3) return;
            if (_failures != 2 && _failures != 4 && _failures != 6) return;

            _hintsGiven++;
            OnHint?.Invoke(BuildHint(eval, _hintsGiven));
        }

        private static string BuildHint(PuzzleEvaluation eval, int hintNumber)
        {
            CaseDiagnostic failing = null;
            if (eval.Diagnostics != null)
            {
                for (int i = 0; i < eval.Diagnostics.Count; i++)
                {
                    if (!eval.Diagnostics[i].IsCorrect)
                    {
                        failing = eval.Diagnostics[i];
                        break;
                    }
                }
            }

            string label = failing != null && !string.IsNullOrEmpty(failing.Label) ? failing.Label : "one sensor case";
            if (!eval.ActivationMatches)
            {
                return "The decision crystal does not match the one this array needs. The numbers can be right and the switch still wrong.";
            }

            switch (hintNumber)
            {
                case 1:
                    return "Case " + label + " disagreed. Look at whether the sum was too weak to fire, or strong enough to fire when the sensors were quiet.";
                case 2:
                    return "Either beacon alone should be enough. Quiet sensors, with neither beacon, should stay dark.";
                default:
                    return "The step crystal cuts at zero. A negative sum stays off. A sum that reaches zero fires. Change one dial, then run the test again.";
            }
        }
    }
}
