using NUnit.Framework;
using Convergence.Core.Puzzles;

namespace Convergence.Tests.EditMode.Core
{
    /// <summary>The v5 mini-game rules: draw the 3, paint with two brushes, plug the strings, roll it down.</summary>
    [TestFixture]
    public class Level1GamesTests
    {
        // A 3x3 "digit": the middle column is ink.
        private static readonly double[] Column = { 0, 0.9, 0, 0, 0.8, 0, 0, 0.5, 0 };

        [Test]
        public void Draw_PassesWhenTheOutlineIsCovered()
        {
            var draw = new DrawPuzzle(Column, inkLevel: 0.3, coverNeeded: 1.0, strayAllowed: 1);
            Assert.AreEqual(3, draw.TargetCount);
            draw.Set(1, true);
            draw.Set(4, true);
            Assert.IsFalse(draw.Passed);
            Assert.AreEqual(7, draw.Hint(out bool ink));
            Assert.IsTrue(ink);
            draw.Set(7, true);
            Assert.IsTrue(draw.Passed);
            Assert.AreEqual(-1, draw.Hint(out _));
        }

        [Test]
        public void Draw_AScribbleOverEverythingNeverPasses()
        {
            var draw = new DrawPuzzle(Column, inkLevel: 0.3, coverNeeded: 1.0, strayAllowed: 1);
            for (int c = 0; c < 9; c++) draw.Set(c, true);
            Assert.AreEqual(6, draw.Stray);
            Assert.IsFalse(draw.Passed);
            Assert.AreEqual(0, draw.Hint(out bool ink));
            Assert.IsFalse(ink, "with too much stray ink the hint erases");
        }

        [Test]
        public void Paint_GreenAndRedBrushes_SameColourAgainClears()
        {
            Assert.AreEqual(1, WeightPaintPuzzle.PaintValue(0, 1));
            Assert.AreEqual(-1, WeightPaintPuzzle.PaintValue(0, -1));
            Assert.AreEqual(-1, WeightPaintPuzzle.PaintValue(1, -1));
            Assert.AreEqual(0, WeightPaintPuzzle.PaintValue(1, 1));
            Assert.AreEqual(0, WeightPaintPuzzle.PaintValue(-1, -1));
        }

        [Test]
        public void Plug_IntoTheWantedAnswer_RaisesItUntilItWins()
        {
            // Answer 1 leads by 1.0; each string adds brightness × 2.
            var plug = new PlugPuzzle(new[] { 0.0, 1.0, 0.0 }, new[] { 0.3, 0.4 }, wanted: 2, plugWeight: 2.0);
            Assert.AreEqual(1, plug.Guess);
            Assert.AreEqual(1, plug.Hint(), "the brightest string first");
            plug.Plug(1, 2);
            Assert.AreEqual(0.8, plug.Logit(2), 1e-12);
            Assert.IsFalse(plug.Passed);
            plug.Plug(0, 2);
            Assert.IsTrue(plug.Passed);
            Assert.Less(plug.Gap, 0);
            Assert.AreEqual(1.0, Sum(plug.Probabilities()), 1e-12);
        }

        [Test]
        public void Plug_IntoAWrongAnswer_MakesItStronger()
        {
            var plug = new PlugPuzzle(new[] { 0.0, 1.0, 0.0 }, new[] { 0.5 }, wanted: 2, plugWeight: 2.0);
            double before = plug.Gap;
            plug.Plug(0, 1);
            Assert.Greater(plug.Gap, before);
            plug.Plug(0, -1);
            Assert.AreEqual(before, plug.Gap, 1e-12);
        }

        [Test]
        public void Descent_MediumStepsReachTheBottom_BigStepsOvershoot()
        {
            var game = new DescentGame();
            double before = game.X;
            double after = game.Step(1.8);
            Assert.IsTrue(DescentGame.Overshoots(before, after, game.Bottom), "a step size over 1 / (2 curve) jumps past the bottom");

            game.Reset();
            int steps = 0;
            while (!game.Passed && steps < 20)
            {
                game.Step(0.7);
                steps++;
            }

            Assert.IsTrue(game.Passed);
            Assert.LessOrEqual(game.Steps, game.Par);
            Assert.AreEqual(3, game.Stars);
            Assert.Less(game.Loss(), game.Loss(game.Start));
        }

        [Test]
        public void Descent_TinyStepsAreSlow()
        {
            var game = new DescentGame();
            for (int i = 0; i < game.Par; i++) game.Step(0.1);
            Assert.IsFalse(game.Passed);
        }

        private static double Sum(double[] p)
        {
            double s = 0;
            foreach (double v in p) s += v;
            return s;
        }
    }
}
