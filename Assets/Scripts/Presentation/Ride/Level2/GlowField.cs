using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Many soft camera-facing quads in one mesh and one draw call: stars, sparks, glows, rings, dust, chips of light. A view sets
    /// quad i's centre, size, colour and roll every frame it wants it seen; a quad of size zero is hidden. The quads are written
    /// in the field's own space, so moving the field's parent moves them all.
    /// </summary>
    public sealed class GlowField : MonoBehaviour
    {
        private Mesh _mesh;
        private Vector3[] _centre;
        private Vector2[] _size;
        private Color[] _color;
        private float[] _roll;
        private Vector3[] _v;
        private Color[] _c;
        private Camera _camera;
        private int _count;

        public int Count => _count;

        /// <summary>A field of <paramref name="count"/> quads drawn with a material (the kit's glow or ring).</summary>
        public static GlowField Create(Transform parent, string name, int count, Material material, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = order;
            var field = go.AddComponent<GlowField>();
            field.Init(count);
            return field;
        }

        private void Init(int count)
        {
            _count = count;
            _centre = new Vector3[count];
            _size = new Vector2[count];
            _color = new Color[count];
            _roll = new float[count];
            _v = new Vector3[count * 4];
            _c = new Color[count * 4];
            var uv = new Vector2[count * 4];
            var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int v = i * 4, t = i * 6;
                uv[v] = new Vector2(0f, 0f);
                uv[v + 1] = new Vector2(1f, 0f);
                uv[v + 2] = new Vector2(1f, 1f);
                uv[v + 3] = new Vector2(0f, 1f);
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            _mesh = new Mesh { name = name + " glows" };
            _mesh.MarkDynamic();
            _mesh.vertices = _v;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.colors = _c;
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20000f);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        public void Set(int i, Vector3 local, float size, Color color) => Set(i, local, new Vector2(size, size), color, 0f);

        public void Set(int i, Vector3 local, Vector2 size, Color color, float roll = 0f)
        {
            if (i < 0 || i >= _count) return;
            _centre[i] = local;
            _size[i] = size;
            _color[i] = color;
            _roll[i] = roll;
        }

        public void Hide(int i)
        {
            if (i >= 0 && i < _count) _size[i] = Vector2.zero;
        }

        public void HideAll()
        {
            for (int i = 0; i < _count; i++) _size[i] = Vector2.zero;
        }

        public Color ColorOf(int i) => _color[i];

        private void LateUpdate()
        {
            if (_count == 0) return;
            if (_camera == null) _camera = Camera.main;
            Vector3 right = Vector3.right, up = Vector3.up;
            if (_camera != null)
            {
                right = transform.InverseTransformDirection(_camera.transform.right).normalized;
                up = transform.InverseTransformDirection(_camera.transform.up).normalized;
            }

            for (int i = 0; i < _count; i++)
            {
                int v = i * 4;
                Vector2 s = _size[i] * 0.5f;
                if (s.x <= 0f || s.y <= 0f || _color[i].a <= 0.002f)
                {
                    _v[v] = _v[v + 1] = _v[v + 2] = _v[v + 3] = _centre[i];
                    continue;
                }

                Vector3 r = right, u = up;
                if (_roll[i] != 0f)
                {
                    float cs = Mathf.Cos(_roll[i]), sn = Mathf.Sin(_roll[i]);
                    r = right * cs + up * sn;
                    u = -right * sn + up * cs;
                }

                _v[v] = _centre[i] - r * s.x - u * s.y;
                _v[v + 1] = _centre[i] + r * s.x - u * s.y;
                _v[v + 2] = _centre[i] + r * s.x + u * s.y;
                _v[v + 3] = _centre[i] - r * s.x + u * s.y;
                for (int k = 0; k < 4; k++) _c[v + k] = _color[i];
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
