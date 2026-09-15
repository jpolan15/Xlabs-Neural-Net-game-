using System;
using System.Collections.Generic;
using Convergence.Core.Math;

namespace Convergence.Core.Neural
{
    /// <summary>
    /// Pure C# model of a neural network layer containing multiple neurons.
    /// Handles layer-wide forward passes and vector activations such as Softmax.
    /// </summary>
    public class LayerModel
    {
        private readonly List<NeuronModel> _neurons;

        /// <summary>
        /// Read-only access to neurons in this layer.
        /// </summary>
        public IReadOnlyList<NeuronModel> Neurons => _neurons;

        /// <summary>
        /// Number of neurons in this layer (layer output dimensionality).
        /// </summary>
        public int NeuronCount => _neurons.Count;

        /// <summary>
        /// Expected number of inputs for neurons in this layer.
        /// </summary>
        public int InputCount => _neurons.Count > 0 ? _neurons[0].WeightCount : 0;

        /// <summary>
        /// Optional layer-level vector activation override (e.g. Softmax across all layer neurons).
        /// If null, each neuron evaluates its own scalar Activation.
        /// </summary>
        public ActivationType? LayerActivationOverride { get; set; }

        /// <summary>
        /// Initializes a layer with an existing list of neurons.
        /// </summary>
        public LayerModel(IEnumerable<NeuronModel> neurons, ActivationType? layerActivationOverride = null)
        {
            if (neurons == null)
            {
                throw new ArgumentNullException(nameof(neurons));
            }

            _neurons = new List<NeuronModel>();
            int expectedInputs = -1;

            foreach (var neuron in neurons)
            {
                if (neuron == null)
                {
                    throw new ArgumentException("Neuron cannot be null.", nameof(neurons));
                }
                if (expectedInputs == -1)
                {
                    expectedInputs = neuron.WeightCount;
                }
                else if (neuron.WeightCount != expectedInputs)
                {
                    throw new ArgumentException(
                        $"All neurons in a layer must have identical weight counts. Expected {expectedInputs}, found {neuron.WeightCount}."
                    );
                }
                _neurons.Add(neuron);
            }

            if (_neurons.Count == 0)
            {
                throw new ArgumentException("Layer must contain at least one neuron.", nameof(neurons));
            }

            LayerActivationOverride = layerActivationOverride;
        }

        /// <summary>
        /// Factory creating a layer with specified neuron and input dimensions.
        /// </summary>
        public LayerModel(int inputCount, int neuronCount, ActivationType activation = ActivationType.Linear)
        {
            if (inputCount <= 0) throw new ArgumentOutOfRangeException(nameof(inputCount), "Input count must be > 0.");
            if (neuronCount <= 0) throw new ArgumentOutOfRangeException(nameof(neuronCount), "Neuron count must be > 0.");

            _neurons = new List<NeuronModel>(neuronCount);
            for (int i = 0; i < neuronCount; i++)
            {
                _neurons.Add(new NeuronModel(inputCount, 0.0, activation));
            }

            if (activation == ActivationType.Softmax)
            {
                LayerActivationOverride = ActivationType.Softmax;
            }
        }

        /// <summary>
        /// Executes a forward pass through all neurons in this layer.
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
                    $"Layer received {inputs.Length} inputs, but expects {InputCount} inputs.",
                    nameof(inputs)
                );
            }

            int count = _neurons.Count;
            double[] outputs = new double[count];

            if (LayerActivationOverride == ActivationType.Softmax)
            {
                // Vector-valued Softmax across the entire layer
                double[] logits = new double[count];
                for (int i = 0; i < count; i++)
                {
                    logits[i] = _neurons[i].ComputePreActivation(inputs);
                }

                outputs = ActivationFunctions.Softmax(logits);
                for (int i = 0; i < count; i++)
                {
                    _neurons[i].SetLastOutput(outputs[i]);
                }
            }
            else
            {
                // Scalar activations evaluated per neuron
                for (int i = 0; i < count; i++)
                {
                    outputs[i] = _neurons[i].Forward(inputs);
                }
            }

            return outputs;
        }

        /// <summary>
        /// Creates an independent deep copy of this layer and all its neurons.
        /// </summary>
        public LayerModel DeepCopy()
        {
            var clonedNeurons = new List<NeuronModel>(_neurons.Count);
            for (int i = 0; i < _neurons.Count; i++)
            {
                clonedNeurons.Add(_neurons[i].DeepCopy());
            }
            return new LayerModel(clonedNeurons, LayerActivationOverride);
        }
    }
}
