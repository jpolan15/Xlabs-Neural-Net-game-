using System;
using System.Collections;
using Convergence.Core.Neural;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Regions 3 and 7: the hero "3" goes through the real network, untrained or trained. Core computes every
    /// activation; this exposes them, runs the reveal clock the light follows (pixels, then the hidden layer, then the
    /// ten answers), and then lets its lines be said.
    /// </summary>
    public sealed class ForwardPassMoment : RideMoment
    {
        [SerializeField] private Level1DataSource source;
        [SerializeField] private bool trained;
        [SerializeField] private float raceSeconds = 6f;
        [SerializeField] private float holdAfter = 0.8f;
        [Tooltip("Said one after another once the answer is shown.")]
        [SerializeField] private RideLine[] revealLines = new RideLine[0];

        private double[] _hidden;
        private double[] _outputs;
        private bool _hurry;

        public bool Trained => trained;

        /// <summary>0 before the race, 1 when the light has reached the answers.</summary>
        public float Progress { get; private set; }

        /// <summary>Hidden activations from Core (16).</summary>
        public double[] Hidden => _hidden;

        /// <summary>Output probabilities from Core (10).</summary>
        public double[] Outputs => _outputs;

        /// <summary>The network's answer: the biggest output.</summary>
        public int Guess { get; private set; } = -1;

        public event Action RaceStarted;
        public event Action<int> Revealed;

        /// <summary>Runs Core's forward pass on the hero digit with this moment's weights.</summary>
        public void Compute()
        {
            Level1Data data = source.Data;
            NetworkModel net = trained ? data.CreateTrained() : data.CreateUntrained();
            _outputs = net.Forward(data.File.hero.pixels7);
            var neurons = net.Layers[0].Neurons;
            _hidden = new double[neurons.Count];
            for (int j = 0; j < neurons.Count; j++) _hidden[j] = neurons[j].LastOutput;
            int best = 0;
            for (int k = 1; k < _outputs.Length; k++) if (_outputs[k] > _outputs[best]) best = k;
            Guess = best;
        }

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Run()
        {
            _hurry = false;
            Compute();
            Progress = 0f;
            RaceStarted?.Invoke();
            while (Progress < 1f)
            {
                Progress = Mathf.Min(1f, Progress + Time.deltaTime / Mathf.Max(0.1f, _hurry ? raceSeconds * 0.25f : raceSeconds));
                yield return null;
            }

            Revealed?.Invoke(Guess);
            float held = 0f;
            while (held < holdAfter && !_hurry)
            {
                held += Time.deltaTime;
                yield return null;
            }

            foreach (RideLine line in revealLines)
            {
                float seconds = Director.Narrate(line);
                float t = 0f;
                while (t < seconds && !_hurry)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }
    }
}
