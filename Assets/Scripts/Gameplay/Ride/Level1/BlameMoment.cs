using System;
using System.Collections;
using System.Collections.Generic;
using Convergence.Core.Math;
using Convergence.Core.Neural;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 5: backpropagation. Core runs the rider's untrained network forward on the rider's own digit (v6; the hero 3
    /// if they never drew), measures the cross-entropy against the answer 3, and runs <see cref="NetworkModel.Backward"/>;
    /// every wire's blame is its real ∂loss/∂w, and every nudge is kept in the rider's network for the training game.
    /// A mini-game, "cool the hot wires" (2026-11-17 redesign: playtesters could not tell what the old drag-to-the-3 hunt
    /// wanted): the hottest wires glow, each with a big target on it, and a laser shot at one (trigger or A, as in the
    /// blaster) is one real gradient step on that weight (w ← w − η ∂loss/∂w). The wire cools, a hotter wire scores more,
    /// and the board shows how sure Pixel now is that the digit is a 3 (the real softmax output of the rider's network).
    /// When the hunt's time runs out the ride shoots the rest itself, one at a time, so nobody gets stuck. Stars come from
    /// speed.
    /// </summary>
    public sealed class BlameMoment : RideGame
    {
        /// <summary>One connection and its blame.</summary>
        public readonly struct Wire
        {
            /// <summary>0: pixel → hidden. 1: hidden → answer.</summary>
            public readonly int Layer;
            public readonly int Neuron;
            public readonly int Input;
            public readonly double Weight;
            public readonly double Gradient;

            public Wire(int layer, int neuron, int input, double weight, double gradient)
            {
                Layer = layer;
                Neuron = neuron;
                Input = input;
                Weight = weight;
                Gradient = gradient;
            }
        }

        [SerializeField] private Level1DataSource source;
        [Tooltip("v6: the blame is the rider's own digit's, on the rider's own network, and every nudge stays in it.")]
        [SerializeField] private Level1Session session;
        [Tooltip("How many of the hottest wires the rider is asked to cool.")]
        [SerializeField] private int topCount = 6;
        [Tooltip("Seconds of the hunt: after this the ride starts cooling the wires itself.")]
        [SerializeField] private float timeout = 75f;
        [SerializeField] private float autoStepSeconds = 1.2f;

        private List<Wire> _wires;
        private bool[] _nudged;
        private NetworkModel _net;
        private bool _live;
        private bool _hurry;
        private bool _byRider = true;
        private float _opened;
        private float _lastHit;
        private bool _saidHurry;
        private int _cooled;

        /// <summary>Every wire, the most to blame first.</summary>
        public IReadOnlyList<Wire> Wires => _wires;

        public int TopCount => topCount;
        public bool IsLive => _live;

        /// <summary>The learning rate of a nudge: the bake's own.</summary>
        public double LearningRate => source.Data.File.learningRate;

        /// <summary>The loss of the rider's digit (the hero 3 until they draw) with the weights as nudged so far.</summary>
        public double HeroLoss { get; private set; }

        /// <summary>The loss before any nudge.</summary>
        public double StartLoss { get; private set; }

        /// <summary>How sure the rider's network, as nudged so far, is that the rider's digit is a 3: its softmax output for 3.</summary>
        public double HeroProbability { get; private set; }

        /// <summary>How sure it was before any nudge.</summary>
        public double StartProbability { get; private set; }

        /// <summary>Top wire k was nudged: its weight before and after.</summary>
        public event Action<int, double, double> Nudged;

        public event Action<bool> LiveChanged;

        /// <summary>Top wire k was shot and cooled: the wire, the hand (-1: the ride did it), and how many cooled before it.</summary>
        public event Action<int, int, int> Connected;

        /// <summary>How many top wires are cooled so far.</summary>
        public int CooledCount => Count();

        /// <summary>The hottest top wire that is still hot, or -1.</summary>
        public int HottestLeft
        {
            get
            {
                if (_nudged == null) return -1;
                for (int k = 0; k < _nudged.Length; k++) if (!_nudged[k]) return k;
                return -1;
            }
        }

        public bool IsNudged(int k) => _nudged != null && k >= 0 && k < _nudged.Length && _nudged[k];

        /// <summary>The real blame of every wire for the hero digit, most to blame first. Deterministic.</summary>
        public static List<Wire> ComputeBlame(Level1Data data, out double loss)
        {
            NetworkModel net = data.CreateUntrained();
            return ComputeBlame(net, data.File.hero.pixels7, out loss, out _);
        }

        private static List<Wire> ComputeBlame(NetworkModel net, double[] pixels, out double loss, out double probability)
        {
            double[] p = net.Forward(pixels);
            loss = LossFunctions.CategoricalCrossEntropy(p, Level1Data.HeroDigit);
            probability = p[Level1Data.HeroDigit];
            var dLdp = new double[p.Length];
            LossFunctions.CategoricalCrossEntropyGradient(p, Level1Data.HeroDigit, dLdp);
            net.Backward(dLdp);
            var wires = new List<Wire>();
            for (int l = 0; l < net.Layers.Count; l++)
            {
                var neurons = net.Layers[l].Neurons;
                for (int n = 0; n < neurons.Count; n++)
                {
                    for (int i = 0; i < neurons[n].WeightCount; i++)
                    {
                        wires.Add(new Wire(l, n, i, neurons[n].GetWeight(i), neurons[n].GetWeightGradient(i)));
                    }
                }
            }

            // Biggest |gradient| first; ties by position, so the order never depends on the sort.
            wires.Sort((a, b) =>
            {
                int c = Math.Abs(b.Gradient).CompareTo(Math.Abs(a.Gradient));
                if (c != 0) return c;
                c = a.Layer.CompareTo(b.Layer);
                if (c != 0) return c;
                c = a.Neuron.CompareTo(b.Neuron);
                return c != 0 ? c : a.Input.CompareTo(b.Input);
            });
            return wires;
        }

        /// <summary>The blame changed (the rider drew their digit, or the hunt opened): views re-aim the hot wires.</summary>
        public event Action BlameChanged;

        /// <summary>Computes the blame so views can draw it before the moment opens.</summary>
        public void Prepare()
        {
            if (_wires != null || source == null) return;
            _net = session != null ? session.Network.ToModel() : source.Data.CreateUntrained();
            _wires = ComputeBlame(_net, Digit, out double loss, out double probability);
            StartLoss = loss;
            HeroLoss = loss;
            StartProbability = probability;
            HeroProbability = probability;
            _nudged = new bool[Mathf.Min(topCount, _wires.Count)];
            BlameChanged?.Invoke();
        }

        /// <summary>The digit the blame is for: the rider's (the hero 3 until they draw).</summary>
        private double[] Digit => session != null ? session.Digit : source.Data.File.hero.pixels7;

        private void Start()
        {
            if (session != null) session.DigitChanged += OnDigitChanged;
            Prepare();
        }

        private void OnDestroy()
        {
            if (session != null) session.DigitChanged -= OnDigitChanged;
        }

        private void OnDigitChanged()
        {
            if (_live) return;
            _wires = null;
            Prepare();
        }

        /// <summary>Cools top wire k at once, as if the rider shot it: one real gradient step. The desk and the tests use this.</summary>
        public void Nudge(int k) => Cool(k, -1, true);

        /// <summary>
        /// A hand's laser hit top wire k (its glowing target, or the wire itself): one real gradient step on its weight.
        /// Returns true when the shot did something (the hunt is live and the wire was still hot).
        /// </summary>
        public bool Shoot(int k, int hand)
        {
            if (!_live || _nudged == null || k < 0 || k >= _nudged.Length || _nudged[k]) return false;
            if (hand != RideHand.Left && hand != RideHand.Right) hand = RideHand.Right;
            Cool(k, hand, true);
            return true;
        }

        /// <summary>
        /// Top wire k cools: one real gradient step on its weight, and points for the rider when the rider did it (the
        /// hotter the wire, the more it scores; the "+N" bursts on the table, near enough to read).
        /// </summary>
        private void Cool(int k, int hand, bool byRider)
        {
            if (!_live || IsNudged(k)) return;
            Apply(k);
            if (byRider)
            {
                _lastHit = Time.time;
                AddScore(10 + 10 * (_nudged.Length - k), Vector3.zero);
            }

            Connected?.Invoke(k, hand, _cooled++);
        }

        /// <summary>Seconds left in the hunt.</summary>
        public override float SecondsLeft => _live ? Mathf.Max(0f, timeout - (Time.time - _opened)) : -1f;

        /// <summary>How much of the hunt's time is left: 1 when it opens, 0 when its time is up.</summary>
        public float Heat => _live ? Mathf.Clamp01(1f - (Time.time - _opened) / timeout) : 0f;

        protected override int StarsEarned()
        {
            float took = _lastHit - _opened;
            return took <= timeout * 0.45f ? 3 : took <= timeout * 0.75f ? 2 : 1;
        }

        protected override bool PlayedItself => !_byRider;

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Play()
        {
            // The rider's network as it is now (nothing has trained it yet), and their digit. Prepare raises BlameChanged, so
            // the views re-aim the hot wires and the board's meter starts from this digit's real "how sure".
            _wires = null;
            Prepare();
            _hurry = false;
            _byRider = true;
            _saidHurry = false;
            _cooled = 0;
            _live = true;
            LiveChanged?.Invoke(true);
            _opened = Time.time;
            _lastHit = _opened;
            float opened = _opened;
            float nextAuto = 0f;
            while (Count() < _nudged.Length)
            {
                Progress = (float)Count() / _nudged.Length;
                SetGoal("Shoot the glowing balls:  " + Count() + " of " + _nudged.Length);
                if (!_saidHurry && Time.time - opened > timeout * 0.6f && Count() * 2 < _nudged.Length)
                {
                    _saidHurry = true;
                    Director.Say(RideLine.BackHurry);
                }

                if ((_hurry || Time.time - opened > timeout) && Time.time >= nextAuto)
                {
                    if (Count() * 2 < _nudged.Length) _byRider = false;
                    nextAuto = Time.time + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                    int k = HottestLeft;
                    if (k >= 0) Cool(k, -1, false);
                }

                yield return null;
            }

            _live = false;
            LiveChanged?.Invoke(false);
        }

        private void Apply(int k)
        {
            if (_nudged == null || k < 0 || k >= _nudged.Length || _nudged[k]) return;
            Wire w = _wires[k];
            NeuronModel neuron = _net.Layers[w.Layer].Neurons[w.Neuron];
            double before = neuron.GetWeight(w.Input);
            double after = before - LearningRate * w.Gradient;
            neuron.SetWeight(w.Input, after);
            _nudged[k] = true;
            double[] p = _net.Forward(Digit);
            HeroLoss = LossFunctions.CategoricalCrossEntropy(p, Level1Data.HeroDigit);
            HeroProbability = p[Level1Data.HeroDigit];
            if (session != null)
            {
                session.Network.CopyFrom(_net);
                session.NotifyWeightsChanged();
            }

            Nudged?.Invoke(k, before, after);
        }

        private int Count()
        {
            int n = 0;
            for (int k = 0; k < _nudged.Length; k++) if (_nudged[k]) n++;
            return n;
        }
    }
}
