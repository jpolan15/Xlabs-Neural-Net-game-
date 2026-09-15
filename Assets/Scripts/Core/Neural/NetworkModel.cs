using System;
using System.Collections.Generic;

namespace Convergence.Core.Neural
{
    /// <summary>
    /// Pure C# model of a multi-layer feedforward neural network.
    /// Manages sequential layer propagation and cached activations.
    /// </summary>
    public class NetworkModel
    {
        private readonly List<LayerModel> _layers;

        /// <summary>
        /// Sequential layers forming the network topology.
        /// </summary>
        public IReadOnlyList<LayerModel> Layers => _layers;

        /// <summary>
        /// Expected dimensionality of input vectors.
        /// </summary>
        public int InputCount => _layers.Count > 0 ? _layers[0].InputCount : 0;

        /// <summary>
        /// Dimensionality of final output vectors.
        /// </summary>
        public int OutputCount => _layers.Count > 0 ? _layers[_layers.Count - 1].NeuronCount : 0;

        /// <summary>
        /// Convenience accessor for single-neuron networks (Level 1 OR-gate perceptron).
        /// Returns null if network topology is not a single neuron.
        /// </summary>
        public NeuronModel SingleNeuron =>
            (_layers.Count == 1 && _layers[0].NeuronCount == 1) ? _layers[0].Neurons[0] : null;

        /// <summary>
        /// Initializes a network model with the given sequential layers.
        /// </summary>
        public NetworkModel(IEnumerable<LayerModel> layers)
        {
            if (layers == null)
            {
                throw new ArgumentNullException(nameof(layers));
            }

            _layers = new List<LayerModel>();
            int expectedInputs = -1;

            foreach (var layer in layers)
            {
                if (layer == null)
                {
                    throw new ArgumentException("Layer cannot be null.", nameof(layers));
                }
                if (expectedInputs != -1 && layer.InputCount != expectedInputs)
                {
                    throw new ArgumentException(
                        $"Layer shape mismatch: layer expects {layer.InputCount} inputs, but previous layer outputs {expectedInputs}."
                    );
                }
                _layers.Add(layer);
                expectedInputs = layer.NeuronCount;
            }

            if (_layers.Count == 0)
            {
                throw new ArgumentException("Network must contain at least one layer.", nameof(layers));
            }
        }

        /// <summary>
        /// Convenience factory method to build a single-neuron perceptron network (Level 1).
        /// </summary>
        public static NetworkModel CreateSingleNeuronNetwork(
            int inputCount,
            double[] initialWeights = null,
            double initialBias = 0.0,
            ActivationType activation = ActivationType.Step)
        {
            double[] weights = initialWeights ?? new double[inputCount];
            var neuron = new NeuronModel(weights, initialBias, activation);
            var layer = new LayerModel(new[] { neuron });
            return new NetworkModel(new[] { layer });
        }

        /// <summary>
        /// Propagates input vector through all layers sequentially and returns final output vector.
        /// </summary>
        public double[] Forward(double[] inputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }
            if (inputs.Length != InputCount)
            {
                throw new ArgumentException(
                    $"Input dimension mismatch: network expects {InputCount} inputs, received {inputs.Length}.",
                    nameof(inputs)
                );
            }

            double[] current = inputs;
            for (int i = 0; i < _layers.Count; i++)
            {
                current = _layers[i].Forward(current);
            }

            return current;
        }

        /// <summary>
        /// Creates an independent deep copy of this network and all underlying layers and neurons.
        /// </summary>
        public NetworkModel DeepCopy()
        {
            var clonedLayers = new List<LayerModel>(_layers.Count);
            for (int i = 0; i < _layers.Count; i++)
            {
                clonedLayers.Add(_layers[i].DeepCopy());
            }
            return new NetworkModel(clonedLayers);
        }
    }
}
