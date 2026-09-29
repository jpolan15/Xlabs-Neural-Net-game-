using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Convergence.XR
{
    /// <summary>
    /// Reads a tracked position and rotation from the Input System XR layout.
    /// Falls back to the legacy XR node when that binding has no live control.
    /// </summary>
    public sealed class OpenXrNodePose : IDisposable
    {
        private readonly InputAction _position;
        private readonly InputAction _rotation;
        private readonly XRNode _legacyNode;
        private bool _disposed;

        public OpenXrNodePose(string positionBinding, string rotationBinding, XRNode legacyNode)
        {
            _legacyNode = legacyNode;
            _position = CreateAction(positionBinding, "Vector3");
            _rotation = CreateAction(rotationBinding, "Quaternion");
        }

        public bool TryRead(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;

            bool gotPosition = false;
            bool gotRotation = false;
            try
            {
                gotPosition = TryReadAction(_position, out position);
                gotRotation = TryReadAction(_rotation, out rotation) && IsUsable(rotation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[OpenXrNodePose] Pose read failed: {exception.Message}");
            }

            if (!gotPosition || !gotRotation)
            {
                InputDevice device = InputDevices.GetDeviceAtXRNode(_legacyNode);
                if (!device.isValid && _legacyNode == XRNode.CenterEye)
                    device = InputDevices.GetDeviceAtXRNode(XRNode.Head);

                if (device.isValid)
                {
                    if (!gotPosition && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 legacyPosition))
                    {
                        position = legacyPosition;
                        gotPosition = true;
                    }

                    if (!gotRotation &&
                        device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion legacyRotation) &&
                        IsUsable(legacyRotation))
                    {
                        rotation = legacyRotation;
                        gotRotation = true;
                    }
                }
            }

            return gotRotation;
        }

        public static bool IsUsable(Quaternion rotation)
        {
            float magnitude = (rotation.x * rotation.x) + (rotation.y * rotation.y) + (rotation.z * rotation.z) + (rotation.w * rotation.w);
            return magnitude > 0.5f;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DisposeAction(_position);
            DisposeAction(_rotation);
        }

        private static bool TryReadAction<T>(InputAction action, out T value) where T : struct
        {
            value = default;
            if (action == null || !action.enabled || action.activeControl == null)
                return false;

            value = action.ReadValue<T>();
            return true;
        }

        private static InputAction CreateAction(string binding, string controlType)
        {
            try
            {
                var action = new InputAction(
                    name: binding,
                    type: InputActionType.Value,
                    binding: binding,
                    expectedControlType: controlType);
                action.Enable();
                return action;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[OpenXrNodePose] Could not enable '{binding}': {exception.Message}");
                return null;
            }
        }

        private static void DisposeAction(InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
        }
    }
}
