using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Brightens a doorway light once gameplay has unlocked that door.
    /// </summary>
    public class DoorUnlockBeacon : MonoBehaviour
    {
        [SerializeField] private ShipDoor door;
        [SerializeField] private Light beacon;
        [SerializeField] private float lockedIntensity = 0.08f;
        [SerializeField] private float unlockedIntensity = 4.2f;

        private void Update()
        {
            if (beacon == null || door == null) return;
            float target = door.IsUnlocked ? unlockedIntensity : lockedIntensity;
            beacon.intensity = Mathf.MoveTowards(beacon.intensity, target, 6f * Time.deltaTime);
        }
    }
}
