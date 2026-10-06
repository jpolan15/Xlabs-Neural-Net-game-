using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Tactical Visual Observer for a physical DataTargetReceptor in Level 1.
    /// Manages physical approach motion along defense corridors, mid-flight plasma vaporization explosions,
    /// safe docking halos for allies, and concussive perimeter shield breaches.
    /// Strictly Presentation layer: observes state and never evaluates or decides correctness.
    /// </summary>
    public class DataTargetVisual : MonoBehaviour
    {
        [Header("Receptor Reference")]
        [SerializeField] private DataTargetReceptor receptor;

        [Header("Target 3D Visual Objects")]
        [SerializeField] private Transform floatingTargetBody;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private Renderer subRenderer;
        [SerializeField] private Light auraLight;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private ParticleSystem hazardAmbientParticles;

        [Header("Corridor Movement Vectors")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1.2f, 7.0f);
        [SerializeField] private Vector3 perimeterPosition = new Vector3(0, 1.2f, 2.5f);

        [Header("Tactical Colors")]
        [SerializeField] private Color errorRed = new Color(1.0f, 0.15f, 0.15f, 1.0f);

        private float _impactTimer;
        private Vector3 _currentBasePos;
        private bool _isExploding = false;
        private TextMesh _callout;
        private Camera _billboardCamera;

        public DataTargetReceptor Receptor => receptor;

        private void Awake()
        {
            if (receptor == null) receptor = GetComponent<DataTargetReceptor>();

            if (floatingTargetBody != null)
            {
                _currentBasePos = floatingTargetBody.localPosition;
            }
        }

        private void OnEnable()
        {
            if (receptor != null)
            {
                receptor.OnStateUpdated += HandleStateUpdated;
                receptor.OnPulseImpacted += HandlePulseImpacted;
                receptor.OnApproachUpdated += HandleApproachUpdated;
                receptor.OnVaporized += HandleVaporized;
                receptor.OnBreached += HandleBreached;
                receptor.OnDocked += HandleDocked;
                receptor.OnFlightChanged += HandleFlightChanged;
            }
        }

        private void OnDisable()
        {
            if (receptor != null)
            {
                receptor.OnStateUpdated -= HandleStateUpdated;
                receptor.OnPulseImpacted -= HandlePulseImpacted;
                receptor.OnApproachUpdated -= HandleApproachUpdated;
                receptor.OnVaporized -= HandleVaporized;
                receptor.OnBreached -= HandleBreached;
                receptor.OnDocked -= HandleDocked;
                receptor.OnFlightChanged -= HandleFlightChanged;
            }
        }

        private void Start()
        {
            EnsureCallout();
            SetupTargetTheme();
            UpdateVisualState();
            UpdateApproachPosition();
            ApplyBodyVisibility();
        }

        private void Update()
        {
            // Floating & bobbing kinetic animation
            if (floatingTargetBody != null && !_isExploding)
            {
                int idx = receptor != null ? receptor.CaseIndex : 0;
                float bobFreq = (idx == 0) ? 1.6f : (idx == 3 ? 2.4f : 1.8f);
                float bobAmp = (idx == 0) ? 0.08f : 0.16f;

                float bob = Mathf.Sin(Time.time * bobFreq + (idx * 1.5f)) * bobAmp;
                floatingTargetBody.localPosition = _currentBasePos + new Vector3(0, bob, 0);

                float rotSpeed = (idx == 0) ? 18.0f : (idx == 3 ? 75.0f : 35.0f);
                floatingTargetBody.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.Self);
            }

            if (_callout != null && _callout.gameObject.activeSelf)
            {
                if (_billboardCamera == null) _billboardCamera = Camera.main;
                if (_billboardCamera != null)
                {
                    Vector3 toHead = _billboardCamera.transform.position - _callout.transform.position;
                    if (toHead.sqrMagnitude > 0.01f)
                    {
                        _callout.transform.rotation = Quaternion.LookRotation(toHead, Vector3.up);
                    }
                }
            }

            if (_impactTimer > 0f)
            {
                _impactTimer -= Time.deltaTime;
                if (_impactTimer <= 0f)
                {
                    _isExploding = false;
                    ApplyBodyVisibility();
                }
            }
        }

        private void HandleFlightChanged(DataTargetReceptor r)
        {
            if (r != null && r.IsInFlight)
            {
                _isExploding = false;
                _impactTimer = 0f;
            }
            ApplyBodyVisibility();
        }

        private void ApplyBodyVisibility()
        {
            bool show = receptor != null && receptor.IsInFlight && !receptor.IsVaporized && !_isExploding;
            if (floatingTargetBody != null) floatingTargetBody.gameObject.SetActive(show);
            if (auraLight != null) auraLight.enabled = show;
            if (_callout != null) _callout.gameObject.SetActive(show);
        }

        private void EnsureCallout()
        {
            // Giant window words faced the wrong way. Icon tags arrive in a later package.
        }

        public void SetCorridorWaypoints(Vector3 spawn, Vector3 perimeter)
        {
            spawnPosition = spawn;
            perimeterPosition = perimeter;
            UpdateApproachPosition();
        }

        private void HandleApproachUpdated(DataTargetReceptor r)
        {
            UpdateApproachPosition();
        }

        private void UpdateApproachPosition()
        {
            if (receptor == null) return;

            float t = receptor.ApproachProgress;
            // Interpolate world position along the approach corridor
            Vector3 targetWorldPos = Vector3.Lerp(spawnPosition, perimeterPosition, t);
            transform.position = targetWorldPos;
        }

        private void HandleVaporized(DataTargetReceptor r)
        {
            _isExploding = true;
            _impactTimer = 0.65f;

            if (impactParticles != null)
            {
                impactParticles.Play();
            }

            ApplyBodyVisibility();
            UpdateVisualState();
        }

        private void HandleDocked(DataTargetReceptor r)
        {
            // Smoothly settle into docking position
            transform.position = perimeterPosition;
            UpdateVisualState();
        }

        private void HandleBreached(DataTargetReceptor r)
        {
            _impactTimer = 0.5f;
            if (impactParticles != null)
            {
                impactParticles.Play();
            }
            UpdateVisualState();
        }

        private void SetupTargetTheme()
        {
            if (receptor == null) return;

            Color themeColor = GetBaseTargetColor();
            if (coreRenderer != null && coreRenderer.sharedMaterial != null)
            {
                if (coreRenderer.sharedMaterial.mainTexture == null)
                {
                    coreRenderer.sharedMaterial.color = themeColor;
                }
                coreRenderer.sharedMaterial.SetColor("_EmissionColor", themeColor * 0.8f);
            }
            if (auraLight != null)
            {
                auraLight.color = themeColor;
                auraLight.intensity = 1.8f;
            }
        }

        public Color GetBaseTargetColor()
        {
            int idx = receptor != null ? receptor.CaseIndex : 0;
            return idx switch
            {
                0 => new Color(0.55f, 0.95f, 0.72f),
                1 => new Color(0.45f, 0.86f, 1f),
                2 => new Color(0.95f, 0.46f, 0.16f),
                3 => new Color(0.78f, 0.82f, 1f),
                _ => Color.white
            };
        }

        private void HandleStateUpdated(DataTargetReceptor r)
        {
            UpdateVisualState();
        }

        private void HandlePulseImpacted(DataTargetReceptor r)
        {
            _impactTimer = 0.45f;
            if (impactParticles != null)
            {
                impactParticles.Play();
            }
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (receptor == null) return;

            Color baseColor = GetBaseTargetColor();
            Color activeColor = baseColor;
            float lightIntensity = 1.8f;

            if (receptor.LastDiagnostic != null && !receptor.IsHarmonized)
            {
                activeColor = Color.Lerp(baseColor, errorRed, 0.35f);
                lightIntensity = 2.4f;
            }
            else if (receptor.IsHarmonized)
            {
                lightIntensity = 3.2f;
            }

            if (_impactTimer > 0f)
            {
                activeColor = Color.Lerp(baseColor, Color.white, 0.7f);
                lightIntensity *= 2.2f;
            }

            if (coreRenderer != null && coreRenderer.material != null)
            {
                if (coreRenderer.material.mainTexture != null)
                {
                    coreRenderer.material.color = Color.white;
                    coreRenderer.material.SetColor("_EmissionColor", activeColor * (receptor.IsHarmonized ? 1.4f : 0.35f));
                }
                else
                {
                    coreRenderer.material.color = activeColor;
                    coreRenderer.material.SetColor("_EmissionColor", activeColor * (receptor.IsHarmonized ? 3.0f : 1.6f));
                }
            }

            if (subRenderer != null && subRenderer.material != null)
            {
                subRenderer.material.color = activeColor;
                subRenderer.material.SetColor("_EmissionColor", activeColor * 1.5f);
            }

            if (auraLight != null)
            {
                auraLight.color = activeColor;
                auraLight.intensity = lightIntensity;
            }
        }

    }
}
