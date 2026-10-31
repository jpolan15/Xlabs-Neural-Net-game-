using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// A request for a sound from a moment or a view. Gameplay says what happened ("tick", "solved", "wrong"); the ride's
    /// effects view (Presentation) plays it. No sound is played here, so the games run without audio.
    /// </summary>
    public static class RideSounds
    {
        public static event Action<string, float> Requested;

        public static void Play(string cue, float pitch = 1f) => Requested?.Invoke(cue, pitch);
    }

    /// <summary>
    /// Something a laser can pick: a word, a gate, a tower, a button. <see cref="PickTarget"/> forwards every laser event on
    /// its collider to the owner with the pick's index, so one moment runs many picks without a component per pick.
    /// </summary>
    public interface IPickOwner
    {
        /// <summary>A press landed on pick <paramref name="index"/>. True when it did something (the hand buzzes).</summary>
        bool PickHit(int index, int hand);

        /// <summary>The trigger was let go over pick <paramref name="index"/>.</summary>
        bool PickRelease(int index, int hand);

        /// <summary>A held laser began a stroke on the pick (pressed: the trigger went down right here). True takes the stroke.</summary>
        bool PickStrokeBegin(int index, int hand, Vector3 point, bool pressed);

        void PickStrokeMove(int index, int hand, Vector3 point);

        void PickStrokeEnd(int index, int hand);
    }
}
