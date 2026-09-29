using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Moves the surrounding field closer over time. There is no fail state and no purge.
    /// </summary>
    public class AsteroidDrift : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private float secondsToClose = 180f;
        [SerializeField] private Transform asteroidField;

        /// <summary>0 is the starting distance. 1 is as close as the drift goes.</summary>
        public float Closeness { get; private set; }

        /// <summary>Raised when closeness changes. Payload is the new 0-1 value.</summary>
        public event System.Action<float> OnDriftChanged;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        private void Update()
        {
            if (chamberController != null && chamberController.Phase == ChamberPhase.Arrival) return;

            float next = Mathf.Clamp01(Closeness + Time.deltaTime / Mathf.Max(1f, secondsToClose));
            if (Mathf.Abs(next - Closeness) < 0.0001f) return;
            Closeness = next;
            if (asteroidField != null)
            {
                asteroidField.localScale = Vector3.one * Mathf.Lerp(1f, 0.42f, Closeness);
            }
            OnDriftChanged?.Invoke(Closeness);
        }

        /// <summary>Builder and tests assign the field that should close in.</summary>
        public void SetField(Transform field)
        {
            asteroidField = field;
        }
    }
}
