using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// A button on the game table the laser can shoot (DONE, CLEAR, a step size, the card with the rider's digit). It
    /// passes the press to whichever of its games is live (DONE serves both drawing games), and the release of the hand
    /// that pressed it to the same game, so a hold button stops when the trigger comes up. The game decides.
    /// </summary>
    public sealed class GameButton : MonoBehaviour, IRideTarget
    {
        [SerializeField] private MonoBehaviour[] games = new MonoBehaviour[0];
        [SerializeField] private int button;

        private readonly IButtonGame[] _held = new IButtonGame[2];

        public int Button => button;

        /// <summary>The live game this button serves, or null.</summary>
        public IButtonGame Live
        {
            get
            {
                foreach (MonoBehaviour m in games)
                {
                    if (m is IButtonGame g && g.IsLive) return g;
                }

                return null;
            }
        }

        public bool Hit(int hand)
        {
            IButtonGame g = Live;
            if (g == null || !g.Press(button, hand)) return false;
            if (hand == RideHand.Left || hand == RideHand.Right) _held[hand] = g;
            return true;
        }

        private void OnEnable() => RideAim.Released += OnReleased;

        private void OnDisable()
        {
            RideAim.Released -= OnReleased;
            for (int h = 0; h < _held.Length; h++) OnReleased(h);
        }

        private void OnReleased(int hand)
        {
            if (hand < 0 || hand >= _held.Length || _held[hand] == null) return;
            IButtonGame g = _held[hand];
            _held[hand] = null;
            g.Release(button, hand);
        }
    }
}
