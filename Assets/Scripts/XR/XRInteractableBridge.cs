using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// XRI interactable that translates ray/near select events into the gameplay commands exposed by the
    /// first-party interactors on the same GameObject (ADR-007). Dials and sliders respond to wrist twist
    /// while held; a quick tap cycles them one detent. Every other control acts on select.
    /// The bridge never raycasts and never reads input devices; XRI supplies the interactor.
    /// </summary>
    public sealed class XRInteractableBridge : XRSimpleInteractable
    {
        [Header("Twist Dials")]
        [Tooltip("Wrist rotation, in degrees, that advances a dial by one detent.")]
        [SerializeField] private float degreesPerStep = 18f;
        [SerializeField] private float tapMaxSeconds = 0.35f;
        [SerializeField] private float tapMaxDegrees = 8f;

        [Header("Haptics")]
        [SerializeField] private float hoverHapticAmplitude = 0.12f;
        [SerializeField] private float hoverHapticDuration = 0.02f;
        [SerializeField] private float selectHapticAmplitude = 0.4f;
        [SerializeField] private float selectHapticDuration = 0.05f;
        [SerializeField] private float detentHapticAmplitude = 0.25f;
        [SerializeField] private float detentHapticDuration = 0.03f;

        [Header("References")]
        [SerializeField] private ChamberController chamberController;

        private KineticWeightSliderInteractor _slider;
        private WeightRegulatorInteractor _regulator;
        private BiasDialInteractor _biasDial;
        private ClockPulseLeverInteractor _lever;
        private ActivationSocketInteractor _socket;
        private InputTerminalInteractor _terminal;
        private CableInteractable _cable;
        private DataTargetReceptor _receptor;
        private PhotoRequest _photo;
        private VoyageButton _voyageButton;

        private IXRSelectInteractor _holder;
        private float _selectStartTime;
        private float _accumulatedDegrees;
        private float _totalAbsDegrees;
        private Vector3 _lastPlanar;

        /// <summary>Raised after the bridge has forwarded an interaction to gameplay. Payload: detent step or 0 for a click.</summary>
        public event Action<XRInteractableBridge, int> OnBridgeInteraction;

        public bool IsDial => _slider != null || _regulator != null || _biasDial != null;

        /// <summary>Desktop play uses the same gameplay path as a controller tap.</summary>
        public void Activate() => ExecuteClick();

        /// <summary>Desktop play uses the same detent path as a wrist twist. Ignored for controls that are not dials.</summary>
        public void Step(int steps)
        {
            if (steps != 0 && IsDial)
            {
                ApplyDetent(steps > 0 ? 1 : -1);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _slider = GetComponent<KineticWeightSliderInteractor>();
            _regulator = GetComponent<WeightRegulatorInteractor>();
            _biasDial = GetComponent<BiasDialInteractor>();
            _lever = GetComponent<ClockPulseLeverInteractor>();
            _socket = GetComponent<ActivationSocketInteractor>();
            _terminal = GetComponent<InputTerminalInteractor>();
            _cable = GetComponent<CableInteractable>();
            _receptor = GetComponent<DataTargetReceptor>();
            _photo = GetComponent<PhotoRequest>();
            _voyageButton = GetComponent<VoyageButton>();

            if (_receptor != null && chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            Haptic(args.interactorObject, hoverHapticAmplitude, hoverHapticDuration);
            InteractionAudioBus.RaiseHoverEnter();
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (IsDial)
            {
                _holder = args.interactorObject;
                _selectStartTime = Time.time;
                _accumulatedDegrees = 0f;
                _totalAbsDegrees = 0f;
                _lastPlanar = SamplePlanar(_holder);
                return;
            }

            ExecuteClick();
            Haptic(args.interactorObject, selectHapticAmplitude, selectHapticDuration);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            if (_holder == null || !ReferenceEquals(_holder, args.interactorObject))
            {
                return;
            }

            bool wasTap = _totalAbsDegrees < tapMaxDegrees && (Time.time - _selectStartTime) <= tapMaxSeconds;
            _holder = null;

            if (wasTap && !args.isCanceled)
            {
                ExecuteClick();
                Haptic(args.interactorObject, selectHapticAmplitude, selectHapticDuration);
            }
        }

        private void Update()
        {
            if (_holder == null)
            {
                return;
            }

            Vector3 planar = SamplePlanar(_holder);
            if (planar.sqrMagnitude < 0.01f || _lastPlanar.sqrMagnitude < 0.01f)
            {
                _lastPlanar = planar;
                return;
            }

            float delta = Vector3.SignedAngle(_lastPlanar, planar, transform.up);
            _lastPlanar = planar;
            _accumulatedDegrees += delta;
            _totalAbsDegrees += Mathf.Abs(delta);

            while (Mathf.Abs(_accumulatedDegrees) >= degreesPerStep)
            {
                int step = _accumulatedDegrees > 0f ? 1 : -1;
                _accumulatedDegrees -= step * degreesPerStep;
                ApplyDetent(step);
                Haptic(_holder, detentHapticAmplitude, detentHapticDuration);
            }
        }

        private Vector3 SamplePlanar(IXRSelectInteractor interactor)
        {
            Transform attach = interactor.GetAttachTransform(this);
            return attach == null ? Vector3.zero : Vector3.ProjectOnPlane(attach.right, transform.up).normalized;
        }

        private void ApplyDetent(int step)
        {
            if (_slider != null) _slider.StepAdjust(step);
            else if (_regulator != null) _regulator.StepAdjust(step);
            else if (_biasDial != null) _biasDial.StepAdjust(step);
            else return;

            InteractionAudioBus.RaiseDetentStep(step);
            OnBridgeInteraction?.Invoke(this, step);
        }

        private void ExecuteClick()
        {
            if (_slider != null) { _slider.OnXRInteract(); InteractionAudioBus.RaiseInteractClick(); }
            else if (_regulator != null) { _regulator.OnXRInteract(); InteractionAudioBus.RaiseInteractClick(); }
            else if (_biasDial != null) { _biasDial.OnXRInteract(); InteractionAudioBus.RaiseInteractClick(); }
            else if (_socket != null) { _socket.CycleCrystal(); InteractionAudioBus.RaiseSocketCycle(); }
            else if (_lever != null) { _lever.PullLever(); InteractionAudioBus.RaiseLeverPull(); }
            else if (_terminal != null) { _terminal.ToggleValue(); InteractionAudioBus.RaiseInteractClick(); }
            else if (_cable != null) { _cable.ToggleConnection(); InteractionAudioBus.RaiseCableToggle(); }
            else if (_photo != null) { _photo.Request(); InteractionAudioBus.RaiseInteractClick(); }
            else if (_voyageButton != null) { _voyageButton.Activate(); InteractionAudioBus.RaiseInteractClick(); }
            else if (_receptor != null && chamberController != null)
            {
                chamberController.TriggerSingleCasePass(_receptor.CaseIndex);
                InteractionAudioBus.RaiseInteractClick();
            }
            else
            {
                return;
            }

            OnBridgeInteraction?.Invoke(this, 0);
        }

        private static void Haptic(IXRInteractor interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor input)
            {
                input.SendHapticImpulse(amplitude, duration);
            }
        }
    }
}
