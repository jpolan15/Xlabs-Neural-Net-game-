using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Void-returning relay so a world-space UI button (pressed through XRI's TrackedDeviceGraphicRaycaster)
    /// can request a forward pass. It only asks Gameplay to run the test; it never evaluates anything.
    /// </summary>
    public sealed class UiForwardPassRelay : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }
        }

        public void RequestForwardPass()
        {
            if (chamberController != null)
            {
                chamberController.TriggerForwardPass();
                InteractionAudioBus.RaiseInteractClick();
            }
        }
    }
}
