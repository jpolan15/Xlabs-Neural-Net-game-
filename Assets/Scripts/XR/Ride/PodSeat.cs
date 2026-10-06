using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// Seats the XRI Starter Assets rig in the pod: parents it to the seat anchor and turns off every locomotion
    /// provider, so the player cannot teleport or turn out of the seat while the pod moves. The rig and its
    /// interactors are otherwise untouched (ADR-007).
    /// </summary>
    public sealed class PodSeat : MonoBehaviour
    {
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform rig;

        private void Start()
        {
            if (rig == null && Camera.main != null) rig = Camera.main.transform.root;
            if (rig == null || seatAnchor == null)
            {
                Debug.LogError("[PodSeat] Needs the XR rig and a seat anchor.");
                return;
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
    }
}
