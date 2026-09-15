using System;
using UnityEngine;

namespace Convergence.XR
{
    /// <summary>
    /// Futuristic Arc Blade / Signal Saber tool.
    /// Used to cut or reconnect broken conduits and tap calibration rings.
    /// </summary>
    public class ArcBladeInteractor : MonoBehaviour
    {
        [Header("Blade Settings")]
        [SerializeField] private Transform bladeTip;
        [SerializeField] private float reach = 1.8f;
        [SerializeField] private LayerMask interactableMask = ~0;

        public event Action<Vector3> OnBladeHit;

        private void Awake()
        {
            if (bladeTip == null)
            {
                bladeTip = transform;
            }
        }

        /// <summary>
        /// Executes a slash or tap interaction along the blade vector.
        /// </summary>
        public void Strike()
        {
            Vector3 origin = bladeTip.position;
            Vector3 direction = bladeTip.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, reach, interactableMask))
            {
                OnBladeHit?.Invoke(hit.point);

                // Check for CableInteractable
                var cable = hit.collider.GetComponentInParent<CableInteractable>();
                if (cable != null)
                {
                    cable.ToggleConnection();
                    return;
                }

                // Check for WeightRegulator
                var regulator = hit.collider.GetComponentInParent<WeightRegulatorInteractor>();
                if (regulator != null)
                {
                    regulator.StepAdjust(1);
                    return;
                }

                // Check for BiasDial
                var biasDial = hit.collider.GetComponentInParent<BiasDialInteractor>();
                if (biasDial != null)
                {
                    biasDial.StepAdjust(1);
                    return;
                }
            }
        }
    }
}
