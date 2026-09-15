using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Dual concentric orbital gyroscopic rings that rotate smoothly around the central Convergence Core.
    /// Accelerates spin during forward pass signal transmission and awakened state.
    /// </summary>
    public class GyroscopicRings : MonoBehaviour
    {
        [Header("State Listener")]
        [SerializeField] private ChamberController chamberController;

        [Header("Ring Transforms")]
        [SerializeField] private Transform innerRing;
        [SerializeField] private Transform outerRing;

        [Header("Rotation Settings")]
        [SerializeField] private float innerSpinSpeed = 45.0f;
        [SerializeField] private float outerSpinSpeed = 30.0f;
        [SerializeField] private float pulseSpinMultiplier = 4.0f;

        private float _currentMultiplier = 1.0f;

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandlePulseTriggered;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandlePulseTriggered;
            }
        }

        private void Update()
        {
            bool isAwakened = chamberController != null && chamberController.Phase == ChamberPhase.Awakening;
            float targetMultiplier = isAwakened ? 2.5f : 1.0f;

            _currentMultiplier = Mathf.MoveTowards(_currentMultiplier, targetMultiplier, Time.deltaTime * 2.0f);

            float dt = Time.deltaTime;

            if (innerRing != null)
            {
                innerRing.Rotate(Vector3.right, innerSpinSpeed * _currentMultiplier * dt, Space.Self);
                innerRing.Rotate(Vector3.forward, (innerSpinSpeed * 0.5f) * _currentMultiplier * dt, Space.Self);
            }

            if (outerRing != null)
            {
                outerRing.Rotate(Vector3.up, outerSpinSpeed * _currentMultiplier * dt, Space.Self);
                outerRing.Rotate(Vector3.right, (outerSpinSpeed * 0.35f) * _currentMultiplier * dt, Space.Self);
            }
        }

        private void HandlePulseTriggered()
        {
            _currentMultiplier = pulseSpinMultiplier;
        }
    }
}
