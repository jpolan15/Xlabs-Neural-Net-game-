using System;
using NUnit.Framework;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class PuzzleEvaluatorTests
    {
        private const double Delta = 1e-5;

        [Test]
        public void Evaluate_ValidORGateConfiguration_PassesWithExpectedMargins()
        {
            // Valid OR perceptron: w1 = 1.0, w2 = 1.0, b = -0.5, Step activation
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { 1.0, 1.0 },
                initialBias: -0.5,
                activation: ActivationType.Step
            );

            var puzzle = PuzzleDefinition.CreateORGatePuzzle();
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(network, puzzle);

            Assert.IsTrue(eval.Passed, "Evaluation must pass for valid OR configuration.");
            Assert.AreEqual(1.0, eval.Accuracy, Delta);
            Assert.AreEqual(4, eval.PassedCases);
            Assert.AreEqual(4, eval.TotalCases);
            Assert.IsTrue(eval.ActivationMatches);

            // Assert exact expected margins:
            // (0, 0): z = -0.5 -> 0
            Assert.AreEqual(-0.5, eval.Diagnostics[0].CalculatedZ, Delta);
            Assert.AreEqual(0.0, eval.Diagnostics[0].ActualOutput, Delta);
            Assert.IsTrue(eval.Diagnostics[0].IsCorrect);

            // (0, 1): z =  0.5 -> 1
            Assert.AreEqual(0.5, eval.Diagnostics[1].CalculatedZ, Delta);
            Assert.AreEqual(1.0, eval.Diagnostics[1].ActualOutput, Delta);
            Assert.IsTrue(eval.Diagnostics[1].IsCorrect);

            // (1, 0): z =  0.5 -> 1
            Assert.AreEqual(0.5, eval.Diagnostics[2].CalculatedZ, Delta);
            Assert.AreEqual(1.0, eval.Diagnostics[2].ActualOutput, Delta);
            Assert.IsTrue(eval.Diagnostics[2].IsCorrect);

            // (1, 1): z =  1.5 -> 1
            Assert.AreEqual(1.5, eval.Diagnostics[3].CalculatedZ, Delta);
            Assert.AreEqual(1.0, eval.Diagnostics[3].ActualOutput, Delta);
            Assert.IsTrue(eval.Diagnostics[3].IsCorrect);
        }

        [Test]
        public void Evaluate_ThreeOfFourCorrect_ProducesZeroPointSevenFiveAndFails()
        {
            // w1 = 0, w2 = 0, b = 0, Step:
            // (0, 0) -> z = 0 >= 0 -> 1 (target 0: INCORRECT)
            // (0, 1) -> z = 0 >= 0 -> 1 (target 1: CORRECT)
            // (1, 0) -> z = 0 >= 0 -> 1 (target 1: CORRECT)
            // (1, 1) -> z = 0 >= 0 -> 1 (target 1: CORRECT)
            // 3 out of 4 correct -> Accuracy = 0.75. Must NOT pass!
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { 0.0, 0.0 },
                initialBias: 0.0,
                activation: ActivationType.Step
            );

            var puzzle = PuzzleDefinition.CreateORGatePuzzle();
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(network, puzzle);

            Assert.IsFalse(eval.Passed, "3/4 configuration must NEVER pass evaluation.");
            Assert.AreEqual(0.75, eval.Accuracy, Delta);
            Assert.AreEqual(3, eval.PassedCases);
            Assert.AreEqual(4, eval.TotalCases);
            Assert.IsFalse(eval.Diagnostics[0].IsCorrect, "First case (0,0) must fail.");
            Assert.IsTrue(eval.Diagnostics[1].IsCorrect);
            Assert.IsTrue(eval.Diagnostics[2].IsCorrect);
            Assert.IsTrue(eval.Diagnostics[3].IsCorrect);
        }

        [Test]
        public void Evaluate_ActivationMismatch_DoesNotPassEvenIfAccuracyCoincides()
        {
            // Valid weights but with Linear activation instead of required Step
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { 1.0, 1.0 },
                initialBias: -0.5,
                activation: ActivationType.Linear
            );

            var puzzle = PuzzleDefinition.CreateORGatePuzzle();
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(network, puzzle);

            Assert.IsFalse(eval.Passed, "Evaluation must fail when activation function does not match Step.");
            Assert.IsFalse(eval.ActivationMatches);
            Assert.AreEqual(ActivationType.Linear, eval.ActiveActivation);
            Assert.AreEqual(ActivationType.Step, eval.RequiredActivation);
            StringAssert.Contains("Incompatible activation module", eval.Summary);
        }

        [Test]
        public void Evaluate_NetworkInputCountMismatch_ThrowsArgumentException()
        {
            // 3-input network evaluated against 2-input puzzle
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 3,
                initialWeights: new double[] { 1.0, 1.0, 1.0 },
                initialBias: 0.0,
                activation: ActivationType.Step
            );

            var puzzle = PuzzleDefinition.CreateORGatePuzzle();
            Assert.Throws<ArgumentException>(() => PuzzleEvaluator.Evaluate(network, puzzle));
        }

        [Test]
        public void Evaluate_DiagnosticExplanations_ProvideActionableFeedback()
        {
            // w1 = 0.5, w2 = 0.5, b = -1.0, Step
            // (0, 0) -> z = -1.0 -> 0 (target 0: Correct)
            // (0, 1) -> z = -0.5 -> 0 (target 1: Incorrect - signal too weak)
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { 0.5, 0.5 },
                initialBias: -1.0,
                activation: ActivationType.Step
            );

            var puzzle = PuzzleDefinition.CreateORGatePuzzle();
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(network, puzzle);

            Assert.IsFalse(eval.Passed);
            StringAssert.Contains("Signal too weak", eval.Diagnostics[1].Explanation);
        }
    }
}
