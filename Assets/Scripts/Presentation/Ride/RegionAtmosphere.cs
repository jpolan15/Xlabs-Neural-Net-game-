using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Each region has its own sky and fog (RideTheme.regions): since v7 a lit sky (Convergence/RideSky: a gradient,
    /// a glow on the horizon, stars, a grid floor far below) in the region's colours: dawn blue at the dock, cyan for the
    /// pixels, amber for the neuron, deep blue for the network, red for the wrong guess, violet for backpropagation, a
    /// sunset for the Valley, gold at the end. When the ride crosses into a region this blends the sky, fog colour and
    /// fog density over a few seconds, so a rider feels the border without a cut. While an explainer explains it holds
    /// the "blackboard": the region's own sky dimmed (RideTheme.blackboardDim), its floor at half strength; when the last
    /// holder lets go, the region's full sky comes back. The sky's floor replaced the old pod-relative blackboard grid,
    /// which now stays off (two grids at one depth would shimmer against each other).
    /// </summary>
    public sealed class RegionAtmosphere : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private RideTheme theme;
        [SerializeField] private float blendSeconds = 3f;
        [SerializeField] private float blackboardBlendSeconds = 1.2f;

        [Tooltip("The blackboard's faint distant grid (its material's _BaseColor alpha follows the blackboard).")]
        [SerializeField] private Renderer grid;

        /// <summary>Everything that blends between looks.</summary>
        private struct State
        {
            public Color Background, Fog, Top, Horizon, Glow, Floor;
            public float Density, Stars;

            public static State Lerp(State a, State b, float t) => new State
            {
                Background = Color.Lerp(a.Background, b.Background, t),
                Fog = Color.Lerp(a.Fog, b.Fog, t),
                Top = Color.Lerp(a.Top, b.Top, t),
                Horizon = Color.Lerp(a.Horizon, b.Horizon, t),
                Glow = Color.Lerp(a.Glow, b.Glow, t),
                Floor = Color.Lerp(a.Floor, b.Floor, t),
                Density = Mathf.Lerp(a.Density, b.Density, t),
                Stars = Mathf.Lerp(a.Stars, b.Stars, t),
            };
        }

        private static readonly int SkyTop = Shader.PropertyToID("_RideSkyTop");
        private static readonly int SkyHorizon = Shader.PropertyToID("_RideSkyHorizon");
        private static readonly int SkyGlow = Shader.PropertyToID("_RideSkyGlow");
        private static readonly int SkyFloor = Shader.PropertyToID("_RideSkyFloor");
        private static readonly int SkyParams = Shader.PropertyToID("_RideSkyParams");

        private readonly HashSet<object> _holders = new HashSet<object>();
        private Camera _camera;
        private State _from, _now;
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
            _now = Target();
            Apply(_now);
            _gridAlpha = _gridTo = 0f;
            ApplyGrid(_gridAlpha);
        }

        /// <summary>An explainer starts explaining: the sky dims to the blackboard (at once at load).</summary>
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

        private State Target()
        {
            if (theme == null) return _now;
            State s = From(theme.Region(_region));
            if (!IsBlackboard) return s;

            // The blackboard is the region's own sky, dimmed, with its floor at half strength.
            float k = theme.blackboardDim;
            s.Top = Scale(s.Top, k);
            s.Horizon = Scale(s.Horizon, k);
            s.Glow = Scale(s.Glow, k);
            s.Floor.a *= 0.5f;
            s.Stars *= 0.7f;
            s.Fog = Scale(s.Fog, k);
            s.Background = s.Horizon;
            s.Density = theme.blackboard.fogDensity;
            return s;
        }

        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        private static State From(RideTheme.RegionLook look) => new State
        {
            Background = look.background,
            Fog = look.fog,
            Density = look.fogDensity,
            Top = look.skyTop,
            Horizon = look.skyHorizon,
            Glow = look.skyGlow,
            Floor = look.floor,
            Stars = look.stars,
        };

        private void BlendTo(float seconds)
        {
            if (theme == null) return;
            _from = _now;
            _gridFrom = _gridAlpha;
            _gridTo = 0f;
            _seconds = seconds;
            _t = 0f;
        }

        private void Snap()
        {
            _t = 1f;
            _now = Target();
            Apply(_now);
            _gridAlpha = _gridTo;
            ApplyGrid(_gridAlpha);
        }

        private void Update()
        {
            if (_t >= 1f || theme == null) return;
            _t = Mathf.Min(1f, _t + Time.deltaTime / Mathf.Max(0.05f, _seconds));
            float e = Explainers.Rate.Smooth(_t);
            _now = State.Lerp(_from, Target(), e);
            Apply(_now);
            _gridAlpha = Mathf.Lerp(_gridFrom, _gridTo, e);
            ApplyGrid(_gridAlpha);
        }

        private void Apply(State s)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) _camera.backgroundColor = s.Background;
            RenderSettings.fogColor = s.Fog;
            RenderSettings.fogDensity = s.Density;
            ApplySky(s.Top, s.Horizon, s.Glow, s.Floor, s.Stars, theme);
        }

        /// <summary>Sets the sky shader's globals for one region look (the capture tools use it to light a viewpoint).</summary>
        public static void ApplySky(RideTheme.RegionLook look, RideTheme theme) => ApplySky(look.skyTop, look.skyHorizon, look.skyGlow, look.floor, look.stars, theme);

        private static void ApplySky(Color top, Color horizon, Color glow, Color floor, float stars, RideTheme theme)
        {
            Shader.SetGlobalColor(SkyTop, top);
            Shader.SetGlobalColor(SkyHorizon, horizon);
            Shader.SetGlobalColor(SkyGlow, glow);
            Shader.SetGlobalColor(SkyFloor, floor);
            Shader.SetGlobalVector(SkyParams, new Vector4(stars, theme != null ? theme.floorDepth : 7f, theme != null ? theme.floorSpacing : 3f, 0f));
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
