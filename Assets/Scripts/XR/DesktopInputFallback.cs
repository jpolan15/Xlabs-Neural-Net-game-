using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Complete desktop fallback controller allowing Level 1 to be fully played, tested,
    /// and verified without VR hardware. Routes all commands through the exact same
    /// Gameplay state machines and pure Core evaluation pipelines as the VR interactors.
    /// Supports intuitive WASD horizontal walking, mouse look with standard and inverted modes,
    /// direct mouse cursor clicking, dragging, and scroll wheel adjustments.
    /// </summary>
    public class DesktopInputFallback : MonoBehaviour
    {
        public static event Action OnManualNextPageRequested;
        public static event Action OnManualPrevPageRequested;

        [Header("Scene References")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private LevelResetter levelResetter;
        [SerializeField] private NeuralPulseToolInteractor pulseTool;
        [SerializeField] private ArcBladeInteractor arcBlade;
        [SerializeField] private ActivationSocketInteractor activationSocket;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float lookSpeed = 2.5f;

        [Header("Look Inversion")]
        [SerializeField] private bool invertLookX = false;
        [SerializeField] private bool invertLookY = false;

        [Header("HUD & Help Display")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private bool fpsMouseLookMode = false;

        private float _yaw;
        private float _pitch;
        private Transform _playerRoot;
        private Camera _cam;

        // Hover & Drag interaction state
        private GameObject _hoveredObject;
        private string _hoverTooltip = "";
        private bool _isDraggingSlider = false;
        private KineticWeightSliderInteractor _activeDragSlider;
        private BiasDialInteractor _activeDragDial;
        private Vector2 _dragStartMousePos;
        private double _dragStartValue;

        // Notification popup for settings toggles
        private string _notificationText = "";
        private float _notificationTimer = 0f;

        // GUI Styles
        private GUIStyle _tooltipStyle;
        private GUIStyle _bottomBarStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _notificationStyle;
        private Texture2D _panelBgTex;
        private Texture2D _btnBgTex;
        private Texture2D _btnActiveBgTex;

        private void Awake()
        {
            // ── VR Guard ──────────────────────────────────────────────────────────
            // If an XR device or headset is active, disable desktop fallback completely.
            // Under OpenXR / Quest Link, XRSettings.isDeviceActive or XRDisplaySubsystem indicates VR.
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                Debug.Log("[DesktopInputFallback] XRSettings device active — disabling desktop fallback.");
                enabled = false;
                return;
            }

            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
            {
                if (d.running)
                {
                    Debug.Log("[DesktopInputFallback] XR display active — desktop fallback disabled.");
                    enabled = false;
                    return;
                }
            }
            // ─────────────────────────────────────────────────────────────────────

            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (levelResetter == null) levelResetter = FindAnyObjectByType<LevelResetter>();
            if (pulseTool == null) pulseTool = FindAnyObjectByType<NeuralPulseToolInteractor>();
            if (arcBlade == null) arcBlade = FindAnyObjectByType<ArcBladeInteractor>();
            if (activationSocket == null) activationSocket = FindAnyObjectByType<ActivationSocketInteractor>();

            _cam = GetComponent<Camera>() ?? Camera.main;

            // Locate player root (XR Origin or root ancestor)
            if (transform.parent != null)
            {
                _playerRoot = transform.parent.parent != null ? transform.parent.parent : transform.parent;
            }
            else
            {
                _playerRoot = transform.root;
            }
        }

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _pitch = angles.x;
            if (_pitch > 180f) _pitch -= 360f;
            _yaw = angles.y;

            InitTextures();
        }

        private void InitTextures()
        {
            _panelBgTex = new Texture2D(1, 1);
            _panelBgTex.SetPixel(0, 0, new Color(0.04f, 0.08f, 0.14f, 0.88f));
            _panelBgTex.Apply();

            _btnBgTex = new Texture2D(1, 1);
            _btnBgTex.SetPixel(0, 0, new Color(0.10f, 0.20f, 0.32f, 0.90f));
            _btnBgTex.Apply();

            _btnActiveBgTex = new Texture2D(1, 1);
            _btnActiveBgTex.SetPixel(0, 0, new Color(0.0f, 0.65f, 0.85f, 0.95f));
            _btnActiveBgTex.Apply();
        }

        private void Update()
        {
            HandleNavigation();
            HandleDirectParameterShortcuts();
            HandleHoverDetection();
            HandleMouseWheelAdjustments();
            HandleMouseClickAndDrag();

            if (_notificationTimer > 0f)
            {
                _notificationTimer -= Time.deltaTime;
            }
        }

        private void HandleNavigation()
        {
            // Toggle FPS look mode with Tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                fpsMouseLookMode = !fpsMouseLookMode;
                Cursor.lockState = fpsMouseLookMode ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !fpsMouseLookMode;
                ShowNotification(fpsMouseLookMode ? "FPS Mouse Look: ACTIVE (Press Tab to Free Cursor)" : "Cursor Look: ACTIVE (Hold Right-Click to Look)");
            }

            // Toggle look inversion with [I]
            if (Input.GetKeyDown(KeyCode.I))
            {
                invertLookY = !invertLookY;
                invertLookX = !invertLookX;
                ShowNotification($"Look Inversion: {(invertLookY ? "INVERTED" : "STANDARD")}");
            }

            // Mouse look: active when fpsMouseLookMode is ON OR when Right/Middle mouse is held
            bool isLooking = fpsMouseLookMode || Input.GetMouseButton(1) || Input.GetMouseButton(2);

            if (isLooking)
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

                // Standard natural look: moving mouse right turns right (+yaw), moving mouse up looks up (-pitch)
                float deltaYaw = invertLookX ? -mouseX : mouseX;
                float deltaPitch = invertLookY ? mouseY : -mouseY;

                _yaw += deltaYaw;
                _pitch += deltaPitch;
                _pitch = Mathf.Clamp(_pitch, -80f, 80f);
            }

            // Arrow keys also rotate the camera smoothly
            float arrowSpeed = lookSpeed * 35.0f * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow)) _yaw -= arrowSpeed;
            if (Input.GetKey(KeyCode.RightArrow)) _yaw += arrowSpeed;
            if (Input.GetKey(KeyCode.UpArrow)) _pitch -= arrowSpeed;
            if (Input.GetKey(KeyCode.DownArrow)) _pitch += arrowSpeed;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            // Apply camera rotation only if not in VR
            if (!UnityEngine.XR.XRSettings.isDeviceActive)
            {
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
            }

            // WASD horizontal walking translation across the bridge deck
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            if (moveX != 0 || moveZ != 0)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                Vector3 moveDir = (right * moveX + forward * moveZ).normalized;

                if (_playerRoot != null && _playerRoot != transform)
                {
                    Vector3 newPos = _playerRoot.position + moveDir * (moveSpeed * Time.deltaTime);
                    // Clamp within spaceship bridge deck boundaries
                    newPos.x = Mathf.Clamp(newPos.x, -2.8f, 2.8f);
                    newPos.z = Mathf.Clamp(newPos.z, -3.2f, 2.2f);
                    newPos.y = 0.0f; // Solid deck floor level
                    _playerRoot.position = newPos;

                    // Ensure camera stays at eye level relative to deck
                    transform.localPosition = new Vector3(0, 1.65f, 0);
                }
                else
                {
                    Vector3 newPos = transform.position + moveDir * (moveSpeed * Time.deltaTime);
                    newPos.x = Mathf.Clamp(newPos.x, -2.8f, 2.8f);
                    newPos.z = Mathf.Clamp(newPos.z, -3.2f, 2.2f);
                    newPos.y = 1.65f;
                    transform.position = newPos;
                }
            }
        }

        private Ray GetInteractionRay()
        {
            if (_cam == null) _cam = Camera.main ?? GetComponent<Camera>();
            if (_cam == null) return new Ray(transform.position, transform.forward);

            if (fpsMouseLookMode)
            {
                return _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                return _cam.ScreenPointToRay(Input.mousePosition);
            }
        }

        private void HandleHoverDetection()
        {
            _hoverTooltip = "";
            _hoveredObject = null;

            Ray ray = GetInteractionRay();
            if (Physics.Raycast(ray, out RaycastHit hit, 30.0f))
            {
                _hoveredObject = hit.collider.gameObject;

                // Kinetic Weight Slider
                var slider = hit.collider.GetComponentInParent<KineticWeightSliderInteractor>();
                if (slider != null)
                {
                    string sign = slider.CurrentWeight >= 0 ? "+" : "";
                    _hoverTooltip = $"● SENSITIVITY {slider.SensorName.ToUpper()} [{sign}{slider.CurrentWeight:F1}]\n[Click / Drag / Scroll] to adjust weight";
                    return;
                }

                // Weight Regulator
                var wReg = hit.collider.GetComponentInParent<WeightRegulatorInteractor>();
                if (wReg != null)
                {
                    _hoverTooltip = $"● WEIGHT REGULATOR {wReg.SocketIndex + 1}\n[Click / Scroll] to adjust";
                    return;
                }

                // Bias Dial
                var biasDial = hit.collider.GetComponentInParent<BiasDialInteractor>();
                if (biasDial != null)
                {
                    string sign = biasDial.CurrentBias >= 0 ? "+" : "";
                    _hoverTooltip = $"● NOISE FILTER (BIAS) [{sign}{biasDial.CurrentBias:F1}]\n[Click / Drag / Scroll] to adjust squelch threshold";
                    return;
                }

                // Activation Socket / Crystals
                var socket = hit.collider.GetComponentInParent<ActivationSocketInteractor>();
                if (socket != null || hit.collider.name.ToLower().Contains("crystal"))
                {
                    string act = neuralState != null ? neuralState.Activation.ToString() : "Step";
                    _hoverTooltip = $"● WARP LOCK CRYSTAL CORE [{act.ToUpper()}]\n[Click] or [X] to swap crystal function";
                    return;
                }

                // Master Clock Lever
                var lever = hit.collider.GetComponentInParent<ClockPulseLeverInteractor>();
                if (lever != null)
                {
                    _hoverTooltip = "● MASTER JUMP PULSE LEVER\n[Click] or [Space] to fire evaluation pulse";
                    return;
                }

                // Data Target Receptor (floating 3D screens in room)
                var receptor = hit.collider.GetComponentInParent<DataTargetReceptor>();
                if (receptor != null)
                {
                    string stateStr = receptor.IsHarmonized ? "LOCKED ✓" : "UNSTABLE ✗";
                    _hoverTooltip = $"● {receptor.TargetTitle.ToUpper()} [{stateStr}]\n[Click] to test single scenario pulse";
                    return;
                }

                // Cable
                var cable = hit.collider.GetComponentInParent<CableInteractable>();
                if (cable != null)
                {
                    _hoverTooltip = "● SYNAPTIC CABLE CONDUIT\n[Click] to connect / disconnect";
                    return;
                }

                // Terminal
                var terminal = hit.collider.GetComponentInParent<InputTerminalInteractor>();
                if (terminal != null)
                {
                    _hoverTooltip = "● SENSOR INPUT TERMINAL\n[Click] to toggle test bit";
                    return;
                }
            }
        }

        private void HandleMouseWheelAdjustments()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.01f) return;

            int stepDelta = scroll > 0 ? 1 : -1;

            Ray ray = GetInteractionRay();
            if (Physics.Raycast(ray, out RaycastHit hit, 30.0f))
            {
                var slider = hit.collider.GetComponentInParent<KineticWeightSliderInteractor>();
                if (slider != null)
                {
                    slider.StepAdjust(stepDelta);
                    return;
                }

                var wReg = hit.collider.GetComponentInParent<WeightRegulatorInteractor>();
                if (wReg != null)
                {
                    wReg.StepAdjust(stepDelta);
                    return;
                }

                var biasDial = hit.collider.GetComponentInParent<BiasDialInteractor>();
                if (biasDial != null)
                {
                    biasDial.StepAdjust(stepDelta);
                    return;
                }

                var socket = hit.collider.GetComponentInParent<ActivationSocketInteractor>();
                if (socket != null || hit.collider.name.ToLower().Contains("crystal"))
                {
                    socket?.CycleCrystal();
                    return;
                }
            }
        }

        private void HandleMouseClickAndDrag()
        {
            // [E] key interaction
            if (Input.GetKeyDown(KeyCode.E))
            {
                ExecuteRaycastInteraction();
            }

            // Left Mouse Button Down: start click or drag
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
            {
                Ray ray = GetInteractionRay();
                if (Physics.Raycast(ray, out RaycastHit hit, 30.0f))
                {
                    var slider = hit.collider.GetComponentInParent<KineticWeightSliderInteractor>();
                    if (slider != null)
                    {
                        _isDraggingSlider = true;
                        _activeDragSlider = slider;
                        _dragStartMousePos = Input.mousePosition;
                        _dragStartValue = slider.CurrentWeight;
                        return;
                    }

                    var biasDial = hit.collider.GetComponentInParent<BiasDialInteractor>();
                    if (biasDial != null)
                    {
                        _isDraggingSlider = true;
                        _activeDragDial = biasDial;
                        _dragStartMousePos = Input.mousePosition;
                        _dragStartValue = biasDial.CurrentBias;
                        return;
                    }

                    // Execute immediate click on other interactables
                    ExecuteHitInteraction(hit);
                }
            }

            // Left Mouse Button Dragging
            if (_isDraggingSlider && Input.GetMouseButton(0))
            {
                Vector2 currentMousePos = Input.mousePosition;
                float dragDeltaY = currentMousePos.y - _dragStartMousePos.y;
                float dragDeltaX = currentMousePos.x - _dragStartMousePos.x;
                float effectiveDelta = Mathf.Abs(dragDeltaY) > Mathf.Abs(dragDeltaX) ? dragDeltaY : dragDeltaX;

                int stepOffset = Mathf.RoundToInt(effectiveDelta / 25.0f);
                double targetVal = _dragStartValue + (stepOffset * 0.5);
                targetVal = Math.Round(targetVal / 0.5) * 0.5;

                if (_activeDragSlider != null)
                {
                    _activeDragSlider.SetWeight(targetVal);
                }
                else if (_activeDragDial != null)
                {
                    _activeDragDial.SetBias(targetVal);
                }
            }

            // Left Mouse Button Up: end dragging
            if (Input.GetMouseButtonUp(0))
            {
                if (_isDraggingSlider)
                {
                    // If it was a quick click without much drag, cycle forward
                    Vector2 endMousePos = Input.mousePosition;
                    if (Vector2.Distance(endMousePos, _dragStartMousePos) < 6.0f)
                    {
                        if (_activeDragSlider != null) _activeDragSlider.OnXRInteract();
                        else if (_activeDragDial != null) _activeDragDial.OnXRInteract();
                    }
                }
                _isDraggingSlider = false;
                _activeDragSlider = null;
                _activeDragDial = null;
            }

            // Pull Clock Cycle Lever / Execute Forward Pass on [Space] or [Return]
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                TriggerForwardPass();
            }
        }

        private void ExecuteRaycastInteraction()
        {
            Ray ray = GetInteractionRay();
            if (Physics.Raycast(ray, out RaycastHit hit, 30.0f))
            {
                ExecuteHitInteraction(hit);
            }
        }

        private void ExecuteHitInteraction(RaycastHit hit)
        {
            var receptor = hit.collider.GetComponentInParent<DataTargetReceptor>();
            if (receptor != null && chamberController != null)
            {
                chamberController.TriggerSingleCasePass(receptor.CaseIndex);
                ShowNotification($"Pulse Fired: Scenario {receptor.CaseIndex + 1} ({receptor.TargetTitle})");
                return;
            }

            var slider = hit.collider.GetComponentInParent<KineticWeightSliderInteractor>();
            if (slider != null)
            {
                slider.OnXRInteract();
                return;
            }

            var terminal = hit.collider.GetComponentInParent<InputTerminalInteractor>();
            if (terminal != null)
            {
                terminal.ToggleValue();
                return;
            }

            var lever = hit.collider.GetComponentInParent<ClockPulseLeverInteractor>();
            if (lever != null)
            {
                lever.PullLever();
                return;
            }

            var socket = hit.collider.GetComponentInParent<ActivationSocketInteractor>();
            if (socket != null || hit.collider.name.ToLower().Contains("crystal"))
            {
                if (socket != null) socket.CycleCrystal();
                else if (activationSocket != null) activationSocket.CycleCrystal();
                return;
            }

            var wReg = hit.collider.GetComponentInParent<WeightRegulatorInteractor>();
            if (wReg != null)
            {
                wReg.StepAdjust(1);
                return;
            }

            var biasDial = hit.collider.GetComponentInParent<BiasDialInteractor>();
            if (biasDial != null)
            {
                biasDial.StepAdjust(1);
                return;
            }

            var cable = hit.collider.GetComponentInParent<CableInteractable>();
            if (cable != null)
            {
                cable.ToggleConnection();
                return;
            }
        }

        public void TriggerForwardPass()
        {
            if (chamberController != null)
            {
                chamberController.TriggerForwardPass();
                ShowNotification(">> WARP RECOGNITION PULSE FIRED <<");
            }
        }

        private void HandleDirectParameterShortcuts()
        {
            if (neuralState == null) return;

            // [1] / [2] -> Weight 1 Down / Up
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                double w1 = System.Math.Round((neuralState.Weight1 - 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(0, Mathf.Clamp((float)w1, -2.0f, 2.0f));
                ShowNotification($"Weight 1 (Landmass): {neuralState.Weight1:F1}");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                double w1 = System.Math.Round((neuralState.Weight1 + 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(0, Mathf.Clamp((float)w1, -2.0f, 2.0f));
                ShowNotification($"Weight 1 (Landmass): {neuralState.Weight1:F1}");
            }

            // [3] / [4] -> Weight 2 Down / Up
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                double w2 = System.Math.Round((neuralState.Weight2 - 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(1, Mathf.Clamp((float)w2, -2.0f, 2.0f));
                ShowNotification($"Weight 2 (Atmosphere): {neuralState.Weight2:F1}");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                double w2 = System.Math.Round((neuralState.Weight2 + 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(1, Mathf.Clamp((float)w2, -2.0f, 2.0f));
                ShowNotification($"Weight 2 (Atmosphere): {neuralState.Weight2:F1}");
            }

            // [5] / [6] -> Bias Down / Up
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                double b = System.Math.Round((neuralState.Bias - 0.5) / 0.5) * 0.5;
                neuralState.SetBias(Mathf.Clamp((float)b, -2.0f, 2.0f));
                ShowNotification($"Bias (Noise Filter): {neuralState.Bias:F1}");
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                double b = System.Math.Round((neuralState.Bias + 0.5) / 0.5) * 0.5;
                neuralState.SetBias(Mathf.Clamp((float)b, -2.0f, 2.0f));
                ShowNotification($"Bias (Noise Filter): {neuralState.Bias:F1}");
            }

            // [X] -> Cycle Activation Crystal
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (activationSocket != null)
                {
                    activationSocket.CycleCrystal();
                }
                else
                {
                    ActivationType next = neuralState.Activation == ActivationType.Step
                        ? ActivationType.Linear
                        : (neuralState.Activation == ActivationType.Linear ? ActivationType.ReLU : ActivationType.Step);
                    neuralState.SetActivation(next);
                }
                ShowNotification($"Crystal Activation: {neuralState.Activation}");
            }

            // [C] -> Toggle Cable 1
            if (Input.GetKeyDown(KeyCode.C))
            {
                neuralState.SetCableConnected(0, !neuralState.Cable1Connected);
                ShowNotification($"Cable 1: {(neuralState.Cable1Connected ? "CONNECTED" : "DISCONNECTED")}");
            }

            // [V] -> Toggle Cable 2
            if (Input.GetKeyDown(KeyCode.V))
            {
                neuralState.SetCableConnected(1, !neuralState.Cable2Connected);
                ShowNotification($"Cable 2: {(neuralState.Cable2Connected ? "CONNECTED" : "DISCONNECTED")}");
            }

            // [T] / [Y] -> Next / Previous Page on Field Manual
            if (Input.GetKeyDown(KeyCode.T))
            {
                OnManualNextPageRequested?.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OnManualPrevPageRequested?.Invoke();
            }

            // [R] -> Reset Chamber
            if (Input.GetKeyDown(KeyCode.R) && levelResetter != null)
            {
                levelResetter.ResetToActivePreset();
                ShowNotification("Chamber Configuration Reset to Preset");
            }
        }

        private void ShowNotification(string text)
        {
            _notificationText = text;
            _notificationTimer = 2.2f;
        }

        private void OnGUI()
        {
            if (!showHUD) return;

            InitStyles();

            // 1. Notification Popup at Top Center
            if (_notificationTimer > 0f)
            {
                float notifW = 440f;
                float notifH = 34f;
                Rect notifRect = new Rect((Screen.width - notifW) * 0.5f, 20f, notifW, notifH);
                GUI.Box(notifRect, GUIContent.none, _buttonStyle);
                GUI.Label(notifRect, _notificationText, _notificationStyle);
            }

            // 2. Crosshair in FPS Mode
            if (fpsMouseLookMode)
            {
                float chSize = 10f;
                Rect chRect = new Rect((Screen.width - chSize) * 0.5f, (Screen.height - chSize) * 0.5f, chSize, chSize);
                Color oldColor = GUI.color;
                GUI.color = _hoveredObject != null ? new Color(0.2f, 1.0f, 0.5f, 0.9f) : new Color(0.3f, 0.85f, 1.0f, 0.7f);
                GUI.DrawTexture(chRect, _btnActiveBgTex);
                GUI.color = oldColor;
            }

            // 3. Hover Tooltip
            if (!string.IsNullOrEmpty(_hoverTooltip))
            {
                Vector2 mPos = fpsMouseLookMode ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) : (Vector2)Input.mousePosition;
                float ttW = 340f;
                float ttH = 50f;
                float ttX = Mathf.Clamp(mPos.x + 18f, 10f, Screen.width - ttW - 10f);
                float ttY = Mathf.Clamp(Screen.height - mPos.y - ttH - 12f, 10f, Screen.height - ttH - 65f);

                Rect ttRect = new Rect(ttX, ttY, ttW, ttH);
                GUI.Box(ttRect, GUIContent.none, _bottomBarStyle);
                GUI.Label(new Rect(ttX + 10f, ttY + 5f, ttW - 20f, ttH - 10f), _hoverTooltip, _tooltipStyle);
            }

            // 4. Bottom Desktop Controls Help Bar
            float barW = Mathf.Min(Screen.width - 40f, 1080f);
            float barH = 46f;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = Screen.height - barH - 10f;
            Rect barRect = new Rect(barX, barY, barW, barH);
            GUI.Box(barRect, GUIContent.none, _bottomBarStyle);

            // Left side text: Controls summary
            string controlsText = "<b>[W/A/S/D]</b> Walk   <b>[RMB Drag]</b> Look   <b>[LMB]</b> Use / Drag   <b>[Scroll]</b> Adjust   <b>[Space]</b> Pulse";
            GUI.Label(new Rect(barX + 16f, barY + 11f, 620f, 26f), controlsText, _bottomBarStyle);

            // Right side interactive buttons
            float btnW = 125f;
            float btnH = 28f;
            float btnY = barY + 9f;

            // Invert Look Toggle Button
            float btn1X = barX + barW - (btnW * 3f) - 24f;
            string invLabel = invertLookY ? "Look: Inverted [I]" : "Look: Normal [I]";
            if (GUI.Button(new Rect(btn1X, btnY, btnW, btnH), invLabel, _buttonStyle))
            {
                invertLookY = !invertLookY;
                invertLookX = !invertLookX;
                ShowNotification($"Look Inversion: {(invertLookY ? "INVERTED" : "STANDARD")}");
            }

            // Mouse Look Mode Toggle Button
            float btn2X = barX + barW - (btnW * 2f) - 16f;
            string lookModeLabel = fpsMouseLookMode ? "FPS Look [Tab]" : "Cursor Mode [Tab]";
            if (GUI.Button(new Rect(btn2X, btnY, btnW, btnH), lookModeLabel, _buttonStyle))
            {
                fpsMouseLookMode = !fpsMouseLookMode;
                Cursor.lockState = fpsMouseLookMode ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !fpsMouseLookMode;
                ShowNotification(fpsMouseLookMode ? "FPS Mouse Look: ACTIVE" : "Cursor Look: ACTIVE");
            }

            // Fire Pulse Button
            float btn3X = barX + barW - btnW - 8f;
            if (GUI.Button(new Rect(btn3X, btnY, btnW, btnH), "Fire Pulse [Space]", _buttonStyle))
            {
                TriggerForwardPass();
            }
        }

        private void InitStyles()
        {
            if (_bottomBarStyle != null) return;

            _bottomBarStyle = new GUIStyle(GUI.skin.box);
            _bottomBarStyle.normal.background = _panelBgTex;
            _bottomBarStyle.normal.textColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
            _bottomBarStyle.fontSize = 13;
            _bottomBarStyle.alignment = TextAnchor.MiddleLeft;
            _bottomBarStyle.richText = true;

            _tooltipStyle = new GUIStyle(GUI.skin.label);
            _tooltipStyle.normal.textColor = new Color(0.25f, 0.95f, 1.0f, 1.0f);
            _tooltipStyle.fontSize = 12;
            _tooltipStyle.fontStyle = FontStyle.Bold;
            _tooltipStyle.alignment = TextAnchor.UpperLeft;
            _tooltipStyle.wordWrap = true;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.background = _btnBgTex;
            _buttonStyle.hover.background = _btnActiveBgTex;
            _buttonStyle.normal.textColor = new Color(0.9f, 0.96f, 1.0f, 1.0f);
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.fontSize = 11;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;

            _notificationStyle = new GUIStyle(GUI.skin.label);
            _notificationStyle.normal.textColor = new Color(0.3f, 1.0f, 0.65f, 1.0f);
            _notificationStyle.fontSize = 13;
            _notificationStyle.fontStyle = FontStyle.Bold;
            _notificationStyle.alignment = TextAnchor.MiddleCenter;
        }
    }
}
