using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using Convergence.Gameplay.Ride;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// Seats the XRI Starter Assets rig in the pod: parents it to the seat anchor and turns off every locomotion
    /// provider, so the player cannot teleport or turn out of the seat while the pod moves. The rig and its
    /// interactors are otherwise untouched (ADR-007).
    /// Normalizes eye height so the eye sits at targetEyeHeight above the pod floor and the dash is always the same
    /// size and place in view. It re-applies whenever the headset goes back on the head (user presence), on every
    /// tracking origin update (recenter), and when the eye has stayed far from the target for a while with the head
    /// still and no game running (never mid-lean or mid-game), so a height measured while the headset was still in a
    /// hand or on a table never sticks. The XR input subsystem is found
    /// lazily, because OpenXR can start it after this component is enabled. The tracking origin mode is never changed.
    /// </summary>
    public sealed class PodSeat : MonoBehaviour
    {
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform rig;
        [SerializeField] private Transform cameraFloorOffset;
        [SerializeField] private float targetEyeHeight = 1.30f;
        [Tooltip("Re-seat when the eye has been further than this from the target height (a lean to reach stays under it)...")]
        [SerializeField] private float driftTolerance = 0.35f;
        [Tooltip("...for this many seconds in a row.")]
        [SerializeField] private float driftSeconds = 3f;

        private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>(1);
        private XRInputSubsystem inputSubsystem;
        private Transform headCamera;
        private bool eyeHeightPending = true;
        private bool wasPresent;
        private float driftSince = -1f;
        private Vector3 lastHead;
        private RideDirector director;

        private void OnDisable()
        {
            if (inputSubsystem != null) inputSubsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            inputSubsystem = null;
        }

        private void OnEnable() => eyeHeightPending = true;

        private void Start()
        {
            if (rig == null && Camera.main != null) rig = Camera.main.transform.root;
            if (rig == null || seatAnchor == null)
            {
                Debug.LogError("[PodSeat] Needs the XR rig and a seat anchor.");
                return;
            }

            // The head camera sits under the camera floor offset (XR Origin > Camera Offset > Main Camera).
            if (Camera.main != null)
            {
                headCamera = Camera.main.transform;
                if (cameraFloorOffset == null) cameraFloorOffset = headCamera.parent;
            }

            if (rig.parent != seatAnchor)
            {
                rig.SetParent(seatAnchor, false);
                rig.localPosition = Vector3.zero;
                rig.localRotation = Quaternion.identity;
            }

            foreach (LocomotionProvider provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
            {
                provider.enabled = false;
            }

            var body = rig.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
        }

        private void Update()
        {
            if (inputSubsystem == null && !FindSubsystem()) return;
            if (!IsTracking()) return;

            // Headset put back on: measure again, the old height was taken without a head in it.
            bool present = UserPresent();
            if (present && !wasPresent) eyeHeightPending = true;
            wasPresent = present;
            if (!present) return;

            if (eyeHeightPending)
            {
                NormalizeEyeHeight();
                return;
            }

            float speed = (headCamera.localPosition - lastHead).magnitude / Mathf.Max(1e-4f, Time.unscaledDeltaTime);
            lastHead = headCamera.localPosition;
            if (director == null) director = FindAnyObjectByType<RideDirector>();
            bool busy = speed > 0.15f || (director != null && (director.ActiveMoment is RideGame || director.WaitingForGo));
            float error = Mathf.Abs(EyeAboveFloor() - targetEyeHeight);
            if (error < driftTolerance || busy) driftSince = -1f;
            else if (driftSince < 0f) driftSince = Time.unscaledTime;
            else if (Time.unscaledTime - driftSince > driftSeconds) NormalizeEyeHeight();
        }

        private bool FindSubsystem()
        {
            inputSubsystems.Clear();
            SubsystemManager.GetSubsystems(inputSubsystems);
            foreach (XRInputSubsystem candidate in inputSubsystems)
            {
                if (!candidate.running) continue;
                inputSubsystem = candidate;
                inputSubsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
                eyeHeightPending = true;
                return true;
            }

            return false;
        }

        private void OnTrackingOriginUpdated(XRInputSubsystem subsystem)
        {
            // Defer to Update so the head pose is already refreshed for the new origin.
            eyeHeightPending = true;
        }

        private bool IsTracking()
        {
            return headCamera != null && inputSubsystem != null && inputSubsystem.running
                && headCamera.localPosition != Vector3.zero;
        }

        /// <summary>True when the headset reports a head in it, or cannot tell (then assume it is worn).</summary>
        private static bool UserPresent()
        {
            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return !head.isValid || !head.TryGetFeatureValue(CommonUsages.userPresence, out bool present) || present;
        }

        private float EyeAboveFloor() => headCamera.position.y - seatAnchor.position.y;

        private void NormalizeEyeHeight()
        {
            if (headCamera == null || cameraFloorOffset == null || seatAnchor == null) return;

            float worldLift = targetEyeHeight - EyeAboveFloor();

            // Vertical only, converted into the offset's parent space, so the seated x/z position is never shifted.
            Transform parent = cameraFloorOffset.parent;
            float localLift = parent != null ? parent.InverseTransformVector(Vector3.up * worldLift).y : worldLift;
            cameraFloorOffset.localPosition += new Vector3(0f, localLift, 0f);
            eyeHeightPending = false;
            driftSince = -1f;
        }
    }
}
