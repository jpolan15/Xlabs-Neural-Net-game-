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
    /// A mini-game, the hot-wire hunt: the rider grabs the hottest wires with a laser (trigger or A) and drags each one's
    /// loose end to the answer "3" before they cool down. The wire snaps onto the 3 once its end comes within a few
    /// degrees of it, and that is one real gradient step on that weight (w ← w − η ∂loss/∂w); a hotter wire scores more.
    /// Letting go anywhere else drops the wire back where it was. When the hunt's time runs out the ride connects the
    /// rest itself, one at a time, so nobody gets stuck. Stars come from speed.
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
        [Tooltip("How many of the brightest wires the rider is asked to connect.")]
        [SerializeField] private int topCount = 4;
        [Tooltip("Seconds of the hunt: the wires cool down over this time.")]
        [SerializeField] private float timeout = 40f;
        [SerializeField] private float autoStepSeconds = 1.2f;
        [Tooltip("The answer node the wires are dragged to (the 3). Set by the scene builder.")]
        [SerializeField] private Transform socket;
        [Tooltip("A dragged wire's end snaps onto the node when it comes within this many degrees of it, seen from the hand.")]
        [SerializeField] private float snapDegrees = 4f;
        [Tooltip("...but never closer than this many metres, so a near view is as forgiving as a far one.")]
        [SerializeField] private float snapMinRadius = 0.8f;

        private List<Wire> _wires;
        private bool[] _nudged;
        private NetworkModel _net;
        private bool _live;
        private bool _hurry;
        private bool _byRider = true;
        private float _opened;
        private float _lastHit;
        private bool _saidHurry;
        private int _connections;
        private readonly int[] _carrying = { -1, -1 };

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

        /// <summary>Top wire k was nudged: its weight before and after.</summary>
        public event Action<int, double, double> Nudged;

        public event Action<bool> LiveChanged;

        /// <summary>A hand picked up top wire k (wire, hand).</summary>
        public event Action<int, int> Grabbed;

        /// <summary>A hand let go of top wire k without connecting it (wire, hand).</summary>
        public event Action<int, int> Dropped;

        /// <summary>Top wire k snapped onto the 3: the wire, the hand (-1: the ride did it), and how many came before it.</summary>
        public event Action<int, int, int> Connected;

        /// <summary>The 3 the wires are dragged to.</summary>
        public Transform Socket => socket;

        /// <summary>The top wire a hand is carrying, or -1.</summary>
        public int Carrying(int hand) => hand == RideHand.Left || hand == RideHand.Right ? _carrying[hand] : -1;

        /// <summary>How many top wires are connected to the 3 so far.</summary>
        public int ConnectedCount => Count();

        /// <summary>How close to the 3 a dragged wire's end must come to snap, seen from the eye: a few degrees, at least a minimum.</summary>
        public float SnapRadiusFrom(Vector3 eye)
        {
            float distance = socket != null ? Vector3.Distance(eye, socket.position) : 0f;
            return Mathf.Max(snapMinRadius, distance * Mathf.Tan(snapDegrees * Mathf.Deg2Rad));
        }

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

        /// <summary>The blame changed (the rider drew their digit): views re-aim the hunt's wires.</summary>
        public event Action BlameChanged;

        /// <summary>Computes the blame so views can draw it before the moment opens.</summary>
        public void Prepare()
        {
            if (_wires != null || source == null) return;
            _net = session != null ? session.Network.ToModel() : source.Data.CreateUntrained();
            _wires = ComputeBlame(_net, Digit, out double loss);
            StartLoss = loss;
            HeroLoss = loss;
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

        /// <summary>Connects top wire k to the 3 at once: one real gradient step on it. The desk and the tests use this.</summary>
        public void Nudge(int k) => Connect(k, -1, true);

        /// <summary>
        /// A hand picks up top wire k (a laser press on the wire or its glowing end). The wire's end then follows the laser
        /// until it snaps onto the 3 or the hand lets go. Returns true when the wire was picked up.
        /// </summary>
        public bool Grab(int k, int hand)
        {
            if (!_live || _nudged == null || k < 0 || k >= _nudged.Length || _nudged[k]) return false;
            if (hand != RideHand.Left && hand != RideHand.Right) hand = RideHand.Right;
            int other = 1 - hand;
            if (_carrying[other] == k) Drop(other);
            if (_carrying[hand] == k) return true;
            if (_carrying[hand] >= 0) Drop(hand);
            _carrying[hand] = k;
            Grabbed?.Invoke(k, hand);
            return true;
        }

        /// <summary>A hand lets go of the wire it carries; the wire goes back to where it hung.</summary>
        public void Drop(int hand)
        {
            if (hand != RideHand.Left && hand != RideHand.Right) return;
            int k = _carrying[hand];
            if (k < 0) return;
            _carrying[hand] = -1;
            Dropped?.Invoke(k, hand);
        }

        /// <summary>
        /// Where the wire a hand carries ends this frame: the hand's laser projected onto the plane through the 3, square to
        /// the line of sight, so the end slides over the network as the rider sweeps the laser. False when the hand has no laser.
        /// </summary>
        public bool TryGetCursor(int hand, out Vector3 point) => TryGetCursor(hand, out point, out _);

        private bool TryGetCursor(int hand, out Vector3 point, out Vector3 eye)
        {
            point = default;
            eye = default;
            if (socket == null || !RideAim.TryGet(hand, out Vector3 origin, out Vector3 aim)) return false;
            Vector3 direction = aim - origin;
            if (direction.sqrMagnitude < 1e-8f) return false;
            direction.Normalize();
            Vector3 centre = socket.position;
            Vector3 toEye = origin - centre;
            if (toEye.sqrMagnitude < 1e-8f) return false;
            eye = origin;
            var plane = new Plane(toEye.normalized, centre);
            var ray = new Ray(origin, direction);
            // Aiming away from the board: keep the end at the 3's distance along the laser, so it never jumps.
            point = plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : ray.GetPoint(toEye.magnitude);
            return true;
        }

        private void OnEnable() => RideAim.Released += OnReleased;

        private void OnDisable()
        {
            RideAim.Released -= OnReleased;
            _carrying[0] = _carrying[1] = -1;
        }

        private void OnReleased(int hand) => Drop(hand);

        private void Update()
        {
            if (!_live || socket == null) return;
            for (int hand = 0; hand < _carrying.Length; hand++)
            {
                int k = _carrying[hand];
                if (k < 0 || !TryGetCursor(hand, out Vector3 cursor, out Vector3 eye)) continue;
                if (Vector3.Distance(cursor, socket.position) <= SnapRadiusFrom(eye)) Connect(k, hand, true);
            }
        }

        /// <summary>
        /// Top wire k snaps onto the 3: one real gradient step on its weight, and points for the rider when the rider did it
        /// (the hotter the wire, the more it scores).
        /// </summary>
        private void Connect(int k, int hand, bool byRider)
        {
            if (!_live || IsNudged(k)) return;
            for (int h = 0; h < _carrying.Length; h++) if (_carrying[h] == k) _carrying[h] = -1;
            Apply(k);
            if (byRider)
            {
                _lastHit = Time.time;
                AddScore(10 + 10 * (_nudged.Length - k), Vector3.zero);
            }

            Connected?.Invoke(k, hand, _connections++);
        }

        /// <summary>Seconds left in the hunt.</summary>
        public override float SecondsLeft => _live ? Mathf.Max(0f, timeout - (Time.time - _opened)) : -1f;

        /// <summary>How hot the hunt's wires still are: 1 when it opens, 0 when its time is up.</summary>
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
            // The rider's network as it is now (nothing has trained it yet), and their digit.
            _wires = null;
            Prepare();
            _hurry = false;
            _byRider = true;
            _saidHurry = false;
            _connections = 0;
            _carrying[0] = _carrying[1] = -1;
            _live = true;
            LiveChanged?.Invoke(true);
            _opened = Time.time;
            _lastHit = _opened;
            float opened = _opened;
            float nextAuto = 0f;
            while (Count() < _nudged.Length)
            {
                Progress = (float)Count() / _nudged.Length;
                SetGoal("Wires on the 3: " + Count() + " of " + _nudged.Length);
                if (!_saidHurry && Time.time - opened > timeout * 0.6f && Count() * 2 < _nudged.Length)
                {
                    _saidHurry = true;
                    Director.Say(RideLine.BackHurry);
                }

                if ((_hurry || Time.time - opened > timeout) && Time.time >= nextAuto)
                {
                    if (Count() * 2 < _nudged.Length) _byRider = false;
                    nextAuto = Time.time + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                    for (int k = 0; k < _nudged.Length; k++)
                    {
                        if (_nudged[k]) continue;
                        Connect(k, -1, false);
                        break;
                    }
                }

                yield return null;
            }

            _live = false;
            _carrying[0] = _carrying[1] = -1;
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
            HeroLoss = LossFunctions.CategoricalCrossEntropy(_net.Forward(Digit), Level1Data.HeroDigit);
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
