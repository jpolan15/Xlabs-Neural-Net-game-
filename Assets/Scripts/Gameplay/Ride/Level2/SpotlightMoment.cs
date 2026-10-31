using System;
using System.Collections;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// Stop 3, attention: the spotlight hall. A sentence hangs in front of the rider with one word lit, the word whose meaning is
    /// being worked out ("dragon" in "The tiny green dragon ate the pizza", or "it" in "She poured milk into the glass and drank
    /// it"). The machine reads left to right, so it can only look back at the words before it; the rider is the attention: holding
    /// the trigger on a word pours attention into it, and the shares of all the earlier words always add up to a hundred percent
    /// (a softmax, as in the model), so looking at one word takes attention from the rest. When enough of the share sits on the
    /// words that explain the lit one, the round is won and the real weights of a GPT-2 attention head are shown beside the
    /// rider's. Which words matter is the bake's (the words a person would say; checked against the real head, which agrees).
    /// </summary>
    public sealed class SpotlightMoment : Level2Game
    {
        public enum Phase { Waiting, Aim, Reveal }

        [SerializeField] private Level2DataSource source;
        [Tooltip("The share of attention that must sit on the words that explain the lit one.")]
        [SerializeField] private float winShare = 0.80f;
        [Tooltip("When there are two words to look at, each needs at least this much.")]
        [SerializeField] private float eachShare = 0.25f;
        [SerializeField] private float holdSeconds = 0.8f;
        [SerializeField] private float roundSeconds = 24f;
        [SerializeField] private float revealSeconds = 5f;
        [SerializeField] private float pourRate = 3.2f;
        [SerializeField] private float drainRate = 0.5f;
        [SerializeField] private float maxLogit = 4.5f;
        [SerializeField] private int roundPoints = 40;
        [SerializeField] private int wrongPoints = -5;

        private readonly int[] _focus = { -1, -1 };
        private float[] _logits = new float[0];
        private float[] _shares = new float[0];
        private float _roundStarted;
        private float _heldFor;
        private float _wrongFor;
        private bool _hurry;
        private bool _helped;
        private int _wins;
        private int _wrongs;
        private bool _saidSpread;
        private bool _saidWrong;

        public Phase Stage { get; private set; }
        public int Round { get; private set; }
        public int RoundCount => Level2Data.SpotlightRounds.Length;
        public L2Attention Current { get; private set; }
        public bool LastWon { get; private set; }

        /// <summary>The words of the sentence the view lays out, and the one being worked out.</summary>
        public string[] Words => Current != null ? Current.words : new string[0];

        public int QueryIndex => Current != null ? Current.query : -1;

        /// <summary>The share of attention on word i, 0 to 1 (zero for a word the machine cannot see yet).</summary>
        public float Share(int i) => i >= 0 && i < _shares.Length ? _shares[i] : 0f;

        /// <summary>How much of the share sits on the words that matter.</summary>
        public float AnswerShare
        {
            get
            {
                if (Current == null) return 0f;
                float sum = 0f;
                foreach (int a in Current.answers) sum += Share(a);
                return sum;
            }
        }

        /// <summary>True when word i is one of the words before the lit one: the only ones the machine can look at.</summary>
        public bool CanLookAt(int i) => Current != null && i >= 0 && i < Current.query;

        public int FocusOf(int hand) => _focus[hand == RideHand.Left ? 0 : 1];

        public bool IsLive => Stage == Phase.Aim;

        public override float SecondsLeft => Stage == Phase.Aim ? Mathf.Max(0f, roundSeconds - (Time.time - _roundStarted)) : -1f;

        public event Action<int> RoundStarted;
        public event Action<int, bool> RoundEnded;
        public event Action<int> WrongWord;

        public override void Hurry()
        {
            _hurry = true;
            _helped = true;
        }

        protected override int StarsEarned() => _wins == RoundCount && _wrongs == 0 ? 3 : _wins >= RoundCount - 1 ? 2 : 1;

        protected override bool PlayedItself => _helped;

        // ---------- the picks (one per word of the sentence) ----------

        public override bool PickStrokeBegin(int index, int hand, Vector3 point, bool pressed)
        {
            if (Stage != Phase.Aim || !CanLookAt(index)) return false;
            _focus[hand == RideHand.Left ? 0 : 1] = index;
            return true;
        }

        public override void PickStrokeMove(int index, int hand, Vector3 point)
        {
            if (Stage == Phase.Aim && CanLookAt(index)) _focus[hand == RideHand.Left ? 0 : 1] = index;
        }

        public override void PickStrokeEnd(int index, int hand)
        {
            int side = hand == RideHand.Left ? 0 : 1;
            if (_focus[side] == index) _focus[side] = -1;
        }

        /// <summary>A press with no stroke (a mouse click, a shot): a short burst of attention.</summary>
        public override bool PickHit(int index, int hand)
        {
            if (Stage != Phase.Aim || !CanLookAt(index)) return false;
            _logits[index] = Mathf.Min(maxLogit, _logits[index] + 1.2f);
            RideSounds.Play("tick", 1.2f);
            return true;
        }

        // ---------- the game ----------

        private void Update()
        {
            if (Stage != Phase.Aim || Current == null) return;
            int n = Current.query;
            for (int side = 0; side < 2; side++)
            {
                int f = _focus[side];
                if (f >= 0 && f < n) _logits[f] = Mathf.Min(maxLogit, _logits[f] + pourRate * Time.deltaTime);
            }

            for (int i = 0; i < n; i++)
            {
                bool looked = _focus[0] == i || _focus[1] == i;
                if (!looked) _logits[i] = Mathf.Max(0f, _logits[i] - drainRate * Time.deltaTime);
            }

            Softmax(n);
        }

        private void Softmax(int n)
        {
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                _shares[i] = Mathf.Exp(_logits[i]);
                total += _shares[i];
            }

            for (int i = 0; i < n; i++) _shares[i] = total > 0f ? _shares[i] / total : 0f;
        }

        private bool Satisfied()
        {
            if (AnswerShare < winShare) return false;
            if (Current.answers.Length < 2) return true;
            foreach (int a in Current.answers) if (Share(a) < eachShare) return false;
            return true;
        }

        private int TopShare(out float share)
        {
            int best = -1;
            share = 0f;
            for (int i = 0; i < Current.query; i++)
            {
                if (_shares[i] <= share) continue;
                share = _shares[i];
                best = i;
            }

            return best;
        }

        protected override IEnumerator Play()
        {
            _hurry = false;
            _helped = false;
            _wins = _wrongs = 0;
            _saidSpread = _saidWrong = false;
            _focus[0] = _focus[1] = -1;
            for (Round = 0; Round < RoundCount; Round++)
            {
                Current = source.Data.Attention(Level2Data.SpotlightRounds[Round]);
                _logits = new float[Current.words.Length];
                _shares = new float[Current.words.Length];
                Softmax(Current.query);
                _roundStarted = Time.time;
                _heldFor = _wrongFor = 0f;
                LastWon = false;
                Stage = Phase.Aim;
                RoundStarted?.Invoke(Round);
                if (Current.answers.Length > 1 && !_saidSpread && Director != null)
                {
                    _saidSpread = true;
                    Director.Say(RideLine.AttSpread);
                }

                while (Stage == Phase.Aim)
                {
                    Progress = (Round + Mathf.Clamp01(AnswerShare)) / RoundCount;
                    SetGoal("Find the words that explain  \"" + Current.words[Current.query] + "\"  (" + (Round + 1) + " of " + RoundCount + ")");
                    if (_hurry)
                    {
                        // The machine looks for the rider: every word that matters, in turn.
                        foreach (int a in Current.answers) _logits[a] = Mathf.MoveTowards(_logits[a], maxLogit, 6f * Time.deltaTime);
                        Softmax(Current.query);
                    }

                    _heldFor = Satisfied() ? _heldFor + Time.deltaTime : 0f;
                    int top = TopShare(out float topShare);
                    bool onWrong = top >= 0 && topShare >= 0.6f && Array.IndexOf(Current.answers, top) < 0;
                    _wrongFor = onWrong ? _wrongFor + Time.deltaTime : 0f;

                    if (_heldFor >= holdSeconds)
                    {
                        LastWon = true;
                        _wins++;
                        float left = SecondsLeft;
                        AddScore(roundPoints + Mathf.RoundToInt(Mathf.Min(30f, left * 2f)), Vector3.zero);
                        RideSounds.Play("solved", 1f);
                        break;
                    }

                    if (_wrongFor >= 1.1f)
                    {
                        _wrongFor = 0f;
                        _wrongs++;
                        _logits[top] = 0f;
                        AddScore(wrongPoints, Vector3.zero);
                        WrongWord?.Invoke(top);
                        RideSounds.Play("wrong", 1f);
                        if (!_saidWrong && Director != null)
                        {
                            _saidWrong = true;
                            Director.Say(RideLine.AttWrong);
                        }
                    }

                    if (Time.time - _roundStarted > roundSeconds) break;
                    yield return null;
                }

                Stage = Phase.Reveal;
                _focus[0] = _focus[1] = -1;
                RoundEnded?.Invoke(Round, LastWon);
                yield return new WaitForSeconds(_hurry ? 1.5f : revealSeconds);
            }

            Stage = Phase.Waiting;
        }
    }
}
