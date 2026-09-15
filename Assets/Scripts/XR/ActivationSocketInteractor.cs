using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Physical/XR socket interactor that accepts insertable activation crystals.
    /// Mutates NeuralState.Activation only upon physical insertion or removal of crystals.
    /// Never fakes or hardcodes activation state.
    /// </summary>
    public class ActivationSocketInteractor : MonoBehaviour
    {
        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        [Header("Socket Status")]
        [SerializeField] private bool hasCrystalInserted = true;
        [SerializeField] private ActivationType currentCrystalType = ActivationType.Linear;

        public bool HasCrystal => hasCrystalInserted;
        public ActivationType CurrentCrystal => currentCrystalType;

        public event Action<ActivationType> OnCrystalInserted;
        public event Action OnCrystalRemoved;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
        }

        private void Start()
        {
            if (neuralState != null)
            {
                // Reflect starting state from NeuralState
                currentCrystalType = neuralState.Activation;
                hasCrystalInserted = true;
            }
        }

        /// <summary>
        /// Inserts an activation crystal into the socket and updates the live network model.
        /// </summary>
        public void InsertCrystal(ActivationType type)
        {
            hasCrystalInserted = true;
            currentCrystalType = type;
            if (neuralState != null)
            {
                neuralState.SetActivation(type);
            }
            OnCrystalInserted?.Invoke(type);
        }

        /// <summary>
        /// Removes the crystal from the socket. Defaults to Linear (raw pre-activation pass-through).
        /// </summary>
        public void RemoveCrystal()
        {
            hasCrystalInserted = false;
            currentCrystalType = ActivationType.Linear;
            if (neuralState != null)
            {
                neuralState.SetActivation(ActivationType.Linear);
            }
            OnCrystalRemoved?.Invoke();
        }

        /// <summary>
        /// Convenience cycle method for desktop keyboard fallback (Tab key).
        /// Cycles through Linear -> ReLU -> Step -> Sigmoid.
        /// </summary>
        public void CycleCrystal()
        {
            ActivationType next;
            switch (currentCrystalType)
            {
                case ActivationType.Linear:
                    next = ActivationType.ReLU;
                    break;
                case ActivationType.ReLU:
                    next = ActivationType.Step;
                    break;
                case ActivationType.Step:
                    next = ActivationType.Sigmoid;
                    break;
                default:
                    next = ActivationType.Linear;
                    break;
            }
            InsertCrystal(next);
        }
    }
}
