using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Development-only. Starts the MCP for Unity session (ADR-009) after each domain reload so agents do not
    /// need a manual click in Window > MCP for Unity. Uses reflection so no assembly reference is required.
    /// </summary>
    [InitializeOnLoad]
    internal static class McpAutoConnect
    {
        private const double RetryIntervalSeconds = 5.0;
        private const int MaxAttempts = 24;

        private static double _nextAttemptAt;
        private static int _attempts;
        private static volatile bool _starting;

        static McpAutoConnect()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            _nextAttemptAt = EditorApplication.timeSinceStartup + 2.0;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (_starting || EditorApplication.timeSinceStartup < _nextAttemptAt)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                _nextAttemptAt = EditorApplication.timeSinceStartup + 1.0;
                return;
            }

            _attempts++;
            _nextAttemptAt = EditorApplication.timeSinceStartup + RetryIntervalSeconds;
            if (_attempts > MaxAttempts)
            {
                EditorApplication.update -= Tick;
                return;
            }

            try
            {
                Type locator = Type.GetType("MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor");
                object bridge = locator?.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (bridge == null)
                {
                    return;
                }

                Type bridgeType = bridge.GetType();
                bool running = (bool)bridgeType.GetProperty("IsRunning").GetValue(bridge);
                if (running)
                {
                    EditorApplication.update -= Tick;
                    return;
                }

                _starting = true;
                var task = (System.Threading.Tasks.Task<bool>)bridgeType.GetMethod("StartAsync").Invoke(bridge, null);
                task.ContinueWith(t =>
                {
                    _starting = false;
                });
            }
            catch (Exception ex)
            {
                _starting = false;
                Debug.LogWarning($"[McpAutoConnect] {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
