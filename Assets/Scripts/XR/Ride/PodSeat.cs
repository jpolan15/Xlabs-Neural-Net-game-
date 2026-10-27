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
    /// Normalizes the eye so it sits targetEyeHeight right above the seat (height and x/z) and the dash and the game
    /// table are always the same size and place in view. It re-applies whenever the headset goes back on the head (user
    /// presence; never required, Link reports it wrong), on every tracking origin update (recenter), when the eye has
    /// stayed somewhat off for a while with the head still and no game running (never mid-lean or mid-game), and soon
    /// after the eye is far off (over 0.6 m, game or not), so a height measured while the headset was still in a
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
        [Tooltip("Re-seat, even mid-game, when the eye has been this far from the seat (up/down or sideways)...")]
        [SerializeField] private float resetTolerance = 0.6f;
        [Tooltip("...for this many seconds in a row.")]
        [SerializeField] private float resetSeconds = 0.75f;

        private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>(1);
        private XRInputSubsystem inputSubsystem;
        private Transform headCamera;
        private bool eyeHeightPending = true;
        private bool wasPresent;
        private float driftSince = -1f;
        private float farSince = -1f;
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

            // Headset put back on: measure again, the old height was taken without a head in it. Presence only triggers a
            // new measure and never blocks one: over Quest Link it reads "not present" while worn (2026-10-27), and the
            // seat never corrected, so the rider sat 1.3 m too high with every board 78 degrees down.
            bool present = UserPresent();
            if (present && !wasPresent) eyeHeightPending = true;
            wasPresent = present;

            if (eyeHeightPending)
            {
                NormalizeEyeHeight();
                return;
            }

            // Far off (a height taken before the head pose settled, a rider who stood up): re-seat soon, game or not.
            Vector3 eye = seatAnchor.InverseTransformPoint(headCamera.position);
            float off = Mathf.Max(Mathf.Abs(eye.y - targetEyeHeight), new Vector2(eye.x, eye.z).magnitude);
            if (off < resetTolerance) farSince = -1f;
            else if (farSince < 0f) farSince = Time.unscaledTime;
            else if (Time.unscaledTime - farSince > resetSeconds)
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

            // The eye goes to targetEyeHeight right above the seat: the height, and the x/z too (a rider sitting 33 cm ahead
            // of the play area's centre had the table at half its distance). Converted into the offset's parent space.
            Vector3 eye = seatAnchor.InverseTransformPoint(headCamera.position);
            Vector3 worldShift = seatAnchor.TransformVector(new Vector3(-eye.x, targetEyeHeight - eye.y, -eye.z));
            Transform parent = cameraFloorOffset.parent;
            cameraFloorOffset.localPosition += parent != null ? parent.InverseTransformVector(worldShift) : worldShift;
            eyeHeightPending = false;
            driftSince = -1f;
            farSince = -1f;
        }
    }
}
