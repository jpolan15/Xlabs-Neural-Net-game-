using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The rider's 7×7 weight grid on the dash (Region 2). One mesh: each cell is a tile plus two bars that draw "+" or
    /// "−" on it; a cell is cyan for +, amber for − and dark for 0. The orange frame says "press this" while the puzzle
    /// is live, and an orange ring pulses on the cell Core's hint points at. Display only: taps go through the XR cells
    /// to <see cref="WeightPaintMoment.Cycle"/>, and Core decides everything.
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
            moment.HintChanged -= OnHint;
            moment.LiveChanged -= OnLive;
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
            if (show)
            {
                moment.Prepare();
                Paint();
            }
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
