using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Interactable conduit representing physical neural connections.
    /// Supports reconnecting/cutting conduits with tools.
    /// Updates NeuralState.CableConnected state.
    /// </summary>
    public class CableInteractable : MonoBehaviour
    {
        [Header("Cable Socket Identity")]
        [Tooltip("0 for Input 1 conduit, 1 for Input 2 conduit")]
        [SerializeField] private int cableIndex = 0;

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        public int CableIndex => cableIndex;
        public bool IsConnected => neuralState != null ? (cableIndex == 0 ? neuralState.Cable1Connected : neuralState.Cable2Connected) : true;

        public event Action<int, bool> OnConnectionStateChanged;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
        }

        public void Connect()
        {
            SetConnected(true);
        }

        public void Disconnect()
        {
            SetConnected(false);
        }

        public void ToggleConnection()
        {
            SetConnected(!IsConnected);
        }

        private void SetConnected(bool connected)
        {
            if (neuralState != null)
            {
                neuralState.SetCableConnected(cableIndex, connected);
            }
            OnConnectionStateChanged?.Invoke(cableIndex, connected);
        }
    }
}
