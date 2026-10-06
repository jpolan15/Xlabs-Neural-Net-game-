using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Gameplay.Ride;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// The one control type on the pod dash: a handle that slides along one axis and snaps to detents.
    /// It turns where the hand (or the mouse) is along the track into a requested value and raises
    /// <see cref="UserMoved"/>; the scene wires that event to the channel. It never moves the handle itself;
    /// the lever view does, so a trainer driving the channel moves the handle the same way a hand does.
    /// </summary>
    public sealed class PodLeverInteractable : XRSimpleInteractable
    {
        [SerializeField] private RideControlChannel channel;
        [SerializeField] private Vector3 axis = Vector3.up;
        [SerializeField] private float halfTravel = 0.11f;
        [SerializeField] private float hoverHaptic = 0.1f;
        [SerializeField] private float detentHaptic = 0.3f;

        [Tooltip("Raised with the requested value (range units) while a hand or mouse drags the handle.")]
        [SerializeField] private UnityEvent<float> userMoved = new UnityEvent<float>();

        private IXRSelectInteractor _holder;
        private float _grabOffset;
        private float _startFraction;
        private float _largestMove;
        private bool _dragging;

        /// <summary>The channel this lever writes.</summary>
        public RideControlChannel Channel => channel;

        /// <summary>True while a hand or the mouse holds the handle.</summary>
        public bool IsGrabbed => _dragging;

        /// <summary>Raised with the requested value while dragging.</summary>
        public UnityEvent<float> UserMoved => userMoved;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (channel == null) return;
            channel.Changed += OnChannelChanged;
            channel.StateChanged += ApplyGate;
            ApplyGate(channel);
        }

        protected override void OnDisable()
        {
            if (channel != null)
            {
                channel.Changed -= OnChannelChanged;
                channel.StateChanged -= ApplyGate;
            }

            EndGrab();
            base.OnDisable();
        }

        /// <summary>0..1 position along the track for a world point, from the lever's own axis.</summary>
        public float FractionFromPoint(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return Mathf.InverseLerp(-halfTravel, halfTravel, Vector3.Dot(local, axis.normalized));
        }

        /// <summary>0..1 position along the track for the point on the track closest to a pointing ray.</summary>
        public float FractionFromRay(Ray ray)
        {
            Vector3 dir = transform.TransformDirection(axis.normalized);
            Vector3 w = transform.position - ray.origin;
            float b = Vector3.Dot(dir, ray.direction);
            float d = Vector3.Dot(dir, w);
            float e = Vector3.Dot(ray.direction, w);
            float denom = 1f - b * b;
            float s = denom < 1e-5f ? 0f : (b * e - d) / denom;
            return Mathf.InverseLerp(-halfTravel, halfTravel, s);
        }

        /// <summary>Starts a drag. The handle follows the pointer relative to where it was grabbed.</summary>
        public void BeginGrab(float pointerFraction)
        {
            if (channel == null || channel.Locked) return;
            _dragging = true;
            _grabOffset = pointerFraction - channel.Fraction;
            _startFraction = pointerFraction;
            _largestMove = 0f;
        }

        /// <summary>Continues a drag with the pointer's current track fraction.</summary>
        public void UpdateGrab(float pointerFraction)
        {
            if (!_dragging || channel == null) return;
            _largestMove = Mathf.Max(_largestMove, Mathf.Abs(pointerFraction - _startFraction));
            float fraction = Mathf.Clamp01(pointerFraction - _grabOffset);
            userMoved.Invoke(Mathf.Lerp(channel.Min, channel.Max, fraction));
        }

        /// <summary>Ends a drag.</summary>
        public void EndGrab()
        {
            bool wasTap = _dragging && _largestMove < 0.05f;
            _dragging = false;
            _holder = null;
            if (wasTap) Tap();
        }

        /// <summary>
        /// A click, or a squeeze and release, without dragging. A two-position lever (GO, LEARN) is pulled to its end.
        /// A three-position lever (the learning rate) steps to the next detent and wraps around. Others ignore a tap.
        /// </summary>
        public void Tap()
        {
            if (channel == null || channel.Locked) return;
            float range = channel.Max - channel.Min;
            if (range <= channel.Step * 1.01f)
            {
                if (channel.Value < channel.Max) userMoved.Invoke(channel.Max);
            }
            else if (range <= channel.Step * 2.01f)
            {
                float next = channel.Value + channel.Step;
                userMoved.Invoke(next > channel.Max + 0.001f ? channel.Min : next);
            }
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            Haptic(args.interactorObject, hoverHaptic, 0.02f);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            _holder = args.interactorObject;
            Transform pointer = _holder.transform;
            BeginGrab(FractionFromRay(new Ray(pointer.position, pointer.forward)));
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            EndGrab();
        }

        private void Update()
        {
            if (_holder == null || !_dragging) return;
            Transform pointer = _holder.transform;
            UpdateGrab(FractionFromRay(new Ray(pointer.position, pointer.forward)));
        }

        /// <summary>A locked or hidden lever cannot be hit: its colliders switch off.</summary>
        private void ApplyGate(RideControlChannel changed)
        {
            bool live = changed.Visible && !changed.Locked;
            foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = live;
            if (!live) EndGrab();
        }

        private void OnChannelChanged(RideControlChannel changed, bool byUser)
        {
            if (byUser && _holder != null) Haptic(_holder, detentHaptic, 0.03f);
        }

        private static void Haptic(IXRInteractor interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor input) input.SendHapticImpulse(amplitude, duration);
        }
    }
}
