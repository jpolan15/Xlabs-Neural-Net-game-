using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Premium Astronaut Engineer Sci-Fi HUD for Level 1.
    /// Provides unambiguous interactive controls for Activation modes, Weights, Bias, and Conduits,
    /// complete with a center-screen reticle, dynamic aim tooltips, and real-time OR-gate diagnostic telemetry.
    /// Decouples parameter adjustment from pulse submission to eliminate accidental goal triggers.
    /// </summary>
    public class SciFiEngineerHUD : MonoBehaviour
    {
        [Header("State Listeners")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Display Settings")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private KeyCode toggleHUDKey = KeyCode.H;

        [Header("Reticle Settings")]
        [SerializeField] private bool showReticle = true;
        [SerializeField] private float raycastDistance = 25.0f;
        [SerializeField] private LayerMask interactableMask = ~0;

        // Colors
        private readonly Color _panelBg = new Color(0.04f, 0.07f, 0.12f, 0.88f);
        private readonly Color _panelBorder = new Color(0.0f, 0.8f, 1.0f, 0.9f);
        private readonly Color _neonCyan = new Color(0.0f, 0.95f, 1.0f, 1.0f);
        private readonly Color _neonEmerald = new Color(0.0f, 0.95f, 0.45f, 1.0f);
        private readonly Color _neonAmber = new Color(1.0f, 0.72f, 0.0f, 1.0f);
        private readonly Color _neonRed = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        private readonly Color _dimText = new Color(0.6f, 0.75f, 0.85f, 1.0f);

        // State caching
        private string _hoverTooltip = "";
        private bool _isHoveringInteractable = false;
        private Camera _mainCamera;
        private Rect _hudRect;
        private Texture2D _whiteTexture;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            _mainCamera = Camera.main;

            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleHUDKey))
            {
                showHUD = !showHUD;
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
                string objName = hit.collider.gameObject.name.ToLower();
                _isHoveringInteractable = true;

                if (objName.Contains("crystal") || objName.Contains("socket"))
                {
                    _hoverTooltip = "ACTIVATION CRYSTAL RECEPTACLE  [Tab / Click to Cycle]";
                }
                else if (objName.Contains("w1") || objName.Contains("regulator_w1"))
                {
                    _hoverTooltip = "WEIGHT 1 REGULATOR  [Click / Scroll to Tune]";
                }
                else if (objName.Contains("w2") || objName.Contains("regulator_w2"))
                {
                    _hoverTooltip = "WEIGHT 2 REGULATOR  [Click / Scroll to Tune]";
                }
                else if (objName.Contains("bias"))
                {
                    _hoverTooltip = "BIAS VOLTAGE REGULATOR  [Click / Scroll to Tune]";
                }
                else if (objName.Contains("cable") || objName.Contains("conduit"))
                {
                    _hoverTooltip = "NEURAL CONDUIT  [Click / C / V to Toggle]";
                }
                else if (objName.Contains("core") || objName.Contains("convergence"))
                {
                    _hoverTooltip = "CONVERGENCE CORE  [Space / Click to Transmit Pulse]";
                }
                else if (objName.Contains("gate") || objName.Contains("portal"))
                {
                    _hoverTooltip = "THE AWAKENING GATE  [Achieve 100% Convergence to Unseal]";
                }
                else
                {
                    _isHoveringInteractable = false;
                    _hoverTooltip = "";
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
            // OnGUI coords have y=0 at top
            return _hudRect.Contains(mousePosition);
        }

        private void OnGUI()
        {
            DrawReticle();

            if (!showHUD || neuralState == null) return;

            DrawEngineerHUD();
        }

        private void DrawReticle()
        {
            if (!showReticle) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

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

            // Tooltip label
            if (!string.IsNullOrEmpty(_hoverTooltip))
            {
                GUIStyle tipStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = _neonCyan }
                };

                Rect tipRect = new Rect(cx - 250, cy + 25, 500, 24);
                DrawRect(new Rect(tipRect.x + 30, tipRect.y, tipRect.width - 60, tipRect.height), new Color(0.02f, 0.05f, 0.1f, 0.75f));
                GUI.Label(tipRect, _hoverTooltip, tipStyle);
            }
        }

        private void DrawEngineerHUD()
        {
            float hudW = 420f;
            float hudH = 390f;
            _hudRect = new Rect(16, 16, hudW, hudH);

            // Background & border
            DrawRect(_hudRect, _panelBg);
            DrawBorder(_hudRect, _panelBorder, 2);

            // Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _neonCyan }
            };
            GUI.Label(new Rect(_hudRect.x + 14, _hudRect.y + 8, hudW - 28, 22), "CONVERGENCE // NEURAL REPAIR CONSOLE", headerStyle);

            GUIStyle subHeader = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10,
                normal = { textColor = _dimText }
            };
            GUI.Label(new Rect(_hudRect.x + 14, _hudRect.y + 8, hudW - 28, 22), "[H] Toggle HUD", subHeader);

            DrawRect(new Rect(_hudRect.x + 12, _hudRect.y + 32, hudW - 24, 1), new Color(0.0f, 0.8f, 1.0f, 0.35f));

            float y = _hudRect.y + 38;

            // --- 1. ACTIVATION MODE SELECTOR ---
            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(_hudRect.x + 14, y, 180, 20), "ACTIVATION MODULE", sectionStyle);
            y += 20;

            ActivationType currentAct = neuralState.Activation;
            float btnW = (hudW - 40) / 4f;

            DrawActivationButton(new Rect(_hudRect.x + 14, y, btnW, 28), "Linear", ActivationType.Linear, currentAct);
            DrawActivationButton(new Rect(_hudRect.x + 14 + btnW + 4, y, btnW, 28), "ReLU", ActivationType.ReLU, currentAct);
            DrawActivationButton(new Rect(_hudRect.x + 14 + (btnW + 4) * 2, y, btnW, 28), "Step", ActivationType.Step, currentAct);
            DrawActivationButton(new Rect(_hudRect.x + 14 + (btnW + 4) * 3, y, btnW, 28), "Sigmoid", ActivationType.Sigmoid, currentAct);
            y += 36;

            // --- 2. PARAMETER STEPPERS (W1, W2, BIAS) ---
            GUI.Label(new Rect(_hudRect.x + 14, y, 200, 20), "SYNAPSE & BIAS VOLTAGES", sectionStyle);
            y += 20;

            DrawStepperRow(ref y, "Weight 1 (w1)", neuralState.Weight1, val => neuralState.SetWeight(0, val));
            DrawStepperRow(ref y, "Weight 2 (w2)", neuralState.Weight2, val => neuralState.SetWeight(1, val));
            DrawStepperRow(ref y, "Bias Voltage (b)", neuralState.Bias, val => neuralState.SetBias(val));
            y += 6;

            // --- 3. CONDUIT SWITCHES ---
            float halfW = (hudW - 32) / 2f;
            bool c1 = neuralState.Cable1Connected;
            bool c2 = neuralState.Cable2Connected;

            Color c1Color = c1 ? _neonCyan : _neonAmber;
            if (GUI.Button(new Rect(_hudRect.x + 14, y, halfW, 26), $"X1 Conduit: {(c1 ? "ONLINE" : "CUT")}"))
            {
                neuralState.SetCableConnected(0, !c1);
            }

            Color c2Color = c2 ? _neonCyan : _neonAmber;
            if (GUI.Button(new Rect(_hudRect.x + 18 + halfW, y, halfW, 26), $"X2 Conduit: {(c2 ? "ONLINE" : "CUT")}"))
            {
                neuralState.SetCableConnected(1, !c2);
            }
            y += 34;

            // --- 4. PROMINENT PULSE TRANSMITTER BUTTON ---
            GUI.backgroundColor = _neonCyan;
            if (GUI.Button(new Rect(_hudRect.x + 14, y, hudW - 28, 36), "⚡ TRANSMIT NEURAL PULSE  [SPACE]"))
            {
                if (chamberController != null)
                {
                    chamberController.TriggerForwardPass();
                }
            }
            GUI.backgroundColor = Color.white;
            y += 44;

            // --- 5. TELEMETRY TRUTH TABLE ---
            DrawTelemetrySection(y, hudW);
        }

        private void DrawActivationButton(Rect r, string label, ActivationType type, ActivationType active)
        {
            bool isCurrent = (type == active);
            if (isCurrent)
            {
                DrawRect(r, new Color(0.0f, 0.5f, 0.7f, 0.6f));
                DrawBorder(r, _neonEmerald, 2);
            }

            if (GUI.Button(r, label))
            {
                neuralState.SetActivation(type);
            }
        }

        private void DrawStepperRow(ref float y, string label, double currentValue, Action<float> onApply)
        {
            float rowX = _hudRect.x + 14;
            float rowW = _hudRect.width - 28;

            GUIStyle lblStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = _dimText }
            };
            GUI.Label(new Rect(rowX, y, 130, 24), label, lblStyle);

            // Minus button
            if (GUI.Button(new Rect(rowX + 130, y, 34, 24), "–"))
            {
                float newVal = Mathf.Clamp((float)Math.Round((currentValue - 0.5) / 0.5) * 0.5f, -2.0f, 2.0f);
                onApply(newVal);
            }

            // Current Value display
            GUIStyle valStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _neonCyan }
            };
            GUI.Label(new Rect(rowX + 168, y, 64, 24), $"{currentValue:+0.0;-0.0;0.0}", valStyle);

            // Plus button
            if (GUI.Button(new Rect(rowX + 236, y, 34, 24), "+"))
            {
                float newVal = Mathf.Clamp((float)Math.Round((currentValue + 0.5) / 0.5) * 0.5f, -2.0f, 2.0f);
                onApply(newVal);
            }

            y += 26;
        }

        private void DrawTelemetrySection(float y, float hudW)
        {
            DrawRect(new Rect(_hudRect.x + 12, y, hudW - 24, 1), new Color(0.0f, 0.8f, 1.0f, 0.35f));
            y += 6;

            if (chamberController == null) return;

            var eval = chamberController.LastEvaluation;
            GUIStyle statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = (eval != null && eval.Passed) ? _neonEmerald : _neonAmber }
            };

            string statusStr = eval == null
                ? "OR GATE: AWAITING PULSE TRANSMISSION"
                : (eval.Passed ? "CONVERGENCE ACHIEVED (100% ACCURACY) — GATE UNSEALED" : $"ACCURACY: {eval.PassedCases}/{eval.TotalCases} ({eval.Accuracy * 100:F0}%)");

            GUI.Label(new Rect(_hudRect.x + 14, y, hudW - 28, 20), statusStr, statStyle);
            y += 20;

            GUIStyle tableHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = _dimText }
            };
            GUI.Label(new Rect(_hudRect.x + 14, y, hudW - 28, 16), "  (X1, X2)  |  Target  |  Calculated z  |  Output  |  Status", tableHeader);
            y += 16;

            if (eval != null && eval.Diagnostics != null)
            {
                for (int i = 0; i < eval.Diagnostics.Count && i < 4; i++)
                {
                    var d = eval.Diagnostics[i];
                    Color rowColor = d.IsCorrect ? _neonEmerald : _neonRed;
                    string passStr = d.IsCorrect ? "[PASS]" : "[FAIL]";

                    GUIStyle rowStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 10,
                        normal = { textColor = rowColor }
                    };

                    string line = $"  {d.Label,-9} |    {d.ExpectedOutput:F0}   |     {d.CalculatedZ:+0.00;-0.00;0.00}     |    {d.ActualOutput:F0}     |  {passStr}";
                    GUI.Label(new Rect(_hudRect.x + 14, y, hudW - 28, 16), line, rowStyle);
                    y += 16;
                }
            }
            else
            {
                GUIStyle tip = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    normal = { textColor = _dimText }
                };
                GUI.Label(new Rect(_hudRect.x + 14, y, hudW - 28, 30), "Goal: Output 1 if X1=1 or X2=1; Output 0 if both 0.\nSolution Hint: w1=1.0, w2=1.0, b=-0.5, Step Crystal.", tip);
            }
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
            DrawRect(new Rect(rect.x, rect.y, rect.width, width), color); // Top
            DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color); // Bottom
            DrawRect(new Rect(rect.x, rect.y, width, rect.height), color); // Left
            DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color); // Right
        }
    }
}
