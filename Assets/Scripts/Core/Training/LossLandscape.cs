using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Training
{
    /// <summary>
    /// Samples the mean loss of a two-weight neuron over a square grid of (w₁, w₂).
    /// The surface the loss-landscape view draws. The template network is never changed.
    /// </summary>
    public static class LossLandscape
    {
        /// <summary>
        /// Returns grid[i, j] = mean loss with w₁ = min + i·step and w₂ = min + j·step.
        /// Resolution is the number of samples per axis and must be at least 2.
        /// </summary>
        public static double[,] SampleGrid(
            NetworkModel template,
            IReadOnlyList<double[]> inputs,
            IReadOnlyList<double[]> targets,
            LossKind loss,
            double min,
            double max,
            int resolution)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (resolution < 2) throw new ArgumentOutOfRangeException(nameof(resolution));
            if (!(max > min)) throw new ArgumentException("Max must be greater than min.");
            if (template.SingleNeuron == null || template.SingleNeuron.WeightCount != 2)
            {
                throw new ArgumentException("The landscape needs a single neuron with two weights.", nameof(template));
            }

            NetworkModel probe = template.DeepCopy();
            NeuronModel neuron = probe.SingleNeuron;
            double step = (max - min) / (resolution - 1);
            var grid = new double[resolution, resolution];
            for (int i = 0; i < resolution; i++)
            {
                neuron.SetWeight(0, min + i * step);
                for (int j = 0; j < resolution; j++)
                {
                    neuron.SetWeight(1, min + j * step);
                    grid[i, j] = BatchGradientDescent.MeanLoss(probe, inputs, targets, loss);
                }
            }

            return grid;
        }
    }
}
