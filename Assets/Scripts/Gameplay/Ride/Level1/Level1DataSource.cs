using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>Holds the baked Level 1 data asset and parses it once for every moment and view that reads it.</summary>
    public sealed class Level1DataSource : MonoBehaviour
    {
        [SerializeField] private TextAsset data;

        private Level1Data _parsed;

        /// <summary>The parsed bake. Throws with the asset path if it is missing.</summary>
        public Level1Data Data
        {
            get
            {
                if (_parsed != null) return _parsed;
                if (data == null) throw new System.InvalidOperationException("Level1DataSource has no data; expected " + Level1Data.AssetPath);
                _parsed = Level1Data.Parse(data.text);
                return _parsed;
            }
        }
    }
}
