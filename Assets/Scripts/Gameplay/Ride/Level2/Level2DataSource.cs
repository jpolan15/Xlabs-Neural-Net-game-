using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>Holds the baked Level 2 data asset and parses it once for every moment and view that reads it.</summary>
    public sealed class Level2DataSource : MonoBehaviour
    {
        [SerializeField] private TextAsset data;

        private Level2Data _parsed;

        /// <summary>The parsed bake. Throws with the asset path if it is missing.</summary>
        public Level2Data Data
        {
            get
            {
                if (_parsed != null) return _parsed;
                if (data == null) throw new InvalidOperationException("Level2DataSource has no data; expected " + Level2Data.AssetPath);
                _parsed = Level2Data.Parse(data.text);
                return _parsed;
            }
        }

        /// <summary>Tests and tools hand the data in directly.</summary>
        public void Use(Level2Data parsed) => _parsed = parsed;
    }
}
