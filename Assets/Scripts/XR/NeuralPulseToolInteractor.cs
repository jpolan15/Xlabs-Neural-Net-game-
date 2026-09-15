using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Handheld futuristic diagnostic tool that fires neural energy pulses
    /// into the neuron machine, triggering a full forward pass and evaluation.
    /// </summary>
    public class NeuralPulseToolInteractor : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private ChamberController chamberController;

        [Header("Tool Settings")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private float rayDistance = 25.0f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        public event Action<Vector3, Vector3> OnPulseFired; // muzzlePos, hitPos

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }
            if (muzzlePoint == null)
            {
                muzzlePoint = transform;
            }
        }

        /// <summary>
        /// Fires an energy pulse along the forward aim vector and triggers circuit evaluation.
        /// </summary>
        public void FirePulse()
        {
            Vector3 origin = muzzlePoint.position;
            Vector3 direction = muzzlePoint.forward;
            Vector3 hitPoint = origin + (direction * rayDistance);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance, interactableLayers))
            {
                hitPoint = hit.point;
            }

            OnPulseFired?.Invoke(origin, hitPoint);

            if (chamberController != null)
            {
                chamberController.TriggerForwardPass();
            }
        }
    }
}
