using System;

namespace Convergence.Core.Math
{
    /// <summary>
    /// Pure C# mathematical implementations of standard neural network activation functions
    /// and their derivatives. Contains no engine references.
    /// </summary>
    public static class ActivationFunctions
    {
        /// <summary>
        /// Heaviside Step activation function.
        /// Returns 1.0 if z >= 0.0, else 0.0.
        /// Primary activation function for Level 1 (OR-gate perceptron).
        /// </summary>
        public static double Step(double z)
        {
            return z >= 0.0 ? 1.0 : 0.0;
        }

        /// <summary>
        /// Step function derivative compatibility stub.
        /// The step function is non-differentiable at z = 0 and has derivative 0 everywhere else.
        /// Returns 0.0 strictly as a compatibility value for gradient interfaces.
        /// </summary>
        public static double StepDerivative(double z)
        {
            return 0.0;
        }

        /// <summary>
        /// Identity / Linear activation function f(z) = z.
        /// </summary>
        public static double Linear(double z)
        {
            return z;
        }

        /// <summary>
        /// Derivative of identity activation f'(z) = 1.0.
        /// </summary>
        public static double LinearDerivative(double z)
        {
            return 1.0;
        }

        /// <summary>
        /// Rectified Linear Unit f(z) = max(0, z).
        /// </summary>
        public static double ReLU(double z)
        {
            return System.Math.Max(0.0, z);
        }

        /// <summary>
        /// Derivative of ReLU: 1.0 if z > 0.0, else 0.0.
        /// </summary>
        public static double ReLUDerivative(double z)
        {
            return z > 0.0 ? 1.0 : 0.0;
        }

        /// <summary>
        /// Standard Logistic Sigmoid activation f(z) = 1 / (1 + e^(-z)).
        /// Clamped to prevent overflow for extreme negative values.
        /// </summary>
        public static double Sigmoid(double z)
        {
            if (z < -45.0) return 0.0;
            if (z > 45.0) return 1.0;
            return 1.0 / (1.0 + System.Math.Exp(-z));
        }

        /// <summary>
        /// Derivative of Sigmoid: sigma(z) * (1 - sigma(z)).
        /// </summary>
        public static double SigmoidDerivative(double z)
        {
            double s = Sigmoid(z);
            return s * (1.0 - s);
        }

        /// <summary>
        /// Hyperbolic Tangent activation f(z) = tanh(z).
        /// </summary>
        public static double Tanh(double z)
        {
            return System.Math.Tanh(z);
        }

        /// <summary>
        /// Derivative of Tanh: 1 - tanh^2(z).
        /// </summary>
        public static double TanhDerivative(double z)
        {
            double t = System.Math.Tanh(z);
            return 1.0 - (t * t);
        }

        /// <summary>
        /// Numerically stable vector Softmax using shifted logits:
        /// P(y = i) = exp(z_i - max(z)) / sum(exp(z_j - max(z))).
        /// Note: Softmax is vector-valued and applies across a layer of neurons.
        /// </summary>
        /// <param name="logits">Vector of input values.</param>
        /// <returns>Normalized probability distribution summing to 1.0.</returns>
        public static double[] Softmax(double[] logits)
        {
            if (logits == null)
            {
                throw new ArgumentNullException(nameof(logits));
            }
            if (logits.Length == 0)
            {
                throw new ArgumentException("Logits array cannot be empty.", nameof(logits));
            }

            int length = logits.Length;
            double maxLogit = logits[0];
            for (int i = 1; i < length; i++)
            {
                if (logits[i] > maxLogit)
                {
                    maxLogit = logits[i];
                }
            }

            double[] probabilities = new double[length];
            double sumExp = 0.0;

            for (int i = 0; i < length; i++)
            {
                double expVal = System.Math.Exp(logits[i] - maxLogit);
                probabilities[i] = expVal;
                sumExp += expVal;
            }

            for (int i = 0; i < length; i++)
            {
                probabilities[i] /= sumExp;
            }

            return probabilities;
        }
    }
}
