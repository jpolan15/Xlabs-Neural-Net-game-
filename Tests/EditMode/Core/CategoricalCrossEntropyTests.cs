using System;
using NUnit.Framework;
using Convergence.Core.Math;
using Convergence.Core.Neural;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class CategoricalCrossEntropyTests
    {
        [Test]
        public void Loss_IsMinusLogOfTheTargetProbability()
        {
            var p = new[] { 0.2, 0.5, 0.3 };
            Assert.AreEqual(-Math.Log(0.5), LossFunctions.CategoricalCrossEntropy(p, 1), 1e-12);
            Assert.AreEqual(0.0, LossFunctions.CategoricalCrossEntropy(new[] { 0.0, 1.0 }, 1), 1e-12);
        }

        [Test]
        public void Loss_ZeroProbability_IsClampedByEpsilon()
        {
            double loss = LossFunctions.CategoricalCrossEntropy(new[] { 1.0, 0.0 }, 1);
            Assert.AreEqual(-Math.Log(LossFunctions.DefaultEpsilon), loss, 1e-9);
        }

        [Test]
        public void Gradient_IsMinusOneOverTargetProbability_ZeroElsewhere()
        {
            var dest = new[] { 9.0, 9.0, 9.0 };
            LossFunctions.CategoricalCrossEntropyGradient(new[] { 0.2, 0.5, 0.3 }, 2, dest);
            Assert.AreEqual(0.0, dest[0]);
            Assert.AreEqual(0.0, dest[1]);
            Assert.AreEqual(-1.0 / 0.3, dest[2], 1e-12);
        }

        [Test]
        public void ThroughSoftmax_GradientOfLogitsIsProbabilitiesMinusTarget()
        {
            // Softmax layer of 3 linear neurons over 2 inputs: dL/dz = p − t, so dL/dw_ni = (p_n − t_n) x_i.
            var net = NetworkModel.CreateDense(2,
                new[] { new[] { 0.3, -0.2, 0.1, 0.4, -0.5, 0.2 } },
                new[] { new[] { 0.0, 0.1, -0.1 } },
                new[] { ActivationType.Softmax });
            var x = new[] { 1.5, -0.5 };
            double[] p = net.Forward(x);
            var dLdp = new double[3];
            LossFunctions.CategoricalCrossEntropyGradient(p, 0, dLdp);
            net.Backward(dLdp);
            for (int n = 0; n < 3; n++)
            {
                double dz = p[n] - (n == 0 ? 1.0 : 0.0);
                for (int i = 0; i < 2; i++)
                    Assert.AreEqual(dz * x[i], net.Layers[0].Neurons[n].GetWeightGradient(i), 1e-12);
            }
        }

        [Test]
        public void InvalidInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LossFunctions.CategoricalCrossEntropy(null, 0));
            Assert.Throws<ArgumentException>(() => LossFunctions.CategoricalCrossEntropy(new double[0], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LossFunctions.CategoricalCrossEntropy(new[] { 1.0 }, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LossFunctions.CategoricalCrossEntropy(new[] { 1.0 }, -1));
            Assert.Throws<ArgumentException>(() => LossFunctions.CategoricalCrossEntropyGradient(new[] { 1.0, 0.0 }, 0, new double[1]));
        }

        [Test]
        public void CreateDense_WrongShapes_Throw()
        {
            Assert.Throws<ArgumentException>(() => NetworkModel.CreateDense(2,
                new[] { new[] { 1.0, 2.0, 3.0 } }, new[] { new[] { 0.0, 0.0 } }, new[] { ActivationType.Sigmoid }));
            Assert.Throws<ArgumentException>(() => NetworkModel.CreateDense(2,
                new[] { new[] { 1.0, 2.0 } }, new[] { new[] { 0.0 } }, new ActivationType[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => NetworkModel.CreateDense(0,
                new[] { new[] { 1.0 } }, new[] { new[] { 0.0 } }, new[] { ActivationType.Sigmoid }));
        }

        [Test]
        public void CreateDense_RowMajorWeights_AndLayerWideSoftmax()
        {
            var net = NetworkModel.CreateDense(2,
                new[] { new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }, new[] { 1.0, 0.0, -1.0, 0.5, 0.5, 0.5 } },
                new[] { new[] { 0.1, 0.2, 0.3 }, new[] { 0.0, 0.0 } },
                new[] { ActivationType.Tanh, ActivationType.Softmax });
            Assert.AreEqual(3.0, net.Layers[0].Neurons[1].GetWeight(0));
            Assert.AreEqual(4.0, net.Layers[0].Neurons[1].GetWeight(1));
            Assert.AreEqual(0.2, net.Layers[0].Neurons[1].Bias);
            Assert.AreEqual(ActivationType.Tanh, net.Layers[0].Neurons[0].Activation);
            Assert.AreEqual(ActivationType.Softmax, net.Layers[1].LayerActivationOverride);
            double[] p = net.Forward(new[] { 0.5, -0.25 });
            Assert.AreEqual(1.0, p[0] + p[1], 1e-12);
        }
    }
}
