using System;
using NUnit.Framework;
using Convergence.Core.Math;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class LossFunctionsTests
    {
        private const double Delta = 1e-5;

        [Test]
        public void MeanSquaredError_PerfectPrediction_ReturnsZero()
        {
            double[] pred = new double[] { 1.0, 0.5, -2.0 };
            double[] target = new double[] { 1.0, 0.5, -2.0 };

            double mse = LossFunctions.MeanSquaredError(pred, target);
            Assert.AreEqual(0.0, mse, Delta);
        }

        [Test]
        public void MeanSquaredError_KnownDifferences_ReturnsExpectedAverage()
        {
            double[] pred = new double[] { 1.0, 2.0 };
            double[] target = new double[] { 2.0, 4.0 };
            // diffs: (1 - 2)^2 = 1, (2 - 4)^2 = 4 -> mean = 2.5
            double mse = LossFunctions.MeanSquaredError(pred, target);
            Assert.AreEqual(2.5, mse, Delta);
        }

        [Test]
        public void MeanSquaredError_LengthMismatch_ThrowsArgumentException()
        {
            double[] pred = new double[] { 1.0, 2.0 };
            double[] target = new double[] { 1.0 };
            Assert.Throws<ArgumentException>(() => LossFunctions.MeanSquaredError(pred, target));
        }

        [Test]
        public void BinaryCrossEntropy_PerfectMatch_ReturnsNearZero()
        {
            double[] pred = new double[] { 1.0, 0.0 };
            double[] target = new double[] { 1.0, 0.0 };
            double bce = LossFunctions.BinaryCrossEntropy(pred, target);
            Assert.IsTrue(bce < 1e-6);
        }

        [Test]
        public void BinaryCrossEntropy_NonBinaryTarget_ThrowsArgumentException()
        {
            double[] pred = new double[] { 0.8, 0.2 };
            double[] target = new double[] { 1.5, 0.0 };
            Assert.Throws<ArgumentException>(() => LossFunctions.BinaryCrossEntropy(pred, target));
        }

        [Test]
        public void BinaryAccuracy_AllCorrect_ReturnsOne()
        {
            double[] pred = new double[] { 0.9, 0.8, 0.7, 0.1 };
            double[] target = new double[] { 1.0, 1.0, 1.0, 0.0 };
            double acc = LossFunctions.BinaryAccuracy(pred, target);
            Assert.AreEqual(1.0, acc, Delta);
        }

        [Test]
        public void BinaryAccuracy_ThreeOfFourCorrect_ReturnsZeroPointSevenFive()
        {
            // Specifically testing 3/4 correct -> 0.75 accuracy (critical negative test)
            double[] pred = new double[] { 0.0, 1.0, 1.0, 1.0 };
            double[] target = new double[] { 1.0, 1.0, 1.0, 1.0 }; // 1st is wrong, rest are right
            double acc = LossFunctions.BinaryAccuracy(pred, target);
            Assert.AreEqual(0.75, acc, Delta);
        }

        [Test]
        public void BinaryAccuracy_ThresholdBoundary_PointFiveIsOne()
        {
            double[] pred = new double[] { 0.5, 0.4999 };
            double[] target = new double[] { 1.0, 0.0 };
            double acc = LossFunctions.BinaryAccuracy(pred, target);
            Assert.AreEqual(1.0, acc, Delta);
        }

        [Test]
        public void BinaryAccuracy_NonBinaryTarget_ThrowsArgumentException()
        {
            double[] pred = new double[] { 0.8, 0.2 };
            double[] target = new double[] { 0.5, 0.0 };
            Assert.Throws<ArgumentException>(() => LossFunctions.BinaryAccuracy(pred, target));
        }

        [Test]
        public void BinaryAccuracy_LengthMismatch_ThrowsArgumentException()
        {
            double[] pred = new double[] { 1.0, 0.0, 1.0 };
            double[] target = new double[] { 1.0, 0.0 };
            Assert.Throws<ArgumentException>(() => LossFunctions.BinaryAccuracy(pred, target));
        }
    }
}
