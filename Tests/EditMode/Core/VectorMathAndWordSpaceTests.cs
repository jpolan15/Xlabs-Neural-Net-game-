using System;
using System.Collections.Generic;
using NUnit.Framework;
using Convergence.Core.Math;
using Convergence.Core.Neural;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class VectorMathAndWordSpaceTests
    {
        private const double Tol = 1e-12;

        [Test]
        public void AddSubDot_PositiveNegativeAndZero()
        {
            double[] a = { 1.0, -2.0, 0.0 };
            double[] b = { 0.5, 3.0, 0.0 };
            CollectionAssert.AreEqual(new[] { 1.5, 1.0, 0.0 }, VectorMath.Add(a, b));
            CollectionAssert.AreEqual(new[] { 0.5, -5.0, 0.0 }, VectorMath.Sub(a, b));
            Assert.AreEqual(0.5 - 6.0, VectorMath.Dot(a, b), Tol);
            Assert.AreEqual(0.0, VectorMath.Dot(a, new double[3]), Tol);
        }

        [Test]
        public void Add_DoesNotChangeInputs()
        {
            double[] a = { 1.0, 2.0 };
            VectorMath.Add(a, a);
            CollectionAssert.AreEqual(new[] { 1.0, 2.0 }, a);
        }

        [Test]
        public void LengthMismatchAndNull_Throw()
        {
            Assert.Throws<ArgumentException>(() => VectorMath.Dot(new double[2], new double[3]));
            Assert.Throws<ArgumentNullException>(() => VectorMath.Add(null, new double[1]));
            Assert.Throws<ArgumentNullException>(() => VectorMath.Nearest(null, new List<double[]>()));
        }

        [Test]
        public void Cosine_SameOppositeOrthogonalAndZero()
        {
            Assert.AreEqual(1.0, VectorMath.Cosine(new[] { 2.0, 0.0 }, new[] { 5.0, 0.0 }), Tol);
            Assert.AreEqual(-1.0, VectorMath.Cosine(new[] { 2.0, 0.0 }, new[] { -1.0, 0.0 }), Tol);
            Assert.AreEqual(0.0, VectorMath.Cosine(new[] { 1.0, 0.0 }, new[] { 0.0, 3.0 }), Tol);
            Assert.AreEqual(0.0, VectorMath.Cosine(new[] { 0.0, 0.0 }, new[] { 1.0, 1.0 }), Tol);
        }

        [Test]
        public void Nearest_SkipsExcludedAndBreaksTiesLow()
        {
            var table = new List<double[]> { new[] { 0.0 }, new[] { 1.0 }, new[] { -1.0 } };
            Assert.AreEqual(0, VectorMath.Nearest(new[] { 0.1 }, table));
            Assert.AreEqual(1, VectorMath.Nearest(new[] { 0.1 }, table, new HashSet<int> { 0 }));
            Assert.AreEqual(1, VectorMath.Nearest(new[] { 0.0 }, table, new HashSet<int> { 0 }), "tie → lower index");
            Assert.AreEqual(-1, VectorMath.Nearest(new[] { 0.0 }, table, new HashSet<int> { 0, 1, 2 }));
        }

        [Test]
        public void KingMinusManPlusWoman_IsQueen()
        {
            WordSpace s = WordSpace.CreateRideTable();
            Assert.AreEqual("queen", s.Solve("man", "woman", "king"));
            double[] tip = VectorMath.Add(VectorMath.Sub(s.VectorOf("king"), s.VectorOf("man")), s.VectorOf("woman"));
            Assert.AreEqual("queen", s.Nearest(tip, new[] { "king", "man", "woman" }));
        }

        [Test]
        public void Nearest_WithoutExclusion_CanReturnAQueryWord_WithExclusionNever()
        {
            WordSpace s = WordSpace.CreateRideTable();
            // Carrying a zero-length arrow lands back on the tail itself.
            Assert.AreEqual("king", s.Nearest(s.CarryArrow("man", "man", "king")));
            Assert.AreNotEqual("king", s.Nearest(s.CarryArrow("man", "man", "king"), new[] { "man", "king" }));
        }

        [Test]
        public void EveryRideAnalogy_ResolvesToItsAnswer()
        {
            WordSpace s = WordSpace.CreateRideTable();
            var all = new List<WordAnalogy>(WordSpace.ClockAnalogies) { WordSpace.RoyalAnalogy };
            foreach (WordAnalogy a in all)
            {
                Assert.AreEqual(a.Answer, s.Solve(a.From, a.To, a.Query), a.ToString());
                // The reverse direction holds too.
                Assert.AreEqual(a.From, s.Solve(a.Answer, a.Query, a.To), "reverse of " + a);
            }
        }

        [Test]
        public void AnswersAreNear_NotExactlyOn_TheArrowTip()
        {
            WordSpace s = WordSpace.CreateRideTable();
            double miss = VectorMath.Distance(s.CarryArrow("man", "woman", "king"), s.VectorOf("queen"));
            Assert.Greater(miss, 0.0);
            Assert.Less(miss, 0.1);
        }

        [Test]
        public void Clusters_AreTighterThanTheGapsBetweenThem()
        {
            WordSpace s = WordSpace.CreateRideTable();
            double maxInside = 0.0;
            double minBetween = double.PositiveInfinity;
            for (int c = 0; c < s.Clusters.Count; c++)
            {
                foreach (string a in s.Clusters[c])
                {
                    foreach (string b in s.Clusters[c]) maxInside = Math.Max(maxInside, s.Distance(a, b));
                    for (int d = 0; d < s.Clusters.Count; d++)
                    {
                        if (d == c) continue;
                        foreach (string b in s.Clusters[d]) minBetween = Math.Min(minBetween, s.Distance(a, b));
                    }
                }
            }

            Assert.Less(maxInside, minBetween, $"inside {maxInside:F3} vs between {minBetween:F3}");
            Assert.AreEqual(5, s.Clusters.Count);
            Assert.AreEqual(20, s.Words.Count);
        }

        [Test]
        public void EveryWord_FitsTheRoom()
        {
            WordSpace s = WordSpace.CreateRideTable();
            foreach (double[] v in s.Vectors)
            {
                Assert.AreEqual(WordSpace.Dimensions, v.Length);
                foreach (double x in v) Assert.That(Math.Abs(x), Is.LessThanOrEqualTo(1.0));
            }
        }

        [Test]
        public void UnknownWord_Throws()
        {
            WordSpace s = WordSpace.CreateRideTable();
            Assert.IsFalse(s.Contains("banana"));
            Assert.Throws<ArgumentException>(() => s.IndexOf("banana"));
            Assert.Throws<ArgumentException>(() => new WordAnalogy("a", "", "c", "d"));
        }
    }
}
