using System.Collections.Generic;
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

    /// <summary>Building blocks every Level 2 world shares.</summary>
    public static class Level2Scenery
    {
        /// <summary>Two glowing rails with sleepers, under the pod's way, from z0 to z1 (the track the pod rides).</summary>
        public static void Rails(Transform parent, float z0, float z1, Color color)
        {
            Level2Kit kit = Level2Kit.Current;
            var shapes = new List<Shape>();
            foreach (float x in new[] { -0.55f, 0.55f })
            {
                shapes.Add(new Shape
                {
                    kind = Shape.Kind.Line, position = new Vector3(x, -0.06f, z0), end = new Vector3(x, -0.06f, z1), size = new Vector2(0.06f, 0.06f),
                    color = color, round = 1f, rotation = Quaternion.Euler(90f, 0f, 0f),
                });
            }

            Color sleeper = Fx.A(color, color.a * 0.45f);
            for (float z = z0; z <= z1; z += 2.5f)
            {
                shapes.Add(new Shape
                {
                    kind = Shape.Kind.Quad, position = new Vector3(0f, -0.07f, z), size = new Vector2(1.5f, 0.12f), color = sleeper, round = 0.5f,
                    rotation = Quaternion.Euler(90f, 0f, 0f),
                });
            }

            kit.Batch(parent, "Rails", shapes, -3);
        }

        /// <summary>A square frame (a gate) standing across the way: four glowing bars.</summary>
        public static void Frame(List<Shape> into, Vector3 centre, Vector2 size, float width, Color color)
        {
            float hx = size.x * 0.5f, hy = size.y * 0.5f;
            into.Add(Level2Kit.Line(centre + new Vector3(-hx, -hy, 0f), centre + new Vector3(-hx, hy, 0f), width, color));
            into.Add(Level2Kit.Line(centre + new Vector3(hx, -hy, 0f), centre + new Vector3(hx, hy, 0f), width, color));
            into.Add(Level2Kit.Line(centre + new Vector3(-hx, hy, 0f), centre + new Vector3(hx, hy, 0f), width, color));
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
