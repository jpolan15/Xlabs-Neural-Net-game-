using System;
using NUnit.Framework;
using Convergence.Core.Neural;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class NeuronModelTests
    {
        private const double Delta = 1e-5;

        [Test]
        public void NeuronModel_ORGateStepForward_ComputesExpectedMargins()
        {
            // Level 1 OR gate configuration: w1 = 1, w2 = 1, b = -0.5, Activation = Step
            var neuron = new NeuronModel(new double[] { 1.0, 1.0 }, -0.5, ActivationType.Step);

            // (0, 0) -> z = -0.5, step(z) = 0.0
            double out00 = neuron.Forward(new double[] { 0.0, 0.0 });
            Assert.AreEqual(-0.5, neuron.LastZ, Delta);
            Assert.AreEqual(0.0, out00, Delta);

            // (0, 1) -> z = 0.5, step(z) = 1.0
            double out01 = neuron.Forward(new double[] { 0.0, 1.0 });
            Assert.AreEqual(0.5, neuron.LastZ, Delta);
            Assert.AreEqual(1.0, out01, Delta);

            // (1, 0) -> z = 0.5, step(z) = 1.0
            double out10 = neuron.Forward(new double[] { 1.0, 0.0 });
            Assert.AreEqual(0.5, neuron.LastZ, Delta);
            Assert.AreEqual(1.0, out10, Delta);

            // (1, 1) -> z = 1.5, step(z) = 1.0
            double out11 = neuron.Forward(new double[] { 1.0, 1.0 });
            Assert.AreEqual(1.5, neuron.LastZ, Delta);
            Assert.AreEqual(1.0, out11, Delta);
        }

        [Test]
        public void NeuronModel_SoftmaxActivation_ThrowsNotSupportedException()
        {
            var neuron = new NeuronModel(new double[] { 1.0, 1.0 }, 0.0, ActivationType.Softmax);

            var ex = Assert.Throws<NotSupportedException>(() => neuron.Forward(new double[] { 0.5, 0.5 }));
            StringAssert.Contains("Softmax is a vector-valued activation", ex.Message);
        }

        [Test]
        public void NeuronModel_InputDimensionMismatch_ThrowsArgumentException()
        {
            var neuron = new NeuronModel(new double[] { 1.0, 1.0 }, 0.0, ActivationType.Linear);

            // Expects 2 inputs, pass 3
            Assert.Throws<ArgumentException>(() => neuron.Forward(new double[] { 1.0, 2.0, 3.0 }));
            // Pass 1
            Assert.Throws<ArgumentException>(() => neuron.Forward(new double[] { 1.0 }));
        }

        [Test]
        public void NeuronModel_WeightsProperty_MaintainsDefensiveCopies()
        {
            double[] original = new double[] { 1.0, 2.0 };
            var neuron = new NeuronModel(original, 0.0, ActivationType.Linear);

            // Mutating original array should not affect neuron
            original[0] = 999.0;
            Assert.AreEqual(1.0, neuron.GetWeight(0), Delta);

            // Mutating returned array should not affect neuron
            double[] fetched = neuron.Weights;
            fetched[0] = 888.0;
            Assert.AreEqual(1.0, neuron.GetWeight(0), Delta);
        }

        [Test]
        public void NeuronModel_DeepCopy_IsCompletelyIndependent()
        {
            var original = new NeuronModel(new double[] { 1.0, 2.0 }, -0.5, ActivationType.ReLU);
            original.Forward(new double[] { 2.0, 3.0 }); // LastZ = 1*2 + 2*3 - 0.5 = 7.5

            var clone = original.DeepCopy();
            Assert.AreEqual(original.LastZ, clone.LastZ, Delta);
            Assert.AreEqual(original.LastOutput, clone.LastOutput, Delta);

            // Mutate clone
            clone.SetWeight(0, 50.0);
            clone.Bias = 10.0;
            clone.Activation = ActivationType.Sigmoid;

            // Original remains unchanged
            Assert.AreEqual(1.0, original.GetWeight(0), Delta);
            Assert.AreEqual(-0.5, original.Bias, Delta);
            Assert.AreEqual(ActivationType.ReLU, original.Activation);
        }
    }
}
