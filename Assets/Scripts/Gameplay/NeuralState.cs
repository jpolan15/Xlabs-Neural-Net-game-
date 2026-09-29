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
        private int _editLayer;
        private int _editNeuron;

        public NetworkModel Network => _network;

        public double Weight1 => ReadWeight(0);
        public double Weight2 => ReadWeight(1);
        public double Bias => EditableNeuron?.Bias ?? 0.0;
        public ActivationType Activation => EditableNeuron?.Activation ?? ActivationType.Linear;

        /// <summary>Layer whose weights the dials currently edit. A single neuron is always layer 0.</summary>
        public int EditLayer => _editLayer;

        /// <summary>Neuron inside <see cref="EditLayer"/> that the dials currently edit.</summary>
        public int EditNeuron => _editNeuron;

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

        /// <summary>
        /// Points the dials at one neuron. Ignored when the index is outside the live network.
        /// </summary>
        public void SelectEditableNeuron(int layer, int neuron)
        {
            if (_network == null) return;
            if (layer < 0 || layer >= _network.Layers.Count) return;
            if (neuron < 0 || neuron >= _network.Layers[layer].NeuronCount) return;
            _editLayer = layer;
            _editNeuron = neuron;
            OnStateMutated?.Invoke();
        }

        public void SetWeight(int index, double value)
        {
            NeuronModel neuron = EditableNeuron;
            if (neuron == null) return;
            if (index < 0 || index >= neuron.WeightCount) return;

            neuron.SetWeight(index, value);
            OnWeightChanged?.Invoke(index, value);
            OnStateMutated?.Invoke();
        }

        public void SetBias(double value)
        {
            NeuronModel neuron = EditableNeuron;
            if (neuron == null) return;

            neuron.Bias = value;
            OnBiasChanged?.Invoke(value);
            OnStateMutated?.Invoke();
        }

        public void SetActivation(ActivationType activation)
        {
            NeuronModel neuron = EditableNeuron;
            if (neuron == null) return;

            neuron.Activation = activation;
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
        /// Sets weight 0 and weight 1 to the same value. Bias is left alone.
        /// </summary>
        public void SetUnifiedWeight(double value)
        {
            NeuronModel neuron = EditableNeuron;
            if (neuron == null) return;
            if (neuron.WeightCount > 0) neuron.SetWeight(0, value);
            if (neuron.WeightCount > 1) neuron.SetWeight(1, value);
            if (neuron.WeightCount > 0) OnWeightChanged?.Invoke(0, value);
            if (neuron.WeightCount > 1) OnWeightChanged?.Invoke(1, value);
            OnStateMutated?.Invoke();
        }

        private NeuronModel EditableNeuron
        {
            get
            {
                if (_network == null || _network.Layers.Count == 0) return null;
                if (_network.SingleNeuron != null) return _network.SingleNeuron;
                int layer = _editLayer;
                int neuron = _editNeuron;
                if (layer < 0 || layer >= _network.Layers.Count) return _network.Layers[0].Neurons[0];
                var neurons = _network.Layers[layer].Neurons;
                if (neuron < 0 || neuron >= neurons.Count) return neurons[0];
                return neurons[neuron];
            }
        }

        private double ReadWeight(int index)
        {
            NeuronModel neuron = EditableNeuron;
            if (neuron == null || index < 0 || index >= neuron.WeightCount) return 0.0;
            return neuron.GetWeight(index);
        }

        /// <summary>
        /// Pre-configures the network for the Level 1 simplified starting state:
        /// both cables connected, Step activation installed, weights and bias zeroed.
        /// Call this after BrokenConfigurationSO.ApplyTo() when using the guided sandbox mode.
        /// </summary>
        public void AutoConfigureForLevel1()
        {
            if (_network?.SingleNeuron == null) return;
            _cablesConnected[0] = true;
            _cablesConnected[1] = true;
            _network.SingleNeuron.Activation = ActivationType.Step;
            OnCableStateChanged?.Invoke(0, true);
            OnCableStateChanged?.Invoke(1, true);
            OnActivationChanged?.Invoke(ActivationType.Step);
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
