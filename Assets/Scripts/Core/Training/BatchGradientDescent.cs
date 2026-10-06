using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Training
{
    /// <summary>
    /// Mean loss and mean gradient over a whole example set, for one single-neuron network.
    /// </summary>
    public sealed class BatchGradient
    {
        /// <summary>Mean loss over every example, before any update.</summary>
        public double Loss { get; }

        /// <summary>Mean ∂L/∂w for each weight.</summary>
        public double[] WeightGradients { get; }

        /// <summary>Mean ∂L/∂b.</summary>
        public double BiasGradient { get; }

        /// <summary>Creates a batch gradient. The array is cloned.</summary>
        public BatchGradient(double loss, double[] weightGradients, double biasGradient)
        {
            Loss = loss;
            WeightGradients = weightGradients == null ? Array.Empty<double>() : (double[])weightGradients.Clone();
            BiasGradient = biasGradient;
        }

        /// <summary>Euclidean length of the weight gradient. Zero means the weights sit at a flat point.</summary>
        public double WeightGradientNorm
        {
            get
            {
                double sum = 0.0;
                for (int i = 0; i < WeightGradients.Length; i++) sum += WeightGradients[i] * WeightGradients[i];
                return System.Math.Sqrt(sum);
            }
        }
    }

    /// <summary>
    /// Full-batch gradient descent for a single neuron: w ← w − η · mean(∂L/∂w) over every example.
    /// The existing per-example <see cref="StochasticGradientDescent"/> is unchanged. This is the step the
    /// loss-landscape marble takes, so the marble follows the slope of the surface that is drawn.
    /// </summary>
    public static class BatchGradientDescent
    {
        /// <summary>
        /// Mean loss and mean gradient over all examples. Does not change the network.
        /// </summary>
        public static BatchGradient ComputeGradient(
            NetworkModel network,
            IReadOnlyList<double[]> inputs,
            IReadOnlyList<double[]> targets,
            LossKind loss)
        {
            NeuronModel neuron = Validate(network, inputs, targets);

            int count = inputs.Count;
            double[] weightSum = new double[neuron.WeightCount];
            double biasSum = 0.0;
            double lossSum = 0.0;
            double[] gradient = new double[1];

            for (int i = 0; i < count; i++)
            {
                double[] prediction = network.Forward(inputs[i]);
                lossSum += StochasticGradientDescent.ComputeLoss(prediction, targets[i], loss);
                StochasticGradientDescent.ComputeGradient(prediction, targets[i], loss, gradient);
                network.Backward(gradient);
                for (int w = 0; w < weightSum.Length; w++) weightSum[w] += neuron.GetWeightGradient(w);
                biasSum += neuron.BiasGradient;
            }

            network.ClearGradients();
            for (int w = 0; w < weightSum.Length; w++) weightSum[w] /= count;
            return new BatchGradient(lossSum / count, weightSum, biasSum / count);
        }

        /// <summary>
        /// One full-batch update. Returns the gradient that was applied (loss is the value before the update).
        /// When <paramref name="updateBias"/> is false the bias stays where it is, which keeps the loss a
        /// surface over the weights alone.
        /// </summary>
        public static BatchGradient Step(
            NetworkModel network,
            IReadOnlyList<double[]> inputs,
            IReadOnlyList<double[]> targets,
            double learningRate,
            LossKind loss,
            bool updateBias = false)
        {
            if (learningRate < 0.0 || double.IsNaN(learningRate) || double.IsInfinity(learningRate))
            {
                throw new ArgumentOutOfRangeException(nameof(learningRate));
            }

            BatchGradient gradient = ComputeGradient(network, inputs, targets, loss);
            NeuronModel neuron = network.SingleNeuron;
            for (int w = 0; w < gradient.WeightGradients.Length; w++)
            {
                neuron.SetWeight(w, neuron.GetWeight(w) - learningRate * gradient.WeightGradients[w]);
            }

            if (updateBias) neuron.Bias -= learningRate * gradient.BiasGradient;
            return gradient;
        }

        /// <summary>
        /// Mean loss over all examples at the network's current weights. Does not change the network.
        /// </summary>
        public static double MeanLoss(
            NetworkModel network,
            IReadOnlyList<double[]> inputs,
            IReadOnlyList<double[]> targets,
            LossKind loss)
        {
            Validate(network, inputs, targets);
            double sum = 0.0;
            for (int i = 0; i < inputs.Count; i++)
            {
                sum += StochasticGradientDescent.ComputeLoss(network.Forward(inputs[i]), targets[i], loss);
            }

            return sum / inputs.Count;
        }

        private static NeuronModel Validate(
            NetworkModel network,
            IReadOnlyList<double[]> inputs,
            IReadOnlyList<double[]> targets)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (inputs.Count != targets.Count) throw new ArgumentException("Input and target example counts must match.");
            if (inputs.Count == 0) throw new ArgumentException("At least one example is required.");
            NeuronModel neuron = network.SingleNeuron;
            if (neuron == null) throw new ArgumentException("Batch gradient descent needs a single-neuron network.", nameof(network));
            return neuron;
        }
    }
}
