using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>Spins a child around its own axis. Used for the drone's rotors.</summary>
    public sealed class SpinBehaviour : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 900f, 0f);

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
