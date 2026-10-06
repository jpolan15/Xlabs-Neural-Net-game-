using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// SELF TEST runs the existing forward pass. After a pass it becomes the engage replay.
    /// </summary>
    public sealed class ChamberTestButton : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private LiveEvaluationRelay live;

        void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (live == null) live = FindAnyObjectByType<LiveEvaluationRelay>();
        }

        public void Press()
        {
            if (chamberController == null) return;
            if (chamberController.LastEvaluation != null && chamberController.LastEvaluation.Passed && live != null)
            {
                live.BeginEngage();
                return;
            }

            chamberController.TriggerForwardPass();
        }
    }
}
