using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The rider's 7×7 grid on the dash, shared by two games. In Region 1 (draw the 3) a cell is a faint blue square
    /// where the hero 3 has ink and dark elsewhere, and white once the rider inks it. In Region 2 (paint the weights)
    /// each cell is a tile plus two bars that draw "+" or "−" on it: green for +, red for −, dark for 0. The orange
    /// frame says "press this" while a game is live, and an orange ring pulses on the cell Core's hint points at.
    /// Display only: presses go through the XR cells to the live game, and Core decides everything.
    /// </summary>
    public sealed class WeightGridView : MonoBehaviour
    {
        /// <summary>Vertices per cell in the grid mesh: the tile, the horizontal bar, the vertical bar (4 each).</summary>
        public const int VertsPerCell = 12;

        [SerializeField] private RideDirector director;
        [SerializeField] private WeightPaintMoment moment;
        [SerializeField] private DrawMoment draw;
        [SerializeField] private int drawSegment = 0;
        [SerializeField] private RideTheme theme;
        [SerializeField] private GameObject grid;
        [SerializeField] private MeshFilter cells;
        [SerializeField] private Renderer frame;
        [SerializeField] private Material frameLive;
        [SerializeField] private Material frameLocked;
        [SerializeField] private Transform hintRing;
        [SerializeField] private int showFromSegment = 1;
        [SerializeField] private float cellPitch = 0.058f;
        [Tooltip("A painted cell flashes the brush's colour for this long: the right hand's green, the left hand's red.")]
        [SerializeField] private float flashSeconds = 0.22f;
        [SerializeField] private Color flashPositive = new Color(0.180f, 0.800f, 0.443f, 1f);
        [SerializeField] private Color flashNegative = new Color(0.906f, 0.298f, 0.235f, 1f);

        private readonly float[] _flash = new float[49];
        private readonly bool[] _flashPositive = new bool[49];
        private bool _flashing;
        private Mesh _mesh;
        private Color32[] _colors;
        private Color32 _plus, _minus, _zero, _mark, _outline, _inked, _stray;
        private bool _drawing;

        private void Awake()
        {
            _mesh = Instantiate(cells.sharedMesh);
            cells.sharedMesh = _mesh;
            _colors = _mesh.colors32;
            _plus = (Color)theme.explainGreen;
            _minus = (Color)theme.explainRed;
            _zero = new Color(0.10f, 0.13f, 0.20f, 0f);
            _mark = new Color(0.02f, 0.03f, 0.06f, 0f);
            _outline = new Color(0.20f, 0.42f, 0.62f, 0.35f);
            _inked = new Color(1f, 1f, 1f, 1f);
            _stray = new Color(0.85f, 0.85f, 0.95f, 0.85f);
            if (hintRing != null) hintRing.gameObject.SetActive(false);
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
                draw.CellChanged += OnDrawChanged;
                draw.HintChanged += OnDrawHint;
                draw.LiveChanged += OnLive;
            }

            if (moment == null) return;
            moment.Changed += OnChanged;
            moment.Stroked += OnStroked;
            moment.HintChanged += OnHint;
            moment.LiveChanged += OnLive;
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
                draw.CellChanged -= OnDrawChanged;
                draw.HintChanged -= OnDrawHint;
                draw.LiveChanged -= OnLive;
            }

            if (moment == null) return;
            moment.Changed -= OnChanged;
            moment.Stroked -= OnStroked;
            moment.HintChanged -= OnHint;
            moment.LiveChanged -= OnLive;
        }

        private void OnStroked(int cell, int brush, int value, bool byRider)
        {
            if (cell < 0 || cell >= _flash.Length) return;
            _flash[cell] = 1f;
            _flashPositive[cell] = brush >= 0;
            _flashing = true;
        }

        private void OnDrawChanged(int cell) => Paint();

        private void OnDrawHint(int cell) => OnHint(cell, 1);

        private void Start()
        {
            grid.SetActive(false);
            Paint();
            OnLive(false);
        }

        private void OnSegment(int index)
        {
            _drawing = draw != null && index == drawSegment;
            bool show = index == showFromSegment || _drawing;
            grid.SetActive(show);
            if (hintRing != null) hintRing.gameObject.SetActive(false);
            if (!show) return;
            if (_drawing) draw.Prepare();
            else moment.Prepare();
            Paint();
        }

        private void OnSegmentEnded(int index)
        {
            if (index == showFromSegment || (draw != null && index == drawSegment)) grid.SetActive(false);
        }

        private void OnChanged(Convergence.Core.Puzzles.WeightPaintResult result) => Paint();

        private void OnLive(bool live)
        {
            if (frame != null) frame.sharedMaterial = live ? frameLive : frameLocked;
        }

        private void OnHint(int cell, int value)
        {
            if (hintRing == null) return;
            hintRing.gameObject.SetActive(cell >= 0);
            if (cell < 0) return;
            int row = cell / 7, col = cell % 7;
            hintRing.localPosition = new Vector3((col - 3) * cellPitch, (3 - row) * cellPitch, hintRing.localPosition.z);
        }

        private void Update()
        {
            if (hintRing != null && hintRing.gameObject.activeSelf)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 7f);
                hintRing.localScale = new Vector3(pulse, pulse, 1f);
            }

            if (!_flashing) return;
            _flashing = false;
            for (int c = 0; c < _flash.Length; c++)
            {
                if (_flash[c] <= 0f) continue;
                _flash[c] = Mathf.MoveTowards(_flash[c], 0f, Time.deltaTime / Mathf.Max(0.01f, flashSeconds));
                _flashing |= _flash[c] > 0f;
            }

            Paint(); // the last repaint (every flash at 0) puts the plain colours back
        }

        private void Paint()
        {
            if (_drawing)
            {
                PaintDrawing();
                return;
            }

            if (_colors == null || moment == null || moment.Weights == null) return;
            for (int c = 0; c < 49 && (c + 1) * VertsPerCell <= _colors.Length; c++)
            {
                int w = moment.Weights[c];
                Color32 tile = w > 0 ? _plus : w < 0 ? _minus : _zero;
                tile.a = (byte)(w != 0 ? 200 : 0);
                Color32 h = w != 0 ? _mark : tile;
                Color32 v = w > 0 ? _mark : tile;
                float flash = _flash[c];
                if (flash > 0f)
                {
                    // A press lights the cell in the brush's colour, with its + or − glyph in white, then fades.
                    Color burst = _flashPositive[c] ? flashPositive : flashNegative;
                    tile = Color32.Lerp(tile, burst, flash);
                    if (w != 0) h = Color32.Lerp(h, Color.white, flash * 0.85f);
                    if (w > 0) v = Color32.Lerp(v, Color.white, flash * 0.85f);
                }

                int at = c * VertsPerCell;
                for (int k = 0; k < 4; k++)
                {
                    _colors[at + k] = tile;
                    _colors[at + 4 + k] = h;
                    _colors[at + 8 + k] = v;
                }
            }

            _mesh.colors32 = _colors;
        }

        private void PaintDrawing()
        {
            if (_colors == null || draw == null || draw.Puzzle == null) return;
            for (int c = 0; c < 49 && (c + 1) * VertsPerCell <= _colors.Length; c++)
            {
                bool inked = draw.Puzzle.IsInked(c), target = draw.Puzzle.IsTarget(c);
                Color32 tile = inked ? (target ? _inked : _stray) : target ? _outline : _zero;
                int at = c * VertsPerCell;
                for (int k = 0; k < 12; k++) _colors[at + k] = tile;
            }

            _mesh.colors32 = _colors;
        }
    }
}
