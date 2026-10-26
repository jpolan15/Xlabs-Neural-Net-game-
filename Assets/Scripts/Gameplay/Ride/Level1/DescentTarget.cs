using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>One of the descent game's step buttons on the dash. The laser hits it; the game takes the step.</summary>
    public sealed class DescentTarget : MonoBehaviour, IRideTarget
    {
        [SerializeField] private DescentMoment game;
        [SerializeField] private int button;

        public int Button => button;

        /// <summary>Used by the scene builder.</summary>
        public void Set(DescentMoment moment, int index)
        {
            game = moment;
            button = index;
        }

        public bool Hit(int hand) => game != null && game.Press(button, transform.position);
    }
}
