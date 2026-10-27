using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The training game's board on the table (v6): the loss curve as it is measured (red-orange, falling as the network
    /// learns, spiking off the top when a huge step blows it up), the test accuracy under it (cyan) with the target as a
    /// green line, a ball riding the head of the loss curve, and the three step-size buttons (lit green while held, dark
    /// red for a moment after an undo; their labels are white, so every colour here keeps them readable). Until the
    /// first step the empty plot says what to do. Display only: <see cref="TrainMoment"/> and Core decide.
    /// </summary>
    public sealed class TrainBoardView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private TrainMoment game;
        [SerializeField] private GameObject board;
        [SerializeField] private int showSegment = -1;
        [SerializeField] private LineRenderer lossCurve;
        [SerializeField] private LineRenderer accuracyCurve;
        [SerializeField] private Transform ball;
        [SerializeField] private MeshFilter tiles;
        [Tooltip("\"Hold a step size\", shown in the empty plot until the first training step.")]
        [SerializeField] private GameObject hint;
        [Tooltip("The plot's size and centre, in the board's local metres.")]
        [SerializeField] private Vector2 plotSize = new Vector2(0.36f, 0.2f);
        [SerializeField] private Vector2 plotCentre = new Vector2(-0.1f, 0.0f);
        [Tooltip("The loss that reaches the top of the plot, as a multiple of the starting loss.")]
        [SerializeField] private float lossTop = 1.4f;
        [SerializeField] private Color buttonColor = new Color(0.15f, 0.35f, 0.6f, 0.55f);
        [SerializeField] private Color idlePulse = new Color(0.2f, 0.45f, 0.85f, 0.75f);
        [SerializeField] private Color heldColor = new Color(0.08f, 0.5f, 0.28f, 1f);
        [SerializeField] private Color coolColor = new Color(0.7f, 0.12f, 0.1f, 0.8f);
        [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.15f, 0.9f);

        private Mesh _mesh;
        private Color[] _colors;
        private Color[] _base;
        private float _flashUntil = -1f;
        private readonly List<Vector3> _points = new List<Vector3>();

        private void Awake()
        {
            if (tiles == null) return;
            _mesh = Instantiate(tiles.sharedMesh);
            tiles.sharedMesh = _mesh;
            _colors = _mesh.colors;
            _base = (Color[])_colors.Clone();
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.SegmentStarted += OnSegment;
                director.SegmentEnded += OnSegmentEnded;
            }

            if (game != null) game.BlewUp += OnBlewUp;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SegmentStarted -= OnSegment;
                director.SegmentEnded -= OnSegmentEnded;
            }

            if (game != null) game.BlewUp -= OnBlewUp;
        }

        private void Start()
        {
            if (board != null) board.SetActive(false);
        }

        private void OnSegment(int index)
        {
            if (index == showSegment && board != null) board.SetActive(true);
        }

        private void OnSegmentEnded(int index)
        {
            if (index == showSegment && board != null) board.SetActive(false);
        }

        private void OnBlewUp() => _flashUntil = Time.time + 0.6f;

        private void Update()
        {
            if (board == null || !board.activeSelf || game == null) return;
            float start = Mathf.Max(0.01f, (float)game.StartLoss);
            Plot(lossCurve, game.Losses, start * lossTop, out Vector3 head);
            Plot(accuracyCurve, game.Accuracies, 1f, out _);
            if (ball != null)
            {
                ball.localPosition = head + new Vector3(0f, 0f, -0.004f);
                ball.localScale = Vector3.one * (0.022f * (game.Held >= 0 ? 1f + 0.15f * Mathf.Sin(Time.time * 18f) : 1f));
            }

            bool showHint = game.IsLive && game.Steps == 0;
            if (hint != null && hint.activeSelf != showHint) hint.SetActive(showHint);

            if (_colors == null) return;
            bool flashing = Time.time < _flashUntil;
            for (int b = 0; b < TrainMoment.ButtonCount; b++)
            {
                Color c = buttonColor;
                if (game.Cooling) c = coolColor;
                else if (game.Held == b) c = heldColor;
                else if (game.IsLive && game.Held < 0) c = Color.Lerp(buttonColor, idlePulse, 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + b));
                for (int i = b * 4; i < b * 4 + 4 && i < _colors.Length; i++) _colors[i] = c;
            }

            // The rest of the tiles (the target line, the plot's frame) keep their colours, and flash red on a blow-up.
            for (int i = TrainMoment.ButtonCount * 4; i < _colors.Length; i++) _colors[i] = flashing ? flashColor : _base[i];
            _mesh.colors = _colors;
        }

        /// <summary>Draws values (each divided by top, clamped to the plot) across the plot, oldest on the left.</summary>
        private void Plot(LineRenderer line, IReadOnlyList<float> values, float top, out Vector3 head)
        {
            head = Corner(0f, 0f);
            if (line == null) return;
            _points.Clear();
            int n = values.Count;
            for (int i = 0; i < n; i++)
            {
                float u = n <= 1 ? 0f : i / (float)(n - 1);
                _points.Add(Corner(u * Mathf.Min(1f, n / 40f), Mathf.Clamp01(values[i] / top)));
            }

            if (_points.Count == 1) _points.Add(_points[0]);
            line.positionCount = _points.Count;
            for (int i = 0; i < _points.Count; i++) line.SetPosition(i, _points[i]);
            if (_points.Count > 0) head = _points[_points.Count - 1];
        }

        /// <summary>A point of the plot: u across (0 left to 1 right), v up (0 bottom to 1 top).</summary>
        private Vector3 Corner(float u, float v) => new Vector3(plotCentre.x + (u - 0.5f) * plotSize.x, plotCentre.y + (v - 0.5f) * plotSize.y, -0.007f);
    }
}
