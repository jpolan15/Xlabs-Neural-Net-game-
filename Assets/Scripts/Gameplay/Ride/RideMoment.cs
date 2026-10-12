using System;
using System.Collections;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// Something that happens at a stop and must finish before the ride moves on: the neuron puzzle, the forward pass,
    /// the blame, the Valley drop. Every moment has a path to its end that needs nobody (a timeout or an operator
    /// skip), so no rider can get stuck. A moment says its own lines through the director and owns its outcome; the
    /// director only waits for it.
    /// </summary>
    public abstract class RideMoment : MonoBehaviour
    {
        /// <summary>The moment began (views wake up).</summary>
        public event Action Started;

        /// <summary>The moment ended.</summary>
        public event Action Finished;

        public bool IsRunning { get; private set; }
        public bool HasFinished { get; private set; }

        protected RideDirector Director { get; private set; }

        /// <summary>Runs the moment to its end. Called by the director.</summary>
        public IEnumerator RunMoment(RideDirector director)
        {
            Director = director;
            IsRunning = true;
            HasFinished = false;
            Started?.Invoke();
            yield return Run();
            IsRunning = false;
            HasFinished = true;
            Finished?.Invoke();
        }

        protected abstract IEnumerator Run();

        /// <summary>The operator wants the ride to move on: finish quickly, still honestly.</summary>
        public virtual void Hurry()
        {
        }
    }
}
