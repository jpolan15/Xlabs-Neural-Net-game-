using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Draws one dash lever from its channel: the handle glides to the value (so a trainer driving the channel
    /// moves it like a hand would), turns orange when the player may touch it and grey when locked, and the whole
    /// lever disappears when it is not part of this stop.
    /// </summary>
    public sealed class PodLeverView : MonoBehaviour
    {
        [SerializeField] private RideControlChannel channel;
        [SerializeField] private Transform handle;
        [SerializeField] private Renderer handleRenderer;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Material liveMaterial;
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Vector3 axis = Vector3.up;
        [SerializeField] private float halfTravel = 0.11f;
        [SerializeField] private float follow = 14f;

        private void OnEnable()
        {
            if (channel == null) return;
            channel.StateChanged += Refresh;
            Refresh(channel);
            handle.localPosition = TargetPosition();
        }

        private void OnDisable()
        {
            if (channel != null) channel.StateChanged -= Refresh;
        }

        private void Update()
        {
            if (channel == null || handle == null) return;
            handle.localPosition = Vector3.Lerp(handle.localPosition, TargetPosition(), 1f - Mathf.Exp(-follow * Time.deltaTime));
        }

        private Vector3 TargetPosition()
        {
            return axis.normalized * Mathf.Lerp(-halfTravel, halfTravel, channel.Fraction);
        }

        private void Refresh(RideControlChannel changed)
        {
            if (visualRoot != null) visualRoot.SetActive(changed.Visible);
            if (handleRenderer != null)
            {
                handleRenderer.sharedMaterial = changed.Locked ? lockedMaterial : liveMaterial;
            }
        }
    }
}
