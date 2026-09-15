using System;

namespace Convergence.Core.Math
{
    /// <summary>
    /// Pure C# mathematical implementations of neural network loss and evaluation metric functions.
    /// Contains no engine references.
    /// </summary>
    public static class LossFunctions
    {
        /// <summary>
        /// Default epsilon used for clamping probabilities to avoid log(0) in cross-entropy.
        /// </summary>
        public const double DefaultEpsilon = 1e-15;

        /// <summary>
        /// Mean Squared Error (MSE):
        /// MSE = (1 / n) * sum((predicted_i - target_i)^2)
        /// </summary>
        public static double MeanSquaredError(double[] predicted, double[] targets)
        {
            ValidateArrays(predicted, targets);

            double sumSquaredError = 0.0;
            int n = predicted.Length;

            for (int i = 0; i < n; i++)
            {
                double diff = predicted[i] - targets[i];
                sumSquaredError += diff * diff;
            }

            return sumSquaredError / n;
        }

        /// <summary>
        /// Binary Cross-Entropy (BCE) with epsilon clamping to avoid log(0):
        /// BCE = -(1 / n) * sum(y_i * log(p_i + eps) + (1 - y_i) * log(1 - p_i + eps))
        /// </summary>
        public static double BinaryCrossEntropy(double[] predicted, double[] targets, double epsilon = DefaultEpsilon)
        {
            ValidateArrays(predicted, targets);

            if (epsilon <= 0.0 || epsilon >= 0.5)
            {
                throw new ArgumentOutOfRangeException(nameof(epsilon), "Epsilon must be between 0.0 and 0.5.");
            }

            double totalLoss = 0.0;
            int n = predicted.Length;

            for (int i = 0; i < n; i++)
            {
                double y = targets[i];
                if (y != 0.0 && y != 1.0)
                {
                    throw new ArgumentException($"Target at index {i} must be binary (0.0 or 1.0), found {y}.", nameof(targets));
                }

                double p = predicted[i];
                // Clamp probability to [epsilon, 1.0 - epsilon]
                if (p < epsilon) p = epsilon;
                else if (p > 1.0 - epsilon) p = 1.0 - epsilon;

                totalLoss += y * System.Math.Log(p) + (1.0 - y) * System.Math.Log(1.0 - p);
            }

            return -totalLoss / n;
        }

        /// <summary>
        /// Calculates binary classification accuracy:
        /// Classifies predicted[i] >= threshold as 1.0, else 0.0.
        /// Validates that targets are strictly binary (0.0 or 1.0).
        /// Returns fraction of correct predictions in range [0.0, 1.0].
        /// </summary>
        /// <param name="predicted">Continuous or discrete predicted outputs.</param>
        /// <param name="targets">Ground-truth binary targets (must be 0.0 or 1.0).</param>
        /// <param name="threshold">Classification decision boundary (default 0.5).</param>
        public static double BinaryAccuracy(double[] predicted, double[] targets, double threshold = 0.5)
        {
            ValidateArrays(predicted, targets);

            int correct = 0;
            int n = predicted.Length;

            for (int i = 0; i < n; i++)
            {
                double target = targets[i];
                if (target != 0.0 && target != 1.0)
                {
                    throw new ArgumentException($"Target at index {i} must be binary (0.0 or 1.0), found {target}.", nameof(targets));
                }

                double binaryPrediction = predicted[i] >= threshold ? 1.0 : 0.0;
                if (binaryPrediction == target)
                {
                    correct++;
                }
            }

            return (double)correct / n;
        }

        private static void ValidateArrays(double[] predicted, double[] targets)
        {
            if (predicted == null)
            {
                throw new ArgumentNullException(nameof(predicted));
            }
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }
            if (predicted.Length != targets.Length)
            {
                throw new ArgumentException($"Length mismatch: predicted ({predicted.Length}) != targets ({targets.Length}).");
            }
            if (predicted.Length == 0)
            {
                throw new ArgumentException("Input arrays cannot be empty.");
            }
        }
    }
}
