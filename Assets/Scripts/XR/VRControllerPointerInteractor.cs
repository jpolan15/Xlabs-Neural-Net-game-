using System;
using System.Collections.Generic;
using UnityEngine;
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

        // Locomotion
        private Transform _headTransform;  // Camera / XR head
        private float _footstepAccum;      // distance walked this cycle
        private AudioSource _footstepSource;
        private const float FootstepDistance = 1.4f; // metres per step sound

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

            // Footstep audio source (2-D, no spatial blend needed â€” it's a body sound)
            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake = false;
            _footstepSource.spatialBlend = 0f;
            _footstepSource.volume = 0.25f;

            SetupVisuals();
        }

        private void Start()
        {
            RefreshInputDevice();
            _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        private void Update()
        {
            if (!_targetDevice.isValid)
            {
                RefreshInputDevice();
            }

            // Update physical transform from XR tracking if device has pose
            if (_targetDevice.isValid)
            {
                if (_targetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                {
                    transform.localPosition = pos;
                }
                if (_targetDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                {
                    transform.localRotation = rot;
                }
            }

            // Left-hand controller always tracks for locomotion, regardless of which hand this instance is
            if (hand == ControllerHand.LeftHand)
            {
                if (!_leftDevice.isValid)
                    _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                ProcessLocomotion();
            }

            ProcessRaycastAndInteraction();
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
            if (xrOriginRoot == null || _headTransform == null) return;

            Vector2 stick = Vector2.zero;
            _leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);

            if (stick.magnitude < moveDeadzone) return;

            // Head-relative horizontal direction (no pitch)
            Vector3 forward = Vector3.ProjectOnPlane(_headTransform.forward, Vector3.up).normalized;
            Vector3 right   = Vector3.ProjectOnPlane(_headTransform.right,   Vector3.up).normalized;
            Vector3 moveDir = (forward * stick.y + right * stick.x).normalized;

            float distance = moveSpeed * stick.magnitude * Time.deltaTime;
            xrOriginRoot.position += moveDir * distance;

            // Footstep sound accumulator
            _footstepAccum += distance;
            if (_footstepAccum >= FootstepDistance)
            {
                _footstepAccum -= FootstepDistance;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            if (_footstepSource == null) return;

            // Procedural dull thud: low-frequency sine with fast decay
            int rate = 44100;
            float dur = 0.08f;
            int count = (int)(rate * dur);
            float[] s = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Clamp01(1f - t / dur);
                s[i] = Mathf.Sin(2f * Mathf.PI * 90f * t) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("Footstep", count, 1, rate, false);
            clip.SetData(s, 0);
            _footstepSource.PlayOneShot(clip, 0.25f);
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

        private void ProcessRaycastAndInteraction()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance);

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

