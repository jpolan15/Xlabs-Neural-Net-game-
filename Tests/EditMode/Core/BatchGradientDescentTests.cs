using System;
using System.Collections.Generic;
using NUnit.Framework;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class BatchGradientDescentTests
    {
        private const double Delta = 1e-6;

        private static readonly List<double[]> OrInputs = new List<double[]>
        {
            new[] { 0.0, 0.0 }, new[] { 0.0, 1.0 }, new[] { 1.0, 0.0 }, new[] { 1.0, 1.0 }
        };

        private static readonly List<double[]> OrTargets = new List<double[]>
        {
            new[] { 0.0 }, new[] { 1.0 }, new[] { 1.0 }, new[] { 1.0 }
        };

        private static NetworkModel Neuron(double w1, double w2)
        {
            return NetworkModel.CreateSingleNeuronNetwork(2, new[] { w1, w2 }, 0.0, ActivationType.Linear);
        }

        [Test]
        public void ComputeGradient_AtStart_MatchesHandCalculation()
        {
            // w = (0, 0): outputs 0. Errors are 0, -1, -1, -1.
            // dL/dw1 = mean(2 * err * x1) = (0 + 0 + 2*-1*1 + 2*-1*1) / 4 = -1.0
            BatchGradient g = BatchGradientDescent.ComputeGradient(Neuron(0.0, 0.0), OrInputs, OrTargets, LossKind.MeanSquaredError);

            Assert.AreEqual(0.75, g.Loss, Delta);
            Assert.AreEqual(-1.0, g.WeightGradients[0], Delta);
            Assert.AreEqual(-1.0, g.WeightGradients[1], Delta);
            Assert.AreEqual(-1.5, g.BiasGradient, Delta);
        }

        [Test]
        public void ComputeGradient_AtOptimum_IsZero()
        {
            double w = 2.0 / 3.0;
            BatchGradient g = BatchGradientDescent.ComputeGradient(Neuron(w, w), OrInputs, OrTargets, LossKind.MeanSquaredError);

            Assert.AreEqual(0.0, g.WeightGradientNorm, 1e-9);
            Assert.AreEqual(1.0 / 12.0, g.Loss, 1e-9);
        }

        [Test]
        public void Step_DoesNotChangeTheNetworkWhenLearningRateIsZero()
        {
            NetworkModel network = Neuron(-1.5, 1.5);
            BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.0, LossKind.MeanSquaredError);

            Assert.AreEqual(-1.5, network.SingleNeuron.GetWeight(0), Delta);
            Assert.AreEqual(1.5, network.SingleNeuron.GetWeight(1), Delta);
        }

        [Test]
        public void Step_KeepsTheBiasUnlessAsked()
        {
            NetworkModel network = NetworkModel.CreateSingleNeuronNetwork(2, new[] { 0.0, 0.0 }, 0.25, ActivationType.Linear);
            BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.5, LossKind.MeanSquaredError);
            Assert.AreEqual(0.25, network.SingleNeuron.Bias, Delta);

            BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.5, LossKind.MeanSquaredError, updateBias: true);
            Assert.That(network.SingleNeuron.Bias, Is.Not.EqualTo(0.25).Within(Delta));
        }

        [Test]
        public void Step_GoodRate_LossDecreasesEveryStepOnOr()
        {
            NetworkModel network = Neuron(-1.5, 1.5);
            double previous = double.MaxValue;
            for (int i = 0; i < 25; i++)
            {
                BatchGradient g = BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.5, LossKind.MeanSquaredError);
                Assert.Less(g.Loss, previous, "step " + i);
                previous = g.Loss;
            }
        }

        [Test]
        public void Step_GoodRate_ConvergesToTwoThirdsAndPassesOrWithStepNeuron()
        {
            NetworkModel network = Neuron(-1.5, 1.5);
            for (int i = 0; i < 120; i++)
            {
                BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.5, LossKind.MeanSquaredError);
            }

            double w1 = network.SingleNeuron.GetWeight(0);
            double w2 = network.SingleNeuron.GetWeight(1);
            Assert.AreEqual(2.0 / 3.0, w1, 1e-4);
            Assert.AreEqual(2.0 / 3.0, w2, 1e-4);

            PuzzleEvaluation decision = CurriculumCatalog.EvaluateSingleNeuron(PuzzleDefinition.CreateORGatePuzzle(), w1, w2, -0.5);
            Assert.IsTrue(decision.Passed);
        }

        [Test]
        public void Step_SlowRate_StillDescendsButMovesLittle()
        {
            NetworkModel network = Neuron(-1.5, 1.5);
            BatchGradient first = BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.05, LossKind.MeanSquaredError);
            BatchGradient second = BatchGradientDescent.Step(network, OrInputs, OrTargets, 0.05, LossKind.MeanSquaredError);

            Assert.Less(second.Loss, first.Loss);
            Assert.Less(System.Math.Abs(network.SingleNeuron.GetWeight(0) - -1.5), 0.5);
        }

        [Test]
        public void Step_CrazyRate_Diverges()
        {
            NetworkModel network = Neuron(-1.5, 1.5);
            double first = BatchGradientDescent.Step(network, OrInputs, OrTargets, 1.6, LossKind.MeanSquaredError).Loss;
            double last = first;
            for (int i = 0; i < 12; i++)
            {
                last = BatchGradientDescent.Step(network, OrInputs, OrTargets, 1.6, LossKind.MeanSquaredError).Loss;
            }

            Assert.Greater(last, first * 10.0);
        }

        [Test]
        public void Step_NegativeLearningRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BatchGradientDescent.Step(Neuron(0, 0), OrInputs, OrTargets, -0.1, LossKind.MeanSquaredError));
        }

        [Test]
        public void ComputeGradient_RejectsMismatchedOrEmptyData()
        {
            Assert.Throws<ArgumentException>(() =>
                BatchGradientDescent.ComputeGradient(Neuron(0, 0), OrInputs, new List<double[]>(), LossKind.MeanSquaredError));
            Assert.Throws<ArgumentException>(() =>
                BatchGradientDescent.ComputeGradient(Neuron(0, 0), new List<double[]>(), new List<double[]>(), LossKind.MeanSquaredError));
        }

        [Test]
        public void ComputeGradient_RejectsMultiLayerNetwork()
        {
            NetworkModel xor = CurriculumCatalog.CreateXorReferenceNetwork();
            Assert.Throws<ArgumentException>(() =>
                BatchGradientDescent.ComputeGradient(xor, OrInputs, OrTargets, LossKind.MeanSquaredError));
        }

        [Test]
        public void SampleGrid_MinimumSitsNextToTwoThirds_AndTemplateIsUntouched()
        {
            NetworkModel template = Neuron(-1.5, 1.5);
            double[,] grid = LossLandscape.SampleGrid(template, OrInputs, OrTargets, LossKind.MeanSquaredError, -2.0, 3.0, 11);

            int bestI = 0, bestJ = 0;
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    if (grid[i, j] < grid[bestI, bestJ]) { bestI = i; bestJ = j; }
                }
            }

            // Grid step is 0.5, so the sample nearest 0.667 is 0.5 (index 5) or 1.0 (index 6).
            Assert.That(bestI, Is.InRange(5, 6));
            Assert.That(bestJ, Is.InRange(5, 6));
            Assert.AreEqual(-1.5, template.SingleNeuron.GetWeight(0), Delta);
            Assert.AreEqual(1.5, template.SingleNeuron.GetWeight(1), Delta);
        }

        [Test]
        public void SampleGrid_CornerValueMatchesMeanLoss()
        {
            double[,] grid = LossLandscape.SampleGrid(Neuron(0, 0), OrInputs, OrTargets, LossKind.MeanSquaredError, 0.0, 1.0, 2);
            Assert.AreEqual(0.75, grid[0, 0], Delta);
            double expected = BatchGradientDescent.MeanLoss(Neuron(1.0, 1.0), OrInputs, OrTargets, LossKind.MeanSquaredError);
            Assert.AreEqual(expected, grid[1, 1], Delta);
        }

        [Test]
        public void SampleGrid_RejectsBadArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LossLandscape.SampleGrid(Neuron(0, 0), OrInputs, OrTargets, LossKind.MeanSquaredError, 0.0, 1.0, 1));
            Assert.Throws<ArgumentException>(() =>
                LossLandscape.SampleGrid(Neuron(0, 0), OrInputs, OrTargets, LossKind.MeanSquaredError, 1.0, 1.0, 5));
        }
    }
}
