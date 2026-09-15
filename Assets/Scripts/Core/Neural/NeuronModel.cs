using System;
using Convergence.Core.Math;

namespace Convergence.Core.Neural
{
    /// <summary>
    /// Pure C# model of an individual artificial neuron.
    /// Tracks weights, bias, activation type, pre-activation sum z, and activated output.
    /// </summary>
    public class NeuronModel
    {
        private double[] _weights;

        /// <summary>
        /// Gets or sets the connection weights. Uses defensive array cloning.
        /// </summary>
        public double[] Weights
        {
            get => (double[])_weights.Clone();
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "Weights array cannot be null.");
                }
                _weights = (double[])value.Clone();
            }
        }

        /// <summary>
        /// Number of incoming input connections / weights.
        /// </summary>
        public int WeightCount => _weights.Length;

        /// <summary>
        /// Bias term added to the weighted sum: z = sum(w_i * x_i) + b.
        /// </summary>
        public double Bias { get; set; }

        /// <summary>
        /// Activation function applied to pre-activation sum z.
        /// Note: Softmax is vector-valued and must be evaluated on LayerModel, not NeuronModel.
        /// </summary>
        public ActivationType Activation { get; set; }

        /// <summary>
        /// Cached pre-activation weighted sum z from the most recent forward pass.
        /// </summary>
        public double LastZ { get; private set; }

        /// <summary>
        /// Cached activated output a = f(z) from the most recent forward pass.
        /// </summary>
        public double LastOutput { get; private set; }

        /// <summary>
        /// Initializes a new neuron with the given weights, bias, and activation type.
        /// </summary>
        public NeuronModel(double[] weights, double bias = 0.0, ActivationType activation = ActivationType.Linear)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }
            if (weights.Length == 0)
            {
                throw new ArgumentException("Neuron must have at least one weight/input.", nameof(weights));
            }

            _weights = (double[])weights.Clone();
            Bias = bias;
            Activation = activation;
            LastZ = 0.0;
            LastOutput = 0.0;
        }

        /// <summary>
        /// Initializes a new neuron with n zero weights.
        /// </summary>
        public NeuronModel(int inputCount, double bias = 0.0, ActivationType activation = ActivationType.Linear)
            : this(new double[inputCount], bias, activation)
        {
        }

        /// <summary>
        /// Gets an individual weight without allocating a full array copy.
        /// </summary>
        public double GetWeight(int index)
        {
            if (index < 0 || index >= _weights.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _weights[index];
        }

        /// <summary>
        /// Sets an individual weight.
        /// </summary>
        public void SetWeight(int index, double value)
        {
            if (index < 0 || index >= _weights.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _weights[index] = value;
        }

        /// <summary>
        /// Evaluates the pre-activation sum z = sum(w_i * x_i) + b without applying activation.
        /// Updates LastZ.
        /// </summary>
        public double ComputePreActivation(double[] inputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }
            if (inputs.Length != _weights.Length)
            {
                throw new ArgumentException(
                    $"Input dimension mismatch: received {inputs.Length} inputs, but neuron expects {_weights.Length} weights.",
                    nameof(inputs)
                );
            }

            double z = Bias;
            for (int i = 0; i < _weights.Length; i++)
            {
                z += _weights[i] * inputs[i];
            }

            LastZ = z;
            return z;
        }

        /// <summary>
        /// Executes a scalar forward pass: computes z = sum(w_i * x_i) + b,
        /// caches LastZ and LastOutput, and returns the activated output.
        /// Throws NotSupportedException if Activation is set to Softmax.
        /// </summary>
        public double Forward(double[] inputs)
        {
            if (Activation == ActivationType.Softmax)
            {
                throw new NotSupportedException(
                    "Softmax is a vector-valued activation across a layer and is not supported on a scalar NeuronModel. " +
                    "Configure Softmax at the LayerModel or NetworkModel level."
                );
            }

            double z = ComputePreActivation(inputs);
            double output = EvaluateScalarActivation(z, Activation);
            LastOutput = output;
            return output;
        }

        /// <summary>
        /// Sets LastOutput directly (used by LayerModel when applying vector activations like Softmax).
        /// </summary>
        internal void SetLastOutput(double output)
        {
            LastOutput = output;
        }

        /// <summary>
        /// Creates an independent deep copy of this neuron and its current cached state.
        /// </summary>
        public NeuronModel DeepCopy()
        {
            var copy = new NeuronModel(_weights, Bias, Activation)
            {
                LastZ = this.LastZ,
                LastOutput = this.LastOutput
            };
            return copy;
        }

        private static double EvaluateScalarActivation(double z, ActivationType activation)
        {
            switch (activation)
            {
                case ActivationType.Step:
                    return ActivationFunctions.Step(z);
                case ActivationType.Linear:
                    return ActivationFunctions.Linear(z);
                case ActivationType.ReLU:
                    return ActivationFunctions.ReLU(z);
                case ActivationType.Sigmoid:
                    return ActivationFunctions.Sigmoid(z);
                case ActivationType.Tanh:
                    return ActivationFunctions.Tanh(z);
                default:
                    throw new ArgumentOutOfRangeException(nameof(activation), $"Unsupported activation type: {activation}");
            }
        }
    }
}
