using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Training
{
    /// <summary>Who did better in "Beat AURA".</summary>
    public enum DescentWinner
    {
        /// <summary>The rider found a lower loss.</summary>
        Rider,

        /// <summary>Equal within tolerance.</summary>
        Tie,

        /// <summary>The trainer found a lower loss.</summary>
        Trainer
    }

    /// <summary>
    /// Stop 2 (Learning). Tracks the lowest loss and fewest mistakes the rider found by hand, and compares them
    /// with the trainer's. A mistake is a case whose output is on the wrong side of 0.5.
    /// </summary>
    public sealed class ManualDescentScore
    {
        /// <summary>Losses closer than this count as a tie.</summary>
        public const double TieTolerance = 1e-9;

        /// <summary>Lowest loss the rider reached. +∞ before any probe.</summary>
        public double BestLoss { get; private set; } = double.PositiveInfinity;

        /// <summary>Fewest mistakes the rider reached. −1 before any probe.</summary>
        public int BestMistakes { get; private set; } = -1;

        /// <summary>The weights at the best loss, or null before any probe.</summary>
        public double[] BestWeights { get; private set; }

        /// <summary>Number of positions probed.</summary>
        public int Probes { get; private set; }

        /// <summary>Counts cases whose output is on the wrong side of <paramref name="threshold"/>.</summary>
        public static int CountMistakes(NetworkModel network, IReadOnlyList<double[]> inputs, IReadOnlyList<double[]> targets, double threshold = 0.5)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (inputs.Count != targets.Count) throw new ArgumentException("Inputs and targets must have the same count.");

            int mistakes = 0;
            for (int i = 0; i < inputs.Count; i++)
            {
                double[] output = network.Forward(inputs[i]);
                for (int j = 0; j < output.Length; j++)
                {
                    bool fired = output[j] >= threshold;
                    bool wanted = targets[i][j] >= threshold;
                    if (fired != wanted)
                    {
                        mistakes++;
                        break;
                    }
                }
            }

            return mistakes;
        }

        /// <summary>Records where the rider's levers put the network. Keeps the lowest loss (ties go to fewer mistakes).</summary>
        public void Record(NetworkModel network, IReadOnlyList<double[]> inputs, IReadOnlyList<double[]> targets, LossKind loss)
        {
            double value = BatchGradientDescent.MeanLoss(network, inputs, targets, loss);
            int mistakes = CountMistakes(network, inputs, targets);
            Probes++;
            bool better = value < BestLoss - TieTolerance
                || (System.Math.Abs(value - BestLoss) <= TieTolerance && mistakes < BestMistakes);
            if (BestMistakes < 0 || better)
            {
                BestLoss = value;
                BestMistakes = mistakes;
                NeuronModel n = network.SingleNeuron;
                BestWeights = n != null ? n.Weights : null;
            }
        }

        /// <summary>Compares the rider's best loss with the trainer's final loss.</summary>
        public DescentWinner Compare(double trainerLoss)
        {
            if (Probes == 0) return DescentWinner.Trainer;
            if (System.Math.Abs(BestLoss - trainerLoss) <= TieTolerance) return DescentWinner.Tie;
            return BestLoss < trainerLoss ? DescentWinner.Rider : DescentWinner.Trainer;
        }

        /// <summary>Clears the score.</summary>
        public void Reset()
        {
            BestLoss = double.PositiveInfinity;
            BestMistakes = -1;
            BestWeights = null;
            Probes = 0;
        }
    }
}
