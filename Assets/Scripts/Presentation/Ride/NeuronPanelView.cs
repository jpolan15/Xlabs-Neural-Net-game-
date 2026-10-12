using TMPro;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Region 2's "why" panel. The puzzle's eight real test digits roll past one at a time; for the one on show it draws
    /// its pixels, each cell's share of the total (ink × weight: cyan adds, amber takes away), the total against the
    /// line, and whether the neuron fired, and the big amber neuron flares when it does. A row of all eight digits
    /// carries a check or a cross each. Every number comes from Core's <see cref="WeightPaintResult"/>.
    /// </summary>
    public sealed class NeuronPanelView : MonoBehaviour
    {
        [SerializeField] private WeightPaintMoment moment;
        [SerializeField] private RideTheme theme;
        [SerializeField] private MeshFilter pixels;
        [SerializeField] private MeshFilter contributions;
        [SerializeField] private Transform sumBar;
        [SerializeField] private Renderer sumBarRenderer;
        [SerializeField] private Material sumFires;
        [SerializeField] private Material sumQuiet;
        [SerializeField] private float barMetersPerUnit = 0.11f;
        [SerializeField] private TMP_Text sumText;
        [SerializeField] private TMP_Text verdictText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Renderer[] checks = new Renderer[0];
        [SerializeField] private Renderer[] crosses = new Renderer[0];
        [SerializeField] private Transform[] thumbnails = new Transform[0];
        [SerializeField] private Transform focusFrame;
        [SerializeField] private Transform neuronGlow;
        [SerializeField] private float dwellSeconds = 2.4f;

        private Mesh _pixelMesh, _contributionMesh;
        private Color32[] _pixelColors, _contributionColors;
        private int _featured;
        private float _nextAt;
        private float _flare;
        private Vector3 _glowScale;

        private void Awake()
        {
            _pixelMesh = Instantiate(pixels.sharedMesh);
            pixels.sharedMesh = _pixelMesh;
            _pixelColors = _pixelMesh.colors32;
            _contributionMesh = Instantiate(contributions.sharedMesh);
            contributions.sharedMesh = _contributionMesh;
            _contributionColors = _contributionMesh.colors32;
            if (neuronGlow != null) _glowScale = neuronGlow.localScale;
        }

        private void OnEnable()
        {
            if (moment != null) moment.Changed += OnChanged;
            _nextAt = 0f;
        }

        private void OnDisable()
        {
            if (moment != null) moment.Changed -= OnChanged;
        }

        private void OnChanged(WeightPaintResult result) => Draw();

        private void Update()
        {
            if (moment == null) return;
            moment.Prepare();
            if (moment.Puzzle == null) return;
            if (Time.time >= _nextAt)
            {
                _nextAt = Time.time + dwellSeconds;
                _featured = (_featured + 1) % moment.Puzzle.DigitCount;
                Draw();
                if (moment.Result != null && moment.Result.Fired(_featured)) _flare = 1f;
            }

            _flare = Mathf.Max(0f, _flare - Time.deltaTime * 0.8f);
            if (neuronGlow != null) neuronGlow.localScale = _glowScale * (0.7f + 0.6f * _flare);
        }

        private void Draw()
        {
            WeightPaintPuzzle puzzle = moment.Puzzle;
            WeightPaintResult result = moment.Result;
            if (puzzle == null || result == null) return;
            int d = Mathf.Clamp(_featured, 0, puzzle.DigitCount - 1);

            float biggest = 0.0001f;
            for (int c = 0; c < puzzle.CellCount; c++) biggest = Mathf.Max(biggest, Mathf.Abs((float)result.Contribution(d, c)));
            for (int c = 0; c < puzzle.CellCount; c++)
            {
                float ink = (float)puzzle.Pixel(d, c);
                Fill(_pixelColors, c, new Color(ink, ink, ink, ink));
                float share = (float)result.Contribution(d, c);
                Color tint = share >= 0f ? theme.cyan : theme.amber;
                float strength = Mathf.Abs(share) / Mathf.Max(1f, biggest);
                Color dark = new Color(0.06f, 0.07f, 0.10f, 0f);
                Color cell = Color.Lerp(dark, tint, Mathf.Clamp01(strength));
                cell.a = strength;
                Fill(_contributionColors, c, cell);
            }

            _pixelMesh.colors32 = _pixelColors;
            _contributionMesh.colors32 = _contributionColors;

            float sum = (float)result.Sum(d);
            bool fired = result.Fired(d);
            bool should = puzzle.ShouldFire(d);
            if (sumBar != null)
            {
                float h = Mathf.Clamp(sum, -4f, 6f) * barMetersPerUnit;
                sumBar.localScale = new Vector3(sumBar.localScale.x, Mathf.Max(0.004f, Mathf.Abs(h)), sumBar.localScale.z);
                sumBar.localPosition = new Vector3(sumBar.localPosition.x, h * 0.5f, sumBar.localPosition.z);
            }

            if (sumBarRenderer != null) sumBarRenderer.sharedMaterial = fired ? sumFires : sumQuiet;
            if (sumText != null) sumText.text = "TOTAL " + sum.ToString("0.0") + "\nLINE " + puzzle.Threshold.ToString("0.0");
            if (verdictText != null)
            {
                string act = fired ? "FIRES" : "QUIET";
                bool right = fired == should;
                verdictText.text = "A " + (should ? "1" : "0") + ": " + act + (right ? " - RIGHT" : " - WRONG");
                verdictText.color = right ? theme.right : theme.wrongRed;
            }

            for (int i = 0; i < puzzle.DigitCount; i++)
            {
                bool ok = result.Correct(i);
                if (i < checks.Length && checks[i] != null) checks[i].enabled = ok;
                if (i < crosses.Length && crosses[i] != null) crosses[i].enabled = !ok;
            }

            if (scoreText != null) scoreText.text = result.CorrectCount + " / " + puzzle.DigitCount + " RIGHT";
            if (focusFrame != null && d < thumbnails.Length && thumbnails[d] != null)
            {
                focusFrame.position = thumbnails[d].position;
            }
        }

        private static void Fill(Color32[] colors, int cell, Color c)
        {
            int at = cell * 4;
            if (at + 4 > colors.Length) return;
            Color32 c32 = c;
            for (int k = 0; k < 4; k++) colors[at + k] = c32;
        }
    }
}
