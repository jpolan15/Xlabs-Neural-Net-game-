using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>A mini-game of Level 2: a <see cref="RideGame"/> that owns picks (see <see cref="PickTarget"/>).</summary>
    public abstract class Level2Game : RideGame, IPickOwner
    {
        public virtual bool PickHit(int index, int hand) => false;

        public virtual bool PickRelease(int index, int hand) => false;

        public virtual bool PickStrokeBegin(int index, int hand, Vector3 point, bool pressed) => false;

        public virtual void PickStrokeMove(int index, int hand, Vector3 point)
        {
        }

        public virtual void PickStrokeEnd(int index, int hand)
        {
        }

        /// <summary>The hand's laser as a ray, or false when it has none.</summary>
        protected static bool TryRay(int hand, out Ray ray)
        {
            ray = default;
            if (hand != RideHand.Left && hand != RideHand.Right) hand = RideHand.Right;
            if (!RideAim.TryGet(hand, out Vector3 origin, out Vector3 point)) return false;
            Vector3 d = point - origin;
            if (d.sqrMagnitude < 1e-8f) return false;
            ray = new Ray(origin, d.normalized);
            return true;
        }
    }
}
