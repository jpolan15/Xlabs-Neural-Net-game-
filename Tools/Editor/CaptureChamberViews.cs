// WP1 capture rig. Appendix A2 was a sketch; this file is compiled against the APIs found in
// this repo and in Unity 6000.6.0f1 / URP 17.6.0.
//
// Confirmed by search:
// - UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest and SupportsRenderRequest
//   (UnityEngine.CoreModule.xml).
// - UniversalRenderPipeline.SingleCameraRequest, the type IsRenderRequestSupported accepts
//   (Library/PackageCache com.unity.render-pipelines.universal Runtime/UniversalRenderPipeline.cs).
//   The obsolete RenderSingleCamera message names UniversalRenderer.SingleCameraRequest; that
//   type is not in this package. SingleCameraRequest on UniversalRenderPipeline is the one that exists.
// - RenderSingleCameraInternal reads camera.targetTexture dimensions and skips non-base cameras.
//   It does not check Camera.enabled in the lines that were read. The capture camera stays disabled
//   so the player loop does not also render it. If captures are black, that assumption is wrong.
// - EditorApplication.update, delayCall, and timeSinceStartup (UnityEditor.CoreModule.xml).
// - Time.frameCount and Time.unscaledTimeAsDouble (UnityEngine.CoreModule.xml).
// - UnityEditor.UnityStats.triangles and UnityStats.drawCalls (metadata of UnityEditor.CoreModule.dll).
// - ChamberController.TriggerForwardPass, NeuralState.SetWeight / SetBias / SetActivation /
//   SetCableConnected, LevelResetter.ResetToPreset, BrokenConfig_Opening.
//
// Bound scene objects (names from Level01SceneBuilder):
// - Player start: the XROrigin placed by InstantiateXrRig. Floor position is the rig transform.
//   Seated eye is that floor plus 1.2 m. Standing eye is that floor plus 1.6 m. Those heights are
//   the WP1 view spec, not coordinates stored in the scene. The prefab Camera Offset is not used
//   as the standing height.
// - Console: TactileEngineeringWorkstation and its child ConsoleBounds.
// - Window: WindowBounds under ForwardObservationViewportAndDeepSpaceVista.
// - Value probes: ValueProbe_Ceiling, ValueProbe_Floor, ValueProbe_Wall.
//
// Assumptions that can make a capture lie are listed in Tools/Editor/WP1-ASSUMPTIONS.md.
// This menu does not judge images. A human reads DevHeadsetTextStrip and writes that result
// into the task file. This script does not invent that result.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Convergence.EditorTools
{
    public static class CaptureChamberViews
    {
        const string Wp = "B0";
        const int Width = 1920;
        const int Height = 1080;
        const int BlockColumns = 16;
        const int BlockRows = 9;
        const float SeatedEyeMeters = 1.2f;
        const float StandingEyeMeters = 1.6f;
        const float LookUpDegrees = 35f;
        const float ConsoleCloseMeters = 0.6f;
        const string ScenePath = "Assets/Scenes/Level01_AwakeningGate.unity";

        static bool s_Running;

        [MenuItem("Tools/Chamber 01/Capture Views")]
        public static async void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode first.");
                return;
            }

            if (s_Running)
            {
                Debug.LogError("[CaptureChamberViews] A capture is already running.");
                return;
            }

            s_Running = true;
            try
            {
                await CaptureAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                s_Running = false;
            }
        }

        /// <summary>
        /// Batchmode entry point used by Tools/Verify-Chamber01.ps1 after BuildLevel01.
        /// Writes Temp/chamber01_hierarchy.txt relative to the project root.
        /// </summary>
        public static void DumpHierarchy()
        {
            string projectRoot = ProjectRoot();
            string sceneAsset = Path.Combine(projectRoot, ScenePath);
            if (!File.Exists(sceneAsset))
            {
                Debug.LogError("[CaptureChamberViews] Scene is missing: " + ScenePath);
                EditorApplication.Exit(1);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var lines = new List<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                WalkHierarchy(root.transform, "", lines);
            }

            string outPath = Path.Combine(projectRoot, "Temp", "chamber01_hierarchy.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllLines(outPath, lines);
            Debug.Log("[CaptureChamberViews] Wrote hierarchy dump " + lines.Count + " lines to " + outPath);
        }

        static async Task CaptureAsync()
        {
            string root = ProjectRoot();
            string outDir = Path.Combine(root, "Documentation", "Design", "captures");
            Directory.CreateDirectory(outDir);
            float clippedMin = ReadClippedLuminanceMin(root);

            XROrigin origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Transform consoleBounds = FindRequired("ConsoleBounds");
            Transform windowBounds = FindRequired("WindowBounds");
            Transform probeCeiling = FindRequired("ValueProbe_Ceiling");
            Transform probeFloor = FindRequired("ValueProbe_Floor");
            Transform probeWall = FindRequired("ValueProbe_Wall");
            if (origin == null || consoleBounds == null || windowBounds == null
                || probeCeiling == null || probeFloor == null || probeWall == null)
            {
                return;
            }

            Camera rigCamera = origin.Camera;
            var go = new GameObject("CaptureCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            go.AddComponent<UniversalAdditionalCameraData>();
            CopyRigCamera(cam, rigCamera);
            var rt = new RenderTexture(
                Width,
                Height,
                GraphicsFormat.R8G8B8A8_SRGB,
                UnityEngine.Rendering.CoreUtils.GetDefaultDepthOnlyFormat());
            cam.targetTexture = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            try
            {
                var states = new[]
                {
                    new CaptureState("crisis", 0.5f, ApplyCrisis),
                    new CaptureState("success", 3.0f, () => ApplyEvaluated(expectPass: true)),
                    new CaptureState("fail", 1.0f, () => ApplyEvaluated(expectPass: false))
                };

                bool wroteStats = false;
                foreach (CaptureState state in states)
                {
                    if (!state.Apply())
                    {
                        continue;
                    }

                    await WaitGameSeconds(state.SettleSeconds);
                    if (!Application.isPlaying)
                    {
                        Debug.LogError("[CaptureChamberViews] Play Mode ended during capture.");
                        return;
                    }

                    if (!wroteStats)
                    {
                        WriteEditorNumbers(outDir);
                        wroteStats = true;
                    }

                    foreach (CaptureView view in ViewsFor(state.Name, origin.transform, consoleBounds, windowBounds, cam))
                    {
                        await NextGameFrame();
                        if (!Application.isPlaying)
                        {
                            Debug.LogError("[CaptureChamberViews] Play Mode ended during capture.");
                            return;
                        }

                        Vector3 direction = view.LookAt - view.Position;
                        if (direction.sqrMagnitude < 0.0001f)
                        {
                            Debug.LogError("[CaptureChamberViews] " + view.Name + " looks at its own position. Skipped.");
                            continue;
                        }

                        go.transform.SetPositionAndRotation(view.Position, Quaternion.LookRotation(direction, Vector3.up));
                        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                        if (!RenderPipeline.SupportsRenderRequest(cam, request))
                        {
                            Debug.LogError("[CaptureChamberViews] URP rejected SingleCameraRequest.");
                            return;
                        }

                        RenderPipeline.SubmitRenderRequest(cam, request);
                        RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                        tex.Apply();
                        RenderTexture.active = null;

                        Color32[] pixels = tex.GetPixels32();
                        string stem = "C01_" + view.Name + "_" + state.Name + "_" + Wp;
                        File.WriteAllBytes(Path.Combine(outDir, stem + ".png"), tex.EncodeToPNG());
                        WriteGray(outDir, stem, pixels);
                        WriteSquint(outDir, stem, pixels);
                        AppendMetrics(outDir, stem, view.Name, state.Name, pixels, cam, consoleBounds, windowBounds, probeCeiling, probeFloor, probeWall, clippedMin);
                        Debug.Log("[CaptureChamberViews] Wrote " + stem);
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
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CopyRigCamera(Camera cam, Camera rigCamera)
        {
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.016f, 0.030f);
            cam.cullingMask = ~0;
            cam.fieldOfView = 90f;
            if (rigCamera == null)
            {
                Debug.LogWarning("[CaptureChamberViews] XROrigin.Camera is missing. Capture camera used the fallback lens.");
                return;
            }

            cam.fieldOfView = rigCamera.fieldOfView;
            cam.nearClipPlane = rigCamera.nearClipPlane;
            cam.farClipPlane = rigCamera.farClipPlane;
            cam.clearFlags = rigCamera.clearFlags;
            cam.backgroundColor = rigCamera.backgroundColor;
            cam.cullingMask = rigCamera.cullingMask;
            cam.allowHDR = rigCamera.allowHDR;
            cam.allowMSAA = rigCamera.allowMSAA;
        }

        static IEnumerable<CaptureView> ViewsFor(string state, Transform rig, Transform consoleBounds, Transform windowBounds, Camera cam)
        {
            Vector3 floor = rig.position;
            Vector3 seatEye = floor + Vector3.up * SeatedEyeMeters;
            Vector3 standEye = floor + Vector3.up * StandingEyeMeters;
            Vector3 consoleCenter = consoleBounds.position;
            Vector3 windowCenter = windowBounds.position;
            Vector3 gaze = Vector3.Lerp(consoleCenter, windowCenter, 0.65f);
            var views = new List<CaptureView>
            {
                new CaptureView("V1", seatEye, gaze)
            };

            if (state != "crisis")
            {
                return views;
            }

            views.Add(new CaptureView("V2", standEye, gaze));
            AddCorner(views, "V3", "Bulkhead_Port", seatEye, consoleCenter, gaze, cam);
            AddCorner(views, "V4", "Bulkhead_Starboard", seatEye, consoleCenter, gaze, cam);
            views.Add(new CaptureView("V5", seatEye, LookUpTarget(seatEye, gaze)));
            views.Add(new CaptureView("V6", CloseToConsole(seatEye, consoleCenter), consoleCenter));
            views.Add(new CaptureView("V7", WindowCamera(seatEye, windowCenter, windowBounds, cam), windowCenter));
            return views;
        }

        static void AddCorner(List<CaptureView> views, string name, string prefix, Vector3 seatEye, Vector3 consoleCenter, Vector3 gaze, Camera cam)
        {
            Transform wall = FindClosestNamed(prefix, seatEye);
            if (wall == null)
            {
                Debug.LogError("[CaptureChamberViews] No " + prefix + " module. " + name + " was skipped.");
                return;
            }

            Vector3 towardConsole = consoleCenter - wall.position;
            towardConsole.y = 0f;
            if (towardConsole.sqrMagnitude < 0.0001f)
            {
                Debug.LogError("[CaptureChamberViews] " + wall.name + " is on the console. " + name + " was skipped.");
                return;
            }

            float clear = Mathf.Max(Mathf.Abs(wall.lossyScale.x), Mathf.Abs(wall.lossyScale.z)) * 0.5f;
            clear += cam.nearClipPlane * 4f;
            Vector3 pos = wall.position + towardConsole.normalized * clear;
            pos.y = seatEye.y;
            views.Add(new CaptureView(name, pos, gaze));
        }

        static Vector3 LookUpTarget(Vector3 eye, Vector3 gaze)
        {
            Vector3 horizontal = gaze - eye;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.forward;
            Quaternion level = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            Vector3 direction = level * Quaternion.Euler(-LookUpDegrees, 0f, 0f) * Vector3.forward;
            return eye + direction;
        }

        static Vector3 CloseToConsole(Vector3 seatEye, Vector3 consoleCenter)
        {
            Vector3 away = seatEye - consoleCenter;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.back;
            return consoleCenter + away.normalized * ConsoleCloseMeters;
        }

        static Vector3 WindowCamera(Vector3 seatEye, Vector3 windowCenter, Transform windowBounds, Camera cam)
        {
            Vector3 toWindow = windowCenter - seatEye;
            float span = toWindow.magnitude;
            if (span < 0.001f) return seatEye;
            float halfHeight = Mathf.Abs(windowBounds.lossyScale.y) * 0.5f;
            float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = halfFov > 0.001f ? halfHeight / Mathf.Tan(halfFov) : span * 0.5f;
            float margin = Mathf.Max(cam.nearClipPlane * 4f, Mathf.Abs(windowBounds.lossyScale.z));
            float used = Mathf.Min(distance, Mathf.Max(margin, span - margin));
            return windowCenter - toWindow.normalized * used;
        }

        static Transform FindClosestNamed(string prefix, Vector3 near)
        {
            Transform best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!transform.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                Vector3 delta = transform.position - near;
                delta.y = 0f;
                float distance = delta.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = transform;
                }
            }

            return best;
        }

        static bool ApplyCrisis()
        {
            return ResetOpening();
        }

        static bool ApplyEvaluated(bool expectPass)
        {
            if (!ResetOpening()) return false;
            NeuralState neural = UnityEngine.Object.FindAnyObjectByType<NeuralState>();
            ChamberController chamber = UnityEngine.Object.FindAnyObjectByType<ChamberController>();
            if (neural == null || chamber == null)
            {
                Debug.LogError("[CaptureChamberViews] NeuralState or ChamberController is missing.");
                return false;
            }

            // Canonical OR solution is w1=1, w2=1, b=-0.5, Step, both cables connected
            // (Assets/Puzzles/Chamber01/AGENTS.md). The fail weights are a real Step
            // network that does not satisfy that table. Passed comes only from TriggerForwardPass.
            if (expectPass)
            {
                neural.SetWeight(0, 1.0);
                neural.SetWeight(1, 1.0);
                neural.SetBias(-0.5);
            }
            else
            {
                neural.SetWeight(0, 0.0);
                neural.SetWeight(1, 0.0);
                neural.SetBias(-0.5);
            }

            neural.SetActivation(ActivationType.Step);
            neural.SetCableConnected(0, true);
            neural.SetCableConnected(1, true);
            PuzzleEvaluation evaluation = chamber.TriggerForwardPass();
            if (evaluation == null || evaluation.Passed != expectPass)
            {
                Debug.LogError("[CaptureChamberViews] TriggerForwardPass Passed="
                    + (evaluation != null && evaluation.Passed)
                    + " but this capture expected " + expectPass + ". Those views were skipped.");
                return false;
            }

            return true;
        }

        static bool ResetOpening()
        {
            LevelResetter resetter = UnityEngine.Object.FindAnyObjectByType<LevelResetter>();
            NeuralState neural = UnityEngine.Object.FindAnyObjectByType<NeuralState>();
            if (resetter == null || neural == null)
            {
                Debug.LogError("[CaptureChamberViews] LevelResetter or NeuralState is missing.");
                return false;
            }

            BrokenConfigurationSO opening = null;
            BrokenConfigurationSO[] presets = resetter.AvailablePresets;
            if (presets != null)
            {
                for (int i = 0; i < presets.Length; i++)
                {
                    if (presets[i] != null && presets[i].name == "BrokenConfig_Opening")
                    {
                        opening = presets[i];
                        break;
                    }
                }
            }

            if (opening != null)
            {
                resetter.ResetToPreset(opening);
                return true;
            }

            resetter.ResetToActivePreset();
            neural.SetWeight(0, 0.0);
            neural.SetWeight(1, 0.0);
            neural.SetBias(-1.0);
            neural.SetActivation(ActivationType.Linear);
            neural.SetCableConnected(0, false);
            neural.SetCableConnected(1, false);
            Debug.LogWarning("[CaptureChamberViews] BrokenConfig_Opening was not on LevelResetter. Applied the opening preset values from GenerateBrokenPresets.");
            return true;
        }

        static void WriteEditorNumbers(string outDir)
        {
            int materials = CountMaterials();
            int lights = 0;
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light != null && light.enabled && light.gameObject.activeInHierarchy) lights++;
            }

            int particleCap = 0;
            foreach (ParticleSystem system in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                particleCap = Mathf.Max(particleCap, system.main.maxParticles);
            }

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"source\": \"UnityEditor.UnityStats during Play Mode capture\",");
            builder.AppendLine("  \"triangles\": " + UnityStats.triangles.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"drawCalls\": " + UnityStats.drawCalls.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"materials\": " + materials.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"lights\": " + lights.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"particleCap\": " + particleCap.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(outDir, "editor_numbers.json"), builder.ToString());
        }

        static int CountMaterials()
        {
            var ids = new HashSet<EntityId>();
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null) continue;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null) ids.Add(materials[i].GetEntityId());
                }
            }

            return ids.Count;
        }

        static void WriteGray(string outDir, string stem, Color32[] pixels)
        {
            var grayPixels = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte lum = (byte)Mathf.Clamp(Mathf.RoundToInt(Lum(pixels[i])), 0, 255);
                grayPixels[i] = new Color32(lum, lum, lum, 255);
            }

            WritePixels(outDir, stem + "_gray.png", grayPixels);
        }

        static void WriteSquint(string outDir, string stem, Color32[] pixels)
        {
            Color32[] blocks = BlockAverages(pixels, out _, out _, out _);
            var scaled = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                int row = y * BlockRows / Height;
                if (row >= BlockRows) row = BlockRows - 1;
                for (int x = 0; x < Width; x++)
                {
                    int col = x * BlockColumns / Width;
                    if (col >= BlockColumns) col = BlockColumns - 1;
                    scaled[(y * Width) + x] = blocks[(row * BlockColumns) + col];
                }
            }

            WritePixels(outDir, stem + "_squint.png", scaled);
        }

        static void WritePixels(string outDir, string fileName, Color32[] pixels)
        {
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.SetPixels32(pixels);
            image.Apply();
            File.WriteAllBytes(Path.Combine(outDir, fileName), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }

        static void AppendMetrics(
            string outDir,
            string stem,
            string view,
            string state,
            Color32[] pixels,
            Camera cam,
            Transform consoleBounds,
            Transform windowBounds,
            Transform probeCeiling,
            Transform probeFloor,
            Transform probeWall,
            float clippedMin)
        {
            bool hasConsole = TryProjectBox(cam, consoleBounds, out ScreenRect console);
            bool hasWindow = TryProjectBox(cam, windowBounds, out ScreenRect window);
            int consolePixels = 0;
            int consoleClipped = 0;
            int centerPixels = 0;
            int centerClipped = 0;
            int windowPixels = 0;
            float windowPeak = 0f;
            float consolePeak = 0f;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float lum = Lum(pixels[(y * Width) + x]);
                    bool inWindow = hasWindow && PixelInside(window, x, y);
                    bool inConsole = hasConsole && PixelInside(console, x, y);
                    if (inWindow)
                    {
                        windowPixels++;
                        if (lum > windowPeak) windowPeak = lum;
                    }
                    else
                    {
                        centerPixels++;
                        if (lum >= clippedMin) centerClipped++;
                    }

                    if (inConsole)
                    {
                        consolePixels++;
                        if (lum >= clippedMin) consoleClipped++;
                        if (lum > consolePeak) consolePeak = lum;
                    }
                }
            }

            BlockAverages(pixels, out double[] blockLum, out int[] blockX, out int[] blockY);
            int brightest = -1;
            for (int i = 0; i < blockLum.Length; i++)
            {
                int x1 = (i % BlockColumns + 1) * Width / BlockColumns;
                int y1 = (i / BlockColumns + 1) * Height / BlockRows;
                if (hasWindow && RectOverlaps(blockX[i], blockY[i], x1, y1, window)) continue;
                if (brightest < 0 || blockLum[i] > blockLum[brightest]) brightest = i;
            }

            double consolePct = consolePixels == 0 ? 0.0 : (100.0 * consoleClipped) / consolePixels;
            double centerPct = centerPixels == 0 ? 0.0 : (100.0 * centerClipped) / centerPixels;
            var record = new StringBuilder();
            record.AppendLine("    {");
            record.AppendLine("      \"name\": \"" + stem + "\",");
            record.AppendLine("      \"wp\": \"" + Wp + "\",");
            record.AppendLine("      \"view\": \"" + view + "\",");
            record.AppendLine("      \"state\": \"" + state + "\",");
            record.AppendLine("      \"clippedLuminanceMin\": " + F(clippedMin) + ",");
            record.AppendLine("      \"clippedConsolePct\": " + F(consolePct) + ",");
            record.AppendLine("      \"clippedCenterExclWindowPct\": " + F(centerPct) + ",");
            record.AppendLine("      \"consolePixelCount\": " + consolePixels + ",");
            record.AppendLine("      \"centerPixelCount\": " + centerPixels + ",");
            record.AppendLine("      \"windowPixelCount\": " + windowPixels + ",");
            record.Append("      \"brightestBlockOutsideWindow\": ");
            if (brightest < 0)
            {
                record.AppendLine("null,");
            }
            else
            {
                int x0 = blockX[brightest];
                int y0 = blockY[brightest];
                int x1 = ((brightest % BlockColumns) + 1) * Width / BlockColumns;
                int y1 = ((brightest / BlockColumns) + 1) * Height / BlockRows;
                float centerX = (x0 + x1) * 0.5f;
                float centerY = (y0 + y1) * 0.5f;
                bool inside = hasConsole && centerX >= console.XMin && centerX < console.XMax && centerY >= console.YMin && centerY < console.YMax;
                record.AppendLine("{");
                record.AppendLine("        \"x\": " + x0 + ",");
                record.AppendLine("        \"y\": " + y0 + ",");
                record.AppendLine("        \"width\": " + (x1 - x0) + ",");
                record.AppendLine("        \"height\": " + (y1 - y0) + ",");
                record.AppendLine("        \"meanLum\": " + F(blockLum[brightest]) + ",");
                record.AppendLine("        \"insideConsole\": " + (inside ? "true" : "false"));
                record.AppendLine("      },");
            }

            record.AppendLine("      \"windowPeakLum\": " + (windowPixels > 0 ? F(windowPeak) : "null") + ",");
            record.AppendLine("      \"consolePeakLum\": " + (consolePixels > 0 ? F(consolePeak) : "null") + ",");
            record.AppendLine("      \"probeCeilingLum\": " + Nullable(MeanLumAt(pixels, cam.WorldToScreenPoint(probeCeiling.position))) + ",");
            record.AppendLine("      \"probeFloorLum\": " + Nullable(MeanLumAt(pixels, cam.WorldToScreenPoint(probeFloor.position))) + ",");
            record.AppendLine("      \"probeWallLum\": " + Nullable(MeanLumAt(pixels, cam.WorldToScreenPoint(probeWall.position))));
            record.Append("    }");
            AppendRecord(Path.Combine(outDir, "metrics.json"), record.ToString());
        }

        static Color32[] BlockAverages(Color32[] pixels, out double[] meanLum, out int[] originX, out int[] originY)
        {
            var colors = new Color32[BlockColumns * BlockRows];
            meanLum = new double[colors.Length];
            originX = new int[colors.Length];
            originY = new int[colors.Length];
            for (int row = 0; row < BlockRows; row++)
            {
                int y0 = row * Height / BlockRows;
                int y1 = (row + 1) * Height / BlockRows;
                for (int col = 0; col < BlockColumns; col++)
                {
                    int x0 = col * Width / BlockColumns;
                    int x1 = (col + 1) * Width / BlockColumns;
                    int index = (row * BlockColumns) + col;
                    originX[index] = x0;
                    originY[index] = y0;
                    double r = 0, g = 0, b = 0, lum = 0;
                    int count = 0;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            Color32 pixel = pixels[(y * Width) + x];
                            r += pixel.r;
                            g += pixel.g;
                            b += pixel.b;
                            lum += Lum(pixel);
                            count++;
                        }
                    }

                    if (count == 0) continue;
                    colors[index] = new Color32((byte)Mathf.RoundToInt((float)(r / count)), (byte)Mathf.RoundToInt((float)(g / count)), (byte)Mathf.RoundToInt((float)(b / count)), 255);
                    meanLum[index] = lum / count;
                }
            }

            return colors;
        }

        static double? MeanLumAt(Color32[] pixels, Vector3 screen)
        {
            if (screen.z <= 0f) return null;
            int cx = Mathf.RoundToInt(screen.x);
            int cy = Mathf.RoundToInt(screen.y);
            double sum = 0;
            int count = 0;
            for (int y = cy - 2; y <= cy + 2; y++)
            {
                for (int x = cx - 2; x <= cx + 2; x++)
                {
                    if ((uint)x >= Width || (uint)y >= Height) continue;
                    sum += Lum(pixels[(y * Width) + x]);
                    count++;
                }
            }

            if (count == 0) return null;
            return sum / count;
        }

        static bool TryProjectBox(Camera cam, Transform box, out ScreenRect rect)
        {
            float xMin = float.PositiveInfinity;
            float yMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMax = float.NegativeInfinity;
            bool any = false;
            for (int i = 0; i < 8; i++)
            {
                var local = new Vector3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f);
                Vector3 screen = cam.WorldToScreenPoint(box.TransformPoint(local));
                if (screen.z <= 0f) continue;
                any = true;
                if (screen.x < xMin) xMin = screen.x;
                if (screen.y < yMin) yMin = screen.y;
                if (screen.x > xMax) xMax = screen.x;
                if (screen.y > yMax) yMax = screen.y;
            }

            rect = new ScreenRect(xMin, yMin, xMax, yMax);
            return any && xMax > xMin && yMax > yMin;
        }

        static bool PixelInside(ScreenRect rect, int x, int y)
        {
            float px = x + 0.5f;
            float py = y + 0.5f;
            return px >= rect.XMin && px < rect.XMax && py >= rect.YMin && py < rect.YMax;
        }

        static bool RectOverlaps(int x0, int y0, int x1, int y1, ScreenRect rect)
        {
            return x0 < rect.XMax && x1 > rect.XMin && y0 < rect.YMax && y1 > rect.YMin;
        }

        static float Lum(Color32 color)
        {
            return (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
        }

        static float ReadClippedLuminanceMin(string projectRoot)
        {
            string path = Path.Combine(projectRoot, "Tools", "chamber01-thresholds.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Thresholds file is missing.", path);
            }

            ClippedThreshold data = JsonUtility.FromJson<ClippedThreshold>(File.ReadAllText(path));
            if (data == null || data.clippedLuminanceMin <= 0f)
            {
                throw new InvalidOperationException("clippedLuminanceMin is missing from Tools/chamber01-thresholds.json.");
            }

            return data.clippedLuminanceMin;
        }

        static void AppendRecord(string path, string record)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "{\n  \"records\": [\n" + record + "\n  ]\n}\n");
                return;
            }

            string text = File.ReadAllText(path);
            int close = text.LastIndexOf(']');
            if (close < 0 || text.IndexOf("\"records\"", StringComparison.Ordinal) < 0)
            {
                Debug.LogError("[CaptureChamberViews] metrics.json is not a records array. The new record was not written, and the file was left untouched.");
                return;
            }

            string head = text.Substring(0, close).TrimEnd();
            File.WriteAllText(path, head + ",\n" + record + "\n  ]\n}\n");
        }

        static async Task WaitGameSeconds(float seconds)
        {
            double end = Time.unscaledTimeAsDouble + seconds;
            while (Application.isPlaying && Time.unscaledTimeAsDouble < end)
            {
                await NextEditorTick();
            }
        }

        static Task NextGameFrame()
        {
            int seen = Time.frameCount;
            var done = new TaskCompletionSource<bool>();
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!Application.isPlaying || Time.frameCount != seen)
                {
                    EditorApplication.update -= tick;
                    done.TrySetResult(true);
                }
            };
            EditorApplication.update += tick;
            return done.Task;
        }

        static Task NextEditorTick()
        {
            var done = new TaskCompletionSource<bool>();
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                EditorApplication.update -= tick;
                done.TrySetResult(true);
            };
            EditorApplication.update += tick;
            return done.Task;
        }

        static Transform FindRequired(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found == null)
            {
                Debug.LogError("[CaptureChamberViews] Required object is missing: " + name);
                return null;
            }

            return found.transform;
        }

        static void WalkHierarchy(Transform transform, string parent, List<string> lines)
        {
            string path = string.IsNullOrEmpty(parent) ? transform.name : parent + "/" + transform.name;
            lines.Add(path);
            for (int i = 0; i < transform.childCount; i++)
            {
                WalkHierarchy(transform.GetChild(i), path, lines);
            }
        }

        static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        static string F(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        static string Nullable(double? value)
        {
            return value.HasValue ? F(value.Value) : "null";
        }

        readonly struct CaptureState
        {
            public CaptureState(string name, float settleSeconds, Func<bool> apply)
            {
                Name = name;
                SettleSeconds = settleSeconds;
                Apply = apply;
            }

            public string Name { get; }
            public float SettleSeconds { get; }
            public Func<bool> Apply { get; }
        }

        readonly struct CaptureView
        {
            public CaptureView(string name, Vector3 position, Vector3 lookAt)
            {
                Name = name;
                Position = position;
                LookAt = lookAt;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public Vector3 LookAt { get; }
        }

        readonly struct ScreenRect
        {
            public ScreenRect(float xMin, float yMin, float xMax, float yMax)
            {
                XMin = xMin;
                YMin = yMin;
                XMax = xMax;
                YMax = yMax;
            }

            public float XMin { get; }
            public float YMin { get; }
            public float XMax { get; }
            public float YMax { get; }
        }

        [Serializable]
        public sealed class ClippedThreshold
        {
            public float clippedLuminanceMin;
        }
    }
}
