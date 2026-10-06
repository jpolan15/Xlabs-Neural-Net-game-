using System.Globalization;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The neuron core's energy tank. The bar grows up from zero when the sum is positive (cyan) and hangs below
    /// zero when a backwards pipe drains it (amber). A white line marks the trigger; the core fires when the bar
    /// reaches it. It shows numbers that Gameplay already evaluated; it decides nothing.
    /// </summary>
    public sealed class CoreTankView : MonoBehaviour
    {
        [SerializeField] private StationController station;
        [SerializeField] private RideTheme theme;
        [SerializeField] private Transform fillBar;
        [SerializeField] private Renderer fillRenderer;
        [SerializeField] private Material positiveMaterial;
        [SerializeField] private Material negativeMaterial;
        [SerializeField] private Transform triggerLine;
        [SerializeField] private TMP_Text sumLabel;
        [SerializeField] private TMP_Text triggerLabel;
        [SerializeField] private LineRenderer beam;
        [SerializeField] private Transform beamOrigin;
        [SerializeField] private GameObject firedGlow;
        [SerializeField] private float beamSeconds = 0.35f;

        private RideControlChannel _trigger;
        private float _targetSum;
        private float _shownSum;
        private float _beamUntil;
        private float _glowUntil;

        private void OnEnable()
        {
            _targetSum = 0f;
            _shownSum = 0f;
            beam.enabled = false;
            if (firedGlow != null) firedGlow.SetActive(false);
            if (station == null || station.Controls == null) return;
            _trigger = station.Controls.Get(RideControlIds.Trigger);
            if (_trigger == null) return;
            _trigger.Changed += OnTrigger;
            RefreshTrigger();
            Apply();
            SetSumLabel(0f);
        }

        private void OnDisable()
        {
            if (_trigger != null) _trigger.Changed -= OnTrigger;
            _trigger = null;
        }

        /// <summary>The bar will move to this sum.</summary>
        public void SetSum(double sum)
        {
            _targetSum = (float)sum;
            SetSumLabel(_targetSum);
        }

        /// <summary>Flashes the core. When it fired, also draws a beam to <paramref name="target"/>.</summary>
        public void Fire(bool fired, Vector3 target)
        {
            if (!fired) return;
            beam.enabled = true;
            beam.SetPosition(0, beamOrigin.position);
            beam.SetPosition(1, target);
            _beamUntil = Time.time + beamSeconds;
            if (firedGlow != null) firedGlow.SetActive(true);
            _glowUntil = Time.time + beamSeconds * 1.6f;
        }

        private void Update()
        {
            _shownSum = Mathf.Lerp(_shownSum, _targetSum, 1f - Mathf.Exp(-theme.tankFollow * Time.deltaTime));
            Apply();
            if (beam.enabled && Time.time > _beamUntil) beam.enabled = false;
            if (firedGlow != null && firedGlow.activeSelf && Time.time > _glowUntil) firedGlow.SetActive(false);
        }

        private void Apply()
        {
            float clamped = Mathf.Clamp(_shownSum, -theme.tankMaxSum, theme.tankMaxSum);
            float top = clamped * theme.tankUnitsPerSum;
            float length = Mathf.Max(0.012f, Mathf.Abs(top));
            Vector3 p = fillBar.localPosition;
            p.y = top * 0.5f;
            fillBar.localPosition = p;
            Vector3 s = fillBar.localScale;
            s.y = length;
            fillBar.localScale = s;
            fillRenderer.sharedMaterial = clamped < -0.001f ? negativeMaterial : positiveMaterial;
        }

        private void OnTrigger(RideControlChannel changed, bool byUser) => RefreshTrigger();

        private void RefreshTrigger()
        {
            float level = Mathf.Clamp(_trigger.Value, -theme.tankMaxSum, theme.tankMaxSum) * theme.tankUnitsPerSum;
            Vector3 p = triggerLine.localPosition;
            p.y = level;
            triggerLine.localPosition = p;
            if (triggerLabel != null)
            {
                triggerLabel.text = "fire at " + _trigger.Value.ToString("0.0", CultureInfo.InvariantCulture);
                Vector3 lp = triggerLabel.transform.localPosition;
                lp.y = level;
                triggerLabel.transform.localPosition = lp;
            }
        }

        private void SetSumLabel(float sum)
        {
            if (sumLabel != null) sumLabel.text = "sum " + sum.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
