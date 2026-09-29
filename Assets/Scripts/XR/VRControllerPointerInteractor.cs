using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Complete VR Touch Controller pointer and interactor for Meta Quest 2 / 3 / Pro.
    /// Provides 6DOF laser pointer, reticle indicator, trigger/button interaction,
    /// haptic impulses, and thumbstick detent stepping for chamber interactables.
    /// Follows XR layer boundary rules: converts physical input to gameplay commands.
    /// </summary>
    public class VRControllerPointerInteractor : MonoBehaviour
    {
        public enum ControllerHand
        {
            RightHand,
            LeftHand
        }

        [Header("Controller Hand")]
        [SerializeField] private ControllerHand hand = ControllerHand.RightHand;

        [Header("Laser & Reticle Visuals")]
        [SerializeField] private float maxRayDistance = 15.0f;
        [SerializeField] private Color normalLaserColor = new Color(0.18f, 0.78f, 1.0f, 0.85f);
        [SerializeField] private Color hoverLaserColor = new Color(0.0f, 1.0f, 0.65f, 0.95f);
        [SerializeField] private Color activeLaserColor = new Color(1.0f, 0.65f, 0.10f, 1.0f);
        [SerializeField] private float beamWidth = 0.008f;

        [Header("Locomotion (Left Hand Only)")]
        [Tooltip("Smooth move speed in m/s. Only used when hand = LeftHand.")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Dead-zone for the locomotion thumbstick.")]
        [SerializeField] private float moveDeadzone = 0.2f;
        [Tooltip("Transform that is physically moved (XR Origin root). Auto-found if null.")]
        [SerializeField] private Transform xrOriginRoot;

        [Header("References")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private LevelResetter levelResetter;

        private LineRenderer _lineRenderer;
        private GameObject _reticleGo;
        private Material _laserMaterial;
        private Material _reticleMaterial;
        private GameObject _controllerModelGo;

        private InputDevice _targetDevice;
        private InputDevice _leftDevice;   // always tracked for locomotion on the left-hand instance
        private bool _wasTriggerPressed;
        private bool _wasPrimaryPressed;
        private bool _wasSecondaryPressed;
        private bool _wasGripPressed;
        private float _lastStickStepTime;
        private GameObject _lastHoveredObject;

        // Locomotion and tracking
        private Transform _headTransform;
        private float _footstepAccum;
        private AudioSource _footstepSource;
        private AudioClip _footstepClip;
        private const float FootstepDistance = 1.4f;
        private OpenXrNodePose _headPose;
        private OpenXrNodePose _handPose;
        private InputAction _stickThumb;
        private InputAction _stickAxis;
        private bool _renderHooked;
        private readonly RaycastHit[] _rayHits = new RaycastHit[16];

        private XRNode TargetXRNode => hand == ControllerHand.RightHand ? XRNode.RightHand : XRNode.LeftHand;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (levelResetter == null) levelResetter = FindAnyObjectByType<LevelResetter>();

            // Locate XR Origin root for locomotion (walk up from camera until we hit the rig root)
            if (xrOriginRoot == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    // Walk up: Camera â†’ CameraOffset â†’ XROrigin
                    Transform t = cam.transform;
                    while (t.parent != null) t = t.parent;
                    xrOriginRoot = t;
                    _headTransform = cam.transform;
                }
            }
            else
            {
                _headTransform = Camera.main != null ? Camera.main.transform : transform;
            }

            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake = false;
            _footstepSource.spatialBlend = 0f;
            _footstepSource.volume = 0.25f;
            _footstepClip = CreateFootstepClip();

            _headPose = new OpenXrNodePose("<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", XRNode.CenterEye);
            string handRole = hand == ControllerHand.RightHand ? "RightHand" : "LeftHand";
            _handPose = new OpenXrNodePose(
                $"<XRController>{{{handRole}}}/devicePosition",
                $"<XRController>{{{handRole}}}/deviceRotation",
                TargetXRNode);

            if (hand == ControllerHand.LeftHand)
            {
                _stickThumb = CreateStickAction("<XRController>{LeftHand}/thumbstick");
                _stickAxis = CreateStickAction("<XRController>{LeftHand}/primary2DAxis");
            }

            SetupVisuals();
        }

        private void OnEnable()
        {
            if (_renderHooked) return;
            Application.onBeforeRender += ApplyHeadPoseBeforeRender;
            _renderHooked = true;
        }

        private void OnDisable()
        {
            if (!_renderHooked) return;
            Application.onBeforeRender -= ApplyHeadPoseBeforeRender;
            _renderHooked = false;
        }

        private void OnDestroy()
        {
            _headPose?.Dispose();
            _handPose?.Dispose();
            DisposeAction(_stickThumb);
            DisposeAction(_stickAxis);
        }

        private static InputAction CreateStickAction(string binding)
        {
            try
            {
                var action = new InputAction(
                    name: binding,
                    type: InputActionType.Value,
                    binding: binding,
                    expectedControlType: "Vector2");
                action.Enable();
                return action;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[VRControllerPointerInteractor] Stick binding '{binding}' unavailable: {exception.Message}");
                return null;
            }
        }

        private static void DisposeAction(InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
        }

        private void Start()
        {
            RefreshInputDevice();
            _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        private void LateUpdate()
        {
            if (!_targetDevice.isValid)
                RefreshInputDevice();

            ApplyHeadPose();
            ApplyHandPose();

            if (hand == ControllerHand.LeftHand)
            {
                if (!_leftDevice.isValid)
                    _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                ProcessLocomotion();
            }

            ProcessRaycastAndInteraction();
        }

        private void ApplyHeadPoseBeforeRender()
        {
            ApplyHeadPose();
        }

        /// <summary>
        /// Writes the headset pose onto the camera. Nothing in this method recenters yaw.
        /// A near-zero position means a head-relative tracking origin, so the authored eye height stays.
        /// </summary>
        private void ApplyHeadPose()
        {
            if (_headTransform == null && Camera.main != null)
                _headTransform = Camera.main.transform;
            if (_headTransform == null || _headPose == null)
                return;
            if (!_headPose.TryRead(out Vector3 position, out Quaternion rotation))
                return;

            _headTransform.localRotation = rotation;
            if (position.sqrMagnitude > 0.01f)
                _headTransform.localPosition = position;

            DesktopInputFallback fallback = _headTransform.GetComponent<DesktopInputFallback>();
            if (fallback != null && fallback.enabled)
                fallback.enabled = false;
        }

        private void ApplyHandPose()
        {
            if (_handPose == null || !_handPose.TryRead(out Vector3 position, out Quaternion rotation))
                return;

            transform.localRotation = rotation;
            if (position.sqrMagnitude > 0.0004f)
                transform.localPosition = position;
        }

        private void RefreshInputDevice()
        {
            _targetDevice = InputDevices.GetDeviceAtXRNode(TargetXRNode);
        }

        /// <summary>
        /// Smooth locomotion driven by the left thumbstick.
        /// Direction is head-relative so the player walks where they look.
        /// Only runs on the LeftHand instance.
        /// </summary>
        private void ProcessLocomotion()
        {
            if (xrOriginRoot == null) return;
            if (!TryReadMoveStick(out Vector2 stick) || stick.magnitude < moveDeadzone)
            {
                xrOriginRoot.position = PlayspaceMotor.Move(xrOriginRoot.position, Vector3.zero);
                return;
            }

            Transform heading = _headTransform != null ? _headTransform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(heading.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 moveDir = (forward * stick.y) + (right * stick.x);
            if (moveDir.sqrMagnitude < 1e-6f) return;
            moveDir.Normalize();

            float distance = moveSpeed * stick.magnitude * Time.deltaTime;
            xrOriginRoot.position = PlayspaceMotor.Move(xrOriginRoot.position, moveDir * distance);

            _footstepAccum += distance;
            if (_footstepAccum >= FootstepDistance)
            {
                _footstepAccum -= FootstepDistance;
                PlayFootstep();
            }
        }

        private bool TryReadMoveStick(out Vector2 stick)
        {
            if (_stickThumb != null && _stickThumb.enabled && _stickThumb.activeControl != null)
            {
                stick = _stickThumb.ReadValue<Vector2>();
                return true;
            }

            if (_stickAxis != null && _stickAxis.enabled && _stickAxis.activeControl != null)
            {
                stick = _stickAxis.ReadValue<Vector2>();
                return true;
            }

            stick = Vector2.zero;
            return _leftDevice.isValid && _leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);
        }

        private void PlayFootstep()
        {
            if (_footstepSource == null || _footstepClip == null) return;
            _footstepSource.PlayOneShot(_footstepClip, 0.25f);
        }

        private static AudioClip CreateFootstepClip()
        {
            const int rate = 44100;
            const float duration = 0.08f;
            int count = (int)(rate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * 90f * t) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Footstep", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void SetupVisuals()
        {
            // Laser Line Renderer
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            Shader unlitShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            _laserMaterial = new Material(unlitShader);
            SetMaterialColorSafe(_laserMaterial, normalLaserColor);

            _lineRenderer.material = _laserMaterial;
            _lineRenderer.startWidth = beamWidth;
            _lineRenderer.endWidth = beamWidth * 0.4f;
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;

            // Reticle / Hit Marker Sphere
            _reticleGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _reticleGo.name = $"{gameObject.name}_Reticle";
            _reticleGo.transform.SetParent(transform, false);
            _reticleGo.transform.localScale = Vector3.one * 0.035f;

            var reticleCollider = _reticleGo.GetComponent<Collider>();
            if (reticleCollider != null) Destroy(reticleCollider);

            _reticleMaterial = new Material(unlitShader);
            SetMaterialColorSafe(_reticleMaterial, normalLaserColor);
            var reticleRenderer = _reticleGo.GetComponent<MeshRenderer>();
            if (reticleRenderer != null)
            {
                reticleRenderer.material = _reticleMaterial;
            }

            // Stylized 3D Sci-Fi Controller Body
            CreateControllerModel();
        }

        private void CreateControllerModel()
        {
            if (_controllerModelGo != null) return;

            _controllerModelGo = new GameObject($"{gameObject.name}_ModelRoot");
            _controllerModelGo.transform.SetParent(transform, false);

            Shader standardShader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var bodyMat = new Material(standardShader);
            SetMaterialColorSafe(bodyMat, new Color(0.16f, 0.18f, 0.24f));
            if (bodyMat.HasProperty("_Metallic")) bodyMat.SetFloat("_Metallic", 0.85f);
            if (bodyMat.HasProperty("_Glossiness")) bodyMat.SetFloat("_Glossiness", 0.70f);

            var glowMat = new Material(standardShader);
            Color accentColor = hand == ControllerHand.RightHand ? new Color(0.20f, 0.80f, 1.0f) : new Color(1.0f, 0.65f, 0.15f);
            SetMaterialColorSafe(glowMat, accentColor);
            glowMat.EnableKeyword("_EMISSION");
            if (glowMat.HasProperty("_EmissionColor")) glowMat.SetColor("_EmissionColor", accentColor * 1.8f);

            // Handle Grip
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "ControllerHandle";
            grip.transform.SetParent(_controllerModelGo.transform, false);
            grip.transform.localPosition = new Vector3(0, -0.045f, -0.05f);
            grip.transform.localRotation = Quaternion.Euler(55f, 0, 0);
            grip.transform.localScale = new Vector3(0.030f, 0.060f, 0.030f);
            grip.GetComponent<Renderer>().material = bodyMat;
            var c = grip.GetComponent<Collider>();
            if (c != null) Destroy(c);

            // Forward Emitter Nozzle
            var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nozzle.name = "LaserEmitterNozzle";
            nozzle.transform.SetParent(_controllerModelGo.transform, false);
            nozzle.transform.localPosition = new Vector3(0, 0, 0.015f);
            nozzle.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            nozzle.transform.localScale = new Vector3(0.018f, 0.020f, 0.018f);
            nozzle.GetComponent<Renderer>().material = glowMat;
            var nc = nozzle.GetComponent<Collider>();
            if (nc != null) Destroy(nc);

            // Halo Guard Ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TrackingHaloRing";
            ring.transform.SetParent(_controllerModelGo.transform, false);
            ring.transform.localPosition = new Vector3(0, 0.025f, -0.025f);
            ring.transform.localRotation = Quaternion.Euler(30f, 0, 0);
            ring.transform.localScale = new Vector3(0.095f, 0.005f, 0.095f);
            ring.GetComponent<Renderer>().material = bodyMat;
            var rc = ring.GetComponent<Collider>();
            if (rc != null) Destroy(rc);
        }

        private void SetMaterialColorSafe(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        /// <summary>
        /// Closest solid hit from the controller. Skips the rig itself so the body capsule
        /// and controller mesh do not eat the beam. Walls and consoles still block it.
        /// </summary>
        private bool TryRaycast(Ray ray, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(
                ray,
                _rayHits,
                maxRayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _rayHits[i].collider;
                if (collider == null || collider.isTrigger)
                    continue;
                if (collider.transform == transform || collider.transform.IsChildOf(transform))
                    continue;
                if (xrOriginRoot != null &&
                    collider.transform.IsChildOf(xrOriginRoot) &&
                    !IsInteractable(collider))
                    continue;

                if (_rayHits[i].distance < bestDistance)
                {
                    bestDistance = _rayHits[i].distance;
                    best = _rayHits[i];
                    found = true;
                }
            }

            return found;
        }

        private void ProcessRaycastAndInteraction()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            bool hasHit = TryRaycast(ray, out RaycastHit hit);

            Vector3 endPos = hasHit ? hit.point : transform.position + (transform.forward * maxRayDistance);

            // Update Laser visuals
            if (_lineRenderer != null)
            {
                _lineRenderer.SetPosition(0, transform.position);
                _lineRenderer.SetPosition(1, endPos);
            }

            if (_reticleGo != null)
            {
                _reticleGo.transform.position = endPos;
                _reticleGo.SetActive(hasHit);
            }

            // Check Hover target
            GameObject currentTarget = hasHit ? hit.collider.gameObject : null;
            bool isHoveringInteractable = false;

            if (hasHit)
            {
                isHoveringInteractable = IsInteractable(hit.collider);
                if (currentTarget != _lastHoveredObject && isHoveringInteractable)
                {
                    // Light haptic tick on hover enter
                    SendHaptic(0.15f, 0.02f);
                    InteractionAudioBus.RaiseHoverEnter();
                }
            }
            _lastHoveredObject = currentTarget;

            // Update color based on hover state
            Color activeColor = isHoveringInteractable ? hoverLaserColor : normalLaserColor;

            // Read Controller Buttons
            bool isTriggerDown = false;
            bool isTriggerJustPressed = false;
            bool isPrimaryJustPressed = false;
            bool isSecondaryJustPressed = false;
            bool isGripJustPressed = false;
            Vector2 thumbstick = Vector2.zero;

            if (_targetDevice.isValid)
            {
                if (_targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool trgBtn))
                {
                    isTriggerDown = trgBtn;
                }
                else if (_targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float trgVal))
                {
                    isTriggerDown = trgVal > 0.4f;
                }

                if (isTriggerDown && !_wasTriggerPressed)
                {
                    isTriggerJustPressed = true;
                }
                _wasTriggerPressed = isTriggerDown;

                if (_targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primBtn))
                {
                    if (primBtn && !_wasPrimaryPressed) isPrimaryJustPressed = true;
                    _wasPrimaryPressed = primBtn;
                }

                if (_targetDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secBtn))
                {
                    if (secBtn && !_wasSecondaryPressed) isSecondaryJustPressed = true;
                    _wasSecondaryPressed = secBtn;
                }

                if (_targetDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool grpBtn))
                {
                    if (grpBtn && !_wasGripPressed) isGripJustPressed = true;
                    _wasGripPressed = grpBtn;
                }

                _targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick);
            }

            // Handle Trigger / Primary click interaction
            if (isTriggerJustPressed || isPrimaryJustPressed)
            {
                activeColor = activeLaserColor;
                if (hasHit)
                {
                    ExecuteInteract(hit.collider);
                }
            }

            // Handle Secondary button (e.g. Forward Pass / Pulse)
            if (isSecondaryJustPressed)
            {
                if (chamberController != null)
                {
                    chamberController.TriggerForwardPass();
                    SendHaptic(0.4f, 0.08f);
                }
            }

            // Handle Grip button (e.g. Reset or Cable disconnect)
            if (isGripJustPressed && hasHit)
            {
                var cable = hit.collider.GetComponentInParent<CableInteractable>();
                if (cable != null)
                {
                    cable.ToggleConnection();
                    SendHaptic(0.3f, 0.05f);
                }
            }

            // Handle Thumbstick Stepping for Hovered Regulators/Sliders/Dials
            if (hasHit && Mathf.Abs(thumbstick.x) > 0.5f && (Time.time - _lastStickStepTime > 0.25f))
            {
                int step = thumbstick.x > 0 ? 1 : -1;
                if (ExecuteThumbstickStep(hit.collider, step))
                {
                    _lastStickStepTime = Time.time;
                    SendHaptic(0.25f, 0.04f);
                }
            }

            // Apply color to materials
            SetMaterialColorSafe(_laserMaterial, activeColor);
            SetMaterialColorSafe(_reticleMaterial, activeColor);
        }

        private bool IsInteractable(Collider col)
        {
            if (col == null) return false;
            return col.GetComponentInParent<KineticWeightSliderInteractor>() != null ||
                   col.GetComponentInParent<WeightRegulatorInteractor>() != null ||
                   col.GetComponentInParent<BiasDialInteractor>() != null ||
                   col.GetComponentInParent<ClockPulseLeverInteractor>() != null ||
                   col.GetComponentInParent<ActivationSocketInteractor>() != null ||
                   col.GetComponentInParent<InputTerminalInteractor>() != null ||
                   col.GetComponentInParent<CableInteractable>() != null ||
                   col.GetComponentInParent<DataTargetReceptor>() != null;
        }

        private void ExecuteInteract(Collider col)
        {
            if (col == null) return;

            var slider = col.GetComponentInParent<KineticWeightSliderInteractor>();
            if (slider != null)
            {
                slider.OnXRInteract();
                SendHaptic(0.35f, 0.05f);
                InteractionAudioBus.RaiseInteractClick();
                return;
            }

            var wReg = col.GetComponentInParent<WeightRegulatorInteractor>();
            if (wReg != null)
            {
                wReg.StepAdjust(1);
                SendHaptic(0.35f, 0.05f);
                InteractionAudioBus.RaiseInteractClick();
                return;
            }

            var biasDial = col.GetComponentInParent<BiasDialInteractor>();
            if (biasDial != null)
            {
                biasDial.StepAdjust(1);
                SendHaptic(0.35f, 0.05f);
                InteractionAudioBus.RaiseInteractClick();
                return;
            }

            var socket = col.GetComponentInParent<ActivationSocketInteractor>();
            if (socket != null)
            {
                socket.CycleCrystal();
                SendHaptic(0.45f, 0.06f);
                InteractionAudioBus.RaiseSocketCycle();
                return;
            }

            var lever = col.GetComponentInParent<ClockPulseLeverInteractor>();
            if (lever != null)
            {
                lever.PullLever();
                SendHaptic(0.5f, 0.08f);
                InteractionAudioBus.RaiseLeverPull();
                return;
            }

            var terminal = col.GetComponentInParent<InputTerminalInteractor>();
            if (terminal != null)
            {
                terminal.ToggleValue();
                SendHaptic(0.3f, 0.04f);
                InteractionAudioBus.RaiseInteractClick();
                return;
            }

            var cable = col.GetComponentInParent<CableInteractable>();
            if (cable != null)
            {
                cable.ToggleConnection();
                SendHaptic(0.35f, 0.05f);
                InteractionAudioBus.RaiseCableToggle();
                return;
            }

            var receptor = col.GetComponentInParent<DataTargetReceptor>();
            if (receptor != null && chamberController != null)
            {
                chamberController.TriggerSingleCasePass(receptor.CaseIndex);
                SendHaptic(0.4f, 0.06f);
                InteractionAudioBus.RaiseInteractClick();
                return;
            }
        }

        private bool ExecuteThumbstickStep(Collider col, int step)
        {
            if (col == null) return false;

            var slider = col.GetComponentInParent<KineticWeightSliderInteractor>();
            if (slider != null)
            {
                slider.StepAdjust(step);
                InteractionAudioBus.RaiseDetentStep(step);
                return true;
            }

            var wReg = col.GetComponentInParent<WeightRegulatorInteractor>();
            if (wReg != null)
            {
                wReg.StepAdjust(step);
                InteractionAudioBus.RaiseDetentStep(step);
                return true;
            }

            var biasDial = col.GetComponentInParent<BiasDialInteractor>();
            if (biasDial != null)
            {
                biasDial.StepAdjust(step);
                InteractionAudioBus.RaiseDetentStep(step);
                return true;
            }

            return false;
        }

        private void SendHaptic(float amplitude, float duration)
        {
            if (_targetDevice.isValid)
            {
                _targetDevice.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}

