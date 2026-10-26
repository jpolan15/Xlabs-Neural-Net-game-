using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// The dash's 7×7 grid is shared by two games: draw the 3 (Region 1) and paint the weights (Region 2). XR sends every
    /// press here with the hand that made it, and this hands it to whichever game is live. It decides nothing itself.
    /// </summary>
    public sealed class DashGridInput : MonoBehaviour
    {
        [SerializeField] private DrawMoment draw;
        [SerializeField] private WeightPaintMoment paint;

        /// <summary>A press on a cell. Returns true when a live game used it.</summary>
        public bool Press(int cell, int hand)
        {
            if (draw != null && draw.IsLive) return draw.Press(cell, hand);
            if (paint != null && paint.IsLive) return paint.Press(cell, RideHand.Brush(hand));
            return false;
        }

        /// <summary>True while one of the grid games is live.</summary>
        public bool IsLive => (draw != null && draw.IsLive) || (paint != null && paint.IsLive);
    }
}
