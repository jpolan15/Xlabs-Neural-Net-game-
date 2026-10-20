using System;
using System.Linq;
using NUnit.Framework;
using Convergence.Core.Math;
using Convergence.Core.Neural;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class NextTokenModelTests
    {
        private static readonly string[] Prompt = NextTokenModel.OrderPromptTokens.ToArray();

        [TestCase(0, "rock")]
        [TestCase(1, "pilot")]
        [TestCase(2, "drones")]
        public void Pronoun_AttendsMostToItsReferent(int sentence, string referent)
        {
            var m = new NextTokenModel();
            string[] s = NextTokenModel.SpotlightSentences[sentence];
            int top = m.TopAttention(s, s.Length - 1);
            Assert.AreEqual(referent, s[top]);
            double[] row = m.AttentionRow(s, s.Length - 1);
            Assert.Greater(row[top], 0.5, "the referent beam is clearly the brightest");
        }

        [Test]
        public void AttentionRow_SumsToOne_AndNeverLooksAhead()
        {
            var m = new NextTokenModel();
            string[] s = NextTokenModel.SpotlightSentences[0];
            for (int q = 0; q < s.Length; q++)
            {
                double[] row = m.AttentionRow(s, q);
                Assert.AreEqual(1.0, row.Sum(), 1e-9);
                for (int k = q + 1; k < s.Length; k++) Assert.AreEqual(0.0, row[k]);
                if (q > 0) Assert.AreEqual(0.0, row[q], "a word looks back, not at itself");
            }

            Assert.AreEqual(1.0, m.AttentionRow(s, 0)[0], 1e-12, "the first word can only look at itself");
        }

        [Test]
        public void ColdTemperature_PicksLaser()
        {
            var m = new NextTokenModel();
            Assert.AreEqual("laser", m.TopK(Prompt, 0.2, 1)[0].Token);
            Assert.Greater(m.TopK(Prompt, 0.2, 1)[0].Probability, 0.99);
            for (double u = 0.0; u < 0.99; u += 0.05) Assert.AreEqual("laser", m.Sample(Prompt, 0.2, u));
        }

        [Test]
        public void AtTemperatureOne_LaserLeads_BananaIsTiny()
        {
            var m = new NextTokenModel();
            double[] p = m.NextProbabilities(Prompt, 1.0);
            int laser = m.OutputVocabulary.ToList().IndexOf("laser");
            int banana = m.OutputVocabulary.ToList().IndexOf("banana");
            Assert.That(p[laser], Is.InRange(0.75, 0.95));
            Assert.Less(p[banana], 0.01);
        }

        [Test]
        public void HotTemperature_SpreadsProbability()
        {
            var m = new NextTokenModel();
            double cold = m.TopK(Prompt, 0.2, 1)[0].Probability;
            double hot = m.TopK(Prompt, 2.0, 1)[0].Probability;
            Assert.Less(hot, 0.6);
            Assert.Less(hot, cold);
            int banana = m.OutputVocabulary.ToList().IndexOf("banana");
            Assert.Greater(m.NextProbabilities(Prompt, 2.0)[banana], m.NextProbabilities(Prompt, 0.2)[banana]);
            // A hot draw near the top of [0, 1) writes something silly.
            Assert.AreNotEqual("laser", m.Sample(Prompt, 2.0, 0.99));
        }

        [TestCase(0.2)]
        [TestCase(1.0)]
        [TestCase(2.0)]
        [TestCase(10.0)]
        public void Probabilities_SumToOne(double t)
        {
            var m = new NextTokenModel();
            Assert.AreEqual(1.0, m.NextProbabilities(Prompt, t).Sum(), 1e-9);
        }

        [Test]
        public void TopK_IsSortedAndClipped()
        {
            var m = new NextTokenModel();
            var top = m.TopK(Prompt, 1.0, 3);
            Assert.AreEqual(3, top.Count);
            Assert.GreaterOrEqual(top[0].Probability, top[1].Probability);
            Assert.GreaterOrEqual(top[1].Probability, top[2].Probability);
            Assert.AreEqual(m.OutputVocabulary.Count, m.TopK(Prompt, 1.0, 99).Count);
        }

        [Test]
        public void BadInput_Throws()
        {
            var m = new NextTokenModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => m.NextProbabilities(Prompt, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.NextProbabilities(Prompt, -1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.Sample(Prompt, 1.0, 1.0));
            Assert.Throws<ArgumentException>(() => m.NextLogits(new[] { "fire", "the", "zorp" }));
            Assert.Throws<ArgumentException>(() => m.NextLogits(new string[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.AttentionRow(Prompt, Prompt.Length));
        }

        [Test]
        public void SoftmaxWithTemperature_OneMatchesPlainSoftmax()
        {
            double[] z = { 1.0, -2.0, 0.0, 3.5 };
            CollectionAssert.AreEqual(ActivationFunctions.Softmax(z), ActivationFunctions.Softmax(z, 1.0));
            double[] hot = ActivationFunctions.Softmax(z, 1e6);
            foreach (double p in hot) Assert.AreEqual(0.25, p, 1e-5);
            Assert.Throws<ArgumentOutOfRangeException>(() => ActivationFunctions.Softmax(z, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => ActivationFunctions.Softmax(z, double.NaN));
        }
    }
}
