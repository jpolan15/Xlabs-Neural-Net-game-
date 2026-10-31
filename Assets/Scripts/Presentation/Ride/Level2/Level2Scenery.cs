using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Boxes and flat quads with vertex colours in one mesh, for the worlds' towers, columns and floors: one draw call, lit by
    /// the ride's holographic solid shader (the vertex alpha is how much a face glows).
    /// </summary>
    public sealed class SolidMesh
    {
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Vector3> _n = new List<Vector3>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<int> _t = new List<int>();

        public int Vertices => _v.Count;

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
        {
            int i = _v.Count;
            _v.Add(a);
            _v.Add(b);
            _v.Add(c);
            _v.Add(d);
            for (int k = 0; k < 4; k++)
            {
                _n.Add(normal);
                _c.Add(color);
            }

            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
            {
                _t.Add(i); _t.Add(i + 1); _t.Add(i + 2); _t.Add(i); _t.Add(i + 2); _t.Add(i + 3);
            }
            else
            {
                _t.Add(i); _t.Add(i + 2); _t.Add(i + 1); _t.Add(i); _t.Add(i + 3); _t.Add(i + 2);
            }
        }

        /// <summary>A box standing on y = bottomCentre.y: the roof in <paramref name="top"/>, the four sides in <paramref name="side"/>.</summary>
        public void Box(Vector3 bottomCentre, Vector3 size, Color top, Color side)
        {
            Vector3 h = size * 0.5f;
            Vector3 o = bottomCentre + Vector3.up * h.y;
            Vector3 P(float x, float y, float z) => o + new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Vector3.up, top);
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector3.back, side);
            Quad(P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), Vector3.forward, side);
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), Vector3.left, side);
            Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), Vector3.right, side);
        }

        /// <summary>A box of any size and turn about its centre, one colour.</summary>
        public void Block(Vector3 centre, Vector3 size, Quaternion turn, Color color)
        {
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z) => centre + turn * new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), turn * Vector3.up, color);
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), turn * Vector3.down, color);
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), turn * Vector3.back, color);
            Quad(P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), turn * Vector3.forward, color);
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), turn * Vector3.left, color);
            Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), turn * Vector3.right, color);
        }

        /// <summary>An upright many-sided post standing on <paramref name="bottomCentre"/>, narrowing to <paramref name="topRadius"/> (0: a cone).</summary>
        public void Post(Vector3 bottomCentre, float radius, float height, int sides, Color side, Color top, float topRadius = -1f)
        {
            if (topRadius < 0f) topRadius = radius;
            float slope = (radius - topRadius) / Mathf.Max(0.001f, height);
            Vector3 up = bottomCentre + Vector3.up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides, am = (a0 + a1) * 0.5f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 n = (new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am)) + Vector3.up * slope).normalized;
                Quad(bottomCentre + d0 * radius, bottomCentre + d1 * radius, up + d1 * topRadius, up + d0 * topRadius, n, side);
                if (topRadius > 0.001f) Quad(up, up, up + d1 * topRadius, up + d0 * topRadius, Vector3.up, top);
            }
        }

        /// <summary>A round thing (a low-poly ball) stretched by <paramref name="radii"/>: tree tops, balloons, bushes, hills, domes.</summary>
        public void Ball(Vector3 centre, Vector3 radii, Color color, int slices = 8, int stacks = 5, float fromStack = 0f)
        {
            Vector3 P(int i, int j)
            {
                float lon = i * Mathf.PI * 2f / slices, lat = Mathf.PI * (j / (float)stacks - 0.5f);
                return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon) * radii.x, Mathf.Sin(lat) * radii.y, Mathf.Cos(lat) * Mathf.Sin(lon) * radii.z);
            }

            for (int j = Mathf.FloorToInt(fromStack * stacks); j < stacks; j++)
            {
                for (int i = 0; i < slices; i++)
                {
                    Vector3 a = P(i, j), b = P(i + 1, j), c = P(i + 1, j + 1), d = P(i, j + 1);
                    Vector3 mid = (a + b + c + d) * 0.25f;
                    Vector3 n = new Vector3(mid.x / (radii.x * radii.x), mid.y / (radii.y * radii.y), mid.z / (radii.z * radii.z)).normalized;
                    Quad(centre + a, centre + b, centre + c, centre + d, n, color);
                }
            }
        }

        /// <summary>A pitched roof over a box of <paramref name="size"/> (its ridge along x) whose eaves sit on <paramref name="eaves"/>.</summary>
        public void Roof(Vector3 eaves, Vector3 size, float rise, Color roof, Color gable)
        {
            float hx = size.x * 0.5f + 0.2f, hz = size.z * 0.5f + 0.25f;
            Vector3 ridgeL = eaves + new Vector3(-hx, rise, 0f), ridgeR = eaves + new Vector3(hx, rise, 0f);
            Vector3 fl = eaves + new Vector3(-hx, 0f, -hz), fr = eaves + new Vector3(hx, 0f, -hz), bl = eaves + new Vector3(-hx, 0f, hz), br = eaves + new Vector3(hx, 0f, hz);
            Quad(fl, fr, ridgeR, ridgeL, new Vector3(0f, hz, -rise).normalized, roof);
            Quad(br, bl, ridgeL, ridgeR, new Vector3(0f, hz, rise).normalized, roof);
            float gx = size.x * 0.5f, gz = size.z * 0.5f;
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 a = eaves + new Vector3(s * gx, 0f, -gz), b = eaves + new Vector3(s * gx, 0f, gz), top = eaves + new Vector3(s * gx, rise, 0f);
                Quad(a, b, top, top, new Vector3(s, 0f, 0f), gable);
            }
        }

        public GameObject Build(Transform parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            if (_v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetColors(_c);
            mesh.SetTriangles(_t, 0);
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }
    }

    /// <summary>Building blocks every Level 2 world shares: the daylight countryside the bike rides through (ADR-023).</summary>
    public static class Level2Scenery
    {
        // The countryside's palette: a sunny picture book, every colour lit by the solid shader's key light.
        public static readonly Color Leaf = new Color(0.32f, 0.66f, 0.34f, 0f);
        public static readonly Color LeafDark = new Color(0.2f, 0.5f, 0.3f, 0f);
        public static readonly Color LeafWarm = new Color(0.62f, 0.76f, 0.3f, 0f);
        public static readonly Color Bark = new Color(0.5f, 0.34f, 0.22f, 0f);
        public static readonly Color Wall = new Color(0.98f, 0.93f, 0.82f, 0f);
        public static readonly Color RoofRed = new Color(0.86f, 0.32f, 0.26f, 0f);
        public static readonly Color RoofBlue = new Color(0.3f, 0.5f, 0.86f, 0f);
        public static readonly Color Wood = new Color(0.78f, 0.56f, 0.34f, 0f);
        public static readonly Color Stone = new Color(0.72f, 0.74f, 0.78f, 0f);
        public static readonly Color Asphalt = new Color(0.34f, 0.36f, 0.42f, 0f);
        public static readonly Color Paint = new Color(0.98f, 0.97f, 0.92f, 0f);
        public static readonly Color[] Bright =
        {
            new Color(1f, 0.36f, 0.32f, 0f), new Color(1f, 0.76f, 0.2f, 0f), new Color(0.3f, 0.66f, 1f, 0f),
            new Color(0.4f, 0.82f, 0.45f, 0f), new Color(0.95f, 0.45f, 0.78f, 0f), new Color(0.6f, 0.45f, 0.95f, 0f),
        };

        /// <summary>A round tree: a trunk and two or three leafy balls, a little different for every seed.</summary>
        public static void Tree(SolidMesh m, Vector3 at, float scale, int seed)
        {
            float r1 = Fx.Rand01(seed * 3 + 1), r2 = Fx.Rand01(seed * 3 + 2);
            float h = (1.6f + 0.8f * r1) * scale;
            m.Post(at, 0.16f * scale, h, 6, Bark, Bark, 0.11f * scale);
            Color leaf = r2 < 0.33f ? Leaf : r2 < 0.66f ? LeafDark : LeafWarm;
            float w = (1.3f + 0.5f * r2) * scale;
            m.Ball(at + Vector3.up * (h + w * 0.55f), new Vector3(w, w * 0.95f, w), leaf, 6, 4);
            m.Ball(at + new Vector3(w * 0.45f, h + w * 0.2f, -w * 0.2f), Vector3.one * w * 0.62f, Fx.Mul(leaf, 0.9f), 6, 4);
            if (r1 > 0.5f) m.Ball(at + new Vector3(-w * 0.4f, h + w * 0.95f, w * 0.15f), Vector3.one * w * 0.55f, Fx.Mul(leaf, 1.08f), 6, 4);
        }

        /// <summary>A pointed tree (a fir): a short trunk and three stacked cones.</summary>
        public static void Fir(SolidMesh m, Vector3 at, float scale)
        {
            m.Post(at, 0.14f * scale, 0.7f * scale, 5, Bark, Bark);
            for (int k = 0; k < 3; k++)
            {
                float r = (1.25f - k * 0.3f) * scale;
                m.Post(at + Vector3.up * (0.6f + k * 0.95f) * scale, r, 1.5f * scale, 7, LeafDark, LeafDark, 0f);
            }
        }

        /// <summary>A little house: walls, a pitched roof, a door and two windows on the side that faces <paramref name="facing"/> (+1 toward +x).</summary>
        public static void House(SolidMesh m, Vector3 at, Vector3 size, Color roof, float facing)
        {
            m.Box(at, size, Wall, Wall);
            m.Roof(at + Vector3.up * size.y, size, size.z * 0.55f, roof, Wall);
            float x = at.x + facing * (size.x * 0.5f + 0.02f);
            Color dark = new Color(0.25f, 0.3f, 0.42f, 0f);
            m.Block(new Vector3(x, 0.9f, at.z), new Vector3(0.05f, 1.8f, 0.9f), Quaternion.identity, new Color(0.55f, 0.32f, 0.2f, 0f));
            foreach (float dz in new[] { -size.z * 0.3f, size.z * 0.3f })
            {
                m.Block(new Vector3(x, size.y * 0.62f, at.z + dz), new Vector3(0.05f, 0.8f, 0.8f), Quaternion.identity, dark);
            }
        }

        /// <summary>A striped fair tent: a round wall in two colours and a cone roof with a flag.</summary>
        public static void Tent(SolidMesh m, Vector3 at, float radius, Color a, Color b)
        {
            const int sides = 12;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides, am = (a0 + a1) * 0.5f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius, d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                Color c = i % 2 == 0 ? a : b;
                m.Quad(at + d0, at + d1, at + d1 + Vector3.up * 2.4f, at + d0 + Vector3.up * 2.4f, new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am)), c);
                Vector3 tip = at + Vector3.up * (2.4f + radius * 0.9f);
                m.Quad(at + d0 * 1.08f + Vector3.up * 2.4f, at + d1 * 1.08f + Vector3.up * 2.4f, tip, tip, (new Vector3(Mathf.Cos(am), 0.9f, Mathf.Sin(am))).normalized, c);
            }

            Vector3 top = at + Vector3.up * (2.4f + radius * 0.9f);
            m.Post(top, 0.04f, 1.0f, 4, Paint, Paint);
            m.Block(top + new Vector3(0.3f, 0.85f, 0f), new Vector3(0.55f, 0.3f, 0.03f), Quaternion.identity, b);
        }

        /// <summary>A street lamp beside the road (its lamp glows: vertex alpha).</summary>
        public static void Lamp(SolidMesh m, Vector3 at, float towardRoad)
        {
            Color pole = new Color(0.24f, 0.28f, 0.34f, 0f);
            m.Post(at, 0.07f, 3.6f, 6, pole, pole);
            m.Block(at + new Vector3(towardRoad * 0.35f, 3.55f, 0f), new Vector3(0.75f, 0.08f, 0.08f), Quaternion.identity, pole);
            m.Ball(at + new Vector3(towardRoad * 0.68f, 3.42f, 0f), new Vector3(0.2f, 0.16f, 0.2f), new Color(1f, 0.93f, 0.7f, 0.8f), 6, 4);
        }

        /// <summary>A patch of flowers: a green stem and a coloured head each, scattered on the grass.</summary>
        public static void Flowers(SolidMesh m, Vector3 centre, Vector2 spread, int count, int seed)
        {
            Color stem = new Color(0.25f, 0.55f, 0.28f, 0f);
            for (int i = 0; i < count; i++)
            {
                float r1 = Fx.Rand01(seed + i * 5 + 1), r2 = Fx.Rand01(seed + i * 5 + 2), r3 = Fx.Rand01(seed + i * 5 + 3);
                Vector3 p = centre + new Vector3((r1 * 2f - 1f) * spread.x, 0f, (r2 * 2f - 1f) * spread.y);
                float h = 0.22f + 0.14f * r3;
                m.Block(p + Vector3.up * h * 0.5f, new Vector3(0.025f, h, 0.025f), Quaternion.identity, stem);
                m.Block(p + Vector3.up * h, new Vector3(0.11f, 0.06f, 0.11f), Quaternion.Euler(0f, r3 * 90f, 0f), Bright[(int)(r3 * Bright.Length) % Bright.Length]);
            }
        }

        /// <summary>A soft green hill, mostly under the ground.</summary>
        public static void Hill(SolidMesh m, Vector3 at, Vector3 radii, Color color) => m.Ball(at, radii, color, 12, 6, 0.5f);

        /// <summary>Text in the scenery draws before the boards and glows (sorting order), so a far sign never shows through a near board.</summary>
        public const int SceneryOrder = -6;

        /// <summary>Words painted on the scenery (an arch, a sign, a block): the kit's outlined text, sorted behind the boards.</summary>
        public static TextMeshPro Words(Transform parent, string name, string text, float meters, Color color, Vector3 at, float width)
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return null;
            TextMeshPro t = kit.Text(parent, name, text, meters, color, at, width);
            t.GetComponent<Renderer>().sortingOrder = SceneryOrder;
            return t;
        }

        /// <summary>A sign on two posts with words on it, facing the rider at the stop (the board is solid, the words are text).</summary>
        public static TextMeshPro Sign(SolidMesh m, Transform parent, Vector3 at, Vector2 board, string words, Color boardColor, Color ink)
        {
            foreach (float s in new[] { -1f, 1f }) m.Post(at + new Vector3(s * board.x * 0.4f, 0f, 0.05f), 0.07f, board.y * 0.5f + 1.6f, 5, Wood, Wood);
            m.Block(at + new Vector3(0f, 1.6f + board.y * 0.5f, 0f), new Vector3(board.x, board.y, 0.12f), Quaternion.identity, boardColor);
            return Words(parent, "Sign", words, board.y * 0.42f, ink, at + new Vector3(0f, 1.6f + board.y * 0.5f, -0.08f), board.x * 0.92f);
        }

        /// <summary>Makes <paramref name="t"/> face the rider (its +z points away from the eye), turning around y only unless told otherwise.</summary>
        public static void FaceEye(Transform t, Vector3 eyeWorld, bool flat = true)
        {
            Vector3 d = t.position - eyeWorld;
            if (flat) d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        public static Vector3 Eye(Transform stage) => stage.TransformPoint(new Vector3(0f, 1.3f, 0f));
    }
}
