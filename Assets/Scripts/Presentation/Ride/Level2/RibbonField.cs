using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Curved ribbons of light in one mesh: attention arcs, the roads of the story, streams of tokens. A ribbon runs from a to b along a
    /// bend (a quadratic curve bowed by <c>arch</c> metres), always facing the camera, soft at its edges (the beam texture) and
    /// fading at its ends. <c>draw</c> grows it from a toward b.
    /// </summary>
    public sealed class RibbonField : MonoBehaviour
    {
        public const int Samples = 22;

        private Mesh _mesh;
        private int _count;
        private Vector3[] _a, _b;
        private Vector3[] _bow;
        private float[] _width, _draw;
        private Color[] _color;
        private bool[] _on;
        private Vector3[] _v;
        private Color[] _c;
        private Camera _camera;

        public static RibbonField Create(Transform parent, string name, int count, Material material, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = order;
            var field = go.AddComponent<RibbonField>();
            field.Init(count);
            return field;
        }

        private void Init(int count)
        {
            _count = count;
            _a = new Vector3[count];
            _b = new Vector3[count];
            _bow = new Vector3[count];
            _width = new float[count];
            _draw = new float[count];
            _color = new Color[count];
            _on = new bool[count];
            int per = Samples * 2;
            _v = new Vector3[count * per];
            _c = new Color[count * per];
            var uv = new Vector2[count * per];
            var triangles = new int[count * (Samples - 1) * 6];
            int tri = 0;
            for (int i = 0; i < count; i++)
            {
                for (int s = 0; s < Samples; s++)
                {
                    float u = s / (float)(Samples - 1);
                    uv[i * per + s * 2] = new Vector2(u, 0f);
                    uv[i * per + s * 2 + 1] = new Vector2(u, 1f);
                    if (s == Samples - 1) continue;
                    int v = i * per + s * 2;
                    triangles[tri++] = v;
                    triangles[tri++] = v + 2;
                    triangles[tri++] = v + 1;
                    triangles[tri++] = v + 1;
                    triangles[tri++] = v + 2;
                    triangles[tri++] = v + 3;
                }
            }

            _mesh = new Mesh { name = name + " ribbons" };
            _mesh.MarkDynamic();
            _mesh.vertices = _v;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.colors = _c;
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20000f);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        /// <summary>Ribbon i from a to b (the field's space), bowed upward by arch, width metres, fully drawn when draw is 1.</summary>
        public void Set(int i, Vector3 a, Vector3 b, float arch, float width, Color color, float draw = 1f)
        {
            if (i < 0 || i >= _count) return;
            _a[i] = a;
            _b[i] = b;
            _bow[i] = Vector3.up * arch;
            _width[i] = width;
            _color[i] = color;
            _draw[i] = Mathf.Clamp01(draw);
            _on[i] = width > 0f && color.a > 0.002f && draw > 0.001f;
        }

        public void Hide(int i)
        {
            if (i >= 0 && i < _count) _on[i] = false;
        }

        public void HideAll()
        {
            for (int i = 0; i < _count; i++) _on[i] = false;
        }

        private void LateUpdate()
        {
            if (_count == 0) return;
            if (_camera == null) _camera = Camera.main;
            Vector3 eye = _camera != null ? transform.InverseTransformPoint(_camera.transform.position) : new Vector3(0f, 1.3f, 0f);
            int per = Samples * 2;
            for (int i = 0; i < _count; i++)
            {
                int o = i * per;
                if (!_on[i])
                {
                    for (int k = 0; k < per; k++) _v[o + k] = _a[i];
                    continue;
                }

                Vector3 control = (_a[i] + _b[i]) * 0.5f + _bow[i];
                for (int s = 0; s < Samples; s++)
                {
                    float t = s / (float)(Samples - 1) * _draw[i];
                    float u = 1f - t;
                    Vector3 p = u * u * _a[i] + 2f * u * t * control + t * t * _b[i];
                    Vector3 tangent = 2f * u * (control - _a[i]) + 2f * t * (_b[i] - control);
                    Vector3 side = Vector3.Cross(tangent, p - eye);
                    side = side.sqrMagnitude < 1e-8f ? Vector3.up : side.normalized;
                    float taper = Mathf.Sin(Mathf.Clamp01(s / (float)(Samples - 1)) * Mathf.PI);
                    float half = _width[i] * 0.5f * Mathf.Lerp(0.35f, 1f, taper);
                    _v[o + s * 2] = p - side * half;
                    _v[o + s * 2 + 1] = p + side * half;
                    Color c = _color[i];
                    c.a *= Mathf.Lerp(0.35f, 1f, taper);
                    _c[o + s * 2] = c;
                    _c[o + s * 2 + 1] = c;
                }
            }

            _mesh.vertices = _v;
            _mesh.colors = _c;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
