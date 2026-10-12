using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Each region has its own sky and fog (RideTheme.regions): warm paper at the dock, a black void for the pixels,
    /// navy for the network, red for the wrong guess, magenta for the rewind, a sunset for the Valley, gold at the end.
    /// When the ride crosses into a region this blends the camera background, fog colour and fog density over a few
    /// seconds, so a rider feels the border without a cut.
    /// </summary>
    public sealed class RegionAtmosphere : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private RideTheme theme;
        [SerializeField] private float blendSeconds = 3f;

        private Camera _camera;
        private Color _fromBackground, _fromFog;
        private float _fromDensity;
        private int _target;
        private float _t = 1f;

        private void OnEnable()
        {
            if (director != null) director.RegionChanged += OnRegion;
        }

        private void OnDisable()
        {
            if (director != null) director.RegionChanged -= OnRegion;
        }

        private void Start()
        {
            _target = director != null ? director.Region : 0;
            Apply(theme.Region(_target).background, theme.Region(_target).fog, theme.Region(_target).fogDensity);
        }

        private void OnRegion(int region)
        {
            _fromBackground = CurrentBackground();
            _fromFog = RenderSettings.fogColor;
            _fromDensity = RenderSettings.fogDensity;
            _target = region;
            _t = 0f;
        }

        private void Update()
        {
            if (_t >= 1f || theme == null) return;
            _t = Mathf.Min(1f, _t + Time.deltaTime / Mathf.Max(0.05f, blendSeconds));
            float e = _t * _t * (3f - 2f * _t);
            RideTheme.RegionLook look = theme.Region(_target);
            Apply(Color.Lerp(_fromBackground, look.background, e), Color.Lerp(_fromFog, look.fog, e), Mathf.Lerp(_fromDensity, look.fogDensity, e));
        }

        private Color CurrentBackground()
        {
            if (_camera == null) _camera = Camera.main;
            return _camera != null ? _camera.backgroundColor : RenderSettings.fogColor;
        }

        private void Apply(Color background, Color fog, float density)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) _camera.backgroundColor = background;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = density;
        }
    }
}
