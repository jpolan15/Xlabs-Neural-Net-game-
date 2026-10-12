// Model sheets: every model in the Neural Ride photographed from every side (TASK_LEVEL1_DIGITS_RESTRUCTURE, section 6).
// A seat capture shows what the rider sees from one spot. A model sheet shows one model on its own, from all around
// and above, so a piece that only looks right from the seat (a flat back, a hole, a part floating loose, a glow that
// swallows the shape) is caught before anyone films it or walks around it.
//
// Each sheet is one PNG of 12 tiles:
//   row 1  front, front-right 45, right 90, back-right 135     (orbit 12 degrees above the model's center)
//   row 2  back 180, back-left 225, left 270, front-left 315
//   row 3  top, low hero 3/4, rider eye 1.30 m, in the world
// "Front" is the side that faces the rider when they meet the model. The first ten tiles show the model alone (a copy
// in a preview scene, no fog or distance fade, the ride's background color), framed tight per view. The last two show it in the whole
// ride (a copy of every ride root, with the pod moved to where the rider meets it), so the sheet answers both "is the
// model good" and "does it work where it stands".
//
// Nothing in the open scene is changed: all rendering happens on copies in preview scenes, which are closed after.
// The copies are made under an inactive parent and lose their scripts (text excepted) before they are switched on, so
// no view, sound or XR component on a copy ever runs.
// Edit mode shows the saved scene. Play Mode works too and shows the moment it is called, which is how geometry the
// ride only makes at run time (the loss landscape's surface, the pipes) gets photographed; the sheet header says which.
// Prerequisite: Assets/Scenes/NeuralRide.unity is open, built by Convergence/Build Neural Ride.
// Output: Documentation/Design/captures/model_sheets/<id>.png, the single tiles in <id>/, and index.md.
// From the Unity CLI: unity command eval "return Convergence.EditorTools.RideModelSheets.Capture();"
// (pass a model id, or several separated by commas, to shoot only those).
//
// Every model the builder makes must be listed in Models below (or in NotModels with the reason). The builder calls
// AssertCoverage, so a new piece without a sheet fails the build. Development tooling only.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Convergence.EditorTools
{
    public static class RideModelSheets
    {
        /// <summary>One model: one or more scene objects shot together, and where the rider meets them.</summary>
        public sealed class Model
        {
            public readonly string Id;
            public readonly string Note;
            public readonly string RiderAt;
            public readonly string[] Paths;

            /// <summary>Paths under the model (relative to it) left out of the isolated copy, such as panels that belong to another sheet.</summary>
            public string[] Skip = new string[0];

            /// <summary>Turns on every child of the copy: for templates the ride spawns at run time, which are hidden in the scene.</summary>
            public bool ShowHidden;

            /// <summary>Background of the isolated tiles; null uses the ride camera's background.</summary>
            public Color? Background;

            /// <summary>
            /// Scene path of the spot the ride shows the model at, when that is not where the scene keeps it (a template
            /// the ride spawns on the lane sits at the station origin in the scene). The copies are moved there.
            /// </summary>
            public string ShowAt;

            /// <summary>The model's front in its own local space, when "the side facing the rider" does not say it (a template).</summary>
            public Vector3? Front;

            /// <summary>Shader floats set on the model's copies only (a property block), to show a run-time state such as the lit finale.</summary>
            public Dictionary<string, float> MaterialFloats;

            /// <summary>Shader colours set the same way, for a run-time state such as the wrong guess's red flash.</summary>
            public Dictionary<string, Color> MaterialColors;

            /// <param name="riderAt">Scene path of the transform the pod sits at when the rider meets the model ("Pod" for where the pod is now).</param>
            public Model(string id, string note, string riderAt, params string[] paths)
            {
                Id = id;
                Note = note;
                RiderAt = riderAt;
                Paths = paths;
            }
        }

        // The models of Level 1 (TASK_LEVEL1_DIGITS_RESTRUCTURE section 6). Every set piece the builder makes is here, in
        // the same change that builds it; the rider spot is the RideViewpoint where the rider meets it.
        public static readonly Model[] Models =
        {
            new Model("pod", "The rider's cart: tub, seat and the dash with GO, the subtitle screen and the region tag.", "Pod", "Pod")
            {
                Skip = new[] { "SeatAnchor", "EndCard", "Dash/WeightGrid" }
            },
            new Model("weight_grid", "Region 2: the 7x7 weight grid on the dash the rider paints (+ cyan, - amber, orange frame = press).", "Viewpoints/neuron", "Pod/Dash/WeightGrid"),
            new Model("end_card", "The end card with the next level and the credits.", "Viewpoints/end", "Pod/EndCard"),
            new Model("rails", "The two slate rails under every leg of the track.", "Viewpoints/dock", "Track"),
            new Model("page", "Region 0: the sheet with the real handwritten 3 (test digit) on its desk.", "Viewpoints/dock", "Region_0/Page", "Region_0/Desk"),
            new Model("pixel_field", "Region 1: the 3's 784 pixels as pillars, height = ink, with three values.", "Viewpoints/grid_approach", "Region_1/PixelField"),
            new Model("input_grid_7x7", "Region 1: the 49 numbers the machine gets (4x4 averages) and their sign.", "Viewpoints/grid", "Region_1/InputGrid7"),
            new Model("neuron_amber", "Region 2: the big amber neuron with its 49 dendrites.", "Viewpoints/neuron", "Region_2/Neuron"),
            new Model("why_readout", "Region 2: the why panel: the digit on show, ink x weight, the total against the line, all eight digits.", "Viewpoints/neuron", "Region_2/Panel"),
            new Model("network_untrained", "Region 3: the real 49-16-10 network with its untrained weights, wires by sign and size, neurons by activation.", "Viewpoints/network", "Region_3/Network"),
            new Model("output_labels", "Region 3: the ten answers 0 to 9 on solid tags, and the guess glow.", "Viewpoints/wrong", "Region_3/Network/Outputs"),
            new Model("network_wrong", "Region 4: the untrained network flashing red, with the wrong guess.", "Viewpoints/wrong", "Region_3/Network", "Region_4/GuessSign")
            {
                MaterialColors = new Dictionary<string, Color> { { "_PosColor", new Color(1f, 0.23f, 0.23f) }, { "_NegColor", new Color(0.7f, 0.16f, 0.16f) } }
            },
            new Model("blame_wires", "Region 5: every wire burning by its real blame, and the brightest six as pointable targets.", "Viewpoints/blame", "Region_5/Blame")
            {
                MaterialFloats = new Dictionary<string, float> { { "_LitUpTo", 2f } }
            },
            new Model("valley_terrain", "Region 6: the ground of real loss along the training path, with contours, the trail and the hop posts.", "Viewpoints/rim", "Region_6/Ground"),
            new Model("valley_set", "Region 6: the deck at the rim, the sign, and the pink sun.", "Viewpoints/rim", "Region_6/Deck", "Region_6/Sign", "Region_6/Sun"),
            new Model("network_trained", "Region 7: the same network with its trained weights, in gold.", "Viewpoints/right", "Region_7/Network"),
            new Model("finale_digits", "Region 7: 60 real test digits with the trained network's real answers (wrong ones red).", "Viewpoints/right", "Region_7/DigitWall"),
            new Model("accuracy_sign", "Region 7: the real accuracy and training count.", "Viewpoints/right", "Region_7/AccuracySign"),
            new Model("words", "Region 7: the teaser, the same machinery with words.", "Viewpoints/end", "Region_7/Words"),
        };

        /// <summary>Renderable ride objects that are not models, by scene path, with the reason. Keep this short.</summary>
        public static readonly Dictionary<string, string> NotModels = new Dictionary<string, string>
        {
            { "Pod/SeatAnchor", "The XR rig's controller models; they follow the rider's hands." },
        };

        private const int TileWidth = 960;
        private const int TileHeight = 540;
        private const int Columns = 4;
        private const int Rows = 3;
        private const int HeaderHeight = 150;
        private const float OrbitFov = 32f;
        private const float OrbitElevation = 12f;
        private const float RiderEyeHeight = 1.30f;
        private const float RiderFov = 68f;   // the seat captures' field of view (RideSeatCaptures)
        private static readonly string[] RootNames = { "Pod", "Track" };
        private static readonly string[] RuntimeOnlyPanels = { "Pod/EndCard", "Pod/Dash/WeightGrid" };

        private sealed class Tile
        {
            public string Label;
            public string Slug;
            public Color32[] Pixels;
        }

        private sealed class Stats
        {
            public int Triangles;
            public int Renderers;
            public int Materials;
            public Vector3 Size;
        }

        [MenuItem("Convergence/Capture Model Sheets")]
        public static void CaptureMenu()
        {
            string summary = Capture();
            if (summary.StartsWith("FAILED", StringComparison.Ordinal)) Debug.LogError("[RideModelSheets] " + summary);
            else Debug.Log("[RideModelSheets] " + summary);
        }

        /// <summary>
        /// Shoots the sheets. only: a model id, or ids separated by commas (null for all). outDir: null for
        /// Documentation/Design/captures/model_sheets. Returns a summary: what was written, and any model that failed and why.
        /// </summary>
        public static string Capture(string only = null, string outDir = null)
        {

            Scene scene = SceneManager.GetActiveScene();
            if (FindPath(scene, "Pod") == null) return "FAILED: no Pod in " + scene.name + ". Open Assets/Scenes/NeuralRide.unity (built by Convergence/Build Neural Ride) first. Nothing was changed.";

            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(only))
            {
                foreach (string id in only.Split(',')) wanted.Add(id.Trim());
                foreach (string id in wanted)
                {
                    if (Array.Find(Models, m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        return "FAILED: no model with id '" + id + "'. Ids: " + string.Join(", ", Array.ConvertAll(Models, m => m.Id));
                    }
                }
            }

            outDir = outDir ?? Path.Combine(ProjectRoot(), "Documentation", "Design", "captures", "model_sheets");
            Directory.CreateDirectory(outDir);
            Color background = RideBackground(scene);

            var written = new List<string>();
            var failed = new List<string>();
            var rows = new List<(Model model, Stats stats)>();
            Scene world = default;
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                world = EditorSceneManager.NewPreviewScene();
                World copy = CopyWorld(scene, world);
                // sRGB only in a Linear project: in Gamma (this project) Unity falls back to UNorm with a warning per texture.
                GraphicsFormat color = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                var desc = new RenderTextureDescriptor(TileWidth, TileHeight, color, CoreUtils.GetDefaultDepthOnlyFormat()) { msaaSamples = 4 };
                rt = new RenderTexture(desc);
                tex = new Texture2D(TileWidth, TileHeight, TextureFormat.RGBA32, false);

                foreach (Model model in Models)
                {
                    if (wanted.Count > 0 && !wanted.Contains(model.Id)) continue;
                    try
                    {
                        Stats stats = CaptureModel(scene, copy, model, outDir, model.Background ?? background, rt, tex);
                        rows.Add((model, stats));
                        written.Add(model.Id + ".png");
                    }
                    catch (Exception e)
                    {
                        failed.Add(model.Id + ": " + e.Message);
                    }
                }
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
                if (world.IsValid()) EditorSceneManager.ClosePreviewScene(world);
            }

            List<string> uncovered = FindUncovered(scene);
            if (wanted.Count == 0) WriteIndex(outDir, rows, failed, uncovered);

            var summary = new StringBuilder();
            if (failed.Count > 0) summary.Append("FAILED for ").Append(failed.Count).Append(" model(s): ").Append(string.Join(" | ", failed)).Append(". ");
            summary.Append("Wrote ").Append(written.Count).Append(" sheet(s) to ").Append(outDir).Append(": ").Append(string.Join(", ", written)).Append('.');
            if (uncovered.Count > 0) summary.Append(" Not covered by any sheet: ").Append(string.Join(", ", uncovered)).Append('.');
            if (scene.isDirty) summary.Append(" Note: ").Append(scene.name).Append(" has unsaved changes, and the sheets show them.");
            return summary.ToString();
        }

        // ---------- coverage ----------

        /// <summary>
        /// Throws if a renderable piece of the ride has no model sheet, or a model's path, rider spot or show-at spot no
        /// longer exists (a renamed object). Called by the builder after the scene is built.
        /// </summary>
        public static void AssertCoverage()
        {
            Scene scene = SceneManager.GetActiveScene();
            List<string> uncovered = FindUncovered(scene);
            List<string> missing = FindMissingPaths(scene);
            if (uncovered.Count == 0 && missing.Count == 0) return;
            var message = new StringBuilder();
            if (uncovered.Count > 0)
            {
                message.Append("These ride objects have no model sheet: ").Append(string.Join(", ", uncovered))
                    .Append(". Add each one to RideModelSheets.Models in Tools/Editor/RideModelSheets.cs (or to NotModels with the reason), ")
                    .Append("then shoot it with Convergence/Capture Model Sheets. ");
            }

            if (missing.Count > 0)
            {
                message.Append("RideModelSheets.Models names objects the ride no longer has: ").Append(string.Join(", ", missing))
                    .Append(". Update the paths in Tools/Editor/RideModelSheets.cs.");
            }

            throw new InvalidOperationException(message.ToString().Trim());
        }

        /// <summary>Every path a model names (its objects, rider spot and show-at spot) that is not in the scene, as "id: path".</summary>
        public static List<string> FindMissingPaths(Scene scene)
        {
            var missing = new List<string>();
            foreach (Model m in Models)
            {
                foreach (string p in m.Paths)
                {
                    if (FindPath(scene, p) == null) missing.Add(m.Id + ": " + p);
                }

                if (FindPath(scene, m.RiderAt) == null) missing.Add(m.Id + ": rider at " + m.RiderAt);
                if (m.ShowAt != null && FindPath(scene, m.ShowAt) == null) missing.Add(m.Id + ": show at " + m.ShowAt);
            }

            return missing;
        }

        /// <summary>
        /// Every direct child of a ride root (Pod, Track, Region_N; Station_N for older scenes) that renders anything must sit
        /// on, above, or under a model's path. Station_N roots compare as one (Station_2 is covered by Station_1); regions do not.
        /// </summary>
        public static List<string> FindUncovered(Scene scene)
        {
            var covered = new List<string>();
            foreach (Model m in Models)
            {
                foreach (string p in m.Paths) covered.Add(Normalize(p));
            }

            foreach (string p in NotModels.Keys) covered.Add(Normalize(p));

            var missing = new List<string>();
            var seen = new HashSet<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!IsRideRoot(root.name)) continue;
                if (root.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                foreach (Transform child in root.transform)
                {
                    if (child.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                    string path = root.name + "/" + child.name;
                    string key = Normalize(path);
                    if (!seen.Add(key)) continue;
                    bool ok = covered.Exists(c => c == key || key.StartsWith(c + "/", StringComparison.Ordinal) || c.StartsWith(key + "/", StringComparison.Ordinal));
                    if (!ok) missing.Add(path);
                }
            }

            return missing;
        }

        private static bool IsRideRoot(string name)
        {
            if (Array.IndexOf(RootNames, name) >= 0) return true;
            return name.StartsWith("Station_", StringComparison.Ordinal) || name.StartsWith("Region_", StringComparison.Ordinal);
        }

        /// <summary>"Station_2/Neuron" and "Station_1/Neuron" compare equal: a trailing _N on a Station root becomes _*. Region roots differ, so they stay apart.</summary>
        private static string Normalize(string path)
        {
            int slash = path.IndexOf('/');
            string root = slash < 0 ? path : path.Substring(0, slash);
            string rest = slash < 0 ? "" : path.Substring(slash);
            int us = root.LastIndexOf('_');
            if (root.StartsWith("Station_", StringComparison.Ordinal) && us > 0 && us < root.Length - 1 && int.TryParse(root.Substring(us + 1), out _)) root = root.Substring(0, us) + "_*";
            return root + rest;
        }

        // ---------- one model ----------

        private static Stats CaptureModel(Scene scene, World world, Model model, string outDir, Color background, RenderTexture rt, Texture2D tex)
        {
            var sources = new List<Transform>();
            foreach (string p in model.Paths)
            {
                Transform t = FindPath(scene, p);
                if (t == null) throw new InvalidOperationException("no object at " + p + " (renamed? update RideModelSheets.Models)");
                sources.Add(t);
            }

            Transform rider = FindPath(scene, model.RiderAt);
            if (rider == null) throw new InvalidOperationException("no rider spot at " + model.RiderAt);
            Transform showAt = null;
            if (model.ShowAt != null)
            {
                showAt = FindPath(scene, model.ShowAt);
                if (showAt == null) throw new InvalidOperationException("no show-at spot at " + model.ShowAt);
            }

            Vector3? front = model.Front.HasValue ? sources[0].TransformDirection(model.Front.Value) : (Vector3?)null;

            var tiles = new List<Tile>();
            Scene iso = EditorSceneManager.NewPreviewScene();
            Stats stats;
            Bounds solid;
            Vector3 forward, right;
            // Fog follows the open scene even in a preview scene, and at orbit distance it blacks a big model out. It is
            // off only while the isolated tiles render (this does not mark the scene changed), and on again for the world.
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            try
            {
                var skip = new HashSet<string>(model.Skip);
                foreach (Transform src in sources)
                {
                    GameObject holder = NewIn(iso, "Holder");
                    holder.SetActive(false); // nothing on the copy wakes up while it is made
                    if (src.parent != null)
                    {
                        holder.transform.SetPositionAndRotation(src.parent.position, src.parent.rotation);
                        holder.transform.localScale = src.parent.lossyScale;
                    }

                    GameObject copy = CopyVisual(src, holder.transform, "", skip);
                    StripBehaviours(holder);
                    holder.SetActive(true);
                    if (copy == null) continue;
                    copy.SetActive(true); // a model sheet shows the model, even if the scene keeps it hidden until its moment
                    if (model.ShowHidden)
                    {
                        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.SetActive(true);
                    }

                    if (showAt != null) copy.transform.position = showAt.position;
                    PrepareStudio(copy, model.MaterialFloats);
                }

                foreach (TMP_Text text in AllIn<TMP_Text>(iso)) text.ForceMeshUpdate();
                stats = Measure(iso, out solid);
                if (stats.Renderers == 0) throw new InvalidOperationException("nothing renders in " + string.Join(", ", model.Paths));

                Vector3 eye = rider.position + Vector3.up * RiderEyeHeight;
                FrontAndRight(solid, eye, rider, front, out forward, out right);

                Camera cam = NewCamera(iso, background, rt);
                Vector3[] corners = Corners(solid);
                float radius = Mathf.Max(0.05f, solid.extents.magnitude);
                string[] names = { "FRONT", "FRONT-RIGHT 45", "RIGHT 90", "BACK-RIGHT 135", "BACK 180", "BACK-LEFT 225", "LEFT 270", "FRONT-LEFT 315" };
                for (int i = 0; i < 8; i++)
                {
                    Vector3 dir = Orbit(forward, right, i * 45f, OrbitElevation);
                    Frame(cam, solid.center, dir, corners, radius, OrbitFov, 0.84f);
                    tiles.Add(Shoot(cam, rt, tex, names[i], (i + 1).ToString("00") + "_" + Slug(names[i])));
                }

                Frame(cam, solid.center, Orbit(forward, right, 0f, 80f), corners, radius, OrbitFov, 0.84f);
                tiles.Add(Shoot(cam, rt, tex, "TOP", "09_top"));

                // Low hero: almost level with the model, from the rider's right, with a wider lens for some drama.
                Frame(cam, solid.center, Orbit(forward, right, 35f, 3f), corners, radius, 50f, 0.92f);
                tiles.Add(Shoot(cam, rt, tex, "LOW HERO 3/4", "10_low_hero"));
            }
            finally
            {
                RenderSettings.fog = fog;
                EditorSceneManager.ClosePreviewScene(iso);
            }

            // In the ride: the whole world copy, staged for the moment the rider meets this model.
            world.Stage(model, rider, showAt);
            Camera worldCam = NewCamera(world.Scene, RideBackground(scene), rt);
            worldCam.nearClipPlane = 0.03f;
            worldCam.farClipPlane = 400f;
            Vector3 seatEye = rider.position + Vector3.up * RiderEyeHeight;
            Bounds inflated = solid;
            inflated.Expand(1.0f);
            bool riderInside = inflated.Contains(seatEye);
            Vector3 look = riderInside ? Flat(rider.forward) : (solid.center - seatEye).normalized;
            worldCam.fieldOfView = RiderFov;
            worldCam.transform.SetPositionAndRotation(seatEye, Quaternion.LookRotation(look, Vector3.up));
            tiles.Add(Shoot(worldCam, rt, tex, "RIDER EYE 1.30 M", "11_rider_eye"));

            Vector3 wideDir = Orbit(forward, right, 35f, 28f);
            float radiusWorld = Mathf.Max(0.05f, solid.extents.magnitude);
            float fitted = Frame(worldCam, solid.center, wideDir, Corners(solid), radiusWorld, 45f, 0.55f);
            worldCam.nearClipPlane = 0.03f;
            worldCam.farClipPlane = Mathf.Max(400f, fitted * 3f);
            tiles.Add(Shoot(worldCam, rt, tex, "IN THE WORLD (RIDE FOG)", "12_in_the_world"));
            UnityEngine.Object.DestroyImmediate(worldCam.gameObject);

            string tileDir = Path.Combine(outDir, model.Id);
            Directory.CreateDirectory(tileDir);
            foreach (Tile tile in tiles) WritePng(Path.Combine(tileDir, tile.Slug + ".png"), tile.Pixels, TileWidth, TileHeight);
            WriteSheet(Path.Combine(outDir, model.Id + ".png"), model, stats, tiles);
            return stats;
        }

        /// <summary>
        /// Front: the side facing the rider. When the rider sits inside the model (the pod, the track), the side ahead of
        /// the rider. Right is always the rider's right, so "right 90" shows what is on the rider's right-hand side.
        /// </summary>
        private static void FrontAndRight(Bounds solid, Vector3 eye, Transform rider, Vector3? given, out Vector3 front, out Vector3 right)
        {
            if (given.HasValue)
            {
                front = Flat(given.Value);
                right = Vector3.Cross(Vector3.up, -front).normalized; // a given front faces the viewer
                return;
            }

            Bounds inflated = solid;
            inflated.Expand(1.0f);
            Vector3 toRider = eye - solid.center;
            toRider.y = 0f;
            bool inside = inflated.Contains(eye) || toRider.sqrMagnitude < 0.01f;
            front = inside ? Flat(rider.forward) : toRider.normalized;
            Vector3 riderLooks = inside ? front : -front;
            right = Vector3.Cross(Vector3.up, riderLooks).normalized;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized;
        }

        /// <summary>The direction from the model to the camera: azimuth toward the rider's right, elevation up.</summary>
        private static Vector3 Orbit(Vector3 front, Vector3 right, float azimuthDeg, float elevationDeg)
        {
            float a = azimuthDeg * Mathf.Deg2Rad, e = elevationDeg * Mathf.Deg2Rad;
            Vector3 horizontal = front * Mathf.Cos(a) + right * Mathf.Sin(a);
            return (horizontal * Mathf.Cos(e) + Vector3.up * Mathf.Sin(e)).normalized;
        }

        /// <summary>Places the camera along dir and moves it in or out until the box fills fill of the frame. Returns the distance.</summary>
        private static float Frame(Camera cam, Vector3 center, Vector3 dir, Vector3[] corners, float radius, float fov, float fill)
        {
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.001f;
            cam.farClipPlane = 10000f;
            float d = radius / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
            float closest = radius * 1.02f + 0.02f; // never inside the model's bounding sphere
            for (int i = 0; i < 12; i++)
            {
                Place(cam, center, dir, d);
                float extent = 0f;
                foreach (Vector3 c in corners)
                {
                    Vector3 v = cam.WorldToViewportPoint(c);
                    extent = Mathf.Max(extent, Mathf.Abs(v.x - 0.5f) * 2f, Mathf.Abs(v.y - 0.5f) * 2f);
                }

                if (extent <= 0f) break;
                float next = Mathf.Max(closest, d * Mathf.Lerp(1f, extent / fill, 0.75f));
                bool settled = Mathf.Abs(next - d) < d * 0.003f;
                d = next;
                if (settled) break;
            }

            Place(cam, center, dir, d);
            cam.nearClipPlane = Mathf.Max(0.01f, d - radius * 1.2f);
            cam.farClipPlane = d + radius * 1.2f + 1f;
            return d;
        }

        private static void Place(Camera cam, Vector3 center, Vector3 dir, float distance)
        {
            cam.transform.SetPositionAndRotation(center + dir * distance, Quaternion.LookRotation(-dir, Vector3.up));
        }

        private static Vector3[] Corners(Bounds b)
        {
            Vector3 c = b.center, e = b.extents;
            var list = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                list[i] = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            }

            return list;
        }

        /// <summary>
        /// Counts what renders in the copy and finds its solid bounds. Glow billboards are left out of the bounds (they
        /// still render): a 6 m halo around a 1.6 m core would shrink the core to a dot in every tile.
        /// </summary>
        private static Stats Measure(Scene scene, out Bounds solid)
        {
            var stats = new Stats();
            var materials = new HashSet<Material>();
            bool any = false, anySolid = false;
            Bounds all = default;
            solid = default;
            foreach (Renderer r in AllIn<Renderer>(scene))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                stats.Renderers++;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null) materials.Add(m);
                }

                stats.Triangles += Triangles(r);
                Bounds b = r.bounds;
                if (b.size == Vector3.zero) continue;
                if (!any) { all = b; any = true; } else all.Encapsulate(b);
                if (r.name.IndexOf("glow", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!anySolid) { solid = b; anySolid = true; } else solid.Encapsulate(b);
            }

            if (!anySolid) solid = all;
            stats.Materials = materials.Count;
            stats.Size = solid.size;
            return stats;
        }

        private static int Triangles(Renderer r)
        {
            if (r is LineRenderer line) return Mathf.Max(0, line.positionCount - 1) * 2;
            if (r is SkinnedMeshRenderer skinned) return skinned.sharedMesh != null ? CountTriangles(skinned.sharedMesh) : 0;
            var filter = r.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null ? CountTriangles(filter.sharedMesh) : 0;
        }

        private static int CountTriangles(Mesh mesh)
        {
            long count = 0;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) == MeshTopology.Triangles) count += mesh.GetIndexCount(s) / 3;
            }

            return (int)count;
        }

        // ---------- copies ----------

        /// <summary>
        /// Gets a copy ready to be judged on its own: lines the ride only draws at run time are hidden (their saved
        /// points are Unity's default and run to the world origin), the network shaders' fade-out with camera distance is
        /// switched off (it is about the camera, not the model, and the orbit tiles stand far back from big models),
        /// and the model's own shader floats are applied.
        /// </summary>
        private static void PrepareStudio(GameObject copy, Dictionary<string, float> floats)
        {
            HideUnsetLines(copy);
            var block = new MaterialPropertyBlock();
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true))
            {
                Material m = r.sharedMaterial;
                if (m == null) continue;
                bool far = m.HasProperty("_FarStart") && m.HasProperty("_FarEnd");
                bool any = far;
                if (floats != null)
                {
                    foreach (string name in floats.Keys) any |= m.HasProperty(name);
                }

                if (!any) continue;
                r.GetPropertyBlock(block);
                if (far)
                {
                    block.SetFloat("_FarStart", 100000f);
                    block.SetFloat("_FarEnd", 200000f);
                }

                ApplyFloats(block, m, floats);
                r.SetPropertyBlock(block);
            }
        }

        private static void ApplyFloats(MaterialPropertyBlock block, Material m, Dictionary<string, float> floats)
        {
            if (floats == null) return;
            foreach (KeyValuePair<string, float> f in floats)
            {
                if (m.HasProperty(f.Key)) block.SetFloat(f.Key, f.Value);
            }
        }

        /// <summary>A line renderer still holding Unity's default points (0,0,0) to (0,0,1) is drawn by a view at run time; hide it.</summary>
        private static void HideUnsetLines(GameObject root)
        {
            foreach (LineRenderer line in root.GetComponentsInChildren<LineRenderer>(true))
            {
                if (line.positionCount == 0)
                {
                    line.enabled = false;
                    continue;
                }

                if (line.positionCount != 2) continue;
                if (line.GetPosition(0) == Vector3.zero && line.GetPosition(1) == Vector3.forward) line.enabled = false;
            }
        }

        /// <summary>A copy of every ride root in a preview scene, which can be staged without touching the open scene.</summary>
        private sealed class World
        {
            public Scene Scene;
            public Transform Pod;
            public Vector3 PodPosition;
            public Quaternion PodRotation;
            public readonly Dictionary<string, Transform> Roots = new Dictionary<string, Transform>();
            public readonly List<(GameObject go, bool active)> Saved = new List<(GameObject, bool)>();
            private readonly List<(Transform t, Vector3 position)> _moved = new List<(Transform, Vector3)>();
            private readonly List<Renderer> _blocked = new List<Renderer>();

            /// <summary>Back to the scene as saved, then: the pod at the rider spot, the stop's own groups live, runtime panels hidden, the model shown.</summary>
            public void Stage(Model model, Transform rider, Transform showAt)
            {
                foreach ((GameObject go, bool active) in Saved) go.SetActive(active);
                foreach ((Transform t, Vector3 position) in _moved) t.position = position;
                _moved.Clear();
                foreach (Renderer r in _blocked) r.SetPropertyBlock(null);
                _blocked.Clear();
                if (Pod != null)
                {
                    Pod.SetPositionAndRotation(PodPosition, PodRotation);
                    if (model.RiderAt != "Pod") Pod.SetPositionAndRotation(rider.position, rider.rotation);
                }

                foreach (string panel in RuntimeOnlyPanels)
                {
                    Transform t = Find(panel);
                    if (t != null) t.gameObject.SetActive(false);
                }

                foreach (string p in model.Paths)
                {
                    Transform t = Find(p);
                    if (t == null) continue;
                    for (Transform up = t; up != null; up = up.parent) up.gameObject.SetActive(true);
                    if (model.ShowHidden)
                    {
                        foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) c.gameObject.SetActive(true);
                    }

                    if (showAt != null)
                    {
                        _moved.Add((t, t.position));
                        t.position = showAt.position;
                    }

                    if (model.MaterialFloats == null && model.MaterialColors == null) continue;
                    var block = new MaterialPropertyBlock();
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r.sharedMaterial == null) continue;
                        r.GetPropertyBlock(block);
                        if (model.MaterialFloats != null) ApplyFloats(block, r.sharedMaterial, model.MaterialFloats);
                        if (model.MaterialColors != null)
                        {
                            foreach (KeyValuePair<string, Color> c in model.MaterialColors)
                            {
                                if (r.sharedMaterial.HasProperty(c.Key)) block.SetColor(c.Key, c.Value);
                            }
                        }

                        r.SetPropertyBlock(block);
                        _blocked.Add(r);
                    }
                }
            }

            public Transform Find(string path)
            {
                int slash = path.IndexOf('/');
                string rootName = slash < 0 ? path : path.Substring(0, slash);
                if (!Roots.TryGetValue(rootName, out Transform root)) return null;
                return slash < 0 ? root : root.Find(path.Substring(slash + 1));
            }
        }

        private static World CopyWorld(Scene scene, Scene into)
        {
            var world = new World { Scene = into };
            var none = new HashSet<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                GameObject holder = NewIn(into, "Holder");
                holder.SetActive(false);
                GameObject copy = CopyVisual(root.transform, holder.transform, "", none);
                if (copy == null)
                {
                    UnityEngine.Object.DestroyImmediate(holder);
                    continue;
                }

                StripBehaviours(holder);
                holder.SetActive(true);
                world.Roots[root.name] = copy.transform;
                if (root.name == "Pod")
                {
                    world.Pod = copy.transform;
                    world.PodPosition = copy.transform.position;
                    world.PodRotation = copy.transform.rotation;
                }
            }

            foreach (Transform root in world.Roots.Values)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) world.Saved.Add((t.gameObject, t.gameObject.activeSelf));
            }

            foreach (Transform root in world.Roots.Values) HideUnsetLines(root.gameObject);
            foreach (TMP_Text text in AllIn<TMP_Text>(into)) text.ForceMeshUpdate();
            return world;
        }

        /// <summary>
        /// Copies src under parent with the same local pose. Whole subtrees are copied in one Instantiate; a subtree that
        /// holds a camera (the XR rig) or a skipped path is rebuilt node by node so those parts stay out.
        /// </summary>
        private static GameObject CopyVisual(Transform src, Transform parent, string rel, HashSet<string> skip)
        {
            if (rel.Length > 0 && skip.Contains(rel)) return null;
            if (src.GetComponent<Camera>() != null) return null;
            if (!NeedsShell(src, rel, skip))
            {
                GameObject copy = UnityEngine.Object.Instantiate(src.gameObject, parent);
                copy.name = src.name;
                return copy;
            }

            var shell = new GameObject(src.name);
            SceneManager.MoveGameObjectToScene(shell, parent.gameObject.scene);
            shell.transform.SetParent(parent, false);
            shell.transform.localPosition = src.localPosition;
            shell.transform.localRotation = src.localRotation;
            shell.transform.localScale = src.localScale;
            var filter = src.GetComponent<MeshFilter>();
            var renderer = src.GetComponent<MeshRenderer>();
            if (filter != null && renderer != null)
            {
                shell.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var r = shell.AddComponent<MeshRenderer>();
                r.sharedMaterials = renderer.sharedMaterials;
                r.enabled = renderer.enabled;
            }

            foreach (Transform child in src) CopyVisual(child, shell.transform, rel.Length == 0 ? child.name : rel + "/" + child.name, skip);
            shell.SetActive(src.gameObject.activeSelf);
            return shell;
        }

        private static bool NeedsShell(Transform t, string rel, HashSet<string> skip)
        {
            foreach (Transform child in t)
            {
                string childRel = rel.Length == 0 ? child.name : rel + "/" + child.name;
                if (skip.Contains(childRel) || child.GetComponent<Camera>() != null) return true;
                if (NeedsShell(child, childRel, skip)) return true;
            }

            return false;
        }

        private static GameObject NewIn(Scene scene, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        /// <summary>
        /// Removes every script from the copies under root except text, and every sound source, so a copy only draws.
        /// A script another one requires goes in a later pass, after the one that requires it.
        /// </summary>
        private static void StripBehaviours(GameObject root)
        {
            var doomed = new List<Component>();
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c is AudioSource || c is AudioListener) doomed.Add(c);
                else if (c is MonoBehaviour && !(c is TMP_Text) && !(c is TMP_SubMesh)) doomed.Add(c);
            }

            for (int pass = 0; pass < 6 && doomed.Count > 0; pass++)
            {
                for (int i = doomed.Count - 1; i >= 0; i--)
                {
                    Component c = doomed[i];
                    if (c == null || IsRequiredBySibling(c, doomed)) continue;
                    UnityEngine.Object.DestroyImmediate(c);
                    doomed.RemoveAt(i);
                }
            }
        }

        private static bool IsRequiredBySibling(Component target, List<Component> doomed)
        {
            foreach (Component other in target.GetComponents<Component>())
            {
                if (other == null || other == target || !doomed.Contains(other)) continue;
                foreach (RequireComponent req in (RequireComponent[])Attribute.GetCustomAttributes(other.GetType(), typeof(RequireComponent), true))
                {
                    foreach (Type t in new[] { req.m_Type0, req.m_Type1, req.m_Type2 })
                    {
                        if (t != null && t.IsInstanceOfType(target)) return true;
                    }
                }
            }

            return false;
        }

        private static List<T> AllIn<T>(Scene scene) where T : Component
        {
            var list = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects()) list.AddRange(root.GetComponentsInChildren<T>(true));
            return list;
        }

        // ---------- rendering ----------

        private static Camera NewCamera(Scene scene, Color background, RenderTexture rt)
        {
            GameObject go = NewIn(scene, "SheetCamera");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.scene = scene;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.aspect = (float)TileWidth / TileHeight;
            cam.allowMSAA = true;
            cam.allowHDR = false;
            cam.targetTexture = rt;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
            return cam;
        }

        private static Tile Shoot(Camera cam, RenderTexture rt, Texture2D tex, string label, string slug)
        {
            cam.targetTexture = rt;
            cam.aspect = (float)TileWidth / TileHeight;
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, TileWidth, TileHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i].a = 255;
            return new Tile { Label = label, Slug = slug, Pixels = px };
        }

        private static void WritePng(string path, Color32[] pixels, int width, int height)
        {
            var t = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                t.SetPixels32(pixels);
                t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(t);
            }
        }

        private static void WriteSheet(string path, Model model, Stats stats, List<Tile> tiles)
        {
            int width = TileWidth * Columns;
            int height = TileHeight * Rows + HeaderHeight;
            var px = new Color32[width * height];
            var ink = new Color32(230, 236, 245, 255);
            var dim = new Color32(150, 165, 185, 255);
            Fill(px, width, height, 0, 0, width, height, new Color32(14, 17, 24, 255));

            for (int i = 0; i < tiles.Count && i < Columns * Rows; i++)
            {
                int col = i % Columns, row = i / Columns;
                int left = col * TileWidth;
                int top = HeaderHeight + row * TileHeight;
                Blit(px, width, height, tiles[i].Pixels, left, top);
                int labelWidth = TextWidth(tiles[i].Label, 3) + 24;
                Shade(px, width, height, left, top, labelWidth, 38, 0.65f);
                DrawText(px, width, height, left + 12, top + 9, tiles[i].Label, 3, ink);
                // a thin dark gutter between tiles
                Fill(px, width, height, left, top, 2, TileHeight, new Color32(14, 17, 24, 255));
                Fill(px, width, height, left, top, TileWidth, 2, new Color32(14, 17, 24, 255));
            }

            DrawText(px, width, height, 24, 22, model.Id.ToUpperInvariant(), 6, new Color32(120, 220, 255, 255));
            DrawText(px, width, height, 24 + TextWidth(model.Id, 6) + 36, 34, model.Note, 3, ink);
            string line = string.Format(CultureInfo.InvariantCulture,
                "TRIS {0:N0}   RENDERERS {1}   MATERIALS {2}   SIZE {3:0.00} X {4:0.00} X {5:0.00} M (W X H X D, GLOW LEFT OUT)   RIDER AT {6}   SOURCE {7}",
                stats.Triangles, stats.Renderers, stats.Materials, stats.Size.x, stats.Size.y, stats.Size.z, model.RiderAt, string.Join(" + ", model.Paths));
            DrawText(px, width, height, 24, 98, line, 3, dim);
            string mode = Application.isPlaying
                ? "PLAY MODE AT " + Time.time.ToString("0.0", CultureInfo.InvariantCulture) + " S"
                : "EDIT MODE: THE SAVED SCENE (RUN-TIME GEOMETRY SHOWS AS SAVED)";
            DrawText(px, width, height, width - TextWidth(mode, 3) - 24, 34, mode, 3, Application.isPlaying ? new Color32(255, 200, 90, 255) : dim);
            WritePng(path, px, width, height);
        }

        // Pixel helpers. x and top are measured from the top-left; Unity textures start at the bottom-left.
        private static void Blit(Color32[] dst, int width, int height, Color32[] src, int left, int top)
        {
            for (int y = 0; y < TileHeight; y++)
            {
                int dy = height - 1 - (top + (TileHeight - 1 - y));
                if (dy < 0 || dy >= height) continue;
                Array.Copy(src, y * TileWidth, dst, dy * width + left, TileWidth);
            }
        }

        private static void Fill(Color32[] px, int width, int height, int left, int top, int w, int h, Color32 c)
        {
            for (int y = top; y < top + h && y < height; y++)
            {
                int row = (height - 1 - y) * width;
                for (int x = left; x < left + w && x < width; x++) px[row + x] = c;
            }
        }

        private static void Shade(Color32[] px, int width, int height, int left, int top, int w, int h, float amount)
        {
            for (int y = top; y < top + h && y < height; y++)
            {
                int row = (height - 1 - y) * width;
                for (int x = left; x < left + w && x < width; x++)
                {
                    Color32 c = px[row + x];
                    px[row + x] = new Color32((byte)(c.r * (1f - amount)), (byte)(c.g * (1f - amount)), (byte)(c.b * (1f - amount)), 255);
                }
            }
        }

        private static int TextWidth(string text, int scale) => text.Length * 6 * scale;

        private static void DrawText(Color32[] px, int width, int height, int left, int top, string text, int scale, Color32 color)
        {
            int x = left;
            foreach (char raw in text.ToUpperInvariant())
            {
                if (!Glyphs.TryGetValue(raw, out byte[] rows)) rows = Glyphs['?'];
                for (int gy = 0; gy < 7; gy++)
                {
                    for (int gx = 0; gx < 5; gx++)
                    {
                        if ((rows[gy] & (1 << (4 - gx))) == 0) continue;
                        Fill(px, width, height, x + gx * scale, top + gy * scale, scale, scale, color);
                    }
                }

                x += 6 * scale;
                if (x > width - 6 * scale) break;
            }
        }

        // A 5 x 7 pixel font, enough for labels: one byte per row, the high five bits left to right.
        private static readonly Dictionary<char, byte[]> Glyphs = new Dictionary<char, byte[]>
        {
            { ' ', new byte[] { 0, 0, 0, 0, 0, 0, 0 } },
            { 'A', new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 } },
            { 'B', new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 } },
            { 'C', new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 } },
            { 'D', new byte[] { 0b11100, 0b10010, 0b10001, 0b10001, 0b10001, 0b10010, 0b11100 } },
            { 'E', new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 } },
            { 'F', new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 } },
            { 'G', new byte[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01111 } },
            { 'H', new byte[] { 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 } },
            { 'I', new byte[] { 0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 } },
            { 'J', new byte[] { 0b00111, 0b00010, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100 } },
            { 'K', new byte[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 } },
            { 'L', new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 } },
            { 'M', new byte[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 } },
            { 'N', new byte[] { 0b10001, 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001 } },
            { 'O', new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 } },
            { 'P', new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 } },
            { 'Q', new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 } },
            { 'R', new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 } },
            { 'S', new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 } },
            { 'T', new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 } },
            { 'U', new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 } },
            { 'V', new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 } },
            { 'W', new byte[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b10101, 0b01010 } },
            { 'X', new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001 } },
            { 'Y', new byte[] { 0b10001, 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100 } },
            { 'Z', new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111 } },
            { '0', new byte[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110 } },
            { '1', new byte[] { 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 } },
            { '2', new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111 } },
            { '3', new byte[] { 0b11111, 0b00010, 0b00100, 0b00010, 0b00001, 0b10001, 0b01110 } },
            { '4', new byte[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010 } },
            { '5', new byte[] { 0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110 } },
            { '6', new byte[] { 0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110 } },
            { '7', new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000 } },
            { '8', new byte[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 } },
            { '9', new byte[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00010, 0b01100 } },
            { '-', new byte[] { 0, 0, 0, 0b11111, 0, 0, 0 } },
            { '_', new byte[] { 0, 0, 0, 0, 0, 0, 0b11111 } },
            { '.', new byte[] { 0, 0, 0, 0, 0, 0b01100, 0b01100 } },
            { ',', new byte[] { 0, 0, 0, 0, 0b01100, 0b00100, 0b01000 } },
            { ':', new byte[] { 0, 0b01100, 0b01100, 0, 0b01100, 0b01100, 0 } },
            { ';', new byte[] { 0, 0b01100, 0b01100, 0, 0b01100, 0b00100, 0b01000 } },
            { '/', new byte[] { 0, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0 } },
            { '(', new byte[] { 0b00010, 0b00100, 0b01000, 0b01000, 0b01000, 0b00100, 0b00010 } },
            { ')', new byte[] { 0b01000, 0b00100, 0b00010, 0b00010, 0b00010, 0b00100, 0b01000 } },
            { '+', new byte[] { 0, 0b00100, 0b00100, 0b11111, 0b00100, 0b00100, 0 } },
            { '=', new byte[] { 0, 0, 0b11111, 0, 0b11111, 0, 0 } },
            { '\'', new byte[] { 0b01100, 0b00100, 0b01000, 0, 0, 0, 0 } },
            { '"', new byte[] { 0b01010, 0b01010, 0, 0, 0, 0, 0 } },
            { '!', new byte[] { 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0, 0b00100 } },
            { '?', new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0, 0b00100 } },
            { '#', new byte[] { 0b01010, 0b01010, 0b11111, 0b01010, 0b11111, 0b01010, 0b01010 } },
            { '%', new byte[] { 0b11000, 0b11001, 0b00010, 0b00100, 0b01000, 0b10011, 0b00011 } },
        };

        // ---------- index ----------

        private static void WriteIndex(string outDir, List<(Model model, Stats stats)> rows, List<string> failed, List<string> uncovered)
        {
            var md = new StringBuilder();
            md.AppendLine("# Model sheets");
            md.AppendLine();
            md.AppendLine("Every model in the Neural Ride from all sides, shot by `Convergence/Capture Model Sheets` (`Tools/Editor/RideModelSheets.cs`).");
            md.AppendLine("Tiles: row 1 front, front-right 45, right 90, back-right 135; row 2 back, back-left, left, front-left; row 3 top, low hero 3/4, rider eye 1.30 m, in the world.");
            md.AppendLine("\"Front\" faces the rider. The first ten tiles show the model alone; the last two show it in the ride. Do not edit by hand: re-run the capture.");
            md.AppendLine();
            md.AppendLine("Shot " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " from " + SceneManager.GetActiveScene().path
                + (Application.isPlaying ? " in Play Mode at " + Time.time.ToString("0.0", CultureInfo.InvariantCulture) + " s." : " in Edit mode (the saved scene; geometry made at run time shows as saved)."));
            md.AppendLine();
            md.AppendLine("| Model | What it is | Tris | Renderers | Materials | Size (m) | Sheet |");
            md.AppendLine("|---|---|---:|---:|---:|---|---|");
            foreach ((Model model, Stats stats) in rows)
            {
                md.AppendLine(string.Format(CultureInfo.InvariantCulture, "| `{0}` | {1} | {2:N0} | {3} | {4} | {5:0.0} × {6:0.0} × {7:0.0} | [{0}.png]({0}.png) |",
                    model.Id, model.Note, stats.Triangles, stats.Renderers, stats.Materials, stats.Size.x, stats.Size.y, stats.Size.z));
            }

            if (failed.Count > 0)
            {
                md.AppendLine();
                md.AppendLine("Failed:");
                foreach (string f in failed) md.AppendLine("- " + f);
            }

            if (uncovered.Count > 0)
            {
                md.AppendLine();
                md.AppendLine("Not covered by any sheet:");
                foreach (string u in uncovered) md.AppendLine("- " + u);
            }

            File.WriteAllText(Path.Combine(outDir, "index.md"), md.ToString());
        }

        // ---------- scene lookups ----------

        private static Transform FindPath(Scene scene, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            int slash = path.IndexOf('/');
            string rootName = slash < 0 ? path : path.Substring(0, slash);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != rootName) continue;
                return slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
            }

            return null;
        }

        private static Color RideBackground(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
                {
                    if (cam.CompareTag("MainCamera")) return cam.backgroundColor;
                }
            }

            return new Color(0.012f, 0.016f, 0.030f);
        }

        private static string Slug(string label)
        {
            var sb = new StringBuilder();
            foreach (char c in label.ToLowerInvariant()) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString().Trim('_').Replace("__", "_");
        }

        private static string ProjectRoot() => Directory.GetParent(Application.dataPath).FullName;
    }
}
