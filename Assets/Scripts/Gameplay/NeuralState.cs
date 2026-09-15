using System;
using UnityEngine;
using Convergence.Core.Neural;

namespace Convergence.Gameplay
{
    /// <summary>
    /// MonoBehaviour bridge between pure C# Core neural models and the Unity GameObject hierarchy.
    /// Manages the live NetworkModel and dispatches state change events.
    /// </summary>
    public class NeuralState : MonoBehaviour
    {
        private NetworkModel _network;
        private bool[] _cablesConnected = new bool[] { true, true };

        public NetworkModel Network => _network;

        public double Weight1 => _network?.SingleNeuron?.GetWeight(0) ?? 0.0;
        public double Weight2 => _network?.SingleNeuron?.GetWeight(1) ?? 0.0;
        public double Bias => _network?.SingleNeuron?.Bias ?? 0.0;
        public ActivationType Activation => _network?.SingleNeuron?.Activation ?? ActivationType.Linear;

        public bool Cable1Connected => _cablesConnected[0];
        public bool Cable2Connected => _cablesConnected[1];

        // State change events
        public event Action<int, double> OnWeightChanged;
        public event Action<double> OnBiasChanged;
        public event Action<ActivationType> OnActivationChanged;
        public event Action<int, bool> OnCableStateChanged;
        public event Action OnStateMutated;

        private void Awake()
        {
            if (_network == null)
            {
                Initialize(NetworkModel.CreateSingleNeuronNetwork(2));
            }
        }

        /// <summary>
        /// Replaces the underlying network with a new model.
        /// </summary>
        public void Initialize(NetworkModel network)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            OnStateMutated?.Invoke();
        }

        public void SetWeight(int index, double value)
        {
            if (_network?.SingleNeuron == null) return;
            if (index < 0 || index >= _network.SingleNeuron.WeightCount) return;

            _network.SingleNeuron.SetWeight(index, value);
            OnWeightChanged?.Invoke(index, value);
            OnStateMutated?.Invoke();
        }

        public void SetBias(double value)
        {
            if (_network?.SingleNeuron == null) return;

            _network.SingleNeuron.Bias = value;
            OnBiasChanged?.Invoke(value);
            OnStateMutated?.Invoke();
        }

        public void SetActivation(ActivationType activation)
        {
            if (_network?.SingleNeuron == null) return;

            _network.SingleNeuron.Activation = activation;
            OnActivationChanged?.Invoke(activation);
            OnStateMutated?.Invoke();
        }

        public void SetCableConnected(int index, bool connected)
        {
            if (index < 0 || index >= _cablesConnected.Length) return;

            _cablesConnected[index] = connected;
            OnCableStateChanged?.Invoke(index, connected);
            OnStateMutated?.Invoke();
        }

        /// <summary>
        /// Reads effective input vector taking cable disconnections into account.
        /// Disconnected cable drops the corresponding input signal to 0.0.
        /// </summary>
        public double[] FilterInputsByCables(double[] rawInputs)
        {
            if (rawInputs == null) return Array.Empty<double>();
            double[] filtered = (double[])rawInputs.Clone();

            for (int i = 0; i < filtered.Length && i < _cablesConnected.Length; i++)
            {
                if (!_cablesConnected[i])
                {
                    filtered[i] = 0.0;
                }
            }

            return filtered;
        }
    }
}
