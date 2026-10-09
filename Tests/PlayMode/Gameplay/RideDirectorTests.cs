using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Convergence.Gameplay.Ride;

namespace Convergence.Tests.PlayMode.Gameplay
{
    /// <summary>
    /// Plays the NeuralRide scene headless. The test moves levers only through the same channel call a hand or the
    /// mouse makes, and uses the catalog's reference weights. It never sets a station to solved.
    /// </summary>
    [TestFixture]
    public class RideDirectorTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            if (director != null)
            {
                director.StopAllCoroutines();
                director.enabled = false;
            }
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

        /// <summary>The ride opens with the narrated intro and holds GO. Skip it the way an operator does (Space).</summary>
        private static IEnumerator SkipIntro(RideDirector director)
        {
            Assert.AreEqual(RideState.Intro, director.State, "The ride must open with the narrated intro");
            director.SkipNarration();
            yield return WaitFor(() => director.State == RideState.Dock, "the dock after the intro", 20f);
        }

        private static StationController Station(int number)
        {
            foreach (StationController s in UnityEngine.Object.FindObjectsByType<StationController>(FindObjectsInactive.Include))
            {
                if (s.name == "Station_" + number) return s;
            }

            Assert.Fail("Station_" + number + " not found");
            return null;
        }

        [UnityTest]
        public IEnumerator Ride_ReachesAndSolvesEveryStop_WithReferenceWeights()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            Assert.IsNotNull(director, "RideDirector missing from NeuralRide");
            RideControlSet controls = director.Controls;
            yield return SkipIntro(director);
            Time.timeScale = 10f;

            // Dock: pull the launch lever.
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 0, "stop 1");

            // Stop 1: a trigger of 2.0 lets the rock through, so it must not be solved yet.
            StationController one = Station(1);
            Assert.IsFalse(one.IsSolved, "Stop 1 must not start solved");
            controls.Get(RideControlIds.Trigger).SetFromUser(-0.5f);
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(one.IsSolved, "A trigger below zero zaps the drone and must not solve");
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => one.IsSolved, "stop 1 to solve");

            // Stop 2: catalog reference weights 1, 1, bias -0.5 (trigger 0.5).
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 1, "stop 2");
            StationController two = Station(2);
            Assert.IsFalse(two.IsSolved, "Stop 2 must not start solved");
            controls.Get(RideControlIds.Rock).SetFromUser(1f);
            controls.Get(RideControlIds.Ice).SetFromUser(1f);
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => two.IsSolved, "stop 2 to solve");

            // Stop 3: pull LEARN with the learning rate on "good".
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 2, "stop 3");
            StationController three = Station(3);
            Assert.IsFalse(three.IsSolved, "Stop 3 must not start solved");
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => three.IsSolved, "stop 3 to converge");
            Assert.AreEqual(2.0 / 3.0, controls.Get(RideControlIds.Rock).Value, 0.05);
            Assert.AreEqual(2.0 / 3.0, controls.Get(RideControlIds.Ice).Value, 0.05);

            yield return WaitFor(() => director.State == RideState.Complete, "ride to complete");
        }

        [UnityTest]
        public IEnumerator FollowingTheArrows_SolvesStops1And2_WithoutKnowingTheAnswer()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            RideControlSet controls = director.Controls;
            string hintId = null;
            int hintDir = 0;
            director.HintChanged += (id, dir) => { hintId = id; hintDir = dir; };
            yield return SkipIntro(director);
            Time.timeScale = 10f;

            // The dock arrow points at GO.
            yield return null;
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 0, "stop 1");

            for (int stop = 0; stop < 2; stop++)
            {
                if (stop == 1) yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 1, "stop 2");
                StationController station = Station(stop + 1);
                if (stop == 1)
                {
                    // The situation that got a player stuck: every lever parked on zero.
                    controls.Get(RideControlIds.Rock).SetFromUser(0f);
                    controls.Get(RideControlIds.Ice).SetFromUser(0f);
                    controls.Get(RideControlIds.Trigger).SetFromUser(0f);
                    yield return null;
                    Assert.IsNotNull(hintId, "With every lever on zero the arrow must still point somewhere");
                }

                int moves = 0;
                while (!station.IsSolved && moves < 30)
                {
                    yield return null;
                    if (hintId == null) { yield return new WaitForSeconds(0.2f); continue; }
                    RideControlChannel ch = controls.Get(hintId);
                    ch.SetFromUser(ch.Value + hintDir * ch.Step);
                    moves++;
                }

                Assert.Less(moves, 30, "Following the arrows should solve stop " + (stop + 1));
                yield return WaitFor(() => station.IsSolved, "stop " + (stop + 1) + " to hold its solve");
            }
        }

        [UnityTest]
        public IEnumerator Narration_EveryLineIsSpokenInItsOwnSection()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            RideControlSet controls = director.Controls;
            var heard = new System.Collections.Generic.List<(RideLine line, RideState state, int stop)>();
            director.Said += line => heard.Add((line, director.State, director.StopIndex));
            director.Narrated += line => heard.Add((line, director.State, director.StopIndex));
            yield return SkipIntro(director);
            Time.timeScale = 10f;

            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 0, "stop 1");
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => Station(1).IsSolved, "stop 1 to solve");
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 1, "stop 2");
            controls.Get(RideControlIds.Rock).SetFromUser(1f);
            controls.Get(RideControlIds.Ice).SetFromUser(1f);
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => Station(2).IsSolved, "stop 2 to solve");
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 2, "stop 3");
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => Station(3).IsSolved, "stop 3 to solve");
            yield return WaitFor(() => director.State == RideState.Complete, "the finish");

            Assert.IsTrue(heard.Exists(h => h.line == RideLine.CourseLocked), "The closing line must play");
            Assert.IsFalse(heard.Exists(h => h.line == RideLine.Converged), "The second stop-3 closing line was dropped to keep it concise");
            foreach (var h in heard)
            {
                switch (h.line)
                {
                    case RideLine.Launch:
                        Assert.AreEqual(RideState.Dock, h.state, "Launch line outside the dock"); break;
                    case RideLine.NeuronIntro: case RideLine.LowerTrigger: case RideLine.DroneOops: case RideLine.WeightTimesInput:
                        Assert.AreEqual(0, h.stop, h.line + " must be heard at stop 1"); Assert.AreEqual(RideState.AtStop, h.state); break;
                    case RideLine.TwoSensors: case RideLine.TurnPipes: case RideLine.OrBuilt:
                        Assert.AreEqual(1, h.stop, h.line + " must be heard at stop 2"); Assert.AreEqual(RideState.AtStop, h.state); break;
                    case RideLine.PullLearn: case RideLine.HillIsError: case RideLine.GradientDescent: case RideLine.LrTooHigh:
                        Assert.AreEqual(2, h.stop, h.line + " must be heard at stop 3"); Assert.AreEqual(RideState.AtStop, h.state); break;
                    case RideLine.TargetingRestored:
                        Assert.AreEqual(RideState.Traveling, h.state, "The finish line plays while the pod rolls on"); break;
                }
            }
        }

        [UnityTest]
        public IEnumerator Stop3_CrazyLearningRate_Overshoots_AndDoesNotSolve()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            RideControlSet controls = director.Controls;
            yield return SkipIntro(director);
            Time.timeScale = 10f;
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 0, "stop 1");
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => Station(1).IsSolved, "stop 1 to solve");
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 1, "stop 2");
            controls.Get(RideControlIds.Rock).SetFromUser(1f);
            controls.Get(RideControlIds.Ice).SetFromUser(1f);
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => Station(2).IsSolved, "stop 2 to solve");
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 2, "stop 3");

            StationController three = Station(3);
            bool overshot = false;
            three.PhaseChanged += (kind, phase) => { if (phase == StationPhase.Overshoot) overshot = true; };
            controls.Get(RideControlIds.Eta).SetFromUser(2f);
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => overshot, "the overshoot");
            Assert.IsFalse(three.IsSolved, "A learning rate that is too high must not solve the stop");
        }

        [UnityTest]
        public IEnumerator Intro_ARiderTouchingGo_DoesNotLaunch_AndSkipOpensTheDock()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            RideControlChannel go = director.Controls.Get(RideControlIds.Action);
            Assert.AreEqual(RideState.Intro, director.State);
            Assert.IsTrue(director.IsNarrating, "The intro is narrated");

            // A short touch of GO is not a launch and not a skip.
            go.SetFromUser(1f);
            yield return new WaitForSecondsRealtime(0.5f);
            go.SetFromUser(0f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(RideState.Intro, director.State, "GO must not launch the ride during the intro");

            director.SkipNarration();
            yield return WaitFor(() => director.State == RideState.Dock, "the dock after skipping", 20f);
            Assert.IsFalse(director.IsNarrating);

            go.SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.Traveling, "launch from the dock", 20f);
        }

        [UnityTest]
        public IEnumerator Intro_HoldingGoForTwoSeconds_SkipsIt()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            Assert.AreEqual(RideState.Intro, director.State);
            director.Controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.Dock, "the dock after holding GO", 8f);
        }

        [UnityTest]
        public IEnumerator Intro_PullingGoToWakeAura_WakesHer_AndHoldingOnDoesNotSkip()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            RideControlChannel go = director.Controls.Get(RideControlIds.Action);
            string asked = null;
            bool? wokenByRider = null;
            director.RiderTaskStarted += (id, atLeast) => { if (asked == null) asked = id; };
            director.RiderTaskDone += (id, byRider) => { if (id == RideControlIds.Action) wokenByRider = byRider; };

            Time.timeScale = 3f; // the beats run on game time; the hold-to-skip timer runs on real time
            yield return WaitFor(() => asked != null, "the first hands-on beat", 30f);
            Assert.AreEqual(RideControlIds.Action, asked, "The first thing the rider does is pull GO to wake AURA");

            go.SetFromUser(1f);
            yield return WaitFor(() => wokenByRider.HasValue, "AURA to wake", 5f);
            Assert.IsTrue(wokenByRider.Value, "The rider's pull should be what woke her");

            // A first-timer keeps holding the lever. That must not read as the operator's two-second skip.
            yield return new WaitForSecondsRealtime(2.6f);
            Assert.AreEqual(RideState.Intro, director.State, "Holding GO after the wake pull must not skip the intro");
            go.SetFromUser(0f);

            // After a release the operator's hold-to-skip works again.
            yield return null;
            go.SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.Dock, "the dock after a fresh two-second hold", 8f);
        }

        [UnityTest]
        public IEnumerator Intro_MakeItFire_WaitsForTheWeightToReachTheLine()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            RideControlChannel go = director.Controls.Get(RideControlIds.Action);
            RideControlChannel rock = director.Controls.Get(RideControlIds.Rock);
            float fireAt = -1f;
            bool? firedByRider = null;
            director.RiderTaskStarted += (id, atLeast) =>
            {
                if (id == RideControlIds.Action) go.SetFromUser(1f); // wake AURA at once
                if (id == RideControlIds.Rock) fireAt = atLeast;
            };
            director.RiderTaskDone += (id, byRider) =>
            {
                if (id == RideControlIds.Action) go.SetFromUser(0f);
                if (id == RideControlIds.Rock) firedByRider = byRider;
            };

            Time.timeScale = 3f;
            yield return WaitFor(() => fireAt > 0f, "the make-it-fire beat", 40f);
            Assert.IsFalse(rock.Locked, "The ROCK lever must be live for the make-it-fire beat");
            Assert.Less(rock.Value, fireAt, "The neuron must start below its line, or there is nothing to do");

            rock.SetFromUser(fireAt - rock.Step); // one detent short: not yet
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(firedByRider.HasValue, "The beat must wait until the weight reaches the line");

            rock.SetFromUser(fireAt);
            yield return WaitFor(() => firedByRider.HasValue, "the neuron to fire", 5f);
            Assert.IsTrue(firedByRider.Value, "The rider's weight should be what fired it");
            yield return WaitFor(() => director.State == RideState.Dock, "the dock after the intro", 60f);
            Assert.IsFalse(rock.Locked, "At the dock the ROCK weight is the rider's toy again");
        }

        [UnityTest]
        public IEnumerator Intro_WithNobodyTouchingAnything_TimesOutAndStillReachesTheDock()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            var done = new System.Collections.Generic.List<(string id, bool byRider)>();
            director.RiderTaskDone += (id, byRider) => done.Add((id, byRider));

            Time.timeScale = 8f;
            yield return WaitFor(() => director.State == RideState.Dock, "the dock with no input at all", 60f);
            Assert.AreEqual(2, done.Count, "Both hands-on beats should end by timing out");
            foreach (var d in done) Assert.IsFalse(d.byRider, d.id + " was marked as done by a rider who never touched it");
            Assert.AreEqual(RideState.Dock, director.State);
        }

        [UnityTest]
        public IEnumerator Briefing_PlaysDuringTravel_AndEndsBeforeTheStationGoesLive()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            yield return SkipIntro(director);
            Time.timeScale = 10f;

            int briefingStarted = -1;
            int briefingEnded = -1;
            bool narratingAtArrival = true;
            bool beganWhileNarrating = false;
            director.BriefingStarted += i => briefingStarted = i;
            director.BriefingEnded += i => briefingEnded = i;
            director.StopReached += i => narratingAtArrival = director.IsNarrating;
            Station(1).Began += kind => beganWhileNarrating |= director.IsNarrating;

            director.Controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return null;
            Assert.AreEqual(0, briefingStarted, "The first briefing starts with the first travel");
            Assert.IsTrue(director.IsNarrating);

            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 0, "stop 1");
            Assert.AreEqual(0, briefingEnded, "The briefing must be over when the pod arrives");
            Assert.IsFalse(narratingAtArrival, "No narration may still be running when the stop is reached");
            Assert.IsFalse(beganWhileNarrating, "The station must not go live before its briefing ends");
        }

        [UnityTest]
        public IEnumerator Stop_LetsTheLineSaidAtTheSolveFinish_BeforeMovingOn()
        {
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;

            var director = UnityEngine.Object.FindFirstObjectByType<RideDirector>();
            Assert.IsNotNull(director.Script, "The built ride runs on a RideScript");
            float lineSeconds = director.Script.SecondsOf(RideLine.WeightTimesInput);
            Assert.Greater(lineSeconds, 3f, "The stop 1 punchline is longer than the old fixed 3 s wait, so this test means something");
            yield return SkipIntro(director);
            Time.timeScale = 10f;

            float saidAt = -1f;
            float leftAt = -1f;
            director.Said += line => { if (line == RideLine.WeightTimesInput) saidAt = Time.time; };
            director.StateChanged += state => { if (state == RideState.Traveling && saidAt >= 0f && leftAt < 0f) leftAt = Time.time; };

            RideControlSet controls = director.Controls;
            controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => director.State == RideState.AtStop && director.StopIndex == 0, "stop 1");
            controls.Get(RideControlIds.Trigger).SetFromUser(0.5f);
            yield return WaitFor(() => Station(1).IsSolved, "stop 1 to solve");
            Assert.GreaterOrEqual(saidAt, 0f, "Solving stop 1 says its punchline");
            yield return WaitFor(() => leftAt >= 0f, "the pod to leave stop 1");

            Assert.GreaterOrEqual(leftAt - saidAt, lineSeconds - 0.05f,
                "The pod left stop 1 before the punchline finished, so the next briefing would cut it off");
        }
    }
}
