using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Region 6: the Valley. The ground is the real loss along the real training path (built by the builder). This
    /// shows the step and the loss of every hop on the dash as the pod reaches it, and squashes the Valley in gentle
    /// mode so the ground stays under the pod (the director lowers the track by the same factor).
    /// </summary>
    public sealed class ValleyView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private ValleyRunMoment moment;
        [Tooltip("Scaled in y by the director's vertical scale (gentle mode).")]
        [SerializeField] private Transform ground;

        private void OnEnable()
        {
            if (director != null) director.GentleModeChanged += OnGentle;
            if (moment != null) moment.HopReached += OnHop;
            OnGentle(director != null && director.GentleMode);
        }

        private void OnDisable()
        {
            if (director != null) director.GentleModeChanged -= OnGentle;
            if (moment != null) moment.HopReached -= OnHop;
        }

        private void OnGentle(bool gentle)
        {
            if (ground == null || director == null) return;
            Vector3 s = ground.localScale;
            s.y = director.VerticalScale;
            ground.localScale = s;
        }

        private void OnHop(int k)
        {
            if (dash == null || moment == null) return;
            Level1Hop hop = moment.Hop(k);
            string where = k == 0 ? "HEIGHT = HOW WRONG.   START" : "STEP " + hop.step;
            string trend = k == 0 ? "" : hop.loss > moment.Hop(k - 1).loss ? "   UP: TOO BIG A STEP" : "   DOWN";
            dash.ShowReadout(where + "   LOSS " + hop.loss.ToString("0.00") + trend);
        }
    }
}
