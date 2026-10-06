using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Neural;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Session copies of networks the player already solved.
    /// </summary>
    public sealed class SavedCircuitLibrary : MonoBehaviour
    {
        readonly List<SavedCircuit> _circuits = new List<SavedCircuit>();

        public IReadOnlyList<SavedCircuit> Circuits => _circuits;

        public void Remember(string name, NetworkModel network)
        {
            if (string.IsNullOrWhiteSpace(name) || network == null) return;
            for (int i = 0; i < _circuits.Count; i++)
            {
                if (_circuits[i].Name == name)
                {
                    _circuits[i] = new SavedCircuit(name, network.DeepCopy());
                    return;
                }
            }

            _circuits.Add(new SavedCircuit(name, network.DeepCopy()));
        }

        public NetworkModel Find(string name)
        {
            for (int i = 0; i < _circuits.Count; i++)
            {
                if (_circuits[i].Name == name) return _circuits[i].Network.DeepCopy();
            }

            return null;
        }
    }

    /// <summary>One named copy of a solved network.</summary>
    public sealed class SavedCircuit
    {
        public SavedCircuit(string name, NetworkModel network)
        {
            Name = name;
            Network = network;
        }

        public string Name { get; }
        public NetworkModel Network { get; }
    }
}
