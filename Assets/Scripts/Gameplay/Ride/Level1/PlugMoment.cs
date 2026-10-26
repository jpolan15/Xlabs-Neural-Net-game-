using System;
using System.Collections;
using System.Collections.Generic;
using Convergence.Core.Puzzles;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 4, a mini-game: "plug the strings". Pixel just said the wrong answer. The ten answer sockets start from the
    /// untrained network's real scores for the hero 3 (its logits), so the "1" glows brightest, as the forward pass
    /// showed. Each string is one of the six brightest real hidden neurons for that 3. The rider grabs a string with the
    /// laser (trigger or A) and plugs it into a socket; Core's <see cref="PlugPuzzle"/> adds a strong wire from that
    /// neuron to that answer and decides when the 3 wins. Plugging into a wrong answer makes it brighter, and Pixel
    /// says so. After <see cref="RideGame.giveUpAfter"/> (or an operator hurry) the game plugs Core's hints itself.
    /// </summary>
    public sealed class PlugMoment : RideGame
    {
        [SerializeField] private Level1DataSource source;
        [SerializeField] private int stringCount = 6;
        [Tooltip("The weight of one plugged wire (a strong one: the untrained weights are about ±0.5).")]
        [SerializeField] private float plugWeight = 1.5f;
        [SerializeField] private float autoStepSeconds = 1f;
        [SerializeField] private float wrongEvery = 8f;

        private PlugPuzzle _puzzle;
        private int[] _neurons;
        private readonly int[] _carrying = { -1, -1 };
        private bool _live;
        private bool _hurry;
        private bool _byRider = true;
        private int _plugs;
        private float _lastWrong = -100f;

        public PlugPuzzle Puzzle => _puzzle;
        public bool IsLive => _live;

        /// <summary>The hidden neuron string s is.</summary>
        public int NeuronOf(int s) => _neurons[s];

        /// <summary>The string a hand is carrying, or -1.</summary>
        public int Carrying(int hand) => _carrying[hand];

        /// <summary>Anything changed: a plug, an unplug, a pick-up.</summary>
        public event Action Changed;

        public event Action<bool> LiveChanged;

        /// <summary>String s went into socket k (k = -1: dropped loose).</summary>
        public event Action<int, int> Plugged;

        public void Prepare()
        {
            if (_puzzle != null || source == null) return;
            Level1Data data = source.Data;
            Level1DataFile f = data.File;
            int hidden = f.hiddenSize;
            double[] h = f.hero.untrainedHidden;
            var logits = new double[f.outputSize];
            for (int k = 0; k < logits.Length; k++)
            {
                double z = f.initial.b2[k];
                for (int j = 0; j < hidden; j++) z += f.initial.w2[k * hidden + j] * h[j];
                logits[k] = z;
            }

            var order = new List<int>();
            for (int j = 0; j < hidden; j++) order.Add(j);
            order.Sort((a, b) => h[b] != h[a] ? h[b].CompareTo(h[a]) : a.CompareTo(b));
            _neurons = order.GetRange(0, Mathf.Min(stringCount, hidden)).ToArray();
            var brightness = new double[_neurons.Length];
            for (int s = 0; s < _neurons.Length; s++) brightness[s] = h[_neurons[s]];
            _puzzle = new PlugPuzzle(logits, brightness, Level1Data.HeroDigit, plugWeight);
        }

        private void Start() => Prepare();

        public override void Hurry() => _hurry = true;

        /// <summary>The laser hit string s: pick it up (out of any socket it was in).</summary>
        public bool GrabString(int s, int hand)
        {
            if (!_live || s < 0 || s >= _puzzle.StringCount) return false;
            for (int other = 0; other < 2; other++) if (_carrying[other] == s) _carrying[other] = -1;
            _carrying[hand] = s;
            _puzzle.Plug(s, -1);
            Refresh(-1);
            return true;
        }

        /// <summary>The laser hit socket k: plug in the carried string, or pick up the last string plugged there.</summary>
        public bool HitSocket(int k, int hand, Vector3 where)
        {
            if (!_live || k < 0 || k >= _puzzle.AnswerCount) return false;
            int s = _carrying[hand];
            if (s < 0)
            {
                for (int t = _puzzle.StringCount - 1; t >= 0; t--)
                {
                    if (_puzzle.PluggedInto(t) == k) return GrabString(t, hand);
                }

                return false;
            }

            _carrying[hand] = -1;
            _puzzle.Plug(s, k);
            _plugs++;
            if (k == _puzzle.Wanted) AddScore(25, where);
            else if (Time.time - _lastWrong > wrongEvery)
            {
                _lastWrong = Time.time;
                Director.Say(RideLine.PlugWrong);
            }

            Plugged?.Invoke(s, k);
            Refresh(k);
            return true;
        }

        protected override IEnumerator Play()
        {
            Prepare();
            for (int s = 0; s < _puzzle.StringCount; s++) _puzzle.Plug(s, -1);
            _carrying[0] = _carrying[1] = -1;
            _plugs = 0;
            _hurry = false;
            _byRider = true;
            _live = true;
            LiveChanged?.Invoke(true);
            Refresh(-1);
            float opened = Time.time;
            float nextAuto = 0f;
            while (!_puzzle.Passed)
            {
                if ((_hurry || Time.time - opened > giveUpAfter) && Time.time >= nextAuto)
                {
                    _byRider = false;
                    nextAuto = Time.time + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                    int s = _puzzle.Hint();
                    if (s >= 0)
                    {
                        _carrying[0] = _carrying[1] = -1;
                        _puzzle.Plug(s, _puzzle.Wanted);
                        Plugged?.Invoke(s, _puzzle.Wanted);
                        Refresh(_puzzle.Wanted);
                    }
                }

                yield return null;
            }

            _carrying[0] = _carrying[1] = -1;
            _live = false;
            LiveChanged?.Invoke(false);
            Changed?.Invoke();
        }

        protected override int StarsEarned() => _plugs <= 4 ? 3 : _plugs <= 8 ? 2 : 1;

        protected override bool PlayedItself => !_byRider;

        private void Refresh(int socket)
        {
            Refresh();
            double gap = _puzzle.Gap;
            int guess = _puzzle.Guess;
            Progress = _puzzle.Passed ? 1f : Mathf.Clamp01(1f - (float)gap / 2.5f);
            SetGoal(_puzzle.Passed ? "Pixel says 3!" : "Pixel says " + guess + ". Make the 3 the brightest!");
            Changed?.Invoke();
        }
    }
}
