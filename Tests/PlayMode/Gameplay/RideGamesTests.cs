using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.XR.Ride;

namespace Convergence.Tests.PlayMode.Gameplay
{
    /// <summary>
    /// Level 1 v5 (TASK_LEVEL1_V5_GAMEPLAY): every stop is a mini-game the rider wins, then GO moves on. These play the
    /// NeuralRide scene the way a rider does, through each game's own inputs (a hand's press on a cell, a grab and a
    /// plug, a shot wire, a step button) and GO's channel, and never set a game to won. A game must end with a win the
    /// rider earned, and the ride must wait for GO after it.
    /// </summary>
    [TestFixture]
    public class RideGamesTests
    {
        private readonly List<string> _errors = new List<string>();
        private RideDirector _director;
        private readonly Dictionary<RideGame, (int stars, bool byRider)> _wins = new Dictionary<RideGame, (int, bool)>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _errors.Clear();
            _wins.Clear();
            Application.logMessageReceived += CollectError;
        }

        private void CollectError(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.Contains("MCP-FOR-UNITY")) return;
            string at = stackTrace != null ? stackTrace.Split('\n')[0] : "";
            _errors.Add(type + ": " + message + (at.Length > 0 ? " (at " + at.Trim() + ")" : ""));
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived -= CollectError;
            Time.timeScale = 1f;
            if (_director != null)
            {
                _director.StopAllCoroutines();
                _director.enabled = false;
            }

            if (_errors.Count > 0) Assert.Fail("Errors logged during the ride:\n" + string.Join("\n", _errors));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what, float realSeconds = 90f)
        {
            float end = Time.realtimeSinceStartup + realSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        private T Game<T>(string name) where T : RideGame
        {
            foreach (T m in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            {
                if (m.name != name) continue;
                m.Won += (stars, byRider) => _wins[m] = (stars, byRider);
                return m;
            }

            Assert.Fail(name + " game missing");
            return null;
        }

        private IEnumerator Launch(float timeScale)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;
            _director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            Assert.IsNotNull(_director, "RideDirector missing from NeuralRide");

            // The ride starts behind a black fade (RideBoot warms the scene up first); play begins when it lifts.
            var boot = UnityEngine.Object.FindAnyObjectByType<RideBoot>();
            if (boot != null) yield return WaitFor(() => !boot.IsBooting, "the boot fade to lift", 60f);
            yield return null;
            yield return null;
            _director.SkipNarration();
            yield return WaitFor(() => _director.State == RideState.Dock, "the dock after the intro", 20f);
            Time.timeScale = timeScale;
            _director.Controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => _director.State == RideState.Traveling, "launch from the dock", 20f);
        }

        /// <summary>After a win the ride waits with GO lit; the rider pulls it and the next segment starts.</summary>
        private IEnumerator WonThenPullGo(RideGame game)
        {
            yield return WaitFor(() => _director.WaitingForGo, game.name + "'s win to light GO");
            Assert.IsTrue(_wins.ContainsKey(game), game.name + " must raise Won");
            Assert.IsTrue(_wins[game].byRider, game.name + " was won by the rider, not by its give-up help");
            Assert.GreaterOrEqual(_wins[game].stars, 1);
            int segment = _director.SegmentIndex;
            RideControlChannel go = _director.Controls.Get(RideControlIds.Action);
            Assert.IsFalse(go.Locked, "GO must be live while the ride waits for it");
            go.SetFromUser(1f);
            yield return WaitFor(() => _director.SegmentIndex > segment, "the ride to move on after GO", 30f);
        }

        [UnityTest]
        public IEnumerator EveryGame_IsWonByTheRider_ThroughItsOwnInputs_AndGoMovesOn()
        {
            yield return Launch(4f);
            var draw = Game<DrawMoment>("Draw");
            var paint = Game<WeightPaintMoment>("WeightPaint");
            var plug = Game<PlugMoment>("Plug");
            var blame = Game<BlameMoment>("Blame");
            var descent = Game<DescentMoment>("Descent");
            var grid = UnityEngine.Object.FindAnyObjectByType<DashGridInput>();
            Assert.IsNotNull(grid, "The dash grid's router is missing");

            // Draw the 3: the right hand inks, the left rubs out; a scribble does not pass.
            yield return WaitFor(() => draw.IsLive, "the draw game to open");
            int stray = -1;
            for (int c = 0; c < 49 && stray < 0; c++) if (!draw.Puzzle.IsTarget(c)) stray = c;
            Assert.IsTrue(grid.Press(stray, RideHand.Right));
            Assert.IsTrue(draw.Puzzle.IsInked(stray));
            Assert.IsTrue(grid.Press(stray, RideHand.Left), "the left hand rubs out");
            Assert.IsFalse(draw.Puzzle.IsInked(stray));
            for (int c = 0; c < 49; c++) if (draw.Puzzle.IsTarget(c)) grid.Press(c, RideHand.Right);
            Assert.IsTrue(draw.Puzzle.Passed);
            yield return WonThenPullGo(draw);

            // On the way to the neuron, noise drifts past: a hit pops it and scores.
            ZapSpeck speck = null;
            yield return WaitFor(() =>
            {
                foreach (ZapSpeck s in UnityEngine.Object.FindObjectsByType<ZapSpeck>(FindObjectsSortMode.None)) if (s.gameObject.activeInHierarchy) speck = s;
                return speck != null;
            }, "a noise speck while travelling", 30f);
            int score = _director.Score;
            Assert.IsTrue(speck.Hit(RideHand.Right));
            Assert.IsFalse(speck.gameObject.activeSelf, "a zapped speck pops");
            Assert.AreEqual(score + 10, _director.Score);

            // Teach one neuron: the baked reference, right hand green, left hand red.
            yield return WaitFor(() => paint.IsLive, "the paint game to open");
            int[] reference = _director.GetComponent<Level1DataSource>().Data.File.puzzle.referenceSolution;

            // A real fingertip reaching a cell presses it with its own hand: the right hand paints green (+), the left red (-).
            yield return new WaitForSecondsRealtime(1.2f); // the table finishes rising
            var tips = new Dictionary<int, HandPolarityController>();
            foreach (HandPolarityController tip in UnityEngine.Object.FindObjectsByType<HandPolarityController>(FindObjectsInactive.Include))
            {
                tips[tip.Hand] = tip;
            }

            Assert.AreEqual(2, tips.Count, "Each controller wears a HandPolarityController (left and right)");
            // With no headset the rig leaves its controller objects switched off; a headset turns them on. Do what it does.
            foreach (HandPolarityController tip in tips.Values) tip.gameObject.SetActive(true);
            yield return null;
            int plus = Array.IndexOf(reference, 1), minus = Array.IndexOf(reference, -1);
            foreach ((int hand, int cell, int expected) in new[] { (RideHand.Right, plus, 1), (RideHand.Left, minus, -1) })
            {
                WeightCellInteractable target = null;
                foreach (WeightCellInteractable c in UnityEngine.Object.FindObjectsByType<WeightCellInteractable>(FindObjectsInactive.Include))
                {
                    if (c.Cell == cell) target = c;
                }

                Assert.IsNotNull(target, "cell " + cell + " has a collider");
                Transform controller = tips[hand].transform;
                Vector3 delta = target.GetComponent<Collider>().bounds.center - tips[hand].Position;
                controller.position += delta;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.AreEqual(expected, paint.Weights[cell], (hand == RideHand.Right ? "right" : "left") + " fingertip touching cell " + cell + " paints it " + (expected > 0 ? "green (+)" : "red (-)"));
                int painted = 0;
                for (int c = 0; c < 49; c++) if (paint.Weights[c] != 0) painted++;
                Assert.AreEqual(hand == RideHand.Right ? 1 : 2, painted, "a fingertip presses only the cell under it");
                controller.position -= delta; // back where it was
            }

            // Then the rest of the reference, by the cells' own presses: the colour that paints a weight (or rubs it out).
            for (int c = 0; c < 49; c++)
            {
                int have = paint.Weights[c], want = reference[c];
                if (have == want) continue;
                grid.Press(c, (want != 0 ? want : have) > 0 ? RideHand.Right : RideHand.Left);
            }

            Assert.IsTrue(paint.Result.Passed, "the reference weights, painted with two hands, solve it");
            yield return WonThenPullGo(paint);

            // Plug the strings: a wrong socket makes things worse; strings into the 3 make Pixel say 3.
            yield return WaitFor(() => plug.IsLive, "the plug game to open", 150f);
            Assert.AreNotEqual(Level1Data.HeroDigit, plug.Puzzle.Guess, "the untrained network starts wrong");
            double gap = plug.Puzzle.Gap;
            Assert.IsTrue(plug.GrabString(0, RideHand.Right));
            Assert.IsTrue(plug.HitSocket(plug.Puzzle.Guess, RideHand.Right, Vector3.zero));
            Assert.Greater(plug.Puzzle.Gap, gap, "plugging into the wrong answer makes it stronger");
            int moves = 0;
            while (!plug.Puzzle.Passed)
            {
                int s = plug.Puzzle.Hint();
                Assert.IsTrue(plug.GrabString(s, RideHand.Left));
                Assert.IsTrue(plug.HitSocket(Level1Data.HeroDigit, RideHand.Left, Vector3.zero));
                Assert.LessOrEqual(++moves, plug.Puzzle.StringCount);
            }

            yield return WonThenPullGo(plug);

            // The hot-wire hunt: grab a hot wire with a laser, drag its loose end to the 3, and it snaps on. (The desk seat and
            // the hand lasers are switched off here so they do not overwrite the aim this test sets.)
            yield return WaitFor(() => blame.IsLive, "the hunt to open", 150f);
            UnityEngine.Object.FindAnyObjectByType<DesktopSeat>().enabled = false;
            UnityEngine.Object.FindAnyObjectByType<RideHandLasers>().enabled = false;
            double before = blame.HeroLoss;
            Vector3 eye = Camera.main.transform.position;
            var connected = new List<(int wire, int hand, int order)>();
            blame.Connected += (wire, hand, order) => connected.Add((wire, hand, order));

            Assert.IsFalse(blame.Grab(blame.TopCount + 3, RideHand.Right), "there is no such wire");
            Assert.IsTrue(blame.Grab(0, RideHand.Right));
            Assert.AreEqual(0, blame.Carrying(RideHand.Right));
            RideAim.Set(RideHand.Right, eye, blame.Socket.position + Vector3.up * 25f, true); // pointing well away from the 3
            yield return null;
            Assert.IsFalse(blame.IsNudged(0), "a wire far from the 3 does not snap");
            Assert.AreEqual(0, blame.Carrying(RideHand.Right), "and the hand still carries it");
            Assert.IsTrue(blame.TryGetCursor(RideHand.Right, out Vector3 cursor));
            Assert.Greater(Vector3.Distance(cursor, blame.Socket.position), blame.SnapRadiusFrom(eye), "the wire's end follows the laser");
            RideAim.RaiseReleased(RideHand.Right); // let go over nothing
            Assert.AreEqual(-1, blame.Carrying(RideHand.Right), "letting go drops the wire back");
            Assert.IsFalse(blame.IsNudged(0));

            Assert.IsTrue(blame.Grab(0, RideHand.Left));
            RideAim.Set(RideHand.Left, eye, blame.Socket.position, true); // pointing straight at the 3
            yield return null;
            Assert.IsTrue(blame.IsNudged(0), "a wire dragged to the 3 snaps on, as one real gradient step");
            Assert.AreEqual(-1, blame.Carrying(RideHand.Left), "a snapped wire is let go");
            CollectionAssert.AreEqual(new[] { (0, RideHand.Left, 0) }, connected);
            Assert.AreEqual(1, blame.ConnectedCount);
            Assert.IsFalse(blame.Grab(0, RideHand.Right), "a wire that is on the 3 cannot be grabbed again");

            RideAim.Set(RideHand.Left, Vector3.zero, Vector3.zero, false);
            RideAim.Set(RideHand.Right, Vector3.zero, Vector3.zero, false);
            for (int k = 0; k < blame.TopCount; k++) blame.Nudge(k);
            Assert.Less(blame.HeroLoss, before, "the real gradient steps lower the loss");
            yield return WonThenPullGo(blame);

            // Roll it down: a giant leap flies past the bottom, medium steps settle.
            yield return WaitFor(() => descent.IsLive, "the descent game to open", 150f);
            double start = descent.Game.X;
            Assert.IsTrue(descent.Press(2, Vector3.zero));
            Assert.Greater(descent.Game.X, descent.Game.Bottom, "the giant leap overshoots");
            Assert.Less(start, descent.Game.Bottom);
            while (!descent.Game.Passed)
            {
                yield return WaitFor(() => descent.Press(1, Vector3.zero), "the ball to stop rolling", 10f);
                Assert.Less(descent.Game.Steps, 20);
            }

            yield return WonThenPullGo(descent);
            Assert.Greater(_director.Score, 0, "the ride keeps score");
            yield return WaitFor(() => _director.State == RideState.Complete, "the end of the ride", 240f);
        }
    }
}
