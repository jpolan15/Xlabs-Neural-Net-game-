using System;
using System.Collections.Generic;
using NUnit.Framework;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;

namespace Convergence.Tests.EditMode.Core
{
    [TestFixture]
    public class RideV4PuzzleTests
    {
        // ---------- Stop 3: XOR ----------

        [Test]
        public void Xor_OneNeuron_IsStuckAtThreeOfFour_OverTheLeverRange()
        {
            Assert.AreEqual(3, XorPuzzle.BestSingleNeuronCases(-2.0, 2.0, 0.25));
        }

        [Test]
        public void Xor_OrSettings_ScoreThreeOfFour_AndFail()
        {
            PuzzleEvaluation e = XorPuzzle.EvaluateSingleNeuron(1.0, 1.0, 0.5);
            Assert.AreEqual(3, e.PassedCases);
            Assert.IsFalse(e.Passed);
        }

        [Test]
        public void Xor_HelperFiringOnlyOnBoth_PassesFourOfFour()
        {
            PuzzleEvaluation e = XorPuzzle.EvaluateWithHelper(1.0, 1.0, 0.5, 1.5);
            Assert.IsTrue(e.Passed);
            Assert.AreEqual(4, e.PassedCases);
        }

        [TestCase(0.5, 2)]  // helper fires on one input too: blocks the single cases
        [TestCase(2.5, 3)]  // helper never fires: still plain OR
        [TestCase(0.0, 2)]  // helper always fires: blocks every shot, so only the two "don't shoot" cases are right
        public void Xor_HelperMistuned_Fails(double helperTrigger, int expectedCases)
        {
            PuzzleEvaluation e = XorPuzzle.EvaluateWithHelper(1.0, 1.0, 0.5, helperTrigger);
            Assert.IsFalse(e.Passed);
            Assert.AreEqual(expectedCases, e.PassedCases);
        }

        [Test]
        public void Xor_ZeroWeights_Fail()
        {
            Assert.IsFalse(XorPuzzle.EvaluateWithHelper(0.0, 0.0, 0.5, 1.5).Passed);
        }

        // ---------- Stop 4: analogies ----------

        [Test]
        public void Analogy_AllThreeClockRounds_PassOnlyWhenAllAreWon()
        {
            AnalogyPuzzle p = AnalogyPuzzle.CreateClockRound();
            Assert.IsFalse(p.Passed);
            Assert.IsTrue(p.Try(0, "puppy", "dog", "kitten"));
            Assert.IsTrue(p.Try(1, "Paris", "France", "Tokyo"));
            Assert.IsFalse(p.Passed, "2 of 3 is not a pass");
            Assert.IsTrue(p.Try(2, "rock", "rocks", "drone"));
            Assert.IsTrue(p.Passed);
            p.Reset();
            Assert.AreEqual(0, p.SolvedCount);
        }

        [Test]
        public void Analogy_WrongArrowOrWrongTail_DoesNotCount()
        {
            AnalogyPuzzle p = AnalogyPuzzle.CreateClockRound();
            Assert.IsFalse(p.Try(0, "man", "woman", "kitten"), "the gender arrow does not make a kitten a cat");
            Assert.IsFalse(p.Try(0, "puppy", "dog", "puppy"), "tail must be on the question word");
            Assert.AreEqual(0, p.SolvedCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Try(3, "a", "b", "c"));
        }

        [Test]
        public void Analogy_RoyalArrow_LandsOnQueen()
        {
            var p = new AnalogyPuzzle(WordSpace.CreateRideTable(), new[] { WordSpace.RoyalAnalogy });
            Assert.AreEqual("queen", p.Landing("man", "woman", "king"));
            Assert.IsTrue(p.WithinReach("man", "woman", "king", "queen"));
            Assert.IsFalse(p.WithinReach("man", "woman", "king", "king"));
            Assert.IsTrue(p.Try(0, "man", "woman", "king"));
            Assert.IsTrue(p.Passed);
        }

        [Test]
        public void Analogy_UnknownWord_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new AnalogyPuzzle(WordSpace.CreateRideTable(), new[] { new WordAnalogy("a", "b", "c", "banana") }));
        }

        // ---------- Stop 5: attention ----------

        [Test]
        public void Attention_PassesOnlyAtThreeSpotlightsAndThreeOrders()
        {
            var p = new AttentionPuzzle();
            for (int s = 0; s < p.Sentences.Count; s++)
            {
                Assert.IsTrue(p.AimSpotlight(s, p.Referent(s)));
            }

            Assert.AreEqual(3, p.SpotlightsCorrect);
            Assert.IsFalse(p.Passed);
            for (int i = 0; i < AttentionPuzzle.RequiredOrders; i++)
            {
                Assert.AreEqual("laser", p.WriteOrder(0.2, 0.5));
            }

            Assert.IsTrue(p.Passed);
        }

        [Test]
        public void Attention_WrongSpotlight_DoesNotCount()
        {
            var p = new AttentionPuzzle();
            string[] s = p.Sentences[1];
            int rock = Array.IndexOf(s, "rock");
            Assert.IsFalse(p.AimSpotlight(1, rock), "'she' is the pilot, not the rock");
            Assert.AreEqual(0, p.SpotlightsCorrect);
        }

        [Test]
        public void Attention_SillyOrder_IsRefused_AndDoesNotCount()
        {
            var p = new AttentionPuzzle();
            string word = p.WriteOrder(2.0, 0.999);
            Assert.AreNotEqual("laser", word);
            Assert.AreEqual(0, p.CorrectOrders);
            Assert.AreEqual(1, p.RefusedOrders);
        }

        [Test]
        public void Attention_Beams_SumToOne()
        {
            var p = new AttentionPuzzle();
            double sum = 0.0;
            foreach (double w in p.Beams(0)) sum += w;
            Assert.AreEqual(1.0, sum, 1e-9);
        }

        // ---------- Stop 6: agent ----------

        [Test]
        public void Agent_LaserAtAFriend_IsUnsafe_AtARock_IsSafe()
        {
            Assert.IsFalse(AgentStepEvaluator.Evaluate(AgentGoal.Home, new AgentAction(AgentTool.Laser, "drone")).Safe);
            Assert.IsTrue(AgentStepEvaluator.Evaluate(AgentGoal.Home, new AgentAction(AgentTool.Laser, "rock")).Safe);
            Assert.IsTrue(AgentStepEvaluator.Evaluate(AgentGoal.Home, new AgentAction(AgentTool.Scanner, "drone")).Safe);
            Assert.IsTrue(AgentStepEvaluator.Evaluate(AgentGoal.Home, new AgentAction(AgentTool.Thrusters, "drone")).Safe);
        }

        [TestCase(AgentGoal.Home)]
        [TestCase(AgentGoal.MineRocks)]
        [TestCase(AgentGoal.RescueDrone)]
        public void Agent_EveryPlan_HasTheUnsafeTwistOnLoopThree_AndEndsOnTheGoal(AgentGoal goal)
        {
            IReadOnlyList<AgentAction> plan = AgentStepEvaluator.Plan(goal);
            for (int i = 0; i < plan.Count; i++)
            {
                AgentStepVerdict v = AgentStepEvaluator.Evaluate(goal, plan[i]);
                Assert.AreEqual(i != AgentStepEvaluator.TwistStep, v.Safe, plan[i].Words);
                Assert.AreEqual(i == plan.Count - 1, v.ReachesGoal, plan[i].Words);
            }
        }

        [TestCase(AgentGoal.Home)]
        [TestCase(AgentGoal.MineRocks)]
        [TestCase(AgentGoal.RescueDrone)]
        public void Agent_StoppingTheUnsafeStep_Wins(AgentGoal goal)
        {
            var e = new AgentEpisode(goal);
            while (!e.Finished)
            {
                if (e.ProposedVerdict.Safe) e.Execute();
                else e.Stop();
            }

            Assert.IsTrue(e.Won);
            Assert.AreEqual(1, e.UnsafeStopped);
            Assert.AreEqual(0, e.UnsafeExecuted);
        }

        [Test]
        public void Agent_LettingTheUnsafeStepRun_Loses_ThenSecondChanceWins()
        {
            var e = new AgentEpisode(AgentGoal.Home);
            e.Execute();
            e.Execute();
            Assert.IsFalse(e.Execute().Safe, "loop 3 fires at the drone");
            Assert.AreEqual(1, e.UnsafeExecuted);
            e.Execute();
            Assert.IsTrue(e.GoalReached);
            Assert.IsFalse(e.Won, "home, but a friend got hurt");

            e.RetryLastStep();
            e.RetryLastStep();
            Assert.AreEqual("fire laser at drone", e.Proposed.Words);
            e.Stop();
            e.Execute();
            e.Execute();
            Assert.IsTrue(e.Won);
        }

        [Test]
        public void Agent_StoppingASafeStep_IsCounted_AndStillReplans()
        {
            var e = new AgentEpisode(AgentGoal.MineRocks);
            Assert.IsTrue(e.Stop().Safe);
            Assert.AreEqual(1, e.SafeStopped);
            Assert.AreEqual(AgentTool.Scanner, e.Proposed.Tool);
        }

        [Test]
        public void Agent_FinishedEpisode_Throws()
        {
            var e = new AgentEpisode(AgentGoal.Home);
            while (!e.Finished) e.Execute();
            Assert.IsNull(e.Proposed);
            Assert.Throws<InvalidOperationException>(() => e.Execute());
            Assert.Throws<ArgumentException>(() => new AgentAction(AgentTool.Laser, ""));
        }

        // ---------- Stop 2: rider vs trainer ----------

        private static readonly double[][] OrInputs = { new[] { 0.0, 0.0 }, new[] { 0.0, 1.0 }, new[] { 1.0, 0.0 }, new[] { 1.0, 1.0 } };
        private static readonly double[][] OrTargets = { new[] { 0.0 }, new[] { 1.0 }, new[] { 1.0 }, new[] { 1.0 } };

        private static NetworkModel Neuron(double w1, double w2) =>
            NetworkModel.CreateSingleNeuronNetwork(2, new[] { w1, w2 }, -0.5, ActivationType.Sigmoid);

        [Test]
        public void ManualDescent_KeepsTheLowestLoss()
        {
            var score = new ManualDescentScore();
            score.Record(Neuron(-1.5, 1.5), OrInputs, OrTargets, LossKind.MeanSquaredError);
            double first = score.BestLoss;
            score.Record(Neuron(1.5, 1.5), OrInputs, OrTargets, LossKind.MeanSquaredError);
            Assert.Less(score.BestLoss, first);
            score.Record(Neuron(-2.0, -2.0), OrInputs, OrTargets, LossKind.MeanSquaredError);
            Assert.AreEqual(2, score.BestWeights.Length);
            Assert.AreEqual(1.5, score.BestWeights[0]);
            Assert.AreEqual(3, score.Probes);
        }

        [Test]
        public void ManualDescent_CountsMistakes()
        {
            Assert.AreEqual(0, ManualDescentScore.CountMistakes(Neuron(2.0, 2.0), OrInputs, OrTargets));
            Assert.AreEqual(2, ManualDescentScore.CountMistakes(Neuron(-1.5, 1.5), OrInputs, OrTargets), "the plan's start is 2 of 4 wrong");
            Assert.AreEqual(3, ManualDescentScore.CountMistakes(Neuron(0.0, 0.0), OrInputs, OrTargets));
        }

        [Test]
        public void ManualDescent_ComparesWithTheTrainer()
        {
            var score = new ManualDescentScore();
            Assert.AreEqual(DescentWinner.Trainer, score.Compare(0.5), "no probe yet");
            score.Record(Neuron(1.0, 1.0), OrInputs, OrTargets, LossKind.MeanSquaredError);
            Assert.AreEqual(DescentWinner.Rider, score.Compare(score.BestLoss + 0.1));
            Assert.AreEqual(DescentWinner.Trainer, score.Compare(score.BestLoss - 0.1));
            Assert.AreEqual(DescentWinner.Tie, score.Compare(score.BestLoss));

            var trained = Neuron(-1.5, 1.5);
            for (int i = 0; i < 400; i++) BatchGradientDescent.Step(trained, OrInputs, OrTargets, 2.0, LossKind.MeanSquaredError);
            double trainerLoss = BatchGradientDescent.MeanLoss(trained, OrInputs, OrTargets, LossKind.MeanSquaredError);
            Assert.AreEqual(DescentWinner.Trainer, score.Compare(trainerLoss), "AURA beats a 1/1 hand guess");
            score.Reset();
            Assert.AreEqual(0, score.Probes);
        }
    }
}
