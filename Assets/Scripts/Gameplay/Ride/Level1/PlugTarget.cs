using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>A collider on the plug board: a string's head or an answer socket. The laser hits it; the plug game decides.</summary>
    public sealed class PlugTarget : MonoBehaviour, IRideTarget, IRideReleaseTarget
    {
        [SerializeField] private PlugMoment game;
        [SerializeField] private bool socket;
        [SerializeField] private int index;

        public bool IsSocket => socket;
        public int Index => index;

        /// <summary>Used by the scene builder.</summary>
        public void Set(PlugMoment moment, bool isSocket, int i)
        {
            game = moment;
            socket = isSocket;
            index = i;
        }

        public bool Hit(int hand)
        {
            if (game == null) return false;
            return socket ? game.HitSocket(index, hand, transform.position) : game.GrabString(index, hand);
        }

        /// <summary>Trigger released over a socket while carrying a string: plug it in (grab, drag, let go).</summary>
        public bool Release(int hand) => socket && game != null && game.Carrying(hand) >= 0 && game.HitSocket(index, hand, transform.position);
    }
}
