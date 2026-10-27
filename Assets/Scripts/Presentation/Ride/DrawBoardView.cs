using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The drawing board on the table (v6), shared by "draw your 3" and the bonus round: the rider's ink on the canvas
    /// (every stroke in one mesh of short ribbons, rebuilt when the ink changes), the 49 numbers Core made of it as a live
    /// 7×7 ("what Pixel sees"), DONE and CLEAR, and in the bonus round the digit Pixel asks for and ten bars, Pixel's live
    /// probability for each digit. One tile mesh holds, in order, the 49 cells, the 10 bars, DONE, CLEAR, the canvas's four
    /// edges (always lit), then the bars' digits; the view hides the part a game does not use by colour. Two draw calls in all. Display only.
    /// </summary>
    public sealed class DrawBoardView : MonoBehaviour
    {
        public const int CellFaces = 49, BarFaces = 10, ButtonFaces = 2, EdgeFaces = 4;
        private const int FirstBar = CellFaces, FirstButton = CellFaces + BarFaces, FirstGlyph = CellFaces + BarFaces + ButtonFaces + EdgeFaces;

        [SerializeField] private RideDirector director;
        [SerializeField] private DrawMoment draw;
        [SerializeField] private StumpMoment stump;
        [SerializeField] private GameObject board;
        [SerializeField] private int drawSegment = -1;
        [SerializeField] private int stumpSegment = -1;
        [Tooltip("Holds the ink mesh: at the canvas's centre, unscaled.")]
        [SerializeField] private MeshFilter ink;
        [Tooltip("The canvas's side, metres.")]
        [SerializeField] private float canvasSize = 0.27f;
        [SerializeField] private float inkWidth = 0.0075f;
        [SerializeField] private MeshFilter tiles;
        [SerializeField] private TextMeshPro prompt;
        [SerializeField] private TextMeshPro answer;
        [Tooltip("The hint in the empty canvas (\"hold trigger and draw\"), shown while a game is live and the canvas has no ink.")]
        [SerializeField] private TextMeshPro count;
        [Tooltip("A faint 3 to trace, shown in \"draw your 3\" until there is enough ink.")]
        [SerializeField] private TextMeshPro ghost;
        [SerializeField] private float barHeight = 0.12f;
        [SerializeField] private Color inkColor = new Color(0.85f, 0.98f, 1f, 1f);
        [SerializeField] private Color cellDark = new Color(0.04f, 0.06f, 0.12f, 0.9f);
        [SerializeField] private Color cellInk = new Color(0.55f, 0.95f, 1f, 1f);
        [SerializeField] private Color barColor = new Color(0.3f, 0.6f, 0.95f, 0.85f);
        [SerializeField] private Color barGuess = new Color(1f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.15f, 0.35f, 0.6f, 0.6f);
        [SerializeField] private Color buttonReady = new Color(0.05f, 0.5f, 0.25f, 0.95f);

        private Mesh _tiles;
        private Color[] _colors;
        private Color[] _base;
        private Vector3[] _vertices;
        private float[] _barBottom;
        private Mesh _ink;
        private readonly List<Vector3> _inkVerts = new List<Vector3>();
        private readonly List<int> _inkTris = new List<int>();
        private readonly List<Color> _inkColors = new List<Color>();
        private bool _dirtyInk = true;
        private double[] _pixels = new double[CellFaces];
        private float _judgedUntil = -1f;
        private bool _judgedRight;

        private void Awake()
        {
            if (tiles != null && tiles.sharedMesh != null)
            {
                _tiles = Instantiate(tiles.sharedMesh);
                tiles.sharedMesh = _tiles;
                _colors = _tiles.colors;
                _base = (Color[])_colors.Clone();
                _vertices = _tiles.vertices;
                _barBottom = new float[BarFaces];
                for (int b = 0; b < BarFaces; b++) _barBottom[b] = _vertices[(FirstBar + b) * 4].y;
            }

            if (ink != null)
            {
                _ink = new Mesh { name = "Ink" };
                _ink.MarkDynamic();
                ink.sharedMesh = _ink;
            }
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.SegmentStarted += OnSegment;
                director.SegmentEnded += OnSegmentEnded;
            }

            if (draw != null)
            {
                draw.InkChanged += OnInk;
                draw.PixelsChanged += OnPixels;
            }

            if (stump != null)
            {
                stump.InkChanged += OnInk;
                stump.Prompted += OnPrompted;
                stump.Judged += OnJudged;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SegmentStarted -= OnSegment;
                director.SegmentEnded -= OnSegmentEnded;
            }

            if (draw != null)
            {
                draw.InkChanged -= OnInk;
                draw.PixelsChanged -= OnPixels;
            }

            if (stump != null)
            {
                stump.InkChanged -= OnInk;
                stump.Prompted -= OnPrompted;
                stump.Judged -= OnJudged;
            }
        }

        private void Start()
        {
            if (board != null) board.SetActive(false);
        }

        private bool Stumping => director != null && director.SegmentIndex == stumpSegment;

        private void OnSegment(int index)
        {
            if (board == null || (index != drawSegment && index != stumpSegment)) return;
            board.SetActive(true);
            bool bonus = index == stumpSegment;
            if (prompt != null) prompt.text = bonus ? "" : "PIXEL SEES";
            if (answer != null) answer.text = "";
            System.Array.Clear(_pixels, 0, _pixels.Length);
            _dirtyInk = true;
        }

        private void OnSegmentEnded(int index)
        {
            if (board != null && (index == drawSegment || index == stumpSegment)) board.SetActive(false);
        }

        private void OnInk() => _dirtyInk = true;

        private void OnPixels(double[] pixels) => _pixels = pixels;

        private void OnPrompted(int digit)
        {
            if (prompt != null) prompt.text = "DRAW A  " + digit;
        }

        private void OnJudged(int asked, int guess, bool right)
        {
            _judgedUntil = Time.time + 1.4f;
            _judgedRight = right;
        }

        private void Update()
        {
            if (board == null || !board.activeSelf) return;
            bool bonus = Stumping;
            DigitCanvas live = bonus ? (stump != null ? stump.Canvas : null) : (draw != null ? draw.Canvas : null);
            if (_dirtyInk && live != null) DrawInk(live);
            bool playing = bonus ? stump != null && stump.IsLive : draw != null && draw.IsLive;
            bool hint = playing && live != null && live.StrokeCount == 0;
            if (count != null && count.gameObject.activeSelf != hint) count.gameObject.SetActive(hint);
            bool trace = playing && !bonus && live != null && !live.HasEnoughInk;
            if (ghost != null && ghost.gameObject.activeSelf != trace) ghost.gameObject.SetActive(trace);
            if (_tiles == null) return;

            for (int i = 0; i < CellFaces; i++) Face(i, bonus ? Color.clear : Color.Lerp(cellDark, cellInk, Mathf.Clamp01((float)_pixels[i])));
            if (bonus) ShowBars();
            else for (int b = 0; b < BarFaces; b++) Face(FirstBar + b, Color.clear);
            for (int f = FirstGlyph; f * 4 + 3 < _colors.Length; f++) Face(f, bonus ? _base[f * 4] : Color.clear);

            bool ready = live != null && live.HasEnoughInk;
            Face(FirstButton, ready ? Color.Lerp(buttonColor, buttonReady, 0.6f + 0.4f * Mathf.Sin(Time.time * 4f)) : buttonColor);
            Face(FirstButton + 1, live != null && live.StrokeCount > 0 ? buttonColor * 1.2f : buttonColor * 0.6f);
            _tiles.colors = _colors;
        }

        private void ShowBars()
        {
            if (stump == null) return;
            double[] p = stump.Probabilities;
            int guess = stump.Guess;
            for (int b = 0; b < BarFaces; b++)
            {
                float h = Mathf.Max(0.004f, (float)p[b] * barHeight);
                int v = (FirstBar + b) * 4;
                _vertices[v + 2].y = _barBottom[b] + h;
                _vertices[v + 3].y = _barBottom[b] + h;
                Face(FirstBar + b, b == guess ? barGuess : barColor);
            }

            _tiles.vertices = _vertices;
            if (answer == null) return;
            if (Time.time < _judgedUntil) answer.text = _judgedRight ? "PIXEL READ IT!" : "FOOLED PIXEL!";
            else if (guess < 0) answer.text = "";
            else answer.SetText("PIXEL: {0:0}  {1:0}%", guess, (float)p[guess] * 100f);
        }

        /// <summary>Every stroke as a ribbon of quads (with a square at each point so the joints stay solid), in metres.</summary>
        private void DrawInk(DigitCanvas live)
        {
            _dirtyInk = false;
            if (_ink == null) return;
            _inkVerts.Clear();
            _inkTris.Clear();
            _inkColors.Clear();
            float h = inkWidth * 0.5f;
            for (int s = 0; s < live.StrokeCount; s++)
            {
                IReadOnlyList<DigitCanvas.Point> stroke = live.Stroke(s);
                for (int i = 0; i < stroke.Count; i++)
                {
                    Vector3 b = At(stroke[i]);
                    Quad(b + new Vector3(-h, -h, 0f), b + new Vector3(h, -h, 0f), b + new Vector3(h, h, 0f), b + new Vector3(-h, h, 0f));
                    if (i == 0) continue;
                    Vector3 a = At(stroke[i - 1]);
                    Vector3 d = b - a;
                    if (d.sqrMagnitude < 1e-10f) continue;
                    Vector3 n = new Vector3(-d.y, d.x, 0f).normalized * h;
                    Quad(a - n, b - n, b + n, a + n);
                }
            }

            _ink.Clear();
            _ink.SetVertices(_inkVerts);
            _ink.SetColors(_inkColors);
            _ink.SetTriangles(_inkTris, 0);
            _ink.bounds = new Bounds(Vector3.zero, new Vector3(canvasSize, canvasSize, 0.05f));
        }

        private Vector3 At(DigitCanvas.Point p) => new Vector3(((float)p.X - 0.5f) * canvasSize, (0.5f - (float)p.Y) * canvasSize, 0f);

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = _inkVerts.Count;
            _inkVerts.Add(a);
            _inkVerts.Add(b);
            _inkVerts.Add(c);
            _inkVerts.Add(d);
            for (int k = 0; k < 4; k++) _inkColors.Add(inkColor);
            // Both windings, so the ink shows whichever way round the canvas is drawn.
            Tri(i, i + 1, i + 2);
            Tri(i, i + 2, i + 3);
            Tri(i, i + 2, i + 1);
            Tri(i, i + 3, i + 2);
        }

        private void Tri(int a, int b, int c)
        {
            _inkTris.Add(a);
            _inkTris.Add(b);
            _inkTris.Add(c);
        }

        private void Face(int face, Color c)
        {
            int v = face * 4;
            if (_colors == null || v + 3 >= _colors.Length) return;
            _colors[v] = _colors[v + 1] = _colors[v + 2] = _colors[v + 3] = c;
        }
    }
}
