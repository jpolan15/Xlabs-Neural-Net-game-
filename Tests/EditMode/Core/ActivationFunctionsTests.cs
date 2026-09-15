using System;
using NUnit.Framework;
using Convergence.Core.Math;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class ActivationFunctionsTests
    {
        private const double Delta = 1e-5;

        [Test]
        public void Step_NegativeInput_ReturnsZero()
        {
            Assert.AreEqual(0.0, ActivationFunctions.Step(-1.0), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.Step(-0.001), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.Step(-100.0), Delta);
        }

        [Test]
        public void Step_ZeroInput_ReturnsOne()
        {
            Assert.AreEqual(1.0, ActivationFunctions.Step(0.0), Delta);
        }

        [Test]
        public void Step_PositiveInput_ReturnsOne()
        {
            Assert.AreEqual(1.0, ActivationFunctions.Step(0.001), Delta);
            Assert.AreEqual(1.0, ActivationFunctions.Step(1.0), Delta);
            Assert.AreEqual(1.0, ActivationFunctions.Step(50.0), Delta);
        }

        [Test]
        public void StepDerivative_AlwaysReturnsZeroCompatibilityStub()
        {
            Assert.AreEqual(0.0, ActivationFunctions.StepDerivative(-1.0), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.StepDerivative(0.0), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.StepDerivative(1.0), Delta);
        }

        [Test]
        public void Linear_ReturnsInputValue()
        {
            Assert.AreEqual(-2.5, ActivationFunctions.Linear(-2.5), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.Linear(0.0), Delta);
            Assert.AreEqual(3.1415, ActivationFunctions.Linear(3.1415), Delta);
        }

        [Test]
        public void LinearDerivative_ReturnsOne()
        {
            Assert.AreEqual(1.0, ActivationFunctions.LinearDerivative(-5.0), Delta);
            Assert.AreEqual(1.0, ActivationFunctions.LinearDerivative(0.0), Delta);
            Assert.AreEqual(1.0, ActivationFunctions.LinearDerivative(5.0), Delta);
        }

        [Test]
        public void ReLU_NegativeInput_ReturnsZero()
        {
            Assert.AreEqual(0.0, ActivationFunctions.ReLU(-10.0), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.ReLU(-0.1), Delta);
        }

        [Test]
        public void ReLU_ZeroInput_ReturnsZero()
        {
            Assert.AreEqual(0.0, ActivationFunctions.ReLU(0.0), Delta);
        }

        [Test]
        public void ReLU_PositiveInput_ReturnsInput()
        {
            Assert.AreEqual(2.5, ActivationFunctions.ReLU(2.5), Delta);
        }

        [Test]
        public void ReLUDerivative_BoundaryAndSignCheck()
        {
            Assert.AreEqual(0.0, ActivationFunctions.ReLUDerivative(-1.0), Delta);
            Assert.AreEqual(0.0, ActivationFunctions.ReLUDerivative(0.0), Delta);
            Assert.AreEqual(1.0, ActivationFunctions.ReLUDerivative(1.0), Delta);
        }

        [Test]
        public void Sigmoid_StandardValues()
        {
            Assert.AreEqual(0.5, ActivationFunctions.Sigmoid(0.0), Delta);
            Assert.AreEqual(1.0 / (1.0 + System.Math.Exp(-1.0)), ActivationFunctions.Sigmoid(1.0), Delta);
            Assert.AreEqual(1.0 / (1.0 + System.Math.Exp(1.0)), ActivationFunctions.Sigmoid(-1.0), Delta);
        }

        [Test]
        public void Sigmoid_ExtremeValues_ClampedGracefully()
        {
            Assert.AreEqual(0.0, ActivationFunctions.Sigmoid(-100.0), Delta);
            Assert.AreEqual(1.0, ActivationFunctions.Sigmoid(100.0), Delta);
        }

        [Test]
        public void SigmoidDerivative_AtZero_ReturnsQuarter()
        {
            // sigma(0) = 0.5, sigma'(0) = 0.5 * (1 - 0.5) = 0.25
            Assert.AreEqual(0.25, ActivationFunctions.SigmoidDerivative(0.0), Delta);
        }

        [Test]
        public void Tanh_StandardValues()
        {
            Assert.AreEqual(0.0, ActivationFunctions.Tanh(0.0), Delta);
            Assert.AreEqual(System.Math.Tanh(1.0), ActivationFunctions.Tanh(1.0), Delta);
            Assert.AreEqual(System.Math.Tanh(-1.0), ActivationFunctions.Tanh(-1.0), Delta);
        }

        [Test]
        public void TanhDerivative_AtZero_ReturnsOne()
        {
            // 1 - tanh^2(0) = 1 - 0 = 1.0
            Assert.AreEqual(1.0, ActivationFunctions.TanhDerivative(0.0), Delta);
        }

        [Test]
        public void Softmax_SumsToOne()
        {
            double[] logits = new double[] { 1.0, 2.0, 3.0 };
            double[] probs = ActivationFunctions.Softmax(logits);

            Assert.AreEqual(3, probs.Length);
            double sum = probs[0] + probs[1] + probs[2];
            Assert.AreEqual(1.0, sum, 1e-6);
            Assert.IsTrue(probs[2] > probs[1]);
            Assert.IsTrue(probs[1] > probs[0]);
        }

        [Test]
        public void Softmax_ExtremeValues_DoesNotOverflow()
        {
            double[] logits = new double[] { 1000.0, 1001.0, 1002.0 };
            double[] probs = ActivationFunctions.Softmax(logits);

            Assert.IsFalse(double.IsNaN(probs[0]));
            Assert.IsFalse(double.IsNaN(probs[1]));
            Assert.IsFalse(double.IsNaN(probs[2]));
            double sum = probs[0] + probs[1] + probs[2];
            Assert.AreEqual(1.0, sum, 1e-6);
        }

        [Test]
        public void Softmax_NullOrEmpty_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => ActivationFunctions.Softmax(null));
            Assert.Throws<ArgumentException>(() => ActivationFunctions.Softmax(new double[0]));
        }
    }
}
