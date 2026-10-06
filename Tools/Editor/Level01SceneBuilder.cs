using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Convergence.Core.Neural;
using Convergence.Gameplay;
using Convergence.XR;
using Convergence.Presentation;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Autonomous scene and asset builder for Level 1 — The Awakening Gate.
    /// Assembles an ultra-sleek, futuristic Cyber-Quantum Laboratory:
    /// - Deep Obsidian Titanium, Polished Silicon, and Velvet Dark Carbon architecture.
    /// - Centerpiece: Floating 360-degree rotating 3D Neural Network with Ruby Coral/Amber Inputs, Cyber Violet Hidden Nodes, and Mint Emerald Outputs.
    /// - Low-profile Tactile Console framing the bottom of the viewport with zero visual occlusion.
    /// - Dual Holographic Telemetry Stations (Neuron Architecture breakdown on left, Diagnostic Truth Table on right).
    /// - Cinematic wide-angle framing with rich depth, refined contrast, and mature sci-fi aesthetics.
    /// </summary>
    public static partial class Level01SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Level01_AwakeningGate.unity";
        private const string PresetsFolder = "Assets/Puzzles/Chamber01";
        private const string MaterialsFolder = "Assets/Materials";

        // Official Unity Sample Asset Paths
        private const string PathBlaster = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/Primitive_Blaster.fbx";
        private const string PathBlasterLong = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/Primitive_Blaster_Long.fbx";
        private const string PathPushButton = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/PushButton.fbx";
        private const string PathTorus = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/Primitive_Torus.fbx";
        private const string PathTorusCut = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/Primitive_Torus_Cut.fbx";
        private const string PathPyramid = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/Primitive_Pyramid.fbx";
        private const string PathTaperedCyl = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Models/Primitive_Tapered_Cylinder.fbx";
        private const string PathWedge = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Models/Primitive_Wedge.fbx";
        private const string PathDisc = "Assets/Samples/XR Interaction Toolkit/3.6.0/Hands Interaction Demo/DemoAssets/Models/Primitives/Disc.fbx";
        private const string PathTabletop = "Assets/Samples/XR Interaction Toolkit/3.6.0/Hands Interaction Demo/DemoAssets/Models/VirtualTabletop.fbx";
        private const string PathPlatform = "Assets/Samples/XR Interaction Toolkit/3.6.0/Hands Interaction Demo/DemoAssets/Models/TeleportAnchorPlatform.fbx";
        private const string PathController = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Models/UniversalController.fbx";
        private const string PathButtonPopAudio = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Audio/Button Pop.wav";
        private const string PathConcreteAlbedo = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Textures/Concrete_Albedo.tif";
        private const string PathConcreteNormal = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Textures/Concrete_Normal.tif";
        private const string PathConcreteMetallic = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/DemoAssets/Textures/Concrete_Metallic.tif";

        // Open-Source Kenney Space Station Asset Paths (CC0 1.0 Universal)
        private const string PathKenneyBase = "Assets/ThirdParty/Kenney/SpaceStation/Models/";
        private const string PathKenneyWall = PathKenneyBase + "wall.fbx";
        private const string PathKenneyWallCornerRound = PathKenneyBase + "wall-corner-round.fbx";
        private const string PathKenneyWallPillar = PathKenneyBase + "wall-pillar.fbx";
        private const string PathKenneyWallDetail = PathKenneyBase + "wall-detail.fbx";
        private const string PathKenneyWallDoorWide = PathKenneyBase + "wall-door-wide.fbx";
        private const string PathKenneyWallWindow = PathKenneyBase + "wall-window.fbx";
        private const string PathKenneyWallWindowFrame = PathKenneyBase + "wall-window-frame.fbx";
        private const string PathKenneyWallWindowShutters = PathKenneyBase + "wall-window-shutters.fbx";
        private const string PathKenneyFloor = PathKenneyBase + "floor.fbx";
        private const string PathKenneyFloorPanel = PathKenneyBase + "floor-panel.fbx";
        private const string PathKenneyFloorDetail = PathKenneyBase + "floor-detail.fbx";
        private const string PathKenneyBalconyRail = PathKenneyBase + "balcony-rail.fbx";
        private const string PathKenneyBalconyRailCorner = PathKenneyBase + "balcony-rail-corner.fbx";
        private const string PathKenneyTableDisplayPlanet = PathKenneyBase + "table-display-planet.fbx";
        private const string PathKenneyTableDisplay = PathKenneyBase + "table-display.fbx";
        private const string PathKenneyTableLarge = PathKenneyBase + "table-large.fbx";
        private const string PathKenneyComputerScreen = PathKenneyBase + "computer-screen.fbx";
        private const string PathKenneyComputerWide = PathKenneyBase + "computer-wide.fbx";
        private const string PathKenneyComputerSystem = PathKenneyBase + "computer-system.fbx";
        private const string PathKenneyChair = PathKenneyBase + "chair-armrest-headrest.fbx";
        private const string PathKenneyStructure = PathKenneyBase + "structure.fbx";
        private const string PathKenneyStructurePanel = PathKenneyBase + "structure-panel.fbx";
        private const string PathKenneyPipe = PathKenneyBase + "pipe.fbx";
        private const string PathKenneyPipeRing = PathKenneyBase + "pipe-ring-colored.fbx";
        private const string PathKenneyPipeBend = PathKenneyBase + "pipe-bend.fbx";
        private const string PathKenneyTexture = "Assets/ThirdParty/Kenney/SpaceStation/Textures/variation-a.png";

        private static GameObject CreateModelInstance(string path, string name, Transform parent = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null)
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                instance.name = name;
                return instance;
            }
            return null;
        }

        private static Mesh GetSampleMesh(string path)
        {
            var directMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (directMesh != null) return directMesh;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                foreach (var obj in all)
                {
                    if (obj is Mesh m && !string.IsNullOrEmpty(m.name) && !m.name.StartsWith("__preview__"))
                        return m;
                }
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null)
            {
                var mf = asset.GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null)
                    return mf.sharedMesh;
            }
            return null;
        }

        private static Mesh CreateProceduralTorusMesh(float radius = 1.0f, float tubeRadius = 0.04f, int radialSegments = 36, int tubularSegments = 16)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Procedural_Torus";
            Vector3[] vertices = new Vector3[(radialSegments + 1) * (tubularSegments + 1)];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[radialSegments * tubularSegments * 6];

            for (int i = 0; i <= radialSegments; i++)
            {
                float u = (float)i / radialSegments * Mathf.PI * 2f;
                for (int j = 0; j <= tubularSegments; j++)
                {
                    float v = (float)j / tubularSegments * Mathf.PI * 2f;
                    int idx = i * (tubularSegments + 1) + j;
                    float x = (radius + tubeRadius * Mathf.Cos(v)) * Mathf.Cos(u);
                    float y = tubeRadius * Mathf.Sin(v);
                    float z = (radius + tubeRadius * Mathf.Cos(v)) * Mathf.Sin(u);
                    vertices[idx] = new Vector3(x, y, z);
                    normals[idx] = new Vector3(Mathf.Cos(v) * Mathf.Cos(u), Mathf.Sin(v), Mathf.Cos(v) * Mathf.Sin(u));
                    uv[idx] = new Vector2((float)i / radialSegments, (float)j / tubularSegments);
                }
            }

            int t = 0;
            for (int i = 0; i < radialSegments; i++)
            {
                for (int j = 0; j < tubularSegments; j++)
                {
                    int a = i * (tubularSegments + 1) + j;
                    int b = (i + 1) * (tubularSegments + 1) + j;
                    int c = (i + 1) * (tubularSegments + 1) + j + 1;
                    int d = i * (tubularSegments + 1) + j + 1;

                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = d;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetMeshOrKeepPrimitive(GameObject go, string samplePath, bool isTorus = false)
        {
            var sampleMesh = GetSampleMesh(samplePath);
            if (sampleMesh == null && isTorus)
            {
                sampleMesh = CreateProceduralTorusMesh(1.0f, 0.04f, 36, 16);
            }
            if (sampleMesh != null)
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = sampleMesh;
            }
        }

        private static void ApplyMaterialRecursively(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
            {
                rend.sharedMaterial = mat;
            }
        }

        private static AudioClip GetSampleAudio(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static Texture2D GetSampleTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public struct LevelMaterials
        {
            public Material DarkPlating;
            public Material CpuSubstrate;
            public Material BusTraceGlow;
            public Material WallPillarAccent;
            public Material HeatSinkFins;
            public Material MemoryChip;
            public Material AxonPositive;
            public Material AxonNegative;
            public Material AxonNeutral;
            public Material GlowCyan;
            public Material GlowAmber;
            public Material GlowEmerald;
            public Material GlowViolet;
            public Material GlowCoral;
            public Material GlassHologram;
            public Material CorePlasma;
            public Material PortalCurtain;
            public Material GunMetal;
            public Material GunAccent;
            public Material StanchionGlow;

            // Spaceship Bridge & Deep Space Architecture Materials
            public Material SpaceshipHull;
            public Material CeilingValue;
            public Material BulkheadRib;
            public Material ConduitPipe;
            public Material HazardYellow;
            public Material HazardDark;
            public Material WarningRed;
            public Material WarningAmber;
            public Material CeilingLight;
            public Material EarthWater;
            public Material EarthLand;
            public Material EarthAtmosphere;
            public Material SpaceAsteroid;
            public Material StarGlow;
            public Material PlanetGasGiant;
            public Material PlanetRings;
            public Material ViewportGlass;
            public Material EarthPhotoDisplay;
            public Material KenneyStation;
        }

        [MenuItem("Convergence/Build Level 1 — The Awakening Gate")]
        public static void BuildLevel01()
        {
            Debug.Log("[Level01SceneBuilder] Assembling futuristic Level 1 — The Awakening Gate...");

            // Ensure URP RenderPipelineAsset is configured in GraphicsSettings and QualitySettings per AGENTS.md
            const string PathURPAsset = "Assets/Samples/Universal Render Pipeline/17.6.0/URP Package Samples/SharedAssets/Settings/PackageSamplesURPAsset.asset";
            var urpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(PathURPAsset);
            if (urpAsset != null)
            {
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = urpAsset;
                UnityEngine.QualitySettings.renderPipeline = urpAsset;
                Debug.Log($"[Level01SceneBuilder] Configured URP RenderPipelineAsset: {urpAsset.name}");
            }
            else
            {
                Debug.LogWarning("[Level01SceneBuilder] URP RenderPipelineAsset not found at: " + PathURPAsset);
            }

            // 1. Ensure target directories exist
            EnsureDirectories();

            // 2. Generate Broken Configuration ScriptableObject Presets
            var presets = GenerateBrokenPresets();

            // 3. Generate Cohesive Futuristic Sci-Fi Materials Palette
            var mats = GenerateMaterials();

            // 4. Assemble Unity Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Environment Root ---
            var envRoot = new GameObject("MainframeEnvironment");

            // Configure Ambient Modern Spaceship Bridge Lighting (Cinematic Sci-Fi Palette: Rich contrast, starlight depth, crisp highlights)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.13f, 0.20f);
            RenderSettings.skybox = null;
            RenderSettings.ambientIntensity = 0.75f;
            RenderSettings.reflectionIntensity = 0.60f;

            // Key Directional Light: Stellar Light Streaming In from Deep Space Viewport
            var keyLightGo = new GameObject("DirectionalLight_StellarKey");
            keyLightGo.transform.SetParent(envRoot.transform);
            var keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.92f, 0.96f, 1.0f);
            keyLight.intensity = 1.05f;
            keyLightGo.transform.rotation = Quaternion.Euler(20f, 180f, 0f);

            // Fill / Rim Light: Interior Deck Sci-Fi Ambient Fill
            var fillLightGo = new GameObject("DirectionalLight_DeckFill");
            fillLightGo.transform.SetParent(envRoot.transform);
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.25f, 0.35f, 0.55f);
            fillLight.intensity = 0.45f;
            fillLightGo.transform.rotation = Quaternion.Euler(55f, 30f, 0f);

            // Hull is a Kenney-module prefab. The scene only instantiates it.
            PlaceShip(envRoot.transform, mats);
            AssignStarfield();
            CreateBloomVolume(envRoot.transform);

            // 2. Panoramic Forward Observation Viewport & Deep Space Vista (Starfield, Ringed Gas Giant, Asteroids)
            CreateForwardObservationViewportAndSpaceVista(envRoot.transform, mats);

            // 3. Ergonomic Low Tactical Command Bridge Rail (Replaces Dark Obstructive Shield Sheet)
            CreateTactileCommandBridgeRail(envRoot.transform, mats);

            // Motherboard Circuit Bus-Traces etched across the deck
            CreateBusTraces(envRoot.transform, mats.BusTraceGlow);

            // Perimeter Auxiliary Computing Banks & Heat Sink Fin Columns
            CreatePerimeterHardware(envRoot.transform, mats);

            // Ambient Data Stream Particles
            CreateDataParticles(envRoot.transform, mats.BusTraceGlow);

            // Emergency System Spark Particles (Signaling Ship Breakdown Crisis)
            CreateSparkParticles(envRoot.transform, mats.WarningAmber);


            // --- Gameplay Controllers Root ---
            var controllersGo = new GameObject("GameplayControllers");
            var neuralState = controllersGo.AddComponent<NeuralState>();
            var performanceTracker = controllersGo.AddComponent<PerformanceTracker>();
            var chamberController = controllersGo.AddComponent<ChamberController>();
            var levelResetter = controllersGo.AddComponent<LevelResetter>();
            var audioHookManager = controllersGo.AddComponent<AudioHookManager>();

            // Configure LevelResetter
            var resetterSO = new SerializedObject(levelResetter);
            resetterSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            resetterSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            resetterSO.FindProperty("performanceTracker").objectReferenceValue = performanceTracker;
            resetterSO.FindProperty("activePreset").objectReferenceValue = presets[0];

            var presetsArrayProp = resetterSO.FindProperty("availablePresets");
            presetsArrayProp.arraySize = presets.Length;
            for (int i = 0; i < presets.Length; i++)
            {
                presetsArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = presets[i];
            }
            resetterSO.ApplyModifiedProperties();

            // Configure ChamberController
            var chamberSO = new SerializedObject(chamberController);
            chamberSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            chamberSO.FindProperty("performanceTracker").objectReferenceValue = performanceTracker;
            chamberSO.FindProperty("initialPreset").objectReferenceValue = presets[0];
            chamberSO.FindProperty("externalWaveDirector").boolValue = true;
            chamberSO.FindProperty("enableSandboxMode").boolValue = false;
            chamberSO.FindProperty("enableSoftReroute").boolValue = true;
            chamberSO.ApplyModifiedProperties();

            // --- 3D Floating & Rotating Neural Network Visualizer (Centerpiece, Elevated at Eye Level) ---
            var neuronMachineGo = new GameObject("ClassicNeuralNetwork_3D");
            neuronMachineGo.transform.position = new Vector3(0, 1.12f, 0.22f);

            // Dedicated subtle spotlight illuminating the neural network
            var netSpotGo = new GameObject("NeuralNetwork_Spotlight");
            netSpotGo.transform.SetParent(neuronMachineGo.transform, false);
            netSpotGo.transform.position = new Vector3(0, 3.15f, 0.55f);
            netSpotGo.transform.rotation = Quaternion.Euler(90f, 0, 0);
            var netSpot = netSpotGo.AddComponent<Light>();
            netSpot.type = LightType.Spot;
            netSpot.range = 5.0f;
            netSpot.spotAngle = 75f;
            netSpot.intensity = 2.6f;
            netSpot.color = new Color(0.70f, 0.90f, 1.0f);

            // Cyan aura so the network lights the surrounding bulkheads and reads as the focal point of the room
            var netAuraGo = new GameObject("NeuralNetwork_Aura");
            netAuraGo.transform.SetParent(neuronMachineGo.transform, false);
            netAuraGo.transform.localPosition = Vector3.zero;
            var netAura = netAuraGo.AddComponent<Light>();
            netAura.type = LightType.Point;
            netAura.range = 4.5f;
            netAura.intensity = 1.9f;
            netAura.color = new Color(0.30f, 0.80f, 1.0f);

            // Dark holo-stage behind the network so it reads against the bright viewport
            var netBackdropGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            netBackdropGo.name = "NeuralNetwork_Backdrop";
            netBackdropGo.transform.SetParent(neuronMachineGo.transform, false);
            netBackdropGo.transform.position = new Vector3(0, 1.12f, 0.55f);
            netBackdropGo.transform.localScale = new Vector3(1.05f, 0.72f, 1f);
            netBackdropGo.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateMaterial("Mat_NeuralNetBackdrop", new Color(0.0f, 0.01f, 0.03f, 0.78f), 0.0f, 0.1f, isTransparent: true);
            UnityEngine.Object.DestroyImmediate(netBackdropGo.GetComponent<Collider>());

            // Glowing floor projection ring directly beneath (Sample Torus model)
            var floorRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floorRing.name = "FloorProjectionEmitter";
            SetMeshOrKeepPrimitive(floorRing, PathTorus, true);
            floorRing.transform.position = new Vector3(0, 0.04f, 0.55f);
            floorRing.transform.localScale = new Vector3(2.2f, 0.20f, 2.2f);
            floorRing.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            CreatePlatformTrim(floorRing.transform, new Vector3(0, 0.52f, 0), new Vector3(1.01f, 0.02f, 1.01f), mats.GunMetal);
            UnityEngine.Object.DestroyImmediate(floorRing.GetComponent<Collider>());

            // Rotating Container Pivot
            var rotationPivotGo = new GameObject("RotationPivot");
            rotationPivotGo.transform.SetParent(neuronMachineGo.transform, false);
            rotationPivotGo.transform.localPosition = Vector3.zero;
            rotationPivotGo.transform.localScale = Vector3.one * 0.85f;

            // Dual Gyroscopic Orbital Rings (Smooth Torus Meshes framing the central network)
            CreateOrbitRing("OrbitalGyroRing_Pitch", rotationPivotGo.transform, 0.58f, Quaternion.Euler(72f, 0f, 0f), mats.AxonNeutral);
            CreateOrbitRing("OrbitalGyroRing_Yaw", rotationPivotGo.transform, 0.66f, Quaternion.Euler(82f, 0f, 0f), mats.GlowCyan);

            var visualizer = neuronMachineGo.AddComponent<NeuronTopologyVisualizer>();
            var visSO = new SerializedObject(visualizer);
            visSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            visSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            visSO.FindProperty("networkPivot").objectReferenceValue = rotationPivotGo.transform;
            visSO.FindProperty("inputMaterial").objectReferenceValue = mats.GlowCoral;
            visSO.FindProperty("biasMaterial").objectReferenceValue = mats.GlowAmber;
            visSO.FindProperty("neuronMaterial").objectReferenceValue = mats.AxonNeutral;
            visSO.FindProperty("outputMaterial").objectReferenceValue = mats.GlowCyan;
            visSO.FindProperty("synapseMaterial").objectReferenceValue = mats.AxonPositive;
            visSO.FindProperty("packetMaterial").objectReferenceValue = mats.CorePlasma;
            visSO.ApplyModifiedProperties();
            visualizer.RebuildFromNetwork(NetworkModel.CreateSingleNeuronNetwork(2));

            // --- Ergonomic Tactile Engineering Workstation (VR Ready Height) ---
            var consoleGo = new GameObject("TactileEngineeringWorkstation");
            consoleGo.transform.position = new Vector3(0, 0, -0.22f);

            // Authentic Kenney Sci-Fi Flight Command Desk (table-large.fbx)
            var flightDeskModel = CreateModelInstance(PathKenneyTableLarge, "ConsoleDesk_FlightModel", consoleGo.transform);
            if (flightDeskModel != null)
            {
                flightDeskModel.transform.localPosition = new Vector3(0, 0f, 0f);
                flightDeskModel.transform.localScale = new Vector3(1.30f, 1.0f, 1.0f);
                flightDeskModel.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                ApplyMaterialRecursively(flightDeskModel, mats.DarkPlating);
            }

            // Solid backing desk collider so interactors and controllers don't fall through
            var consoleTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleTable.name = "ConsoleTable_Collider";
            consoleTable.transform.SetParent(consoleGo.transform, false);
            consoleTable.transform.localPosition = new Vector3(0, 0.425f, 0);
            consoleTable.transform.localScale = new Vector3(1.40f, 0.85f, 0.45f);
            consoleTable.GetComponent<Renderer>().enabled = false;

            // Inset Flight Avionics & Reconnaissance Terminal positioned on starboard wing
            var avionicsScreen = CreateModelInstance(PathKenneyComputerWide, "Console_AvionicsTerminal", consoleGo.transform);
            if (avionicsScreen != null)
            {
                avionicsScreen.transform.localPosition = new Vector3(0.90f, 0.86f, 0.05f);
                avionicsScreen.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                avionicsScreen.transform.localRotation = Quaternion.Euler(0, -32f, 0);
                ApplyMaterialRecursively(avionicsScreen, mats.DarkPlating);
            }

            // Pilot & Navigator Bridge Command Seats (chair-armrest-headrest.fbx)
            var leftChair = CreateModelInstance(PathKenneyChair, "BridgeChair_Left", consoleGo.transform);
            if (leftChair != null)
            {
                leftChair.transform.localPosition = new Vector3(-1.85f, 0, 0.15f);
                leftChair.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                leftChair.transform.localRotation = Quaternion.Euler(0, 22f, 0);
                ApplyMaterialRecursively(leftChair, mats.KenneyStation);
            }

            var rightChair = CreateModelInstance(PathKenneyChair, "BridgeChair_Right", consoleGo.transform);
            if (rightChair != null)
            {
                rightChair.transform.localPosition = new Vector3(1.85f, 0, 0.15f);
                rightChair.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                rightChair.transform.localRotation = Quaternion.Euler(0, -22f, 0);
                ApplyMaterialRecursively(rightChair, mats.KenneyStation);
            }

            var machineVisual = consoleGo.AddComponent<NeuronMachineVisual>();

            // --- Kinetic Power Sliders: W1 (Radiation Sensitivity) & W2 (Bio Sensitivity) ---
            var sliderW1Track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sliderW1Track.name = "SliderTrack_W1";
            sliderW1Track.transform.SetParent(consoleGo.transform, false);
            sliderW1Track.transform.localPosition = new Vector3(-0.32f, 0.85f, -0.05f);
            sliderW1Track.transform.localScale = new Vector3(0.07f, 0.02f, 0.28f);
            sliderW1Track.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var w1PlasmaGauge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w1PlasmaGauge.name = "PlasmaGauge_W1";
            w1PlasmaGauge.transform.SetParent(sliderW1Track.transform, false);
            w1PlasmaGauge.transform.localPosition = new Vector3(0, 0.6f, 0);
            w1PlasmaGauge.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            w1PlasmaGauge.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var w1GaugeRend = w1PlasmaGauge.GetComponent<Renderer>();
            w1GaugeRend.sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(w1PlasmaGauge.GetComponent<Collider>());

            var w1Handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w1Handle.name = "SliderHandle_W1";
            w1Handle.transform.SetParent(sliderW1Track.transform, false);
            w1Handle.transform.localPosition = new Vector3(0, 1.1f, 0);
            w1Handle.transform.localScale = new Vector3(1.2f, 1.4f, 0.22f);
            w1Handle.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;

            var sliderW1 = sliderW1Track.AddComponent<KineticWeightSliderInteractor>();
            var s1SO = new SerializedObject(sliderW1);
            s1SO.FindProperty("socketIndex").intValue = 0;
            s1SO.FindProperty("sensorName").stringValue = "Earth Continental Sensor";
            s1SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            s1SO.FindProperty("sliderHandle").objectReferenceValue = w1Handle.transform;
            s1SO.FindProperty("plasmaGaugeRenderer").objectReferenceValue = w1GaugeRend;
            s1SO.ApplyModifiedProperties();

            // Backward-compatible WeightRegulatorInteractor component
            var dialW1 = sliderW1Track.AddComponent<WeightRegulatorInteractor>();
            var d1SO = new SerializedObject(dialW1);
            d1SO.FindProperty("socketIndex").intValue = 0;
            d1SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            d1SO.ApplyModifiedProperties();

            var w1Light = sliderW1Track.AddComponent<Light>();
            w1Light.type = LightType.Point;
            w1Light.range = 0.9f;
            w1Light.intensity = 0.8f;
            w1Light.color = new Color(0.20f, 0.75f, 0.98f);

            var w1Label = new GameObject("W1_Label");
            w1Label.transform.SetParent(consoleGo.transform, false);
            w1Label.transform.localPosition = new Vector3(-0.32f, 0.90f, -0.21f);
            w1Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var w1Txt = w1Label.AddComponent<TextMesh>();
            w1Txt.text = "W1";
            w1Txt.fontSize = 17;
            w1Txt.characterSize = 0.011f;
            w1Txt.fontStyle = FontStyle.Bold;
            w1Txt.alignment = TextAlignment.Center;
            w1Txt.anchor = TextAnchor.MiddleCenter;
            w1Txt.color = new Color(0.35f, 0.85f, 1.0f);

            var sliderW2Track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sliderW2Track.name = "SliderTrack_W2";
            sliderW2Track.transform.SetParent(consoleGo.transform, false);
            sliderW2Track.transform.localPosition = new Vector3(0.32f, 0.85f, -0.05f);
            sliderW2Track.transform.localScale = new Vector3(0.07f, 0.02f, 0.28f);
            sliderW2Track.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var w2PlasmaGauge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w2PlasmaGauge.name = "PlasmaGauge_W2";
            w2PlasmaGauge.transform.SetParent(sliderW2Track.transform, false);
            w2PlasmaGauge.transform.localPosition = new Vector3(0, 0.6f, 0);
            w2PlasmaGauge.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            w2PlasmaGauge.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var w2GaugeRend = w2PlasmaGauge.GetComponent<Renderer>();
            w2GaugeRend.sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(w2PlasmaGauge.GetComponent<Collider>());

            var w2Handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w2Handle.name = "SliderHandle_W2";
            w2Handle.transform.SetParent(sliderW2Track.transform, false);
            w2Handle.transform.localPosition = new Vector3(0, 1.1f, 0);
            w2Handle.transform.localScale = new Vector3(1.2f, 1.4f, 0.22f);
            w2Handle.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;

            var sliderW2 = sliderW2Track.AddComponent<KineticWeightSliderInteractor>();
            var s2SO = new SerializedObject(sliderW2);
            s2SO.FindProperty("socketIndex").intValue = 1;
            s2SO.FindProperty("sensorName").stringValue = "Earth Atmosphere Sensor";
            s2SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            s2SO.FindProperty("sliderHandle").objectReferenceValue = w2Handle.transform;
            s2SO.FindProperty("plasmaGaugeRenderer").objectReferenceValue = w2GaugeRend;
            s2SO.ApplyModifiedProperties();

            // Backward-compatible WeightRegulatorInteractor component
            var dialW2 = sliderW2Track.AddComponent<WeightRegulatorInteractor>();
            var d2SO = new SerializedObject(dialW2);
            d2SO.FindProperty("socketIndex").intValue = 1;
            d2SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            d2SO.ApplyModifiedProperties();

            var w2Light = sliderW2Track.AddComponent<Light>();
            w2Light.type = LightType.Point;
            w2Light.range = 0.9f;
            w2Light.intensity = 0.8f;
            w2Light.color = new Color(0.20f, 0.75f, 0.98f);

            var w2Label = new GameObject("W2_Label");
            w2Label.transform.SetParent(consoleGo.transform, false);
            w2Label.transform.localPosition = new Vector3(0.32f, 0.90f, -0.21f);
            w2Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var w2Txt = w2Label.AddComponent<TextMesh>();
            w2Txt.text = "W2";
            w2Txt.fontSize = 17;
            w2Txt.characterSize = 0.011f;
            w2Txt.fontStyle = FontStyle.Bold;
            w2Txt.alignment = TextAlignment.Center;
            w2Txt.anchor = TextAnchor.MiddleCenter;
            w2Txt.color = new Color(0.35f, 0.85f, 1.0f);

            // --- Threshold Squelch Valve (Bias Dial) ---
            var biasGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            biasGo.name = "ThresholdSquelchValve";
            biasGo.transform.SetParent(consoleGo.transform, false);
            biasGo.transform.localPosition = new Vector3(0.0f, 0.87f, 0.04f);
            biasGo.transform.localScale = new Vector3(0.13f, 0.035f, 0.13f);
            var biasRend = biasGo.GetComponent<Renderer>();
            biasRend.sharedMaterial = mats.GunAccent;

            var dialB = biasGo.AddComponent<BiasDialInteractor>();
            var dbSO = new SerializedObject(dialB);
            dbSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            dbSO.FindProperty("valveWheelTransform").objectReferenceValue = biasGo.transform;
            dbSO.FindProperty("valveGlowRenderer").objectReferenceValue = biasRend;
            dbSO.ApplyModifiedProperties();

            var biasLight = biasGo.AddComponent<Light>();
            biasLight.type = LightType.Point;
            biasLight.range = 0.9f;
            biasLight.intensity = 0.8f;
            biasLight.color = new Color(0.98f, 0.70f, 0.15f);

            var biasLabel = new GameObject("Bias_Label");
            biasLabel.transform.SetParent(consoleGo.transform, false);
            biasLabel.transform.localPosition = new Vector3(0.0f, 0.90f, 0.13f);
            biasLabel.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var biasTxt = biasLabel.AddComponent<TextMesh>();
            biasTxt.text = "TRIGGER BIAS";
            biasTxt.fontSize = 17;
            biasTxt.characterSize = 0.011f;
            biasTxt.fontStyle = FontStyle.Bold;
            biasTxt.alignment = TextAlignment.Center;
            biasTxt.anchor = TextAnchor.MiddleCenter;
            biasTxt.color = new Color(0.98f, 0.70f, 0.15f);

            // Central Activation Socket & Crystal Models
            var socketGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            socketGo.name = "ActivationCrystalSocket";
            socketGo.transform.SetParent(consoleGo.transform, false);
            socketGo.transform.localPosition = new Vector3(0.0f, 0.86f, -0.16f);
            socketGo.transform.localScale = new Vector3(0.12f, 0.030f, 0.12f);
            socketGo.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            SetMeshOrKeepPrimitive(socketGo, PathDisc);
            var socketInteractor = socketGo.AddComponent<ActivationSocketInteractor>();
            var skSO = new SerializedObject(socketInteractor);
            skSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            skSO.ApplyModifiedProperties();

            var crystalRootGo = new GameObject("SocketedCrystalRoot");
            crystalRootGo.transform.SetParent(socketGo.transform, false);
            crystalRootGo.transform.localPosition = new Vector3(0, 1.2f, 0);

            var linearCry = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            linearCry.name = "Crystal_Linear";
            linearCry.transform.SetParent(crystalRootGo.transform, false);
            linearCry.transform.localPosition = Vector3.zero;
            linearCry.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            linearCry.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            SetMeshOrKeepPrimitive(linearCry, PathTaperedCyl);
            UnityEngine.Object.DestroyImmediate(linearCry.GetComponent<Collider>());

            var reluCry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reluCry.name = "Crystal_ReLU";
            reluCry.transform.SetParent(crystalRootGo.transform, false);
            reluCry.transform.localPosition = Vector3.zero;
            reluCry.transform.localScale = new Vector3(0.14f, 0.20f, 0.14f);
            reluCry.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            reluCry.GetComponent<Renderer>().sharedMaterial = mats.GlowAmber;
            SetMeshOrKeepPrimitive(reluCry, PathPyramid);
            UnityEngine.Object.DestroyImmediate(reluCry.GetComponent<Collider>());

            var stepCry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepCry.name = "Crystal_Step";
            stepCry.transform.SetParent(crystalRootGo.transform, false);
            stepCry.transform.localPosition = Vector3.zero;
            stepCry.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
            stepCry.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            stepCry.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            SetMeshOrKeepPrimitive(stepCry, PathWedge);
            UnityEngine.Object.DestroyImmediate(stepCry.GetComponent<Collider>());

            var sigCry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sigCry.name = "Crystal_Sigmoid";
            sigCry.transform.SetParent(crystalRootGo.transform, false);
            sigCry.transform.localPosition = Vector3.zero;
            sigCry.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            sigCry.GetComponent<Renderer>().sharedMaterial = mats.GlowViolet;
            SetMeshOrKeepPrimitive(sigCry, PathDisc);
            UnityEngine.Object.DestroyImmediate(sigCry.GetComponent<Collider>());

            linearCry.SetActive(true);
            reluCry.SetActive(false);
            stepCry.SetActive(false);
            sigCry.SetActive(false);

            var crystalLight = crystalRootGo.AddComponent<Light>();
            crystalLight.type = LightType.Point;
            crystalLight.range = 1.0f;
            crystalLight.intensity = 0.6f;
            crystalLight.color = new Color(0.20f, 0.75f, 0.98f);

            var crystalVisual = crystalRootGo.AddComponent<ActivationCrystalVisual>();
            var crySO = new SerializedObject(crystalVisual);
            crySO.FindProperty("neuralState").objectReferenceValue = neuralState;
            crySO.FindProperty("linearCrystalModel").objectReferenceValue = linearCry;
            crySO.FindProperty("reluCrystalModel").objectReferenceValue = reluCry;
            crySO.FindProperty("stepCrystalModel").objectReferenceValue = stepCry;
            crySO.FindProperty("sigmoidCrystalModel").objectReferenceValue = sigCry;
            crySO.FindProperty("crystalGlowLight").objectReferenceValue = crystalLight;
            crySO.ApplyModifiedProperties();

            var socketLabel = new GameObject("Socket_Label");
            socketLabel.transform.SetParent(consoleGo.transform, false);
            socketLabel.transform.localPosition = new Vector3(0.0f, 0.90f, -0.24f);
            socketLabel.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var socketTxt = socketLabel.AddComponent<TextMesh>();
            socketTxt.text = "FIRE";
            socketTxt.fontSize = 18;
            socketTxt.characterSize = 0.012f;
            socketTxt.fontStyle = FontStyle.Bold;
            socketTxt.alignment = TextAlignment.Center;
            socketTxt.anchor = TextAnchor.MiddleCenter;
            socketTxt.color = new Color(0.20f, 0.90f, 0.55f);

            // Master Clock Cycle / Ignition Lever with Tactile Push Button Model & Audio
            var leverBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverBase.name = "ClockPulseLever_Base";
            leverBase.transform.SetParent(consoleGo.transform, false);
            leverBase.transform.localPosition = new Vector3(0.52f, 0.87f, -0.06f);
            leverBase.transform.localScale = new Vector3(0.11f, 0.040f, 0.13f);
            leverBase.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            SetMeshOrKeepPrimitive(leverBase, PathPushButton);

            var leverLight = leverBase.AddComponent<Light>();
            leverLight.type = LightType.Point;
            leverLight.range = 1.0f;
            leverLight.intensity = 0.8f;
            leverLight.color = new Color(0.98f, 0.70f, 0.15f);

            var leverLabel = new GameObject("Lever_Label");
            leverLabel.transform.SetParent(consoleGo.transform, false);
            leverLabel.transform.localPosition = new Vector3(0.52f, 0.90f, -0.14f);
            leverLabel.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var leverTxt = leverLabel.AddComponent<TextMesh>();
            leverTxt.text = "SELF-TEST";
            leverTxt.fontSize = 18;
            leverTxt.characterSize = 0.012f;
            leverTxt.fontStyle = FontStyle.Bold;
            leverTxt.alignment = TextAlignment.Center;
            leverTxt.anchor = TextAnchor.MiddleCenter;
            leverTxt.color = new Color(0.98f, 0.70f, 0.15f);

            var leverArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leverArm.name = "LeverArm";
            leverArm.transform.SetParent(leverBase.transform, false);
            leverArm.transform.localPosition = new Vector3(0, 0.8f, 0);
            leverArm.transform.localScale = new Vector3(0.20f, 0.7f, 0.20f);
            leverArm.transform.localRotation = Quaternion.Euler(20f, 0, 0);
            leverArm.GetComponent<Renderer>().sharedMaterial = mats.GlowAmber;

            // Audio pop feedback from sample assets
            var popAudio = GetSampleAudio(PathButtonPopAudio);
            if (popAudio != null)
            {
                var leverAudio = leverBase.AddComponent<AudioSource>();
                leverAudio.clip = popAudio;
                leverAudio.playOnAwake = false;
                leverAudio.spatialBlend = 1.0f;
            }

            var leverInteractor = leverBase.AddComponent<ClockPulseLeverInteractor>();
            var leverSO = new SerializedObject(leverInteractor);
            leverSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            leverSO.FindProperty("leverArm").objectReferenceValue = leverArm.transform;
            leverSO.ApplyModifiedProperties();

            // Conduits / Patch Cables
            var cable1Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable1Go.name = "NeuralCable_1";
            cable1Go.transform.SetParent(consoleGo.transform, false);
            cable1Go.transform.localPosition = new Vector3(-0.48f, 0.72f, 0.05f);
            cable1Go.transform.localScale = new Vector3(0.035f, 0.24f, 0.035f);
            cable1Go.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            var cable1Interactable = cable1Go.AddComponent<CableInteractable>();
            var c1SO = new SerializedObject(cable1Interactable);
            c1SO.FindProperty("cableIndex").intValue = 0;
            c1SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            c1SO.ApplyModifiedProperties();

            var cable1Light = cable1Go.AddComponent<Light>();
            cable1Light.type = LightType.Point;
            cable1Light.range = 0.9f;
            cable1Light.intensity = 0.8f;
            cable1Light.color = new Color(0.20f, 0.75f, 0.98f);

            var c1Label = new GameObject("Cable1_Label");
            c1Label.transform.SetParent(consoleGo.transform, false);
            c1Label.transform.localPosition = new Vector3(-0.48f, 0.90f, 0.12f);
            c1Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var c1Txt = c1Label.AddComponent<TextMesh>();
            c1Txt.text = "ROCK SENSOR";
            c1Txt.fontSize = 15;
            c1Txt.characterSize = 0.010f;
            c1Txt.fontStyle = FontStyle.Bold;
            c1Txt.alignment = TextAlignment.Center;
            c1Txt.anchor = TextAnchor.MiddleCenter;
            c1Txt.color = new Color(0.98f, 0.65f, 0.35f);

            var cable2Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable2Go.name = "NeuralCable_2";
            cable2Go.transform.SetParent(consoleGo.transform, false);
            cable2Go.transform.localPosition = new Vector3(0.48f, 0.72f, 0.05f);
            cable2Go.transform.localScale = new Vector3(0.035f, 0.24f, 0.035f);
            cable2Go.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            var cable2Interactable = cable2Go.AddComponent<CableInteractable>();
            var c2SO = new SerializedObject(cable2Interactable);
            c2SO.FindProperty("cableIndex").intValue = 1;
            c2SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            c2SO.ApplyModifiedProperties();

            var cable2Light = cable2Go.AddComponent<Light>();
            cable2Light.type = LightType.Point;
            cable2Light.range = 0.9f;
            cable2Light.intensity = 0.8f;
            cable2Light.color = new Color(0.20f, 0.75f, 0.98f);

            var c2Label = new GameObject("Cable2_Label");
            c2Label.transform.SetParent(consoleGo.transform, false);
            c2Label.transform.localPosition = new Vector3(0.48f, 0.90f, 0.12f);
            c2Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var c2Txt = c2Label.AddComponent<TextMesh>();
            c2Txt.text = "ICE SENSOR";
            c2Txt.fontSize = 15;
            c2Txt.characterSize = 0.010f;
            c2Txt.fontStyle = FontStyle.Bold;
            c2Txt.alignment = TextAlignment.Center;
            c2Txt.anchor = TextAnchor.MiddleCenter;
            c2Txt.color = new Color(0.98f, 0.65f, 0.35f);

            // Link visual fields on NeuronMachineVisual
            var mvSO = new SerializedObject(machineVisual);
            mvSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            mvSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            mvSO.FindProperty("w1RegulatorDial").objectReferenceValue = sliderW1Track.transform;
            mvSO.FindProperty("w2RegulatorDial").objectReferenceValue = sliderW2Track.transform;
            mvSO.FindProperty("biasDial").objectReferenceValue = biasGo.transform;
            mvSO.FindProperty("corePulseLight").objectReferenceValue = crystalLight;
            mvSO.FindProperty("cable1Renderer").objectReferenceValue = cable1Go.GetComponent<Renderer>();
            mvSO.FindProperty("cable2Renderer").objectReferenceValue = cable2Go.GetComponent<Renderer>();
            mvSO.ApplyModifiedProperties();

            // --- Planetary AI Consciousness Holosphere (SYNAPSE-GPT Core) ---
            CreatePlanetaryAICoreHologram(envRoot.transform, mats, chamberController, neuralState);

            // Sentry turret removed: the neural network is the focal point of the room (Phase 1 premise lock).

            // --- The Awakening Blast Doors & Energy Portal Gateway ---
            var gateGo = new GameObject("AwakeningBlastDoors");
            gateGo.transform.position = new Vector3(0, 0, -3.45f);
            gateGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var leftPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPortal.name = "BlastDoor_LeftWing";
            leftPortal.transform.SetParent(gateGo.transform, false);
            leftPortal.transform.localPosition = new Vector3(-2.4f, 1.6f, 0);
            leftPortal.transform.localScale = new Vector3(1.6f, 3.2f, 0.3f);
            leftPortal.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;
            CreatePlatformTrim(leftPortal.transform, new Vector3(0.48f, 0, 0.52f), new Vector3(0.04f, 0.95f, 0.02f), mats.GunMetal);

            var rightPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPortal.name = "BlastDoor_RightWing";
            rightPortal.transform.SetParent(gateGo.transform, false);
            rightPortal.transform.localPosition = new Vector3(2.4f, 1.6f, 0);
            rightPortal.transform.localScale = new Vector3(1.6f, 3.2f, 0.3f);
            rightPortal.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;
            CreatePlatformTrim(rightPortal.transform, new Vector3(-0.48f, 0, 0.52f), new Vector3(0.04f, 0.95f, 0.02f), mats.GunMetal);

            var gateCurtainGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateCurtainGo.name = "PortalEnergyCurtain";
            gateCurtainGo.transform.SetParent(gateGo.transform, false);
            gateCurtainGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            gateCurtainGo.transform.localScale = new Vector3(3.2f, 2.8f, 0.04f);
            gateCurtainGo.GetComponent<Renderer>().sharedMaterial = mats.PortalCurtain;
            UnityEngine.Object.DestroyImmediate(gateCurtainGo.GetComponent<Collider>());

            // Transverse Arch Lintel Beam above blast door
            var gateLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateLintel.name = "BlastDoor_ArchLintel";
            gateLintel.transform.SetParent(gateGo.transform, false);
            gateLintel.transform.localPosition = new Vector3(0, 3.35f, 0);
            gateLintel.transform.localScale = new Vector3(7.2f, 0.35f, 0.5f);
            gateLintel.GetComponent<Renderer>().sharedMaterial = mats.BulkheadRib;

            var gateLightGo = new GameObject("GateAuraLight");
            gateLightGo.transform.SetParent(gateGo.transform, false);
            gateLightGo.transform.localPosition = new Vector3(0, 2.2f, -0.3f);
            var gateLight = gateLightGo.AddComponent<Light>();
            gateLight.type = LightType.Point;
            gateLight.range = 6.0f;
            gateLight.intensity = 0.35f;
            gateLight.color = new Color(0.90f, 0.45f, 0.12f);

            var gatewayController = gateGo.AddComponent<GatewayController>();
            var gateVisual = gateGo.AddComponent<AwakeningGateVisual>();

            var gvSO = new SerializedObject(gateVisual);
            gvSO.FindProperty("gatewayController").objectReferenceValue = gatewayController;
            gvSO.FindProperty("leftPortalWing").objectReferenceValue = leftPortal.transform;
            gvSO.FindProperty("rightPortalWing").objectReferenceValue = rightPortal.transform;
            gvSO.FindProperty("gateAuraLight").objectReferenceValue = gateLight;
            gvSO.FindProperty("leftOpenOffset").vector3Value = new Vector3(-1.6f, 0, 0);
            gvSO.FindProperty("rightOpenOffset").vector3Value = new Vector3(1.6f, 0, 0);
            gvSO.ApplyModifiedProperties();

            var gcSO = new SerializedObject(gatewayController);
            gcSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            gcSO.FindProperty("deferOpenToDirector").boolValue = true;
            gcSO.ApplyModifiedProperties();

            resetterSO.Update();
            resetterSO.FindProperty("gatewayController").objectReferenceValue = gatewayController;
            resetterSO.ApplyModifiedProperties();

            // --- Portal-Style Stasis Awakening Pod (Docked behind player) ---
            var podRoot = new GameObject("StasisAwakeningPod");
            podRoot.transform.position = new Vector3(0, 0, -2.85f);

            var podShell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            podShell.name = "StasisPod_Frame";
            podShell.transform.SetParent(podRoot.transform);
            podShell.transform.localPosition = new Vector3(0, 1.5f, 0);
            podShell.transform.localScale = new Vector3(2.2f, 3.0f, 1.2f);
            podShell.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var podInner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            podInner.name = "StasisPod_Interior";
            podInner.transform.SetParent(podRoot.transform);
            podInner.transform.localPosition = new Vector3(0, 1.5f, 0);
            podInner.transform.localScale = new Vector3(1.8f, 2.8f, 0.9f);
            podInner.GetComponent<Renderer>().sharedMaterial = mats.CpuSubstrate;

            // Stasis Glass Sliding Doors (Retracted to sides so camera is never blocked)
            var doorLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeft.name = "StasisDoor_Left";
            doorLeft.transform.SetParent(podRoot.transform);
            doorLeft.transform.localPosition = new Vector3(-1.2f, 1.5f, 0.62f);
            doorLeft.transform.localScale = new Vector3(0.8f, 2.6f, 0.05f);
            doorLeft.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;

            var doorRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorRight.name = "StasisDoor_Right";
            doorRight.transform.SetParent(podRoot.transform);
            doorRight.transform.localPosition = new Vector3(1.2f, 1.5f, 0.62f);
            doorRight.transform.localScale = new Vector3(0.8f, 2.6f, 0.05f);
            doorRight.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;

            var podLightGo = new GameObject("StasisPod_AwakeningLight");
            podLightGo.transform.SetParent(podRoot.transform);
            podLightGo.transform.localPosition = new Vector3(0, 2.4f, 0);
            var podLight = podLightGo.AddComponent<Light>();
            podLight.type = LightType.Point;
            podLight.range = 4.0f;
            podLight.intensity = 0.5f;
            podLight.color = new Color(0.20f, 0.65f, 0.95f);

            // --- Facility Signboard ---
            var signboardGo = new GameObject("Chamber01_WallSignboard");
            signboardGo.transform.position = new Vector3(-3.45f, 2.2f, -0.5f);
            signboardGo.transform.rotation = Quaternion.Euler(0, 90f, 0);

            var signBacking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBacking.name = "SignboardBacking";
            signBacking.transform.SetParent(signboardGo.transform);
            signBacking.transform.localPosition = Vector3.zero;
            signBacking.transform.localScale = new Vector3(2.0f, 1.1f, 0.04f);
            signBacking.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(signBacking.transform, new Vector3(0, 0, 0.52f), new Vector3(1.01f, 1.01f, 0.02f), mats.GunMetal);

            var signNumGo = new GameObject("Sign_ChamberNumber");
            signNumGo.transform.SetParent(signboardGo.transform);
            signNumGo.transform.localPosition = new Vector3(0, 0.28f, -0.04f);
            var numTextMesh = signNumGo.AddComponent<TextMesh>();
            numTextMesh.text = "NEURAL";
            numTextMesh.fontSize = 22;
            numTextMesh.characterSize = 0.022f;
            numTextMesh.fontStyle = FontStyle.Bold;
            numTextMesh.alignment = TextAlignment.Center;
            numTextMesh.anchor = TextAnchor.MiddleCenter;
            numTextMesh.color = new Color(0.25f, 0.80f, 1.0f);

            var signSubGo = new GameObject("Sign_Subtitle");
            signSubGo.transform.SetParent(signboardGo.transform);
            signSubGo.transform.localPosition = new Vector3(0, -0.05f, -0.04f);
            var subTextMesh = signSubGo.AddComponent<TextMesh>();
            subTextMesh.text = "";
            subTextMesh.fontSize = 16;
            subTextMesh.characterSize = 0.018f;
            subTextMesh.alignment = TextAlignment.Center;
            subTextMesh.anchor = TextAnchor.MiddleCenter;
            subTextMesh.color = Color.white;

            var signLeds = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                var led = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                led.name = $"TargetLED_{i + 1}";
                led.transform.SetParent(signboardGo.transform);
                led.transform.localPosition = new Vector3(-0.55f + (i * 0.37f), -0.34f, -0.04f);
                led.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                led.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                var ledRend = led.GetComponent<Renderer>();
                ledRend.sharedMaterial = mats.GlowAmber;
                signLeds[i] = ledRend;
                UnityEngine.Object.DestroyImmediate(led.GetComponent<Collider>());
            }

            var signVisual = signboardGo.AddComponent<ChamberSignboardVisual>();
            var signSO = new SerializedObject(signVisual);
            signSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            signSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            signSO.FindProperty("chamberNumberText").objectReferenceValue = numTextMesh;
            signSO.FindProperty("chamberSubtitleText").objectReferenceValue = subTextMesh;
            var ledsProp = signSO.FindProperty("caseStatusLeds");
            ledsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) ledsProp.GetArrayElementAtIndex(i).objectReferenceValue = signLeds[i];
            signSO.ApplyModifiedProperties();

            // --- Animated Dotted Floor Traces ---
            var tracesGo = new GameObject("PortalDottedFloorTraces");
            var traceLines = new LineRenderer[2];

            var line1Go = new GameObject("Trace_PodToConsole");
            line1Go.transform.SetParent(tracesGo.transform);
            var lr1 = line1Go.AddComponent<LineRenderer>();
            lr1.sharedMaterial = mats.BusTraceGlow;
            lr1.positionCount = 3;
            lr1.SetPosition(0, new Vector3(0, 0.02f, -1.9f));
            lr1.SetPosition(1, new Vector3(0, 0.02f, -1.0f));
            lr1.SetPosition(2, new Vector3(0, 0.02f, -0.15f));
            lr1.startWidth = 0.012f;
            lr1.endWidth = 0.012f;
            traceLines[0] = lr1;

            var line2Go = new GameObject("Trace_ConsoleToGate");
            line2Go.transform.SetParent(tracesGo.transform);
            var lr2 = line2Go.AddComponent<LineRenderer>();
            lr2.sharedMaterial = mats.BusTraceGlow;
            lr2.positionCount = 3;
            lr2.SetPosition(0, new Vector3(0, 0.02f, 0.15f));
            lr2.SetPosition(1, new Vector3(0, 0.02f, 1.6f));
            lr2.SetPosition(2, new Vector3(0, 0.02f, 3.15f));
            lr2.startWidth = 0.014f;
            lr2.endWidth = 0.018f;
            traceLines[1] = lr2;

            var floorVisual = tracesGo.AddComponent<DottedFloorTraceVisual>();
            var fvSO = new SerializedObject(floorVisual);
            fvSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            fvSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            var flsProp = fvSO.FindProperty("floorTraceLines");
            flsProp.arraySize = 2;
            flsProp.GetArrayElementAtIndex(0).objectReferenceValue = lr1;
            flsProp.GetArrayElementAtIndex(1).objectReferenceValue = lr2;
            fvSO.ApplyModifiedProperties();

            // --- 4 Physical Data Target Receptor Pods (Between Console and Blast Gate) ---
            var dataTargetReceptors = CreateDataTargetPods(envRoot.transform, mats);
            var recsProp = chamberSO.FindProperty("targetReceptors");
            recsProp.arraySize = dataTargetReceptors.Count;
            for (int r = 0; r < dataTargetReceptors.Count; r++)
            {
                recsProp.GetArrayElementAtIndex(r).objectReferenceValue = dataTargetReceptors[r];
            }
            chamberSO.ApplyModifiedProperties();

            // --- Chamber Onboarding Controller ---
            var onboardingController = controllersGo.AddComponent<ChamberOnboardingController>();
            var obSO = new SerializedObject(onboardingController);
            obSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            obSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            obSO.FindProperty("gatewayController").objectReferenceValue = gatewayController;
            obSO.FindProperty("awakeningDelay").floatValue = 1.2f;
            obSO.FindProperty("stasisDoorLeft").objectReferenceValue = doorLeft.transform;
            obSO.FindProperty("stasisDoorRight").objectReferenceValue = doorRight.transform;
            obSO.FindProperty("stasisPodLight").objectReferenceValue = podLight;

            var obLightsProp = obSO.FindProperty("interactableLights");
            obLightsProp.arraySize = 7;
            obLightsProp.GetArrayElementAtIndex(0).objectReferenceValue = cable1Light;
            obLightsProp.GetArrayElementAtIndex(1).objectReferenceValue = cable2Light;
            obLightsProp.GetArrayElementAtIndex(2).objectReferenceValue = w1Light;
            obLightsProp.GetArrayElementAtIndex(3).objectReferenceValue = w2Light;
            obLightsProp.GetArrayElementAtIndex(4).objectReferenceValue = biasLight;
            obLightsProp.GetArrayElementAtIndex(5).objectReferenceValue = crystalLight;
            obLightsProp.GetArrayElementAtIndex(6).objectReferenceValue = leverLight;
            obSO.ApplyModifiedProperties();

            // --- In-World Holographic VR Subtitle & Objective Banner ---
            var subtitleHoloGo = new GameObject("VR_Holographic_Subtitle_Banner");
            subtitleHoloGo.transform.position = new Vector3(0, 2.62f, 2.35f);
            subtitleHoloGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var subText = subtitleHoloGo.AddComponent<TextMesh>();
            subText.text = "";
            subText.fontSize = 24;
            subText.characterSize = 0.028f;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAlignment.Center;
            subText.anchor = TextAnchor.MiddleCenter;
            subText.color = new Color(0.2f, 0.95f, 1.0f, 1.0f);

            // AuraSubtitles owns the banner. FacilityAIVoiceAnnouncer is not added.

            // --- XRI Starter Assets rig, interaction services, bridges and world-space HUD (ADR-007) ---
            InstantiateXrRig();
            CreateXrInteractionServices();
            var voyage = controllersGo.AddComponent<VoyageDirector>();
            var voyageSO = new SerializedObject(voyage);
            voyageSO.FindProperty("chamber").objectReferenceValue = chamberController;
            voyageSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            voyageSO.ApplyModifiedProperties();
            controllersGo.AddComponent<FailureHintDirector>();
            var drift = controllersGo.AddComponent<AsteroidDrift>();
            var driftSO = new SerializedObject(drift);
            driftSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            var field = GameObject.Find("FloatingAsteroidField");
            if (field != null) driftSO.FindProperty("asteroidField").objectReferenceValue = field.transform;
            driftSO.ApplyModifiedProperties();
            controllersGo.AddComponent<SkyPhotoCapture>();
            var finale = controllersGo.AddComponent<FinaleSequence>();
            var finaleSO = new SerializedObject(finale);
            finaleSO.FindProperty("voyage").objectReferenceValue = voyage;
            var earth = GameObject.Find("Celestial_FaintEarth");
            if (earth != null) finaleSO.FindProperty("earth").objectReferenceValue = earth.transform;
            var cinematicShip = GameObject.Find("CinematicShip");
            if (cinematicShip != null) finaleSO.FindProperty("ship").objectReferenceValue = cinematicShip.transform;
            var starfield = GameObject.Find("Starfield_Constellation");
            if (starfield != null) finaleSO.FindProperty("starfield").objectReferenceValue = starfield.transform;
            var giant = GameObject.Find("Celestial_GasGiantPlanet");
            if (giant != null) finaleSO.FindProperty("gasGiant").objectReferenceValue = giant.transform;
            if (field != null) finaleSO.FindProperty("asteroidField").objectReferenceValue = field.transform;
            var engineLights = new Light[2];
            if (cinematicShip != null)
            {
                var leftEngine = cinematicShip.transform.Find("Engine_L");
                var rightEngine = cinematicShip.transform.Find("Engine_R");
                if (leftEngine != null) engineLights[0] = leftEngine.GetComponent<Light>();
                if (rightEngine != null) engineLights[1] = rightEngine.GetComponent<Light>();
            }
            var enginesProp = finaleSO.FindProperty("engines");
            enginesProp.arraySize = 2;
            enginesProp.GetArrayElementAtIndex(0).objectReferenceValue = engineLights[0];
            enginesProp.GetArrayElementAtIndex(1).objectReferenceValue = engineLights[1];
            var creditsGo = new GameObject("DepartureCredits");
            creditsGo.transform.position = new Vector3(0f, 1.72f, 1.55f);
            creditsGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var creditsMesh = creditsGo.AddComponent<TextMesh>();
            creditsMesh.text = string.Empty;
            creditsMesh.fontSize = 28;
            creditsMesh.characterSize = 0.018f;
            creditsMesh.fontStyle = FontStyle.Bold;
            creditsMesh.alignment = TextAlignment.Center;
            creditsMesh.anchor = TextAnchor.MiddleCenter;
            creditsMesh.color = new Color(0.75f, 0.92f, 1f);
            finaleSO.FindProperty("creditsMesh").objectReferenceValue = creditsMesh;
            finaleSO.ApplyModifiedProperties();
            var aura = subtitleHoloGo.AddComponent<AuraSubtitles>();
            var auraSO = new SerializedObject(aura);
            auraSO.FindProperty("onboarding").objectReferenceValue = onboardingController;
            auraSO.FindProperty("legacyLine").objectReferenceValue = subText;
            var voice = subtitleHoloGo.AddComponent<AudioSource>();
            voice.spatialBlend = 1f;
            voice.playOnAwake = false;
            auraSO.ApplyModifiedProperties();

            WirePointDefense(controllersGo, chamberController, gatewayController, neuralState, onboardingController, audioHookManager, subText, voice);

            AttachXriBridges();
            var live = controllersGo.AddComponent<LiveEvaluationRelay>();
            var liveSO = new SerializedObject(live);
            liveSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            liveSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            liveSO.FindProperty("hintIdleSeconds").floatValue = 45f;
            liveSO.FindProperty("engageCaseSeconds").floatValue = 6f;
            liveSO.ApplyModifiedProperties();
            CreateWorldSpaceHud(chamberController, neuralState, onboardingController);
            var feedbackGo = new GameObject("ChamberFeedback");
            var feedback = feedbackGo.AddComponent<ChamberFeedbackVisual>();
            var feedbackSO = new SerializedObject(feedback);
            feedbackSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            feedbackSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            feedbackSO.FindProperty("live").objectReferenceValue = live;
            feedbackSO.ApplyModifiedProperties();
            BakeNonKeyLights();
            Chamber01Concept.Install(neuralState, chamberController);
            Chamber01TextRules.Apply();

            CreateChamber01CaptureMarkers();
            Chamber01TextRules.WriteAudit();
            Chamber01StateTest.Write();
            Chamber01CurriculumAudit.Write();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register in Build Settings
            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Level01SceneBuilder] Level 1 generated and saved successfully to '{ScenePath}'.");

            // Automatically capture bridge view
            CaptureBridgeView();
        }

        private const string PathXrRigPrefab = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        private static void CreateOrbitRing(string name, Transform parent, float radius, Quaternion rotation, Material material)
        {
            const int segments = 96;
            var ringGo = new GameObject(name);
            ringGo.transform.SetParent(parent, false);
            ringGo.transform.localRotation = rotation;
            var line = ringGo.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = 0.006f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static GameObject InstantiateXrRig()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathXrRigPrefab);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"XRI Starter Assets rig prefab not found at '{PathXrRigPrefab}'. Import the Starter Assets sample from Package Manager > XR Interaction Toolkit > Samples.");
            }

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.transform.position = new Vector3(0f, 0f, -0.90f);
            rig.AddComponent<DesktopWalk>();

            var origin = rig.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
            }

            var rigCamera = rig.GetComponentInChildren<Camera>();
            if (rigCamera != null)
            {
                rigCamera.gameObject.tag = "MainCamera";
                rigCamera.clearFlags = CameraClearFlags.SolidColor;
                rigCamera.backgroundColor = new Color(0.012f, 0.016f, 0.030f);
                rigCamera.nearClipPlane = 0.03f;
                rigCamera.farClipPlane = 500f;
                if (rigCamera.GetComponent<AudioListener>() == null)
                {
                    rigCamera.gameObject.AddComponent<AudioListener>();
                }
            }

            return rig;
        }

        private static void CreateXrInteractionServices()
        {
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            }

            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
            }
        }

        private static void AttachXriBridges()
        {
            int count = 0;
            count += BridgeAll<KineticWeightSliderInteractor>();
            count += BridgeAll<WeightRegulatorInteractor>();
            count += BridgeAll<BiasDialInteractor>();
            count += BridgeAll<ClockPulseLeverInteractor>();
            count += BridgeAll<ActivationSocketInteractor>();
            count += BridgeAll<InputTerminalInteractor>();
            count += BridgeAll<CableInteractable>();
            count += BridgeAll<DataTargetReceptor>();
            count += BridgeAll<PhotoRequest>();
            count += BridgeAll<VoyageButton>();
            Debug.Log($"[Level01SceneBuilder] Attached {count} XRI interactable bridge(s).");
        }

        private static int BridgeAll<T>() where T : Component
        {
            int added = 0;
            foreach (var component in UnityEngine.Object.FindObjectsByType<T>())
            {
                GameObject go = component.gameObject;
                if (go.GetComponent<XRInteractableBridge>() != null)
                {
                    continue;
                }

                EnsureComfortCollider(go);
                go.AddComponent<XRInteractableBridge>();
                added++;
            }

            return added;
        }

        // Quest ray targeting needs at least ~14 cm of solid collider; several console controls are 2-8 cm.
        private static void EnsureComfortCollider(GameObject go)
        {
            const float minExtent = 0.14f;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 world = new Vector3(
                Mathf.Max(bounds.size.x, minExtent),
                Mathf.Max(bounds.size.y, minExtent),
                Mathf.Max(bounds.size.z, minExtent));

            Vector3 scale = go.transform.lossyScale;
            if (Mathf.Abs(scale.x) < 1e-4f || Mathf.Abs(scale.y) < 1e-4f || Mathf.Abs(scale.z) < 1e-4f)
            {
                return;
            }

            var pad = go.AddComponent<BoxCollider>();
            pad.center = go.transform.InverseTransformPoint(bounds.center);
            pad.size = new Vector3(world.x / Mathf.Abs(scale.x), world.y / Mathf.Abs(scale.y), world.z / Mathf.Abs(scale.z));
        }

        private static void CreateWorldSpaceHud(ChamberController chamberController, NeuralState neuralState, ChamberOnboardingController onboardingController)
        {
            var canvasGo = new GameObject("WorldSpaceHud");
            canvasGo.transform.position = new Vector3(-1.05f, 1.40f, -0.10f);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>().dynamicPixelsPerUnit = 6f;
            canvasGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 420f);

            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(canvasGo.transform, false);
            StretchRect(background.GetComponent<RectTransform>());
            var backgroundImage = background.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0.02f, 0.05f, 0.09f, 0.88f);
            backgroundImage.raycastTarget = false;

            Color label = new Color(0.90f, 0.91f, 0.88f, 1f);
            var objective = CreateHudText(canvasGo.transform, "Objective", new Vector2(0.03f, 0.62f), new Vector2(0.97f, 0.97f), 36f, label, TMPro.TextAlignmentOptions.TopLeft);
            objective.text = "Plug in the ROCK wire.";
            var readout = CreateHudText(canvasGo.transform, "Readout", new Vector2(0.03f, 0.48f), new Vector2(0.97f, 0.62f), 28f, label, TMPro.TextAlignmentOptions.Left);
            readout.text = string.Empty;
            readout.gameObject.SetActive(false);
            var result = CreateHudText(canvasGo.transform, "Result", new Vector2(0.03f, 0.22f), new Vector2(0.97f, 0.48f), 28f, label, TMPro.TextAlignmentOptions.TopLeft);
            result.text = "Not yet.";

            var buttonGo = new GameObject("RunTestButton", typeof(RectTransform));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.02f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.02f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(360f, 80f);
            var buttonImage = buttonGo.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.25f, 0.28f, 0.32f, 0.95f);
            var button = buttonGo.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = buttonImage;
            CreateHudText(buttonGo.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 1f), 30f, label, TMPro.TextAlignmentOptions.Center).text = "SELF-TEST";

            var mathGo = new GameObject("MathButton", typeof(RectTransform));
            mathGo.transform.SetParent(canvasGo.transform, false);
            var mathRect = mathGo.GetComponent<RectTransform>();
            mathRect.anchorMin = new Vector2(0.08f, 0.02f);
            mathRect.anchorMax = new Vector2(0.08f, 0.02f);
            mathRect.pivot = new Vector2(0f, 0f);
            mathRect.sizeDelta = new Vector2(280f, 80f);
            var mathImage = mathGo.AddComponent<UnityEngine.UI.Image>();
            mathImage.color = new Color(0.25f, 0.28f, 0.32f, 0.95f);
            var mathButton = mathGo.AddComponent<UnityEngine.UI.Button>();
            mathButton.targetGraphic = mathImage;
            CreateHudText(mathGo.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 1f), 28f, label, TMPro.TextAlignmentOptions.Center).text = "Show the math";
            var relay = buttonGo.AddComponent<UiForwardPassRelay>();
            var relaySO = new SerializedObject(relay);
            relaySO.FindProperty("chamberController").objectReferenceValue = chamberController;
            relaySO.ApplyModifiedProperties();
            var testButton = canvasGo.AddComponent<ChamberTestButton>();
            var testSO = new SerializedObject(testButton);
            testSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            testSO.FindProperty("live").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<LiveEvaluationRelay>();
            testSO.ApplyModifiedProperties();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick, new UnityEngine.Events.UnityAction(testButton.Press));

            var hud = canvasGo.AddComponent<WorldSpaceHud>();
            var hudSO = new SerializedObject(hud);
            hudSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            hudSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            hudSO.FindProperty("onboardingController").objectReferenceValue = onboardingController;
            hudSO.FindProperty("objectiveText").objectReferenceValue = objective;
            hudSO.FindProperty("readoutText").objectReferenceValue = readout;
            hudSO.FindProperty("resultText").objectReferenceValue = result;
            hudSO.FindProperty("showMath").boolValue = false;
            hudSO.ApplyModifiedProperties();

            var mathToggle = mathGo.AddComponent<ShowMathToggle>();
            var mathSO = new SerializedObject(mathToggle);
            mathSO.FindProperty("hud").objectReferenceValue = hud;
            mathSO.ApplyModifiedProperties();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                mathButton.onClick, new UnityEngine.Events.UnityAction(mathToggle.Toggle));
        }

        private static TMPro.TextMeshProUGUI CreateHudText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, Color color, TMPro.TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TMPro.TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }


        [MenuItem("Convergence/Capture Bridge View")]
        public static void CaptureBridgeView()
        {
            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[Level01SceneBuilder] No camera found to capture bridge view.");
                return;
            }

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogWarning("[Level01SceneBuilder] Graphics device is Null (running in -nographics mode). Skipping offscreen render capture.");
                return;
            }

            int width = 1920;
            int height = 1080;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevRt;

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string outArtifact = "C:/Users/Panda/.gemini/antigravity-ide/brain/8c630c5d-6533-41e1-ab1c-a9c9c0ede0d4/modern_bridge_ingame_view.png";
            try
            {
                File.WriteAllBytes(outArtifact, bytes);
                Debug.Log($"[Level01SceneBuilder] Captured bridge view to: {outArtifact} ({bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Level01SceneBuilder] Failed to save screenshot to artifact dir: {ex.Message}");
            }
            AssetDatabase.Refresh();

            Debug.Log($"[Level01SceneBuilder] Level 1 generated and saved successfully to '{ScenePath}'.");
        }

        private static void CreateSpaceshipBridgeHull(Transform parent, LevelMaterials mats)
        {
            var hullRoot = new GameObject("SpaceshipBridgeHull");
            hullRoot.transform.SetParent(parent, false);

            // 1. Solid Base Deck (Command Bridge Deck Plates with Colliders)
            var cmdDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cmdDeck.name = "Deck_CommandBridge";
            cmdDeck.transform.SetParent(hullRoot.transform, false);
            cmdDeck.transform.position = new Vector3(0, -0.25f, 0.0f);
            cmdDeck.transform.localScale = new Vector3(7.4f, 0.5f, 7.8f);
            cmdDeck.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;

            // Modular Kenney Sci-Fi Deck Tiling (floor-panel.fbx / floor-detail.fbx)
            var deckTilesRoot = new GameObject("Kenney_DeckTiles");
            deckTilesRoot.transform.SetParent(hullRoot.transform, false);

            for (int x = -2; x <= 2; x += 2)
            {
                for (int z = -2; z <= 2; z += 2)
                {
                    var tile = CreateModelInstance(PathKenneyFloorPanel, $"DeckTile_{x}_{z}", deckTilesRoot.transform);
                    if (tile != null)
                    {
                        tile.transform.position = new Vector3(x, 0.01f, z);
                        tile.transform.localScale = new Vector3(2.0f, 1.0f, 2.0f);
                        ApplyMaterialRecursively(tile, mats.KenneyStation);
                    }
                }
            }

            // High-Visibility Hazard Striping
            CreateHazardStripeSegment(hullRoot.transform, new Vector3(0, 0.015f, 0.45f), new Vector3(5.2f, 0.01f, 0.25f), mats);
            CreateHazardStripeSegment(hullRoot.transform, new Vector3(-3.15f, 0.015f, 0.0f), new Vector3(0.25f, 0.01f, 6.0f), mats);
            CreateHazardStripeSegment(hullRoot.transform, new Vector3(3.15f, 0.015f, 0.0f), new Vector3(0.25f, 0.01f, 6.0f), mats);

            // Recessed Cyan Runway Guide Lights
            CreateDeckRunwayLights(hullRoot.transform, mats.GlowCyan);

            // 2. Modular Sci-Fi Bulkheads (Molded White Panels framing the cockpit)
            var bulkheadsRoot = new GameObject("Kenney_ModularBulkheads");
            bulkheadsRoot.transform.SetParent(hullRoot.transform, false);

            // Port Bulkhead Modules (x = -3.55m, facing inward rot = 90 deg Y)
            float[] portZ = new float[] { -2.6f, -0.9f, 0.8f, 2.5f };
            for (int i = 0; i < portZ.Length; i++)
            {
                string mPath = (i == 1 || i == 2) ? PathKenneyWallWindowFrame : (i % 2 == 0 ? PathKenneyWall : PathKenneyWallDetail);
                var wallMod = CreateModelInstance(mPath, $"Bulkhead_Port_{i:D2}", bulkheadsRoot.transform);
                if (wallMod != null)
                {
                    wallMod.transform.position = new Vector3(-3.55f, 0f, portZ[i]);
                    wallMod.transform.localScale = new Vector3(1.8f, 3.6f, 1.0f);
                    wallMod.transform.rotation = Quaternion.Euler(0, 90f, 0);
                    ApplyMaterialRecursively(wallMod, mats.SpaceshipHull);
                }

                if (i == 1 || i == 2)
                {
                    var pGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pGlass.name = $"WindowGlass_Port_{i:D2}";
                    pGlass.transform.SetParent(bulkheadsRoot.transform, false);
                    pGlass.transform.position = new Vector3(-3.55f, 1.8f, portZ[i]);
                    pGlass.transform.localScale = new Vector3(0.04f, 2.2f, 1.7f);
                    pGlass.GetComponent<Renderer>().sharedMaterial = mats.ViewportGlass;
                    UnityEngine.Object.DestroyImmediate(pGlass.GetComponent<Collider>());
                }

                if (i < portZ.Length - 1)
                {
                    var pillar = CreateModelInstance(PathKenneyWallPillar, $"Pillar_Port_{i:D2}", bulkheadsRoot.transform);
                    if (pillar != null)
                    {
                        pillar.transform.position = new Vector3(-3.50f, 0f, (portZ[i] + portZ[i + 1]) * 0.5f);
                        pillar.transform.localScale = new Vector3(1.2f, 3.6f, 1.2f);
                        pillar.transform.rotation = Quaternion.Euler(0, 90f, 0);
                        ApplyMaterialRecursively(pillar, mats.SpaceshipHull);
                    }
                }
            }

            // Starboard Bulkhead Modules (x = 3.55m, facing inward rot = -90 deg Y)
            for (int i = 0; i < portZ.Length; i++)
            {
                string mPath = (i == 1 || i == 2) ? PathKenneyWallWindowFrame : (i % 2 == 0 ? PathKenneyWall : PathKenneyWallDetail);
                var wallMod = CreateModelInstance(mPath, $"Bulkhead_Starboard_{i:D2}", bulkheadsRoot.transform);
                if (wallMod != null)
                {
                    wallMod.transform.position = new Vector3(3.55f, 0f, portZ[i]);
                    wallMod.transform.localScale = new Vector3(1.8f, 3.6f, 1.0f);
                    wallMod.transform.rotation = Quaternion.Euler(0, -90f, 0);
                    ApplyMaterialRecursively(wallMod, mats.SpaceshipHull);
                }

                if (i == 1 || i == 2)
                {
                    var sGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sGlass.name = $"WindowGlass_Starboard_{i:D2}";
                    sGlass.transform.SetParent(bulkheadsRoot.transform, false);
                    sGlass.transform.position = new Vector3(3.55f, 1.8f, portZ[i]);
                    sGlass.transform.localScale = new Vector3(0.04f, 2.2f, 1.7f);
                    sGlass.GetComponent<Renderer>().sharedMaterial = mats.ViewportGlass;
                    UnityEngine.Object.DestroyImmediate(sGlass.GetComponent<Collider>());
                }

                if (i < portZ.Length - 1)
                {
                    var pillar = CreateModelInstance(PathKenneyWallPillar, $"Pillar_Starboard_{i:D2}", bulkheadsRoot.transform);
                    if (pillar != null)
                    {
                        pillar.transform.position = new Vector3(3.50f, 0f, (portZ[i] + portZ[i + 1]) * 0.5f);
                        pillar.transform.localScale = new Vector3(1.2f, 3.6f, 1.2f);
                        pillar.transform.rotation = Quaternion.Euler(0, -90f, 0);
                        ApplyMaterialRecursively(pillar, mats.SpaceshipHull);
                    }
                }
            }

            // Aft Bulkhead Wall (Behind Player: z = -3.75m) with Heavy Blast Door Airlock
            var aftDoor = CreateModelInstance(PathKenneyWallDoorWide, "Bulkhead_Aft_BlastDoor", bulkheadsRoot.transform);
            if (aftDoor != null)
            {
                aftDoor.transform.position = new Vector3(0, 0f, -3.75f);
                aftDoor.transform.localScale = new Vector3(2.6f, 3.6f, 1.0f);
                aftDoor.transform.rotation = Quaternion.identity;
                ApplyMaterialRecursively(aftDoor, mats.SpaceshipHull);
            }

            float[] aftX = new float[] { -2.2f, 2.2f };
            for (int a = 0; a < aftX.Length; a++)
            {
                var aftWall = CreateModelInstance(PathKenneyWall, $"Bulkhead_Aft_Panel_{a + 1}", bulkheadsRoot.transform);
                if (aftWall != null)
                {
                    aftWall.transform.position = new Vector3(aftX[a], 0f, -3.75f);
                    aftWall.transform.localScale = new Vector3(1.8f, 3.6f, 1.0f);
                    aftWall.transform.rotation = Quaternion.identity;
                    ApplyMaterialRecursively(aftWall, mats.SpaceshipHull);
                }
            }

            // Backing collision walls
            var collL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collL.name = "BackingCollider_Port";
            collL.transform.SetParent(hullRoot.transform, false);
            collL.transform.position = new Vector3(-3.75f, 1.8f, 0f);
            collL.transform.localScale = new Vector3(0.2f, 4.0f, 8.0f);
            collL.GetComponent<Renderer>().enabled = false;

            var collR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collR.name = "BackingCollider_Starboard";
            collR.transform.SetParent(hullRoot.transform, false);
            collR.transform.position = new Vector3(3.75f, 1.8f, 0f);
            collR.transform.localScale = new Vector3(0.2f, 4.0f, 8.0f);
            collR.GetComponent<Renderer>().enabled = false;

            var collAftL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collAftL.name = "BackingCollider_AftLeft";
            collAftL.transform.SetParent(hullRoot.transform, false);
            collAftL.transform.position = new Vector3(-2.35f, 1.8f, -3.85f);
            collAftL.transform.localScale = new Vector3(3.3f, 4.0f, 0.2f);
            collAftL.GetComponent<Renderer>().enabled = false;

            var collAftR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collAftR.name = "BackingCollider_AftRight";
            collAftR.transform.SetParent(hullRoot.transform, false);
            collAftR.transform.position = new Vector3(2.35f, 1.8f, -3.85f);
            collAftR.transform.localScale = new Vector3(3.3f, 4.0f, 0.2f);
            collAftR.GetComponent<Renderer>().enabled = false;

            // Horizontal Ceiling Span in Pristine White Composite
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Bulkhead_CeilingSpan";
            ceiling.transform.SetParent(hullRoot.transform, false);
            ceiling.transform.position = new Vector3(0, 3.65f, 0.0f);
            ceiling.transform.localScale = new Vector3(7.6f, 0.3f, 7.8f);
            ceiling.GetComponent<Renderer>().sharedMaterial = mats.CeilingValue;

            // Recessed Ceiling Daylight Panels & Point Lights
            float[] ceilingZ = new float[] { -2.0f, -0.6f, 0.8f, 2.2f };
            for (int c = 0; c < ceilingZ.Length; c++)
            {
                var lightPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lightPanel.name = $"CeilingLightPanel_{c + 1}";
                lightPanel.transform.SetParent(ceiling.transform, false);
                lightPanel.transform.localPosition = new Vector3(0, -0.51f, (ceilingZ[c] / 7.8f));
                lightPanel.transform.localScale = new Vector3(0.35f, 0.02f, 0.12f);
                lightPanel.GetComponent<Renderer>().sharedMaterial = mats.CeilingLight;
                UnityEngine.Object.DestroyImmediate(lightPanel.GetComponent<Collider>());

                var cLightGo = new GameObject($"CeilingDaylight_{c + 1}");
                cLightGo.transform.SetParent(hullRoot.transform, false);
                cLightGo.transform.position = new Vector3(0, 3.42f, ceilingZ[c]);
                var cl = cLightGo.AddComponent<Light>();
                cl.type = LightType.Point;
                cl.range = 7.5f;
                cl.intensity = 1.35f;
                cl.color = new Color(0.95f, 0.98f, 1.0f);
            }

            // Overhead Longitudinal LED Strip Runners
            for (int s = -1; s <= 1; s += 2)
            {
                var ledStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledStrip.name = $"OverheadLEDRunner_{(s < 0 ? "Port" : "Starboard")}";
                ledStrip.transform.SetParent(hullRoot.transform, false);
                ledStrip.transform.position = new Vector3(s * 1.6f, 3.54f, 0.0f);
                ledStrip.transform.localScale = new Vector3(0.08f, 0.02f, 7.0f);
                ledStrip.GetComponent<Renderer>().sharedMaterial = mats.CeilingLight;
                UnityEngine.Object.DestroyImmediate(ledStrip.GetComponent<Collider>());
            }

            // 3. Structural Arch Ribs along the vessel corridor
            float[] ribZPositions = new float[] { -2.0f, 0.2f, 2.4f };
            for (int r = 0; r < ribZPositions.Length; r++)
            {
                CreateBulkheadRib(hullRoot.transform, ribZPositions[r], r + 1, mats);
            }

            // 4. Overhead Industrial Conduit Pipes
            CreateCeilingConduitSystem(hullRoot.transform, mats);

            // 5. Port & Starboard Auxiliary Telemetry Bridge Consoles
            CreateAuxiliaryShipConsoles(hullRoot.transform, mats);
        }

        private static void CreateBulkheadRib(Transform parent, float z, int index, LevelMaterials mats)
        {
            var ribGo = new GameObject($"BulkheadRib_{index:D2}");
            ribGo.transform.SetParent(parent, false);
            ribGo.transform.position = new Vector3(0, 0, z);

            // Left & Right Molded Sci-Fi Structural Columns (structure.fbx)
            var lStruct = CreateModelInstance(PathKenneyStructure, "Structure_Port", ribGo.transform);
            if (lStruct != null)
            {
                lStruct.transform.localPosition = new Vector3(-3.45f, 1.8f, 0);
                lStruct.transform.localScale = new Vector3(1.0f, 3.6f, 1.0f);
                ApplyMaterialRecursively(lStruct, mats.SpaceshipHull);
            }

            var rStruct = CreateModelInstance(PathKenneyStructure, "Structure_Starboard", ribGo.transform);
            if (rStruct != null)
            {
                rStruct.transform.localPosition = new Vector3(3.45f, 1.8f, 0);
                rStruct.transform.localScale = new Vector3(1.0f, 3.6f, 1.0f);
                ApplyMaterialRecursively(rStruct, mats.SpaceshipHull);
            }

            // Overhead Cross-Girder Beam
            var crossBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossBeam.name = "CrossGirder_Overhead";
            crossBeam.transform.SetParent(ribGo.transform, false);
            crossBeam.transform.localPosition = new Vector3(0, 3.52f, 0);
            crossBeam.transform.localScale = new Vector3(7.1f, 0.28f, 0.40f);
            crossBeam.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;

            // Recessed Daylight LED Light Strip
            var lightRunner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightRunner.name = "LED_LightRunner";
            lightRunner.transform.SetParent(ribGo.transform, false);
            lightRunner.transform.localPosition = new Vector3(0, 3.38f, 0);
            lightRunner.transform.localScale = new Vector3(6.5f, 0.04f, 0.12f);
            lightRunner.GetComponent<Renderer>().sharedMaterial = mats.CeilingLight;
            UnityEngine.Object.DestroyImmediate(lightRunner.GetComponent<Collider>());

            // Downlight Point Light (Clean daylight illumination across the deck)
            var downlightGo = new GameObject("RibDownlight");
            downlightGo.transform.SetParent(ribGo.transform, false);
            downlightGo.transform.localPosition = new Vector3(0, 3.30f, 0);
            var downlight = downlightGo.AddComponent<Light>();
            downlight.type = LightType.Point;
            downlight.range = 8.0f;
            downlight.intensity = 1.2f;
            downlight.color = new Color(0.95f, 0.98f, 1.0f);

            // Alert Strobe Beacon (Alternating Red/Amber on outer edge)
            var strobeLeftGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            strobeLeftGo.name = "AlertStrobe_Port";
            strobeLeftGo.transform.SetParent(ribGo.transform, false);
            strobeLeftGo.transform.localPosition = new Vector3(-3.35f, 2.6f, -0.22f);
            strobeLeftGo.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            var strobeLeftRend = strobeLeftGo.GetComponent<Renderer>();
            strobeLeftRend.sharedMaterial = mats.WarningAmber;
            UnityEngine.Object.DestroyImmediate(strobeLeftGo.GetComponent<Collider>());

            var strobeRightGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            strobeRightGo.name = "AlertStrobe_Starboard";
            strobeRightGo.transform.SetParent(ribGo.transform, false);
            strobeRightGo.transform.localPosition = new Vector3(3.35f, 2.6f, -0.22f);
            strobeRightGo.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            var strobeRightRend = strobeRightGo.GetComponent<Renderer>();
            strobeRightRend.sharedMaterial = mats.WarningAmber;
            UnityEngine.Object.DestroyImmediate(strobeRightGo.GetComponent<Collider>());
        }

        private static void CreateCeilingConduitSystem(Transform parent, LevelMaterials mats)
        {
            var conduitRoot = new GameObject("OverheadCeilingConduits");
            conduitRoot.transform.SetParent(parent, false);

            float[] pipeX = new float[] { -1.8f, -0.6f, 0.6f, 1.8f };
            for (int p = 0; p < pipeX.Length; p++)
            {
                var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = $"ConduitPipe_{p + 1}";
                pipe.transform.SetParent(conduitRoot.transform, false);
                pipe.transform.position = new Vector3(pipeX[p], 3.52f, 0.0f);
                pipe.transform.localScale = new Vector3(0.06f, 3.5f, 0.06f);
                pipe.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                pipe.GetComponent<Renderer>().sharedMaterial = mats.ConduitPipe;
                UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());

                // Glowing conduit couplings along the length
                for (int c = 0; c < 4; c++)
                {
                    float coupZ = -2.25f + (c * 1.5f);
                    var coupling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    coupling.name = $"Coupling_{c + 1}";
                    coupling.transform.SetParent(pipe.transform, false);
                    coupling.transform.localPosition = new Vector3(0, (coupZ / 3.5f), 0);
                    coupling.transform.localScale = new Vector3(1.35f, 0.012f, 1.35f);
                    coupling.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
                    UnityEngine.Object.DestroyImmediate(coupling.GetComponent<Collider>());
                }
            }
        }


        private static void CreateAuxiliaryShipConsoles(Transform parent, LevelMaterials mats)
        {
            var auxRoot = new GameObject("AuxiliaryBridgeStations");
            auxRoot.transform.SetParent(parent, false);

            // Port Side Station 1: Warp Diagnostics (x = -3.0m, z = -1.0m)
            var portStation = CreateModelInstance(PathKenneyTableDisplay, "AuxStation_Port_Warp", auxRoot.transform);
            if (portStation != null)
            {
                portStation.transform.position = new Vector3(-3.0f, 0.0f, -1.0f);
                portStation.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                portStation.transform.rotation = Quaternion.Euler(0, 90f, 0);
                ApplyMaterialRecursively(portStation, mats.KenneyStation);
            }

            var portScreen = CreateModelInstance(PathKenneyComputerWide, "TerminalScreen_Port", auxRoot.transform);
            if (portScreen != null)
            {
                portScreen.transform.position = new Vector3(-3.0f, 0.85f, -1.0f);
                portScreen.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                portScreen.transform.rotation = Quaternion.Euler(0, 90f, 0);
                ApplyMaterialRecursively(portScreen, mats.KenneyStation);
            }

            var port1TextGo = new GameObject("ScreenText_Port");
            port1TextGo.transform.SetParent(auxRoot.transform, false);
            port1TextGo.transform.position = new Vector3(-2.95f, 0.95f, -1.0f);
            port1TextGo.transform.rotation = Quaternion.Euler(0, 90f, 0);
            var p1Txt = port1TextGo.AddComponent<TextMesh>();
            p1Txt.text = "";
            p1Txt.fontSize = 20;
            p1Txt.characterSize = 0.015f;
            p1Txt.alignment = TextAlignment.Center;
            p1Txt.anchor = TextAnchor.MiddleCenter;
            p1Txt.color = new Color(0.95f, 0.35f, 0.35f);

            // Starboard Side Station 2: Hyperspace Coils (x = 3.0m, z = -1.0m)
            var stbStation = CreateModelInstance(PathKenneyTableDisplay, "AuxStation_Starboard_Hyperspace", auxRoot.transform);
            if (stbStation != null)
            {
                stbStation.transform.position = new Vector3(3.0f, 0.0f, -1.0f);
                stbStation.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                stbStation.transform.rotation = Quaternion.Euler(0, -90f, 0);
                ApplyMaterialRecursively(stbStation, mats.KenneyStation);
            }

            var stbScreen = CreateModelInstance(PathKenneyComputerWide, "TerminalScreen_Starboard", auxRoot.transform);
            if (stbScreen != null)
            {
                stbScreen.transform.position = new Vector3(3.0f, 0.85f, -1.0f);
                stbScreen.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                stbScreen.transform.rotation = Quaternion.Euler(0, -90f, 0);
                ApplyMaterialRecursively(stbScreen, mats.KenneyStation);
            }

            var stb1TextGo = new GameObject("ScreenText_Starboard");
            stb1TextGo.transform.SetParent(auxRoot.transform, false);
            stb1TextGo.transform.position = new Vector3(2.95f, 0.95f, -1.0f);
            stb1TextGo.transform.rotation = Quaternion.Euler(0, -90f, 0);
            var s1Txt = stb1TextGo.AddComponent<TextMesh>();
            s1Txt.text = "";
            s1Txt.fontSize = 20;
            s1Txt.characterSize = 0.015f;
            s1Txt.alignment = TextAlignment.Center;
            s1Txt.anchor = TextAnchor.MiddleCenter;
            s1Txt.color = new Color(0.98f, 0.65f, 0.10f);
        }

        private static void CreateForwardObservationViewportAndSpaceVista(Transform parent, LevelMaterials mats)
        {
            var vistaRoot = new GameObject("ForwardObservationViewportAndDeepSpaceVista");
            vistaRoot.transform.SetParent(parent, false);

            // 1. Panoramic Forward Canopy Window Frame (Positioned at z = 3.50m)
            var frameGo = new GameObject("Viewport_StructuralCanopyFrame");
            frameGo.transform.SetParent(vistaRoot.transform, false);
            frameGo.transform.position = new Vector3(0, 0, 3.50f);

            // Molded White Upper Header & Lower Sill
            var header = GameObject.CreatePrimitive(PrimitiveType.Cube);
            header.name = "ViewportHeader_Upper";
            header.transform.SetParent(frameGo.transform, false);
            header.transform.localPosition = new Vector3(0, 3.45f, 0);
            header.transform.localScale = new Vector3(7.2f, 0.45f, 0.40f);
            header.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;

            var sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "ViewportSill_Lower";
            sill.transform.SetParent(frameGo.transform, false);
            sill.transform.localPosition = new Vector3(0, 0.55f, 0);
            sill.transform.localScale = new Vector3(7.2f, 0.55f, 0.40f);
            sill.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;

            // Vertical Canopy Mullions framing the panoramic glass
            float[] mullionX = new float[] { -3.4f, 3.4f };
            for (int m = 0; m < mullionX.Length; m++)
            {
                var mullion = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mullion.name = $"CanopyMullion_{m + 1}";
                mullion.transform.SetParent(frameGo.transform, false);
                mullion.transform.localPosition = new Vector3(mullionX[m], 2.0f, 0);
                mullion.transform.localScale = new Vector3(0.22f, 2.55f, 0.35f);
                mullion.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;
            }

            // Crystal-Clear Reinforced Viewport Glass Pane framing the space vista
            var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "PanoramicViewportGlass";
            glass.transform.SetParent(frameGo.transform, false);
            glass.transform.localPosition = new Vector3(0, 2.0f, 0.05f);
            glass.transform.localScale = new Vector3(6.8f, 2.45f, 0.03f);
            glass.GetComponent<Renderer>().sharedMaterial = mats.ViewportGlass;
            UnityEngine.Object.DestroyImmediate(glass.GetComponent<Collider>());

            // Backing collider so player/interactors cannot breach canopy into space
            var glassCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassCollider.name = "Viewport_BackingCollider";
            glassCollider.transform.SetParent(frameGo.transform, false);
            glassCollider.transform.localPosition = new Vector3(0, 2.0f, 0.25f);
            glassCollider.transform.localScale = new Vector3(7.2f, 3.5f, 0.2f);
            glassCollider.GetComponent<Renderer>().enabled = false;

            // 2. Outer Space Deep Celestial Vista (Positioned z = 8m to 30m outside viewport)
            var spaceVistaRoot = new GameObject("OuterSpace_CelestialEntities");
            spaceVistaRoot.transform.SetParent(vistaRoot.transform, false);

            // Starfield: 200 Emissive Stars distributed across the forward canopy window
            var starfieldGo = new GameObject("Starfield_Constellation");
            starfieldGo.transform.SetParent(spaceVistaRoot.transform, false);

            var rnd = new System.Random(42);
            for (int i = 0; i < 200; i++)
            {
                float x = (float)(rnd.NextDouble() * 16.0 - 8.0);
                float y = (float)(rnd.NextDouble() * 5.5 + 0.5);
                float z = (float)(rnd.NextDouble() * 20.0 + 8.0);
                float starSize = (float)(rnd.NextDouble() * 0.16 + 0.07);

                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = $"Star_{i:D3}";
                star.transform.SetParent(starfieldGo.transform, false);
                star.transform.position = new Vector3(x, y, z);
                star.transform.localScale = new Vector3(starSize, starSize, starSize);

                var sRend = star.GetComponent<Renderer>();
                sRend.sharedMaterial = mats.StarGlow;

                UnityEngine.Object.DestroyImmediate(star.GetComponent<Collider>());
            }

            // Colossal Ringed Gas Giant Planet (Framed directly through upper forward canopy window)
            var planetGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetGo.name = "Celestial_GasGiantPlanet";
            planetGo.transform.SetParent(spaceVistaRoot.transform, false);
            planetGo.transform.position = new Vector3(2.4f, 2.15f, 9.5f);
            planetGo.transform.localScale = new Vector3(3.4f, 3.4f, 3.4f);
            planetGo.transform.rotation = Quaternion.Euler(18f, -25f, 10f);
            planetGo.GetComponent<Renderer>().sharedMaterial = mats.PlanetGasGiant;
            UnityEngine.Object.DestroyImmediate(planetGo.GetComponent<Collider>());

            // Glowing Planetary Rings
            var ringsGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringsGo.name = "PlanetaryRings";
            ringsGo.transform.SetParent(planetGo.transform, false);
            ringsGo.transform.localPosition = Vector3.zero;
            ringsGo.transform.localScale = new Vector3(2.25f, 0.06f, 2.25f);
            ringsGo.transform.localRotation = Quaternion.Euler(14f, 0, 0);
            SetMeshOrKeepPrimitive(ringsGo, PathTorus, true);
            ringsGo.GetComponent<Renderer>().sharedMaterial = mats.PlanetRings;
            UnityEngine.Object.DestroyImmediate(ringsGo.GetComponent<Collider>());

            // Floating Asteroid Belt Cluster (Tumbling outside canopy)
            var asteroidFieldGo = new GameObject("FloatingAsteroidField");
            asteroidFieldGo.AddComponent<FloatingAsteroidField>();
            asteroidFieldGo.transform.SetParent(spaceVistaRoot.transform, false);
            Vector3[] astPositions = new Vector3[]
            {
                new Vector3(-3.0f, 2.6f, 11.0f),
                new Vector3(3.2f, 1.8f, 10.0f),
                new Vector3(-4.2f, 3.5f, 15.0f),
                new Vector3(4.5f, 3.2f, 16.0f),
                new Vector3(-5.5f, 1.5f, 9.5f),
                new Vector3(2.0f, 4.2f, 18.0f)
            };
            float[] astScales = new float[] { 1.2f, 0.85f, 1.5f, 1.1f, 0.75f, 1.6f };

            for (int a = 0; a < astPositions.Length; a++)
            {
                var ast = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ast.name = $"Asteroid_{a + 1}";
                ast.transform.SetParent(asteroidFieldGo.transform, false);
                ast.transform.position = astPositions[a];
                ast.transform.localScale = new Vector3(astScales[a], astScales[a] * 0.85f, astScales[a] * 1.2f);
                ast.transform.rotation = Quaternion.Euler(a * 37f, a * 53f, a * 19f);
                ast.GetComponent<Renderer>().sharedMaterial = mats.SpaceAsteroid;
                SetMeshOrKeepPrimitive(ast, PathPyramid);
                UnityEngine.Object.DestroyImmediate(ast.GetComponent<Collider>());
            }

            var faintEarth = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            faintEarth.name = "Celestial_FaintEarth";
            faintEarth.transform.SetParent(spaceVistaRoot.transform, false);
            faintEarth.transform.position = new Vector3(8f, 3.2f, 40f);
            faintEarth.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            faintEarth.GetComponent<Renderer>().sharedMaterial = mats.EarthWater;
            UnityEngine.Object.DestroyImmediate(faintEarth.GetComponent<Collider>());

            CreateCinematicShip(spaceVistaRoot.transform, mats);

            var nose = CreateModelInstance(PathKenneyStructure, "ShipNose", spaceVistaRoot.transform);
            if (nose != null)
            {
                nose.transform.position = new Vector3(0f, 0.85f, 5.4f);
                nose.transform.localScale = new Vector3(1.4f, 0.6f, 2.2f);
                ApplyMaterialRecursively(nose, mats.SpaceshipHull);
            }

            var shutterRoot = new GameObject("CanopyShutters");
            shutterRoot.transform.SetParent(spaceVistaRoot.transform, false);
            var left = CreateModelInstance(PathKenneyWallWindowShutters, "ShutterLeft", shutterRoot.transform);
            var right = CreateModelInstance(PathKenneyWallWindowShutters, "ShutterRight", shutterRoot.transform);
            if (left != null)
            {
                left.transform.position = new Vector3(-2.85f, 1.7f, 3.35f);
                left.transform.localScale = new Vector3(0.35f, 2.3f, 0.2f);
            }
            if (right != null)
            {
                right.transform.position = new Vector3(2.85f, 1.7f, 3.35f);
                right.transform.localScale = new Vector3(0.35f, 2.3f, 0.2f);
            }
            var canopy = shutterRoot.AddComponent<CanopyShutters>();
            var canopySO = new SerializedObject(canopy);
            if (left != null) canopySO.FindProperty("leftShutter").objectReferenceValue = left.transform;
            if (right != null) canopySO.FindProperty("rightShutter").objectReferenceValue = right.transform;
            canopySO.ApplyModifiedProperties();

            CreatePhotoBodies(spaceVistaRoot.transform, mats);
        }

        private static void CreateTactileCommandBridgeRail(Transform parent, LevelMaterials mats)
        {
            var railRoot = new GameObject("StationCommandBridgeRail");
            railRoot.transform.SetParent(parent, false);
            railRoot.transform.position = new Vector3(0, 0, 0.55f);

            // Modular Kenney Balustrade Rails (balcony-rail.fbx) across command deck edge
            for (int i = -2; i <= 2; i++)
            {
                var rail = CreateModelInstance(PathKenneyBalconyRail, $"BalconyRail_{i + 2}", railRoot.transform);
                if (rail != null)
                {
                    rail.transform.localPosition = new Vector3(i * 1.0f, 0, 0);
                    rail.transform.localScale = Vector3.one;
                    rail.transform.localRotation = Quaternion.identity;
                    ApplyMaterialRecursively(rail, mats.SpaceshipHull);
                }
            }

            // Left & Right Corner End Caps
            var leftCorner = CreateModelInstance(PathKenneyBalconyRailCorner, "BalconyRail_Corner_Left", railRoot.transform);
            if (leftCorner != null)
            {
                leftCorner.transform.localPosition = new Vector3(-2.5f, 0, 0);
                leftCorner.transform.localScale = Vector3.one;
                leftCorner.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                ApplyMaterialRecursively(leftCorner, mats.SpaceshipHull);
            }

            var rightCorner = CreateModelInstance(PathKenneyBalconyRailCorner, "BalconyRail_Corner_Right", railRoot.transform);
            if (rightCorner != null)
            {
                rightCorner.transform.localPosition = new Vector3(2.5f, 0, 0);
                rightCorner.transform.localScale = Vector3.one;
                rightCorner.transform.localRotation = Quaternion.identity;
                ApplyMaterialRecursively(rightCorner, mats.SpaceshipHull);
            }

            // Backing collider so player cannot fall past command edge
            var railColl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railColl.name = "CommandRail_Collider";
            railColl.transform.SetParent(railRoot.transform, false);
            railColl.transform.localPosition = new Vector3(0, 0.5f, 0);
            railColl.transform.localScale = new Vector3(5.4f, 1.0f, 0.25f);
            railColl.GetComponent<Renderer>().enabled = false;
        }

        private static void CreateHazardStripeSegment(Transform parent, Vector3 center, Vector3 size, LevelMaterials mats)
        {
            var hazardRoot = new GameObject("HazardStripingSegment");
            hazardRoot.transform.SetParent(parent, false);
            hazardRoot.transform.position = center;

            // Backing plate
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "HazardBacking";
            backing.transform.SetParent(hazardRoot.transform, false);
            backing.transform.localPosition = Vector3.zero;
            backing.transform.localScale = size;
            backing.GetComponent<Renderer>().sharedMaterial = mats.HazardDark;

            // Alternating diagonal hazard blocks
            bool isLongitudinal = size.z > size.x;
            float totalLength = isLongitudinal ? size.z : size.x;
            int stripeCount = Mathf.Max(4, Mathf.RoundToInt(totalLength / 0.5f));
            float stripeWidth = totalLength / stripeCount;

            for (int i = 0; i < stripeCount; i += 2)
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = $"YellowStripe_{i}";
                stripe.transform.SetParent(hazardRoot.transform, false);
                float offset = -totalLength * 0.5f + (i + 0.5f) * stripeWidth;
                if (isLongitudinal)
                {
                    stripe.transform.localPosition = new Vector3(0, 0.005f, offset);
                    stripe.transform.localScale = new Vector3(size.x * 0.96f, size.y * 1.2f, stripeWidth * 0.85f);
                }
                else
                {
                    stripe.transform.localPosition = new Vector3(offset, 0.005f, 0);
                    stripe.transform.localScale = new Vector3(stripeWidth * 0.85f, size.y * 1.2f, size.z * 0.96f);
                }
                stripe.GetComponent<Renderer>().sharedMaterial = mats.HazardYellow;
                UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());
            }
        }

        private static void CreateDeckRunwayLights(Transform parent, Material lightMat)
        {
            var lightsRoot = new GameObject("DeckRunwayGuideLights");
            lightsRoot.transform.SetParent(parent, false);

            for (int i = 0; i < 7; i++)
            {
                float z = -2.7f + (i * 0.9f);

                var runL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                runL.name = $"RunwayLED_Port_{i + 1}";
                runL.transform.SetParent(lightsRoot.transform, false);
                runL.transform.position = new Vector3(-2.8f, 0.02f, z);
                runL.transform.localScale = new Vector3(0.06f, 0.02f, 0.50f);
                runL.GetComponent<Renderer>().sharedMaterial = lightMat;
                UnityEngine.Object.DestroyImmediate(runL.GetComponent<Collider>());

                var runR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                runR.name = $"RunwayLED_Starboard_{i + 1}";
                runR.transform.SetParent(lightsRoot.transform, false);
                runR.transform.position = new Vector3(2.8f, 0.02f, z);
                runR.transform.localScale = new Vector3(0.06f, 0.02f, 0.50f);
                runR.GetComponent<Renderer>().sharedMaterial = lightMat;
                UnityEngine.Object.DestroyImmediate(runR.GetComponent<Collider>());
            }
        }

        private static void CreateSparkParticles(Transform parent, Material sparkMat)
        {
            var partGo = new GameObject("EmergencySparkParticles");
            partGo.transform.SetParent(parent, false);
            partGo.transform.position = new Vector3(-3.2f, 2.2f, 0.8f);

            var ps = partGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(1.0f, 0.75f, 0.2f, 1.0f);
            main.startSize = 0.035f;
            main.startLifetime = 0.8f;
            main.startSpeed = 2.5f;
            main.gravityModifier = 1.0f;
            main.maxParticles = 25;

            var emission = ps.emission;
            emission.rateOverTime = 4f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var rend = partGo.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = sparkMat;

            var breach = GameObject.CreatePrimitive(PrimitiveType.Quad);
            breach.name = "HullBreachPatch";
            breach.transform.SetParent(partGo.transform, false);
            breach.transform.localPosition = new Vector3(0f, 0.4f, 0.2f);
            breach.transform.localScale = new Vector3(0.8f, 0.5f, 1f);
            breach.GetComponent<Renderer>().sharedMaterial = sparkMat;
            UnityEngine.Object.DestroyImmediate(breach.GetComponent<Collider>());

            var damage = partGo.AddComponent<ShipDamageVisual>();
            var damageSO = new SerializedObject(damage);
            damageSO.FindProperty("sparks").objectReferenceValue = ps;
            damageSO.FindProperty("breachPatch").objectReferenceValue = breach;
            damageSO.ApplyModifiedProperties();
        }

        private static void CreatePlatformTrim(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "PlatformTrim_Glow";
            trim.transform.SetParent(parent);
            trim.transform.localPosition = localPos;
            trim.transform.localScale = localScale;
            trim.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());
        }

        private static void CreateBusTraces(Transform parent, Material traceMat)
        {
            var tracesRoot = new GameObject("MotherboardBusTraces");
            tracesRoot.transform.SetParent(parent);

            Vector3[] traceOffsets = new Vector3[]
            {
                new Vector3(-1.8f, 0.012f, 0.0f), new Vector3(0.015f, 0.005f, 5.0f),
                new Vector3(1.8f, 0.012f, 0.0f), new Vector3(0.015f, 0.005f, 5.0f),
                new Vector3(-0.8f, 0.012f, 0.0f), new Vector3(0.012f, 0.005f, 4.0f),
                new Vector3(0.8f, 0.012f, 0.0f), new Vector3(0.012f, 0.005f, 4.0f),
                new Vector3(0f, 0.012f, -1.5f), new Vector3(4.0f, 0.005f, 0.015f),
                new Vector3(0f, 0.012f, 0.5f), new Vector3(4.0f, 0.005f, 0.015f),
                new Vector3(0f, 0.012f, 2.2f), new Vector3(4.0f, 0.005f, 0.015f)
            };

            for (int i = 0; i < traceOffsets.Length; i += 2)
            {
                var trace = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trace.name = $"BusTrace_{i / 2}";
                trace.transform.SetParent(tracesRoot.transform);
                trace.transform.localPosition = traceOffsets[i];
                trace.transform.localScale = traceOffsets[i + 1];
                trace.GetComponent<Renderer>().sharedMaterial = traceMat;
                UnityEngine.Object.DestroyImmediate(trace.GetComponent<Collider>());
            }
        }

        private static Renderer[] CreatePerimeterHardware(Transform parent, LevelMaterials mats)
        {
            var hardwareRoot = new GameObject("MotherboardPerimeterHardware");
            hardwareRoot.transform.SetParent(parent);

            var ledsList = new System.Collections.Generic.List<Renderer>();

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 3.35f;
                for (int i = 0; i < 3; i++)
                {
                    float z = -1.8f + (i * 1.8f);

                    var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chip.name = $"IC_MemoryRegister_{((side + 1) * 3) + i:D2}";
                    chip.transform.SetParent(hardwareRoot.transform);
                    chip.transform.position = new Vector3(x, 0.10f, z);
                    chip.transform.localScale = new Vector3(0.5f, 0.20f, 0.8f);
                    chip.GetComponent<Renderer>().sharedMaterial = mats.MemoryChip;

                    var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    led.name = "StatusLED";
                    led.transform.SetParent(chip.transform);
                    led.transform.localPosition = new Vector3(0, 0.55f, 0);
                    led.transform.localScale = new Vector3(0.20f, 0.12f, 0.20f);
                    var ledRend = led.GetComponent<Renderer>();
                    ledRend.sharedMaterial = mats.GlowCyan;
                    ledsList.Add(ledRend);
                    UnityEngine.Object.DestroyImmediate(led.GetComponent<Collider>());

                    var heatSink = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    heatSink.name = $"HeatSinkFin_{((side + 1) * 3) + i:D2}";
                    heatSink.transform.SetParent(hardwareRoot.transform);
                    heatSink.transform.position = new Vector3(x + (side * 0.15f), 0.30f, z);
                    heatSink.transform.localScale = new Vector3(0.20f, 0.40f, 0.7f);
                    heatSink.GetComponent<Renderer>().sharedMaterial = mats.HeatSinkFins;
                }
            }

            return ledsList.ToArray();
        }

        private static void CreateDataParticles(Transform parent, Material particleMat)
        {
            var partGo = new GameObject("DataStreamParticles");
            partGo.transform.SetParent(parent);
            partGo.transform.position = new Vector3(0, 1.8f, 0.5f);

            var ps = partGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.25f, 0.75f, 1.0f, 0.30f);
            main.startSize = 0.020f;
            main.startLifetime = 4.0f;
            main.startSpeed = 0.12f;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5.0f, 2.5f, 5.0f);

            var rend = partGo.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = particleMat;
        }

        private static LevelMaterials GenerateMaterials()
        {
            var mats = new LevelMaterials();

            var normalMap = GetSampleTexture(PathConcreteNormal);
            var metallicMap = GetSampleTexture(PathConcreteMetallic);
            var kenneyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PathKenneyTexture);

            // 1. Kenney Space Station Master Material (Authentic UV-mapped modular sci-fi panels)
            // Value order: ceiling darkest, floor, walls, then the console surround.
            // Smoothness stays at or below 0.3 and metallic at or below 0.3 on these surfaces.
            mats.KenneyStation = CreateOrUpdateMaterial("Mat_Kenney_SpaceStation", new Color(0.18f, 0.19f, 0.21f), 0.15f, 0.25f);
            mats.SpaceshipHull = CreateOrUpdateMaterial("Mat_Spaceship_Hull", new Color(0.32f, 0.34f, 0.37f), 0.20f, 0.25f);
            mats.CeilingValue = CreateOrUpdateMaterial("Mat_Chamber01_Ceiling", new Color(0.07f, 0.08f, 0.09f), 0.20f, 0.25f);
            mats.DarkPlating = CreateOrUpdateMaterial("Mat_Mainframe_DarkPlating", new Color(0.50f, 0.52f, 0.55f), 0.30f, 0.25f);
            mats.CpuSubstrate = CreateOrUpdateMaterial("Mat_Mainframe_Substrate", new Color(0.40f, 0.42f, 0.45f), 0.30f, 0.25f);

            mats.HeatSinkFins = CreateOrUpdateMaterial("Mat_Mainframe_HeatSink", new Color(0.35f, 0.38f, 0.42f), 0.30f, 0.25f);
            mats.MemoryChip = CreateOrUpdateMaterial("Mat_Mainframe_MemoryChip", new Color(0.28f, 0.30f, 0.33f), 0.30f, 0.25f);

            // Deck traces are scenery. Saturated cyan stays on state, not on the floor.
            mats.BusTraceGlow = CreateOrUpdateMaterial("Mat_Mainframe_BusTrace", new Color(0.28f, 0.32f, 0.36f), 0.20f, 0.25f);
            mats.WallPillarAccent = CreateOrUpdateMaterial("Mat_Mainframe_WallPillar", new Color(0.34f, 0.36f, 0.40f), 0.20f, 0.25f);

            mats.AxonPositive = CreateOrUpdateUnlitMaterial("Mat_Axon_Positive", new Color(0.22f, 0.75f, 0.98f, 0.75f));
            mats.AxonNegative = CreateOrUpdateUnlitMaterial("Mat_Axon_Negative", new Color(0.22f, 0.75f, 0.98f, 0.75f));
            mats.AxonNeutral = CreateOrUpdateMaterial("Mat_Axon_Neutral", new Color(0.30f, 0.34f, 0.38f, 0.40f), 0.10f, 0.25f, isTransparent: true);

            // Cyan and amber are state. Former green, violet, and coral swatches are slate or off-white.
            // Console-used glow emission is half of the previous 1.4 multiplier.
            mats.GlowCyan = CreateOrUpdateMaterial("Mat_Glow_Cyan", new Color(0.20f, 0.75f, 0.98f), 0.20f, 0.30f, new Color(0.20f, 0.75f, 0.98f) * 0.7f);
            mats.GlowAmber = CreateOrUpdateMaterial("Mat_Glow_Amber", new Color(0.98f, 0.62f, 0.08f), 0.20f, 0.30f, new Color(0.98f, 0.60f, 0.08f) * 0.7f);
            mats.GlowEmerald = CreateOrUpdateMaterial("Mat_Glow_Emerald", new Color(0.42f, 0.46f, 0.50f), 0.10f, 0.25f);
            mats.GlowViolet = CreateOrUpdateMaterial("Mat_Glow_Violet", new Color(0.40f, 0.42f, 0.46f), 0.10f, 0.25f);
            mats.GlowCoral = CreateOrUpdateMaterial("Mat_Glow_Coral", new Color(0.82f, 0.83f, 0.80f), 0.05f, 0.25f);

            mats.GlassHologram = CreateOrUpdateMaterial("Mat_Mainframe_GlassHologram", new Color(0.45f, 0.50f, 0.55f, 0.18f), 0.05f, 0.30f, isTransparent: true);
            mats.CorePlasma = CreateOrUpdateUnlitMaterial("Mat_Mainframe_CorePlasma", new Color(0.20f, 0.75f, 0.98f, 0.85f));
            mats.PortalCurtain = CreateOrUpdateMaterial("Mat_Mainframe_PortalCurtain", new Color(0.55f, 0.40f, 0.16f, 0.30f), 0.05f, 0.30f, emissionColor: new Color(0.98f, 0.60f, 0.08f) * 0.25f, isTransparent: true);

            mats.GunMetal = CreateOrUpdateMaterial("Mat_Mainframe_GunMetal", new Color(0.36f, 0.38f, 0.42f), 0.30f, 0.25f);
            mats.GunAccent = CreateOrUpdateMaterial("Mat_Mainframe_GunAccent", new Color(0.48f, 0.50f, 0.54f), 0.30f, 0.25f);
            mats.StanchionGlow = CreateOrUpdateMaterial("Mat_Mainframe_StanchionGlow", new Color(0.40f, 0.43f, 0.47f), 0.20f, 0.25f);

            mats.BulkheadRib = CreateOrUpdateMaterial("Mat_Spaceship_BulkheadRib", new Color(0.09f, 0.10f, 0.11f), 0.20f, 0.25f);
            mats.ConduitPipe = CreateOrUpdateMaterial("Mat_Spaceship_ConduitPipe", new Color(0.30f, 0.32f, 0.35f), 0.30f, 0.25f);
            mats.HazardYellow = CreateOrUpdateMaterial("Mat_Spaceship_HazardYellow", new Color(0.42f, 0.40f, 0.36f), 0.10f, 0.25f);
            mats.HazardDark = CreateOrUpdateMaterial("Mat_Spaceship_HazardDark", new Color(0.12f, 0.12f, 0.14f), 0.20f, 0.25f);

            // Red stays defined for overload. The room strobes use amber, which is the crisis state.
            mats.WarningRed = CreateOrUpdateMaterial("Mat_Spaceship_WarningRed", new Color(0.95f, 0.15f, 0.15f), 0.20f, 0.30f, emissionColor: new Color(1.0f, 0.15f, 0.15f) * 0.6f);
            mats.WarningAmber = CreateOrUpdateMaterial("Mat_Spaceship_WarningAmber", new Color(0.98f, 0.60f, 0.05f), 0.20f, 0.30f, emissionColor: new Color(1.0f, 0.60f, 0.05f) * 0.8f);
            mats.CeilingLight = CreateOrUpdateMaterial("Mat_Spaceship_CeilingLight", new Color(0.55f, 0.58f, 0.62f), 0.05f, 0.25f, emissionColor: new Color(0.70f, 0.74f, 0.78f) * 0.2f);

            // 11. Earth Planetary Navigation & Deep Space Vista Materials
            var earthTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Images/EarthTarget.jpg");

            mats.EarthWater = CreateOrUpdateMaterial("Mat_Earth_Water", Color.white, 0.05f, 0.85f, emissionColor: new Color(0.12f, 0.35f, 0.70f) * 0.15f, baseMap: earthTex);
            mats.EarthLand = CreateOrUpdateMaterial("Mat_Earth_Land", Color.white, 0.05f, 0.75f, emissionColor: new Color(0.10f, 0.50f, 0.25f) * 0.15f, baseMap: earthTex);
            mats.EarthAtmosphere = CreateOrUpdateMaterial("Mat_Earth_Atmosphere", new Color(0.45f, 0.52f, 0.58f, 0.22f), 0.0f, 0.30f, isTransparent: true);
            mats.EarthPhotoDisplay = CreateOrUpdateUnlitMaterial("Mat_Earth_PhotoDisplay", Color.white, baseMap: earthTex);
            mats.SpaceAsteroid = CreateOrUpdateMaterial("Mat_Space_Asteroid", new Color(0.35f, 0.32f, 0.30f), 0.10f, 0.40f, normalMap: normalMap);
            mats.StarGlow = CreateOrUpdateMaterial("Mat_Space_StarGlow", new Color(0.75f, 0.78f, 0.82f), 0.0f, 0.25f, emissionColor: new Color(0.75f, 0.78f, 0.82f) * 0.35f);

            mats.PlanetGasGiant = CreateOrUpdateMaterial("Mat_Space_GasGiant", new Color(0.42f, 0.38f, 0.32f), 0.10f, 0.25f);
            mats.PlanetRings = CreateOrUpdateMaterial("Mat_Space_PlanetRings", new Color(0.40f, 0.38f, 0.34f, 0.55f), 0.05f, 0.25f, isTransparent: true);
            mats.ViewportGlass = CreateOrUpdateMaterial("Mat_Spaceship_ViewportGlass", new Color(0.55f, 0.60f, 0.64f, 0.04f), 0.0f, 0.30f, isTransparent: true);

            return mats;
        }

        private static Material CreateOrUpdateMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emissionColor = null, bool isTransparent = false, Texture2D normalMap = null, Texture2D metallicMap = null, Texture2D baseMap = null)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (baseMap != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseMap);
            }

            if (normalMap != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metallicMap != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", metallicMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor.Value);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            if (isTransparent)
            {
                if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 2900; // Render before TextMesh (queue 3000) so text is always crisp and visible
            }
            else
            {
                if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 0);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrUpdateUnlitMaterial(string name, Color color, Texture2D baseMap = null)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (baseMap != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseMap);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!Directory.Exists(PresetsFolder)) Directory.CreateDirectory(PresetsFolder);
            if (!Directory.Exists(MaterialsFolder)) Directory.CreateDirectory(MaterialsFolder);
        }

        private static BrokenConfigurationSO[] GenerateBrokenPresets()
        {
            var presets = new BrokenConfigurationSO[7];

            // Opening strike: both sensor cables ripped out. This is presets[0], the level start.
            presets[0] = CreateOrUpdatePreset("BrokenConfig_Opening", "Preset Opening", 0.0, 0.0, -1.0, ActivationType.Linear, true, true, "Opening strike. Both sensor cables ripped out. The laser cannot see rock or ice.");

            presets[1] = CreateOrUpdatePreset("BrokenConfig_A", "Preset A", 0.0, 0.0, -1.0, ActivationType.Linear, true, false, "Cold initial state with disconnected X1 conduit.");
            presets[2] = CreateOrUpdatePreset("BrokenConfig_B", "Preset B", -0.5, 1.0, 0.0, ActivationType.Step, false, true, "Negative W1 corruption and disconnected X2 conduit.");
            presets[3] = CreateOrUpdatePreset("BrokenConfig_C", "Preset C", 1.0, 1.0, -2.0, ActivationType.ReLU, false, false, "Severe negative bias under-activation with incompatible ReLU module.");
            presets[4] = CreateOrUpdatePreset("BrokenConfig_D", "Preset D", 0.5, 0.5, 0.0, ActivationType.Linear, true, true, "Weak synapses with both input conduits disconnected.");
            presets[5] = CreateOrUpdatePreset("BrokenConfig_E", "Preset E", -1.0, -1.0, 1.0, ActivationType.Step, true, false, "Inverted negative weights and high bias error.");
            presets[6] = CreateOrUpdatePreset("BrokenConfig_F", "Preset F", 0.0, 0.0, 0.0, ActivationType.Step, false, false, "Zero-energy dormant state with Step activation socketed.");

            return presets;
        }

        private static BrokenConfigurationSO CreateOrUpdatePreset(
            string assetName,
            string id,
            double w1,
            double w2,
            double bias,
            ActivationType activation,
            bool c1Dis,
            bool c2Dis,
            string description)
        {
            string path = $"{PresetsFolder}/{assetName}.asset";
            var preset = AssetDatabase.LoadAssetAtPath<BrokenConfigurationSO>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<BrokenConfigurationSO>();
                AssetDatabase.CreateAsset(preset, path);
            }

            var so = new SerializedObject(preset);
            so.FindProperty("configId").stringValue = id;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("initialW1").doubleValue = w1;
            so.FindProperty("initialW2").doubleValue = w2;
            so.FindProperty("initialBias").doubleValue = bias;
            so.FindProperty("initialActivation").enumValueIndex = (int)activation;
            so.FindProperty("cable1Disconnected").boolValue = c1Dis;
            so.FindProperty("cable2Disconnected").boolValue = c2Dis;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static void CreatePlanetaryAICoreHologram(Transform parent, LevelMaterials mats, ChamberController chamberController, NeuralState neuralState)
        {
            // 0. Physical Holographic Projection Pedestal (Kenney table-display-planet.fbx)
            var pedestal = CreateModelInstance(PathKenneyTableDisplayPlanet, "PlanetaryCore_HoloPedestal", parent);
            if (pedestal != null)
            {
                pedestal.transform.position = new Vector3(-2.60f, 0f, 2.60f);
                pedestal.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                pedestal.transform.rotation = Quaternion.identity;
                ApplyMaterialRecursively(pedestal, mats.KenneyStation);
            }

            // Vertical holographic emitter beam connecting pedestal to the hovering AI mind sphere
            var beamGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beamGo.name = "Pedestal_ProjectionBeam";
            beamGo.transform.SetParent(parent, false);
            beamGo.transform.position = new Vector3(-2.60f, 1.10f, 2.60f);
            beamGo.transform.localScale = new Vector3(0.25f, 1.10f, 0.25f);
            beamGo.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;
            UnityEngine.Object.DestroyImmediate(beamGo.GetComponent<Collider>());

            var planetRoot = new GameObject("PlanetaryAICore_Hologram");
            planetRoot.transform.SetParent(parent, false);
            planetRoot.transform.position = new Vector3(-2.60f, 2.25f, 2.60f);

            // 1. Central AI Mind Sphere (Photorealistic Earth Navigation Hologram)
            var sphereGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereGo.name = "PlanetaryCore_Sphere";
            sphereGo.transform.SetParent(planetRoot.transform, false);
            sphereGo.transform.localPosition = Vector3.zero;
            sphereGo.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            sphereGo.GetComponent<Renderer>().sharedMaterial = mats.EarthWater;
            UnityEngine.Object.DestroyImmediate(sphereGo.GetComponent<Collider>());

            // Atmospheric Haze Glow Shell
            var atmoGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmoGo.name = "PlanetaryCore_AtmosphereShell";
            atmoGo.transform.SetParent(planetRoot.transform, false);
            atmoGo.transform.localPosition = Vector3.zero;
            atmoGo.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
            atmoGo.GetComponent<Renderer>().sharedMaterial = mats.EarthAtmosphere;
            UnityEngine.Object.DestroyImmediate(atmoGo.GetComponent<Collider>());

            // 2. Inner Gyroscopic Ring
            var innerRingGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRingGo.name = "InnerOrbitalRing";
            innerRingGo.transform.SetParent(planetRoot.transform, false);
            innerRingGo.transform.localPosition = Vector3.zero;
            innerRingGo.transform.localScale = new Vector3(0.95f, 0.012f, 0.95f);
            innerRingGo.transform.localRotation = Quaternion.Euler(35f, 20f, 0);
            innerRingGo.GetComponent<Renderer>().sharedMaterial = mats.BusTraceGlow;
            UnityEngine.Object.DestroyImmediate(innerRingGo.GetComponent<Collider>());

            // 3. Outer Gyroscopic Ring
            var outerRingGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRingGo.name = "OuterOrbitalRing";
            outerRingGo.transform.SetParent(planetRoot.transform, false);
            outerRingGo.transform.localPosition = Vector3.zero;
            outerRingGo.transform.localScale = new Vector3(1.25f, 0.012f, 1.25f);
            outerRingGo.transform.localRotation = Quaternion.Euler(-25f, 45f, 30f);
            outerRingGo.GetComponent<Renderer>().sharedMaterial = mats.WallPillarAccent;
            UnityEngine.Object.DestroyImmediate(outerRingGo.GetComponent<Collider>());

            // 4. Core Aura Light
            var coreLight = planetRoot.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 4.0f;
            coreLight.intensity = 0.45f;
            coreLight.color = new Color(0.95f, 0.25f, 0.25f);

            // 5. Billboard Text Container
            var textContainer = new GameObject("Planetary_BillboardTexts");
            textContainer.transform.SetParent(planetRoot.transform, false);
            textContainer.transform.localPosition = new Vector3(0, 0.60f, 0);

            var headerGo = new GameObject("Planetary_Header");
            headerGo.transform.SetParent(textContainer.transform, false);
            headerGo.transform.localPosition = new Vector3(0, 0.22f, 0);
            var headerTxt = headerGo.AddComponent<TextMesh>();
            headerTxt.text = "";
            headerTxt.fontSize = 20;
            headerTxt.characterSize = 0.014f;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAlignment.Center;
            headerTxt.anchor = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(0.95f, 0.30f, 0.30f);

            var barGo = new GameObject("Planetary_IntegrityBar");
            barGo.transform.SetParent(textContainer.transform, false);
            barGo.transform.localPosition = new Vector3(0, 0.04f, 0);
            var barTxt = barGo.AddComponent<TextMesh>();
            barTxt.text = "";
            barTxt.fontSize = 18;
            barTxt.characterSize = 0.012f;
            barTxt.fontStyle = FontStyle.Bold;
            barTxt.alignment = TextAlignment.Center;
            barTxt.anchor = TextAnchor.MiddleCenter;
            barTxt.color = new Color(0.98f, 0.65f, 0.10f);

            var detailsGo = new GameObject("Planetary_StatusDetails");
            detailsGo.transform.SetParent(textContainer.transform, false);
            detailsGo.transform.localPosition = new Vector3(0, -0.15f, 0);
            var detailsTxt = detailsGo.AddComponent<TextMesh>();
            detailsTxt.text = "";
            detailsTxt.fontSize = 15;
            detailsTxt.characterSize = 0.010f;
            detailsTxt.alignment = TextAlignment.Center;
            detailsTxt.anchor = TextAnchor.MiddleCenter;
            detailsTxt.color = Color.white;

            // 6. PlanetaryAICoreHologram Component
            var planetComponent = planetRoot.AddComponent<PlanetaryAICoreHologram>();
            var pSO = new SerializedObject(planetComponent);
            pSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            pSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            pSO.FindProperty("planetSphere").objectReferenceValue = sphereGo.transform;
            pSO.FindProperty("innerOrbitalRing").objectReferenceValue = innerRingGo.transform;
            pSO.FindProperty("outerOrbitalRing").objectReferenceValue = outerRingGo.transform;
            pSO.FindProperty("coreAuraLight").objectReferenceValue = coreLight;
            pSO.FindProperty("headerTextMesh").objectReferenceValue = headerTxt;
            pSO.FindProperty("integrityBarTextMesh").objectReferenceValue = barTxt;
            pSO.FindProperty("statusDetailsTextMesh").objectReferenceValue = detailsTxt;
            pSO.ApplyModifiedProperties();
        }


        private static void PlaceShip(Transform parent, LevelMaterials mats)
        {
            var temp = new GameObject("PF_Ship_Hull");
            CreateSpaceshipBridgeHull(temp.transform, mats);
            CreateAftShip(temp.transform, mats);
            string folder = "Assets/_Project/Prefabs/Ship";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Ship");
            }

            string path = folder + "/PF_Ship_Hull.prefab";
            PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            foreach (var filter in instance.GetComponentsInChildren<MeshRenderer>())
            {
                GameObjectUtility.SetStaticEditorFlags(filter.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            }
        }

        private static void CreateAftShip(Transform parent, LevelMaterials mats)
        {
            var root = new GameObject("AftShip");
            root.transform.SetParent(parent, false);

            MakeDoor(root.transform, "Door_SensorBay", "SensorBay", new Vector3(0f, 1.1f, -3.85f), mats);
            for (int i = 0; i < 4; i++)
            {
                float z = -5.2f - i * 1.6f;
                var left = CreateModelInstance(PathKenneyWall, "CorridorWall_L_" + i, root.transform);
                var right = CreateModelInstance(PathKenneyWall, "CorridorWall_R_" + i, root.transform);
                var floor = CreateModelInstance(PathKenneyFloor, "CorridorFloor_" + i, root.transform);
                PlaceModule(left, new Vector3(-1.6f, 0f, z), Quaternion.Euler(0f, 90f, 0f), mats.SpaceshipHull);
                PlaceModule(right, new Vector3(1.6f, 0f, z), Quaternion.Euler(0f, -90f, 0f), mats.SpaceshipHull);
                PlaceModule(floor, new Vector3(0f, 0f, z), Quaternion.identity, mats.KenneyStation);
            }

            var observatory = CreateModelInstance(PathKenneyWallDoorWide, "ObservatoryDoorFrame", root.transform);
            PlaceModule(observatory, new Vector3(1.6f, 0f, -7.2f), Quaternion.Euler(0f, -90f, 0f), mats.SpaceshipHull);
            MakeDoor(root.transform, "Door_Observatory", "Observatory", new Vector3(2.4f, 1.1f, -7.2f), mats);
            var telescope = CreateModelInstance(PathKenneyComputerSystem, "Telescope", root.transform);
            PlaceModule(telescope, new Vector3(4.2f, 0f, -7.2f), Quaternion.Euler(0f, -90f, 0f), mats.DarkPlating);
            MakeVoyageButton(root.transform, "InstallHiddenLayer", VoyageButton.Kind.InstallHiddenLayer, new Vector3(3.4f, 1.05f, -6.6f), mats);
            MakeVoyageButton(root.transform, "CycleNeuron", VoyageButton.Kind.CycleNeuron, new Vector3(3.4f, 1.05f, -7.8f), mats);
            MakePhotoButton(root.transform, "Tray_Earth", true, new Vector3(4.6f, 1.05f, -6.4f), mats);
            MakePhotoButton(root.transform, "Tray_NotEarth", false, new Vector3(4.6f, 1.05f, -8.0f), mats);
            MakeVoyageButton(root.transform, "TrainEarth", VoyageButton.Kind.TrainEarth, new Vector3(4.6f, 1.05f, -7.2f), mats);

            var engineFrame = CreateModelInstance(PathKenneyWallDoorWide, "EngineDoorFrame", root.transform);
            PlaceModule(engineFrame, new Vector3(0f, 0f, -11.2f), Quaternion.identity, mats.SpaceshipHull);
            MakeDoor(root.transform, "Door_Engine", "Engine", new Vector3(0f, 1.1f, -11.2f), mats);
            var drive = CreateModelInstance(PathKenneyStructure, "JumpDrive", root.transform);
            PlaceModule(drive, new Vector3(0f, 0.2f, -13.2f), Quaternion.identity, mats.SpaceshipHull);
            MakeVoyageButton(root.transform, "MaskNoise", VoyageButton.Kind.MaskNoise, new Vector3(0f, 1.15f, -12.4f), mats);

            var corridorCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridorCollider.name = "CorridorDeckCollider";
            corridorCollider.transform.SetParent(root.transform, false);
            corridorCollider.transform.position = new Vector3(0f, -0.2f, -8f);
            corridorCollider.transform.localScale = new Vector3(3f, 0.4f, 10f);
            corridorCollider.GetComponent<Renderer>().enabled = false;
        }

        private static void PlaceModule(GameObject module, Vector3 position, Quaternion rotation, Material material)
        {
            if (module == null) return;
            module.transform.position = position;
            module.transform.rotation = rotation;
            ApplyMaterialRecursively(module, material);
        }

        private static void MakeDoor(Transform parent, string name, string id, Vector3 position, LevelMaterials mats)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(root.transform, false);
            panel.transform.localScale = new Vector3(1.3f, 2.2f, 0.12f);
            panel.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;
            var door = root.AddComponent<ShipDoor>();
            var so = new SerializedObject(door);
            so.FindProperty("doorId").stringValue = id;
            so.FindProperty("blocker").objectReferenceValue = panel.GetComponent<Collider>();
            so.FindProperty("panel").objectReferenceValue = panel.transform;
            so.ApplyModifiedProperties();

            if (id != "SensorBay") return;

            var beaconGo = new GameObject("AftBeacon");
            beaconGo.transform.SetParent(root.transform, false);
            beaconGo.transform.localPosition = new Vector3(0f, 1.35f, 0.2f);
            var beaconLight = beaconGo.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.range = 4.5f;
            beaconLight.intensity = 0.08f;
            beaconLight.color = new Color(0.35f, 0.85f, 1f);
            var beacon = root.AddComponent<DoorUnlockBeacon>();
            var beaconSO = new SerializedObject(beacon);
            beaconSO.FindProperty("door").objectReferenceValue = door;
            beaconSO.FindProperty("beacon").objectReferenceValue = beaconLight;
            beaconSO.ApplyModifiedProperties();
        }

        private static void CreateCinematicShip(Transform parent, LevelMaterials mats)
        {
            var ship = new GameObject("CinematicShip");
            ship.transform.SetParent(parent, false);
            ship.transform.position = new Vector3(0f, 1.45f, 8.6f);
            ship.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(ship.transform, false);
            hull.transform.localScale = new Vector3(1.15f, 0.36f, 2.6f);
            hull.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;
            UnityEngine.Object.DestroyImmediate(hull.GetComponent<Collider>());

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(ship.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.02f, 1.55f);
            nose.transform.localScale = new Vector3(0.48f, 0.26f, 0.72f);
            nose.GetComponent<Renderer>().sharedMaterial = mats.SpaceshipHull;
            UnityEngine.Object.DestroyImmediate(nose.GetComponent<Collider>());

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(ship.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.28f, 0.35f);
            canopy.transform.localScale = new Vector3(0.46f, 0.22f, 0.7f);
            canopy.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(canopy.GetComponent<Collider>());

            CreateWing(ship.transform, "Wing_L", -0.95f, mats.SpaceshipHull);
            CreateWing(ship.transform, "Wing_R", 0.95f, mats.SpaceshipHull);
            CreateEngine(ship.transform, "Engine_L", -0.38f, mats.GlowAmber);
            CreateEngine(ship.transform, "Engine_R", 0.38f, mats.GlowAmber);
        }

        private static void CreateWing(Transform ship, string name, float x, Material material)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = name;
            wing.transform.SetParent(ship, false);
            wing.transform.localPosition = new Vector3(x, -0.02f, -0.15f);
            wing.transform.localScale = new Vector3(0.9f, 0.08f, 1.1f);
            wing.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(wing.GetComponent<Collider>());
        }

        private static void CreateEngine(Transform ship, string name, float x, Material material)
        {
            var engine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            engine.name = name;
            engine.transform.SetParent(ship, false);
            engine.transform.localPosition = new Vector3(x, 0f, -1.25f);
            engine.transform.localScale = new Vector3(0.28f, 0.28f, 0.42f);
            engine.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(engine.GetComponent<Collider>());
            var engineLight = engine.AddComponent<Light>();
            engineLight.type = LightType.Point;
            engineLight.range = 3.2f;
            engineLight.intensity = 0.35f;
            engineLight.color = new Color(1f, 0.55f, 0.15f);
        }

        private static void MakeVoyageButton(Transform parent, string name, VoyageButton.Kind kind, Vector3 position, LevelMaterials mats)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            go.GetComponent<Renderer>().sharedMaterial = mats.GlowAmber;
            var button = go.AddComponent<VoyageButton>();
            var so = new SerializedObject(button);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.ApplyModifiedProperties();
        }

        private static void MakePhotoButton(Transform parent, string name, bool earth, Vector3 position, LevelMaterials mats)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);
            go.GetComponent<Renderer>().sharedMaterial = earth ? mats.EarthLand : mats.GlowCoral;
            var request = go.AddComponent<PhotoRequest>();
            var so = new SerializedObject(request);
            so.FindProperty("fileAsEarth").boolValue = earth;
            so.ApplyModifiedProperties();
        }

        private static void CreatePhotoBodies(Transform parent, LevelMaterials mats)
        {
            MakeBody(parent, "PhotoBody_Earth", new Vector3(-2.2f, 2.4f, 14f), 0.8f, mats.EarthWater);
            MakeBody(parent, "PhotoBody_IceGiant", new Vector3(0f, 3.1f, 16f), 1.1f, mats.GlowCyan);
            MakeBody(parent, "PhotoBody_Rust", new Vector3(2.4f, 2.2f, 13f), 0.7f, mats.GlowCoral);
        }

        private static void MakeBody(Transform parent, string name, Vector3 position, float scale, Material material)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = name;
            body.transform.SetParent(parent, false);
            body.transform.position = position;
            body.transform.localScale = Vector3.one * scale;
            body.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AssignStarfield()
        {
            const string cubePath = "Assets/Materials/Sky_Starfield.cubemap";
            const string matPath = "Assets/Materials/Sky_Starfield.mat";
            if (AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath) != null) AssetDatabase.DeleteAsset(cubePath);
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null) AssetDatabase.DeleteAsset(matPath);

            const int size = 128;
            var cube = new Cubemap(size, TextureFormat.RGB24, false);
            var rng = new System.Random(7);
            for (int face = 0; face < 6; face++)
            {
                var colors = new Color[size * size];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(0.008f, 0.01f, 0.02f);
                    double roll = rng.NextDouble();
                    if (roll > 0.996)
                    {
                        float v = 0.55f + (float)rng.NextDouble() * 0.45f;
                        colors[i] = new Color(v, v, Mathf.Lerp(v, 1f, 0.2f));
                    }
                }
                cube.SetPixels(colors, (CubemapFace)face);
            }
            cube.Apply();
            AssetDatabase.CreateAsset(cube, cubePath);

            var shader = Shader.Find("Skybox/Cubemap");
            var sky = new Material(shader);
            sky.SetTexture("_Tex", cube);
            AssetDatabase.CreateAsset(sky, matPath);
            RenderSettings.skybox = sky;
        }

        private static void CreateBloomVolume(Transform parent)
        {
            // WP2 keeps bloom off. RECON found an empty profile, and the glare bisect
            // could not be measured from batchmode. Do not add a Bloom override.
            const string path = "Assets/Materials/Volume_Bloom.asset";
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            for (int i = profile.components.Count - 1; i >= 0; i--)
            {
                var component = profile.components[i];
                if (component == null || component is UnityEngine.Rendering.Universal.Bloom)
                {
                    profile.components.RemoveAt(i);
                    if (component != null) UnityEngine.Object.DestroyImmediate(component, true);
                }
            }

            EditorUtility.SetDirty(profile);

            var go = new GameObject("GlobalVolume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void BakeNonKeyLights()
        {
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                light.shadows = LightShadows.None;
                if (light.name == "DirectionalLight_StellarKey")
                {
                    light.enabled = true;
                    light.lightmapBakeType = LightmapBakeType.Realtime;
                }
                else
                {
                    // WP2 glare step: non-key point, spot, and fill lights stay off.
                    light.enabled = false;
                    light.intensity = 0f;
                    light.lightmapBakeType = LightmapBakeType.Baked;
                }
            }
            // Lightmap bake is started by the owner from the Lighting window. Calling it here
            // locks the Editor for the rest of the build, so the scene never reaches SaveScene.
        }

        /// <summary>
        /// Invisible capture markers and the dev headset text strip.
        /// Parents: ConsoleBounds under TactileEngineeringWorkstation, WindowBounds under
        /// ForwardObservationViewportAndDeepSpaceVista (sized from PanoramicViewportGlass),
        /// ValueProbe_Ceiling under Bulkhead_CeilingSpan, ValueProbe_Floor under DeckTile_0_0,
        /// ValueProbe_Wall under Bulkhead_Port_00. Empty transforms only: no renderer, no collider.
        /// BuildLevel01 opens a new empty scene, so a second build does not append duplicates.
        /// The text strip is not the gameplay subtitle path. A human must read it in a headset
        /// and write the result into the task file. This method does not record that result.
        /// Tools/Editor does not compile into player builds, so the strip cannot be wrapped in a
        /// player-only script. It is scene content produced by this editor builder.
        /// </summary>
        private static void CreateChamber01CaptureMarkers()
        {
            var console = GameObject.Find("TactileEngineeringWorkstation");
            if (console == null)
            {
                Debug.LogError("[Level01SceneBuilder] TactileEngineeringWorkstation is missing. ConsoleBounds was not created.");
            }
            else
            {
                var consoleBounds = CreateEmptyChild(console.transform, "ConsoleBounds");
                if (TryEncapsulateConsoleRenderers(console, out Bounds box))
                {
                    consoleBounds.transform.SetPositionAndRotation(box.center, Quaternion.identity);
                    ApplyWorldSize(consoleBounds.transform, box.size);
                }
                else
                {
                    Debug.LogError("[Level01SceneBuilder] TactileEngineeringWorkstation has no renderers. ConsoleBounds has no size.");
                }
            }

            var viewport = GameObject.Find("ForwardObservationViewportAndDeepSpaceVista");
            var glass = GameObject.Find("PanoramicViewportGlass");
            if (viewport == null || glass == null)
            {
                Debug.LogError("[Level01SceneBuilder] Viewport or PanoramicViewportGlass is missing. WindowBounds was not created.");
            }
            else
            {
                var windowBounds = CreateEmptyChild(viewport.transform, "WindowBounds");
                windowBounds.transform.SetPositionAndRotation(glass.transform.position, glass.transform.rotation);
                ApplyWorldSize(windowBounds.transform, glass.transform.lossyScale);
            }

            CreateValueProbe("ValueProbe_Ceiling", "Bulkhead_CeilingSpan");
            CreateValueProbe("ValueProbe_Floor", "DeckTile_0_0");
            CreateValueProbe("ValueProbe_Wall", "Bulkhead_Port_00");
            CreateDevHeadsetTextStrip();
        }

        private static GameObject CreateEmptyChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void CreateValueProbe(string probeName, string parentName)
        {
            var parentGo = GameObject.Find(parentName);
            if (parentGo == null)
            {
                Debug.LogError("[Level01SceneBuilder] Capture probe parent missing: " + parentName);
                return;
            }

            var probe = CreateEmptyChild(parentGo.transform, probeName);
            probe.transform.localPosition = Vector3.zero;
            probe.transform.localRotation = Quaternion.identity;
            probe.transform.localScale = Vector3.one;
        }

        private static bool TryEncapsulateConsoleRenderers(GameObject console, out Bounds box)
        {
            bool any = false;
            box = new Bounds(console.transform.position, Vector3.zero);
            foreach (var renderer in console.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled) continue;
                if (renderer is ParticleSystemRenderer) continue;
                if (renderer.gameObject.name.StartsWith("BridgeChair_", StringComparison.Ordinal)) continue;
                if (!any)
                {
                    box = renderer.bounds;
                    any = true;
                }
                else
                {
                    box.Encapsulate(renderer.bounds);
                }
            }

            return any;
        }

        private static void ApplyWorldSize(Transform marker, Vector3 worldSize)
        {
            Vector3 parentScale = marker.parent != null ? marker.parent.lossyScale : Vector3.one;
            marker.localScale = new Vector3(
                DivideScale(worldSize.x, parentScale.x),
                DivideScale(worldSize.y, parentScale.y),
                DivideScale(worldSize.z, parentScale.z));
        }

        private static float DivideScale(float size, float parentScale)
        {
            if (Mathf.Abs(parentScale) < 0.000001f) return size;
            return size / parentScale;
        }

        /// <summary>
        /// One sample sentence at 1.0, 1.5, 2.0, 2.5 and 3.0 degrees.
        /// height_m = distance_m * tan(angle), at the seated eye (rig floor + 1.2 m) to the console.
        /// Facing follows section 4.3: Quaternion.LookRotation(textPos - eyePos).
        /// A human reads this in the headset and writes the smallest comfortable angle, and the
        /// angle where reading needs a head turn, into the task file. Do not invent that result.
        /// </summary>
        private static void CreateDevHeadsetTextStrip()
        {
            var existing = GameObject.Find("DevHeadsetTextStrip");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var origin = UnityEngine.Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            var console = GameObject.Find("TactileEngineeringWorkstation");
            if (origin == null || console == null)
            {
                Debug.LogError("[Level01SceneBuilder] XR rig or console is missing. DevHeadsetTextStrip was not created.");
                return;
            }

            const float seatedEyeMeters = 1.2f;
            Vector3 eye = origin.transform.position + Vector3.up * seatedEyeMeters;
            Vector3 anchor = new Vector3(console.transform.position.x, eye.y, console.transform.position.z);
            float distance = Vector3.Distance(eye, anchor);
            if (distance < 0.05f)
            {
                Debug.LogError("[Level01SceneBuilder] Seated eye and console are coincident. DevHeadsetTextStrip was not created.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null)
            {
                Debug.LogError("[Level01SceneBuilder] LiberationSans SDF is missing. DevHeadsetTextStrip was not created.");
                return;
            }

            var root = new GameObject("DevHeadsetTextStrip");
            float[] angles = { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f };
            float gap = distance * Mathf.Tan(0.35f * Mathf.Deg2Rad);
            float total = 0f;
            var heights = new float[angles.Length];
            for (int i = 0; i < angles.Length; i++)
            {
                heights[i] = distance * Mathf.Tan(angles[i] * Mathf.Deg2Rad);
                total += heights[i];
            }

            total += gap * (angles.Length - 1);
            float cursor = anchor.y - (total * 0.5f);
            for (int i = 0; i < angles.Length; i++)
            {
                float height = heights[i];
                string angleLabel = angles[i].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                var line = new GameObject("TextStrip_" + angleLabel);
                line.transform.SetParent(root.transform, false);
                Vector3 pos = new Vector3(anchor.x, cursor + (height * 0.5f), anchor.z);
                line.transform.position = pos;
                line.transform.rotation = Quaternion.LookRotation(pos - eye, Vector3.up);

                var text = line.AddComponent<TMPro.TextMeshPro>();
                text.font = font;
                text.text = angleLabel + " degrees.";
                text.fontSize = 36f;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color = Color.white;
                text.raycastTarget = false;
                text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                text.overflowMode = TMPro.TextOverflowModes.Overflow;
                text.rectTransform.sizeDelta = new Vector2(20f, 4f);
                text.ForceMeshUpdate();

                float localHeight = text.textBounds.size.y;
                if (localHeight < 0.0001f)
                {
                    Debug.LogError("[Level01SceneBuilder] Text strip mesh has no height at " + angleLabel + " degrees.");
                }
                else
                {
                    float scale = height / localHeight;
                    line.transform.localScale = Vector3.one * scale;
                    Vector3 centerOffset = line.transform.TransformVector(text.textBounds.center);
                    line.transform.position = pos - centerOffset;
                }

                cursor += height + gap;
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                    return;
                }
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            Array.Copy(scenes, newScenes, scenes.Length);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
