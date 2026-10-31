using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// What every Level 2 view draws with: the materials (soft glow, ring, beam, flat shapes, lit solids), the two fonts and the theme.
    /// Level 2's worlds and games are built in code when the scene loads (the builder only places them and wires them), so each view asks
    /// the kit for its text, its glow quads, its light ribbons and its shape batches. One per scene; the builder writes its fields.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class Level2Kit : MonoBehaviour
    {
        [SerializeField] private Material glow;
        [SerializeField] private Material ring;
        [SerializeField] private Material beam;
        [SerializeField] private Material shapes;
        [SerializeField] private Material solid;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_FontAsset serif;
        [Tooltip("The font's material with a dark outline (ADR-023): words stay readable on Level 2's bright sky, grass and boards alike.")]
        [SerializeField] private Material outline;
        [SerializeField] private Material serifOutline;
        [SerializeField] private RideTheme theme;

        public static Level2Kit Current { get; private set; }

        /// <summary>Text draws over the shape batches (their orders run up to 5) and under nothing.</summary>
        public const int TextOrder = 8;

        public Material Glow => glow;
        public Material Ring => ring;
        public Material Beam => beam;
        public Material Shapes => shapes;
        public Material Solid => solid;
        public RideTheme Theme => theme;

        // Level 2's palette: a few loud colours, one colour per idea, on the daylight world and on the boards' dark plates.
        public static readonly Color Cyan = new Color(0.25f, 0.88f, 1f);
        public static readonly Color Violet = new Color(0.62f, 0.45f, 1f);
        public static readonly Color Pink = new Color(1f, 0.38f, 0.66f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.38f);
        public static readonly Color Mint = new Color(0.42f, 1f, 0.72f);
        public static readonly Color Coral = new Color(1f, 0.46f, 0.38f);
        public static readonly Color Ink = new Color(0.04f, 0.05f, 0.11f);

        /// <summary>The colour of a neighbourhood of the galaxy.</summary>
        public static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "animals": return Mint;
                case "food": return Gold;
                case "vehicles": return Cyan;
                case "feelings": return Pink;
                case "places": return Violet;
                default: return Color.white;
            }
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        /// <summary>A world-space text on its own object. <paramref name="meters"/> is the height of an em, as in the ride's builder.</summary>
        public TextMeshPro Text(Transform parent, string name, string text, float meters, Color color, Vector3 localPosition, float width = 6f,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = true, bool serifFace = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var t = go.AddComponent<TextMeshPro>();
            bool useSerif = serifFace && serif != null;
            t.font = useSerif ? serif : font;
            Material outlined = useSerif ? serifOutline : outline;
            if (outlined != null) t.fontSharedMaterial = outlined;
            t.fontSize = meters * 10f;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold && !serifFace ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(width, meters * 3f);
            t.text = text;
            t.enableAutoSizing = false;
            var r = t.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = TextOrder; // above the flat shapes (orders 0 to 3) and glows behind it, so a panel never dims its own words
            return t;
        }

        /// <summary>One batch of flat shapes (rounded squares, circles, lines) on its own object: one draw call.</summary>
        public ShapeBatch Batch(Transform parent, string name, IList<Shape> list, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = shapes;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = order;
            var batch = go.AddComponent<ShapeBatch>();
            batch.SetShapes(list);
            return batch;
        }

        public static Shape Square(Vector3 at, Vector2 size, Color fill, float round = 0.2f, Color? stroke = null, float strokeWidth = 0f)
        {
            return new Shape
            {
                kind = Shape.Kind.Quad, position = at, size = size, color = fill, round = round,
                stroke = stroke ?? new Color(1f, 1f, 1f, 0f), strokeWidth = strokeWidth,
            };
        }

        public static Shape Line(Vector3 a, Vector3 b, float width, Color color)
        {
            return new Shape { kind = Shape.Kind.Line, position = a, end = b, size = new Vector2(width, width), color = color, round = 1f };
        }
    }

    /// <summary>Small helpers the Level 2 views share.</summary>
    public static class Fx
    {
        public static float Smooth(float t) => t * t * (3f - 2f * t);

        /// <summary>A pop: 0 to 1 with a little overshoot.</summary>
        public static float Pop(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.7f ? Smooth(t / 0.7f) * 1.12f : Mathf.Lerp(1.12f, 1f, Smooth((t - 0.7f) / 0.3f));
        }

        public static Color A(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

        public static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        /// <summary>The word as the rider reads it (no leading space).</summary>
        public static string Clean(string token) => token.Trim();

        /// <summary>A direction from the eye at azimuth/elevation degrees, at a distance (stage space, eye at (0, 1.3, 0)).</summary>
        public static Vector3 Polar(float azimuth, float elevation, float distance)
        {
            float az = azimuth * Mathf.Deg2Rad, el = elevation * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)) * distance + new Vector3(0f, 1.3f, 0f);
        }

        /// <summary>Spins a quad of glows' roll slowly (for rays, rings).</summary>
        public static float Spin(float speed, float phase = 0f) => Time.time * speed + phase;

        public static float Hash(int n)
        {
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }

        public static float Rand01(int n) => Hash(n) * 0.5f + 0.5f;
    }
}
