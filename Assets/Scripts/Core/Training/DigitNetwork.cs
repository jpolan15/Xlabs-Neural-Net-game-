using System;
using Convergence.Core.Math;
using Convergence.Core.Neural;

namespace Convergence.Core.Training
{
    /// <summary>
    /// Level 1's digit reader as flat arrays: 49 pixels → 16 sigmoid neurons → 10 softmax answers, cross-entropy loss.
    /// The layout is the bake's and <see cref="NetworkModel.CreateDense"/>'s (W1[j * 49 + i] is hidden neuron j's weight
    /// on pixel i, W2[k * 16 + j] is answer k's weight on hidden neuron j), and the math is Core's own
    /// (<see cref="ActivationFunctions.Sigmoid"/>, softmax with the max shift), so it agrees with
    /// <see cref="NetworkModel.Forward"/>. Unlike <see cref="NetworkModel"/> it allocates nothing per pass, so the
    /// headset can train it live (ADR-020).
    /// </summary>
    public sealed class DigitNetwork
    {
        public const int Inputs = 49;
        public const int Hidden = 16;
        public const int Outputs = 10;

        public readonly double[] W1 = new double[Hidden * Inputs];
        public readonly double[] B1 = new double[Hidden];
        public readonly double[] W2 = new double[Outputs * Hidden];
        public readonly double[] B2 = new double[Outputs];

        private readonly double[] _logits = new double[Outputs];

        public DigitNetwork()
        {
        }

        public DigitNetwork(double[] w1, double[] b1, double[] w2, double[] b2)
        {
            Copy(w1, W1, nameof(w1));
            Copy(b1, B1, nameof(b1));
            Copy(w2, W2, nameof(w2));
            Copy(b2, B2, nameof(b2));
        }

        /// <summary>Total weights and biases (944).</summary>
        public static int ParameterCount => Hidden * Inputs + Hidden + Outputs * Hidden + Outputs;

        public DigitNetwork Clone()
        {
            var copy = new DigitNetwork();
            copy.CopyFrom(this);
            return copy;
        }

        public void CopyFrom(DigitNetwork other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Array.Copy(other.W1, W1, W1.Length);
            Array.Copy(other.B1, B1, B1.Length);
            Array.Copy(other.W2, W2, W2.Length);
            Array.Copy(other.B2, B2, B2.Length);
        }

        /// <summary>True when no weight is NaN or infinite.</summary>
        public bool IsFinite => Finite(W1) && Finite(B1) && Finite(W2) && Finite(B2);

        /// <summary>
        /// One forward pass. <paramref name="pixels"/> holds 49 values from 0 (paper) to 1 (ink); the hidden activations
        /// and the ten answer probabilities are written into the given arrays. Returns the guess (the biggest answer).
        /// </summary>
        public int Forward(double[] pixels, double[] hidden, double[] probabilities)
        {
            if (pixels == null || pixels.Length != Inputs) throw new ArgumentException("Expected 49 pixels.", nameof(pixels));
            return Forward(pixels, null, 0, hidden, probabilities);
        }

        /// <summary>A forward pass on digit <paramref name="index"/> of a byte set (49 bytes per digit, 255 = ink).</summary>
        public int Forward(byte[] set, int index, double[] hidden, double[] probabilities)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            return Forward(null, set, index * Inputs, hidden, probabilities);
        }

        /// <summary>The ten probabilities as a new array, and the hidden activations (for views; allocates).</summary>
        public double[] Probabilities(double[] pixels, out double[] hidden)
        {
            hidden = new double[Hidden];
            var p = new double[Outputs];
            Forward(pixels, hidden, p);
            return p;
        }

        /// <summary>Builds the same network as a <see cref="NetworkModel"/> (for Core's backward pass and the views).</summary>
        public NetworkModel ToModel()
        {
            return NetworkModel.CreateDense(
                Inputs,
                new[] { (double[])W1.Clone(), (double[])W2.Clone() },
                new[] { (double[])B1.Clone(), (double[])B2.Clone() },
                new[] { ActivationType.Sigmoid, ActivationType.Softmax });
        }

        /// <summary>Reads the weights back from a <see cref="NetworkModel"/> made by <see cref="ToModel"/>.</summary>
        public void CopyFrom(NetworkModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Layers.Count != 2 || model.InputCount != Inputs || model.Layers[0].NeuronCount != Hidden || model.OutputCount != Outputs)
            {
                throw new ArgumentException("Expected a 49 → 16 → 10 network.", nameof(model));
            }

            for (int j = 0; j < Hidden; j++)
            {
                NeuronModel n = model.Layers[0].Neurons[j];
                Array.Copy(n.Weights, 0, W1, j * Inputs, Inputs);
                B1[j] = n.Bias;
            }

            for (int k = 0; k < Outputs; k++)
            {
                NeuronModel n = model.Layers[1].Neurons[k];
                Array.Copy(n.Weights, 0, W2, k * Hidden, Hidden);
                B2[k] = n.Bias;
            }
        }

        private int Forward(double[] pixels, byte[] set, int offset, double[] hidden, double[] probabilities)
        {
            if (hidden == null || hidden.Length != Hidden) throw new ArgumentException("Expected 16 hidden slots.", nameof(hidden));
            if (probabilities == null || probabilities.Length != Outputs) throw new ArgumentException("Expected 10 answer slots.", nameof(probabilities));

            for (int j = 0; j < Hidden; j++)
            {
                double z = B1[j];
                int row = j * Inputs;
                if (pixels != null)
                {
                    for (int i = 0; i < Inputs; i++) z += W1[row + i] * pixels[i];
                }
                else
                {
                    for (int i = 0; i < Inputs; i++) z += W1[row + i] * (set[offset + i] / 255.0);
                }

                hidden[j] = ActivationFunctions.Sigmoid(z);
            }

            double max = double.NegativeInfinity;
            for (int k = 0; k < Outputs; k++)
            {
                double z = B2[k];
                int row = k * Hidden;
                for (int j = 0; j < Hidden; j++) z += W2[row + j] * hidden[j];
                _logits[k] = z;
                if (z > max) max = z;
            }

            double sum = 0.0;
            for (int k = 0; k < Outputs; k++)
            {
                double e = System.Math.Exp(_logits[k] - max);
                probabilities[k] = e;
                sum += e;
            }

            int best = 0;
            for (int k = 0; k < Outputs; k++)
            {
                probabilities[k] /= sum;
                if (probabilities[k] > probabilities[best]) best = k;
            }

            return best;
        }

        private static void Copy(double[] from, double[] to, string name)
        {
            if (from == null || from.Length != to.Length) throw new ArgumentException($"Expected {to.Length} values.", name);
            Array.Copy(from, to, to.Length);
        }

        private static bool Finite(double[] values)
        {
            foreach (double v in values)
            {
                if (double.IsNaN(v) || double.IsInfinity(v)) return false;
            }

            return true;
        }
    }
}
