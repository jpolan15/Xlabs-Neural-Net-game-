using System;
using System.Collections;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 6: gradient descent as a coaster the game drives. The track through the Valley is the real training
    /// path and its height is the real loss (the builder lays it from the bake). Each hop moves the pod to the weights
    /// after the next group of real steps: hop 1 is the single too-big step that overshoots up the far slope, then
    /// the steps settle into the valley. The pod pauses at every hop, like a step.
    /// </summary>
    public sealed class ValleyRunMoment : RideMoment
    {
        [SerializeField] private Level1DataSource source;
        [Tooltip("Distance along the current leg of every hop, hop 0 (the rim) first. Written by the builder from the bake.")]
        [SerializeField] private float[] hopDistances = new float[0];
        [SerializeField] private float hopSeconds = 2.2f;
        [SerializeField] private float pauseSeconds = 0.45f;

        private bool _hurry;

        public int HopCount => hopDistances.Length;

        /// <summary>The hop the pod has reached (0 at the rim).</summary>
        public int CurrentHop { get; private set; }

        public float HopDistance(int k) => hopDistances[k];

        /// <summary>The bake's record of hop k.</summary>
        public Level1Hop Hop(int k) => source.Data.File.valley.hops[k];

        /// <summary>The pod sets off for hop k.</summary>
        public event Action<int> HopStarted;

        /// <summary>The pod reached hop k: the weights after its real steps, at its real loss.</summary>
        public event Action<int> HopReached;

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Run()
        {
            _hurry = false;
            CurrentHop = 0;
            HopReached?.Invoke(0);
            for (int k = 1; k < hopDistances.Length; k++)
            {
                HopStarted?.Invoke(k);
                float seconds = hopSeconds * (Director.GentleMode ? 1.6f : 1f) * (_hurry ? 0.35f : 1f);
                if (k == 1) seconds *= 1.4f; // the big step is a big move
                yield return Director.TravelTo(hopDistances[k], seconds);
                CurrentHop = k;
                HopReached?.Invoke(k);
                if (k == 1)
                {
                    Director.Narrate(RideLine.ValleyOvershoot);
                    if (!_hurry) yield return Director.WaitForSpeech();
                }
                else if (k == 2)
                {
                    Director.Narrate(RideLine.ValleySmallerSteps);
                }

                float held = 0f;
                while (held < pauseSeconds && !_hurry)
                {
                    held += Time.deltaTime;
                    yield return null;
                }
            }
        }
    }
}
