using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// The pod's dash controls, looked up by id so a station prefab never holds a reference into the pod prefab.
    /// </summary>
    public sealed class RideControlSet : MonoBehaviour
    {
        [SerializeField] private RideControlChannel[] channels = new RideControlChannel[0];

        /// <summary>Every channel on the dash.</summary>
        public IReadOnlyList<RideControlChannel> Channels => channels;

        /// <summary>Returns the channel with this id, or null.</summary>
        public RideControlChannel Get(string id)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i] != null && channels[i].Id == id) return channels[i];
            }

            return null;
        }
    }
}
