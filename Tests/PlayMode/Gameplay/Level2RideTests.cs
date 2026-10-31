using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride;

namespace Convergence.Tests.PlayMode.Gameplay
{
    /// <summary>
    /// Level 2, the Word Machine: the whole ride, played from the dock to the end the way a rider plays it, through each game's own
    /// pick calls and the same hand-aim data the lasers write (RideAim). Nothing here sets a game to won. Every game must be won by
    /// the rider (not by its give-up help), GO must wait for the rider after each win, the story the rider steered must reach the
    /// finale, and nothing may log an error.
    /// </summary>
    [TestFixture]
    public class Level2RideTests
    {
        private const string SceneName = "WordMachine";

        private readonly List<string> _errors = new List<string>();
        private readonly Dictionary<RideGame, (int stars, bool byRider)> _wins = new Dictionary<RideGame, (int, bool)>();
        private RideDirector _director;

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

        private static IEnumerator WaitFor(Func<bool> condition, string what, float realSeconds)
        {
            float end = Time.realtimeSinceStartup + realSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheWordMachine_IsPlayedFromTheDockToTheEnd_ByARider()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            _director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            Assert.IsNotNull(_director, "RideDirector missing from " + SceneName);

            var boot = UnityEngine.Object.FindAnyObjectByType<RideBoot>();
            if (boot != null) yield return WaitFor(() => !boot.IsBooting, "the boot fade to lift", 60f);
            yield return null;
            yield return null;

            var games = new List<RideGame>();
            foreach (RideGame g in UnityEngine.Object.FindObjectsByType<RideGame>(FindObjectsInactive.Include))
            {
                if (g.GetType().Namespace != typeof(SliceMoment).Namespace) continue;
                RideGame captured = g;
                g.Won += (stars, byRider) => _wins[captured] = (stars, byRider);
                games.Add(g);
            }

            Assert.AreEqual(5, games.Count, "the five games of Level 2");

            _director.SkipNarration();
            yield return WaitFor(() => _director.State == RideState.Dock, "the dock after the intro", 40f);
            Time.timeScale = 4f;
            _director.Controls.Get(RideControlIds.Action).SetFromUser(1f);
            yield return WaitFor(() => _director.State == RideState.Traveling || _director.State == RideState.AtStop, "launch from the dock", 20f);

            var rider = new TestRider();
            float end = Time.realtimeSinceStartup + 540f;
            int goPulls = 0;
            while (_director.State != RideState.Complete)
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Level 2 did not finish in time; it stopped in segment " + _director.SegmentIndex + " (" + _director.State + ")");
                if (_director.WaitingForGo)
                {
                    RideControlChannel go = _director.Controls.Get(RideControlIds.Action);
                    Assert.IsFalse(go.Locked, "GO must be live while the ride waits for it");
                    if (rider.CanPullGo())
                    {
                        go.SetFromUser(1f);
                        goPulls++;
                    }
                }
                else
                {
                    rider.Play(_director.ActiveMoment);
                }

                yield return null;
            }

            Assert.AreEqual(5, _wins.Count, "every game must raise Won");
            foreach (KeyValuePair<RideGame, (int stars, bool byRider)> win in _wins)
            {
                Assert.IsTrue(win.Value.byRider, win.Key.GetType().Name + " must be won by the rider, not by its give-up help");
                Assert.GreaterOrEqual(win.Value.stars, 1, win.Key.GetType().Name + " earns at least a star");
            }

            Assert.GreaterOrEqual(goPulls, 5, "GO waits for the rider after each game");
            Assert.Greater(_director.Score, 0);
            var session = UnityEngine.Object.FindAnyObjectByType<Level2Session>();
            Assert.IsNotNull(session, "the ride has a session that keeps the rider's story");
            StringAssert.StartsWith("My robot loves to", session.Story, "the story the rider steered reaches the finale");
            Assert.Greater(session.Story.Split(' ').Length, 5, "the rider's three words are in the story");
        }

        /// <summary>A rider for the games: it does what a person does (swipe, carry, hold, bet, shoot), at a human pace.</summary>
        private sealed class TestRider
        {
            private float _nextAction;
            private int _swipeSlot = -1;
            private float _swipeStart, _swipeSeconds = 0.38f;
            private int _carry = -1;
            private float _carryStart;
            private int _carryCluster;
            private float _goReadyAt = -1f;
            private const int Hand = RideHand.Right;

            /// <summary>GO is pulled a moment after it lights, like a person who has read the stamp.</summary>
            public bool CanPullGo()
            {
                if (_goReadyAt < 0f) _goReadyAt = Time.time + 1.2f;
                if (Time.time < _goReadyAt) return false;
                _goReadyAt = -1f;
                return true;
            }

            public void Play(RideMoment moment)
            {
                if (moment is SliceMoment slice) PlaySlice(slice);
                else if (moment is GalaxyMoment galaxy) PlayGalaxy(galaxy);
                else if (moment is SpotlightMoment spot) PlaySpot(spot);
                else if (moment is PredictMoment bet) PlayBet(bet);
                else if (moment is StoryMoment story) PlayStory(story);
            }

            private static void Aim(Transform stage, Vector3 stagePoint)
            {
                Vector3 origin = stage.TransformPoint(new Vector3(0.25f, 1.05f, 0.2f));
                Vector3 point = stage.TransformPoint(stagePoint);
                RideAim.Set(RideHand.Right, origin, point, true);
                RideAim.Set(RideHand.Left, origin, point, true);
            }

            private void PlaySlice(SliceMoment m)
            {
                if (!m.IsLive || m.Stage == null) return;
                if (_swipeSlot >= 0)
                {
                    SliceMoment.Slot s = m.SlotAt(_swipeSlot);
                    float t = (Time.time - _swipeStart) / _swipeSeconds;
                    float half = s.Width * 0.5f + 0.35f;
                    if (!s.Active || t > 1.05f)
                    {
                        m.PickStrokeEnd(_swipeSlot, Hand);
                        _swipeSlot = -1;
                        _nextAction = Time.time + 0.25f;
                        return;
                    }

                    Aim(m.Stage, new Vector3(s.Position.x + Mathf.Lerp(-half, half, Mathf.Clamp01(t)), s.Position.y, s.Position.z));
                    m.PickStrokeMove(_swipeSlot, Hand, Vector3.zero);
                    return;
                }

                if (Time.time < _nextAction) return;
                for (int i = 0; i < SliceMoment.SlotCount; i++)
                {
                    SliceMoment.Slot s = m.SlotAt(i);
                    if (!s.Active || Time.time - s.Born < 1.2f) continue;
                    if (s.Seams == 0 || s.CutCount >= s.Seams) continue;      // a whole word is left alone
                    Aim(m.Stage, new Vector3(s.Position.x - s.Width * 0.5f - 0.35f, s.Position.y, s.Position.z));
                    if (!m.PickStrokeBegin(i, Hand, Vector3.zero, true)) continue;
                    _swipeSlot = i;
                    _swipeStart = Time.time;
                    _swipeSeconds = s.Boss ? 0.9f : 0.38f;
                    return;
                }
            }

            private void PlayGalaxy(GalaxyMoment m)
            {
                if (!m.IsLive || m.Stage == null) return;
                if (_carry >= 0)
                {
                    GalaxyMoment.Item item = m.Items[_carry];
                    float t = (Time.time - _carryStart) / 0.9f;
                    Aim(m.Stage, Vector3.Lerp(item.Float, m.Clusters[_carryCluster].Centre, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))));
                    if (t >= 1.15f)
                    {
                        RideAim.RaiseReleased(Hand);
                        _carry = -1;
                        _nextAction = Time.time + 0.6f;
                    }

                    return;
                }

                if (Time.time < _nextAction) return;
                for (int i = 0; i < m.Items.Count; i++)
                {
                    GalaxyMoment.Item item = m.Items[i];
                    if (item.Seed || item.Placed || item.Carrier >= 0) continue;
                    Aim(m.Stage, item.Float);
                    if (!m.PickHit(i, Hand)) continue;
                    _carry = i;
                    _carryStart = Time.time;
                    for (int c = 0; c < m.Clusters.Count; c++) if (m.Clusters[c].Category == item.Category) _carryCluster = c;
                    return;
                }
            }

            private void PlaySpot(SpotlightMoment m)
            {
                if (m.Current == null) return;
                if (m.Stage != SpotlightMoment.Phase.Aim)
                {
                    foreach (int hand in new[] { RideHand.Left, RideHand.Right })
                    {
                        int focused = m.FocusOf(hand);
                        if (focused >= 0) m.PickStrokeEnd(focused, hand);
                    }

                    return;
                }

                if (Time.time < _nextAction) return;
                int[] answers = m.Current.answers;
                for (int k = 0; k < answers.Length && k < 2; k++)
                {
                    int hand = k == 0 ? RideHand.Right : RideHand.Left;
                    if (m.FocusOf(hand) != answers[k]) m.PickStrokeBegin(answers[k], hand, Vector3.zero, true);
                }

                _nextAction = Time.time + 0.2f;
            }

            private void PlayBet(PredictMoment m)
            {
                if (m.Stage != PredictMoment.Phase.Betting || m.Current == null || Time.time < _nextAction) return;
                PredictMoment.Offer offer = m.Current;
                int best = 0;
                float bestGap = float.MinValue;
                for (int i = 0; i < offer.Odds.Length; i++)
                {
                    float gap = offer.Odds[i] * PredictMoment.TotalChips + (i == offer.TruthIndex ? 1.5f : 0f) - m.ChipsOn(i);
                    if (gap > bestGap)
                    {
                        bestGap = gap;
                        best = i;
                    }
                }

                if (m.ChipsLeft > 0) m.PickHit(best, Hand);
                _nextAction = Time.time + 0.28f;
            }

            private void PlayStory(StoryMoment m)
            {
                if (!m.IsLive || Time.time < _nextAction) return;
                if (m.PickHit(m.Fork % 2 == 0 ? 1 : 3, Hand)) _nextAction = Time.time + 1.0f;
            }
        }
    }
}
