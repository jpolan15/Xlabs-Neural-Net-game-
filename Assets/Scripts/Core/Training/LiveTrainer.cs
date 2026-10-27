using System;

namespace Convergence.Core.Training
{
    /// <summary>
    /// Trains a <see cref="DigitNetwork"/> live with mini-batch stochastic gradient descent on real digits: each
    /// <see cref="Step"/> draws a batch (with replacement, from a seeded xorshift32 sampler, so a run is repeatable), runs
    /// it forward, measures the mean cross-entropy loss, backpropagates it and moves every weight w ← w − η ∂L/∂w. It is
    /// the bake's own training step (<c>ArtSource/training/bake_level1.py</c>, <c>live_run</c>), which replays the
    /// three step sizes to check that the good one learns, the tiny one crawls and the huge one blows up. Digits are
    /// stored as bytes, 49 per digit, 255 = ink. Allocation-free after construction.
    /// </summary>
    public sealed class LiveTrainer
    {
        private const double SmoothingRate = 0.15;

        private readonly byte[] _trainPixels;
        private readonly int[] _trainLabels;
        private readonly byte[] _testPixels;
        private readonly int[] _testLabels;
        private readonly double[] _x = new double[DigitNetwork.Inputs];
        private readonly double[] _h = new double[DigitNetwork.Hidden];
        private readonly double[] _p = new double[DigitNetwork.Outputs];
        private readonly double[] _dz2 = new double[DigitNetwork.Outputs];
        private readonly double[] _dz1 = new double[DigitNetwork.Hidden];
        private readonly double[] _gW1 = new double[DigitNetwork.Hidden * DigitNetwork.Inputs];
        private readonly double[] _gB1 = new double[DigitNetwork.Hidden];
        private readonly double[] _gW2 = new double[DigitNetwork.Outputs * DigitNetwork.Hidden];
        private readonly double[] _gB2 = new double[DigitNetwork.Outputs];
        private uint _sampler;

        /// <param name="network">Trained in place.</param>
        /// <param name="trainPixels">49 bytes per training digit.</param>
        /// <param name="testPixels">49 bytes per test digit (never trained on).</param>
        /// <param name="samplerSeed">Seed of the batch sampler (non-zero).</param>
        public LiveTrainer(DigitNetwork network, byte[] trainPixels, int[] trainLabels, byte[] testPixels, int[] testLabels,
            int batchSize, uint samplerSeed)
        {
            Network = network ?? throw new ArgumentNullException(nameof(network));
            Check(trainPixels, trainLabels, nameof(trainPixels));
            Check(testPixels, testLabels, nameof(testPixels));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (samplerSeed == 0) throw new ArgumentOutOfRangeException(nameof(samplerSeed), "xorshift32 needs a non-zero seed.");
            _trainPixels = trainPixels;
            _trainLabels = trainLabels;
            _testPixels = testPixels;
            _testLabels = testLabels;
            BatchSize = batchSize;
            _sampler = samplerSeed;
        }

        public DigitNetwork Network { get; }
        public int BatchSize { get; }
        public int TrainCount => _trainLabels.Length;
        public int TestCount => _testLabels.Length;

        /// <summary>Steps taken so far.</summary>
        public int Steps { get; private set; }

        /// <summary>Digits looked at so far (steps × batch size).</summary>
        public long DigitsSeen => (long)Steps * BatchSize;

        /// <summary>Mean loss of the last batch, measured before its step (as the bake records it).</summary>
        public double LastLoss { get; private set; } = double.NaN;

        /// <summary>The batch loss smoothed over the last few steps (NaN before the first step).</summary>
        public double SmoothedLoss { get; private set; } = double.NaN;

        /// <summary>
        /// Takes one step of size <paramref name="learningRate"/>. With an <paramref name="extra"/> digit, every other
        /// slot of the batch is that digit with <paramref name="extraLabel"/> (studying one rider's handwriting).
        /// Returns the batch's mean loss before the step.
        /// </summary>
        public double Step(double learningRate, double[] extra = null, int extraLabel = -1)
        {
            if (learningRate <= 0 || double.IsNaN(learningRate)) throw new ArgumentOutOfRangeException(nameof(learningRate));
            if (extra != null && (extra.Length != DigitNetwork.Inputs || extraLabel < 0 || extraLabel >= DigitNetwork.Outputs))
            {
                throw new ArgumentException("An extra digit needs 49 pixels and a label 0 to 9.", nameof(extra));
            }

            DigitNetwork net = Network;
            Array.Clear(_gW1, 0, _gW1.Length);
            Array.Clear(_gB1, 0, _gB1.Length);
            Array.Clear(_gW2, 0, _gW2.Length);
            Array.Clear(_gB2, 0, _gB2.Length);
            double lossSum = 0.0;
            double inv = 1.0 / BatchSize;

            for (int b = 0; b < BatchSize; b++)
            {
                int label;
                if (extra != null && (b & 1) == 1)
                {
                    Array.Copy(extra, _x, DigitNetwork.Inputs);
                    label = extraLabel;
                }
                else
                {
                    int index = (int)(NextSample() % (uint)_trainLabels.Length);
                    int offset = index * DigitNetwork.Inputs;
                    for (int i = 0; i < DigitNetwork.Inputs; i++) _x[i] = _trainPixels[offset + i] / 255.0;
                    label = _trainLabels[index];
                }

                net.Forward(_x, _h, _p);
                lossSum += -System.Math.Log(System.Math.Max(_p[label], 1e-300));

                for (int k = 0; k < DigitNetwork.Outputs; k++) _dz2[k] = (_p[k] - (k == label ? 1.0 : 0.0)) * inv;
                for (int k = 0; k < DigitNetwork.Outputs; k++)
                {
                    int row = k * DigitNetwork.Hidden;
                    for (int j = 0; j < DigitNetwork.Hidden; j++) _gW2[row + j] += _dz2[k] * _h[j];
                    _gB2[k] += _dz2[k];
                }

                for (int j = 0; j < DigitNetwork.Hidden; j++)
                {
                    double back = 0.0;
                    for (int k = 0; k < DigitNetwork.Outputs; k++) back += _dz2[k] * net.W2[k * DigitNetwork.Hidden + j];
                    _dz1[j] = back * _h[j] * (1.0 - _h[j]);
                }

                for (int j = 0; j < DigitNetwork.Hidden; j++)
                {
                    int row = j * DigitNetwork.Inputs;
                    double d = _dz1[j];
                    for (int i = 0; i < DigitNetwork.Inputs; i++) _gW1[row + i] += d * _x[i];
                    _gB1[j] += d;
                }
            }

            Apply(net.W1, _gW1, learningRate);
            Apply(net.B1, _gB1, learningRate);
            Apply(net.W2, _gW2, learningRate);
            Apply(net.B2, _gB2, learningRate);
            Steps++;
            LastLoss = lossSum * inv;
            SmoothedLoss = double.IsNaN(SmoothedLoss) || double.IsInfinity(SmoothedLoss)
                ? LastLoss
                : SmoothedLoss + (LastLoss - SmoothedLoss) * SmoothingRate;
            return LastLoss;
        }

        /// <summary>Fraction of the test digits the network gets right now.</summary>
        public double TestAccuracy()
        {
            int right = 0;
            for (int d = 0; d < _testLabels.Length; d++)
            {
                if (Network.Forward(_testPixels, d, _h, _p) == _testLabels[d]) right++;
            }

            return (double)right / _testLabels.Length;
        }

        /// <summary>Mean cross-entropy loss over every training digit (a full pass; for checks, not every frame).</summary>
        public double TrainLoss()
        {
            double sum = 0.0;
            for (int d = 0; d < _trainLabels.Length; d++)
            {
                Network.Forward(_trainPixels, d, _h, _p);
                sum += -System.Math.Log(System.Math.Max(_p[_trainLabels[d]], 1e-300));
            }

            return sum / _trainLabels.Length;
        }

        /// <summary>Puts the network back to saved weights and forgets the smoothed loss (an undo after a blow-up).</summary>
        public void Restore(DigitNetwork saved)
        {
            Network.CopyFrom(saved);
            SmoothedLoss = double.NaN;
            LastLoss = double.NaN;
        }

        /// <summary>xorshift32, the bake's <c>XorShift32</c>.</summary>
        private uint NextSample()
        {
            uint x = _sampler;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _sampler = x;
            return x;
        }

        private static void Apply(double[] weights, double[] gradient, double learningRate)
        {
            for (int i = 0; i < weights.Length; i++) weights[i] -= learningRate * gradient[i];
        }

        private static void Check(byte[] pixels, int[] labels, string name)
        {
            if (pixels == null || labels == null || labels.Length == 0 || pixels.Length != labels.Length * DigitNetwork.Inputs)
            {
                throw new ArgumentException("Expected 49 bytes per labelled digit.", name);
            }

            foreach (int label in labels)
            {
                if (label < 0 || label >= DigitNetwork.Outputs) throw new ArgumentException("Labels are 0 to 9.", name);
            }
        }
    }
}
