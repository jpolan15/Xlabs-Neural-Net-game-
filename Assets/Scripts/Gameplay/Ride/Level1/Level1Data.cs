using System;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Level 1's baked data (<c>Assets/_Project/Data/Level1/level1_digits.json</c>, written by
    /// <c>ArtSource/training/bake_level1.py</c> from real MNIST). Turns the flat arrays into Core models; it computes
    /// nothing itself.
    /// </summary>
    public sealed class Level1Data
    {
        /// <summary>Asset path of the baked file.</summary>
        public const string AssetPath = "Assets/_Project/Data/Level1/level1_digits.json";

        /// <summary>The digit the ride follows.</summary>
        public const int HeroDigit = 3;

        /// <summary>The raw baked values.</summary>
        public Level1DataFile File { get; }

        private Level1Data(Level1DataFile file)
        {
            File = file;
        }

        /// <summary>Parses the baked JSON and checks its shapes. Throws when anything is missing or the wrong size.</summary>
        public static Level1Data Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Level 1 data is empty.", nameof(json));
            var file = JsonUtility.FromJson<Level1DataFile>(json);
            Validate(file);
            return new Level1Data(file);
        }

        /// <summary>A fresh network with the untrained (initial) weights.</summary>
        public NetworkModel CreateUntrained() => Create(File.initial);

        /// <summary>A fresh network with the trained weights.</summary>
        public NetworkModel CreateTrained() => Create(File.trained);

        /// <summary>The untrained (initial) or offline-trained weights as a flat <see cref="DigitNetwork"/>.</summary>
        public DigitNetwork CreateDigitNetwork(bool trained)
        {
            Level1Weights w = trained ? File.trained : File.initial;
            return new DigitNetwork(w.w1, w.b1, w.w2, w.b2);
        }

        /// <summary>
        /// A live trainer (v6, the training game) for <paramref name="network">a network</paramref>, on the bake's
        /// balanced subsets of real training and test digits, with the bake's batch size and sampler seed.
        /// </summary>
        public LiveTrainer CreateLiveTrainer(DigitNetwork network)
        {
            Level1Live live = File.live;
            if (_trainPixels == null)
            {
                _trainPixels = Convert.FromBase64String(live.trainPixels);
                _testPixels = Convert.FromBase64String(live.testPixels);
            }

            return new LiveTrainer(network, _trainPixels, live.trainLabels, _testPixels, live.testLabels, live.batch, live.samplerSeed);
        }

        private byte[] _trainPixels;
        private byte[] _testPixels;

        /// <summary>The neuron puzzle of region 2.</summary>
        public WeightPaintPuzzle CreatePuzzle()
        {
            var digits = new double[File.puzzle.digits.Length][];
            var fire = new bool[digits.Length];
            for (int d = 0; d < digits.Length; d++)
            {
                digits[d] = File.puzzle.digits[d].pixels7;
                fire[d] = File.puzzle.digits[d].shouldFire;
            }
            return new WeightPaintPuzzle(digits, fire, File.puzzle.threshold);
        }

        /// <summary>Test accuracy in whole percent, rounded down (what the narrator says).</summary>
        public int TestAccuracyPercent => (int)Math.Floor(File.testAccuracy * 100.0 + 1e-9);

        /// <summary>Real loss of the Valley ribbon at a row (point on the training path) and column (sideways).</summary>
        public double ValleyLoss(int row, int column) => File.valley.loss[row * File.valley.columns + column];

        /// <summary>The ribbon column that lies exactly on the training path.</summary>
        public int ValleyCentreColumn => File.valley.columns / 2;

        private NetworkModel Create(Level1Weights w)
        {
            return NetworkModel.CreateDense(
                File.inputSize,
                new[] { w.w1, w.w2 },
                new[] { w.b1, w.b2 },
                new[] { ActivationType.Sigmoid, ActivationType.Softmax });
        }

        private static void Validate(Level1DataFile f)
        {
            if (f == null) throw new FormatException("Level 1 data did not parse.");
            Require(f.inputSize == 49 && f.hiddenSize > 0 && f.outputSize == 10, "network shape");
            CheckWeights(f.initial, f, "initial");
            CheckWeights(f.trained, f, "trained");
            Require(f.hero != null && f.hero.pixels28 != null && f.hero.pixels28.Length == 784, "hero.pixels28");
            Require(f.hero.pixels7 != null && f.hero.pixels7.Length == f.inputSize, "hero.pixels7");
            Require(f.hero.untrainedOutputs != null && f.hero.untrainedOutputs.Length == f.outputSize, "hero.untrainedOutputs");
            Require(f.hero.trainedOutputs != null && f.hero.trainedOutputs.Length == f.outputSize, "hero.trainedOutputs");
            Require(f.hero.untrainedHidden != null && f.hero.untrainedHidden.Length == f.hiddenSize, "hero.untrainedHidden");
            Require(f.hero.trainedHidden != null && f.hero.trainedHidden.Length == f.hiddenSize, "hero.trainedHidden");
            Require(f.puzzle != null && f.puzzle.digits != null && f.puzzle.digits.Length > 0, "puzzle.digits");
            foreach (var d in f.puzzle.digits) Require(d.pixels7 != null && d.pixels7.Length == f.inputSize, "puzzle digit pixels");
            Require(f.valley != null && f.valley.loss != null && f.valley.loss.Length == f.valley.rows * f.valley.columns, "valley.loss");
            Require(f.valley.hops != null && f.valley.hops.Length >= 3, "valley.hops");
            Require(f.finale != null && f.finale.digits != null && f.finale.digits.Length > 0, "finale.digits");
            Require(f.threes != null && f.threes.Length >= 12, "threes (the dock's gallery)");
            foreach (var t in f.threes) Require(t != null && t.label == HeroDigit && t.pixels28 != null && t.pixels28.Length == 784, "a gallery three's pixels28");
            Require(f.targets != null && f.targets.Length >= 8, "targets (blast the 3s)");
            int targetThrees = 0;
            foreach (var t in f.targets)
            {
                Require(t != null && t.label >= 0 && t.label <= 9 && t.pixels28 != null && t.pixels28.Length == 784, "a target's pixels28");
                if (t.label == HeroDigit) targetThrees++;
            }

            Require(targetThrees > 0 && targetThrees < f.targets.Length, "targets need threes and other digits");
            Level1Live live = f.live;
            Require(live != null && live.rates != null && live.rates.Length == 3 && live.batch > 0 && live.samplerSeed != 0, "live (the training game)");
            Require(live.trainLabels != null && live.trainLabels.Length > 0 && !string.IsNullOrEmpty(live.trainPixels), "live.train");
            Require(live.testLabels != null && live.testLabels.Length > 0 && !string.IsNullOrEmpty(live.testPixels), "live.test");
        }

        private static void CheckWeights(Level1Weights w, Level1DataFile f, string name)
        {
            Require(w != null && w.w1 != null && w.w1.Length == f.hiddenSize * f.inputSize, name + ".w1");
            Require(w.b1 != null && w.b1.Length == f.hiddenSize, name + ".b1");
            Require(w.w2 != null && w.w2.Length == f.outputSize * f.hiddenSize, name + ".w2");
            Require(w.b2 != null && w.b2.Length == f.outputSize, name + ".b2");
        }

        private static void Require(bool ok, string what)
        {
            if (!ok) throw new FormatException("Level 1 data: missing or wrong-sized " + what + ".");
        }
    }

#pragma warning disable CS0649 // fields are filled by JsonUtility
    /// <summary>JSON root of the bake. Field names match the file.</summary>
    [Serializable]
    public sealed class Level1DataFile
    {
        public Level1Meta meta;
        public int inputSize, hiddenSize, outputSize, trainCount, testCount, totalSteps;
        public double testAccuracy, trainAccuracy, learningRate, firstStepLearningRate;
        public Level1Weights initial, trained;
        public Level1Hero hero;
        public Level1Puzzle puzzle;
        public Level1Valley valley;
        public Level1Finale finale;

        /// <summary>The dock's gallery (E0): real test threes, all different, never the hero.</summary>
        public Level1Picture[] threes;

        /// <summary>The travel game (v7, blast the 3s): clear real test digits, threes and look-alikes, 28x28.</summary>
        public Level1Picture[] targets;
        /// <summary>The training game (v6): the digits the rider trains on and is tested on.</summary>
        public Level1Live live;
    }

    /// <summary>Where the data came from.</summary>
    [Serializable]
    public sealed class Level1Meta
    {
        public int version, seedInit, seedFinale, seedWall;
        public string source, licence, generator, numpyVersion, date, notes;
    }

    /// <summary>Flat row-major weights: w1[j * 49 + i] is hidden neuron j's weight on pixel i.</summary>
    [Serializable]
    public sealed class Level1Weights
    {
        public double[] w1, b1, w2, b2;
    }

    /// <summary>The hero "3" and what both networks made of it.</summary>
    [Serializable]
    public sealed class Level1Hero
    {
        public int testIndex, label, untrainedGuess;
        public double[] pixels28, pixels7, untrainedHidden, untrainedOutputs, trainedHidden, trainedOutputs;
    }

    /// <summary>One real test digit, 7×7.</summary>
    [Serializable]
    public sealed class Level1Digit
    {
        public int testIndex, label, prediction;
        public bool shouldFire;
        public double confidence;
        public double[] pixels7;
    }

    /// <summary>One real test digit at full size, 28×28, row 0 at the top, 0 = paper, 1 = ink.</summary>
    [Serializable]
    public sealed class Level1Picture
    {
        public int testIndex, label;
        public double[] pixels28;
    }

    /// <summary>The neuron puzzle's digits and line. referenceSolution is for tests only.</summary>
    [Serializable]
    public sealed class Level1Puzzle
    {
        public double threshold;
        public Level1Digit[] digits;
        public int[] referenceSolution;
        public int hintMovesFromEmpty;
    }

    /// <summary>One coaster hop: the weights after <c>step</c> real steps, on ribbon row <c>row</c>.</summary>
    [Serializable]
    public sealed class Level1Hop
    {
        public int step, realSteps, row;
        public double loss;
    }

    /// <summary>The Valley ribbon: rows follow the training path, columns step sideways; values are real loss.</summary>
    [Serializable]
    public sealed class Level1Valley
    {
        public int rows, columns, samplesPerHop;
        public double wallHalfWidth;
        public double[] loss, rowStep;
        public Level1Hop[] hops;
    }

    /// <summary>Real test digits and the trained network's real predictions.</summary>
    [Serializable]
    public sealed class Level1Finale
    {
        public Level1Digit[] digits;
        public int correct;
    }
    /// <summary>
    /// The training game's data (v6, ADR-020): balanced subsets of real MNIST, 49 bytes per digit (base64, 255 = ink),
    /// the three step sizes (tiny, good, huge), and what the bake's replay of each measured.
    /// </summary>
    [Serializable]
    public sealed class Level1Live
    {
        public int batch, dataSeed, stepsToTarget;
        public uint samplerSeed;
        public double targetAccuracy, startLoss, goodAccuracyAt300, tinyAccuracyAt300, hugePeakLoss;
        public double[] rates;
        public string trainPixels, testPixels;
        public int[] trainLabels, testLabels;
    }
#pragma warning restore CS0649
}
