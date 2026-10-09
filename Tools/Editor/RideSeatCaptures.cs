// Neural Ride seat view re-shoot (TASK_IGNITE_RIDE_V3_MASTER_PLAN, section 8).
// Edit mode only. For each stop N the Pod moves onto Station_N, only that station's StationReveal
// groups for its kind are shown, and Pod/Finale is hidden. Each stop is rendered at three eye heights.
// The dock view keeps the Pod where the scene has it, with every stop group and Finale hidden.
// Every transform and active state changed here is restored in a finally block. The scene is never
// saved and never marked dirty.
// Prerequisite: Assets/Scenes/NeuralRide.unity is the active scene, built by Convergence/Build Neural Ride.
// Output: Documentation/Design/captures/ride_v3/stop{N}_eye{115|130|145}.png and dock_eye130.png.

using System.Collections.Generic;
using System.IO;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Convergence.EditorTools
{
    public static class RideSeatCaptures
    {
        const string PodName = "Pod";
        const string FinaleName = "Finale";
        const int StopCount = 3;
        const int Width = 1600;
        const int Height = 900;
        const float FieldOfView = 68f;
        const float DockEyeHeight = 1.30f;
        static readonly float[] StopEyeHeights = { 1.15f, 1.30f, 1.45f };

        [MenuItem("Convergence/Capture Ride Seat Views")]
        public static void Capture()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[RideSeatCaptures] Exit Play Mode first. Seat views are captured in Edit mode only. Nothing was changed.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.isDirty)
            {
                Debug.LogError("[RideSeatCaptures] " + scene.name + " has unsaved changes. Save or revert it first so the captures match the saved scene. Nothing was changed.");
                return;
            }

            GameObject pod = FindRoot(scene, PodName);
            if (pod == null) return;

            Transform finale = pod.transform.Find(FinaleName);
            if (finale == null)
            {
                Debug.LogError("[RideSeatCaptures] Pod/Finale is missing. Rebuild the ride with Convergence/Build Neural Ride. Nothing was changed.");
                return;
            }

            var stops = new List<StopRig>();
            for (int n = 1; n <= StopCount; n++)
            {
                StopRig stop = ReadStop(scene, n);
                if (stop == null) return;
                stops.Add(stop);
            }

            string outDir = Path.Combine(ProjectRoot(), "Documentation", "Design", "captures", "ride_v3");
            Directory.CreateDirectory(outDir);

            Transform podT = pod.transform;
            Vector3 podLocalPosition = podT.localPosition;
            Quaternion podLocalRotation = podT.localRotation;
            Vector3 podLocalScale = podT.localScale;
            bool finaleWasActive = finale.gameObject.activeSelf;
            Transform card = pod.transform.Find("ChapterCard");
            bool cardWasActive = card != null && card.gameObject.activeSelf;
            if (card != null) card.gameObject.SetActive(false); // it only shows during a briefing

            GameObject camGo = null;
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                camGo = new GameObject("RideSeatCaptureCam") { hideFlags = HideFlags.HideAndDontSave };
                Camera cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                camGo.AddComponent<UniversalAdditionalCameraData>();
                ConfigureCamera(cam, pod);

                rt = new RenderTexture(Width, Height, GraphicsFormat.R8G8B8A8_SRGB, UnityEngine.Rendering.CoreUtils.GetDefaultDepthOnlyFormat());
                cam.targetTexture = rt;
                tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);

                // Dock: no stop is live. The Pod stays where the scene has it.
                ApplyStage(stops, -1, finale);
                Shoot(cam, rt, tex, podT, DockEyeHeight, Path.Combine(outDir, "dock_" + EyeTag(DockEyeHeight) + ".png"));

                for (int i = 0; i < stops.Count; i++)
                {
                    ApplyStage(stops, i, finale);
                    Transform station = stops[i].Station.transform;
                    podT.SetPositionAndRotation(station.position, station.rotation);

                    foreach (float eyeHeight in StopEyeHeights)
                    {
                        string file = "stop" + (i + 1) + "_" + EyeTag(eyeHeight) + ".png";
                        Shoot(cam, rt, tex, podT, eyeHeight, Path.Combine(outDir, file));
                    }
                }
            }
            finally
            {
                foreach (StopRig stop in stops)
                {
                    foreach (GroupEntry group in stop.Groups)
                    {
                        if (group.Root != null) group.Root.SetActive(group.WasActive);
                    }
                }

                finale.gameObject.SetActive(finaleWasActive);
                if (card != null) card.gameObject.SetActive(cardWasActive);
                podT.localPosition = podLocalPosition;
                podT.localRotation = podLocalRotation;
                podT.localScale = podLocalScale;

                RenderTexture.active = null;
                if (camGo != null)
                {
                    Camera cam = camGo.GetComponent<Camera>();
                    if (cam != null) cam.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(camGo);
                }

                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }

                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }

            if (scene.isDirty)
            {
                Debug.LogWarning("[RideSeatCaptures] The capture marked " + scene.name + " as modified. Its values were restored. Revert the scene instead of saving it.");
            }

            Debug.Log("[RideSeatCaptures] Finished. PNGs are in " + outDir);
        }

        static StopRig ReadStop(Scene scene, int number)
        {
            GameObject station = FindRoot(scene, "Station_" + number);
            if (station == null) return null;

            var controller = station.GetComponent<StationController>();
            var reveal = station.GetComponent<StationReveal>();
            if (controller == null || reveal == null)
            {
                Debug.LogError("[RideSeatCaptures] Station_" + number + " has no StationController or StationReveal. Rebuild the ride. Nothing was changed.");
                return null;
            }

            return new StopRig
            {
                Station = station,
                Kind = controller.Kind,
                Groups = ReadGroups(reveal)
            };
        }

        // Reads the StationReveal groups and their current active state before anything is changed.
        static List<GroupEntry> ReadGroups(StationReveal reveal)
        {
            var list = new List<GroupEntry>();
            var so = new SerializedObject(reveal);
            SerializedProperty array = so.FindProperty("groups");
            for (int i = 0; array != null && i < array.arraySize; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                var root = element.FindPropertyRelative("root").objectReferenceValue as GameObject;
                if (root == null) continue;

                list.Add(new GroupEntry
                {
                    Root = root,
                    WasActive = root.activeSelf,
                    OneSignal = element.FindPropertyRelative("oneSignal").boolValue,
                    TwoSignals = element.FindPropertyRelative("twoSignals").boolValue,
                    Learns = element.FindPropertyRelative("learns").boolValue
                });
            }

            so.Dispose();
            return list;
        }

        // Shows only the live stop's groups for its kind, hides every other group, and hides Finale.
        // liveIndex -1 means the dock, where no stop is live.
        static void ApplyStage(List<StopRig> stops, int liveIndex, Transform finale)
        {
            for (int i = 0; i < stops.Count; i++)
            {
                StopRig stop = stops[i];
                foreach (GroupEntry group in stop.Groups)
                {
                    if (group.Root == null) continue;
                    group.Root.SetActive(i == liveIndex && ShowsForKind(group, stop.Kind));
                }
            }

            finale.gameObject.SetActive(false);
        }

        static bool ShowsForKind(GroupEntry group, StationKind kind)
        {
            if (kind == StationKind.OneSignal) return group.OneSignal;
            if (kind == StationKind.TwoSignals) return group.TwoSignals;
            return group.Learns;
        }

        static void ConfigureCamera(Camera cam, GameObject pod)
        {
            cam.fieldOfView = FieldOfView;
            cam.aspect = (float)Width / Height;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.016f, 0.030f);
            cam.cullingMask = ~0;

            // Match the player's head camera (built by NeuralRideBuilder) when it is present.
            Camera head = pod.GetComponentInChildren<Camera>(true);
            if (head == null) return;
            cam.clearFlags = head.clearFlags;
            cam.backgroundColor = head.backgroundColor;
            cam.nearClipPlane = head.nearClipPlane;
            cam.farClipPlane = head.farClipPlane;
        }

        // The Pod origin is the floor: RideDirector places it on the path. The eye sits straight up from
        // there and looks level along the Pod's forward direction.
        static void Shoot(Camera cam, RenderTexture rt, Texture2D tex, Transform pod, float eyeHeight, string path)
        {
            Vector3 forward = pod.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

            Vector3 eye = pod.position + Vector3.up * eyeHeight;
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(forward.normalized, Vector3.up));
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[RideSeatCaptures] Wrote " + path);
        }

        static string EyeTag(float eyeHeight)
        {
            return "eye" + Mathf.RoundToInt(eyeHeight * 100f).ToString();
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }

            Debug.LogError("[RideSeatCaptures] Root object " + name + " is missing from " + scene.name + ". Open Assets/Scenes/NeuralRide.unity first. Nothing was changed.");
            return null;
        }

        static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        sealed class StopRig
        {
            public GameObject Station;
            public StationKind Kind;
            public List<GroupEntry> Groups;
        }

        sealed class GroupEntry
        {
            public GameObject Root;
            public bool WasActive;
            public bool OneSignal;
            public bool TwoSignals;
            public bool Learns;
        }
    }
}
