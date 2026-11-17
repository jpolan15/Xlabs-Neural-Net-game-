using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// One earned topic card on the cart's card rail (2026-11-17 playtest: "when you beat a game, a card pops onto your
    /// cart with more to read"). The laser hits it to open it big again; a slot that has not been earned yet ignores hits.
    /// It decides nothing about the ride: the celebration view listens to <see cref="Opened"/> and shows the card.
    /// </summary>
    public sealed class TopicCardSlot : MonoBehaviour, IRideTarget
    {
        [SerializeField] private int index;

        /// <summary>Which card (0 Pixels, 1 Neuron, 2 Network, 3 Backprop, 4 Training).</summary>
        public int Index => index;

        /// <summary>True once its game was won.</summary>
        public bool Earned { get; set; }

        /// <summary>The rider shot this card to read it.</summary>
        public event Action<int> Opened;

        public bool Hit(int hand)
        {
            if (!Earned) return false;
            Opened?.Invoke(index);
            return true;
        }
    }
}
