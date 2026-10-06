using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// Plays the ride from the desk when no headset is running: hold the right mouse button to look around the
    /// cockpit, and click and drag a lever with the left button. The moment a headset or XR display is running it
    /// gives the camera back to tracking. The mouse writes the same lever API a hand does.
    /// </summary>
    public sealed class DesktopSeat : MonoBehaviour
    {
        [SerializeField] private float seatedEyeHeight = 1.2f;
        [SerializeField] private float lookSpeed = 0.15f;
        [SerializeField] private float maxYaw = 100f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private float reach = 6f;

        private readonly List<XRDisplaySubsystem> _displays = new List<XRDisplaySubsystem>(2);
        private Camera _camera;
        private TrackedPoseDriver _poseDriver;
        private PodLeverInteractable _dragged;
        private bool _driving;
        private float _yaw;
        private float _pitch;
        private float _allowedAfter;

        private void Start()
        {
            // A Link headset and a desk session both need the ride to keep running when the window loses focus.
            Application.runInBackground = true;
            _allowedAfter = Time.unscaledTime + 2f;
        }

        private void OnDisable()
        {
            Release();
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera != null) _poseDriver = _camera.GetComponent<TrackedPoseDriver>();
            }

            if (_camera == null || HeadsetIsActive() || Time.unscaledTime < _allowedAfter)
            {
                Release();
                return;
            }

            Take();
            Look();
            Drag();
        }

        private void Take()
        {
            if (_driving) return;
            _driving = true;
            if (_poseDriver != null) _poseDriver.enabled = false;
            _camera.transform.position = transform.position + Vector3.up * seatedEyeHeight;
            _yaw = 0f;
            _pitch = 16f;
            _camera.fieldOfView = 85f;
            _camera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Release()
        {
            if (!_driving) return;
            _driving = false;
            if (_dragged != null) _dragged.EndGrab();
            _dragged = null;
            if (_poseDriver != null) _poseDriver.enabled = true;
        }

        private void Look()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return;
            Vector2 delta = mouse.delta.ReadValue();
            _yaw = Mathf.Clamp(_yaw + delta.x * lookSpeed, -maxYaw, maxYaw);
            _pitch = Mathf.Clamp(_pitch - delta.y * lookSpeed, -maxPitch, maxPitch);
            _camera.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void Drag()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out RaycastHit hit, reach))
            {
                PodLeverInteractable lever = hit.collider.GetComponentInParent<PodLeverInteractable>();
                if (lever != null && lever.isActiveAndEnabled)
                {
                    _dragged = lever;
                    _dragged.BeginGrab(_dragged.FractionFromRay(ray));
                }
            }

            if (_dragged != null)
            {
                if (mouse.leftButton.isPressed) _dragged.UpdateGrab(_dragged.FractionFromRay(ray));
                else
                {
                    _dragged.EndGrab();
                    _dragged = null;
                }
            }
        }

        private bool HeadsetIsActive()
        {
            if (XRSettings.isDeviceActive) return true;
            _displays.Clear();
            SubsystemManager.GetSubsystems(_displays);
            for (int i = 0; i < _displays.Count; i++)
            {
                if (_displays[i].running) return true;
            }

            return false;
        }
    }
}
