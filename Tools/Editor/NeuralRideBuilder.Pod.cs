using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The pod: a low cockpit tub (nothing above 1.1 m in the forward 70 degrees), the seat, and the dash. For Level 1
    /// the dash keeps one lever, GO (orange: you can grab it), the subtitle screen with a one-line readout for live
    /// numbers, the region tag, and the 7×7 weight grid the rider paints in Region 2 by poke, ray or mouse. The end card
    /// with the credits hangs ahead of the pod and shows when the ride completes.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        /// <summary>The virtual eye every panel is laid out for. PodSeat lifts the rig so the real eye sits here.</summary>
        private static readonly Vector3 EyePoint = new Vector3(0f, 1.30f, 0f);

        private const float CellPitch = 0.058f;

        private sealed class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();

            /// <summary>Adds a flat quad a-b-c-d whose face points toward outward, with hard edges.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(n, outward) < 0f)
                {
                    Vector3 swap = b;
                    b = d;
                    d = swap;
                    n = -n;
                }

                int i = Vertices.Count;
                Vertices.AddRange(new[] { a, b, c, d });
                Normals.AddRange(new[] { n, n, n, n });
                Triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(Vertices);
                mesh.SetNormals(Normals);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Mesh SaveMesh(Mesh mesh, string file)
        {
            string dir = PrefabDir + "/Meshes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(PrefabDir, "Meshes");
            string path = dir + "/" + file + ".asset";
            // Recreate the asset instead of copying into the old one: updating a loaded mesh in place leaves the Editor
            // drawing its previous GPU data, so each build makes a fresh mesh object.
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject AddMeshObject(string name, Transform parent, Mesh mesh, string material)
        {
            var go = Make(name, parent, Vector3.zero);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat(material);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private sealed class Lever
        {
            public RideControlChannel channel;
            public GameObject root;
            public GameObject visual;
            public Transform arrow;
        }

        private static GameObject BuildPod()
        {
            var pod = new GameObject("Pod");
            Transform seat = Make("SeatAnchor", pod.transform, Vector3.zero).transform;

            Prim(PrimitiveType.Cylinder, "Floor", pod.transform, new Vector3(0, -0.03f, 0), new Vector3(2.6f, 0.02f, 2.6f), "PodDark");
            Prim(PrimitiveType.Cylinder, "FloorRing", pod.transform, new Vector3(0, -0.045f, 0), new Vector3(2.75f, 0.01f, 2.75f), "CyanDim");
            Prim(PrimitiveType.Cube, "SeatBack", pod.transform, new Vector3(0, 0.75f, -0.62f), new Vector3(0.62f, 0.9f, 0.1f), "PodMid");
            Prim(PrimitiveType.Cube, "SeatBase", pod.transform, new Vector3(0, 0.3f, -0.4f), new Vector3(0.62f, 0.12f, 0.5f), "PodMid");
            BuildTub(pod.transform);

            // Dash: low and tilted 45 degrees toward the seat, so its top edge sits about 14 degrees under a 1.15 m eye and
            // even a short rider sees the pixels below the horizon ahead. Local +Z points away from the player.
            var dash = Make("Dash", pod.transform, new Vector3(0, 0.66f, 0.62f));
            dash.transform.localRotation = Quaternion.Euler(45f, 0, 0);
            Prim(PrimitiveType.Cube, "Panel", dash.transform, new Vector3(0, 0f, 0.02f), new Vector3(1.3f, 0.75f, 0.04f), "PodDark");
            Prim(PrimitiveType.Cube, "TrimTop", dash.transform, new Vector3(0, 0.37f, -0.004f), new Vector3(1.3f, 0.012f, 0.01f), "CyanDim");
            Quad("Screen", dash.transform, new Vector3(0, 0.255f, -0.004f), new Vector2(0.76f, 0.2f), "Screen");
            // The screen shows who is speaking, then the subtitle, 0.031 m type at under 1 m from the eye (1.7 degrees).
            TextMeshPro instruction = Text("Instruction", dash.transform, new Vector3(0.05f, 0.335f, -0.012f), "", 0.022f, _theme.offWhite, 0.66f, TextAlignmentOptions.Right, true);
            TextMeshPro subtitle = Text("Subtitle", dash.transform, new Vector3(0, 0.245f, -0.012f), "", 0.031f, _theme.offWhite, 0.72f);
            instruction.rectTransform.sizeDelta = new Vector2(0.66f, 0.035f);
            subtitle.rectTransform.sizeDelta = new Vector2(0.72f, 0.13f);
            TextMeshPro readout = Text("Readout", dash.transform, new Vector3(0, 0.125f, -0.012f), "", 0.026f, _theme.cyan, 1.1f, bold: true);
            readout.rectTransform.sizeDelta = new Vector2(1.2f, 0.04f);
            // The topic tag (PIXELS, NEURON, ...): CMU Serif, letter-spaced, like the explainers' titles.
            TextMeshPro regionTag = Text("RegionTag", dash.transform, new Vector3(-0.29f, 0.335f, -0.012f), "MEET PIXEL", 0.032f, _theme.explainWhite, 0.6f, TextAlignmentOptions.Left);
            regionTag.font = ExplainerFont();
            regionTag.characterSpacing = 4f;
            regionTag.enableAutoSizing = true;
            regionTag.fontSizeMin = 0.2f;
            regionTag.fontSizeMax = 0.32f;
            regionTag.rectTransform.sizeDelta = new Vector2(0.6f, 0.035f);

            var channels = new List<Object>();
            Lever action = MakeLever(dash.transform, RideControlIds.Action, "Action", -0.5f, 0f, channels);
            Text("ActionCaption", action.visual.transform, new Vector3(0, -0.165f, -0.012f), "GO!", 0.04f, _theme.orange, 0.2f);
            var set = pod.AddComponent<RideControlSet>();
            Refs(set, "channels", channels);

            BuildGameTable(pod.transform);
            BuildStamp(pod.transform);

            var speaker = Make("DashSpeaker", dash.transform, new Vector3(0, 0.25f, -0.05f)).AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0.6f;
            speaker.volume = 1f;

            var music = Make("Music", pod.transform, Vector3.zero).AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);

            // End card: 2.6 m out, a little above the eye (low enough that the title clears the top of the view at 1.15 m). Solid board with a back, so nothing reads mirrored.
            var end = Make("EndCard", pod.transform, new Vector3(0, 2.08f, 2.6f));
            // The plate grows downward for the recap rows, so the title stays where it was.
            Board("Plate", end.transform, new Vector3(0f, -0.15f, 0f), new Vector2(3.5f, 1.5f), "PlateSolid");
            TextMeshPro title = Text("Title", end.transform, new Vector3(0, 0.40f, -0.01f), "YOU WATCHED A MACHINE LEARN", 0.15f, _theme.gold, 3.4f, bold: true);
            // What the ride went over, numbered as the intro and the recap named it (owner, 2026-10-27).
            TextMeshPro recap = Text("Recap", end.transform, new Vector3(0, 0.17f, -0.01f), "1 PIXELS    2 NEURON    3 NETWORK\n4 BACKPROP    5 TRAINING", 0.11f, Color.white, 3.4f, bold: true);
            recap.rectTransform.sizeDelta = new Vector2(3.4f, 0.28f);
            TextMeshPro next = Text("Next", end.transform, new Vector3(0, -0.07f, -0.01f), "NEXT: LEVEL 2 - WORDS", 0.13f, _theme.cyan, 2.8f, bold: true);
            var credits = Make("Credits", end.transform, new Vector3(0, -0.5f, -0.01f));
            TextMeshPro creditText = Text("Text", credits.transform, Vector3.zero, RideNarrationData.Credits, 0.1f, _theme.offWhite, 3.4f);
            creditText.rectTransform.sizeDelta = new Vector2(3.4f, 0.5f);
            creditText.lineSpacing = 8f;
            var finale = pod.AddComponent<FinaleView>();
            Ref(finale, "panel", end);
            Ref(finale, "toBeContinued", next.gameObject);
            Ref(finale, "credits", credits);

            var hints = pod.AddComponent<HintArrowView>();
            Set(hints, "arrows", p =>
            {
                p.arraySize = 1;
                SerializedProperty e = p.GetArrayElementAtIndex(0);
                e.FindPropertyRelative("id").stringValue = action.channel.Id;
                e.FindPropertyRelative("arrow").objectReferenceValue = action.arrow;
            });

            var screen = pod.AddComponent<DashScreenView>();
            Ref(screen, "instruction", instruction);
            Ref(screen, "subtitle", subtitle);
            Ref(screen, "readout", readout);
            Ref(screen, "voice", speaker);
            Ref(screen, "library", _library);
            Ref(screen, "theme", _theme);
            Ref(hints, "dash", screen); // "pull the orange lever": the arrow points at GO while it is said

            var tagView = pod.AddComponent<RegionTagView>();
            Ref(tagView, "theme", _theme);
            Ref(tagView, "label", regionTag);
            Ref(tagView, "dash", screen);
            Set(tagView, "cueTags", p =>
            {
                var tags = new[] { ("neuron", "NEURON"), ("wires", "WEIGHTS"), ("connect", "NEURAL NETWORK"), ("oops", "LOSS"), ("sentence", "TOKENS") };
                p.arraySize = tags.Length;
                for (int i = 0; i < tags.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).FindPropertyRelative("cue").stringValue = tags[i].Item1;
                    p.GetArrayElementAtIndex(i).FindPropertyRelative("tag").stringValue = tags[i].Item2;
                }
            });

            BuildBlackboardGrid(pod.transform);

            var seatScript = pod.AddComponent<PodSeat>();
            Ref(seatScript, "seatAnchor", seat);
            pod.AddComponent<DesktopSeat>();
            var musicPlayer = pod.AddComponent<RideMusic>();
            Ref(musicPlayer, "source", music);
            Ref(musicPlayer, "dash", screen);
            Float(musicPlayer, "volume", 0.14f);
            Float(musicPlayer, "duckedVolume", 0.05f);
            BuildSfx(pod, screen);
            return pod;
        }

        /// <summary>
        /// The rider's 7×7 weight grid: one mesh (tile + two bars per cell, recoloured by WeightGridView), an orange
        /// frame (live) and an orange hint ring, and 49 colliders with a poke filter, one per cell.
        /// </summary>
        private static Transform BuildWeightGrid(Transform parent)
        {
            Transform grid = Make("WeightGrid", parent, new Vector3(0.08f, -0.12f, -0.006f)).transform;
            Board("Back", grid, new Vector3(0, 0, 0.002f), new Vector2(CellPitch * 7 + 0.05f, CellPitch * 7 + 0.05f), "PodDark", 0.01f);
            Quad("Frame", grid, new Vector3(0, 0, -0.001f), new Vector2(CellPitch * 7 + 0.03f, CellPitch * 7 + 0.03f), "Orange");
            Quad("Inner", grid, new Vector3(0, 0, -0.002f), new Vector2(CellPitch * 7 + 0.006f, CellPitch * 7 + 0.006f), "PodDark");

            var cells = new TileMesh();
            float tile = CellPitch * 0.86f, bar = CellPitch * 0.5f, thick = CellPitch * 0.12f;
            Color zero = new Color(0.10f, 0.13f, 0.20f, 0f);
            for (int c = 0; c < 49; c++)
            {
                Vector3 at = new Vector3((c % 7 - 3) * CellPitch, (3 - c / 7) * CellPitch, -0.004f);
                cells.Face(at, tile, tile, zero);                               // 4 vertices: the tile
                cells.Face(at + new Vector3(0, 0, -0.001f), bar, thick, zero);  // 4: the horizontal bar ("-")
                cells.Face(at + new Vector3(0, 0, -0.0012f), thick, bar, zero); // 4: the vertical bar ("+" with the other)
            }

            GameObject cellObject = TileObject("Cells", grid, cells, "Tiles");
            var ring = Quad("HintRing", grid, new Vector3(0, 0, -0.008f), new Vector2(CellPitch * 1.25f, CellPitch * 1.25f), "GlowOrange");

            Transform colliders = Make("Taps", grid, Vector3.zero).transform;
            for (int c = 0; c < 49; c++)
            {
                var cell = Make("Cell" + c, colliders, new Vector3((c % 7 - 3) * CellPitch, (3 - c / 7) * CellPitch, -0.004f));
                // 3.6 cm deep and standing out in front of the tile (toward the rider, -z), so a fingertip or a ray meets the
                // cell before it could reach the board behind it; neighbours stay 4% apart so each press is one cell.
                var box = cell.AddComponent<BoxCollider>();
                box.size = new Vector3(CellPitch * 0.96f, CellPitch * 0.96f, 0.036f);
                box.center = new Vector3(0, 0, -0.012f);
                var interactable = cell.AddComponent<WeightCellInteractable>();
                interactable.SetCell(c);
                cell.AddComponent<XRPokeFilter>();
            }

            return grid;
        }

        private static Lever MakeLever(Transform dash, string id, string name, float x, float zeroFraction, List<Object> channels)
        {
            const float half = 0.10f;
            var lever = new Lever { root = Make("Lever_" + name, dash, new Vector3(x, -0.12f, -0.01f)) };
            lever.visual = Make("Visual", lever.root.transform, Vector3.zero);
            Transform v = lever.visual.transform;
            Quad("Slot", v, new Vector3(0, 0, 0.003f), new Vector2(0.034f, half * 2f + 0.085f), "SlotFrame");

            // Hint arrow: two stacked chevrons beside the track, shown only when this lever is the one to move.
            var arrow = Make("HintArrow", v, new Vector3(0.075f, 0f, -0.012f));
            for (int c = 0; c < 2; c++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var bar = Prim(PrimitiveType.Cube, "Chevron", arrow.transform, new Vector3(side * 0.017f, c * -0.03f, 0), new Vector3(0.05f, 0.012f, 0.008f), "Orange");
                    bar.transform.localRotation = Quaternion.Euler(0, 0, -side * 40f);
                }
            }

            Quad("ArrowGlow", arrow.transform, new Vector3(0, -0.015f, 0.004f), new Vector2(0.14f, 0.14f), "GlowOrange");
            lever.arrow = arrow.transform;

            var handle = Make("Handle", v, Vector3.zero);
            Prim(PrimitiveType.Cube, "HandleBody", handle.transform, Vector3.zero, new Vector3(0.085f, 0.04f, 0.045f), "Orange");
            var grip = handle.AddComponent<BoxCollider>();
            grip.size = new Vector3(0.14f, 0.10f, 0.12f);

            var channel = lever.root.AddComponent<RideControlChannel>();
            channel.SetId(id);
            lever.channel = channel;
            channels.Add(channel);

            var interactable = lever.root.AddComponent<PodLeverInteractable>();
            Ref(interactable, "channel", channel);
            Float(interactable, "halfTravel", half);
            UnityEventTools.AddPersistentListener(interactable.UserMoved, channel.SetFromUser);

            var view = lever.root.AddComponent<PodLeverView>();
            Ref(view, "channel", channel);
            Ref(view, "handle", handle.transform);
            Ref(view, "handleRenderer", handle.transform.Find("HandleBody").GetComponent<Renderer>());
            Ref(view, "visualRoot", lever.visual);
            Ref(view, "liveMaterial", Mat("Orange"));
            Ref(view, "lockedMaterial", Mat("Locked"));
            Float(view, "halfTravel", half);
            return lever;
        }

        /// <summary>Wires the pod's views to the scene's director and moments (the prefab cannot hold scene references).</summary>
        private static void WirePod(GameObject pod, RideDirector director, Moments moments)
        {
            Ref(pod.GetComponent<DashScreenView>(), "director", director);
            Ref(pod.GetComponent<HintArrowView>(), "director", director);
            Ref(pod.GetComponent<FinaleView>(), "director", director);
            Ref(pod.GetComponent<RideMusic>(), "director", director);
            Ref(pod.GetComponent<RegionTagView>(), "director", director);

            Transform grid = pod.transform.Find("GameTable/WeightGrid");
            var gridView = pod.AddComponent<WeightGridView>();
            Ref(gridView, "director", director);
            Ref(gridView, "moment", moments.Paint);
            Ref(gridView, "theme", _theme);
            Ref(gridView, "grid", grid.gameObject);
            Ref(gridView, "cells", grid.Find("Cells").GetComponent<MeshFilter>());
            Ref(gridView, "frame", grid.Find("Frame").GetComponent<Renderer>());
            Ref(gridView, "frameLive", Mat("Orange"));
            Ref(gridView, "frameLocked", Mat("Locked"));
            Ref(gridView, "hintRing", grid.Find("HintRing"));
            Ref(gridView, "digits", grid.Find("Digits").GetComponent<MeshFilter>());
            Int(gridView, "showFromSegment", SegNeuronPlay);
            Float(gridView, "cellPitch", CellPitch);
            WireSfx(pod, director, moments);
        }

        /// <summary>
        /// The blackboard's very faint, distant coordinate grid (3Blue1Brown's number plane, far below): BLUE lines every
        /// 3 m on a floor 7 m under the pod, fading out with distance. One mesh; RegionAtmosphere fades it in and out.
        /// </summary>
        private static void BuildBlackboardGrid(Transform pod)
        {
            var lines = new List<Shape>();
            const float extent = 60f, spacing = 3f, y = -7f;
            const int pieces = 10;
            Color blue = _theme.explainBlue;
            for (float o = -extent; o <= extent + 0.01f; o += spacing)
            {
                for (int k = 0; k < pieces; k++)
                {
                    float a = -extent + 2f * extent * k / pieces, b = -extent + 2f * extent * (k + 1) / pieces;
                    foreach (bool alongZ in new[] { true, false })
                    {
                        Vector3 from = alongZ ? new Vector3(o, y, a) : new Vector3(a, y, o);
                        Vector3 to = alongZ ? new Vector3(o, y, b) : new Vector3(b, y, o);
                        float d = new Vector2((from.x + to.x) * 0.5f, (from.z + to.z) * 0.5f).magnitude / extent;
                        float fade = Mathf.Clamp01(1f - d) * Mathf.Clamp01(d * 3f + 0.2f);
                        if (fade < 0.03f) continue;
                        lines.Add(new Shape
                        {
                            kind = Shape.Kind.Line, position = from, end = to, size = new Vector2(0.05f, 0.05f),
                            rotation = Quaternion.Euler(90f, 0f, 0f), color = new Color(blue.r, blue.g, blue.b, fade * fade), round = 1f
                        });
                    }
                }
            }

            // Built once with a ShapeBatch and saved as a mesh asset: the grid never changes, only its alpha.
            var go = Make("Blackboard", pod, Vector3.zero);
            var filter = go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat("BlackboardGrid");
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = -10;
            var batch = go.AddComponent<ShapeBatch>();
            batch.SetShapes(lines);
            Mesh mesh = Object.Instantiate(filter.sharedMesh);
            mesh.name = "BlackboardGrid";
            mesh.hideFlags = HideFlags.None;
            Object.DestroyImmediate(batch);
            filter.sharedMesh = SaveMesh(mesh, "BlackboardGrid");
        }

        private static void BuildTub(Transform pod)
        {
            const int segments = 48;
            const float startDeg = 52f, endDeg = 308f;   // open at the front, where the dash and the view are
            const float rIn = 1.28f, rOut = 1.35f;
            const float rearHeight = 0.95f, frontHeight = 0.48f;

            var wall = new MeshData();
            var trim = new MeshData();

            Vector3 At(float r, float deg, float y) => new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad) * r, y, Mathf.Cos(deg * Mathf.Deg2Rad) * r);

            float HeightAt(float deg)
            {
                float fromRear = Mathf.Abs(deg - 180f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 128f, fromRear));
                return Mathf.Lerp(rearHeight, frontHeight, k);
            }

            for (int i = 0; i < segments; i++)
            {
                float d0 = Mathf.Lerp(startDeg, endDeg, i / (float)segments);
                float d1 = Mathf.Lerp(startDeg, endDeg, (i + 1) / (float)segments);
                float h0 = HeightAt(d0), h1 = HeightAt(d1);
                Vector3 radial0 = At(1f, d0, 0f), radial1 = At(1f, d1, 0f);

                wall.Quad(At(rIn, d0, 0f), At(rIn, d1, 0f), At(rIn, d1, h1), At(rIn, d0, h0), -(radial0 + radial1));
                wall.Quad(At(rOut, d0, 0f), At(rOut, d1, 0f), At(rOut, d1, h1), At(rOut, d0, h0), radial0 + radial1);
                wall.Quad(At(rIn, d0, h0), At(rIn, d1, h1), At(rOut, d1, h1), At(rOut, d0, h0), Vector3.up);
                trim.Quad(At(rIn + 0.02f, d0, h0 + 0.004f), At(rIn + 0.02f, d1, h1 + 0.004f), At(rOut - 0.02f, d1, h1 + 0.004f), At(rOut - 0.02f, d0, h0 + 0.004f), Vector3.up);
            }

            foreach (float deg in new[] { startDeg, endDeg })
            {
                float h = HeightAt(deg);
                Vector3 tangent = At(1f, deg + 1f, 0f) - At(1f, deg, 0f);
                wall.Quad(At(rIn, deg, 0f), At(rOut, deg, 0f), At(rOut, deg, h), At(rIn, deg, h), deg < 180f ? -tangent : tangent);
            }

            AddMeshObject("Tub", pod, SaveMesh(wall.ToMesh("PodTub"), "PodTub"), "PodMid");
            AddMeshObject("TubTrim", pod, SaveMesh(trim.ToMesh("PodTubTrim"), "PodTubTrim"), "CyanDim");

            // A headrest behind the player's head, outside every forward sight line.
            Prim(PrimitiveType.Cube, "Headrest", pod, new Vector3(0, 1.32f, -0.74f), new Vector3(0.36f, 0.26f, 0.10f), "PodMid");
            Prim(PrimitiveType.Cube, "HeadrestTrim", pod, new Vector3(0, 1.455f, -0.69f), new Vector3(0.36f, 0.012f, 0.012f), "CyanDim");
        }
    }
}
