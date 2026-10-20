using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>One shape in a <see cref="ShapeBatch"/>: a square, a rounded square, a circle, a picture, or a line.</summary>
    [Serializable]
    public sealed class Shape
    {
        public enum Kind
        {
            Quad,
            Line
        }

        public Kind kind;

        [Tooltip("Centre (a quad) or start (a line), in the batch's space.")]
        public Vector3 position;

        [Tooltip("A quad faces along this rotation's -z; a line lies in the plane whose normal is this rotation's -z.")]
        public Quaternion rotation = Quaternion.identity;

        [Tooltip("A quad's width and height in metres; a line's width in x.")]
        public Vector2 size = Vector2.one;

        [Tooltip("A line's end, in the batch's space.")]
        public Vector3 end;

        [Tooltip("How much of a line is drawn from its start (Create), 0 to 1.")]
        [Range(0f, 1f)] public float grow = 1f;

        public Color color = Color.white;
        public Color stroke = new Color(1f, 1f, 1f, 0f);
        public float strokeWidth;

        [Tooltip("0 = square corners, 1 = fully round (a circle, or a line with round caps).")]
        [Range(0f, 1f)] public float round;

        [Tooltip("Picture coordinates in the batch's texture (x, y, width, height).")]
        public Rect uv = new Rect(0f, 0f, 1f, 1f);

        [Range(0f, 1f)] public float alpha = 1f;
        public float scale = 1f;

        public Shape Clone() => (Shape)MemberwiseClone();

        public void CopyFrom(Shape o)
        {
            kind = o.kind;
            position = o.position;
            rotation = o.rotation;
            size = o.size;
            end = o.end;
            grow = o.grow;
            color = o.color;
            stroke = o.stroke;
            strokeWidth = o.strokeWidth;
            round = o.round;
            uv = o.uv;
            alpha = o.alpha;
            scale = o.scale;
        }
    }

    /// <summary>
    /// Many flat shapes drawn as ONE mesh and one draw call (Quest 2 budget): the explainers' grid cells, circles,
    /// wires and pictures. The builder writes the shapes' home layout; at run time an explainer changes any shape's
    /// fields and the mesh is rebuilt once at the end of the frame. The look (anti-aliased rounded shapes with an
    /// optional stroke) comes from the Convergence/ExplainerFlat shader. Display only.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ShapeBatch : MonoBehaviour
    {
        [SerializeField] private List<Shape> shapes = new List<Shape>();

        private readonly List<Shape> _live = new List<Shape>();
        private Mesh _mesh;
        private bool _dirty = true;

        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<Vector2> _uv0 = new List<Vector2>();
        private readonly List<Vector4> _uv1 = new List<Vector4>();
        private readonly List<Vector4> _uv2 = new List<Vector4>();
        private readonly List<Vector4> _uv3 = new List<Vector4>();
        private readonly List<int> _t = new List<int>();

        public int Count => Live.Count;

        /// <summary>The shape as it is now; change its fields, then <see cref="MarkDirty"/>.</summary>
        public Shape this[int index] => Live[index];

        /// <summary>The shape as the builder laid it out.</summary>
        public Shape Home(int index) => shapes[index];

        private List<Shape> Live
        {
            get
            {
                if (_live.Count != shapes.Count) ResetToHome();
                return _live;
            }
        }

        public void MarkDirty() => _dirty = true;

        /// <summary>Every shape back to the builder's layout.</summary>
        public void ResetToHome()
        {
            _live.Clear();
            foreach (Shape s in shapes) _live.Add(s.Clone());
            _dirty = true;
        }

        /// <summary>Builder only: the home layout.</summary>
        public void SetShapes(IList<Shape> layout)
        {
            shapes = new List<Shape>(layout.Count);
            foreach (Shape s in layout) shapes.Add(s.Clone());
            ResetToHome();
            Rebuild();
        }

        private void OnEnable()
        {
            ResetToHomeIfNeeded();
            Rebuild();
        }

        private void ResetToHomeIfNeeded()
        {
            if (_live.Count != shapes.Count) ResetToHome();
        }

        private void LateUpdate()
        {
            if (_dirty) Rebuild();
        }

        private void OnDestroy()
        {
            if (_mesh == null) return;
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }

        /// <summary>Writes the mesh from the shapes now.</summary>
        public void Rebuild()
        {
            _dirty = false;
            if (_mesh == null)
            {
                _mesh = new Mesh { name = name + " shapes", hideFlags = HideFlags.DontSave };
                _mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }

            _v.Clear();
            _c.Clear();
            _uv0.Clear();
            _uv1.Clear();
            _uv2.Clear();
            _uv3.Clear();
            _t.Clear();
            foreach (Shape s in Live)
            {
                if (s.alpha <= 0.001f || s.scale <= 0.0001f) continue;
                if (s.kind == Shape.Kind.Line) AddLine(s);
                else AddQuad(s);
            }

            _mesh.Clear();
            if (_v.Count > 65000) _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.SetVertices(_v);
            _mesh.SetColors(_c);
            _mesh.SetUVs(0, _uv0);
            _mesh.SetUVs(1, _uv1);
            _mesh.SetUVs(2, _uv2);
            _mesh.SetUVs(3, _uv3);
            _mesh.SetTriangles(_t, 0);
            _mesh.RecalculateBounds();
        }

        private void AddQuad(Shape s)
        {
            float hx = s.size.x * 0.5f * s.scale, hy = s.size.y * 0.5f * s.scale;
            Vector3 right = s.rotation * Vector3.right, up = s.rotation * Vector3.up;
            Emit(s, s.position, right, up, hx, hy, Mathf.Min(hx, hy) * s.round);
        }

        private void AddLine(Shape s)
        {
            Vector3 along = (s.end - s.position) * Mathf.Clamp01(s.grow);
            float length = along.magnitude;
            if (length < 1e-5f) return;
            Vector3 dir = along / length;
            Vector3 normal = s.rotation * Vector3.back;
            Vector3 side = Vector3.Cross(normal, dir);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(Vector3.up, dir);
            side.Normalize();
            float hw = s.size.x * 0.5f * s.scale;
            float hx = length * 0.5f + hw;
            Emit(s, s.position + along * 0.5f, dir, side, hx, hw, hw * s.round);
        }

        private void Emit(Shape s, Vector3 centre, Vector3 right, Vector3 up, float hx, float hy, float corner)
        {
            int i = _v.Count;
            Color fill = s.color;
            fill.a *= s.alpha;
            Color stroke = s.stroke;
            stroke.a *= s.alpha;
            var style = new Vector4(corner, s.strokeWidth * s.scale, 0f, 0f);
            for (int k = 0; k < 4; k++)
            {
                float x = k == 0 || k == 3 ? -1f : 1f;
                float y = k < 2 ? -1f : 1f;
                _v.Add(centre + right * (x * hx) + up * (y * hy));
                _c.Add(fill);
                _uv0.Add(new Vector2(s.uv.x + (x * 0.5f + 0.5f) * s.uv.width, s.uv.y + (y * 0.5f + 0.5f) * s.uv.height));
                _uv1.Add(new Vector4(x * hx, y * hy, hx, hy));
                _uv2.Add(style);
                _uv3.Add(stroke);
            }

            _t.Add(i);
            _t.Add(i + 2);
            _t.Add(i + 1);
            _t.Add(i);
            _t.Add(i + 3);
            _t.Add(i + 2);
        }
    }
}
