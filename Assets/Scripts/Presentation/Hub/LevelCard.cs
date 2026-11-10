using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Hub
{
    /// <summary>
    /// One level's card in the hub: a laser target (or a desk click) that hands the shot to the hub with the card's index. The hub
    /// builds it, draws it and decides what a shot does.
    /// </summary>
    public sealed class LevelCard : MonoBehaviour, IRideTarget
    {
        public LevelHubView Hub;
        public int Index;

        public bool Hit(int hand) => Hub != null && Hub.Choose(Index, hand);
    }
}
