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
        [SerializeField] private Color friendlyAllyColor = new Color(0.1f, 0.95f, 0.55f, 1.0f);   // Mint Emerald
        [SerializeField] private Color biohazardColor = new Color(0.95f, 0.75f, 0.05f, 1.0f);      // Toxic Amber
        [SerializeField] private Color radiationColor = new Color(1.0f, 0.25f, 0.15f, 1.0f);       // Alert Red
        [SerializeField] private Color dualBreachColor = new Color(0.85f, 0.1f, 0.95f, 1.0f);      // Overload Magenta
        [SerializeField] private Color harmonizedEmerald = new Color(0.05f, 1.0f, 0.45f, 1.0f);
        [SerializeField] private Color errorRed = new Color(1.0f, 0.1f, 0.1f, 1.0f);

        private float _impactTimer;
        private Camera _mainCam;
        private Vector3 _currentBasePos;
        private bool _isExploding = false;

        public DataTargetReceptor Receptor => receptor;

        private void Awake()
        {
            if (receptor == null) receptor = GetComponent<DataTargetReceptor>();
            _mainCam = Camera.main;

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
            }
        }

        private void Start()
        {
            SetupTargetTheme();
            UpdateVisualState();
            UpdateApproachPosition();
        }

        private void Update()
        {
            // Floating & bobbing kinetic animation
            if (floatingTargetBody != null && !_isExploding)
            {
                int idx = receptor != null ? receptor.CaseIndex : 0;
                float bobFreq = (idx == 0) ? 2.0f : (idx == 3 ? 4.5f : 3.0f);
                float bobAmp = (idx == 0) ? 0.04f : 0.025f;

                float bob = Mathf.Sin(Time.time * bobFreq + (idx * 1.5f)) * bobAmp;
                floatingTargetBody.localPosition = _currentBasePos + new Vector3(0, bob, 0);

                float rotSpeed = (idx == 0) ? 18.0f : (idx == 3 ? 75.0f : 35.0f);
                floatingTargetBody.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.Self);
            }

            if (_impactTimer > 0f)
            {
                _impactTimer -= Time.deltaTime;
                if (_impactTimer <= 0f)
                {
                    _isExploding = false;
                    if (floatingTargetBody != null) floatingTargetBody.gameObject.SetActive(true);
                }
            }
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

            if (floatingTargetBody != null)
            {
                floatingTargetBody.gameObject.SetActive(false);
            }

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
                coreRenderer.sharedMaterial.color = themeColor;
                coreRenderer.sharedMaterial.SetColor("_EmissionColor", themeColor * 1.2f);
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
                0 => friendlyAllyColor,
                1 => biohazardColor,
                2 => radiationColor,
                3 => dualBreachColor,
                _ => friendlyAllyColor
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

            if (receptor.LastDiagnostic != null)
            {
                if (receptor.IsHarmonized)
                {
                    activeColor = harmonizedEmerald;
                    lightIntensity = 3.5f;
                }
                else
                {
                    activeColor = errorRed;
                    lightIntensity = 2.8f;
                }
            }

            if (_impactTimer > 0f)
            {
                activeColor = Color.white;
                lightIntensity *= 2.2f;
            }

            if (coreRenderer != null && coreRenderer.material != null)
            {
                coreRenderer.material.color = activeColor;
                coreRenderer.material.SetColor("_EmissionColor", activeColor * (receptor.IsHarmonized ? 3.0f : 1.6f));
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

        private void OnGUI()
        {
            if (receptor == null || _isExploding) return;
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 1.3f;
            Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0.5f || screenPos.z > 25.0f) return;

            float scale = Mathf.Clamp(1.0f - (screenPos.z / 32.0f), 0.65f, 1.0f);
            float w = 260f * scale;
            float h = 68f * scale;
            float x = screenPos.x - (w * 0.5f);
            float y = Screen.height - screenPos.y - (h * 0.5f);

            Color statusCol = receptor.IsHarmonized
                ? harmonizedEmerald
                : (receptor.LastDiagnostic == null ? GetBaseTargetColor() : errorRed);

            GUIStyle cardStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(11 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.color = statusCol;
            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUI.color = Color.white;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(10 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(x, y + 2, w, 18 * scale), $"TARGET {receptor.CaseIndex + 1}: {receptor.TargetTitle.ToUpper()}", titleStyle);

            GUIStyle roleStyle = new GUIStyle(titleStyle)
            {
                fontSize = Mathf.RoundToInt(9 * scale),
                normal = { textColor = statusCol }
            };

            string statusText = receptor.IsHarmonized
                ? "[SECURED / PASS]"
                : (receptor.LastDiagnostic == null ? $"APPROACHING [{(1f - receptor.ApproachProgress) * 100f:F0}m]" : "[BREACH / ERROR]");

            string telemetryStr = $"Inputs: (X1={receptor.InputX1:F0}, X2={receptor.InputX2:F0})  |  {statusText}";
            GUI.Label(new Rect(x, y + (20 * scale), w, 16 * scale), telemetryStr, roleStyle);

            GUIStyle subStyle = new GUIStyle(titleStyle)
            {
                fontSize = Mathf.RoundToInt(8.5f * scale),
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.85f, 0.9f, 1f, 0.9f) }
            };

            string ruleStr = $"Required Action: {receptor.ThreatRole}";
            GUI.Label(new Rect(x, y + (38 * scale), w, 16 * scale), ruleStr, subStyle);

            if (receptor.LastDiagnostic != null && !receptor.IsHarmonized)
            {
                string outcomeText = (receptor.LastActualOutput >= 0.5) ? "FIRED PLASMA" : "HOLD / MISSED";
                string errStr = $"Sentry Decision: {outcomeText} (y={receptor.LastActualOutput:F0}) -> MISMATCH!";
                GUIStyle alertStyle = new GUIStyle(subStyle)
                {
                    normal = { textColor = errorRed }
                };
                GUI.Label(new Rect(x, y + (52 * scale), w, 16 * scale), errStr, alertStyle);
            }
        }
    }
}
