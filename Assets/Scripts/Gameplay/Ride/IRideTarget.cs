using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>Which hand did something. The right hand paints green and inks; the left paints red and erases.</summary>
    public static class RideHand
    {
        public const int Left = 0;
        public const int Right = 1;

        /// <summary>The brush a hand paints with: +1 (green) for the right, −1 (red) for the left.</summary>
        public static int Brush(int hand) => hand == Left ? -1 : 1;
    }

    /// <summary>
    /// Something the rider's laser can hit (a noise speck, a string, a socket, a step button). XR turns a trigger or
    /// A/X press while pointing at its collider into <see cref="Hit"/>; the target's own moment decides what it means.
    /// </summary>
    public interface IRideTarget
    {
        /// <summary>Returns true when the hit did something (XR then buzzes the hand).</summary>
        bool Hit(int hand);
    }

    /// <summary>A target that also reacts when the trigger is let go over it (grab a string, drag it, let go on a socket).</summary>
    public interface IRideReleaseTarget
    {
        bool Release(int hand);
    }

    /// <summary>
    /// A hand's fingertip: a small trigger that follows the controller, so a moment or view can tell a hand reached it
    /// (a poke on a grid cell, a rub on Pixel) without knowing about XR. XR implements it; it decides nothing.
    /// </summary>
    public interface IHandTip
    {
        /// <summary><see cref="RideHand.Left"/> or <see cref="RideHand.Right"/>.</summary>
        int Hand { get; }

        /// <summary>The fingertip's world position.</summary>
        Vector3 Position { get; }
    }

    /// <summary>
    /// Where each hand's laser is pointing this frame, written by XR (or the desk mouse) and read by moments and views
    /// that need it (a string being carried follows the laser). No XR types.
    /// </summary>
    public static class RideAim
    {
        private static readonly Vector3[] Points = new Vector3[2];
        private static readonly Vector3[] Origins = new Vector3[2];
        private static readonly bool[] Valid = new bool[2];

        /// <summary>A hand fired its laser (hit says whether it did something).</summary>
        public static event System.Action<int, bool> Fired;

        /// <summary>A hand let go of its trigger or button (a wire being carried is dropped).</summary>
        public static event System.Action<int> Released;

        /// <summary>Something wants a hand to buzz: hand, amplitude 0 to 1, seconds. XR plays it on the controller.</summary>
        public static event System.Action<int, float, float> Buzzed;

        public static void RaiseFired(int hand, bool hit) => Fired?.Invoke(hand, hit);

        public static void RaiseReleased(int hand) => Released?.Invoke(hand);

        public static void Buzz(int hand, float amplitude, float seconds)
        {
            if (hand == RideHand.Left || hand == RideHand.Right) Buzzed?.Invoke(hand, amplitude, seconds);
        }

        public static void Set(int hand, Vector3 origin, Vector3 point, bool valid)
        {
            Origins[hand] = origin;
            Points[hand] = point;
            Valid[hand] = valid;
        }

        public static bool TryGet(int hand, out Vector3 origin, out Vector3 point)
        {
            origin = Origins[hand];
            point = Points[hand];
            return Valid[hand];
        }
    }
}
