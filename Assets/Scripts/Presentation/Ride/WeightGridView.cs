using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The rider's 7×7 weight grid on the game table (Region 2, paint the weights; v6 draws the 3 on its own canvas):
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
        private Color32 _plus, _minus, _zero, _mark;

        private void Awake()
        {
            _mesh = Instantiate(cells.sharedMesh);
            cells.sharedMesh = _mesh;
            _colors = _mesh.colors32;
            _plus = (Color)theme.explainGreen;
            _minus = (Color)theme.explainRed;
            _zero = new Color(0.10f, 0.13f, 0.20f, 0f);
            _mark = new Color(0.02f, 0.03f, 0.06f, 0f);
            if (hintRing != null) hintRing.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.SegmentStarted += OnSegment;
                director.SegmentEnded += OnSegmentEnded;
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

        private void Start()
        {
            grid.SetActive(false);
            Paint();
            OnLive(false);
        }

        private void OnSegment(int index)
        {
            bool show = index == showFromSegment;
            grid.SetActive(show);
            if (hintRing != null) hintRing.gameObject.SetActive(false);
            if (!show) return;
            moment.Prepare();
            Paint();
        }

        private void OnSegmentEnded(int index)
        {
            if (index == showFromSegment) grid.SetActive(false);
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

    }
}
