using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;

namespace Convergence.EditorTools
{
    /// <summary>
    /// A rider for Play Mode: it plays Level 2's games the way a person would, through the games' own pick calls and the same hand-aim
    /// data the lasers write (RideAim), so a desktop run exercises every rule without a headset, and the live shots catch the games
    /// mid-play. It pulls GO after each win. It swipes through seams, carries words to their neighbourhoods, holds its laser on the
    /// words that matter, bets chips and shoots story gates. Skill is a knob (the chance it cuts a whole word, bets against the
    /// machine, chooses a wild lane) so a run can also show the mistakes. Development tooling only.
    /// </summary>
    [InitializeOnLoad]
    public static class Level2Bot
    {
        private static bool _on;
        private static float _skill = 1f;
        private static float _nextAction;
        private static int _hand = RideHand.Right;
        private static RideDirector _director;

        // The swipe in progress.
        private static int _swipeSlot = -1;
        private static float _swipeStart;
        private static float _swipeSeconds = 0.38f;

        // The word being carried.
        private static int _carry = -1;
        private static float _carryStart;
        private static int _carryCluster;

        static Level2Bot()
        {
            EditorApplication.update += Tick;
        }

        /// <summary>Starts the bot (Play Mode only). Skill 0 to 1: how often it does the right thing.</summary>
        public static string Start(float skill = 1f)
        {
            _on = true;
            _skill = skill;
            _swipeSlot = -1;
            _carry = -1;
            return "bot on, skill " + skill;
        }

        public static string Stop()
        {
            _on = false;
            return "bot off";
        }

        private static Vector3 Eye(Transform stage) => stage.TransformPoint(new Vector3(0f, 1.3f, 0f));

        private static void Aim(Transform stage, Vector3 stagePoint)
        {
            Vector3 origin = stage.TransformPoint(new Vector3(0.25f, 1.05f, 0.2f));
            Vector3 point = stage.TransformPoint(stagePoint);
            RideAim.Set(RideHand.Right, origin, point, true);
            RideAim.Set(RideHand.Left, origin, point, true);
        }

        private static void Tick()
        {
            if (!_on || !Application.isPlaying) return;
            if (_director == null) _director = Object.FindAnyObjectByType<RideDirector>();
            if (_director == null) return;

            if (_director.WaitingForGo && Time.time >= _nextAction)
            {
                RideControlChannel go = _director.Controls != null ? _director.Controls.Get(RideControlIds.Action) : null;
                if (go != null) go.SetFromUser(1f);
                _nextAction = Time.time + 1.5f;
                return;
            }

            RideMoment moment = _director.ActiveMoment;
            if (moment is SliceMoment slice) PlaySlice(slice);
            else if (moment is GalaxyMoment galaxy) PlayGalaxy(galaxy);
            else if (moment is SpotlightMoment spot) PlaySpot(spot);
            else if (moment is PredictMoment bet) PlayBet(bet);
            else if (moment is StoryMoment story) PlayStory(story);
        }

        // ---------- stop 1: swipe through the seams ----------

        private static void PlaySlice(SliceMoment m)
        {
            if (!m.IsLive || m.Stage == null) return;
            if (_swipeSlot >= 0)
            {
                SliceMoment.Slot s = m.SlotAt(_swipeSlot);
                float t = (Time.time - _swipeStart) / _swipeSeconds;
                float half = s.Width * 0.5f + 0.35f;
                float x = Mathf.Lerp(-half, half, Mathf.Clamp01(t));
                if (!s.Active || t > 1.05f)
                {
                    m.PickStrokeEnd(_swipeSlot, _hand);
                    _swipeSlot = -1;
                    _nextAction = Time.time + 0.25f;
                    return;
                }

                Aim(m.Stage, new Vector3(s.Position.x + x, s.Position.y, s.Position.z));
                m.PickStrokeMove(_swipeSlot, _hand, Vector3.zero);
                return;
            }

            if (Time.time < _nextAction) return;
            for (int i = 0; i < SliceMoment.SlotCount; i++)
            {
                SliceMoment.Slot s = m.SlotAt(i);
                if (!s.Active || Time.time - s.Born < 1.2f) continue;
                bool wantsCut = s.Seams > 0 && s.CutCount < s.Seams;
                bool mistake = s.Seams == 0 && !s.Broken && Random.value > _skill && Random.value < 0.5f;
                if (!wantsCut && !mistake) continue;
                if (wantsCut && Random.value > _skill) continue;
                Aim(m.Stage, new Vector3(s.Position.x - s.Width * 0.5f - 0.35f, s.Position.y, s.Position.z));
                if (!m.PickStrokeBegin(i, _hand, Vector3.zero, true)) continue;
                _swipeSlot = i;
                _swipeStart = Time.time;
                _swipeSeconds = s.Boss ? 0.9f : 0.38f;
                return;
            }
        }

        // ---------- stop 2: carry the loose words home ----------

        private static void PlayGalaxy(GalaxyMoment m)
        {
            if (!m.IsLive || m.Stage == null) return;
            if (_carry >= 0)
            {
                GalaxyMoment.Item item = m.Items[_carry];
                GalaxyMoment.Cluster target = m.Clusters[_carryCluster];
                float t = (Time.time - _carryStart) / 0.9f;
                Vector3 from = item.Float;
                Vector3 to = target.Centre;
                Vector3 p = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                Aim(m.Stage, p);
                if (t >= 1.15f)
                {
                    RideAim.RaiseReleased(_hand);
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
                if (!m.PickHit(i, _hand)) continue;
                _carry = i;
                _carryStart = Time.time;
                int right = m.Clusters.Count;
                _carryCluster = 0;
                for (int c = 0; c < m.Clusters.Count; c++) if (m.Clusters[c].Category == item.Category) _carryCluster = c;
                if (Random.value > _skill) _carryCluster = (_carryCluster + 1) % right;
                return;
            }
        }

        // ---------- stop 3: hold the laser on the words that matter ----------

        private static void PlaySpot(SpotlightMoment m)
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

            int[] answers = m.Current.answers;
            if (Time.time < _nextAction) return;
            // One hand on each word that matters, kept there.
            for (int k = 0; k < answers.Length && k < 2; k++)
            {
                int hand = k == 0 ? RideHand.Right : RideHand.Left;
                int word = Random.value > _skill ? Mathf.Max(0, answers[k] - 1) : answers[k];
                if (m.FocusOf(hand) != word) m.PickStrokeBegin(word, hand, Vector3.zero, true);
            }

            _nextAction = Time.time + 0.2f;
        }

        // ---------- stop 4: spread the chips the way the machine would, mostly ----------

        private static void PlayBet(PredictMoment m)
        {
            if (m.Stage != PredictMoment.Phase.Betting || m.Current == null) return;
            if (Time.time < _nextAction) return;
            PredictMoment.Offer offer = m.Current;
            // Aim for the machine's own odds, nudged toward the true word when skilled.
            int best = 0;
            float bestGap = float.MinValue;
            for (int i = 0; i < offer.Odds.Length; i++)
            {
                float want = offer.Odds[i] * PredictMoment.TotalChips + (i == offer.TruthIndex ? 1.5f * _skill : 0f);
                float gap = want - m.ChipsOn(i);
                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            if (m.ChipsLeft > 0) m.PickHit(best, RideHand.Right);
            _nextAction = Time.time + 0.28f;
        }

        // ---------- stop 5: choose a lane ----------

        private static void PlayStory(StoryMoment m)
        {
            if (!m.IsLive || Time.time < _nextAction) return;
            int lane = Random.value < _skill ? (m.Fork % 2 == 0 ? 1 : 3) : 0;
            if (m.PickHit(lane, RideHand.Right)) _nextAction = Time.time + 1.0f;
        }
    }
}
