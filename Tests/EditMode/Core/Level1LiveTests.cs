using System;
using System.Collections.Generic;
using NUnit.Framework;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;
using Convergence.Gameplay.Ride.Level1;
using UnityEditor;
using UnityEngine;

namespace Convergence.Tests.EditMode.Core
{
    /// <summary>
    /// Level 1 v6 (ADR-020): the rider's drawing becomes MNIST-style input, and the network the rider trains live is
    /// Core's own network, trained with the bake's own step, so the training game's outcomes are real.
    /// </summary>
    [TestFixture]
    public class Level1LiveTests
    {
        private Level1Data _data;

        [OneTimeSetUp]
        public void Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level1Data.AssetPath);
            Assert.IsNotNull(asset, Level1Data.AssetPath + " is missing; run ArtSource/training/bake_level1.py");
            _data = Level1Data.Parse(asset.text);
        }

        // ---------- the network and the trainer ----------

        [Test]
        public void DigitNetwork_AgreesWithNetworkModel_OnTheHero()
        {
            foreach (bool trained in new[] { false, true })
            {
                DigitNetwork flat = _data.CreateDigitNetwork(trained);
                NetworkModel model = trained ? _data.CreateTrained() : _data.CreateUntrained();
                double[] x = _data.File.hero.pixels7;
                var h = new double[16];
                var p = new double[10];
                int guess = flat.Forward(x, h, p);
                double[] expected = model.Forward(x);
                for (int k = 0; k < 10; k++) Assert.AreEqual(expected[k], p[k], 1e-12, $"answer {k}");
                for (int j = 0; j < 16; j++) Assert.AreEqual(model.Layers[0].Neurons[j].LastOutput, h[j], 1e-12, $"hidden {j}");
                Assert.AreEqual(trained ? 3 : _data.File.hero.untrainedGuess, guess);
            }
        }

        [Test]
        public void DigitNetwork_RoundTripsThroughNetworkModel()
        {
            DigitNetwork a = _data.CreateDigitNetwork(true);
            var b = new DigitNetwork();
            b.CopyFrom(a.ToModel());
            CollectionAssert.AreEqual(a.W1, b.W1);
            CollectionAssert.AreEqual(a.B1, b.B1);
            CollectionAssert.AreEqual(a.W2, b.W2);
            CollectionAssert.AreEqual(a.B2, b.B2);
        }

        [Test]
        public void OneStep_IsCoreBackpropagation_OfTheCrossEntropy()
        {
            // A one-digit training set: every batch slot is the hero, so the step is the hero's own gradient.
            double[] x = _data.File.hero.pixels7;
            var bytes = new byte[49];
            for (int i = 0; i < 49; i++) bytes[i] = (byte)Math.Round(x[i] * 255.0);
            var q = new double[49];
            for (int i = 0; i < 49; i++) q[i] = bytes[i] / 255.0;

            DigitNetwork live = _data.CreateDigitNetwork(false);
            var trainer = new LiveTrainer(live, bytes, new[] { 3 }, bytes, new[] { 3 }, 4, 12345u);
            const double lr = 0.5;
            trainer.Step(lr);

            NetworkModel model = _data.CreateUntrained();
            double[] p = model.Forward(q);
            var dLossDOut = new double[10];
            dLossDOut[3] = -1.0 / p[3];
            model.Backward(dLossDOut);
            model.ApplyGradients(lr);
            var expected = new DigitNetwork();
            expected.CopyFrom(model);

            for (int i = 0; i < expected.W1.Length; i++) Assert.AreEqual(expected.W1[i], live.W1[i], 1e-12, $"W1[{i}]");
            for (int i = 0; i < expected.W2.Length; i++) Assert.AreEqual(expected.W2[i], live.W2[i], 1e-12, $"W2[{i}]");
            for (int i = 0; i < expected.B1.Length; i++) Assert.AreEqual(expected.B1[i], live.B1[i], 1e-12, $"B1[{i}]");
            for (int i = 0; i < expected.B2.Length; i++) Assert.AreEqual(expected.B2[i], live.B2[i], 1e-12, $"B2[{i}]");
            Assert.AreEqual(-Math.Log(p[3]), trainer.LastLoss, 1e-12);
        }

        [Test]
        public void TheGoodStepSize_ReachesTheTarget_WhereTheBakeSaidItWould()
        {
            Level1Live live = _data.File.live;
            LiveTrainer trainer = _data.CreateLiveTrainer(_data.CreateDigitNetwork(false));
            Assert.AreEqual(live.startLoss, trainer.TrainLoss(), 1e-6, "same subset, same starting weights");
            Assert.AreEqual(0.1, trainer.TestAccuracy(), 0.05, "untrained: a guess in ten");

            int reached = -1;
            for (int step = 1; step <= 600 && reached < 0; step++)
            {
                trainer.Step(live.rates[1]);
                if (step % 10 == 0 && trainer.TestAccuracy() >= live.targetAccuracy) reached = step;
            }

            Assert.Greater(reached, 0, "the good step size never reached the target");
            Assert.AreEqual(live.stepsToTarget, reached, 30, "within a few checks of the bake's own replay");
        }

        [Test]
        public void TheTinyStepSize_Crawls_AndTheHugeOne_BlowsUp()
        {
            Level1Live live = _data.File.live;
            LiveTrainer tiny = _data.CreateLiveTrainer(_data.CreateDigitNetwork(false));
            for (int step = 0; step < 300; step++) tiny.Step(live.rates[0]);
            Assert.AreEqual(live.tinyAccuracyAt300, tiny.TestAccuracy(), 0.04);
            Assert.Less(tiny.TestAccuracy(), 0.5);

            LiveTrainer huge = _data.CreateLiveTrainer(_data.CreateDigitNetwork(false));
            double peak = 0.0;
            for (int step = 0; step < 20; step++) peak = Math.Max(peak, huge.Step(live.rates[2]));
            Assert.Greater(peak, 2.5 * live.startLoss, "the loss explodes");
            Assert.AreEqual(live.hugePeakLoss, peak, live.hugePeakLoss * 0.1);
        }

        [Test]
        public void Restore_UndoesABlowUp()
        {
            Level1Live live = _data.File.live;
            DigitNetwork net = _data.CreateDigitNetwork(false);
            LiveTrainer trainer = _data.CreateLiveTrainer(net);
            DigitNetwork saved = net.Clone();
            for (int step = 0; step < 10; step++) trainer.Step(live.rates[2]);
            trainer.Restore(saved);
            CollectionAssert.AreEqual(saved.W1, net.W1);
            Assert.IsTrue(double.IsNaN(trainer.SmoothedLoss));
        }

        [Test]
        public void StudyingOneDigit_TeachesTheNetworkThatDigit()
        {
            // Something the trained network does not call a 3: the hero 7-like stroke below. Studying it as a 3 makes it a 3.
            double[] seven = Draw(Digits[7]).ToPixels7();
            DigitNetwork net = _data.CreateDigitNetwork(true);
            var h = new double[16];
            var p = new double[10];
            Assert.AreNotEqual(3, net.Forward(seven, h, p));
            LiveTrainer trainer = _data.CreateLiveTrainer(net);
            int steps = 0;
            while (net.Forward(seven, h, p) != 3 && steps < 200)
            {
                trainer.Step(_data.File.live.rates[1], seven, 3);
                steps++;
            }

            Assert.AreEqual(3, net.Forward(seven, h, p), $"still not a 3 after {steps} steps");
        }

        // ---------- the canvas ----------

        [Test]
        public void EmptyCanvas_HasNoInk()
        {
            var canvas = new DigitCanvas();
            Assert.IsFalse(canvas.HasEnoughInk);
            foreach (double v in canvas.ToPixels7()) Assert.AreEqual(0.0, v);
        }

        [Test]
        public void HandDrawnDigits_AreReadByTheTrainedNetwork()
        {
            DigitNetwork net = _data.CreateDigitNetwork(true);
            var h = new double[16];
            var p = new double[10];
            var wrong = new List<string>();
            foreach (KeyValuePair<int, Vector2[][]> d in Digits)
            {
                DigitCanvas canvas = Draw(d.Value);
                Assert.IsTrue(canvas.HasEnoughInk, $"digit {d.Key} has too little ink");
                int guess = net.Forward(canvas.ToPixels7(), h, p);
                if (guess != d.Key) wrong.Add($"{d.Key} read as {guess}");
            }

            Assert.IsEmpty(wrong, string.Join(", ", wrong));
        }

        [Test]
        public void ADigitDrawnSmallInACorner_IsTheSameInput()
        {
            double[] big = Draw(Digits[3]).ToPixels7();
            double[] small = Draw(Digits[3], 0.35f, new Vector2(0.05f, 0.6f)).ToPixels7();
            for (int i = 0; i < 49; i++) Assert.AreEqual(big[i], small[i], 1e-4, $"pixel {i}");
        }

        [Test]
        public void AShakyThree_IsStillAThree()
        {
            var rng = new System.Random(4);
            Vector2[][] shaky = Array.ConvertAll(Digits[3], s => Array.ConvertAll(Densify(s, 0.01f),
                v => v + new Vector2((float)(rng.NextDouble() - 0.5) * 0.03f, (float)(rng.NextDouble() - 0.5) * 0.03f)));
            DigitNetwork net = _data.CreateDigitNetwork(true);
            Assert.AreEqual(3, net.Forward(Draw(shaky).ToPixels7(), new double[16], new double[10]));
        }

        [Test]
        public void TheNumbers_AreMnistLike()
        {
            // A drawn 3 carries about as much ink as the real threes in the bake (sums of their 49 pixels).
            double drawn = Sum(Draw(Digits[3]).ToPixels7());
            double real = 0.0;
            int n = 0;
            foreach (Level1Digit d in _data.File.puzzle.digits)
            {
                if (d.label != 3) continue;
                real += Sum(d.pixels7);
                n++;
            }

            Assert.AreEqual(real / n, drawn, 2.5);
        }

        private static double Sum(double[] v)
        {
            double s = 0.0;
            foreach (double x in v) s += x;
            return s;
        }

        private static DigitCanvas Draw(Vector2[][] strokes, float scale = 1f, Vector2 offset = default)
        {
            var canvas = new DigitCanvas();
            foreach (Vector2[] s in strokes)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    Vector2 v = s[i] * scale + offset;
                    if (i == 0) canvas.BeginStroke(v.x, v.y);
                    else canvas.AddPoint(v.x, v.y);
                }
            }

            return canvas;
        }

        private static Vector2[] Densify(Vector2[] s, float step)
        {
            var points = new List<Vector2> { s[0] };
            for (int i = 1; i < s.Length; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(s[i - 1], s[i]) / step));
                for (int k = 1; k <= n; k++) points.Add(Vector2.Lerp(s[i - 1], s[i], k / (float)n));
            }

            return points.ToArray();
        }

        private static Vector2[] Arc(float cx, float cy, float rx, float ry, float from, float to, int n)
        {
            var points = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(from, to, i / (float)(n - 1));
                points[i] = new Vector2(cx + rx * Mathf.Cos(a), cy + ry * Mathf.Sin(a));
            }

            return points;
        }

        private static Vector2[] Join(params Vector2[][] parts)
        {
            var all = new List<Vector2>();
            foreach (Vector2[] p in parts) all.AddRange(p);
            return all.ToArray();
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        // Ten digits as a person might draw them on the canvas (0 to 1, y down).
        private static readonly Dictionary<int, Vector2[][]> Digits = new Dictionary<int, Vector2[][]>
        {
            { 0, new[] { Arc(0.5f, 0.5f, 0.22f, 0.32f, 0f, 360f, 24) } },
            { 1, new[] { new[] { V(0.48f, 0.18f), V(0.52f, 0.82f) } } },
            { 2, new[] { Join(Arc(0.5f, 0.35f, 0.2f, 0.16f, 200f, 380f, 10), new[] { V(0.66f, 0.42f), V(0.3f, 0.82f), V(0.72f, 0.82f) }) } },
            { 3, new[] { Join(Arc(0.5f, 0.33f, 0.19f, 0.15f, 200f, 450f, 12), Arc(0.5f, 0.65f, 0.21f, 0.17f, 270f, 520f, 12)) } },
            { 4, new[] { new[] { V(0.6f, 0.82f), V(0.6f, 0.18f), V(0.28f, 0.6f), V(0.75f, 0.6f) } } },
            { 5, new[] { Join(new[] { V(0.7f, 0.18f), V(0.36f, 0.18f), V(0.33f, 0.47f) }, Arc(0.5f, 0.63f, 0.2f, 0.18f, 230f, 500f, 12)) } },
            { 6, new[] { Join(new[] { V(0.62f, 0.16f), V(0.42f, 0.4f) }, Arc(0.5f, 0.65f, 0.18f, 0.18f, 200f, 560f, 16)) } },
            { 7, new[] { new[] { V(0.28f, 0.2f), V(0.72f, 0.2f), V(0.45f, 0.84f) } } },
            { 8, new[] { Arc(0.5f, 0.32f, 0.16f, 0.14f, 90f, 450f, 14), Arc(0.5f, 0.66f, 0.2f, 0.18f, -90f, 270f, 16) } },
            { 9, new[] { Arc(0.5f, 0.35f, 0.18f, 0.16f, 0f, 360f, 16), new[] { V(0.68f, 0.35f), V(0.62f, 0.85f) } } },
        };
    }
}
