using System;
using System.Collections;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// Stop 1, tokens: the chopper. Real words fly at the rider; each one wears a glowing seam wherever GPT-2's tokenizer would
    /// cut it (the baked, real splits: "jelly|fish", "hamb|urger", eleven pieces for the long one). The rider swipes the laser
    /// through the seams (or shoots them) to chop the word into tokens, and leaves the words the machine keeps whole alone:
    /// a short word GPT-2 knows is one token, and so is the long "unbelievable". Chopping a whole word costs points and the
    /// combo. A swipe is read, in the word's own space, as the laser crossing a seam (so a fast swipe that leaves the word in one
    /// frame still counts); a shot is read as the nearest seam. The last word is the boss. Gameplay only: <see cref="PositionOf"/>
    /// and the events are what the view draws.
    /// </summary>
    public sealed class SliceMoment : Level2Game
    {
        public const int SlotCount = 4;

        /// <summary>The width of a letter of a word, in ems (the view sets the words in a monospaced face of this advance).</summary>
        public const float Advance = 0.62f;

        /// <summary>A word in flight.</summary>
        public sealed class Slot
        {
            public bool Active;
            public string Word;
            public string[] Tokens;
            public bool[] Cut;
            public Vector3 Position;
            public float Size;
            public float Born;
            public float Life;
            public bool Boss;
            public bool Broken;
            public float BrokenAt = -10f;
            public float Phase;
            public float BaseX, BaseY;
            public float AutoAt;

            /// <summary>Where the view measured each seam along the word, metres from its centre (null: the monospaced estimate).</summary>
            public float[] Offsets;

            /// <summary>The word's width as the view measured it (0: the monospaced estimate).</summary>
            public float Measured;

            public int Seams => Tokens.Length - 1;
            public float Width => Measured > 0f ? Measured : Word.Length * Advance * Size;

            public int CutCount
            {
                get
                {
                    int n = 0;
                    foreach (bool c in Cut) if (c) n++;
                    return n;
                }
            }
        }

        /// <summary>What the view needs to play a finished word's tokens flying off.</summary>
        public readonly struct SlicedWord
        {
            public readonly int Slot;
            public readonly string Word;
            public readonly string[] Tokens;
            public readonly Vector3 Position;
            public readonly float Size;
            public readonly bool Boss;

            public SlicedWord(int slot, Slot s)
            {
                Slot = slot;
                Word = s.Word;
                Tokens = s.Tokens;
                Position = s.Position;
                Size = s.Size;
                Boss = s.Boss;
            }
        }

        [SerializeField] private Level2DataSource source;
        [SerializeField] private Level2Session session;
        [Tooltip("The space the words fly in (the stop's root): +z is ahead, the rider sits at its origin.")]
        [SerializeField] private Transform stage;
        [SerializeField] private float spawnEvery = 2.7f;
        [SerializeField] private float lifeSeconds = 7.5f;
        [SerializeField] private float bossLifeSeconds = 15f;
        [SerializeField] private Vector2 farNear = new Vector2(11f, 4.4f);
        [SerializeField] private float wordSize = 0.55f;
        [SerializeField] private float maxWidth = 4.3f;
        [SerializeField] private float bossMaxWidth = 5.2f;
        [SerializeField] private int seamPoints = 10;
        [SerializeField] private int wordBonus = 20;
        [SerializeField] private int wholeBonus = 5;
        [SerializeField] private int brokenPoints = -15;
        [Tooltip("How close a shot must land to a seam to cut it, metres along the word.")]
        [SerializeField] private float shotReach = 0.55f;

        private readonly Slot[] _slots = new Slot[SlotCount];
        private readonly float[,] _last = new float[2, SlotCount];
        private readonly bool[,] _stroking = new bool[2, SlotCount];
        private string[] _order;
        private int _next;
        private int _resolved;
        private bool _hurry;
        private bool _helped;
        private int _good, _bad;
        private bool _saidWhole;
        private bool _saidBoss;

        /// <summary>Seams cut right in a row (the multiplier follows it).</summary>
        public int Combo { get; private set; }

        public int Multiplier => 1 + Mathf.Min(2, Combo / 5);
        public int SeamsCut { get; private set; }
        public int WholeKept { get; private set; }
        public int Mistakes { get; private set; }
        public int Missed { get; private set; }
        public int Total => _order == null ? 0 : _order.Length;
        public int Resolved => _resolved;
        public bool IsLive { get; private set; }
        public Transform Stage => stage;

        public event Action<int> Spawned;
        public event Action<int, int, Vector3, int> SeamWasCut;
        public event Action<SlicedWord> WordDone;
        public event Action<int> KeptWhole;
        public event Action<int> Broken;
        public event Action<int> WordMissed;
        public event Action BossStarted;

        public Slot SlotAt(int i) => _slots[i];

        /// <summary>The word's centre in the stage's space.</summary>
        public Vector3 PositionOf(int slot) => _slots[slot].Position;

        /// <summary>Where a seam sits along a word, metres from its centre (negative: left).</summary>
        public static float SeamOffset(string[] tokens, int seam, float size)
        {
            int letters = 0, before = 0;
            for (int t = 0; t < tokens.Length; t++) letters += tokens[t].Trim().Length;
            for (int t = 0; t <= seam; t++) before += tokens[t].Trim().Length;
            return (before - letters * 0.5f) * Advance * size;
        }

        public float SeamOffset(int slot, int seam)
        {
            Slot s = _slots[slot];
            return s.Offsets != null && seam < s.Offsets.Length ? s.Offsets[seam] : SeamOffset(s.Tokens, seam, s.Size);
        }

        /// <summary>
        /// The view set the word in a face of its own and measured it: the seams sit where the letters really are, and the word is as wide as it
        /// really is. Called as the word spawns, before anything can touch it.
        /// </summary>
        public void SetLayout(int slot, float width, float[] seamOffsets)
        {
            Slot s = _slots[slot];
            s.Measured = width;
            s.Offsets = seamOffsets;
        }

        private void Awake()
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = new Slot();
        }

        public override void Hurry()
        {
            _hurry = true;
            _helped = true;
        }

        protected override int StarsEarned()
        {
            int all = _good + _bad;
            float accuracy = all == 0 ? 1f : _good / (float)all;
            return accuracy >= 0.9f ? 3 : accuracy >= 0.7f ? 2 : 1;
        }

        protected override bool PlayedItself => _helped;

        // ---------- the picks ----------

        private static int Side(int hand) => hand == RideHand.Left ? 0 : 1;

        private bool Alive(int i) => i >= 0 && i < SlotCount && _slots[i].Active && IsLive;

        /// <summary>The laser's place along a word, metres from its centre, where the laser meets the word's plane.</summary>
        private bool TryOffset(int slot, int hand, out float offset)
        {
            offset = 0f;
            if (stage == null || !TryRay(hand, out Ray ray)) return false;
            Vector3 o = stage.InverseTransformPoint(ray.origin);
            Vector3 d = stage.InverseTransformDirection(ray.direction);
            if (Mathf.Abs(d.z) < 1e-4f) return false;
            float t = (_slots[slot].Position.z - o.z) / d.z;
            if (t <= 0f) return false;
            offset = o.x + d.x * t - _slots[slot].Position.x;
            return true;
        }

        public override bool PickHit(int index, int hand)
        {
            if (!Alive(index) || !TryOffset(index, hand, out float u)) return false;
            Slot s = _slots[index];
            int best = -1;
            float bestGap = shotReach;
            for (int k = 0; k < s.Seams; k++)
            {
                if (s.Cut[k]) continue;
                float gap = Mathf.Abs(SeamOffset(index, k) - u);
                if (gap >= bestGap) continue;
                bestGap = gap;
                best = k;
            }

            if (best < 0) return false;
            Cut(index, best, hand);
            return true;
        }

        public override bool PickStrokeBegin(int index, int hand, Vector3 point, bool pressed)
        {
            if (!Alive(index) || !TryOffset(index, hand, out float u)) return false;
            _last[Side(hand), index] = u;
            _stroking[Side(hand), index] = true;
            return true;
        }

        public override void PickStrokeMove(int index, int hand, Vector3 point)
        {
            if (!Alive(index) || !_stroking[Side(hand), index] || !TryOffset(index, hand, out float u)) return;
            Cross(index, hand, _last[Side(hand), index], u);
            _last[Side(hand), index] = u;
        }

        public override void PickStrokeEnd(int index, int hand)
        {
            if (index < 0 || index >= SlotCount) return;
            // A fast swipe leaves the word in one frame: read where the laser is now on the word's plane, so the last stretch counts.
            if (Alive(index) && _stroking[Side(hand), index] && TryOffset(index, hand, out float u)) Cross(index, hand, _last[Side(hand), index], u);
            _stroking[Side(hand), index] = false;
        }

        /// <summary>The laser went from <paramref name="a"/> to <paramref name="b"/> along word i: every seam between them is cut.</summary>
        private void Cross(int i, int hand, float a, float b)
        {
            Slot s = _slots[i];
            if (Mathf.Abs(b - a) < 1e-4f) return;
            float lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            if (s.Seams == 0)
            {
                // A whole word swiped through the middle: the machine keeps it whole.
                if (lo <= 0f && hi >= 0f && Time.time - s.BrokenAt > 0.9f) BreakWhole(i);
                return;
            }

            for (int k = 0; k < s.Seams; k++)
            {
                if (s.Cut[k]) continue;
                float at = SeamOffset(i, k);
                if (at >= lo && at <= hi) Cut(i, k, hand);
            }
        }

        // ---------- the rules ----------

        private void Cut(int i, int seam, int hand)
        {
            Slot s = _slots[i];
            if (!s.Active || s.Cut[seam]) return;
            s.Cut[seam] = true;
            SeamsCut++;
            Combo++;
            _good++;
            int points = (s.Boss ? seamPoints + 5 : seamPoints) * Multiplier;
            Vector3 at = s.Position + new Vector3(SeamOffset(i, seam), 0f, 0f);
            AddScore(points, stage != null ? stage.TransformPoint(at) : at);
            SeamWasCut?.Invoke(i, seam, at, points);
            RideSounds.Play("fire", 0.9f + Mathf.Min(0.6f, Combo * 0.04f));
            RideAim.Buzz(hand, 0.5f, 0.05f);
            if (s.CutCount == s.Seams) Finish(i, hand);
        }

        private void Finish(int i, int hand)
        {
            Slot s = _slots[i];
            AddScore(wordBonus * (s.Boss ? 3 : 1), stage != null ? stage.TransformPoint(s.Position) : s.Position);
            if (session != null) session.AddTokens(s.Tokens.Length);
            WordDone?.Invoke(new SlicedWord(i, s));
            RideSounds.Play("solved", 1f + (s.Boss ? 0.2f : 0f));
            RideAim.Buzz(hand, 0.8f, 0.12f);
            Retire(i);
        }

        private void BreakWhole(int i)
        {
            Slot s = _slots[i];
            s.Broken = true;
            s.BrokenAt = Time.time;
            Mistakes++;
            _bad++;
            Combo = 0;
            AddScore(brokenPoints, stage != null ? stage.TransformPoint(s.Position) : s.Position);
            Broken?.Invoke(i);
            RideSounds.Play("wrong", 1f);
            if (!_saidWhole && Director != null)
            {
                _saidWhole = true;
                Director.Say(RideLine.TokWhole);
            }
        }

        private void Retire(int i)
        {
            _slots[i].Active = false;
            _resolved++;
        }

        private bool FreeSlot(out int slot)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].Active) continue;
                slot = i;
                return true;
            }

            slot = -1;
            return false;
        }

        private int ActiveCount()
        {
            int n = 0;
            foreach (Slot s in _slots) if (s.Active) n++;
            return n;
        }

        private static readonly float[] Lanes = { -1.5f, 2.4f, 0.5f, -0.4f, 1.4f };
        private static readonly float[] Rows = { 1.55f, 2.45f, 1.95f, 2.9f };

        private void Spawn(int i, string word, bool boss, int ordinal)
        {
            Level2Data data = source.Data;
            L2Word baked = data.Word(word);
            Slot s = _slots[i];
            s.Active = true;
            s.Word = word;
            s.Tokens = baked.tokens;
            s.Cut = new bool[Mathf.Max(0, baked.tokens.Length - 1)];
            s.Boss = boss;
            s.Offsets = null;
            s.Measured = 0f;
            s.Broken = false;
            s.BrokenAt = -10f;
            float limit = boss ? bossMaxWidth : maxWidth;
            float size = boss ? limit / (word.Length * Advance) : Mathf.Min(wordSize, limit / (word.Length * Advance));
            s.Size = Mathf.Min(size, boss ? 0.24f : wordSize);
            s.Born = Time.time;
            s.Life = boss ? bossLifeSeconds : lifeSeconds;
            s.Phase = ordinal * 1.7f;
            s.BaseX = boss ? 0f : Lanes[ordinal % Lanes.Length];
            s.BaseY = boss ? 2.1f : Rows[i % Rows.Length];
            s.AutoAt = Time.time + 0.6f;
            Place(s, 0f);
            _stroking[0, i] = _stroking[1, i] = false;
            Spawned?.Invoke(i);
            if (boss)
            {
                BossStarted?.Invoke();
                if (!_saidBoss && Director != null)
                {
                    _saidBoss = true;
                    Director.Say(RideLine.TokBoss);
                }
            }
        }

        private void Place(Slot s, float age)
        {
            float t = Mathf.Clamp01(age / s.Life);
            // Fast at first, easing into the rider's reach and lingering there: the last third of its life is spent close.
            float eased = 1f - (1f - t) * (1f - t);
            float z = Mathf.Lerp(farNear.x, farNear.y, eased);
            s.Position = new Vector3(s.BaseX + 0.25f * Mathf.Sin(age * 1.1f + s.Phase), s.BaseY + 0.1f * Mathf.Sin(age * 1.9f + s.Phase), z);
        }

        // ---------- the game ----------

        protected override IEnumerator Play()
        {
            Level2Data data = source.Data;
            var order = new System.Collections.Generic.List<string>(Level2Data.SliceWords) { Level2Data.BossWord };
            _order = order.ToArray();
            _next = 0;
            _resolved = 0;
            _good = _bad = 0;
            _helped = false;
            _hurry = false;
            _saidWhole = _saidBoss = false;
            SeamsCut = WholeKept = Mistakes = Missed = 0;
            Combo = 0;
            foreach (Slot s in _slots) s.Active = false;
            IsLive = true;
            float opened = Time.time;
            float nextSpawn = Time.time + 1.0f;

            while (_resolved < _order.Length)
            {
                Progress = Total == 0 ? 0f : _resolved / (float)Total;
                SetGoal("Swipe the glowing seams:  " + _resolved + " of " + Total + " words");
                if (!_hurry && Time.time - opened > giveUpAfter)
                {
                    _hurry = true;
                    _helped = true;
                }

                bool bossTurn = _next == _order.Length - 1;
                if (_next < _order.Length && Time.time >= nextSpawn && FreeSlot(out int free) && (!bossTurn || ActiveCount() == 0))
                {
                    Spawn(free, _order[_next], bossTurn, _next);
                    _next++;
                    nextSpawn = Time.time + (_hurry ? 0.5f : spawnEvery);
                }

                for (int i = 0; i < SlotCount; i++)
                {
                    Slot s = _slots[i];
                    if (!s.Active) continue;
                    float age = Time.time - s.Born;
                    Place(s, age);

                    if (_hurry && s.Seams > 0 && Time.time >= s.AutoAt)
                    {
                        for (int k = 0; k < s.Seams; k++)
                        {
                            if (s.Cut[k]) continue;
                            s.AutoAt = Time.time + 0.2f;
                            Cut(i, k, -1);
                            break;
                        }

                        if (!s.Active) continue;
                    }

                    if (age < s.Life) continue;
                    Expire(i);
                }

                yield return null;
            }

            IsLive = false;
            yield return new WaitForSeconds(0.5f);
        }

        private void Expire(int i)
        {
            Slot s = _slots[i];
            if (s.Seams == 0)
            {
                if (!s.Broken)
                {
                    WholeKept++;
                    _good++;
                    AddScore(wholeBonus, stage != null ? stage.TransformPoint(s.Position) : s.Position);
                    if (session != null) session.AddTokens(1);
                    KeptWhole?.Invoke(i);
                    RideSounds.Play("tick", 1.3f);
                }
            }
            else
            {
                Missed++;
                _bad++;
                Combo = 0;
                WordMissed?.Invoke(i);
                RideSounds.Play("wrong", 0.8f);
            }

            Retire(i);
        }
    }
}
