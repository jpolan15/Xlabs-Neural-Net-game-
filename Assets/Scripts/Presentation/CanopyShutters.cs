using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Canopy shutters start closed. The first verified repair swings them open.
    /// </summary>
    public class CanopyShutters : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private Transform leftShutter;
        [SerializeField] private Transform rightShutter;
        [SerializeField] private float openAngle = 70f;
        [SerializeField] private float speed = 25f;

        private bool _open;
        private Quaternion _leftClosed;
        private Quaternion _rightClosed;
        private bool _cached;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            CacheClosed();
        }

        private void OnEnable()
        {
            if (chamberController != null) chamberController.OnPuzzleSolved += HandleSolved;
        }

        private void OnDisable()
        {
            if (chamberController != null) chamberController.OnPuzzleSolved -= HandleSolved;
        }

        private void Update()
        {
            if (!_open) return;
            if (leftShutter != null)
            {
                leftShutter.localRotation = Quaternion.RotateTowards(leftShutter.localRotation, _leftClosed * Quaternion.Euler(0f, 0f, openAngle), speed * Time.deltaTime);
            }
            if (rightShutter != null)
            {
                rightShutter.localRotation = Quaternion.RotateTowards(rightShutter.localRotation, _rightClosed * Quaternion.Euler(0f, 0f, -openAngle), speed * Time.deltaTime);
            }
        }

        private void HandleSolved()
        {
            CacheClosed();
            _open = true;
        }

        private void CacheClosed()
        {
            if (_cached) return;
            if (leftShutter != null) _leftClosed = leftShutter.localRotation;
            if (rightShutter != null) _rightClosed = rightShutter.localRotation;
            _cached = leftShutter != null || rightShutter != null;
        }
    }
}
