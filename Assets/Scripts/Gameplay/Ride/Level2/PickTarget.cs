using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// A laser target that hands every event to a <see cref="IPickOwner"/> with its index. Level 2's words, gates, towers and buttons
    /// each wear one on their collider. It takes a stroke only when the owner does, so a plain press still fires as a shot (and an
    /// aim-helped near miss still counts).
    /// </summary>
    public sealed class PickTarget : MonoBehaviour, IRideTarget, IRideReleaseTarget, IRideStrokeTarget
    {
        public IPickOwner Owner;
        public int Index;

        public bool Hit(int hand) => Owner != null && Owner.PickHit(Index, hand);

        public bool Release(int hand) => Owner != null && Owner.PickRelease(Index, hand);

        public bool StrokeBegin(int hand, Vector3 point, bool pressed) => Owner != null && Owner.PickStrokeBegin(Index, hand, point, pressed);

        public void StrokeMove(int hand, Vector3 point) => Owner?.PickStrokeMove(Index, hand, point);

        public void StrokeEnd(int hand) => Owner?.PickStrokeEnd(Index, hand);
    }
}
