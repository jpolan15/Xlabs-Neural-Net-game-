using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// A doorway that blocks the collider until <see cref="Unlock"/> runs.
    /// </summary>
    public class ShipDoor : MonoBehaviour
    {
        [SerializeField] private string doorId = "SensorBay";
        [SerializeField] private Collider blocker;
        [SerializeField] private Transform panel;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 2.2f, 0f);

        private bool _unlocked;
        private Vector3 _closed;

        public string DoorId => doorId;
        public bool IsUnlocked => _unlocked;

        private void Awake()
        {
            if (panel != null) _closed = panel.localPosition;
        }

        private void Update()
        {
            if (!_unlocked || panel == null) return;
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, _closed + openLocalOffset, 1.4f * Time.deltaTime);
        }

        /// <summary>Opens the door. Repeated calls do nothing.</summary>
        public void Unlock()
        {
            if (_unlocked) return;
            _unlocked = true;
            if (blocker != null) blocker.enabled = false;
        }
    }
}
