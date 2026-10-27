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
    /// Level 1 v6 (TASK_LEVEL1_V6_TRAIN): every stop is a mini-game the rider wins, then GO moves on, and one thread runs
    /// through them (the rider's own 3 and the network they train). These play the NeuralRide scene the way a rider does,
    /// through each game's own inputs (strokes on the canvas, presses and sweeps on cells, buttons, a dragged wire) and
    /// GO's channel, check that a laser from the eye reaches each control first, and never set a game to won. A game must end with a win the
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

        /// <summary>A ray from the eye to a control must reach that control first: nothing on the table may block the laser.</summary>
        private static void AssertLaserReaches(Component control, string what)
        {
            Vector3 eye = Camera.main.transform.position;
            Vector3 at = control.GetComponent<Collider>().bounds.center;
            Assert.IsTrue(Physics.Raycast(eye, (at - eye).normalized, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Collide),
                "a laser from the eye reaches nothing toward " + what);
            Assert.IsTrue(hit.collider.transform.IsChildOf(control.transform) || hit.collider.transform == control.transform,
                "a laser aimed at " + what + " hits " + hit.collider.name + " first");
        }

        private static T Find<T>(string name) where T : Component
        {
            foreach (T c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            {
                if (c.name == name) return c;
            }

            Assert.Fail(name + " (" + typeof(T).Name + ") missing");
            return null;
        }

        /// <summary>Draws strokes (canvas units) on the canvas the way a held laser does: begin, move point by point, end.</summary>
        private static void Draw(CanvasInput canvas, int hand, params Vector2[][] strokes)
        {
            foreach (Vector2[] s in strokes)
            {
                Assert.IsTrue(canvas.StrokeBegin(hand, canvas.ToWorld(s[0]), true), "the canvas takes a stroke");
                for (int i = 1; i < s.Length; i++)
                {
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(s[i - 1], s[i]) / 0.02f));
                    for (int k = 1; k <= n; k++) canvas.StrokeMove(hand, canvas.ToWorld(Vector2.Lerp(s[i - 1], s[i], k / (float)n)));
                }

                canvas.StrokeEnd(hand);
            }
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

        // Digits as a rider might draw them (canvas units, y down).
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

        [UnityTest]
        public IEnumerator EveryGame_IsWonByTheRider_ThroughItsOwnInputs_AndGoMovesOn()
        {
            yield return Launch(4f);
            var draw = Game<DrawMoment>("Draw");
            var paint = Game<WeightPaintMoment>("WeightPaint");
            var blame = Game<BlameMoment>("Blame");
            var train = Game<TrainMoment>("Train");
            var stump = Game<StumpMoment>("Stump");
            ForwardPassMoment send = Find<ForwardPassMoment>("ForwardPassUntrained");
            ForwardPassMoment read = Find<ForwardPassMoment>("ForwardPassTrained");
            var session = UnityEngine.Object.FindAnyObjectByType<Level1Session>();
            Assert.IsNotNull(session, "The rider's run (Level1Session) is missing");
            var grid = UnityEngine.Object.FindAnyObjectByType<DashGridInput>();
            Assert.IsNotNull(grid, "The weight grid's router is missing");
            var canvas = UnityEngine.Object.FindAnyObjectByType<CanvasInput>(FindObjectsInactive.Include);
            Assert.IsNotNull(canvas, "The drawing canvas is missing");
            GameButton done = Find<GameButton>("Done"), clear = Find<GameButton>("Clear");

            // Draw your 3: a held laser draws on the canvas; CLEAR wipes it; DONE keeps it as the rider's digit.
            yield return WaitFor(() => draw.IsLive, "the draw game to open");
            yield return new WaitForSecondsRealtime(1.2f); // the table finishes rising
            AssertLaserReaches(canvas, "the canvas");
            AssertLaserReaches(done, "DONE");
            Assert.IsFalse(done.Hit(RideHand.Right), "DONE does nothing on an empty canvas");
            Draw(canvas, RideHand.Left, new[] { V(0.2f, 0.2f), V(0.8f, 0.8f) });
            Assert.AreEqual(1, draw.Canvas.StrokeCount, "either hand draws");
            Assert.IsTrue(clear.Hit(RideHand.Right));
            Assert.AreEqual(0, draw.Canvas.StrokeCount, "CLEAR wipes the canvas");
            Draw(canvas, RideHand.Right, Digits[3]);
            Assert.IsTrue(draw.Canvas.HasEnoughInk);
            Assert.IsTrue(done.Hit(RideHand.Right), "DONE keeps the drawing");
            yield return WonThenPullGo(draw);
            Assert.IsTrue(session.DigitIsRiders, "the rider's drawing is the digit the rest of the ride follows");
            CollectionAssert.AreEqual(draw.Canvas.ToPixels7(), session.Digit);

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

            // Teach one neuron: fingertips press a cell with their own hand; a held laser sweeps (sprays) without toggling.
            yield return WaitFor(() => paint.IsLive, "the paint game to open");
            int[] reference = _director.GetComponent<Level1DataSource>().Data.File.puzzle.referenceSolution;
            yield return new WaitForSecondsRealtime(1.2f);
            var tips = new Dictionary<int, HandPolarityController>();
            foreach (HandPolarityController tip in UnityEngine.Object.FindObjectsByType<HandPolarityController>(FindObjectsInactive.Include)) tips[tip.Hand] = tip;
            Assert.AreEqual(2, tips.Count, "Each controller wears a HandPolarityController (left and right)");
            foreach (HandPolarityController tip in tips.Values) tip.gameObject.SetActive(true);
            yield return null;
            var cells = new Dictionary<int, WeightCellInteractable>();
            foreach (WeightCellInteractable c in UnityEngine.Object.FindObjectsByType<WeightCellInteractable>(FindObjectsInactive.Include)) cells[c.Cell] = c;
            AssertLaserReaches(cells[24], "the middle weight cell");
            int plus = Array.IndexOf(reference, 1), minus = Array.IndexOf(reference, -1);
            foreach ((int hand, int cell, int expected) in new[] { (RideHand.Right, plus, 1), (RideHand.Left, minus, -1) })
            {
                Transform controller = tips[hand].transform;
                Vector3 delta = cells[cell].GetComponent<Collider>().bounds.center - tips[hand].Position;
                controller.position += delta;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.AreEqual(expected, paint.Weights[cell], (hand == RideHand.Right ? "right" : "left") + " fingertip touching cell " + cell + " paints it " + (expected > 0 ? "+" : "-"));
                controller.position -= delta;
            }

            // A sweep onto a cell already that colour keeps it (a press would clear it).
            Assert.IsFalse(((IRideStrokeTarget)cells[plus]).StrokeBegin(RideHand.Right, Vector3.zero, false));
            Assert.AreEqual(1, paint.Weights[plus], "sweeping over a painted cell keeps its colour");
            for (int c = 0; c < 49; c++)
            {
                int want = reference[c];
                if (paint.Weights[c] == want) continue;
                if (want != 0) ((IRideStrokeTarget)cells[c]).StrokeBegin(want > 0 ? RideHand.Right : RideHand.Left, Vector3.zero, false);
                else grid.Press(c, paint.Weights[c] > 0 ? RideHand.Right : RideHand.Left);
            }

            Assert.IsTrue(paint.Result.Passed, "the reference weights, sprayed with two hands, solve it");
            yield return WonThenPullGo(paint);

            // Send your 3: shoot the card; the light races through the untrained network with the rider's own digit.
            yield return WaitFor(() => send.IsLive, "the send card", 150f);
            yield return new WaitForSecondsRealtime(1.2f);
            GameButton card = Find<GameButton>("Card");
            AssertLaserReaches(card, "the card with the rider's 3");
            bool thrown = false;
            send.Thrown += byRider => thrown = byRider;
            Assert.IsTrue(card.Hit(RideHand.Right));
            yield return WaitFor(() => thrown && send.Guess >= 0 && send.Progress >= 1f, "the race through the network", 60f);
            Assert.AreEqual(session.Read(out _, out _), send.Guess, "the guess is Core's, on the rider's digit");

            // The hot-wire hunt on the rider's digit: each wire dragged to the 3 is a real step on the rider's network.
            yield return WaitFor(() => blame.IsLive, "the hunt to open", 150f);
            UnityEngine.Object.FindAnyObjectByType<DesktopSeat>().enabled = false;
            UnityEngine.Object.FindAnyObjectByType<RideHandLasers>().enabled = false;
            double before = blame.HeroLoss;
            Vector3 eye = Camera.main.transform.position;
            BlameMoment.Wire top = blame.Wires[0];
            double weightBefore = top.Layer == 0 ? session.Network.W1[top.Neuron * 49 + top.Input] : session.Network.W2[top.Neuron * 16 + top.Input];
            Assert.IsTrue(blame.Grab(0, RideHand.Left));
            RideAim.Set(RideHand.Left, eye, blame.Socket.position, true);
            yield return null;
            Assert.IsTrue(blame.IsNudged(0), "a wire dragged to the 3 snaps on, as one real gradient step");
            double weightAfter = top.Layer == 0 ? session.Network.W1[top.Neuron * 49 + top.Input] : session.Network.W2[top.Neuron * 16 + top.Input];
            Assert.AreEqual(weightBefore - blame.LearningRate * top.Gradient, weightAfter, 1e-9, "the step is kept in the rider's network");
            RideAim.Set(RideHand.Left, Vector3.zero, Vector3.zero, false);
            for (int k = 0; k < blame.TopCount; k++) blame.Nudge(k);
            Assert.Less(blame.HeroLoss, before, "the real gradient steps lower the loss of the rider's 3");
            yield return WonThenPullGo(blame);
            UnityEngine.Object.FindAnyObjectByType<DesktopSeat>().enabled = true;
            UnityEngine.Object.FindAnyObjectByType<RideHandLasers>().enabled = true;

            // Train: the huge step blows the loss up (and is undone); holding the medium step trains it to the target.
            yield return WaitFor(() => train.IsLive, "the training game to open", 150f);
            yield return new WaitForSecondsRealtime(1.2f);
            GameButton small = Find<GameButton>("StepButton0"), medium = Find<GameButton>("StepButton1"), huge = Find<GameButton>("StepButton2");
            AssertLaserReaches(medium, "the medium step button");
            double startAccuracy = train.Accuracy;
            Assert.Less(startAccuracy, 0.3, "untrained, it guesses");
            bool blewUp = false;
            train.BlewUp += () => blewUp = true;
            Assert.IsTrue(huge.Hit(RideHand.Right));
            yield return WaitFor(() => blewUp, "the huge step to blow the loss up", 30f);
            Assert.AreEqual(-1, train.Held, "the undo lets go of the button");
            Assert.IsTrue(session.Network.IsFinite);
            yield return WaitFor(() => !train.Cooling, "the buttons to come back", 10f);
            Assert.IsTrue(small.Hit(RideHand.Left));
            yield return new WaitForSeconds(1f);
            RideAim.RaiseReleased(RideHand.Left);
            Assert.AreEqual(-1, train.Held, "letting go stops training");
            int steps = train.Steps;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(steps, train.Steps, "nothing trains while no button is held");
            Assert.IsTrue(medium.Hit(RideHand.Right));
            yield return WaitFor(() => !train.IsLive, "training to reach the target", 90f);
            RideAim.RaiseReleased(RideHand.Right);
            Assert.GreaterOrEqual(train.Accuracy, train.TargetAccuracy, "won on the accuracy Core measured on unseen digits");
            Assert.IsTrue(session.IsTrained);
            yield return WonThenPullGo(train);

            // Pixel reads the rider's 3 with the network the rider trained (studying it first if it has to).
            yield return WaitFor(() => read.HasFinished, "Pixel to read the rider's 3", 150f);
            Assert.AreEqual(3, session.Read(out _, out _), "the rider's network reads the rider's 3");

            // Stump Pixel: draw what it asks for and shoot DONE; it reads each one with the rider's network.
            yield return WaitFor(() => stump.IsLive, "the bonus round", 150f);
            int judged = 0;
            stump.Judged += (asked, guess, right) => judged++;
            for (int p = 0; p < stump.PromptCount; p++)
            {
                yield return WaitFor(() => stump.IsLive && stump.PromptIndex == p, "prompt " + p, 30f);
                Draw(canvas, RideHand.Right, Digits[stump.Prompt]);
                Assert.IsTrue(done.Hit(RideHand.Right), "DONE serves the bonus round too");
                yield return WaitFor(() => judged == p + 1, "Pixel's answer to prompt " + p, 20f);
            }

            yield return WonThenPullGo(stump);
            Assert.Greater(_director.Score, 0, "the ride keeps score");
            yield return WaitFor(() => _director.State == RideState.Complete, "the end of the ride", 240f);
        }
    }
}
