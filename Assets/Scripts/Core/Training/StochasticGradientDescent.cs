using System;
using System.Collections.Generic;
using Convergence.Core.Math;
using Convergence.Core.Neural;

namespace Convergence.Core.Training
{
    /// <summary>
    /// Which scalar loss supplies ∂L/∂ŷ for one SGD step.
    /// </summary>
    public enum LossKind
    {
        /// <summary>Mean squared error.</summary>
        MeanSquaredError,

        /// <summary>Binary cross-entropy. Targets must be 0 or 1.</summary>
        BinaryCrossEntropy
    }

    /// <summary>
    /// One deterministic SGD update. Raised after the weights have changed.
    /// </summary>
    public sealed class TrainingStepRecord
    {
        /// <summary>Zero-based index of this update in the current batch call.</summary>
        public int StepIndex { get; }

        /// <summary>Loss on the example before the weight update.</summary>
        public double Loss { get; }

        /// <summary>Learning rate η applied on this step.</summary>
        public double LearningRate { get; }

        /// <summary>Network output on the example before the update. Defensive clone.</summary>
        public double[] Prediction { get; }

        /// <summary>
        /// Creates a step record.
        /// </summary>
        public TrainingStepRecord(int stepIndex, double loss, double learningRate, double[] prediction)
        {
            StepIndex = stepIndex;
            Loss = loss;
            LearningRate = learningRate;
            Prediction = prediction == null ? Array.Empty<double>() : (double[])prediction.Clone();
        }
    }

    /// <summary>
    /// Deterministic backpropagation and stochastic gradient descent.
    /// Identical weights, examples, learning rate, loss, and seed produce identical updates.
    /// A telemetry event fires once per example step.
    /// </summary>
    public static class StochasticGradientDescent
    {
        /// <summary>
        /// Fired after every example update, including each row of a batch.
        /// </summary>
        public static event Action<TrainingStepRecord> OnStep;

        /// <summary>
        /// Runs online SGD over the examples. <paramref name="seed"/> shuffles the order with <see cref="System.Random"/>.
        /// Pass a negative seed to keep the given order.
        /// </summary>
        public static IReadOnlyList<TrainingStepRecord> Run(
            NetworkModel network,
            IReadOnlyList<double[]> inputs,
            IReadOnlyList<double[]> targets,
            double learningRate,
            LossKind loss,
            int seed = -1)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (inputs.Count != targets.Count)
            {
                throw new ArgumentException("Input and target example counts must match.");
            }
            if (inputs.Count == 0)
            {
                throw new ArgumentException("At least one example is required.");
            }
            if (learningRate < 0.0 || double.IsNaN(learningRate) || double.IsInfinity(learningRate))
            {
                throw new ArgumentOutOfRangeException(nameof(learningRate));
            }

            int[] order = new int[inputs.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            if (seed >= 0 && order.Length > 1)
            {
                var rng = new Random(seed);
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    int swap = order[i];
                    order[i] = order[j];
                    order[j] = swap;
                }
            }

            var records = new List<TrainingStepRecord>(order.Length);
            for (int step = 0; step < order.Length; step++)
            {
                int index = order[step];
                records.Add(Step(network, inputs[index], targets[index], learningRate, loss, step));
            }

            return records;
        }

        /// <summary>
        /// One forward, backward, and parameter update on a single example.
        /// </summary>
        public static TrainingStepRecord Step(
            NetworkModel network,
            double[] inputs,
            double[] targets,
            double learningRate,
            LossKind loss,
            int stepIndex = 0)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (targets.Length != network.OutputCount)
            {
                throw new ArgumentException("Target length must match the network output count.");
            }

            double[] prediction = network.Forward(inputs);
            double lossValue = ComputeLoss(prediction, targets, loss);
            double[] gradient = new double[prediction.Length];
            ComputeGradient(prediction, targets, loss, gradient);
            network.Backward(gradient);
            network.ApplyGradients(learningRate);

            var record = new TrainingStepRecord(stepIndex, lossValue, learningRate, prediction);
            OnStep?.Invoke(record);
            return record;
        }

        private static double ComputeLoss(double[] prediction, double[] targets, LossKind loss)
        {
            switch (loss)
            {
                case LossKind.MeanSquaredError:
                    return LossFunctions.MeanSquaredError(prediction, targets);
                case LossKind.BinaryCrossEntropy:
                    return LossFunctions.BinaryCrossEntropy(prediction, targets);
                default:
                    throw new ArgumentOutOfRangeException(nameof(loss));
            }
        }

        private static void ComputeGradient(double[] prediction, double[] targets, LossKind loss, double[] destination)
        {
            switch (loss)
            {
                case LossKind.MeanSquaredError:
                    LossFunctions.MeanSquaredErrorGradient(prediction, targets, destination);
                    return;
                case LossKind.BinaryCrossEntropy:
                    LossFunctions.BinaryCrossEntropyGradient(prediction, targets, destination);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loss));
            }
        }
    }
}
