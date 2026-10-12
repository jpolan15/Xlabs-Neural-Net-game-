// Level 1 seat views (AGENTS.md validation item 6, TASK_LEVEL1_DIGITS_RESTRUCTURE section 5).
// Edit mode only. For every RideViewpoint (dock, each stop, the rim, the middle of the drop, the valley floor, the end)
// the Pod is moved onto the viewpoint, only the regions the streamer shows at that segment are active, the fog and sky
// are that region's, and pod panels that are hidden there are hidden. Each viewpoint is rendered at eye heights 1.15,
// 1.30 and 1.45 m and gaze yaws -30, 0 and +30 degrees, plus one 3 x 3 contact sheet. Every transform, active state and
// render setting changed here is restored in a finally block. The scene is never saved.
// Prerequisite: Assets/Scenes/NeuralRide.unity is the active scene, built by Convergence/Build Neural Ride.
// Output: Documentation/Design/captures/level1/<viewpoint>/eye{115|130|145}_yaw{m30|0|p30}.jpg and level1/<viewpoint>.jpg.
// From the Unity CLI: unity command eval "return Convergence.EditorTools.RideSeatCaptures.CaptureAll();"

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
        const int Width = 1280;
        const int Height = 720;
        const float FieldOfView = 68f;
        static readonly float[] EyeHeights = { 1.15f, 1.30f, 1.45f };
        static readonly float[] Yaws = { -30f, 0f, 30f };

        [MenuItem("Convergence/Capture Ride Seat Views")]
        public static void Capture() => Debug.Log("[RideSeatCaptures] " + CaptureAll());

        /// <summary>Shoots every viewpoint (or only those whose label is in a comma-separated list). Returns a summary.</summary>
        public static string CaptureAll(string only = null)
        {
            if (Application.isPlaying) return "FAILED: exit Play Mode first. Seat views are captured in edit mode only. Nothing was changed.";
            Scene scene = SceneManager.GetActiveScene();
            if (scene.isDirty) return "FAILED: " + scene.name + " has unsaved changes. Save or revert it first so the captures match the saved scene. Nothing was changed.";

            GameObject pod = null;
            RegionStreamer streamer = null;
            RideDirector director = null;
            var regions = new Dictionary<int, GameObject>();
            var viewpoints = new List<RideViewpoint>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Pod") pod = root;
                if (root.name.StartsWith("Region_") && int.TryParse(root.name.Substring(7), out int n)) regions[n] = root;
                if (streamer == null) streamer = root.GetComponentInChildren<RegionStreamer>(true);
                if (director == null) director = root.GetComponentInChildren<RideDirector>(true);
                viewpoints.AddRange(root.GetComponentsInChildren<RideViewpoint>(true));
            }

            if (pod == null || viewpoints.Count == 0) return "FAILED: no Pod or no viewpoints. Open Assets/Scenes/NeuralRide.unity (built by Convergence/Build Neural Ride). Nothing was changed.";
            var theme = AssetDatabase.LoadAssetAtPath<RideTheme>("Assets/ScriptableObjects/RideTheme.asset");
            var wanted = only == null ? null : new HashSet<string>(only.Split(','));

            string outDir = Path.Combine(ProjectRoot(), "Documentation", "Design", "captures", "level1");
            Directory.CreateDirectory(outDir);
            Transform podT = pod.transform;
            Vector3 podPosition = podT.position;
            Quaternion podRotation = podT.rotation;
            var regionWasActive = new Dictionary<GameObject, bool>();
            foreach (GameObject r in regions.Values) regionWasActive[r] = r.activeSelf;
            var podPanels = new Dictionary<Transform, bool>();
            foreach (string path in new[] { "EndCard", "Dash/WeightGrid" })
            {
                Transform t = podT.Find(path);
                if (t != null) podPanels[t] = t.gameObject.activeSelf;
            }

            bool fog = RenderSettings.fog;
            Color fogColor = RenderSettings.fogColor;
            float fogDensity = RenderSettings.fogDensity;
            GameObject camGo = null;
            RenderTexture rt = null;
            Texture2D tex = null, sheet = null;
            int shots = 0;
            try
            {
                camGo = new GameObject("RideSeatCaptureCam") { hideFlags = HideFlags.HideAndDontSave };
                Camera cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                camGo.AddComponent<UniversalAdditionalCameraData>();
                cam.fieldOfView = FieldOfView;
                cam.aspect = (float)Width / Height;
                cam.nearClipPlane = 0.03f;
                cam.farClipPlane = 160f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                rt = new RenderTexture(Width, Height, GraphicsFormat.R8G8B8A8_UNorm, UnityEngine.Rendering.CoreUtils.GetDefaultDepthOnlyFormat()) { antiAliasing = 4 };
                cam.targetTexture = rt;
                tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                sheet = new Texture2D(Width / 2 * 3, Height / 2 * 3, TextureFormat.RGB24, false);

                foreach (RideViewpoint vp in viewpoints)
                {
                    if (wanted != null && !wanted.Contains(vp.label)) continue;
                    int mask = streamer != null ? streamer.MaskFor(vp.segment) : ~0;
                    foreach (KeyValuePair<int, GameObject> r in regions) r.Value.SetActive((mask & (1 << r.Key)) != 0);
                    foreach (KeyValuePair<Transform, bool> panel in podPanels)
                    {
                        string path = panel.Key.name == "WeightGrid" ? "Dash/WeightGrid" : panel.Key.name;
                        panel.Key.gameObject.SetActive(System.Array.IndexOf(vp.podPanels, path) >= 0);
                    }

                    int region = vp.segment < 0 || director == null ? 0 : director.Segments[vp.segment].region;
                    RideTheme.RegionLook look = theme != null ? theme.Region(region) : default;
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = look.fog;
                    RenderSettings.fogDensity = look.fogDensity;
                    cam.backgroundColor = look.background;

                    podT.SetPositionAndRotation(vp.transform.position, vp.transform.rotation);
                    string dir = Path.Combine(outDir, vp.label);
                    Directory.CreateDirectory(dir);
                    for (int e = 0; e < EyeHeights.Length; e++)
                    {
                        for (int y = 0; y < Yaws.Length; y++)
                        {
                            Shoot(cam, rt, tex, podT, EyeHeights[e], Yaws[y]);
                            File.WriteAllBytes(Path.Combine(dir, "eye" + Mathf.RoundToInt(EyeHeights[e] * 100f) + "_yaw" + YawTag(Yaws[y]) + ".jpg"), tex.EncodeToJPG(88));
                            Paste(tex, sheet, y, 2 - e);
                            shots++;
                        }
                    }

                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(outDir, vp.label + ".jpg"), sheet.EncodeToJPG(85));
                }
            }
            finally
            {
                foreach (KeyValuePair<GameObject, bool> r in regionWasActive) r.Key.SetActive(r.Value);
                foreach (KeyValuePair<Transform, bool> panel in podPanels) panel.Key.gameObject.SetActive(panel.Value);
                RenderSettings.fog = fog;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
                podT.SetPositionAndRotation(podPosition, podRotation);
                RenderTexture.active = null;
                if (camGo != null) Object.DestroyImmediate(camGo);
                if (rt != null)
                {
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }

                if (tex != null) Object.DestroyImmediate(tex);
                if (sheet != null) Object.DestroyImmediate(sheet);
            }

            string note = scene.isDirty ? " The capture marked the scene modified; its values were restored. Revert it instead of saving." : "";
            return "Wrote " + shots + " seat views to " + outDir + " (one folder and one 3 x 3 sheet per viewpoint: rows eye 1.45 / 1.30 / 1.15 m, columns yaw -30 / 0 / +30)." + note;
        }

        static string YawTag(float yaw) => yaw < 0 ? "m" + Mathf.RoundToInt(-yaw) : yaw > 0 ? "p" + Mathf.RoundToInt(yaw) : "0";

        // The pod origin is the floor; the eye sits straight up from there and looks level, turned by the gaze yaw.
        static void Shoot(Camera cam, RenderTexture rt, Texture2D tex, Transform pod, float eyeHeight, float yaw)
        {
            Vector3 forward = pod.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            Quaternion look = Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(5f, yaw, 0f);
            cam.transform.SetPositionAndRotation(pod.position + Vector3.up * eyeHeight, look);
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Copies a frame, halved, into cell (column, row) of the contact sheet (row 0 at the bottom).</summary>
        static void Paste(Texture2D frame, Texture2D sheet, int column, int row)
        {
            int w = Width / 2, h = Height / 2;
            var pixels = new Color32[w * h];
            Color32[] src = frame.GetPixels32();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) pixels[y * w + x] = src[(y * 2) * Width + x * 2];
            }

            sheet.SetPixels32(column * w, row * h, w, h, pixels);
        }

        static string ProjectRoot() => Directory.GetParent(Application.dataPath).FullName;
    }
}
