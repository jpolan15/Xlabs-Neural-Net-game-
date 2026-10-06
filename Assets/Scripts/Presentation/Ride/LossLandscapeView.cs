using System.Globalization;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The loss landscape: a hill mesh sampled from the grid Gameplay built, contour bands for readability, a marble
    /// that sits at the current weights and rolls downhill each training step, a trail, and an arrow along the slope.
    /// </summary>
    public sealed class LossLandscapeView : MonoBehaviour
    {
        [SerializeField] private StationController station;
        [SerializeField] private RideTheme theme;
        [SerializeField] private MeshFilter surface;
        [SerializeField] private MeshRenderer surfaceRenderer;
        [SerializeField] private Transform marble;
        [SerializeField] private LineRenderer trail;
        [SerializeField] private LineRenderer slopeArrow;
        [SerializeField] private TMP_Text lossLabel;
        [SerializeField] private float marbleFollow = 10f;

        private Mesh _mesh;
        private Texture2D _ramp;
        private Vector3 _target;
        private int _trailCount;

        private void OnEnable()
        {
            if (station == null) return;
            station.LandscapeReady += Build;
            station.TrainingStepped += OnStep;
            trail.positionCount = 0;
            slopeArrow.enabled = false;
            _trailCount = 0;
            if (station.LossGrid != null) Build();
        }

        private void OnDisable()
        {
            if (station == null) return;
            station.LandscapeReady -= Build;
            station.TrainingStepped -= OnStep;
        }

        private void Update()
        {
            if (marble == null) return;
            marble.localPosition = Vector3.Lerp(marble.localPosition, _target, 1f - Mathf.Exp(-marbleFollow * Time.deltaTime));
        }

        private void Build()
        {
            double[,] grid = station.LossGrid;
            if (grid == null) return;
            int res = grid.GetLength(0);
            var vertices = new Vector3[res * res];
            var uvs = new Vector2[res * res];
            var triangles = new int[(res - 1) * (res - 1) * 6];
            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    float h = Mathf.Min((float)grid[i, j] * theme.heightPerLoss, theme.maxHeight);
                    vertices[i * res + j] = new Vector3(
                        (i / (float)(res - 1) - 0.5f) * theme.landscapeSize, h,
                        (j / (float)(res - 1) - 0.5f) * theme.landscapeSize);
                    uvs[i * res + j] = new Vector2(h / theme.maxHeight, 0.5f);
                }
            }

            int t = 0;
            for (int i = 0; i < res - 1; i++)
            {
                for (int j = 0; j < res - 1; j++)
                {
                    int a = i * res + j, b = a + 1, c = a + res, d = c + 1;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = c;
                    triangles[t++] = b; triangles[t++] = d; triangles[t++] = c;
                }
            }

            if (_mesh == null) _mesh = new Mesh { name = "LossLandscape" };
            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            surface.sharedMesh = _mesh;

            BuildRamp();
            surfaceRenderer.material.mainTexture = _ramp;
            Vector2 start = station.StartWeights;
            _target = PlaceMarble(start.x, start.y, out double loss);
            marble.localPosition = _target;
            SetLoss(loss);
        }

        private void BuildRamp()
        {
            if (_ramp != null) return;
            const int width = 256;
            _ramp = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float period = theme.contourEvery * theme.heightPerLoss / theme.maxHeight;
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                Color c = Color.Lerp(theme.cyan, theme.amber, Mathf.Clamp01(u * 1.15f));
                float band = Mathf.Repeat(u, period) / period;
                float line = band < 0.1f ? 0.25f : 1f;
                _ramp.SetPixel(x, 0, new Color(c.r * line, c.g * line, c.b * line, 1f));
            }

            _ramp.Apply(false);
        }

        private void OnStep(TrainingFrame frame)
        {
            if (frame.Step == 1)
            {
                trail.positionCount = 0;
                _trailCount = 0;
            }

            _target = PlaceMarble(frame.W1, frame.W2, out double loss);
            SetLoss(loss);
            trail.positionCount = ++_trailCount;
            trail.SetPosition(_trailCount - 1, _target + Vector3.up * 0.02f);

            double slope = System.Math.Sqrt(frame.Gradient1 * frame.Gradient1 + frame.Gradient2 * frame.Gradient2);
            if (slope > 1e-4)
            {
                float span = theme.landscapeSize / (station.LandscapeMax - station.LandscapeMin);
                Vector3 down = new Vector3(-(float)frame.Gradient1, 0f, -(float)frame.Gradient2).normalized;
                float length = Mathf.Min(0.9f, (float)slope * 0.25f * span);
                slopeArrow.enabled = true;
                slopeArrow.SetPosition(0, _target + Vector3.up * 0.05f);
                slopeArrow.SetPosition(1, _target + Vector3.up * 0.05f + down * length);
            }
            else
            {
                slopeArrow.enabled = false;
            }
        }

        private Vector3 PlaceMarble(double w1, double w2, out double loss)
        {
            float min = station.LandscapeMin;
            float max = station.LandscapeMax;
            float fx = Mathf.Clamp01((float)((w1 - min) / (max - min)));
            float fz = Mathf.Clamp01((float)((w2 - min) / (max - min)));
            loss = Sample(fx, fz);
            float h = Mathf.Min((float)loss * theme.heightPerLoss, theme.maxHeight);
            return new Vector3((fx - 0.5f) * theme.landscapeSize, h + theme.marbleRadius, (fz - 0.5f) * theme.landscapeSize);
        }

        private double Sample(float fx, float fz)
        {
            double[,] grid = station.LossGrid;
            int res = grid.GetLength(0);
            float gx = fx * (res - 1);
            float gz = fz * (res - 1);
            int i = Mathf.Min(res - 2, Mathf.FloorToInt(gx));
            int j = Mathf.Min(res - 2, Mathf.FloorToInt(gz));
            float tx = gx - i;
            float tz = gz - j;
            double a = grid[i, j] * (1 - tx) + grid[i + 1, j] * tx;
            double b = grid[i, j + 1] * (1 - tx) + grid[i + 1, j + 1] * tx;
            return a * (1 - tz) + b * tz;
        }

        private void SetLoss(double loss)
        {
            if (lossLabel != null) lossLabel.text = "error " + loss.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
