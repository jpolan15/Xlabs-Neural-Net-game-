using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>Shows one object while a segment's moment runs (the hot-wire hunt's "SHOOT THE HOT WIRES!" card on the table). Display only.</summary>
    public sealed class CueShowView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private GameObject target;
        [SerializeField] private int segment = -1;

        private void Update()
        {
            if (director == null || target == null) return;
            bool show = director.SegmentIndex == segment && director.ActiveMoment != null;
            if (target.activeSelf != show) target.SetActive(show);
        }
    }
}
