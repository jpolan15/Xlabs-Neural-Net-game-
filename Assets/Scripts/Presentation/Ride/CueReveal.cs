using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Shows (or hides) scene objects on a narration cue, so the eye gets the picture while the words are said: the
    /// accuracy sign on "accuracy", the words on "words". Objects revealed by a cue start hidden at run time. The cue
    /// names live in the script (Tools/Editor/RideNarrationData.cs); the builder fails on a cue nobody reacts to.
    /// </summary>
    public sealed class CueReveal : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string cue;
            public GameObject target;
            public bool show;
        }

        [SerializeField] private DashScreenView dash;
        [SerializeField] private Entry[] entries = new Entry[0];

        private void OnEnable()
        {
            if (dash != null) dash.SegmentShown += OnSegment;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnSegment;
        }

        private void Start()
        {
            foreach (Entry e in entries)
            {
                if (e.target != null && e.show) e.target.SetActive(false);
            }
        }

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            foreach (Entry e in entries)
            {
                if (e.target != null && NarrationLibrary.HasCue(segment, e.cue)) e.target.SetActive(e.show);
            }
        }
    }
}
