using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The small region tag on the dash ("2 · ONE NEURON"), instead of the old chapter cards: it changes when the ride
    /// crosses into a region and brightens for a moment, so the rider knows a new part began without a break.
    /// </summary>
    public sealed class RegionTagView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private RideTheme theme;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float flashSeconds = 2.5f;
        [SerializeField] private float restAlpha = 0.7f;

        private float _flashUntil;

        private void OnEnable()
        {
            if (director != null) director.RegionChanged += Show;
        }

        private void OnDisable()
        {
            if (director != null) director.RegionChanged -= Show;
        }

        private void Start() => Show(director != null ? director.Region : 0);

        /// <summary>The tag text for a region, as the dash shows it.</summary>
        public static string TagFor(RideTheme theme, int region) => region + " · " + theme.Region(region).tag;

        private void Show(int region)
        {
            if (label == null || theme == null) return;
            label.text = TagFor(theme, region);
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
