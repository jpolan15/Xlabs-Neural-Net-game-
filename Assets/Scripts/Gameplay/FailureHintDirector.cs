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
            if (!eval.ActivationMatches)
            {
                return "The crystal should be Step. The dials can be right and the laser still wrong.";
            }

            int missed = 0;
            if (eval.Diagnostics != null)
            {
                for (int i = 0; i < eval.Diagnostics.Count; i++)
                {
                    if (!eval.Diagnostics[i].IsCorrect)
                    {
                        missed = i;
                        failing = eval.Diagnostics[i];
                        break;
                    }
                }
            }

            switch (hintNumber)
            {
                case 1:
                    if (failing != null && failing.ActualOutput >= 0.5 && failing.ExpectedOutput < 0.5)
                        return "The drone burned. It has no rock and no ice. Lower the third dial.";
                    if (missed == 1) return "The icy comet got through. Turn the ICE dial up until the sum reaches zero.";
                    if (missed == 2) return "The rocky asteroid got through. Turn the ROCK dial up until the sum reaches zero.";
                    if (missed == 3) return "The rock-and-ice chunk got through. Turn either dial up until the sum reaches zero.";
                    return "A flier got through. Rock alone should burn. Ice alone should burn.";
                case 2:
                    return "Rock alone should be enough. Ice alone should be enough. The drone, with neither, should dock.";
                default:
                    return "The step crystal cuts at zero. Below zero the laser stays dark. At zero it fires. Change one dial.";
            }
        }
    }
}
