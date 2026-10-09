using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The explainer panel under the chapter card: while the Guide explains something, it shows it. Each subtitle
    /// clause can carry cues (RideNarrationData, for example "sum", "trigger", "fire" or "downhill"); when a clause
    /// starts, the panel pops in with the diagram for its cue, so the rider watches the idea instead of waiting
    /// through it. A diagram stays up through the next clauses of the same line until another one is named; a new
    /// line with nothing to show closes the panel. At the dock it shows the weight the rider can play with until
    /// GO is pulled. It is hidden at the stops (the station is the picture there). Observes only.
    /// </summary>
    public sealed class ExplainerView : MonoBehaviour
    {
        [Serializable]
        public struct Visual
        {
            public string cue;
            public GameObject root;
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private GameObject panel;
        [SerializeField] private Visual[] visuals = new Visual[0];

        [Tooltip("The visual shown at the dock after the intro, while the rider can move a weight before launching.")]
        [SerializeField] private string dockCue = "try_weight";

        [SerializeField] private float popSeconds = 0.35f;

        private float _scale;
        private float _target;
        private GameObject _shown;

        /// <summary>The cue on screen now, or null. For tests and captures.</summary>
        public string ShownCue { get; private set; }

        private void OnEnable()
        {
            _scale = 0f;
            _target = 0f;
            _shown = null;
            ShownCue = null;
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].root != null) visuals[i].root.SetActive(false);
            }

            if (panel != null)
            {
                panel.transform.localScale = Vector3.zero;
                panel.SetActive(false);
            }

            if (dash != null) dash.SegmentShown += OnSegment;
            if (director == null) return;
            director.StateChanged += OnState;
            director.BriefingEnded += OnBriefingEnded;
            director.NarrationSkipped += Hide;
            director.RideCompleted += Hide;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnSegment;
            if (director == null) return;
            director.StateChanged -= OnState;
            director.BriefingEnded -= OnBriefingEnded;
            director.NarrationSkipped -= Hide;
            director.RideCompleted -= Hide;
        }

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (director != null && director.State == RideState.AtStop) return;
            int found = Find(segment);
            if (found >= 0) Show(found);
            else if (index == 0) Hide();
        }

        private void OnState(RideState state)
        {
            if (state == RideState.Dock)
            {
                int toy = Find(dockCue);
                if (toy >= 0) Show(toy);
            }
            else if (state != RideState.Intro)
            {
                Hide(); // launching, at a stop, or the end: the panel steps aside
            }
        }

        private void OnBriefingEnded(int stopIndex) => Hide();

        private int Find(NarrationLibrary.Segment segment)
        {
            if (segment.cues == null) return -1;
            for (int c = 0; c < segment.cues.Length; c++)
            {
                int found = Find(segment.cues[c]);
                if (found >= 0) return found;
            }

            return -1;
        }

        private int Find(string cue)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].cue == cue && visuals[i].root != null) return i;
            }

            return -1;
        }

        private void Show(int index)
        {
            GameObject root = visuals[index].root;
            if (_shown != root)
            {
                if (_shown != null) _shown.SetActive(false);
                root.SetActive(true); // restarts its motions
                _shown = root;
            }

            ShownCue = visuals[index].cue;
            if (panel != null && !panel.activeSelf) panel.SetActive(true);
            _target = 1f;
        }

        private void Hide()
        {
            _target = 0f;
            ShownCue = null;
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;
            _scale = Mathf.MoveTowards(_scale, _target, Time.deltaTime / Mathf.Max(0.01f, popSeconds));
            float eased = _scale * _scale * (3f - 2f * _scale);
            panel.transform.localScale = Vector3.one * eased;
            if (_scale > 0f || _target > 0f) return;

            panel.SetActive(false);
            if (_shown != null) _shown.SetActive(false);
            _shown = null;
        }
    }
}
