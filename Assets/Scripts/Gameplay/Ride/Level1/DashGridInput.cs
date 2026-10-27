using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// The game table's 7×7 weight grid (Region 2, paint the weights). XR sends every press, and every cell a held laser
    /// sweeps onto, here with the hand that made it, and this hands it to the paint game while it is live. It decides
    /// nothing itself.
    /// </summary>
    public sealed class DashGridInput : MonoBehaviour
    {
        [SerializeField] private WeightPaintMoment paint;

        /// <summary>A press on a cell. Returns true when the live game used it.</summary>
        public bool Press(int cell, int hand) => Spray(cell, hand, true);

        /// <summary>A held laser on a cell: pressed there (a press), or swept onto it (paint without toggling).</summary>
        public bool Spray(int cell, int hand, bool pressed)
        {
            if (paint == null || !paint.IsLive) return false;
            return pressed ? paint.Press(cell, RideHand.Brush(hand)) : paint.Spray(cell, RideHand.Brush(hand));
        }

        /// <summary>True while the paint game is live.</summary>
        public bool IsLive => paint != null && paint.IsLive;
    }
}
