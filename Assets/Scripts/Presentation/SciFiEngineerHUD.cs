using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Tactical Sentry Defense HUD for Level 1 — The Sentry Intercept Crisis.
    /// Displays live emergency purge countdown clock, containment shield integrity bar,
    /// 4 target scenario badges with approach distance, calibration readouts, and handles screen shake on damage.
    /// </summary>
    public class SciFiEngineerHUD : MonoBehaviour
    {
        [Header("State Listeners")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private ChamberOnboardingController onboardingController;

        [Header("Display Settings")]
        [SerializeField] private bool showHUD = false;
        [SerializeField] private bool showNextActionStrip = true;
        [SerializeField] private KeyCode toggleHUDKey = KeyCode.H;

        [Header("Reticle Settings")]
        [SerializeField] private bool showReticle = true;
        [SerializeField] private float raycastDistance = 25.0f;
        [SerializeField] private LayerMask interactableMask = ~0;

        // Visual Palette
        private readonly Color _panelBg = new Color(0.02f, 0.05f, 0.09f, 0.94f);
        private readonly Color _panelBorder = new Color(0.0f, 0.85f, 1.0f, 0.85f);
        private readonly Color _neonCyan = new Color(0.0f, 0.95f, 1.0f, 1.0f);
        private readonly Color _neonEmerald = new Color(0.0f, 1.0f, 0.45f, 1.0f);
        private readonly Color _neonAmber = new Color(1.0f, 0.72f, 0.0f, 1.0f);
        private readonly Color _neonRed = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        private readonly Color _dimText = new Color(0.7f, 0.82f, 0.92f, 1.0f);
        private readonly Color _badgePassBg = new Color(0.0f, 0.5f, 0.25f, 0.65f);
        private readonly Color _badgeFailBg = new Color(0.55f, 0.1f, 0.1f, 0.65f);

        // Screen Shake & Damage Flash
        private float _shakeIntensity = 0.0f;
        private float _damageFlashAlpha = 0.0f;
        private Vector3 _originalCamPos;

        // State caching
        private string _hoverTooltip = "";
        private bool _isHoveringInteractable = false;
        private Camera _mainCamera;
        private Rect _hudRect;
        private Texture2D _whiteTexture;

        public bool ShowHUD
        {
            get => showHUD;
            set => showHUD = value;
        }

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (onboardingController == null) onboardingController = FindAnyObjectByType<ChamberOnboardingController>();
            _mainCamera = Camera.main;

            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnShieldDamaged += HandleShieldDamaged;
                chamberController.OnEmergencyPurge += HandleEmergencyPurge;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnShieldDamaged -= HandleShieldDamaged;
                chamberController.OnEmergencyPurge -= HandleEmergencyPurge;
            }
        }

        private void HandleShieldDamaged(float damage)
        {
            _shakeIntensity = Mathf.Min(0.35f, damage * 0.015f);
            _damageFlashAlpha = 0.45f;
        }

        private void HandleEmergencyPurge()
        {
            _shakeIntensity = 0.5f;
            _damageFlashAlpha = 0.75f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleHUDKey))
            {
                showHUD = !showHUD;
            }

            // Decay screen shake & damage flash
            if (_shakeIntensity > 0.001f)
            {
                _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f, Time.deltaTime * 6.0f);
            }
            if (_damageFlashAlpha > 0.001f)
            {
                _damageFlashAlpha = Mathf.Lerp(_damageFlashAlpha, 0f, Time.deltaTime * 4.0f);
            }

            UpdateRaycastTooltip();
        }

        private void UpdateRaycastTooltip()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactableMask))
            {
                var targetRec = hit.collider.GetComponentInParent<DataTargetReceptor>();
                if (targetRec != null)
                {
                    _isHoveringInteractable = true;
                    string stateStr = targetRec.IsHarmonized ? "SECURED ✓" : "BREACH / UNSTABLE ✗";
                    _hoverTooltip = $"{targetRec.TargetTitle.ToUpper()} [{stateStr}] — Click to fire single-target diagnostic pulse";
                    return;
                }

                string objName = hit.collider.gameObject.name.ToLower();
                _isHoveringInteractable = true;

                if (objName.Contains("crystal") || objName.Contains("socket") || objName.Contains("prism"))
                {
                    string act = neuralState != null ? neuralState.Activation.ToString() : "Step";
                    _hoverTooltip = $"Activation crystal [{act}] — click or [Tab] to swap crystal type";
                }
                else if (objName.Contains("w1") || objName.Contains("slider_w1"))
                {
                    _hoverTooltip = "Sensitivity dial — scroll up to increase, scroll down to decrease";
                }
                else if (objName.Contains("w2") || objName.Contains("slider_w2"))
                {
                    _hoverTooltip = "Sensitivity dial — scroll up to increase, scroll down to decrease";
                }
                else if (objName.Contains("bias") || objName.Contains("valve"))
                {
                    double b = neuralState != null ? neuralState.Bias : 0.0;
                    _hoverTooltip = $"Noise threshold (auto-tuned: {b:+0.0;-0.0;0.0}) — adjusts automatically with the dial";
                }
                else if (objName.Contains("cable_1") || objName.Contains("conduit_1"))
                {
                    bool c = neuralState != null && neuralState.Cable1Connected;
                    _hoverTooltip = c ? "Radiation sensor cable — connected ✓" : "Radiation sensor cable — click or [C] to plug in";
                }
                else if (objName.Contains("cable_2") || objName.Contains("conduit_2"))
                {
                    bool c = neuralState != null && neuralState.Cable2Connected;
                    _hoverTooltip = c ? "Bio-hazard sensor cable — connected ✓" : "Bio-hazard sensor cable — click or [V] to plug in";
                }
                else if (objName.Contains("lever"))
                {
                    _hoverTooltip = "Pull the lever (or press Space) to run the test across all 4 scenarios";
                }
                else if (objName.Contains("sentry") || objName.Contains("turret"))
                {
                    _hoverTooltip = "Defense sentry — you're calibrating its ability to tell friend from foe";
                }
            }
            else
            {
                _isHoveringInteractable = false;
                _hoverTooltip = "";
            }
        }

        public bool IsPointerOverHUD(Vector2 mousePosition)
        {
            if (!showHUD) return false;
            return _hudRect.Contains(mousePosition);
        }

        private void OnGUI()
        {
            // Red Damage Flash Overlay
            if (_damageFlashAlpha > 0.01f)
            {
                DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, 0.05f, 0.05f, _damageFlashAlpha));
            }

            DrawReticle();

            // Only show purge bar when timer is actually running (not sandbox mode)
            if (chamberController != null && chamberController.PurgeTimer < chamberController.MaxPurgeTime - 1f)
            {
                DrawEmergencyPurgeStatusBar();
            }

            if (showNextActionStrip)
            {
                DrawNextActionStrip();
            }

            if (!showHUD || neuralState == null) return;

            DrawTacticalHUD();
        }

        private void DrawEmergencyPurgeStatusBar()
        {
            float barW = 540f;
            float barH = 32f;
            Rect topRect = new Rect((Screen.width - barW) * 0.5f, 10f, barW, barH);

            float pTime = chamberController != null ? chamberController.PurgeTimer : 90f;
            float sHealth = chamberController != null ? chamberController.ShieldIntegrity : 100f;
            int minutes = Mathf.FloorToInt(pTime / 60f);
            int seconds = Mathf.FloorToInt(pTime % 60f);

            bool isUrgent = pTime < 30f;
            Color timerCol = isUrgent ? (Mathf.Sin(Time.time * 8.0f) > 0 ? _neonRed : _neonAmber) : _neonCyan;
            Color shieldCol = sHealth > 50f ? _neonEmerald : (sHealth > 25f ? _neonAmber : _neonRed);

            DrawRect(topRect, new Color(0.02f, 0.05f, 0.10f, 0.92f));
            DrawBorder(topRect, isUrgent ? _neonRed : _neonCyan, 2);

            GUIStyle timeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = timerCol }
            };
            GUI.Label(new Rect(topRect.x + 12, topRect.y + 6, 200, 20), $"⏱ PURGE: {minutes:00}:{seconds:00}", timeStyle);

            GUIStyle shieldStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = shieldCol }
            };
            GUI.Label(new Rect(topRect.x + topRect.width - 240, topRect.y + 6, 226, 20), $"🛡 SHIELD: {sHealth:F0}%", shieldStyle);
        }

        private void DrawNextActionStrip()
        {
            float stripW = 680f;
            float stripH = 38f;
            Rect stripRect = new Rect((Screen.width - stripW) * 0.5f, Screen.height - stripH - 16f, stripW, stripH);

            string prompt = onboardingController != null
                ? onboardingController.GetCurrentStepPrompt()
                : "Calibrate the sentry to protect against all four scenarios.";

            bool isComplete = onboardingController != null && onboardingController.CurrentStep == OnboardingStep.Completed;
            Color accentColor = isComplete ? _neonEmerald : _neonCyan;

            DrawRect(stripRect, new Color(0.02f, 0.05f, 0.10f, 0.90f));
            DrawBorder(stripRect, accentColor, 2);

            GUIStyle stripStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = accentColor }
            };

            GUI.Label(stripRect, $"  {prompt}  |  [H] for details", stripStyle);
        }

        private void DrawReticle()
        {
            if (!showReticle) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            if (_shakeIntensity > 0.001f)
            {
                cx += UnityEngine.Random.Range(-12f, 12f) * _shakeIntensity;
                cy += UnityEngine.Random.Range(-12f, 12f) * _shakeIntensity;
            }

            Color reticleColor = _isHoveringInteractable ? _neonCyan : new Color(1f, 1f, 1f, 0.45f);

            // Center dot
            DrawRect(new Rect(cx - 2, cy - 2, 4, 4), reticleColor);

            // Crosshair ticks
            float offset = _isHoveringInteractable ? 14f : 8f;
            float tickLen = 6f;
            DrawRect(new Rect(cx - offset - tickLen, cy - 1, tickLen, 2), reticleColor);
            DrawRect(new Rect(cx + offset, cy - 1, tickLen, 2), reticleColor);
            DrawRect(new Rect(cx - 1, cy - offset - tickLen, 2, tickLen), reticleColor);
            DrawRect(new Rect(cx - 1, cy + offset, 2, tickLen), reticleColor);

            if (!string.IsNullOrEmpty(_hoverTooltip))
            {
                GUIStyle tipStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = _neonCyan }
                };

                Rect tipRect = new Rect(cx - 300, cy + 28, 600, 24);
                DrawRect(new Rect(tipRect.x + 20, tipRect.y, tipRect.width - 40, tipRect.height), new Color(0.02f, 0.05f, 0.1f, 0.88f));
                DrawBorder(new Rect(tipRect.x + 20, tipRect.y, tipRect.width - 40, tipRect.height), _neonCyan * 0.5f, 1);
                GUI.Label(tipRect, _hoverTooltip, tipStyle);
            }
        }

        private void DrawTacticalHUD()
        {
            float hudW = 620f;
            float hudH = 162f;
            _hudRect = new Rect((Screen.width - hudW) * 0.5f, 48f, hudW, hudH);

            DrawRect(_hudRect, _panelBg);
            DrawBorder(_hudRect, _panelBorder, 2);

            float y = _hudRect.y + 8;

            // Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _neonCyan }
            };
            GUI.Label(new Rect(_hudRect.x + 14, y, 460, 20), "🛡 SENTRY INTERCEPT STATUS // SECTOR 01 CONTAINMENT GRID", headerStyle);

            GUIStyle subHeader = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10,
                normal = { textColor = _dimText }
            };
            GUI.Label(new Rect(_hudRect.x + hudW - 130, y, 116, 20), "[H] Hide HUD", subHeader);
            y += 20;

            // Objective
            GUIStyle objStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(_hudRect.x + 14, y, hudW - 28, 18), "Objective: Calibrate Sentry Perceptron to Spare Friendly Drone (Y=0) and Intercept All Hazards (Y=1).", objStyle);
            y += 22;

            // 4 Target Badges — friendly names, no math notation
            bool hasEvaluated = chamberController != null && chamberController.LastEvaluation != null;
            PuzzleEvaluation lastEval = hasEvaluated ? chamberController.LastEvaluation : null;
            int passedCount = lastEval != null ? lastEval.PassedCases : 0;
            bool allPassed = lastEval != null && lastEval.Passed;

            float badgeW = 110f;
            float badgeH = 28f;
            float startX = _hudRect.x + 14;

            string[] targetLabels = { "Safe Room", "Bio Leak", "Rad Flare", "Dual Breach" };

            for (int i = 0; i < 4; i++)
            {
                bool pass = false;
                if (hasEvaluated && lastEval.Diagnostics != null && i < lastEval.Diagnostics.Count)
                {
                    pass = lastEval.Diagnostics[i].IsCorrect && lastEval.ActivationMatches;
                }

                Rect badgeRect = new Rect(startX + (i * (badgeW + 6)), y, badgeW, badgeH);

                Color bgCol     = !hasEvaluated ? new Color(0.05f, 0.15f, 0.25f, 0.55f) : (pass ? _badgePassBg : _badgeFailBg);
                Color borderCol = !hasEvaluated ? _neonCyan * 0.6f : (pass ? _neonEmerald : _neonRed);
                Color textCol   = !hasEvaluated ? _neonCyan : (pass ? _neonEmerald : _neonRed);

                DrawRect(badgeRect, bgCol);
                DrawBorder(badgeRect, borderCol, 1);

                GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = textCol }
                };
                string statusText = !hasEvaluated ? "ready" : (pass ? "✓ correct" : "✗ wrong");
                GUI.Label(badgeRect, $"{targetLabels[i]}\n{statusText}", badgeStyle);
            }

            // Summary Badge
            Rect summaryRect = new Rect(startX + (4 * (badgeW + 6)) + 4, y, 154, badgeH);
            Color sumBg = !hasEvaluated ? new Color(0.05f, 0.15f, 0.25f, 0.8f) : (allPassed ? new Color(0.0f, 0.4f, 0.2f, 0.8f) : new Color(0.35f, 0.15f, 0.0f, 0.8f));
            Color sumBorder = !hasEvaluated ? _neonCyan : (allPassed ? _neonEmerald : _neonAmber);
            Color sumTextCol = !hasEvaluated ? _neonCyan : (allPassed ? _neonEmerald : _neonAmber);

            DrawRect(summaryRect, sumBg);
            DrawBorder(summaryRect, sumBorder, 1);

            GUIStyle sumStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = sumTextCol }
            };
            int pulseCount = chamberController != null ? chamberController.PulseCount : 0;
            string sumText = !hasEvaluated ? $"READY (TRIALS: {pulseCount})" : (allPassed ? $"ALL CLEAR! (TRIALS: {pulseCount}) ✓" : $"{passedCount}/4 (TRIALS: {pulseCount})");
            GUI.Label(summaryRect, sumText, sumStyle);
            y += 32;

            // Divider line
            DrawRect(new Rect(_hudRect.x + 14, y, hudW - 28, 1), new Color(0.0f, 0.85f, 1.0f, 0.25f));
            y += 6;

            // Live Calibration Readout — simplified labels
            double w1 = neuralState.Weight1;
            double b  = neuralState.Bias;
            string actStr = neuralState.Activation == ActivationType.Step ? "Step (binary)" : neuralState.Activation.ToString();

            GUIStyle readoutStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _neonCyan }
            };
            GUI.Label(new Rect(_hudRect.x + 14, y, 360, 22), $"Sensitivity: {w1:0.0} | Auto-threshold: {b:+0.0;-0.0;0.0} | Crystal: {actStr}", readoutStyle);

            // Primary Action Button
            GUI.backgroundColor = allPassed ? _neonEmerald : _neonCyan;
            Rect btnRect = new Rect(_hudRect.x + hudW - 214, y - 2, 200, 26);
            if (GUI.Button(btnRect, "⚡ Run Test  [Space]"))
            {
                if (chamberController != null)
                {
                    chamberController.TriggerForwardPass();
                }
            }
            GUI.backgroundColor = Color.white;
            y += 24;

            // Shortcuts
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 9,
                normal = { textColor = _dimText }
            };
            GUI.Label(new Rect(_hudRect.x + 14, y, hudW - 28, 16), "Controls: Scroll on dial | [C][V] cables | [Q][W] sensitivity | [Space] run test | [R] reset", hintStyle);
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = prev;
        }

        private void DrawBorder(Rect rect, Color color, int width)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }
    }
}
