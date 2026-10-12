using System;
using System.Collections;
using System.Collections.Generic;
using Convergence.Core.Math;
using Convergence.Core.Neural;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 5: backpropagation. Core runs the untrained network forward on the hero "3", measures the cross-entropy
    /// against the answer 3, and runs <see cref="NetworkModel.Backward"/>; every wire's blame is its real ∂loss/∂w.
    /// The rider points at the brightest wires; each nudge is one real gradient step on that weight
    /// (w ← w − η ∂loss/∂w). The moment ends when the top wires are nudged, or after a timeout, when the ride nudges the
    /// rest itself, one at a time, so nobody gets stuck.
    /// </summary>
    public sealed class BlameMoment : RideMoment
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
        [Tooltip("How many of the brightest wires the rider is asked to nudge.")]
        [SerializeField] private int topCount = 6;
        [SerializeField] private float timeout = 25f;
        [SerializeField] private float autoStepSeconds = 1.2f;

        private List<Wire> _wires;
        private bool[] _nudged;
        private NetworkModel _net;
        private bool _live;
        private bool _hurry;

        /// <summary>Every wire, the most to blame first.</summary>
        public IReadOnlyList<Wire> Wires => _wires;

        public int TopCount => topCount;
        public bool IsLive => _live;

        /// <summary>The learning rate of a nudge: the bake's own.</summary>
        public double LearningRate => source.Data.File.learningRate;

        /// <summary>The loss of the hero "3" with the weights as nudged so far.</summary>
        public double HeroLoss { get; private set; }

        /// <summary>The loss before any nudge.</summary>
        public double StartLoss { get; private set; }

        /// <summary>Top wire k was nudged: its weight before and after.</summary>
        public event Action<int, double, double> Nudged;

        public event Action<bool> LiveChanged;

        public bool IsNudged(int k) => _nudged != null && k >= 0 && k < _nudged.Length && _nudged[k];

        /// <summary>The real blame of every wire for the hero digit, most to blame first. Deterministic.</summary>
        public static List<Wire> ComputeBlame(Level1Data data, out double loss)
        {
            NetworkModel net = data.CreateUntrained();
            return ComputeBlame(net, data.File.hero.pixels7, out loss);
        }

        private static List<Wire> ComputeBlame(NetworkModel net, double[] pixels, out double loss)
        {
            double[] p = net.Forward(pixels);
            loss = LossFunctions.CategoricalCrossEntropy(p, Level1Data.HeroDigit);
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

        /// <summary>Computes the blame so views can draw it before the moment opens.</summary>
        public void Prepare()
        {
            if (_wires != null || source == null) return;
            _net = source.Data.CreateUntrained();
            _wires = ComputeBlame(_net, source.Data.File.hero.pixels7, out double loss);
            StartLoss = loss;
            HeroLoss = loss;
            _nudged = new bool[Mathf.Min(topCount, _wires.Count)];
        }

        private void Start() => Prepare();

        /// <summary>The rider pointed at top wire k: one real gradient step on it.</summary>
        public void Nudge(int k)
        {
            if (!_live) return;
            Apply(k);
        }

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Run()
        {
            Prepare();
            _hurry = false;
            _live = true;
            LiveChanged?.Invoke(true);
            float opened = Time.time;
            float nextAuto = 0f;
            while (Count() < _nudged.Length)
            {
                if ((_hurry || Time.time - opened > timeout) && Time.time >= nextAuto)
                {
                    nextAuto = Time.time + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                    for (int k = 0; k < _nudged.Length; k++)
                    {
                        if (_nudged[k]) continue;
                        Apply(k);
                        break;
                    }
                }

                yield return null;
            }

            _live = false;
            LiveChanged?.Invoke(false);
            Director.Narrate(RideLine.RewindWiresKnow);
            yield return Director.WaitForSpeech();
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
            HeroLoss = LossFunctions.CategoricalCrossEntropy(_net.Forward(source.Data.File.hero.pixels7), Level1Data.HeroDigit);
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
