using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Complete desktop fallback controller allowing Level 1 to be fully played, tested,
    /// and verified without VR hardware. Routes all commands through the exact same
    /// Gameplay state machines and pure Core evaluation pipelines as the VR interactors.
    /// </summary>
    public class DesktopInputFallback : MonoBehaviour
    {
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

        [Header("HUD Display")]
        [SerializeField] private bool showHUD = false;

        private int _selectedParameter = 0; // 0 = W1, 1 = W2, 2 = Bias
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (levelResetter == null) levelResetter = FindAnyObjectByType<LevelResetter>();
            if (pulseTool == null) pulseTool = FindAnyObjectByType<NeuralPulseToolInteractor>();
            if (arcBlade == null) arcBlade = FindAnyObjectByType<ArcBladeInteractor>();
            if (activationSocket == null) activationSocket = FindAnyObjectByType<ActivationSocketInteractor>();
        }

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _pitch = angles.x;
            _yaw = angles.y;
        }

        private void Update()
        {
            HandleNavigation();
            HandleSelection();
            HandleAdjustments();
            HandleActions();
        }

        private void HandleNavigation()
        {
            // Mouse look when right mouse button is held
            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * lookSpeed;
                _pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
                _pitch = Mathf.Clamp(_pitch, -80f, 80f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
            }

            // WASD translation
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            if (moveX != 0 || moveZ != 0)
            {
                Vector3 moveDir = (transform.right * moveX + transform.forward * moveZ).normalized;
                transform.position += moveDir * (moveSpeed * Time.deltaTime);
            }
        }

        private void HandleSelection()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) _selectedParameter = 0; // W1
            if (Input.GetKeyDown(KeyCode.Alpha2)) _selectedParameter = 1; // W2
            if (Input.GetKeyDown(KeyCode.Alpha3)) _selectedParameter = 2; // Bias
        }

        private void HandleAdjustments()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int step = 0;

            if (scroll > 0.05f || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                step = 1;
            }
            else if (scroll < -0.05f || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                step = -1;
            }

            if (step != 0 && neuralState != null)
            {
                if (_selectedParameter == 0)
                {
                    double w1 = System.Math.Round((neuralState.Weight1 + step * 0.5) / 0.5) * 0.5;
                    neuralState.SetWeight(0, Mathf.Clamp((float)w1, -2.0f, 2.0f));
                }
                else if (_selectedParameter == 1)
                {
                    double w2 = System.Math.Round((neuralState.Weight2 + step * 0.5) / 0.5) * 0.5;
                    neuralState.SetWeight(1, Mathf.Clamp((float)w2, -2.0f, 2.0f));
                }
                else if (_selectedParameter == 2)
                {
                    double b = System.Math.Round((neuralState.Bias + step * 0.5) / 0.5) * 0.5;
                    neuralState.SetBias(Mathf.Clamp((float)b, -2.0f, 2.0f));
                }
            }

            // Tab to cycle activation crystal
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (activationSocket != null)
                {
                    activationSocket.CycleCrystal();
                }
                else if (neuralState != null)
                {
                    // Direct cycle if socket not placed
                    ActivationType next = neuralState.Activation == ActivationType.Step
                        ? ActivationType.Linear
                        : (neuralState.Activation == ActivationType.Linear ? ActivationType.ReLU : ActivationType.Step);
                    neuralState.SetActivation(next);
                }
            }

            // C to toggle Cable 1
            if (Input.GetKeyDown(KeyCode.C) && neuralState != null)
            {
                neuralState.SetCableConnected(0, !neuralState.Cable1Connected);
            }

            // V to toggle Cable 2
            if (Input.GetKeyDown(KeyCode.V) && neuralState != null)
            {
                neuralState.SetCableConnected(1, !neuralState.Cable2Connected);
            }
        }

        private void HandleActions()
        {
            // Interact with targeted object via [E] or Left-Click
            bool interactKey = Input.GetKeyDown(KeyCode.E);
            bool leftClick = Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1);

            // Ignore clicks if mouse is over top-left HUD area
            if (leftClick)
            {
                Vector2 mPos = Input.mousePosition; // (0,0) is bottom-left
                if (mPos.x < 450 && mPos.y > Screen.height - 420)
                {
                    leftClick = false; // Mouse click reserved for HUD buttons
                }
            }

            if (interactKey || leftClick)
            {
                Camera cam = Camera.main ?? GetComponent<Camera>();
                Ray ray = cam != null
                    ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
                    : new Ray(transform.position, transform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, 25.0f))
                {
                    var socket = hit.collider.GetComponentInParent<ActivationSocketInteractor>();
                    if (socket != null)
                    {
                        socket.CycleCrystal();
                        return; // Successfully interacted: DO NOT fire pulse!
                    }

                    var wReg = hit.collider.GetComponentInParent<WeightRegulatorInteractor>();
                    if (wReg != null && neuralState != null)
                    {
                        int idx = wReg.SocketIndex;
                        double curW = idx == 0 ? neuralState.Weight1 : neuralState.Weight2;
                        double nextW = Math.Round((curW + 0.5) / 0.5) * 0.5;
                        if (nextW > 2.0) nextW = -2.0;
                        neuralState.SetWeight(idx, (float)nextW);
                        return; // Successfully interacted: DO NOT fire pulse!
                    }

                    var biasDial = hit.collider.GetComponentInParent<BiasDialInteractor>();
                    if (biasDial != null && neuralState != null)
                    {
                        double curB = neuralState.Bias;
                        double nextB = Math.Round((curB + 0.5) / 0.5) * 0.5;
                        if (nextB > 2.0) nextB = -2.0;
                        neuralState.SetBias((float)nextB);
                        return; // Successfully interacted: DO NOT fire pulse!
                    }

                    var cable = hit.collider.GetComponentInParent<CableInteractable>();
                    if (cable != null && neuralState != null)
                    {
                        int cIdx = cable.CableIndex;
                        bool connected = cIdx == 0 ? neuralState.Cable1Connected : neuralState.Cable2Connected;
                        neuralState.SetCableConnected(cIdx, !connected);
                        return; // Successfully interacted: DO NOT fire pulse!
                    }
                }
            }

            // Fire Neural Pulse Tool: Exclusively on [Space] or Left-Clicking while aiming directly at Convergence Core
            bool fireAtCore = false;
            if (leftClick)
            {
                Camera cam = Camera.main ?? GetComponent<Camera>();
                Ray ray = cam != null
                    ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
                    : new Ray(transform.position, transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 30.0f))
                {
                    string hitName = hit.collider.gameObject.name.ToLower();
                    if (hitName.Contains("core") || hitName.Contains("pulse") || hitName.Contains("convergence"))
                    {
                        fireAtCore = true;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) || fireAtCore)
            {
                if (pulseTool != null)
                {
                    pulseTool.FirePulse();
                }
                else if (chamberController != null)
                {
                    chamberController.TriggerForwardPass();
                }
            }

            // Strike with Arc Blade: F
            if (Input.GetKeyDown(KeyCode.F) && arcBlade != null)
            {
                arcBlade.Strike();
            }

            // Reset Level: R (active preset), T (random preset)
            if (Input.GetKeyDown(KeyCode.R) && levelResetter != null)
            {
                levelResetter.ResetToActivePreset();
            }
            if (Input.GetKeyDown(KeyCode.T) && levelResetter != null)
            {
                levelResetter.ResetRandom();
            }
        }

        private void OnGUI()
        {
            // Plain legacy HUD disabled in favor of Presentation.SciFiEngineerHUD
            if (!showHUD || neuralState == null) return;


            GUI.Box(new Rect(10, 10, 380, 290), "CONVERGENCE — Desktop Simulation Controller");

            string sel0 = _selectedParameter == 0 ? "> [1] Weight 1 (w1): " : "  [1] Weight 1 (w1): ";
            string sel1 = _selectedParameter == 1 ? "> [2] Weight 2 (w2): " : "  [2] Weight 2 (w2): ";
            string sel2 = _selectedParameter == 2 ? "> [3] Bias (b):     " : "  [3] Bias (b):     ";

            GUI.Label(new Rect(20, 35, 360, 20), $"{sel0}{neuralState.Weight1:+0.0;-0.0;0.0}");
            GUI.Label(new Rect(20, 55, 360, 20), $"{sel1}{neuralState.Weight2:+0.0;-0.0;0.0}");
            GUI.Label(new Rect(20, 75, 360, 20), $"{sel2}{neuralState.Bias:+0.0;-0.0;0.0}");

            GUI.Label(new Rect(20, 100, 360, 20), $"[Tab] Activation Module: {neuralState.Activation}");
            GUI.Label(new Rect(20, 120, 360, 20), $"[C] Cable 1 (X1): {(neuralState.Cable1Connected ? "CONNECTED" : "DISCONNECTED")}");
            GUI.Label(new Rect(20, 140, 360, 20), $"[V] Cable 2 (X2): {(neuralState.Cable2Connected ? "CONNECTED" : "DISCONNECTED")}");

            GUI.Label(new Rect(20, 165, 360, 20), $"[Space / Left-Click] Fire Neural Pulse Tool");
            GUI.Label(new Rect(20, 185, 360, 20), $"[R] Reset Active Preset  |  [T] Random Preset");
            GUI.Label(new Rect(20, 205, 360, 20), $"[Right-Mouse + WASD] Free Camera Navigation");

            if (chamberController != null)
            {
                GUI.Label(new Rect(20, 230, 360, 20), $"Chamber Phase: {chamberController.Phase}");
                if (chamberController.LastEvaluation != null)
                {
                    var eval = chamberController.LastEvaluation;
                    string statusColor = eval.Passed ? "SOLVED (100%)" : $"{eval.Accuracy * 100:F0}% ({eval.PassedCases}/{eval.TotalCases})";
                    GUI.Label(new Rect(20, 250, 360, 20), $"Diagnostic Accuracy: {statusColor}");
                    GUI.Label(new Rect(20, 270, 360, 20), $"{eval.Summary}");
                }
            }
        }
    }
}
