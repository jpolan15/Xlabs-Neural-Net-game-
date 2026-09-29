using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Plays the level from the desk when no headset is connected: walk, look, and use the same
    /// puzzle controls the controllers use. The moment a headset or XR display is running, this
    /// script stops and gives the camera back to tracking. It never fires a weapon.
    /// </summary>
    public sealed class DesktopWalk : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float lookSpeed = 2.2f;

        private CharacterController _controller;
        private Camera _camera;
        private TrackedPoseDriver _poseDriver;
        private ChamberController _chamber;
        private readonly List<XRDisplaySubsystem> _displays = new List<XRDisplaySubsystem>(2);
        private float _pitch;
        private bool _desktopDriving;
        private float _desktopAllowedAfter;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _camera = GetComponentInChildren<Camera>();
            if (_camera != null)
            {
                _poseDriver = _camera.GetComponent<TrackedPoseDriver>();
            }

            _chamber = FindAnyObjectByType<ChamberController>();
            // Quest Link often reports no device for the first moments of Play. Stay out of the
            // camera until that window has passed, and give tracking back the frame a device appears.
            _desktopAllowedAfter = Time.unscaledTime + 2f;
        }

        private void OnDisable()
        {
            ReleaseDesktopCamera();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (HeadsetIsActive() || Time.unscaledTime < _desktopAllowedAfter)
            {
                ReleaseDesktopCamera();
                return;
            }

            bool looking = Cursor.lockState == CursorLockMode.Locked;
            TakeDesktopCamera();
            Look();
            Walk();
            UseControls(looking);
        }

        private void TakeDesktopCamera()
        {
            if (_desktopDriving)
            {
                return;
            }

            if (_poseDriver != null)
            {
                _poseDriver.enabled = false;
            }

            _desktopDriving = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_camera != null)
            {
                _pitch = _camera.transform.localEulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }
        }

        private void ReleaseDesktopCamera()
        {
            if (!_desktopDriving)
            {
                return;
            }

            if (_poseDriver != null)
            {
                _poseDriver.enabled = true;
            }

            _desktopDriving = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Look()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked || _camera == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            transform.Rotate(0f, delta.x * lookSpeed * 0.05f, 0f, Space.World);
            _pitch = Mathf.Clamp(_pitch - delta.y * lookSpeed * 0.05f, -80f, 80f);
            _camera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Walk()
        {
            Keyboard keyboard = Keyboard.current;
            if (_controller == null || _camera == null || keyboard == null)
            {
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Vector3 forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
            Vector3 motion = right * x + forward * z;
            if (motion.sqrMagnitude > 1f)
            {
                motion.Normalize();
            }

            motion *= moveSpeed;
            motion.y = _controller.isGrounded ? -0.5f : -2f;
            _controller.Move(motion * Time.deltaTime);
        }

        private void UseControls(bool looking)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && _chamber != null)
            {
                _chamber.TriggerForwardPass();
            }

            if (_camera == null || !looking || mouse == null)
            {
                return;
            }

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, 8f))
            {
                return;
            }

            XRInteractableBridge bridge = hit.collider.GetComponentInParent<XRInteractableBridge>();
            if (bridge == null)
            {
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (bridge.IsDial && Mathf.Abs(scroll) > 0.01f)
            {
                bridge.Step(scroll > 0f ? 1 : -1);
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                bridge.Activate();
            }
        }

        private bool HeadsetIsActive()
        {
            if (XRSettings.isDeviceActive)
            {
                return true;
            }

            _displays.Clear();
            SubsystemManager.GetSubsystems(_displays);
            for (int i = 0; i < _displays.Count; i++)
            {
                if (_displays[i].running)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
