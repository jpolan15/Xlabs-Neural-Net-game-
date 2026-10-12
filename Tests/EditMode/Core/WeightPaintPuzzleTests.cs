using System;
using NUnit.Framework;
using Convergence.Core.Puzzles;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class WeightPaintPuzzleTests
    {
        // 3 pixels: a "one" has ink in the middle, a "zero" on the sides.
        private static WeightPaintPuzzle Small() => new WeightPaintPuzzle(
            new[]
            {
                new[] { 0.0, 1.0, 0.0 },
                new[] { 0.1, 0.9, 0.1 },
                new[] { 1.0, 0.0, 1.0 },
                new[] { 0.8, 0.4, 0.9 },
            },
            new[] { true, true, false, false },
            0.5);

        [Test]
        public void Evaluate_SumsInkTimesWeight_AndFiresAtTheLine()
        {
            var puzzle = Small();
            var result = puzzle.Evaluate(new[] { -1, 1, -1 });

            Assert.AreEqual(1.0, result.Sum(0), 1e-12);
            Assert.AreEqual(0.7, result.Sum(1), 1e-12);
            Assert.AreEqual(-2.0, result.Sum(2), 1e-12);
            Assert.AreEqual(-1.3, result.Sum(3), 1e-12);
            Assert.IsTrue(result.Fired(0));
            Assert.IsFalse(result.Fired(2));
            Assert.IsTrue(result.Passed);
            Assert.AreEqual(4, result.CorrectCount);
            Assert.AreEqual(-0.8, result.Contribution(3, 0), 1e-12);
            Assert.AreEqual(0.0, result.Contribution(0, 0), 1e-12);
        }

        [Test]
        public void Evaluate_TotalExactlyOnTheLine_Fires()
        {
            var puzzle = new WeightPaintPuzzle(new[] { new[] { 0.5, 0.0 } }, new[] { true }, 0.5);
            Assert.IsTrue(puzzle.Evaluate(new[] { 1, 0 }).Fired(0));
            Assert.IsFalse(puzzle.Evaluate(new[] { 0, 0 }).Fired(0));
        }

        [Test]
        public void FirstWrong_ReportsWhetherItFiredWrongly()
        {
            var puzzle = Small();
            var allPlus = puzzle.Evaluate(new[] { 1, 1, 1 });
            Assert.AreEqual(2, allPlus.FirstWrong(out bool firedWrongly));
            Assert.IsTrue(firedWrongly);

            var empty = puzzle.Evaluate(new[] { 0, 0, 0 });
            Assert.AreEqual(0, empty.FirstWrong(out firedWrongly));
            Assert.IsFalse(firedWrongly);

            Assert.AreEqual(-1, puzzle.Evaluate(new[] { -1, 1, -1 }).FirstWrong(out _));
        }

        [Test]
        public void Hint_IsNullWhenSolved_AndSolvesFromEmpty()
        {
            var puzzle = Small();
            Assert.IsNull(puzzle.Hint(new[] { -1, 1, -1 }));

            var weights = new int[3];
            int moves = 0;
            for (var hint = puzzle.Hint(weights); hint.HasValue; hint = puzzle.Hint(weights))
            {
                Assert.AreNotEqual(weights[hint.Value.Cell], hint.Value.Value, "a hint must change something");
                weights[hint.Value.Cell] = hint.Value.Value;
                Assert.LessOrEqual(++moves, 3);
            }
            Assert.IsTrue(puzzle.Evaluate(weights).Passed);
        }

        [Test]
        public void NextValue_CyclesZeroPlusMinus()
        {
            Assert.AreEqual(1, WeightPaintPuzzle.NextValue(0));
            Assert.AreEqual(-1, WeightPaintPuzzle.NextValue(1));
            Assert.AreEqual(0, WeightPaintPuzzle.NextValue(-1));
        }

        [Test]
        public void InvalidInput_Throws()
        {
            var puzzle = Small();
            Assert.Throws<ArgumentException>(() => puzzle.Evaluate(new[] { 1, 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => puzzle.Evaluate(new[] { 2, 0, 0 }));
            Assert.Throws<ArgumentNullException>(() => puzzle.Hint(null));
            Assert.Throws<ArgumentException>(() => new WeightPaintPuzzle(new[] { new[] { 1.0 } }, new[] { true, false }, 0.0));
            Assert.Throws<ArgumentException>(() => new WeightPaintPuzzle(new[] { new[] { 1.0 }, new[] { 1.0, 0.0 } }, new[] { true, false }, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeightPaintPuzzle(new[] { new[] { 1.0 } }, new[] { true }, double.NaN));
        }
    }
}
