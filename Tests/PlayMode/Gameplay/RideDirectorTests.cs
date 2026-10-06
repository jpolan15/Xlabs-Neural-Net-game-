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
            Assert.AreEqual(RideState.Dock, director.State);
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
    }
}
