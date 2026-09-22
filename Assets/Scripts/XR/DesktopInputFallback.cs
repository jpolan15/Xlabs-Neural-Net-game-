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

        [Header("HUD Display")]
        [SerializeField] private bool showHUD = false;

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
            // If VR headset is active, disable mouse/keyboard look to avoid fighting headset tracking
            bool isXRActive = UnityEngine.XR.XRSettings.isDeviceActive;
            if (!isXRActive)
            {
                HandleNavigation();
            }

            HandleDirectParameterShortcuts();
            HandleActions();
            HandleMouseWheelAdjustments();
        }

        private void HandleMouseWheelAdjustments()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.01f) return;

            int stepDelta = scroll > 0 ? 1 : -1;

            Camera cam = Camera.main ?? GetComponent<Camera>();
            Ray ray = cam != null
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
                : new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 25.0f))
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
            }
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

        private void HandleDirectParameterShortcuts()
        {
            if (neuralState == null) return;

            // [1] / [2] -> Weight 1 Down / Up
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                double w1 = System.Math.Round((neuralState.Weight1 - 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(0, Mathf.Clamp((float)w1, -2.0f, 2.0f));
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                double w1 = System.Math.Round((neuralState.Weight1 + 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(0, Mathf.Clamp((float)w1, -2.0f, 2.0f));
            }

            // [3] / [4] -> Weight 2 Down / Up
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                double w2 = System.Math.Round((neuralState.Weight2 - 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(1, Mathf.Clamp((float)w2, -2.0f, 2.0f));
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                double w2 = System.Math.Round((neuralState.Weight2 + 0.5) / 0.5) * 0.5;
                neuralState.SetWeight(1, Mathf.Clamp((float)w2, -2.0f, 2.0f));
            }

            // [5] / [6] -> Bias Down / Up
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                double b = System.Math.Round((neuralState.Bias - 0.5) / 0.5) * 0.5;
                neuralState.SetBias(Mathf.Clamp((float)b, -2.0f, 2.0f));
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                double b = System.Math.Round((neuralState.Bias + 0.5) / 0.5) * 0.5;
                neuralState.SetBias(Mathf.Clamp((float)b, -2.0f, 2.0f));
            }

            // [X] or [Tab] -> Cycle Activation Crystal
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Tab))
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
            }

            // [C] -> Toggle Cable 1
            if (Input.GetKeyDown(KeyCode.C))
            {
                neuralState.SetCableConnected(0, !neuralState.Cable1Connected);
            }

            // [V] -> Toggle Cable 2
            if (Input.GetKeyDown(KeyCode.V))
            {
                neuralState.SetCableConnected(1, !neuralState.Cable2Connected);
            }

            // [T] / [Q] -> Next / Previous Page on Field Manual
            if (Input.GetKeyDown(KeyCode.T))
            {
                OnManualNextPageRequested?.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                OnManualPrevPageRequested?.Invoke();
            }

            // [Q] / [W] -> Unified sensitivity down / up (moves both weights + auto-bias together)
            // This is the primary simplified control for Level 1 guided sandbox mode
            if (Input.GetKeyDown(KeyCode.Q))
            {
                double cur = neuralState.Weight1;
                double next = System.Math.Round((cur - 0.5) / 0.5) * 0.5;
                neuralState.SetUnifiedWeight(Mathf.Clamp((float)next, -2.0f, 2.0f));
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                double cur = neuralState.Weight1;
                double next = System.Math.Round((cur + 0.5) / 0.5) * 0.5;
                neuralState.SetUnifiedWeight(Mathf.Clamp((float)next, -2.0f, 2.0f));
            }

            // [R] -> Reset Chamber
            if (Input.GetKeyDown(KeyCode.R) && levelResetter != null)
            {
                levelResetter.ResetToActivePreset();
            }
        }

        private void HandleActions()
        {
            // Interact with targeted object via [E] or Left-Click
            bool interactKey = Input.GetKeyDown(KeyCode.E);
            bool leftClick = Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1);

            // Ignore clicks if mouse is over top-left HUD area or bottom-left manual area
            if (leftClick)
            {
                Vector2 mPos = Input.mousePosition; // (0,0) is bottom-left
                if (mPos.x < 480 && (mPos.y > Screen.height - 420 || mPos.y < 300))
                {
                    leftClick = false; // Reserved for HUD / Manual UI buttons
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
                    var receptor = hit.collider.GetComponentInParent<DataTargetReceptor>();
                    if (receptor != null && chamberController != null)
                    {
                        chamberController.TriggerSingleCasePass(receptor.CaseIndex);
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
                    if (socket != null)
                    {
                        socket.CycleCrystal();
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
            }

            // Pull Clock Cycle Lever / Execute Forward Pass on [Space] or [Return]
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (chamberController != null)
                {
                    chamberController.TriggerForwardPass();
                }
            }
        }
    }
}
