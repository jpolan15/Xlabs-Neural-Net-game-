using System;
using NUnit.Framework;
using Convergence.Core.Neural;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class NetworkModelTests
    {
        private const double Delta = 1e-5;

        [Test]
        public void LayerModel_VectorSoftmax_OutputsNormalizedDistribution()
        {
            // Layer of 3 neurons, 2 inputs each, Softmax layer activation
            var n1 = new NeuronModel(new double[] { 1.0, 0.0 }, 0.0, ActivationType.Linear);
            var n2 = new NeuronModel(new double[] { 0.0, 1.0 }, 0.0, ActivationType.Linear);
            var n3 = new NeuronModel(new double[] { 1.0, 1.0 }, 0.0, ActivationType.Linear);

            var layer = new LayerModel(new[] { n1, n2, n3 }, ActivationType.Softmax);
            double[] outputs = layer.Forward(new double[] { 1.0, 2.0 });

            Assert.AreEqual(3, outputs.Length);
            double sum = outputs[0] + outputs[1] + outputs[2];
            Assert.AreEqual(1.0, sum, 1e-6);

            // Verify outputs match each neuron's LastOutput
            Assert.AreEqual(outputs[0], n1.LastOutput, Delta);
            Assert.AreEqual(outputs[1], n2.LastOutput, Delta);
            Assert.AreEqual(outputs[2], n3.LastOutput, Delta);
        }

        [Test]
        public void NetworkModel_SingleNeuronNetwork_MatchesPerceptronCalculations()
        {
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { 1.0, 1.0 },
                initialBias: -0.5,
                activation: ActivationType.Step
            );

            Assert.IsNotNull(network.SingleNeuron);
            Assert.AreEqual(2, network.InputCount);
            Assert.AreEqual(1, network.OutputCount);

            double[] out00 = network.Forward(new double[] { 0.0, 0.0 });
            Assert.AreEqual(0.0, out00[0], Delta);

            double[] out01 = network.Forward(new double[] { 0.0, 1.0 });
            Assert.AreEqual(1.0, out01[0], Delta);

            double[] out10 = network.Forward(new double[] { 1.0, 0.0 });
            Assert.AreEqual(1.0, out10[0], Delta);

            double[] out11 = network.Forward(new double[] { 1.0, 1.0 });
            Assert.AreEqual(1.0, out11[0], Delta);
        }

        [Test]
        public void NetworkModel_DeepCopy_CreatesIndependentNetwork()
        {
            var network = NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { 1.0, 1.0 },
                initialBias: -0.5,
                activation: ActivationType.Step
            );

            var clone = network.DeepCopy();
            clone.SingleNeuron.SetWeight(0, -99.0);

            Assert.AreEqual(1.0, network.SingleNeuron.GetWeight(0), Delta);
            Assert.AreEqual(-99.0, clone.SingleNeuron.GetWeight(0), Delta);
        }
    }
}
