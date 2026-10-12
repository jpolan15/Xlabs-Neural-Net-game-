using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Keeps the Quest 2's draw calls down: only the regions the rider can see from the running segment are active
    /// (the one they are in and the one ahead). Each segment has a bit mask over the Region_N roots, written by the
    /// builder; the dock has its own. In the saved scene everything is active, so edit-mode captures see all of it.
    /// </summary>
    public sealed class RegionStreamer : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private GameObject[] roots = new GameObject[0];
        [SerializeField] private int dockMask = 0b11;
        [SerializeField] private int[] segmentMasks = new int[0];

        private void OnEnable()
        {
            if (director != null) director.SegmentStarted += OnSegment;
        }

        private void OnDisable()
        {
            if (director != null) director.SegmentStarted -= OnSegment;
        }

        private void Start() => Apply(dockMask);

        private void OnSegment(int index)
        {
            Apply(index >= 0 && index < segmentMasks.Length ? segmentMasks[index] : ~0);
        }

        /// <summary>The mask the ride uses for a segment (-1: the dock).</summary>
        public int MaskFor(int segment) => segment < 0 ? dockMask : segment < segmentMasks.Length ? segmentMasks[segment] : ~0;

        private void Apply(int mask)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null) roots[i].SetActive((mask & (1 << i)) != 0);
            }
        }
    }
}
