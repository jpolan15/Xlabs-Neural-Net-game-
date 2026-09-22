using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Visual and animation observer for the massive Awakening Gate.
    /// Smoothly transitions doors/portals between sealed and unsealed states,
    /// driving emissive intensity, colors, and gate lighting.
    /// </summary>
    [ExecuteAlways]
    public class AwakeningGateVisual : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private GatewayController gatewayController;

        [Header("Gate Portals / Wings")]
        [SerializeField] private Transform leftPortalWing;
        [SerializeField] private Transform rightPortalWing;
        [SerializeField] private Vector3 leftOpenOffset = new Vector3(-3.5f, 0, 0);
        [SerializeField] private Vector3 rightOpenOffset = new Vector3(3.5f, 0, 0);
        [SerializeField] private float animationSpeed = 2.0f;

        [Header("Lighting & Atmosphere")]
        [SerializeField] private Light gateAuraLight;
        [SerializeField] private Color lockedColor = new Color(0.90f, 0.45f, 0.12f);
        [SerializeField] private Color openColor = new Color(0.10f, 0.90f, 0.50f);

        private Vector3 _leftClosedPos;
        private Vector3 _rightClosedPos;
        private float _openProgress; // 0 = closed, 1 = open

        private void Awake()
        {
            if (gatewayController == null)
            {
                gatewayController = FindAnyObjectByType<GatewayController>();
            }

            if (leftPortalWing != null) _leftClosedPos = leftPortalWing.localPosition;
            if (rightPortalWing != null) _rightClosedPos = rightPortalWing.localPosition;
        }

        private void OnEnable()
        {
            if (gatewayController != null)
            {
                gatewayController.OnGatewayOpened += HandleGatewayOpened;
                gatewayController.OnGatewayClosed += HandleGatewayClosed;
            }
        }

        private void OnDisable()
        {
            if (gatewayController != null)
            {
                gatewayController.OnGatewayOpened -= HandleGatewayOpened;
                gatewayController.OnGatewayClosed -= HandleGatewayClosed;
            }
        }

        private void Update()
        {
            float target = (gatewayController != null && gatewayController.IsOpen) ? 1.0f : 0.0f;
            _openProgress = Mathf.MoveTowards(_openProgress, target, Time.deltaTime * animationSpeed);

            if (leftPortalWing != null)
            {
                leftPortalWing.localPosition = Vector3.Lerp(_leftClosedPos, _leftClosedPos + leftOpenOffset, _openProgress);
            }
            if (rightPortalWing != null)
            {
                rightPortalWing.localPosition = Vector3.Lerp(_rightClosedPos, _rightClosedPos + rightOpenOffset, _openProgress);
            }

            if (gateAuraLight != null)
            {
                gateAuraLight.color = Color.Lerp(lockedColor, openColor, _openProgress);
                gateAuraLight.intensity = Mathf.Lerp(0.35f, 2.5f, _openProgress);
            }
        }

        private void HandleGatewayOpened()
        {
            // Trigger visual opening effects
        }

        private void HandleGatewayClosed()
        {
            // Trigger visual sealing effects
        }
    }
}
