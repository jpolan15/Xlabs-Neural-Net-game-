using NUnit.Framework;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class CurriculumPuzzleTests
    {
        [Test]
        public void ReferenceChecks_CoverOrAndXor()
        {
            string error = CurriculumCatalog.RunReferenceChecks();
            Assert.IsNull(error, error);
        }

        [Test]
        public void Chamber01_PositiveWeightsAndNegativeBias_Pass()
        {
            PuzzleEvaluation eval = CurriculumCatalog.EvaluateSingleNeuron(CurriculumCatalog.Chamber01().Definition, 1.0, 1.0, -0.5);
            Assert.IsTrue(eval.Passed);
            Assert.AreEqual(4, eval.PassedCases);
        }

        [Test]
        public void Chamber01_ZeroWeights_DoNotPass()
        {
            PuzzleEvaluation eval = CurriculumCatalog.EvaluateSingleNeuron(CurriculumCatalog.Chamber01().Definition, 0.0, 0.0, 0.0);
            Assert.IsFalse(eval.Passed);
        }

        [Test]
        public void Chamber03_NegativeIceWeight_Passes()
        {
            PuzzleEvaluation eval = CurriculumCatalog.EvaluateSingleNeuron(CurriculumCatalog.Chamber03().Definition, 1.0, -1.0, -0.5);
            Assert.IsTrue(eval.Passed);
            Assert.AreEqual(0.0, eval.Diagnostics[1].ActualOutput);
            Assert.AreEqual(1.0, eval.Diagnostics[2].ActualOutput);
        }

        [Test]
        public void StepBoundary_ZeroSum_Fires()
        {
            PuzzleEvaluation eval = CurriculumCatalog.EvaluateSingleNeuron(CurriculumCatalog.Chamber01().Definition, 1.0, 0.0, 0.0);
            Assert.AreEqual(0.0, eval.Diagnostics[0].CalculatedZ, 1e-9);
            Assert.AreEqual(1.0, eval.Diagnostics[0].ActualOutput, 1e-9);
        }

        [Test]
        public void GridEdges_NegativeTwoAndPositiveTwo_AreVisited()
        {
            Assert.IsTrue(CurriculumCatalog.SingleNeuronGridSolvable(CurriculumCatalog.Chamber01().Definition, -2.0, 2.0, 2.0));
            Assert.IsFalse(CurriculumCatalog.SingleNeuronGridSolvable(CurriculumCatalog.Chamber04().Definition, -2.0, 2.0, 0.5));
        }

        [Test]
        public void XorHiddenReference_Passes()
        {
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(
                CurriculumCatalog.CreateXorReferenceNetwork(),
                CurriculumCatalog.Chamber04().Definition);
            Assert.IsTrue(eval.Passed);
            Assert.AreEqual(0.0, eval.Diagnostics[0].ActualOutput, 1e-9);
            Assert.AreEqual(1.0, eval.Diagnostics[1].ActualOutput, 1e-9);
            Assert.AreEqual(1.0, eval.Diagnostics[2].ActualOutput, 1e-9);
            Assert.AreEqual(0.0, eval.Diagnostics[3].ActualOutput, 1e-9);
        }
    }
}
