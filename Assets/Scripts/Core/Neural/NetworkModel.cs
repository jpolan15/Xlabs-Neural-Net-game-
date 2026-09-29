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
        /// Factory method to build a larger multi-layer network for the Earth Navigation puzzle.
        /// 16 inputs -> 8 hidden neurons -> 2 output neurons.
        /// </summary>
        public static NetworkModel CreateNavigationNetwork()
        {
            var hiddenNeurons = new List<NeuronModel>();
            for (int i = 0; i < 8; i++)
            {
                hiddenNeurons.Add(new NeuronModel(new double[16], 0.0, ActivationType.Sigmoid));
            }
            var hiddenLayer = new LayerModel(hiddenNeurons);

            var outputNeurons = new List<NeuronModel>();
            for (int i = 0; i < 2; i++)
            {
                outputNeurons.Add(new NeuronModel(new double[8], 0.0, ActivationType.Sigmoid));
            }
            var outputLayer = new LayerModel(outputNeurons);

            return new NetworkModel(new[] { hiddenLayer, outputLayer });
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
        /// Zeros every neuron gradient in the network.
        /// </summary>
        public void ClearGradients()
        {
            for (int layer = 0; layer < _layers.Count; layer++)
            {
                var neurons = _layers[layer].Neurons;
                for (int n = 0; n < neurons.Count; n++)
                {
                    neurons[n].ClearGradients();
                }
            }
        }

        /// <summary>
        /// Backpropagates output-activation gradients to every weight and bias.
        /// Call <see cref="Forward"/> first so each neuron has cached inputs and activations.
        /// </summary>
        public void Backward(double[] dLossDOutputs)
        {
            if (dLossDOutputs == null)
            {
                throw new ArgumentNullException(nameof(dLossDOutputs));
            }
            if (dLossDOutputs.Length != OutputCount)
            {
                throw new ArgumentException(
                    $"Expected {OutputCount} output gradients, received {dLossDOutputs.Length}.",
                    nameof(dLossDOutputs));
            }

            ClearGradients();
            double[] upstream = dLossDOutputs;
            for (int layer = _layers.Count - 1; layer >= 0; layer--)
            {
                var current = _layers[layer];
                double[] downstream = new double[current.InputCount];
                current.Backward(upstream, downstream);
                upstream = downstream;
            }
        }

        /// <summary>
        /// Applies the gradient step w ← w − η ∂L/∂w on every neuron, then clears gradients.
        /// </summary>
        public void ApplyGradients(double learningRate)
        {
            for (int layer = 0; layer < _layers.Count; layer++)
            {
                var neurons = _layers[layer].Neurons;
                for (int n = 0; n < neurons.Count; n++)
                {
                    neurons[n].ApplyGradients(learningRate);
                }
            }
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
