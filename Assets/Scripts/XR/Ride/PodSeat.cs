using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// Seats the XRI Starter Assets rig in the pod: parents it to the seat anchor and turns off every locomotion
    /// provider, so the player cannot teleport or turn out of the seat while the pod moves. The rig and its
    /// interactors are otherwise untouched (ADR-007).
    /// Normalizes eye height: once the headset is tracking, and again on every tracking origin update (recenter),
    /// the camera floor offset is raised or lowered so the eye sits at targetEyeHeight above the pod floor.
    /// The tracking origin mode is never changed, and nothing is adjusted without a running XR input subsystem.
    /// </summary>
    public sealed class PodSeat : MonoBehaviour
    {
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform rig;
        [SerializeField] private Transform cameraFloorOffset;
        [SerializeField] private float targetEyeHeight = 1.30f;

        private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>(1);
        private XRInputSubsystem inputSubsystem;
        private Transform headCamera;
        private bool eyeHeightPending;

        private void OnEnable()
        {
            inputSubsystems.Clear();
            SubsystemManager.GetSubsystems(inputSubsystems);
            if (inputSubsystems.Count == 0) return;

            inputSubsystem = inputSubsystems[0];
            inputSubsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
            eyeHeightPending = true;
        }

        private void OnDisable()
        {
            if (inputSubsystem != null) inputSubsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            inputSubsystem = null;
            eyeHeightPending = false;
        }

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
            // Only waits for the first tracked frame after enable or recenter. Once applied, this is a single bool check.
            if (eyeHeightPending && IsTracking()) NormalizeEyeHeight();
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

        private void NormalizeEyeHeight()
        {
            if (headCamera == null || cameraFloorOffset == null || seatAnchor == null) return;

            float eyeAboveFloor = headCamera.position.y - seatAnchor.position.y;
            float worldLift = targetEyeHeight - eyeAboveFloor;

            // Vertical only, converted into the offset's parent space, so the seated x/z position is never shifted.
            Transform parent = cameraFloorOffset.parent;
            float localLift = parent != null ? parent.InverseTransformVector(Vector3.up * worldLift).y : worldLift;
            cameraFloorOffset.localPosition += new Vector3(0f, localLift, 0f);
            eyeHeightPending = false;
        }
    }
}
