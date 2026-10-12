using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Sightline, viewing-cone, and text-size check for Level 1. At every viewpoint (RideViewpoint: the dock, each stop,
    /// the rim, the drop, the end) it puts a virtual eye on the pod seat at 1.15, 1.30 and 1.45 m and casts sight lines
    /// to the centre and edges of every panel and sign the rider must read there. A target fails when real geometry (a
    /// triangle of a pod mesh or of a region the streamer shows at that segment) is hit first. Panels must sit inside
    /// the viewing cone and text must be big enough to read. It never moves or changes anything in the scene: the pod's
    /// meshes are placed at each viewpoint by a matrix. Development tooling only; game assemblies never reference it.
    ///
    /// What counts as a blocker: solid, non-additive meshes of the pod (minus panels that are hidden at that viewpoint
    /// and the XR rig) and of the regions visible at that segment. Text, glows, and the target's own parts never block.
    /// </summary>
    public static class RideSightlines
    {
        /// <summary>When true, Assert() fails the build on any problem; when false it only logs a warning.</summary>
        public static readonly bool ThrowOnProblems = true;

        // The nominal eye the layout is built for (PodSeat lifts the rig so the real eye sits here) and the cone around it.
        private const float NominalEye = 1.30f;
        private const float CenterAzimuth = 35f, CenterElevationDown = 20f, CenterElevationUp = 22f;
        private const float EdgeSlack = 5f;                  // panel edges may reach this much past the centre limits
        private const float MinTextCapDegrees = 1.5f;        // about 30 px at the Quest 2's roughly 20 px per degree
        private const float TmpCapPerFontSize = 0.72f * 0.1f; // cap height in metres per unit of fontSize (see NeuralRideBuilder.Text)
        private const float HitEpsilon = 0.02f;

        private static readonly float[] EyeHeights = { 1.15f, 1.30f, 1.45f };

        private sealed class Occluder
        {
            public Renderer Renderer;
            public Bounds Bounds;
            public Vector3[] Triangles; // world space, three per triangle
            public string Path;
        }

        private sealed class Target
        {
            public string Label;     // what a person would call it: "network diagram panel", "label 'fire at 2.0'"
            public Transform Root;   // everything under this never blocks this target
            public bool IsPanel;
            public bool IsDash;
            public Vector3 Center;   // for the cone check
            public float FontSize = -1f;
            public float TextScale = 1f;
            public readonly List<Vector3> Points = new List<Vector3>();
        }

        // ---------- entry points ----------

        /// <summary>Fails the build (or warns, see ThrowOnProblems) listing every problem in the open ride scene.</summary>
        public static void Assert()
        {
            List<string> problems = FindBlockers();
            if (problems.Count == 0)
            {
                Debug.Log("[RideSightlines] Clear at 1.15, 1.30 and 1.45 m for every stop; every panel is inside the viewing cone and the text is readable.");
                return;
            }

            string report = "Ride sightline check found " + problems.Count + " problem(s):\n  - " + string.Join("\n  - ", problems);
            if (ThrowOnProblems) throw new InvalidOperationException(report);
            Debug.LogWarning("[RideSightlines] " + report);
        }

        /// <summary>Returns one line per problem. An empty list means the open ride scene passes. Does not change the scene.</summary>
        public static List<string> FindBlockers() => FindBlockers(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        /// <summary>The same check on any loaded scene, for example one a test opens additively.</summary>
        public static List<string> FindBlockers(UnityEngine.SceneManagement.Scene scene)
        {
            var problems = new List<string>();
            GameObject pod = null;
            RegionStreamer streamer = null;
            var regions = new Dictionary<int, GameObject>();
            var viewpoints = new List<RideViewpoint>();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "Pod") pod = go;
                if (go.name.StartsWith("Region_", StringComparison.Ordinal) && int.TryParse(go.name.Substring(7), out int n)) regions[n] = go;
                if (streamer == null) streamer = go.GetComponentInChildren<RegionStreamer>(true);
                viewpoints.AddRange(go.GetComponentsInChildren<RideViewpoint>(true));
            }

            if (pod == null)
            {
                problems.Add("No root object named 'Pod' in the open scene. Run Convergence > Build Neural Ride first.");
                return problems;
            }

            Transform seat = pod.transform.Find("SeatAnchor");
            if (seat == null)
            {
                problems.Add("Pod has no child 'SeatAnchor', so there is no seat to place the eye.");
                return problems;
            }

            if (viewpoints.Count == 0)
            {
                problems.Add("No RideViewpoint in the open scene. Run Convergence > Build Neural Ride first.");
                return problems;
            }

            var meshCache = new Dictionary<Mesh, (Vector3[] vertices, int[] triangles)>();
            foreach (RideViewpoint vp in viewpoints) CheckViewpoint(pod.transform, seat, vp, streamer, regions, meshCache, problems);
            return problems;
        }

        /// <summary>The regions the streamer shows at a viewpoint's segment (all of them when there is no streamer).</summary>
        public static List<GameObject> VisibleRegions(RideViewpoint vp, RegionStreamer streamer, Dictionary<int, GameObject> regions)
        {
            int mask = streamer != null ? streamer.MaskFor(vp.segment) : ~0;
            var visible = new List<GameObject>();
            foreach (KeyValuePair<int, GameObject> r in regions)
            {
                if ((mask & (1 << r.Key)) != 0) visible.Add(r.Value);
            }

            return visible;
        }

        // ---------- one viewpoint ----------

        private static void CheckViewpoint(Transform pod, Transform seat, RideViewpoint vp, RegionStreamer streamer, Dictionary<int, GameObject> regions,
            Dictionary<Mesh, (Vector3[] vertices, int[] triangles)> meshCache, List<string> problems)
        {
            string stop = vp.label;
            Matrix4x4 podAtStop = Matrix4x4.TRS(vp.transform.position, vp.transform.rotation, Vector3.one);
            Matrix4x4 podToStop = podAtStop * pod.worldToLocalMatrix;

            var occluders = new List<Occluder>();
            foreach (Renderer r in pod.GetComponentsInChildren<Renderer>(true))
            {
                if (!CanBlock(r) || IsHiddenPodPart(r.transform, pod, seat, vp)) continue;
                AddOccluder(occluders, r, podToStop * r.localToWorldMatrix, meshCache);
            }

            foreach (GameObject region in VisibleRegions(vp, streamer, regions))
            {
                foreach (Renderer r in region.GetComponentsInChildren<Renderer>(true))
                {
                    if (CanBlock(r)) AddOccluder(occluders, r, r.localToWorldMatrix, meshCache);
                }
            }

            var targets = new List<Target>();
            Transform screen = pod.Find("Dash/Screen");
            if (screen != null) AddGrid(targets, "dash screen", pod.Find("Dash"), screen, podToStop * screen.localToWorldMatrix, true);
            foreach (string path in vp.podPanels)
            {
                Transform panel = pod.Find(path);
                if (panel == null)
                {
                    problems.Add(stop + ": the pod has no " + path + ".");
                    continue;
                }

                int before = targets.Count;
                AddPanel(targets, panel, podToStop);
                // A pod panel is a hand panel (the weight grid at the lap) or the end card: the reading cone is for the world.
                for (int i = before; i < targets.Count; i++) targets[i].IsDash = true;
                foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true)) AddLabel(targets, text, podToStop);
            }

            foreach (Transform target in vp.targets)
            {
                if (target == null) continue;
                AddPanel(targets, target, Matrix4x4.identity);
                foreach (TMP_Text text in target.GetComponentsInChildren<TMP_Text>(true)) AddLabel(targets, text, Matrix4x4.identity);
            }

            Vector3 seatInPod = pod.InverseTransformPoint(seat.position);
            foreach (Target target in targets)
            {
                CheckCone(stop, target, podAtStop, seatInPod, problems);
                CheckText(stop, target, podAtStop, seatInPod, problems);
                CheckSight(stop, target, podAtStop, seatInPod, occluders, problems);
            }
        }

        // ---------- what can block, what is a target ----------

        private static bool CanBlock(Renderer r)
        {
            if (!r.enabled || r is LineRenderer || r.GetComponent<TMP_Text>() != null) return false;
            Material m = r.sharedMaterial;
            if (m == null) return false;
            // The networks' wires and neurons, the rails and every glow are additive light: they never hide anything.
            if (m.shader != null && m.shader.name.StartsWith("Convergence/Neural", StringComparison.Ordinal)) return false;
            bool additive = m.HasProperty("_DstBlend") && Mathf.Approximately(m.GetFloat("_DstBlend"), (float)UnityEngine.Rendering.BlendMode.One);
            return !additive;
        }

        // The end card and the weight grid only show at their moment (a viewpoint lists them in podPanels when they are
        // live); the XR rig is the rider's own hardware.
        private static bool IsHiddenPodPart(Transform t, Transform pod, Transform seat, RideViewpoint vp)
        {
            for (Transform c = t; c != null && c != pod; c = c.parent)
            {
                if (c.name == "EndCard" && Array.IndexOf(vp.podPanels, "EndCard") < 0) return true;
                if (c.name == "WeightGrid" && Array.IndexOf(vp.podPanels, "Dash/WeightGrid") < 0) return true;
                if (c.parent == seat && c.GetComponentInChildren<Camera>(true) != null) return true;
            }

            return false;
        }

        private static bool IsUnder(Transform t, string ancestorName, Transform stop)
        {
            for (Transform c = t; c != null && c != stop; c = c.parent)
            {
                if (c.name == ancestorName) return true;
            }

            return false;
        }

        private static void AddOccluder(List<Occluder> list, Renderer r, Matrix4x4 toWorld, Dictionary<Mesh, (Vector3[] vertices, int[] triangles)> cache)
        {
            var filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null) return;

            if (!cache.TryGetValue(mesh, out (Vector3[] vertices, int[] triangles) data))
            {
                data = (mesh.vertices, mesh.triangles);
                cache[mesh] = data;
            }

            if (data.triangles.Length == 0) return;
            var triangles = new Vector3[data.triangles.Length];
            var bounds = new Bounds(toWorld.MultiplyPoint3x4(data.vertices[data.triangles[0]]), Vector3.zero);
            for (int i = 0; i < data.triangles.Length; i++)
            {
                Vector3 p = toWorld.MultiplyPoint3x4(data.vertices[data.triangles[i]]);
                triangles[i] = p;
                bounds.Encapsulate(p);
            }

            bounds.Expand(0.002f);
            list.Add(new Occluder { Renderer = r, Bounds = bounds, Triangles = triangles, Path = PathOf(r.transform) });
        }

        /// <summary>
        /// A panel or a set piece the rider must see. A board (a child named Plate or Back) is sampled as a 3 by 3 grid on
        /// its face; anything else by the centre and four points across its bounds.
        /// </summary>
        private static void AddPanel(List<Target> targets, Transform root, Matrix4x4 toWorld)
        {
            Transform board = root.Find("Plate") ?? root.Find("Back");
            if (board != null && board.GetComponent<MeshFilter>() != null)
            {
                AddBoard(targets, root.name + " panel", root, board, toWorld * board.localToWorldMatrix);
                return;
            }

            Bounds? bounds = null;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer) || r.GetComponent<TMP_Text>() != null || !CanBlock(r)) continue;
                if (bounds == null) bounds = r.bounds;
                else
                {
                    Bounds b = bounds.Value;
                    b.Encapsulate(r.bounds);
                    bounds = b;
                }
            }

            if (bounds == null) return;
            Bounds box = bounds.Value;
            var target = new Target { Label = root.name, Root = root, IsPanel = true, Center = toWorld.MultiplyPoint3x4(box.center) };
            target.Points.Add(target.Center);
            Vector3 e = box.extents * 0.6f;
            foreach (Vector3 d in new[] { new Vector3(e.x, 0, 0), new Vector3(-e.x, 0, 0), new Vector3(0, e.y, 0), new Vector3(0, -e.y, 0) })
            {
                target.Points.Add(toWorld.MultiplyPoint3x4(box.center + d));
            }

            targets.Add(target);
        }

        // A 3 x 3 grid at 80% of a board's face (a scaled cube or quad): the face is the axis with the least world size.
        private static void AddBoard(List<Target> targets, string label, Transform root, Transform board, Matrix4x4 toWorld)
        {
            Mesh mesh = board.GetComponent<MeshFilter>().sharedMesh;
            Bounds b = mesh.bounds;
            Vector3 scale = board.lossyScale;
            Vector3 size = Vector3.Scale(b.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            int flat = size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
            var target = new Target { Label = label, Root = root, IsPanel = true, Center = toWorld.MultiplyPoint3x4(b.center) };
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector3 local = b.center;
                    int a = 0;
                    for (int axis = 0; axis < 3; axis++)
                    {
                        if (axis == flat)
                        {
                            local[axis] = b.min[axis]; // the face toward the rider (boards face -z)
                            continue;
                        }

                        local[axis] += (a == 0 ? i : j) * b.extents[axis] * 0.8f;
                        a++;
                    }

                    target.Points.Add(toWorld.MultiplyPoint3x4(local));
                }
            }

            targets.Add(target);
        }

        private static Matrix4x4 Matrix(Transform t) => t != null ? t.localToWorldMatrix : Matrix4x4.identity;

        // A 3 x 3 grid at 80% of the flat mesh's two larger extents, on its real (possibly rotated) surface.
        private static void AddGrid(List<Target> targets, string label, Transform root, Transform quad, Matrix4x4 toWorld, bool dash)
        {
            if (quad == null) return;
            var filter = quad.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null) return;

            Bounds b = mesh.bounds;
            int flat = b.size.x <= b.size.y && b.size.x <= b.size.z ? 0 : b.size.y <= b.size.z ? 1 : 2; // the axis with no thickness
            var target = new Target { Label = label, Root = root, IsPanel = true, IsDash = dash, Center = toWorld.MultiplyPoint3x4(b.center) };
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector3 local = b.center;
                    int a = 0;
                    for (int axis = 0; axis < 3; axis++)
                    {
                        if (axis == flat) continue;
                        float offset = (a == 0 ? i : j) * b.extents[axis] * 0.8f;
                        local[axis] += offset;
                        a++;
                    }

                    target.Points.Add(toWorld.MultiplyPoint3x4(local));
                }
            }

            targets.Add(target);
        }

        private static void AddCore(List<Target> targets, Transform neuron, Transform station)
        {
            Transform shell = neuron.Find("Core/Shell");
            if (shell == null) return;
            Vector3 centre = shell.position;
            float radius = shell.lossyScale.x * 0.5f * 0.6f;
            var target = new Target { Label = "neuron core", Root = neuron.Find("Core"), IsPanel = true, Center = centre };
            target.Points.Add(centre);
            foreach (Vector3 dir in new[] { station.right, -station.right, Vector3.up, Vector3.down }) target.Points.Add(centre + dir * radius);
            targets.Add(target);
        }

        private static void AddBoard(List<Target> targets, Transform board)
        {
            var target = new Target { Label = "case board", Root = board, IsPanel = true, Center = board.position };
            for (int i = 0; i < board.childCount; i++)
            {
                Transform frame = board.GetChild(i).Find("Frame");
                if (frame != null && frame.gameObject.activeInHierarchy) target.Points.Add(frame.position);
            }

            // The outer corners of the board, so something across one edge is caught too.
            foreach (Vector2 c in new[] { new Vector2(-0.4f, 0.4f), new Vector2(0.4f, 0.4f), new Vector2(-0.4f, -0.4f), new Vector2(0.4f, -0.4f) })
            {
                target.Points.Add(board.TransformPoint(new Vector3(c.x, c.y, 0f)));
            }

            targets.Add(target);
        }

        // Any text that already has words in it at build time, sampled at its centre and both ends.
        private static void AddLabel(List<Target> targets, TMP_Text text, Matrix4x4 toWorld)
        {
            if (string.IsNullOrWhiteSpace(text.text)) return; // filled in at runtime: nothing to measure yet
            text.ForceMeshUpdate();
            Bounds local = text.bounds;
            if (local.size == Vector3.zero) return;

            Transform t = text.transform;
            Vector3 centre = toWorld.MultiplyPoint3x4(t.TransformPoint(local.center));
            Vector3 along = toWorld.MultiplyVector(t.right) * local.extents.x * t.lossyScale.x * 0.9f;
            var target = new Target
            {
                Label = "label '" + Shorten(text.text) + "'",
                Root = t,
                IsPanel = false,
                Center = centre,
                FontSize = text.fontSize,
                TextScale = t.lossyScale.y
            };
            target.Points.Add(centre);
            target.Points.Add(centre + along);
            target.Points.Add(centre - along);
            targets.Add(target);
        }

        // ---------- checks ----------

        private static void CheckCone(string stop, Target t, Matrix4x4 podAtStop, Vector3 seatInPod, List<string> problems)
        {
            if (t.IsDash || !t.IsPanel) return;
            var eye = new Vector3(seatInPod.x, NominalEye, seatInPod.z);
            Matrix4x4 toPod = podAtStop.inverse;
            Angles(toPod.MultiplyPoint3x4(t.Center), eye, out float az, out float el);
            if (Mathf.Abs(az) > CenterAzimuth || el > CenterElevationUp || el < -CenterElevationDown)
            {
                problems.Add(stop + ": the " + t.Label + " is centred " + Deg(az) + " across and " + Deg(el) + " up; the viewing cone is +-"
                    + CenterAzimuth + " across and " + (-CenterElevationDown) + " to +" + CenterElevationUp + " up.");
            }

            foreach (Vector3 p in t.Points)
            {
                Angles(toPod.MultiplyPoint3x4(p), eye, out float pa, out float pe);
                if (Mathf.Abs(pa) > CenterAzimuth + EdgeSlack || pe > CenterElevationUp + EdgeSlack || pe < -CenterElevationDown - EdgeSlack)
                {
                    problems.Add(stop + ": the " + t.Label + " reaches " + Deg(pa) + " across and " + Deg(pe) + " up, beyond the cone plus " + EdgeSlack + " degrees of slack.");
                    break;
                }
            }
        }

        private static void CheckText(string stop, Target t, Matrix4x4 podAtStop, Vector3 seatInPod, List<string> problems)
        {
            if (t.FontSize <= 0f) return;
            Vector3 eye = podAtStop.MultiplyPoint3x4(new Vector3(seatInPod.x, NominalEye, seatInPod.z));
            float distance = Vector3.Distance(eye, t.Center);
            float cap = t.FontSize * TmpCapPerFontSize * t.TextScale;
            float degrees = 2f * Mathf.Atan(cap * 0.5f / Mathf.Max(0.1f, distance)) * Mathf.Rad2Deg;
            if (degrees < MinTextCapDegrees)
            {
                problems.Add(stop + ": the " + t.Label + " is " + degrees.ToString("0.0", CultureInfo.InvariantCulture) + " degrees tall at the eye (needs "
                    + MinTextCapDegrees.ToString("0.0", CultureInfo.InvariantCulture) + "). Make it bigger or bring it closer.");
            }
        }

        private static void CheckSight(string stop, Target t, Matrix4x4 podAtStop, Vector3 seatInPod, List<Occluder> occluders, List<string> problems)
        {
            var hits = new Dictionary<string, int>();
            var eyesFor = new Dictionary<string, SortedSet<string>>();
            foreach (float height in EyeHeights)
            {
                Vector3 eye = podAtStop.MultiplyPoint3x4(new Vector3(seatInPod.x, height, seatInPod.z));
                foreach (Vector3 point in t.Points)
                {
                    Vector3 offset = point - eye;
                    float distance = offset.magnitude;
                    if (distance < 1e-3f) continue;
                    Occluder first = FirstHit(occluders, t, eye, offset / distance, distance - HitEpsilon);
                    if (first == null) continue;
                    hits.TryGetValue(first.Path, out int n);
                    hits[first.Path] = n + 1;
                    if (!eyesFor.TryGetValue(first.Path, out SortedSet<string> eyes)) eyesFor[first.Path] = eyes = new SortedSet<string>();
                    eyes.Add(height.ToString("0.00", CultureInfo.InvariantCulture));
                }
            }

            foreach (KeyValuePair<string, int> hit in hits)
            {
                problems.Add(stop + ": the " + t.Label + " is hidden by " + hit.Key + " (" + hit.Value + " of " + (t.Points.Count * EyeHeights.Length)
                    + " sight lines, eye " + string.Join(" and ", eyesFor[hit.Key]) + " m).");
            }
        }

        private static Occluder FirstHit(List<Occluder> occluders, Target t, Vector3 origin, Vector3 dir, float maxDistance)
        {
            Occluder best = null;
            float bestT = maxDistance;
            var ray = new Ray(origin, dir);
            foreach (Occluder o in occluders)
            {
                if (t.Root != null && IsUnderRoot(o.Renderer.transform, t.Root)) continue;
                if (!o.Bounds.IntersectRay(ray, out float boxT) || boxT > bestT) continue;
                for (int i = 0; i < o.Triangles.Length; i += 3)
                {
                    if (RayTriangle(origin, dir, o.Triangles[i], o.Triangles[i + 1], o.Triangles[i + 2], out float hit) && hit > 0f && hit < bestT)
                    {
                        bestT = hit;
                        best = o;
                    }
                }
            }

            return best;
        }

        /// <summary>Moller-Trumbore, two-sided. Public so the geometry can be tested on its own.</summary>
        public static bool RayTriangle(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0f;
            Vector3 e1 = b - a, e2 = c - a;
            Vector3 p = Vector3.Cross(d, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-9f) return false;
            float inv = 1f / det;
            Vector3 s = o - a;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0f || u > 1f) return false;
            Vector3 q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(d, q) * inv;
            if (v < 0f || u + v > 1f) return false;
            t = Vector3.Dot(e2, q) * inv;
            return true;
        }

        // ---------- helpers ----------

        private static void Angles(Vector3 point, Vector3 eye, out float azimuth, out float elevation)
        {
            Vector3 d = point - eye;
            azimuth = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            elevation = Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        }

        private static string Deg(float v) => v.ToString("0", CultureInfo.InvariantCulture) + " degrees";

        private static bool IsUnderRoot(Transform t, Transform root)
        {
            for (Transform c = t; c != null; c = c.parent)
            {
                if (c == root) return true;
            }

            return false;
        }

        private static string Shorten(string s)
        {
            s = s.Replace("\n", " ");
            return s.Length > 24 ? s.Substring(0, 24) + "..." : s;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }

    /// <summary>
    /// Rough render budget per viewpoint of Level 1 (WP6, WP8): draw calls and triangles of what is visible there (the
    /// pod without its hidden panels, the track, and the regions the streamer shows at that segment). A renderer counts
    /// as one draw per material. The Quest 2 budget is 100 draw calls and 100k triangles. The device measurement (OVR
    /// Metrics) is the real number; this catches regressions in the Editor.
    /// </summary>
    public static class RideBudget
    {
        public const int MaxDrawCalls = 100;
        public const int MaxTriangles = 100000;

        /// <summary>When true, Report() fails the build over budget.</summary>
        public const bool ThrowOverBudget = true;

        /// <summary>One line per viewpoint: "label: N draw calls, M triangles".</summary>
        public static List<string> Measure(out List<string> over) => Measure(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), out over);

        /// <summary>The same on any loaded scene, for example one a test opens additively.</summary>
        public static List<string> Measure(UnityEngine.SceneManagement.Scene scene, out List<string> over)
        {
            over = new List<string>();
            var lines = new List<string>();
            GameObject pod = null, track = null;
            RegionStreamer streamer = null;
            var regions = new Dictionary<int, GameObject>();
            var viewpoints = new List<RideViewpoint>();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "Pod") pod = go;
                if (go.name == "Track") track = go;
                if (go.name.StartsWith("Region_", StringComparison.Ordinal) && int.TryParse(go.name.Substring(7), out int n)) regions[n] = go;
                if (streamer == null) streamer = go.GetComponentInChildren<RegionStreamer>(true);
                viewpoints.AddRange(go.GetComponentsInChildren<RideViewpoint>(true));
            }

            if (pod == null) return lines;
            foreach (RideViewpoint vp in viewpoints)
            {
                int draws = 0, triangles = 0;
                Count(pod, ref draws, ref triangles, vp);
                if (track != null) Count(track, ref draws, ref triangles, null);
                foreach (GameObject region in RideSightlines.VisibleRegions(vp, streamer, regions)) Count(region, ref draws, ref triangles, null);
                bool ok = draws <= MaxDrawCalls && triangles <= MaxTriangles;
                string line = vp.label + ": about " + draws + " draw calls and " + triangles + " triangles (" + (ok ? "within budget" : "OVER BUDGET") + ")";
                lines.Add(line);
                if (!ok) over.Add(line);
            }

            return lines;
        }

        public static void Report()
        {
            List<string> lines = Measure(out List<string> over);
            if (lines.Count == 0)
            {
                Debug.LogWarning("[RideBudget] No Pod or no viewpoints in the open scene, so nothing was measured.");
                return;
            }

            Debug.Log("[RideBudget] Quest 2 budget " + MaxDrawCalls + " draw calls / " + MaxTriangles + " triangles per view:\n  " + string.Join("\n  ", lines));
            if (ThrowOverBudget && over.Count > 0) throw new InvalidOperationException("Over the Quest 2 render budget: " + string.Join("; ", over));
        }

        // Panels hidden until their moment (end card, weight grid) and the XR rig are not drawn at a viewpoint that does not list them.
        private static void Count(GameObject root, ref int draws, ref int triangles, RideViewpoint podAt)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                if (podAt != null && IsHiddenOrRig(r.transform, root.transform, podAt)) continue;
                var filter = r.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                draws += Mathf.Max(1, r.sharedMaterials.Length);
                if (mesh == null) continue;
                for (int sub = 0; sub < mesh.subMeshCount; sub++) triangles += (int)(mesh.GetIndexCount(sub) / 3);
            }
        }

        private static bool IsHiddenOrRig(Transform t, Transform pod, RideViewpoint vp)
        {
            for (Transform c = t; c != null && c != pod; c = c.parent)
            {
                if (c.name == "EndCard" && Array.IndexOf(vp.podPanels, "EndCard") < 0) return true;
                if (c.name == "WeightGrid" && Array.IndexOf(vp.podPanels, "Dash/WeightGrid") < 0) return true;
                if (c.name == "SeatAnchor") return true;
            }

            return false;
        }
    }
}
