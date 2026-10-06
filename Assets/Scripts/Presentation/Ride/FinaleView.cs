using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The ending: when the ride completes, a panel with one check mark per stop pops up in front of the pod.
    /// </summary>
    public sealed class FinaleView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject toBeContinued;
        [SerializeField] private float popSeconds = 0.6f;
        [SerializeField] private float continuedDelay = 2.5f;

        private float _age = -1f;

        private void OnEnable()
        {
            if (panel != null) panel.SetActive(false);
            if (toBeContinued != null) toBeContinued.SetActive(false);
            if (director != null) director.RideCompleted += OnCompleted;
        }

        private void OnDisable()
        {
            if (director != null) director.RideCompleted -= OnCompleted;
        }

        private void OnCompleted()
        {
            _age = 0f;
            panel.transform.localScale = Vector3.zero;
            panel.SetActive(true);
        }

        private void Update()
        {
            if (_age < 0f) return;
            _age += Time.deltaTime;
            if (toBeContinued != null && !toBeContinued.activeSelf && _age >= continuedDelay) toBeContinued.SetActive(true);
            if (_age > popSeconds) return;
            float t = Mathf.Clamp01(_age / popSeconds);
            float overshoot = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            panel.transform.localScale = Vector3.one * (t * overshoot);
        }
    }
}
