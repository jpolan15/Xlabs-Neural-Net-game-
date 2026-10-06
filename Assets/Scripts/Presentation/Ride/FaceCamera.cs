using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Turns a flat glow card to face the viewer so it reads as a soft light from any side. Rotates only on Y.
    /// </summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        private Transform _camera;

        private void LateUpdate()
        {
            if (_camera == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                _camera = main.transform;
            }

            Vector3 flat = transform.position - _camera.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(flat);
        }
    }
}
