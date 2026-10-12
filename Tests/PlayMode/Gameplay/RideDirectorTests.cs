using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Tests.PlayMode.Gameplay
{
    /// <summary>
    /// Plays Level 1 (the NeuralRide scene) headless. The tests touch the ride only the way a rider or operator does:
    /// GO through its channel, taps through the puzzle's Cycle, Space through SkipNarration, G through GentleMode. They
    /// never set a moment to done. Replaces the v3 tests of the three stops (trigger, OR gate, learning rate), whose
    /// content Level 1 cut (TASK_LEVEL1_DIGITS_RESTRUCTURE, progress log).
    /// </summary>
    [TestFixture]
    public class RideDirectorTests
    {
        private const int NeuronSegment = 1, NetworkSegment = 2, RewindSegment = 4, ValleySegment = 7;

        private readonly List<string> _errors = new List<string>();

        /// <summary>
        /// The MCP-for-Unity editor bridge logs "[WebSocket] Connection failed" as an error whenever its server is
        /// down, which has nothing to do with the ride. Ignore that one source; any other error still fails the test
        /// in TearDown. The runner gives SetUp, the test body and TearDown separate log scopes, so the body turns
        /// the runner's own check off in Load().
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _errors.Clear();
            Application.logMessageReceived += CollectError;
        }

        private void CollectError(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.Contains("MCP-FOR-UNITY")) return;
            _errors.Add(type + ": " + message);
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CollectError;
            Time.timeScale = 1f;
            var director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            if (director != null)
            {
                director.StopAllCoroutines();
                director.enabled = false;
            }

            if (_errors.Count > 0) Assert.Fail("Errors logged during the ride:\n" + string.Join("\n", _errors));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what, float realSeconds = 120f)
        {
            float end = Time.realtimeSinceStartup + realSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        private static RideDirector _director;

        private static IEnumerator Load()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;
            _director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            Assert.IsNotNull(_director, "RideDirector missing from NeuralRide");
        }

        /// <summary>The ride opens with the narrated intro and holds GO. Skip it the way an operator does (Space), then pull GO.</summary>
        private static IEnumerator Launch(float timeScale)
        {
            yield return Load();
            Assert.AreEqual(RideState.Intro, _director.State, "The ride must open with the narrated intro");
            _director.SkipNarration();
            yield return WaitFor(() => _director.State == RideState.Dock, "the dock after the intro", 20f);
            Time.timeScale = timeScale;
            _director.Controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => _director.State == RideState.Traveling, "launch from the dock", 20f);
        }

        private static T Moment<T>(string name) where T : RideMoment
        {
            foreach (T m in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            {
                if (m.name == name) return m;
            }

            Assert.Fail(name + " moment missing");
            return null;
        }

        private static Level1Data Data() => _director.GetComponent<Level1DataSource>().Data;

        [UnityTest]
        public IEnumerator Intro_ARiderTouchingGo_DoesNotLaunch_AndSkipOpensTheDock()
        {
            yield return Load();
            RideControlChannel go = _director.Controls.Get(RideControlIds.Action);
            Assert.AreEqual(RideState.Intro, _director.State);
            Assert.IsTrue(_director.IsNarrating, "The intro is narrated");

            go.SetFromUser(1f);
            yield return new WaitForSecondsRealtime(0.5f);
            go.SetFromUser(0f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(RideState.Intro, _director.State, "GO must not launch the ride during the intro");

            _director.SkipNarration();
            yield return WaitFor(() => _director.State == RideState.Dock, "the dock after skipping", 20f);
            go.SetFromUser(1f);
            yield return WaitFor(() => _director.State == RideState.Traveling && _director.SegmentIndex == 0, "launch from the dock", 20f);
        }

        [UnityTest]
        public IEnumerator Intro_HoldingGoForTwoSeconds_SkipsIt()
        {
            yield return Load();
            Assert.AreEqual(RideState.Intro, _director.State);
            _director.Controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => _director.State == RideState.Dock, "the dock after holding GO", 8f);
        }

        [UnityTest]
        public IEnumerator Ride_RunsEndToEnd_WithNoInputButGo()
        {
            // Nobody touches anything after GO: every moment must reach its end by its own timeout.
            yield return Launch(25f);
            var started = new List<int>();
            bool completed = false;
            _director.SegmentStarted += i => started.Add(i);
            _director.RideCompleted += () => completed = true;
            var paint = Moment<WeightPaintMoment>("WeightPaint");
            var blame = Moment<BlameMoment>("Blame");
            var valley = Moment<ValleyRunMoment>("ValleyRun");
            yield return WaitFor(() => _director.State == RideState.Complete, "the end of the ride", 240f);

            Assert.IsTrue(completed, "RideCompleted must fire");
            var expected = new List<int>();
            for (int i = 1; i < _director.SegmentCount; i++) expected.Add(i);
            CollectionAssert.AreEqual(expected, started, "Every segment runs once, in order");
            Assert.IsTrue(paint.Result.Passed, "The puzzle ends solved (Core's verdict), here by following its own hints");
            Assert.IsFalse(paint.SolvedByRider);
            for (int k = 0; k < blame.TopCount; k++) Assert.IsTrue(blame.IsNudged(k), "Blame wire " + k + " was never nudged");
            Assert.Less(blame.HeroLoss, blame.StartLoss, "Nudging the blamed wires lowers the hero's real loss");
            Assert.AreEqual(valley.HopCount - 1, valley.CurrentHop, "The Valley run reaches the last hop");
            Assert.AreEqual(1f, _director.Progress01, 1e-4);
        }

        [UnityTest]
        public IEnumerator Puzzle_GatesTheNeuronStop_UntilCoreSaysEveryDigitIsRight()
        {
            yield return Launch(8f);
            var paint = Moment<WeightPaintMoment>("WeightPaint");
            yield return WaitFor(() => paint.IsLive, "the puzzle to open", 60f);
            Assert.AreEqual(NeuronSegment, _director.SegmentIndex);
            Assert.IsFalse(paint.Result.Passed, "It must not open solved");

            // Painting everything + is not a solution: the zeros fire too. The ride waits.
            for (int c = 0; c < 49; c++) paint.Cycle(c);
            Assert.IsFalse(paint.Result.Passed, "All plus must not pass");
            yield return new WaitForSeconds(4f);
            Assert.AreEqual(NeuronSegment, _director.SegmentIndex, "The ride must wait at the neuron while the puzzle is unsolved");
            Assert.AreEqual(RideState.AtStop, _director.State);

            // Back to empty (+ -> - -> 0), then the baked reference solution, tap by tap.
            for (int c = 0; c < 49; c++)
            {
                paint.Cycle(c);
                paint.Cycle(c);
            }

            int[] reference = Data().File.puzzle.referenceSolution;
            for (int c = 0; c < 49; c++)
            {
                if (reference[c] == 1) paint.Cycle(c);
                else if (reference[c] == -1)
                {
                    paint.Cycle(c);
                    paint.Cycle(c);
                }
            }

            Assert.IsTrue(paint.Result.Passed, "The reference weights solve it");
            yield return WaitFor(() => _director.SegmentIndex == NetworkSegment, "the ride to move on after the solve", 60f);
            Assert.IsTrue(paint.SolvedByRider);
        }

        [UnityTest]
        public IEnumerator FollowingTheHints_SolvesThePuzzle_WithoutKnowingTheAnswer()
        {
            yield return Launch(8f);
            var paint = Moment<WeightPaintMoment>("WeightPaint");
            yield return WaitFor(() => paint.IsLive, "the puzzle to open", 60f);
            int moves = 0;
            while (!paint.Result.Passed)
            {
                yield return WaitFor(() => paint.HintCell >= 0 || paint.Result.Passed, "a hint after the rider goes quiet", 60f);
                if (paint.Result.Passed) break;
                int cell = paint.HintCell, target = paint.HintValue;
                while (paint.Weights[cell] != target) paint.Cycle(cell);
                Assert.LessOrEqual(++moves, 49, "The hints must converge");
            }

            Assert.IsTrue(paint.SolvedByRider, "Following the hints is the rider solving it");
        }

        [UnityTest]
        public IEnumerator ForwardPass_LightsWhatCoreComputes_AndTheUntrainedNetworkGuessesWrong()
        {
            yield return Launch(12f);
            var forward = Moment<ForwardPassMoment>("ForwardPassUntrained");
            yield return WaitFor(() => forward.HasFinished, "the untrained forward pass", 150f);
            Assert.AreEqual(Data().File.hero.untrainedGuess, forward.Guess);
            Assert.AreNotEqual(Level1Data.HeroDigit, forward.Guess);

            // The node mesh the rider sees carries Core's activations (colour g) for the 49 pixels, 16 hidden and 10 answers.
            Mesh nodes = GameObject.Find("Region_3").transform.Find("Network/Nodes").GetComponent<MeshFilter>().sharedMesh;
            Color[] colors = nodes.colors;
            double[] pixels = Data().File.hero.pixels7;
            for (int i = 0; i < 49; i++) Assert.AreEqual(pixels[i], colors[i * 4].g, 1e-5, "pixel " + i);
            for (int j = 0; j < 16; j++) Assert.AreEqual(forward.Hidden[j], colors[(49 + j) * 4].g, 1e-5, "hidden " + j);
            for (int k = 0; k < 10; k++) Assert.AreEqual(forward.Outputs[k], colors[(65 + k) * 4].g, 1e-5, "answer " + k);
        }

        [UnityTest]
        public IEnumerator Rewind_RollsBackward_WhileTheRiderKeepsFacingTheNetwork()
        {
            yield return Launch(25f);
            yield return WaitFor(() => _director.SegmentIndex == RewindSegment && _director.State == RideState.Traveling, "the rewind", 200f);
            float last = _director.Distance;
            int backward = 0;
            while (_director.State == RideState.Traveling && _director.SegmentIndex == RewindSegment)
            {
                yield return null;
                if (_director.Distance < last - 1e-4f) backward++;
                Assert.LessOrEqual(_director.Distance, last + 1e-4f, "The rewind never moves forward");
                Assert.Greater(Vector3.Dot(_director.Pod.forward, Vector3.forward), 0.99f, "The rider keeps facing the network");
                Assert.AreEqual(0f, _director.Pod.eulerAngles.x, 1e-3f, "No pitch");
                Assert.AreEqual(0f, _director.Pod.eulerAngles.z, 1e-3f, "No roll");
                last = _director.Distance;
            }

            Assert.Greater(backward, 5, "The pod rolled backward along the track");
        }

        [UnityTest]
        public IEnumerator Valley_FollowsTheBakedHops_AndGentleModeHalvesTheDrop()
        {
            yield return Launch(25f);
            var valley = Moment<ValleyRunMoment>("ValleyRun");
            Level1Hop[] hops = Data().File.valley.hops;
            var heights = new float[hops.Length];
            float rimY = float.NaN;
            valley.HopReached += k => heights[k] = _director.Pod.position.y;
            yield return WaitFor(() => _director.SegmentIndex == ValleySegment, "the Valley", 220f);
            rimY = _director.Pod.position.y;
            _director.GentleMode = true;
            yield return WaitFor(() => valley.HasFinished, "the drop", 120f);

            const float metersPerLoss = 5f;
            for (int k = 0; k < hops.Length; k++)
            {
                float expected = rimY + 0.5f * (float)(hops[k].loss - hops[0].loss) * metersPerLoss;
                Assert.AreEqual(expected, heights[k], 0.05f, "hop " + k + " (step " + hops[k].step + ") in gentle mode");
            }

            Assert.Greater(heights[1], heights[0], "The first, too-big step overshoots up the far slope");
            for (int k = 2; k < hops.Length; k++) Assert.Less(heights[k], heights[k - 1], "Hop " + k + " goes downhill");
        }

        [UnityTest]
        public IEnumerator Valley_AtFullHeight_DropsTheRealLossTimesFive()
        {
            yield return Launch(25f);
            var valley = Moment<ValleyRunMoment>("ValleyRun");
            Level1Hop[] hops = Data().File.valley.hops;
            float last = float.NaN;
            float rimY = float.NaN;
            yield return WaitFor(() => _director.SegmentIndex == ValleySegment, "the Valley", 220f);
            rimY = _director.Pod.position.y;
            yield return WaitFor(() => valley.HasFinished, "the drop", 120f);
            last = _director.Pod.position.y;
            Assert.AreEqual((float)(hops[hops.Length - 1].loss - hops[0].loss) * 5f, last - rimY, 0.05f);
        }

        [UnityTest]
        public IEnumerator Skip_HurriesAMoment_SoAnOperatorCanAlwaysMoveOn()
        {
            yield return Launch(8f);
            var paint = Moment<WeightPaintMoment>("WeightPaint");
            yield return WaitFor(() => paint.IsLive, "the puzzle to open", 60f);
            _director.SkipNarration(); // cuts "your turn" if it is still playing
            yield return null;
            _director.SkipNarration(); // hurries the puzzle: it follows its own hints
            yield return WaitFor(() => _director.SegmentIndex == NetworkSegment, "the ride to move on", 30f);
            Assert.IsTrue(paint.Result.Passed, "Hurrying still ends on a real solution");
        }
    }
}
