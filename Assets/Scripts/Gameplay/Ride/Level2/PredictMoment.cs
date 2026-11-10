using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// Stop 4, prediction: the odds city. A sentence stops one word short ("In the morning I drink a cup of ___") and the rider has
    /// ten chips to spread over the words it might be. Every shot adds a chip to the word it hits (right hand), the left hand takes
    /// one back; the rider locks in, and the machine's real odds for the same words rise next to the chips, then the real next word
    /// is shown. Chips on the right word score, and so does beating the machine's own confidence in it. The odds are GPT-2's own
    /// probabilities for the prompt (the bake); the right word is the one the text really continues with, which the machine
    /// sometimes gave only a small chance. The rider learns what the machine's output is: not an answer, but odds.
    /// </summary>
    public sealed class PredictMoment : Level2Game
    {
        public const int TotalChips = 10;
        public const int LockIndex = 100;

        public enum Phase { Waiting, Betting, Reveal }

        /// <summary>One round: a prompt and the words on offer, with the machine's odds for each.</summary>
        public sealed class Offer
        {
            public string Prompt;
            public string Truth;
            public string[] Words;
            public float[] Odds;       // one per word, then the rest (every other word the machine knows)
            public int TruthIndex;
            public float TruthOdds;
            public int TruthRank;

            public int OtherIndex => Words.Length;
        }

        [SerializeField] private Level2DataSource source;
        [SerializeField] private float betSeconds = 26f;
        [SerializeField] private float revealSeconds = 7f;
        [SerializeField] private float settleSeconds = 1.4f;
        [SerializeField] private int words = 6;
        [SerializeField] private int chipPoints = 10;
        [SerializeField] private int beatBonus = 40;

        private readonly List<Offer> _offers = new List<Offer>();
        private int[] _chips = new int[0];
        private float _changedAt;
        private float _betStarted;
        private bool _lock;
        private bool _hurry;
        private bool _helped;
        private int _totalOnTruth;
        private bool _saidBeat;
        private bool _saidMiss;

        public Phase Stage { get; private set; }
        public int Round { get; private set; }
        public int RoundCount => Level2Data.PredictPrompts.Length;
        public Offer Current => Round >= 0 && Round < _offers.Count ? _offers[Round] : null;
        public int ChipsLeft { get; private set; }
        public int ChipsOnTruth => Current != null && _chips.Length > Current.TruthIndex ? _chips[Current.TruthIndex] : 0;
        public bool BeatTheMachine { get; private set; }
        public int RoundPoints { get; private set; }
        public int TotalOnTruth => _totalOnTruth;
        public override float SecondsLeft => Stage == Phase.Betting ? Mathf.Max(0f, betSeconds - (Time.time - _betStarted)) : -1f;

        public int ChipsOn(int i) => i >= 0 && i < _chips.Length ? _chips[i] : 0;

        public event Action<int> RoundStarted;
        public event Action<int, int> ChipsChanged;
        public event Action<int> Locked;
        public event Action<int, int, float, bool> Revealed;

        /// <summary>Reads the offers from the bake (once), so the view can build the towers before the game opens.</summary>
        public void Prepare()
        {
            if (_offers.Count > 0 || source == null) return;
            Level2Data data = source.Data;
            foreach (string prompt in Level2Data.PredictPrompts) _offers.Add(MakeOffer(data.Prediction(prompt)));
        }

        public Offer OfferAt(int round) => round >= 0 && round < _offers.Count ? _offers[round] : null;

        private Offer MakeOffer(L2Prediction p)
        {
            // The most likely words, plus the true one if it is not among them, in alphabetical order (the order must not give the answer).
            var picked = new List<L2Choice>();
            foreach (L2Choice c in p.top)
            {
                if (picked.Count == words) break;
                picked.Add(c);
            }

            if (!picked.Exists(c => c.word == p.truth))
            {
                picked.RemoveAt(picked.Count - 1);
                picked.Add(Array.Find(p.top, c => c.word == p.truth) ?? new L2Choice { word = p.truth, p = p.truthP });
            }

            picked.Sort((a, b) => string.CompareOrdinal(a.word.Trim(), b.word.Trim()));
            var offer = new Offer { Prompt = p.prompt, Truth = p.truth, TruthOdds = p.truthP, TruthRank = p.truthRank, Words = new string[picked.Count], Odds = new float[picked.Count + 1] };
            float total = 0f;
            for (int i = 0; i < picked.Count; i++)
            {
                offer.Words[i] = Level2Data.Clean(picked[i].word);
                offer.Odds[i] = picked[i].p;
                total += picked[i].p;
                if (picked[i].word == p.truth) offer.TruthIndex = i;
            }

            offer.Odds[picked.Count] = Mathf.Max(0f, 1f - total);
            return offer;
        }

        public override void Hurry()
        {
            _hurry = true;
            _helped = true;
        }

        protected override int StarsEarned() => _totalOnTruth >= 22 ? 3 : _totalOnTruth >= 14 ? 2 : 1;

        protected override bool PlayedItself => _helped;

        // ---------- the picks: a tower per word, one for "everything else", and the lock button ----------

        public override bool PickHit(int index, int hand)
        {
            if (Stage != Phase.Betting || Current == null) return false;
            if (index == LockIndex)
            {
                if (ChipsLeft == TotalChips) return false;
                _lock = true;
                return true;
            }

            if (index < 0 || index > Current.OtherIndex) return false;
            bool take = hand == RideHand.Left;
            if (take)
            {
                if (_chips[index] == 0) return false;
                _chips[index]--;
                ChipsLeft++;
                RideSounds.Play("tick", 0.8f);
            }
            else
            {
                if (ChipsLeft == 0) return false;
                _chips[index]++;
                ChipsLeft--;
                RideSounds.Play("tick", 1f + (TotalChips - ChipsLeft) * 0.07f);
            }

            _changedAt = Time.time;
            ChipsChanged?.Invoke(index, _chips[index]);
            return true;
        }

        // ---------- the game ----------

        protected override IEnumerator Play()
        {
            Prepare();
            _hurry = false;
            _helped = false;
            _totalOnTruth = 0;
            _saidBeat = _saidMiss = false;
            for (Round = 0; Round < RoundCount; Round++)
            {
                Offer offer = Current;
                _chips = new int[offer.Words.Length + 1];
                ChipsLeft = TotalChips;
                _lock = false;
                _changedAt = Time.time;
                _betStarted = Time.time;
                BeatTheMachine = false;
                RoundPoints = 0;
                Stage = Phase.Betting;
                RoundStarted?.Invoke(Round);
                SetGoal("Guess the next word: more chips on the words you think are likely  (" + (Round + 1) + " of " + RoundCount + ")");

                while (Stage == Phase.Betting)
                {
                    Progress = (Round + (TotalChips - ChipsLeft) / (float)TotalChips * 0.5f) / RoundCount;
                    if (_hurry && ChipsLeft > 0)
                    {
                        // The machine bets for the rider, in proportion to its own odds.
                        BetLikeTheMachine(offer);
                        _lock = true;
                    }

                    bool allIn = ChipsLeft == 0 && Time.time - _changedAt > settleSeconds;
                    if (_lock || allIn || Time.time - _betStarted > betSeconds) break;
                    yield return null;
                }

                Stage = Phase.Reveal;
                Locked?.Invoke(Round);
                int onTruth = _chips[offer.TruthIndex];
                _totalOnTruth += onTruth;
                BeatTheMachine = onTruth / (float)TotalChips > offer.TruthOdds + 0.02f;
                RoundPoints = onTruth * chipPoints + (BeatTheMachine ? beatBonus : 0);
                AddScore(RoundPoints, Vector3.zero);
                Revealed?.Invoke(Round, onTruth, offer.TruthOdds, BeatTheMachine);
                RideSounds.Play(onTruth > 0 ? "winShimmer" : "wrong", 1f);
                if (BeatTheMachine && !_saidBeat && Director != null)
                {
                    _saidBeat = true;
                    Director.Say(RideLine.PredBeat);
                }
                else if (onTruth == 0 && !_saidMiss && Director != null)
                {
                    _saidMiss = true;
                    Director.Say(RideLine.PredMiss);
                }

                yield return new WaitForSeconds(_hurry ? 2f : revealSeconds);
            }

            Stage = Phase.Waiting;
        }

        private void BetLikeTheMachine(Offer offer)
        {
            // Largest remainders: the chips follow the odds as closely as ten chips can.
            int[] chips = new int[offer.Words.Length + 1];
            float[] remainder = new float[chips.Length];
            int given = 0;
            for (int i = 0; i < chips.Length; i++)
            {
                float exact = offer.Odds[i] * TotalChips;
                chips[i] = Mathf.FloorToInt(exact);
                remainder[i] = exact - chips[i];
                given += chips[i];
            }

            while (given < TotalChips)
            {
                int best = 0;
                for (int i = 1; i < chips.Length; i++) if (remainder[i] > remainder[best]) best = i;
                chips[best]++;
                remainder[best] = -1f;
                given++;
            }

            _chips = chips;
            ChipsLeft = 0;
            for (int i = 0; i < chips.Length; i++) ChipsChanged?.Invoke(i, chips[i]);
        }
    }
}
