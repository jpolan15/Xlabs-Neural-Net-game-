using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Each region has its own sky and fog (RideTheme.regions): a dark warm room at the dock, a black void for the
    /// pixels, navy for the network, red for the wrong guess, magenta for the rewind, a sunset for the Valley, gold at
    /// the end. When the ride crosses into a region this blends the camera background, fog colour and fog density over
    /// a few seconds, so a rider feels the border without a cut. While an explainer explains it holds the sky on the
    /// blackboard (RideTheme.blackboard: near-black navy, no colour cast) and fades in the faint distant grid; when the
    /// last holder lets go, the region's own sky comes back.
    /// </summary>
    public sealed class RegionAtmosphere : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private RideTheme theme;
        [SerializeField] private float blendSeconds = 3f;
        [SerializeField] private float blackboardBlendSeconds = 1.2f;

        [Tooltip("The blackboard's faint distant grid (its material's _BaseColor alpha follows the blackboard).")]
        [SerializeField] private Renderer grid;

        private readonly HashSet<object> _holders = new HashSet<object>();
        private Camera _camera;
        private Color _fromBackground, _fromFog;
        private float _fromDensity;
        private int _region;
        private float _t = 1f;
        private float _seconds = 3f;
        private float _gridAlpha, _gridFrom, _gridTo;
        private Color _gridColor = Color.white;
        private MaterialPropertyBlock _block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>True while an explainer holds the blackboard.</summary>
        public bool IsBlackboard => _holders.Count > 0;

        private void OnEnable()
        {
            if (director != null) director.RegionChanged += OnRegion;
        }

        private void OnDisable()
        {
            if (director != null) director.RegionChanged -= OnRegion;
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (grid != null && grid.sharedMaterial != null) _gridColor = grid.sharedMaterial.GetColor(BaseColor);
        }

        private void Start()
        {
            _region = director != null ? director.Region : 0;
            RideTheme.RegionLook look = Target();
            Apply(look.background, look.fog, look.fogDensity);
            _gridAlpha = _gridTo = IsBlackboard ? 1f : 0f;
            ApplyGrid(_gridAlpha);
        }

        /// <summary>An explainer starts explaining: the sky goes to the blackboard (at once at load).</summary>
        public void HoldBlackboard(object owner, bool instant = false)
        {
            if (!_holders.Add(owner) || _holders.Count > 1) return;
            BlendTo(blackboardBlendSeconds);
            if (instant) Snap();
        }

        /// <summary>An explainer unfolded: when nobody holds the blackboard, the region's sky comes back.</summary>
        public void ReleaseBlackboard(object owner)
        {
            if (!_holders.Remove(owner) || _holders.Count > 0) return;
            BlendTo(blackboardBlendSeconds * 1.5f);
        }

        private void OnRegion(int region)
        {
            _region = region;
            BlendTo(blendSeconds);
        }

        private RideTheme.RegionLook Target() => IsBlackboard ? theme.blackboard : theme.Region(_region);

        private void BlendTo(float seconds)
        {
            if (theme == null) return;
            _fromBackground = CurrentBackground();
            _fromFog = RenderSettings.fogColor;
            _fromDensity = RenderSettings.fogDensity;
            _gridFrom = _gridAlpha;
            _gridTo = IsBlackboard ? 1f : 0f;
            _seconds = seconds;
            _t = 0f;
        }

        private void Snap()
        {
            _t = 1f;
            RideTheme.RegionLook look = Target();
            Apply(look.background, look.fog, look.fogDensity);
            _gridAlpha = _gridTo;
            ApplyGrid(_gridAlpha);
        }

        private void Update()
        {
            if (_t >= 1f || theme == null) return;
            _t = Mathf.Min(1f, _t + Time.deltaTime / Mathf.Max(0.05f, _seconds));
            float e = Explainers.Rate.Smooth(_t);
            RideTheme.RegionLook look = Target();
            Apply(Color.Lerp(_fromBackground, look.background, e), Color.Lerp(_fromFog, look.fog, e), Mathf.Lerp(_fromDensity, look.fogDensity, e));
            _gridAlpha = Mathf.Lerp(_gridFrom, _gridTo, e);
            ApplyGrid(_gridAlpha);
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

        private void ApplyGrid(float alpha)
        {
            if (grid == null) return;
            grid.enabled = alpha > 0.004f;
            if (_block == null) _block = new MaterialPropertyBlock();
            Color c = _gridColor;
            c.a *= alpha;
            grid.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, c);
            grid.SetPropertyBlock(_block);
        }
    }
}
