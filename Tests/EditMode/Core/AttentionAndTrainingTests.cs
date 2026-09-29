using System.Collections.Generic;
using NUnit.Framework;
using Convergence.Core.Math;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class AttentionAndTrainingTests
    {
        private const double Delta = 1e-5;

        [Test]
        public void ScaledDotProduct_MatchingKey_ReceivesMoreWeight()
        {
            var query = new double[,] { { 1.0, 0.0 } };
            var key = new double[,] { { 1.0, 0.0 }, { 0.0, 1.0 } };
            var value = new double[,] { { 5.0, 0.0 }, { 0.0, 7.0 } };

            Attention.Result result = Attention.ScaledDotProduct(query, key, value);

            Assert.Greater(result.Weights[0, 0], result.Weights[0, 1]);
            Assert.AreEqual(1.0, result.Weights[0, 0] + result.Weights[0, 1], Delta);
            Assert.Greater(result.Output[0, 0], result.Output[0, 1]);
        }

        [Test]
        public void ScaledDotProduct_Mask_DropsTheMaskedValue()
        {
            var query = new double[,] { { 1.0, 0.0 } };
            var key = new double[,] { { 1.0, 0.0 }, { 1.0, 0.0 } };
            var value = new double[,] { { 4.0, -1.0 }, { -3.0, 9.0 } };
            var mask = new bool[,] { { true, false } };

            Attention.Result result = Attention.ScaledDotProduct(query, key, value, mask);

            Assert.AreEqual(1.0, result.Weights[0, 0], Delta);
            Assert.AreEqual(0.0, result.Weights[0, 1], Delta);
            Assert.AreEqual(4.0, result.Output[0, 0], Delta);
            Assert.AreEqual(-1.0, result.Output[0, 1], Delta);
        }

        [Test]
        public void ScaledDotProduct_ZeroQuery_SplitsEqualKeysEvenly()
        {
            var query = new double[,] { { 0.0, 0.0 } };
            var key = new double[,] { { 2.0, -3.0 }, { -4.0, 1.0 } };
            var value = new double[,] { { 1.0 }, { 3.0 } };

            Attention.Result result = Attention.ScaledDotProduct(query, key, value);

            Assert.AreEqual(0.5, result.Weights[0, 0], Delta);
            Assert.AreEqual(0.5, result.Weights[0, 1], Delta);
            Assert.AreEqual(2.0, result.Output[0, 0], Delta);
        }

        [Test]
        public void ScaledDotProduct_NegativeQuery_PrefersTheOppositeKey()
        {
            var query = new double[,] { { -1.0, 0.0 } };
            var key = new double[,] { { 1.0, 0.0 }, { -1.0, 0.0 } };
            var value = new double[,] { { 0.0 }, { 1.0 } };

            Attention.Result result = Attention.ScaledDotProduct(query, key, value);

            Assert.Greater(result.Weights[0, 1], result.Weights[0, 0]);
        }

        [Test]
        public void ScaledDotProduct_EmptyQuery_Throws()
        {
            var empty = new double[0, 1];
            var key = new double[,] { { 1.0 } };
            var value = new double[,] { { 1.0 } };
            Assert.Throws<System.ArgumentException>(() => Attention.ScaledDotProduct(empty, key, value));
        }

        [Test]
        public void SgdStep_LinearNeuron_MatchesClosedFormMseUpdate()
        {
            var network = NetworkModel.CreateSingleNeuronNetwork(1, new[] { 0.0 }, 0.0, ActivationType.Linear);
            int events = 0;
            void Count(TrainingStepRecord _) => events++;
            StochasticGradientDescent.OnStep += Count;

            TrainingStepRecord record = StochasticGradientDescent.Step(
                network,
                new[] { 1.0 },
                new[] { 2.0 },
                learningRate: 0.1,
                LossKind.MeanSquaredError);

            StochasticGradientDescent.OnStep -= Count;

            Assert.AreEqual(4.0, record.Loss, Delta);
            Assert.AreEqual(0.4, network.SingleNeuron.GetWeight(0), Delta);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void SgdStep_HighLearningRate_IncreasesLossOnTheNextStep()
        {
            var network = NetworkModel.CreateSingleNeuronNetwork(1, new[] { 0.0 }, 0.0, ActivationType.Linear);
            StochasticGradientDescent.Step(network, new[] { 1.0 }, new[] { 2.0 }, 5.0, LossKind.MeanSquaredError);
            TrainingStepRecord second = StochasticGradientDescent.Step(
                network, new[] { 1.0 }, new[] { 2.0 }, 5.0, LossKind.MeanSquaredError);

            Assert.Greater(second.Loss, 4.0);
        }

        [Test]
        public void SgdRun_SameSeed_IsDeterministic()
        {
            var left = TwoLayer();
            var right = TwoLayer();
            var inputs = new List<double[]> { new[] { 1.0 }, new[] { -1.0 } };
            var targets = new List<double[]> { new[] { 0.2 }, new[] { 0.8 } };

            StochasticGradientDescent.Run(left, inputs, targets, 0.05, LossKind.MeanSquaredError, seed: 3);
            StochasticGradientDescent.Run(right, inputs, targets, 0.05, LossKind.MeanSquaredError, seed: 3);

            Assert.AreEqual(left.Layers[0].Neurons[0].GetWeight(0), right.Layers[0].Neurons[0].GetWeight(0), Delta);
            Assert.AreEqual(left.Layers[1].Neurons[0].Bias, right.Layers[1].Neurons[0].Bias, Delta);
        }

        [Test]
        public void Backward_ReluHiddenLinearOutput_MatchesHandComputedGradient()
        {
            var hidden = new LayerModel(new[] { new NeuronModel(new[] { 1.0 }, 0.0, ActivationType.ReLU) });
            var output = new LayerModel(new[] { new NeuronModel(new[] { 1.0 }, 0.0, ActivationType.Linear) });
            var network = new NetworkModel(new[] { hidden, output });

            network.Forward(new[] { 1.0 });
            network.Backward(new[] { 2.0 * (1.0 - 0.0) });

            Assert.AreEqual(2.0, network.Layers[1].Neurons[0].GetWeightGradient(0), Delta);
            Assert.AreEqual(2.0, network.Layers[0].Neurons[0].GetWeightGradient(0), Delta);
        }

        [Test]
        public void MeanSquaredErrorGradient_ZeroAndNegative()
        {
            var destination = new double[2];
            LossFunctions.MeanSquaredErrorGradient(new[] { 0.0, -2.0 }, new[] { 0.0, -2.0 }, destination);
            Assert.AreEqual(0.0, destination[0], Delta);
            Assert.AreEqual(0.0, destination[1], Delta);

            LossFunctions.MeanSquaredErrorGradient(new[] { 1.0, 0.0 }, new[] { -1.0, 2.0 }, destination);
            Assert.AreEqual(2.0, destination[0], Delta);
            Assert.AreEqual(-2.0, destination[1], Delta);
        }

        [Test]
        public void Evaluate_ContinuousTwoOutputs_UsesPerOutputTolerance()
        {
            var layer = new LayerModel(new[]
            {
                new NeuronModel(new[] { 0.5 }, 0.0, ActivationType.Linear),
                new NeuronModel(new[] { 0.5 }, 0.0, ActivationType.Linear)
            });
            var network = new NetworkModel(new[] { layer });
            var puzzle = new PuzzleDefinition(
                new[] { new TestCase(new[] { 1.0 }, new[] { 0.5, 0.5 }, "center") },
                requiredInputs: 1,
                requiredOutputs: 2,
                requiredActivation: ActivationType.Linear,
                accuracyThreshold: 1.0,
                chamberTitle: "continuous",
                scoring: ScoringMode.Continuous,
                outputTolerances: new[] { 0.1, 0.1 });

            PuzzleEvaluation inside = PuzzleEvaluator.Evaluate(network, puzzle);
            Assert.IsTrue(inside.Passed);

            network.Layers[0].Neurons[1].SetWeight(0, 1.0);
            PuzzleEvaluation outside = PuzzleEvaluator.Evaluate(network, puzzle);
            Assert.IsFalse(outside.Passed);
            StringAssert.Contains("Output 1", outside.Diagnostics[0].Explanation);
        }

        [Test]
        public void Evaluate_BinaryMultiOutput_RequiresEveryOutput()
        {
            var layer = new LayerModel(new[]
            {
                new NeuronModel(new[] { 1.0 }, 0.0, ActivationType.Step),
                new NeuronModel(new[] { -1.0 }, 0.0, ActivationType.Step)
            });
            var network = new NetworkModel(new[] { layer });
            var puzzle = new PuzzleDefinition(
                new[] { new TestCase(new[] { 1.0 }, new[] { 1.0, 1.0 }, "both") },
                1, 2, ActivationType.Step, 1.0, "binary-multi");

            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(network, puzzle);
            Assert.IsFalse(eval.Diagnostics[0].IsCorrect);
            Assert.IsFalse(eval.Passed);
        }

        [Test]
        public void Xor_SingleNeuronCannotPass_HiddenLayerCan()
        {
            var puzzle = PuzzleDefinition.CreateXorPuzzle();
            var single = NetworkModel.CreateSingleNeuronNetwork(2, new[] { 1.0, 1.0 }, -0.5, ActivationType.Step);
            Assert.IsFalse(PuzzleEvaluator.Evaluate(single, puzzle).Passed);

            var hidden = new LayerModel(new[]
            {
                new NeuronModel(new[] { 1.0, 1.0 }, -0.5, ActivationType.Step),
                new NeuronModel(new[] { 1.0, 1.0 }, -1.5, ActivationType.Step)
            });
            var output = new LayerModel(new[]
            {
                new NeuronModel(new[] { 1.0, -1.0 }, -0.5, ActivationType.Step)
            });
            var solved = new NetworkModel(new[] { hidden, output });
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(solved, puzzle);
            Assert.IsTrue(eval.Passed, eval.Summary);
            Assert.AreEqual(4, eval.PassedCases);
        }

        [Test]
        public void NavLog_UnmaskedNoiseMissesHome_MaskPredictsJumpHome()
        {
            NavAttentionReport open = NavLogAttention.Predict(maskNoise: false);
            NavAttentionReport masked = NavLogAttention.Predict(maskNoise: true);

            Assert.IsFalse(open.PredictsJumpHome);
            Assert.Greater(open.NoiseWeight, 0.0);
            Assert.IsTrue(masked.PredictsJumpHome);
            Assert.AreEqual(0.0, masked.NoiseWeight, Delta);
            Assert.Greater(masked.EarthLockWeight, open.EarthLockWeight);
        }

        private static NetworkModel TwoLayer()
        {
            var hidden = new LayerModel(new[]
            {
                new NeuronModel(new[] { 0.2 }, 0.0, ActivationType.ReLU),
                new NeuronModel(new[] { -0.3 }, 0.1, ActivationType.ReLU)
            });
            var output = new LayerModel(new[] { new NeuronModel(new[] { 0.4, -0.2 }, 0.0, ActivationType.Sigmoid) });
            return new NetworkModel(new[] { hidden, output });
        }
    }
}
