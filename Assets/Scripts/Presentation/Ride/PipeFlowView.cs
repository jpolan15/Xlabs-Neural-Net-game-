using System.Globalization;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// One sensor-to-core pipe. Thickness shows how big the weight is, color shows its sign (cyan adds to the core,
    /// amber drains it), the label shows the number, and pulses travel along it when the sensor sees something.
    /// </summary>
    public sealed class PipeFlowView : MonoBehaviour
    {
        [SerializeField] private StationController station;
        [SerializeField] private RideTheme theme;
        [SerializeField] private string channelId = RideControlIds.Rock;
        [SerializeField] private LineRenderer line;
        [SerializeField] private Transform from;
        [SerializeField] private Transform to;
        [SerializeField] private Vector3 bend = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private Material positiveMaterial;
        [SerializeField] private Material negativeMaterial;
        [SerializeField] private Material zeroMaterial;
        [SerializeField] private Renderer[] pulseRenderers = new Renderer[0];
        [SerializeField] private Material positivePulse;
        [SerializeField] private Material negativePulse;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Transform labelAnchor;

        private const int Segments = 16;
        private RideControlChannel _channel;
        private float[] _age;

        private void OnEnable()
        {
            _age = new float[pulseRenderers.Length];
            for (int i = 0; i < _age.Length; i++)
            {
                _age[i] = -1f;
                pulseRenderers[i].gameObject.SetActive(false);
            }

            if (station == null || station.Controls == null) return;
            _channel = station.Controls.Get(channelId);
            if (_channel == null) return;
            _channel.Changed += OnChanged;
            BuildCurve();
            Refresh();
        }

        private void OnDisable()
        {
            if (_channel != null) _channel.Changed -= OnChanged;
            _channel = null;
        }

        /// <summary>Sends one pulse down the pipe. Size follows the weighted signal.</summary>
        public void Pulse(float input)
        {
            if (_channel == null) return;
            for (int i = 0; i < _age.Length; i++)
            {
                if (_age[i] >= 0f) continue;
                _age[i] = 0f;
                float size = 0.05f + 0.05f * Mathf.Min(2f, Mathf.Abs(_channel.Value * input));
                pulseRenderers[i].transform.localScale = Vector3.one * size;
                pulseRenderers[i].sharedMaterial = _channel.Value < 0f ? negativePulse : positivePulse;
                pulseRenderers[i].gameObject.SetActive(true);
                return;
            }
        }

        private void Update()
        {
            if (_age == null) return;
            for (int i = 0; i < _age.Length; i++)
            {
                if (_age[i] < 0f) continue;
                _age[i] += Time.deltaTime / theme.pulseTravelSeconds;
                if (_age[i] >= 1f)
                {
                    _age[i] = -1f;
                    pulseRenderers[i].gameObject.SetActive(false);
                    continue;
                }

                pulseRenderers[i].transform.position = Curve(_age[i]);
            }
        }

        private void OnChanged(RideControlChannel changed, bool byUser) => Refresh();

        private void Refresh()
        {
            float w = _channel.Value;
            float width = theme.pipeHairline + theme.pipeWidthPerWeight * Mathf.Abs(w);
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = w > 0.001f ? positiveMaterial : w < -0.001f ? negativeMaterial : zeroMaterial;
            if (label != null)
            {
                label.text = "×" + w.ToString("0.0", CultureInfo.InvariantCulture);
                label.color = w < 0f ? theme.amber : theme.cyan;
            }
        }

        private void BuildCurve()
        {
            line.useWorldSpace = true;
            line.positionCount = Segments + 1;
            for (int i = 0; i <= Segments; i++) line.SetPosition(i, Curve(i / (float)Segments));
            if (label != null && labelAnchor != null) label.transform.position = labelAnchor.position;
        }

        private Vector3 Curve(float t)
        {
            Vector3 a = from.position;
            Vector3 c = to.position;
            Vector3 b = (a + c) * 0.5f + transform.TransformVector(bend);
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
