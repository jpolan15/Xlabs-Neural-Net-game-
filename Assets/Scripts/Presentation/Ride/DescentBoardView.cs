using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The descent game's board on the table: the loss hill drawn as a curve, a gold ball on it, a gold trail joining
    /// where each step landed (a giant step zig-zags across the bottom), the green bottom mark, and the three step
    /// buttons, which flash when shot. Each step hops the ball along an arc to its new place. Display only:
    /// <see cref="DescentMoment"/> and Core decide.
    /// </summary>
    public sealed class DescentBoardView : MonoBehaviour
    {
        private const int Buttons = 3;

        [SerializeField] private RideDirector director;
        [SerializeField] private DescentMoment game;
        [SerializeField] private GameObject board;
        [SerializeField] private int showSegment = -1;
        [SerializeField] private LineRenderer curve;
        [SerializeField] private LineRenderer trail;
        [SerializeField] private Transform ball;
        [SerializeField] private MeshFilter tiles;
        [Tooltip("The board area the hill fills, in the board's local metres.")]
        [SerializeField] private Vector2 size = new Vector2(0.42f, 0.22f);
        [SerializeField] private Vector2 offset = new Vector2(-0.09f, 0.0f);
        [SerializeField] private float hopSeconds = 0.5f;
        [SerializeField] private Color buttonColor = new Color(0.15f, 0.35f, 0.6f, 0.55f);
        [SerializeField] private Color flashColor = new Color(1f, 0.85f, 0.4f, 1f);

        private double _fromX, _toX;
        private float _hopStarted = -100f;
        private int _pressed = -1;
        private Mesh _mesh;
        private Color[] _colors;

        private void Awake()
        {
            if (tiles == null) return;
            _mesh = Instantiate(tiles.sharedMesh);
            tiles.sharedMesh = _mesh;
            _colors = _mesh.colors;
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.SegmentStarted += OnSegment;
                director.SegmentEnded += OnSegmentEnded;
            }

            if (game != null) game.Stepped += OnStepped;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SegmentStarted -= OnSegment;
                director.SegmentEnded -= OnSegmentEnded;
            }

            if (game != null) game.Stepped -= OnStepped;
        }

        private void Start()
        {
            if (board != null) board.SetActive(false);
        }

        private void OnSegment(int index)
        {
            if (index != showSegment || board == null) return;
            board.SetActive(true);
            _fromX = _toX = game.Game.Start;
            trail.positionCount = 1;
            trail.SetPosition(0, Point(game.Game.Start));
            DrawCurve();
        }

        private void OnSegmentEnded(int index)
        {
            if (index == showSegment && board != null) board.SetActive(false);
        }

        private void OnStepped(double from, double to, int button)
        {
            _fromX = from;
            _toX = to;
            _hopStarted = Time.time;
            _pressed = button;
            trail.positionCount += 1;
            trail.SetPosition(trail.positionCount - 1, Point(to));
        }

        private void Update()
        {
            if (board == null || !board.activeSelf || game == null) return;
            float t = Mathf.Clamp01((Time.time - _hopStarted) / hopSeconds);
            double x = _fromX + (_toX - _fromX) * (t * t * (3f - 2f * t));
            Vector3 p = Point(x);
            float jump = Mathf.Abs((float)(_toX - _fromX)) / (float)(game.Game.MaxX - game.Game.MinX) * size.y * 1.2f;
            p.y += Mathf.Sin(t * Mathf.PI) * jump + 0.012f;
            p.z = -0.012f;
            ball.localPosition = p;

            if (_colors == null) return;
            for (int b = 0; b < Buttons; b++)
            {
                Color c = b == _pressed && t < 1f ? Color.Lerp(flashColor, buttonColor, t) : buttonColor;
                if (game.IsLive && b == 1) c = Color.Lerp(c, new Color(0.25f, 0.55f, 0.9f, 0.7f), 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
                for (int i = b * 4; i < b * 4 + 4 && i < _colors.Length; i++) _colors[i] = c;
            }

            // The bottom mark (the last tile) sits under the bottom of the hill.
            _mesh.colors = _colors;
        }

        private Vector3 Point(double x)
        {
            var g = game.Game;
            double top = System.Math.Max(g.Loss(g.MinX), g.Loss(g.MaxX));
            float u = (float)((x - g.MinX) / (g.MaxX - g.MinX));
            float v = (float)((g.Loss(x) - g.Floor) / (top - g.Floor));
            return new Vector3(offset.x + (u - 0.5f) * size.x, offset.y + (v - 0.5f) * size.y, -0.006f);
        }

        private void DrawCurve()
        {
            if (curve == null) return;
            var g = game.Game;
            int n = curve.positionCount;
            for (int i = 0; i < n; i++) curve.SetPosition(i, Point(g.MinX + (g.MaxX - g.MinX) * i / (n - 1)));

            // Move the bottom mark's tile (the last four vertices) under the hill's bottom.
            if (_mesh == null) return;
            Vector3[] v = _mesh.vertices;
            Vector3 at = Point(g.Bottom) + new Vector3(0, -0.016f, 0.001f);
            int k = v.Length - 4;
            if (k < 0) return;
            Vector3 c = (v[k] + v[k + 1] + v[k + 2] + v[k + 3]) * 0.25f;
            for (int i = k; i < k + 4; i++) v[i] += at - c;
            _mesh.vertices = v;
            _mesh.RecalculateBounds();
        }
    }
}
