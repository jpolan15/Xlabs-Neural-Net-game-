using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Renders one frame of the Neural Ride from the rider's eye while the game is running, and writes it as a PNG.
    /// The Game view shows the desktop fallback camera, not what a headset wearer sees, so this puts a temporary camera
    /// on the pod seat at a chosen eye height, gaze, and field of view. It changes nothing in the scene.
    /// Call from the Unity CLI: unity command eval "return Convergence.EditorTools.RideLiveShot.Shoot(@\"C:\path\a.png\", 1.3f, 90f, 0f, 0f);"
    /// Development tooling only; game assemblies never reference it.
    /// </summary>
    public static class RideLiveShot
    {
        /// <summary>
        /// Writes a 1600 x 900 PNG. Eye height is metres above the pod floor, yaw and pitch are degrees relative to the pod's
        /// heading (pitch positive looks down), fov is the vertical field of view. Returns the path, or a message saying why not.
        /// </summary>
        public static string Shoot(string path, float eyeHeight = 1.30f, float fov = 90f, float yawDegrees = 0f, float pitchDegrees = 0f)
        {
            if (!Application.isPlaying) return "Not in Play Mode. Press Play first; edit-mode views come from Convergence/Capture Ride Seat Views.";
            GameObject pod = GameObject.Find("Pod");
            Camera head = Camera.main;
            if (pod == null || head == null) return "No Pod or no Main Camera in the running scene.";

            const int width = 1600, height = 900;
            var go = new GameObject("RideLiveShotCam") { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.CopyFrom(head);
                cam.enabled = false;
                go.AddComponent<UniversalAdditionalCameraData>();
                cam.fieldOfView = fov;
                cam.aspect = (float)width / height;
                cam.transform.SetPositionAndRotation(
                    pod.transform.position + Vector3.up * eyeHeight,
                    pod.transform.rotation * Quaternion.Euler(pitchDegrees, yawDegrees, 0f));

                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                cam.targetTexture = rt;
                cam.Render();

                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = null;
                if (rt != null)
                {
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }

                if (tex != null) Object.DestroyImmediate(tex);
                Object.DestroyImmediate(go);
            }
        }
    }
}
