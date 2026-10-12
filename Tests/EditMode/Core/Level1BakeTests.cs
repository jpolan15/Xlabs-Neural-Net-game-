using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Convergence.Core.Math;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay.Ride.Level1;
using UnityEditor;
using UnityEngine;

namespace Convergence.Tests.EditMode.Core
{
    /// <summary>
    /// Core reproduces the offline bake exactly: what glows in Level 1 is what Core computes from the baked weights.
    /// </summary>
    [TestFixture]
    public class Level1BakeTests
    {
        private const double Golden = 1e-6;
        private Level1Data _data;

        [OneTimeSetUp]
        public void Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level1Data.AssetPath);
            Assert.IsNotNull(asset, Level1Data.AssetPath + " is missing; run ArtSource/training/bake_level1.py");
            _data = Level1Data.Parse(asset.text);
        }

        private static int ArgMax(double[] v)
        {
            int best = 0;
            for (int i = 1; i < v.Length; i++) if (v[i] > v[best]) best = i;
            return best;
        }

        private static void AssertHidden(NetworkModel net, double[] expected)
        {
            var hidden = net.Layers[0].Neurons;
            Assert.AreEqual(expected.Length, hidden.Count);
            for (int j = 0; j < hidden.Count; j++) Assert.AreEqual(expected[j], hidden[j].LastOutput, Golden, $"hidden {j}");
        }

        [Test]
        public void UntrainedNetwork_ReproducesGoldenActivations_AndGuessesWrong()
        {
            var hero = _data.File.hero;
            var net = _data.CreateUntrained();
            double[] outputs = net.Forward(hero.pixels7);

            AssertHidden(net, hero.untrainedHidden);
            for (int k = 0; k < 10; k++) Assert.AreEqual(hero.untrainedOutputs[k], outputs[k], Golden, $"output {k}");
            Assert.AreEqual(hero.untrainedGuess, ArgMax(outputs));
            Assert.AreNotEqual(Level1Data.HeroDigit, hero.untrainedGuess);
        }

        [Test]
        public void TrainedNetwork_ReproducesGoldenActivations_AndReadsThree()
        {
            var hero = _data.File.hero;
            var net = _data.CreateTrained();
            double[] outputs = net.Forward(hero.pixels7);

            AssertHidden(net, hero.trainedHidden);
            for (int k = 0; k < 10; k++) Assert.AreEqual(hero.trainedOutputs[k], outputs[k], Golden, $"output {k}");
            Assert.AreEqual(Level1Data.HeroDigit, ArgMax(outputs));
            Assert.Greater(outputs[Level1Data.HeroDigit], 0.8);
        }

        [Test]
        public void FinaleDigits_CorePredictionsMatchTheBake()
        {
            var net = _data.CreateTrained();
            int right = 0;
            foreach (var digit in _data.File.finale.digits)
            {
                double[] outputs = net.Forward(digit.pixels7);
                Assert.AreEqual(digit.prediction, ArgMax(outputs), $"test digit #{digit.testIndex}");
                Assert.AreEqual(digit.confidence, outputs.Max(), Golden);
                if (digit.prediction == digit.label) right++;
            }
            Assert.AreEqual(_data.File.finale.correct, right);
            Assert.AreEqual(60000, _data.File.trainCount);
            Assert.That(_data.TestAccuracyPercent, Is.InRange(80, 98));
        }

        [Test]
        public void Puzzle_ReferencePasses_EmptyAndAllPlusFail()
        {
            var puzzle = _data.CreatePuzzle();
            Assert.AreEqual(49, puzzle.CellCount);
            Assert.IsTrue(puzzle.Evaluate(_data.File.puzzle.referenceSolution).Passed, "reference solution");
            Assert.IsFalse(puzzle.Evaluate(new int[49]).Passed, "empty grid");
            Assert.IsFalse(puzzle.Evaluate(Enumerable.Repeat(1, 49).ToArray()).Passed, "everything +");
        }

        [Test]
        public void Puzzle_FollowingHints_SolvesFromEmptyWithin49Moves()
        {
            var puzzle = _data.CreatePuzzle();
            var weights = new int[49];
            int moves = 0;
            for (var hint = puzzle.Hint(weights); hint.HasValue; hint = puzzle.Hint(weights))
            {
                weights[hint.Value.Cell] = hint.Value.Value;
                Assert.LessOrEqual(++moves, 49);
            }
            Assert.IsTrue(puzzle.Evaluate(weights).Passed);
            Assert.AreEqual(_data.File.puzzle.hintMovesFromEmpty, moves, "Core's hint walks the same path as the bake's");
        }

        [Test]
        public void Puzzle_FollowingHints_SolvesFromRandomStates()
        {
            var puzzle = _data.CreatePuzzle();
            var random = new System.Random(20261020);
            for (int trial = 0; trial < 25; trial++)
            {
                var weights = new int[49];
                for (int i = 0; i < 49; i++) weights[i] = random.Next(-1, 2);
                int moves = 0;
                for (var hint = puzzle.Hint(weights); hint.HasValue; hint = puzzle.Hint(weights))
                {
                    weights[hint.Value.Cell] = hint.Value.Value;
                    Assert.LessOrEqual(++moves, 98, $"trial {trial} did not converge");
                }
                Assert.IsTrue(puzzle.Evaluate(weights).Passed);
            }
        }

        [Test]
        public void Blame_IsTheRealGradient_AndItsTopWiresAreStable()
        {
            var hero = _data.File.hero;
            List<(int layer, int neuron, int input, double grad)> Blame()
            {
                var net = _data.CreateUntrained();
                double[] p = net.Forward(hero.pixels7);
                var dLdp = new double[p.Length];
                LossFunctions.CategoricalCrossEntropyGradient(p, Level1Data.HeroDigit, dLdp);
                net.Backward(dLdp);
                var all = new List<(int, int, int, double)>();
                for (int l = 0; l < net.Layers.Count; l++)
                    for (int n = 0; n < net.Layers[l].NeuronCount; n++)
                        for (int i = 0; i < net.Layers[l].InputCount; i++)
                            all.Add((l, n, i, net.Layers[l].Neurons[n].GetWeightGradient(i)));
                return all.OrderByDescending(w => Math.Abs(w.Item4)).ToList();
            }

            var first = Blame();
            var second = Blame();
            Assert.Greater(Math.Abs(first[0].grad), 1e-3, "blame must be non-zero");
            for (int k = 0; k < 12; k++)
            {
                Assert.AreEqual(first[k].layer, second[k].layer);
                Assert.AreEqual(first[k].neuron, second[k].neuron);
                Assert.AreEqual(first[k].input, second[k].input);
            }

            // Each of the top wires matches a finite-difference derivative of the real loss.
            foreach (var wire in first.Take(5))
            {
                double Loss(double delta)
                {
                    var net = _data.CreateUntrained();
                    var neuron = net.Layers[wire.layer].Neurons[wire.neuron];
                    neuron.SetWeight(wire.input, neuron.GetWeight(wire.input) + delta);
                    return LossFunctions.CategoricalCrossEntropy(net.Forward(hero.pixels7), Level1Data.HeroDigit);
                }
                const double h = 1e-6;
                double numeric = (Loss(h) - Loss(-h)) / (2 * h);
                Assert.AreEqual(numeric, wire.grad, 1e-5 + 1e-4 * Math.Abs(numeric));
            }
        }

        [Test]
        public void Valley_HopsSitOnTheRibbonCentre_OvershootThenDescend()
        {
            var valley = _data.File.valley;
            var hops = valley.hops;
            Assert.AreEqual(0, hops[0].step);
            Assert.AreEqual(1, hops[1].realSteps, "hop 1 is the single big step");
            Assert.Greater(hops[1].loss, hops[0].loss, "the big step overshoots: the loss rises");
            for (int k = 2; k < hops.Length; k++) Assert.Less(hops[k].loss, hops[k - 1].loss, $"hop {k} goes downhill");
            Assert.AreEqual(_data.File.totalSteps, hops[hops.Length - 1].step);
            Assert.AreEqual(_data.File.totalSteps, hops.Sum(h => h.realSteps));

            int centre = _data.ValleyCentreColumn;
            foreach (var hop in hops)
            {
                Assert.That(hop.row, Is.InRange(0, valley.rows - 1));
                Assert.AreEqual(hop.loss, _data.ValleyLoss(hop.row, centre), Golden, $"hop at step {hop.step}");
            }

            // The big step crosses lower ground: the bottom it flew past.
            double dip = double.MaxValue;
            for (int r = hops[0].row; r <= hops[1].row; r++) dip = Math.Min(dip, _data.ValleyLoss(r, centre));
            Assert.Less(dip, hops[0].loss - 0.05);
            Assert.Less(hops[hops.Length - 1].loss, 0.5, "the trained weights sit low in the valley");
        }
    }
}
