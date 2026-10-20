using System;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The topic tag on the dash (PIXELS, NEURON, WEIGHTS, NEURAL NETWORK, ...): it changes when the ride crosses into
    /// a region, and when the narrator names a new topic inside one (a cue, for example "wires" names WEIGHTS), and
    /// brightens for a moment, so the rider knows a new idea began without a break. No numbers, an elegant title.
    /// </summary>
    public sealed class RegionTagView : MonoBehaviour
    {
        [Serializable]
        public struct CueTag
        {
            public string cue;
            public string tag;
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private RideTheme theme;
        [SerializeField] private TMP_Text label;
        [SerializeField] private CueTag[] cueTags = new CueTag[0];
        [SerializeField] private float flashSeconds = 2.5f;
        [SerializeField] private float restAlpha = 0.7f;

        private float _flashUntil;

        public string Shown => label != null ? label.text : string.Empty;

        private void OnEnable()
        {
            if (director != null) director.RegionChanged += Show;
            if (dash != null) dash.SegmentShown += OnClause;
        }

        private void OnDisable()
        {
            if (director != null) director.RegionChanged -= Show;
            if (dash != null) dash.SegmentShown -= OnClause;
        }

        private void Start() => Show(director != null ? director.Region : 0);

        /// <summary>The tag text for a region, as the dash shows it.</summary>
        public static string TagFor(RideTheme theme, int region) => theme.Region(region).tag;

        private void Show(int region)
        {
            if (theme != null) SetTag(TagFor(theme, region));
        }

        private void OnClause(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            foreach (CueTag c in cueTags)
            {
                if (NarrationLibrary.HasCue(segment, c.cue)) SetTag(c.tag);
            }
        }

        private void SetTag(string text)
        {
            if (label == null || label.text == text) return;
            label.text = text;
            _flashUntil = Time.time + flashSeconds;
        }

        private void Update()
        {
            if (label == null) return;
            float flash = Mathf.Clamp01((_flashUntil - Time.time) / flashSeconds);
            Color c = label.color;
            c.a = Mathf.Lerp(restAlpha, 1f, flash);
            label.color = c;
        }
    }
}
