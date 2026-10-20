using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Renders one frame of the Neural Ride from the rider's eye while the game is running, and writes it as a PNG.
    /// The Game view shows the desktop fallback camera, not what a headset wearer sees, so this puts a temporary camera
    /// on the pod seat at a chosen eye height, gaze, and field of view. It changes nothing in the scene.
    /// Call from the Unity CLI: unity command eval "return Convergence.EditorTools.RideLiveShot.Shoot(@\"C:\path\a.png\", 1.3f, 90f, 0f, 0f);"
    ///
    /// For the explainers (TASK_LEVEL1_V4_EXPLAINERS) shots can also be planned before or during Play: ShootWhen waits
    /// until an explainer has run a number of steps (or the ride reaches a segment) and then a little game time, and
    /// shoots; AutoPilot pulls GO at the dock so a planned run goes on by itself. Development tooling only; game
    /// assemblies never reference it.
    /// </summary>
    [InitializeOnLoad]
    public static class RideLiveShot
    {
        [Serializable]
        private sealed class Plan
        {
            public string Path, Explainer;
            public int Steps, Segment = -99;
            public float Delay, Eye, Fov, Yaw, Pitch;
            [NonSerialized] public float ArmedAt;
            [NonSerialized] public bool Armed;
        }

        private static readonly List<Plan> Plans = new List<Plan>();
        private static readonly List<string> Done = new List<string>();
        private static bool _autoGo;
        private static float _dockSeconds;
        private static float _dockSince = -1f;
        private static bool _hooked;

        [Serializable]
        private sealed class Saved
        {
            public List<Plan> plans = new List<Plan>();
            public bool autoGo;
            public float dockSeconds;
        }

        private const string StateKey = "Convergence.RideLiveShot.Plans";

        // Plans survive the domain reload of entering Play Mode (they are kept in the editor session).
        static RideLiveShot()
        {
            string json = SessionState.GetString(StateKey, "");
            if (string.IsNullOrEmpty(json)) return;
            Saved saved = JsonUtility.FromJson<Saved>(json);
            if (saved == null) return;
            Plans.AddRange(saved.plans);
            _autoGo = saved.autoGo;
            _dockSeconds = saved.dockSeconds;
            if (Plans.Count > 0 || _autoGo) Hook();
        }

        private static void Save()
        {
            var saved = new Saved { plans = new List<Plan>(Plans), autoGo = _autoGo, dockSeconds = _dockSeconds };
            SessionState.SetString(StateKey, JsonUtility.ToJson(saved));
        }

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

                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();

                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = null;
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }

                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Plans a shot: once the explainer of this type (for example "PixelsExplainer") has run at least steps steps,
        /// waits delay seconds of game time and shoots. Explainer "segment:N" waits for the ride's segment N instead.
        /// </summary>
        public static string ShootWhen(string path, string explainer, int steps, float delay, float eyeHeight = 1.30f, float fov = 70f, float yawDegrees = 0f, float pitchDegrees = 0f)
        {
            var plan = new Plan { Path = path, Explainer = explainer, Steps = steps, Delay = delay, Eye = eyeHeight, Fov = fov, Yaw = yawDegrees, Pitch = pitchDegrees };
            if (explainer.StartsWith("segment:", StringComparison.Ordinal)) plan.Segment = int.Parse(explainer.Substring(8));
            Plans.Add(plan);
            Save();
            Hook();
            return "planned " + Plans.Count;
        }

        /// <summary>Pulls GO once the ride has waited at the dock this many seconds, so a planned run continues on its own.</summary>
        public static string AutoPilot(bool pullGo, float dockSeconds = 4f)
        {
            _autoGo = pullGo;
            _dockSeconds = dockSeconds;
            Save();
            Hook();
            return "autopilot " + pullGo;
        }

        /// <summary>What is still waiting and what was shot.</summary>
        public static string Status() => "pending " + Plans.Count + "; shot " + Done.Count + (Done.Count > 0 ? ": " + string.Join(", ", Done) : "");

        public static string Clear()
        {
            Plans.Clear();
            Done.Clear();
            _autoGo = false;
            Save();
            return "cleared";
        }

        private static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            var director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            if (director == null) return;
            if (director.State != RideState.Dock) _dockSince = -1f;
            else if (_dockSince < 0f) _dockSince = Time.time;
            if (_autoGo && director.State == RideState.Dock && Time.time - _dockSince >= _dockSeconds)
            {
                RideControlChannel go = director.Controls != null ? director.Controls.Get(RideControlIds.Action) : null;
                if (go != null && go.Value < 0.5f) go.SetFromUser(1f);
            }

            for (int i = Plans.Count - 1; i >= 0; i--)
            {
                Plan p = Plans[i];
                if (!p.Armed)
                {
                    if (!Ready(p, director)) continue;
                    p.Armed = true;
                    p.ArmedAt = Time.time;
                }

                if (Time.time - p.ArmedAt < p.Delay) continue;
                string result = Shoot(p.Path, p.Eye, p.Fov, p.Yaw, p.Pitch);
                Done.Add(System.IO.Path.GetFileName(result));
                Plans.RemoveAt(i);
                Save();
            }
        }

        private static bool Ready(Plan p, RideDirector director)
        {
            if (p.Segment != -99) return director.SegmentIndex >= p.Segment;
            foreach (Explainer e in UnityEngine.Object.FindObjectsByType<Explainer>(FindObjectsInactive.Include))
            {
                if (e.GetType().Name == p.Explainer) return e.StepsRun >= p.Steps;
            }

            return false;
        }
    }
}
