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
    public static class Level01SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Level01_AwakeningGate.unity";
        private const string PresetsFolder = "Assets/Puzzles/Chamber01";
        private const string MaterialsFolder = "Assets/Materials";

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
        }

        [MenuItem("Convergence/Build Level 1 — The Awakening Gate")]
        public static void BuildLevel01()
        {
            Debug.Log("[Level01SceneBuilder] Assembling futuristic Level 1 — The Awakening Gate...");

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

            // Configure Ambient Dark Cyber Void Lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.08f, 0.12f);
            RenderSettings.skybox = null;
            RenderSettings.ambientIntensity = 0.55f;
            RenderSettings.reflectionIntensity = 0.25f;

            // Key Directional Light (Soft Cool Studio Key Lighting)
            var keyLightGo = new GameObject("DirectionalLight_MainframeKey");
            keyLightGo.transform.SetParent(envRoot.transform);
            var keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.90f, 0.93f, 0.98f);
            keyLight.intensity = 0.75f;
            keyLightGo.transform.rotation = Quaternion.Euler(42f, -25f, 0f);

            // Fill / Rim Light (Deep Indigo Subtle Ambient Fill)
            var fillLightGo = new GameObject("DirectionalLight_DataFill");
            fillLightGo.transform.SetParent(envRoot.transform);
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.15f, 0.18f, 0.28f);
            fillLight.intensity = 0.35f;
            fillLightGo.transform.rotation = Quaternion.Euler(30f, 155f, 0f);

            // Mainframe Room Shell with Recessed Vertical Light Columns (14m wide, 7.5m tall, 20m deep)
            CreateMainframeRoomShell(envRoot.transform, mats);

            // Motherboard Circuit Bus-Traces etched across the floor
            CreateBusTraces(envRoot.transform, mats.BusTraceGlow);

            // Perimeter Microchip Registers & Heat Sink Fin Columns
            var memoryChipLeds = CreatePerimeterHardware(envRoot.transform, mats);

            // Ambient Data Stream Particles
            CreateDataParticles(envRoot.transform, mats.BusTraceGlow);

            // Motherboard Environment Animation Driver
            var mbEnvGo = new GameObject("MotherboardEnvironmentVisual");
            mbEnvGo.transform.SetParent(envRoot.transform);
            var mbVisual = mbEnvGo.AddComponent<MotherboardEnvironmentVisual>();
            var mbSO = new SerializedObject(mbVisual);
            mbSO.FindProperty("busTraceMaterial").objectReferenceValue = mats.BusTraceGlow;
            mbSO.FindProperty("cpuSubstrateMaterial").objectReferenceValue = mats.CpuSubstrate;
            var ledsArrayProp = mbSO.FindProperty("memoryChipLeds");
            ledsArrayProp.arraySize = memoryChipLeds.Length;
            for (int i = 0; i < memoryChipLeds.Length; i++)
            {
                ledsArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = memoryChipLeds[i];
            }
            mbSO.ApplyModifiedProperties();

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
            chamberSO.ApplyModifiedProperties();

            // --- 3D Floating & Rotating Neural Network Visualizer (Centerpiece) ---
            var neuronMachineGo = new GameObject("ClassicNeuralNetwork_3D");
            neuronMachineGo.transform.position = new Vector3(0, 1.45f, 1.80f);

            // Glowing floor projection ring directly beneath
            var floorRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floorRing.name = "FloorProjectionEmitter";
            floorRing.transform.position = new Vector3(0, 0.01f, 1.80f);
            floorRing.transform.localScale = new Vector3(2.5f, 0.01f, 2.5f);
            floorRing.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(floorRing.transform, new Vector3(0, 0.52f, 0), new Vector3(1.01f, 0.02f, 1.01f), mats.GunMetal);
            UnityEngine.Object.DestroyImmediate(floorRing.GetComponent<Collider>());

            // Rotating Container Pivot
            var rotationPivotGo = new GameObject("RotationPivot");
            rotationPivotGo.transform.SetParent(neuronMachineGo.transform, false);
            rotationPivotGo.transform.localPosition = Vector3.zero;

            // 1. Input Layer (3 Neurons: X1, X2, Bias) - Ruby Coral / Solar Amber Glow
            var inputNodes = new Transform[3];
            var inputRenderers = new Renderer[3];
            Vector3[] inputLocalOffsets = new Vector3[] {
                new Vector3(-1.10f, 0.45f, 0.0f),
                new Vector3(-1.10f, 0.0f, 0.0f),
                new Vector3(-1.10f, -0.45f, 0.0f)
            };
            string[] inputNames = { "InputNeuron_X1", "InputNeuron_X2", "InputNeuron_Bias" };

            for (int i = 0; i < 3; i++)
            {
                var nodeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeGo.name = inputNames[i];
                nodeGo.transform.SetParent(rotationPivotGo.transform, false);
                nodeGo.transform.localPosition = inputLocalOffsets[i];
                nodeGo.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
                var rend = nodeGo.GetComponent<Renderer>();
                rend.sharedMaterial = (i == 2) ? mats.GlowAmber : mats.GlowCoral;
                inputNodes[i] = nodeGo.transform;
                inputRenderers[i] = rend;
            }

            // Input Layer Floating Billboard Label
            var inLabelGo = new GameObject("InputLayer_Label");
            inLabelGo.transform.SetParent(rotationPivotGo.transform, false);
            inLabelGo.transform.localPosition = new Vector3(-1.10f, 0.75f, 0.0f);
            var inTextMesh = inLabelGo.AddComponent<TextMesh>();
            inTextMesh.text = "● SENSORS (RAD, BIO, BIAS)";
            inTextMesh.fontSize = 24;
            inTextMesh.characterSize = 0.024f;
            inTextMesh.fontStyle = FontStyle.Bold;
            inTextMesh.alignment = TextAlignment.Center;
            inTextMesh.anchor = TextAnchor.MiddleCenter;
            inTextMesh.color = new Color(0.98f, 0.65f, 0.35f);

            // 2. Hidden Layer (4 Neurons: H1, H2, H3, H4) - Deep Cyber Violet Glow
            var hiddenNodes = new Transform[4];
            var hiddenRenderers = new Renderer[4];
            Vector3[] hiddenLocalOffsets = new Vector3[] {
                new Vector3(0.0f, 0.60f, 0.0f),
                new Vector3(0.0f, 0.20f, 0.0f),
                new Vector3(0.0f, -0.20f, 0.0f),
                new Vector3(0.0f, -0.60f, 0.0f)
            };

            for (int h = 0; h < 4; h++)
            {
                var hGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hGo.name = $"HiddenNeuron_H{h + 1}";
                hGo.transform.SetParent(rotationPivotGo.transform, false);
                hGo.transform.localPosition = hiddenLocalOffsets[h];
                hGo.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
                var rend = hGo.GetComponent<Renderer>();
                rend.sharedMaterial = mats.GlowViolet;
                hiddenNodes[h] = hGo.transform;
                hiddenRenderers[h] = rend;
            }

            // Hidden Layer Floating Billboard Label
            var hidLabelGo = new GameObject("HiddenLayer_Label");
            hidLabelGo.transform.SetParent(rotationPivotGo.transform, false);
            hidLabelGo.transform.localPosition = new Vector3(0.0f, 0.90f, 0.0f);
            var hidTextMesh = hidLabelGo.AddComponent<TextMesh>();
            hidTextMesh.text = "◆ FEATURE EXTRACTION";
            hidTextMesh.fontSize = 24;
            hidTextMesh.characterSize = 0.024f;
            hidTextMesh.fontStyle = FontStyle.Bold;
            hidTextMesh.alignment = TextAlignment.Center;
            hidTextMesh.anchor = TextAnchor.MiddleCenter;
            hidTextMesh.color = new Color(0.75f, 0.50f, 1.0f);

            // 3. Output Layer (2 Neurons: Y1, Y2) - Mint Emerald Glow
            var outputNodes = new Transform[2];
            var outputRenderers = new Renderer[2];
            Vector3[] outputLocalOffsets = new Vector3[] {
                new Vector3(1.10f, 0.30f, 0.0f),
                new Vector3(1.10f, -0.30f, 0.0f)
            };
            string[] outputNames = { "OutputNeuron_Y1", "OutputNeuron_Y2" };

            for (int o = 0; o < 2; o++)
            {
                var oGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                oGo.name = outputNames[o];
                oGo.transform.SetParent(rotationPivotGo.transform, false);
                oGo.transform.localPosition = outputLocalOffsets[o];
                oGo.transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
                var rend = oGo.GetComponent<Renderer>();
                rend.sharedMaterial = mats.GlowEmerald;
                outputNodes[o] = oGo.transform;
                outputRenderers[o] = rend;
            }

            // Output Layer Floating Billboard Label
            var outLabelGo = new GameObject("OutputLayer_Label");
            outLabelGo.transform.SetParent(rotationPivotGo.transform, false);
            outLabelGo.transform.localPosition = new Vector3(1.10f, 0.60f, 0.0f);
            var outTextMesh = outLabelGo.AddComponent<TextMesh>();
            outTextMesh.text = "▲ QUARANTINE SWITCH";
            outTextMesh.fontSize = 24;
            outTextMesh.characterSize = 0.024f;
            outTextMesh.fontStyle = FontStyle.Bold;
            outTextMesh.alignment = TextAlignment.Center;
            outTextMesh.anchor = TextAnchor.MiddleCenter;
            outTextMesh.color = new Color(0.25f, 0.95f, 0.60f);

            // 4. Synaptic Axons (20 LineRenderers connecting layers)
            var inToHidLines = new LineRenderer[12];
            int synIdx = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int h = 0; h < 4; h++)
                {
                    var synGo = new GameObject($"Synapse_I{i + 1}_H{h + 1}");
                    synGo.transform.SetParent(rotationPivotGo.transform, false);
                    var lr = synGo.AddComponent<LineRenderer>();
                    lr.sharedMaterial = mats.AxonPositive;
                    lr.useWorldSpace = true;
                    lr.positionCount = 2;
                    lr.startWidth = 0.010f;
                    lr.endWidth = 0.012f;
                    lr.SetPosition(0, inputNodes[i].position);
                    lr.SetPosition(1, hiddenNodes[h].position);
                    inToHidLines[synIdx++] = lr;
                }
            }

            var hidToOutLines = new LineRenderer[8];
            int hidOutIdx = 0;
            for (int h = 0; h < 4; h++)
            {
                for (int o = 0; o < 2; o++)
                {
                    var synGo = new GameObject($"Synapse_H{h + 1}_O{o + 1}");
                    synGo.transform.SetParent(rotationPivotGo.transform, false);
                    var lr = synGo.AddComponent<LineRenderer>();
                    lr.sharedMaterial = mats.AxonPositive;
                    lr.useWorldSpace = true;
                    lr.positionCount = 2;
                    lr.startWidth = 0.010f;
                    lr.endWidth = 0.012f;
                    lr.SetPosition(0, hiddenNodes[h].position);
                    lr.SetPosition(1, outputNodes[o].position);
                    hidToOutLines[hidOutIdx++] = lr;
                }
            }

            // Output Laser Beam to Gate
            var beamGo = new GameObject("OutputGateBeam");
            beamGo.transform.SetParent(neuronMachineGo.transform, false);
            var beamLr = beamGo.AddComponent<LineRenderer>();
            beamLr.sharedMaterial = mats.CorePlasma;
            beamLr.useWorldSpace = true;
            beamLr.positionCount = 2;
            beamLr.SetPosition(0, new Vector3(0, 1.45f, 1.80f));
            beamLr.SetPosition(1, new Vector3(0, 3.5f, 8.5f));
            beamLr.startWidth = 0.04f;
            beamLr.endWidth = 0.08f;
            beamLr.enabled = false;

            // Neural Network Visualizer Component
            var visualizer = neuronMachineGo.AddComponent<ClassicNeuralNetwork3DVisualizer>();
            var visSO = new SerializedObject(visualizer);
            visSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            visSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            visSO.FindProperty("networkPivot").objectReferenceValue = rotationPivotGo.transform;
            visSO.FindProperty("autoRotate").boolValue = true;
            visSO.FindProperty("rotationSpeed").floatValue = 8.0f;
            visSO.FindProperty("levitationAmplitude").floatValue = 0.035f;
            visSO.FindProperty("levitationFrequency").floatValue = 1.2f;

            var inNodesProp = visSO.FindProperty("inputNodes");
            inNodesProp.arraySize = 3;
            for (int i = 0; i < 3; i++) inNodesProp.GetArrayElementAtIndex(i).objectReferenceValue = inputNodes[i];

            var hidNodesProp = visSO.FindProperty("hiddenNodes");
            hidNodesProp.arraySize = 4;
            for (int h = 0; h < 4; h++) hidNodesProp.GetArrayElementAtIndex(h).objectReferenceValue = hiddenNodes[h];

            var outNodesProp = visSO.FindProperty("outputNodes");
            outNodesProp.arraySize = 2;
            for (int o = 0; o < 2; o++) outNodesProp.GetArrayElementAtIndex(o).objectReferenceValue = outputNodes[o];

            var inRendProp = visSO.FindProperty("inputRenderers");
            inRendProp.arraySize = 3;
            for (int i = 0; i < 3; i++) inRendProp.GetArrayElementAtIndex(i).objectReferenceValue = inputRenderers[i];

            var hidRendProp = visSO.FindProperty("hiddenRenderers");
            hidRendProp.arraySize = 4;
            for (int h = 0; h < 4; h++) hidRendProp.GetArrayElementAtIndex(h).objectReferenceValue = hiddenRenderers[h];

            var outRendProp = visSO.FindProperty("outputRenderers");
            outRendProp.arraySize = 2;
            for (int o = 0; o < 2; o++) outRendProp.GetArrayElementAtIndex(o).objectReferenceValue = outputRenderers[o];

            var inToHidProp = visSO.FindProperty("inputToHiddenSynapses");
            inToHidProp.arraySize = 12;
            for (int i = 0; i < 12; i++) inToHidProp.GetArrayElementAtIndex(i).objectReferenceValue = inToHidLines[i];

            var hidToOutProp = visSO.FindProperty("hiddenToOutputSynapses");
            hidToOutProp.arraySize = 8;
            for (int h = 0; h < 8; h++) hidToOutProp.GetArrayElementAtIndex(h).objectReferenceValue = hidToOutLines[h];

            visSO.FindProperty("outputToGateBeam").objectReferenceValue = beamLr;
            visSO.FindProperty("inputLayerLabel").objectReferenceValue = inLabelGo.transform;
            visSO.FindProperty("hiddenLayerLabel").objectReferenceValue = hidLabelGo.transform;
            visSO.FindProperty("outputLayerLabel").objectReferenceValue = outLabelGo.transform;
            visSO.ApplyModifiedProperties();

            // --- Ergonomic Tactile Engineering Workstation (VR Ready Height) ---
            var consoleGo = new GameObject("TactileEngineeringWorkstation");
            consoleGo.transform.position = new Vector3(0, 0, -1.60f);

            // Sleek Standing VR Console Desk (Top surface at y = 0.85m)
            var consoleTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleTable.name = "ConsoleTable";
            consoleTable.transform.SetParent(consoleGo.transform, false);
            consoleTable.transform.localPosition = new Vector3(0, 0.425f, 0);
            consoleTable.transform.localScale = new Vector3(1.40f, 0.85f, 0.45f);
            consoleTable.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(consoleTable.transform, new Vector3(0, 0.51f, 0.48f), new Vector3(0.96f, 0.02f, 0.02f), mats.GunMetal);

            var machineVisual = consoleGo.AddComponent<NeuronMachineVisual>();

            // --- Kinetic Power Sliders: W1 (Radiation Sensitivity) & W2 (Bio Sensitivity) ---
            var sliderW1Track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sliderW1Track.name = "SliderTrack_W1";
            sliderW1Track.transform.SetParent(consoleGo.transform, false);
            sliderW1Track.transform.localPosition = new Vector3(-0.35f, 0.86f, 0.02f);
            sliderW1Track.transform.localScale = new Vector3(0.08f, 0.02f, 0.32f);
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
            s1SO.FindProperty("sensorName").stringValue = "Radiation Sentry Sensor";
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
            w1Label.transform.localPosition = new Vector3(-0.35f, 0.92f, -0.16f);
            w1Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var w1Txt = w1Label.AddComponent<TextMesh>();
            w1Txt.text = "RAD SENSITIVITY (W1)";
            w1Txt.fontSize = 17;
            w1Txt.characterSize = 0.011f;
            w1Txt.fontStyle = FontStyle.Bold;
            w1Txt.alignment = TextAlignment.Center;
            w1Txt.anchor = TextAnchor.MiddleCenter;
            w1Txt.color = new Color(0.35f, 0.85f, 1.0f);

            var sliderW2Track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sliderW2Track.name = "SliderTrack_W2";
            sliderW2Track.transform.SetParent(consoleGo.transform, false);
            sliderW2Track.transform.localPosition = new Vector3(0.35f, 0.86f, 0.02f);
            sliderW2Track.transform.localScale = new Vector3(0.08f, 0.02f, 0.32f);
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
            s2SO.FindProperty("sensorName").stringValue = "Biohazard Sentry Sensor";
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
            w2Label.transform.localPosition = new Vector3(0.35f, 0.92f, -0.16f);
            w2Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var w2Txt = w2Label.AddComponent<TextMesh>();
            w2Txt.text = "BIO SENSITIVITY (W2)";
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
            biasGo.transform.localPosition = new Vector3(0.0f, 0.88f, 0.10f);
            biasGo.transform.localScale = new Vector3(0.14f, 0.04f, 0.14f);
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
            biasLabel.transform.localPosition = new Vector3(0.0f, 0.92f, 0.19f);
            biasLabel.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var biasTxt = biasLabel.AddComponent<TextMesh>();
            biasTxt.text = "THRESHOLD SQUELCH (b)";
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
            socketGo.transform.localPosition = new Vector3(0.0f, 0.87f, -0.06f);
            socketGo.transform.localScale = new Vector3(0.11f, 0.03f, 0.11f);
            socketGo.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
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
            linearCry.transform.localScale = new Vector3(0.06f, 0.14f, 0.06f);
            linearCry.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(linearCry.GetComponent<Collider>());

            var reluCry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reluCry.name = "Crystal_ReLU";
            reluCry.transform.SetParent(crystalRootGo.transform, false);
            reluCry.transform.localPosition = Vector3.zero;
            reluCry.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            reluCry.transform.localRotation = Quaternion.Euler(25f, 45f, 0);
            reluCry.GetComponent<Renderer>().sharedMaterial = mats.GlowAmber;
            UnityEngine.Object.DestroyImmediate(reluCry.GetComponent<Collider>());

            var stepCry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepCry.name = "Crystal_Step";
            stepCry.transform.SetParent(crystalRootGo.transform, false);
            stepCry.transform.localPosition = Vector3.zero;
            stepCry.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            stepCry.transform.localRotation = Quaternion.Euler(0, 45f, 45f);
            stepCry.GetComponent<Renderer>().sharedMaterial = mats.GlowEmerald;
            UnityEngine.Object.DestroyImmediate(stepCry.GetComponent<Collider>());

            var sigCry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sigCry.name = "Crystal_Sigmoid";
            sigCry.transform.SetParent(crystalRootGo.transform, false);
            sigCry.transform.localPosition = Vector3.zero;
            sigCry.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            sigCry.GetComponent<Renderer>().sharedMaterial = mats.GlowViolet;
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
            socketLabel.transform.localPosition = new Vector3(0.0f, 0.92f, -0.15f);
            socketLabel.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var socketTxt = socketLabel.AddComponent<TextMesh>();
            socketTxt.text = "DECISION CRYSTAL";
            socketTxt.fontSize = 18;
            socketTxt.characterSize = 0.012f;
            socketTxt.fontStyle = FontStyle.Bold;
            socketTxt.alignment = TextAlignment.Center;
            socketTxt.anchor = TextAnchor.MiddleCenter;
            socketTxt.color = new Color(0.20f, 0.90f, 0.55f);

            // Master Clock Cycle / Ignition Lever
            var leverBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverBase.name = "ClockPulseLever_Base";
            leverBase.transform.SetParent(consoleGo.transform, false);
            leverBase.transform.localPosition = new Vector3(0.50f, 0.88f, 0.0f);
            leverBase.transform.localScale = new Vector3(0.10f, 0.04f, 0.14f);
            leverBase.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;

            var leverLight = leverBase.AddComponent<Light>();
            leverLight.type = LightType.Point;
            leverLight.range = 1.0f;
            leverLight.intensity = 0.8f;
            leverLight.color = new Color(0.98f, 0.70f, 0.15f);

            var leverLabel = new GameObject("Lever_Label");
            leverLabel.transform.SetParent(consoleGo.transform, false);
            leverLabel.transform.localPosition = new Vector3(0.50f, 0.92f, -0.08f);
            leverLabel.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var leverTxt = leverLabel.AddComponent<TextMesh>();
            leverTxt.text = "CLOCK PULSE";
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

            var leverInteractor = leverBase.AddComponent<ClockPulseLeverInteractor>();
            var leverSO = new SerializedObject(leverInteractor);
            leverSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            leverSO.FindProperty("leverArm").objectReferenceValue = leverArm.transform;
            leverSO.ApplyModifiedProperties();

            // Conduits / Patch Cables
            var cable1Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable1Go.name = "NeuralCable_1";
            cable1Go.transform.SetParent(consoleGo.transform, false);
            cable1Go.transform.localPosition = new Vector3(-0.50f, 0.72f, 0.12f);
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
            c1Label.transform.localPosition = new Vector3(-0.50f, 0.92f, 0.18f);
            c1Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var c1Txt = c1Label.AddComponent<TextMesh>();
            c1Txt.text = "RAD SENSOR (X1)";
            c1Txt.fontSize = 15;
            c1Txt.characterSize = 0.010f;
            c1Txt.fontStyle = FontStyle.Bold;
            c1Txt.alignment = TextAlignment.Center;
            c1Txt.anchor = TextAnchor.MiddleCenter;
            c1Txt.color = new Color(0.98f, 0.65f, 0.35f);

            var cable2Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable2Go.name = "NeuralCable_2";
            cable2Go.transform.SetParent(consoleGo.transform, false);
            cable2Go.transform.localPosition = new Vector3(0.50f, 0.72f, 0.12f);
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
            c2Label.transform.localPosition = new Vector3(0.50f, 0.92f, 0.18f);
            c2Label.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            var c2Txt = c2Label.AddComponent<TextMesh>();
            c2Txt.text = "BIO SENSOR (X2)";
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

            // --- In-World Educational Station: Neuron Architecture Breakdown (Left) ---
            var pedagogyGo = new GameObject("NeuronPedagogyStation");
            pedagogyGo.transform.position = new Vector3(-2.70f, 1.85f, 1.40f);
            pedagogyGo.transform.rotation = Quaternion.Euler(0, 32f, 0);

            var pedBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedBoard.name = "PedagogyFrame";
            pedBoard.transform.SetParent(pedagogyGo.transform, false);
            pedBoard.transform.localPosition = Vector3.zero;
            pedBoard.transform.localScale = new Vector3(2.10f, 1.45f, 0.02f);
            pedBoard.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;
            CreatePlatformTrim(pedBoard.transform, new Vector3(0, 0, 0.52f), new Vector3(1.01f, 1.01f, 0.02f), mats.GunMetal);

            var pedLight = pedagogyGo.AddComponent<Light>();
            pedLight.type = LightType.Point;
            pedLight.range = 3.0f;
            pedLight.intensity = 0.30f;
            pedLight.color = new Color(0.20f, 0.65f, 0.95f);

            var pedHeaderGo = new GameObject("Pedagogy_Header");
            pedHeaderGo.transform.SetParent(pedagogyGo.transform, false);
            pedHeaderGo.transform.localPosition = new Vector3(-0.98f, 0.56f, -0.04f);
            var pedHeaderTxt = pedHeaderGo.AddComponent<TextMesh>();
            pedHeaderTxt.text = "HAZARD CLASSIFIER NEURON";
            pedHeaderTxt.fontSize = 28;
            pedHeaderTxt.characterSize = 0.026f;
            pedHeaderTxt.fontStyle = FontStyle.Bold;
            pedHeaderTxt.color = new Color(0.35f, 0.85f, 1.0f);

            var pedFormulaGo = new GameObject("Pedagogy_Formula");
            pedFormulaGo.transform.SetParent(pedagogyGo.transform, false);
            pedFormulaGo.transform.localPosition = new Vector3(-0.98f, 0.36f, -0.04f);
            var pedFormulaTxt = pedFormulaGo.AddComponent<TextMesh>();
            pedFormulaTxt.text = "1. SENSOR SUMMATION (Total Hazard Energy Σ):\n   z = (w1 × Radiation) + (w2 × BioLeak) + Bias\n   z = (0.0 × x1) + (0.0 × x2) + (-1.0)\n\n2. ACTIVATION GATE (Quarantine Switch):\n   y = Linear(z)  [Need Step Crystal]";
            pedFormulaTxt.fontSize = 22;
            pedFormulaTxt.characterSize = 0.023f;
            pedFormulaTxt.color = Color.white;

            var pedExplGo = new GameObject("Pedagogy_Explanation");
            pedExplGo.transform.SetParent(pedagogyGo.transform, false);
            pedExplGo.transform.localPosition = new Vector3(-0.98f, -0.10f, -0.04f);
            var pedExplTxt = pedExplGo.AddComponent<TextMesh>();
            pedExplTxt.text = "• SENSORS (x1, x2)  : Radiation & Bio-Leak line data.\n• WEIGHTS (w1, w2) : Sensitivity knobs for each sensor.\n• BIAS (b)         : Noise filter (-0.5). Prevents false alarms.\n• OUTPUT (y)       : Final Lockdown Switch (0 = Open, 1 = Seal).";
            pedExplTxt.fontSize = 19;
            pedExplTxt.characterSize = 0.021f;
            pedExplTxt.color = new Color(0.88f, 0.92f, 1.0f);

            var pedBadgeGo = new GameObject("Pedagogy_StatusBadge");
            pedBadgeGo.transform.SetParent(pedagogyGo.transform, false);
            pedBadgeGo.transform.localPosition = new Vector3(-0.98f, -0.52f, -0.04f);
            var pedBadgeTxt = pedBadgeGo.AddComponent<TextMesh>();
            pedBadgeTxt.text = "CALIBRATION IN PROGRESS...";
            pedBadgeTxt.fontSize = 22;
            pedBadgeTxt.characterSize = 0.023f;
            pedBadgeTxt.fontStyle = FontStyle.Bold;
            pedBadgeTxt.color = new Color(0.98f, 0.75f, 0.15f);

            var pedagogyVisual = pedagogyGo.AddComponent<NeuronPedagogyHologramVisual>();
            var pedSO = new SerializedObject(pedagogyVisual);
            pedSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            pedSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            pedSO.FindProperty("headerTextMesh").objectReferenceValue = pedHeaderTxt;
            pedSO.FindProperty("formulaTextMesh").objectReferenceValue = pedFormulaTxt;
            pedSO.FindProperty("explanationTextMesh").objectReferenceValue = pedExplTxt;
            pedSO.FindProperty("statusBadgeTextMesh").objectReferenceValue = pedBadgeTxt;
            pedSO.FindProperty("stationLight").objectReferenceValue = pedLight;
            pedSO.FindProperty("panelRenderer").objectReferenceValue = pedBoard.GetComponent<Renderer>();
            pedSO.ApplyModifiedProperties();

            // --- In-World Engineer's Field Manual Tablet ---
            var tabletGo = new GameObject("EngineerFieldManualTablet");
            tabletGo.transform.position = new Vector3(-0.52f, 0.66f, -1.60f);
            tabletGo.transform.rotation = Quaternion.Euler(30f, 25f, 0);

            var tabletBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabletBody.name = "TabletFrame";
            tabletBody.transform.SetParent(tabletGo.transform, false);
            tabletBody.transform.localPosition = Vector3.zero;
            tabletBody.transform.localScale = new Vector3(0.32f, 0.24f, 0.02f);
            tabletBody.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var manualVisual = tabletGo.AddComponent<EngineerFieldManualVisual>();
            var manSO = new SerializedObject(manualVisual);
            manSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            manSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            manSO.FindProperty("tabletRoot").objectReferenceValue = tabletGo.transform;
            manSO.FindProperty("showScreenOverlay").boolValue = false;
            manSO.ApplyModifiedProperties();

            // --- Eye-Level 3D Holographic Diagnostic Matrix Display (Right) ---
            var hologramGo = new GameObject("DiagnosticHologramMatrix");
            hologramGo.transform.position = new Vector3(2.70f, 1.85f, 1.40f);
            hologramGo.transform.rotation = Quaternion.Euler(0, -32f, 0);

            var holoBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            holoBoard.name = "HologramPanel";
            holoBoard.transform.SetParent(hologramGo.transform, false);
            holoBoard.transform.localPosition = Vector3.zero;
            holoBoard.transform.localScale = new Vector3(2.10f, 1.45f, 0.02f);
            holoBoard.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;
            CreatePlatformTrim(holoBoard.transform, new Vector3(0, 0, 0.52f), new Vector3(1.01f, 1.01f, 0.02f), mats.GunMetal);

            var holoLight = hologramGo.AddComponent<Light>();
            holoLight.type = LightType.Point;
            holoLight.range = 3.0f;
            holoLight.intensity = 0.30f;
            holoLight.color = new Color(0.20f, 0.65f, 0.95f);

            var holoHeaderGo = new GameObject("Diagnostic_Header");
            holoHeaderGo.transform.SetParent(hologramGo.transform, false);
            holoHeaderGo.transform.localPosition = new Vector3(-0.95f, 0.55f, -0.04f);
            var holoHeaderTxt = holoHeaderGo.AddComponent<TextMesh>();
            holoHeaderTxt.text = "HAZARD CLASSIFIER — SENSOR TELEMETRY MATRIX";
            holoHeaderTxt.fontSize = 24;
            holoHeaderTxt.characterSize = 0.023f;
            holoHeaderTxt.fontStyle = FontStyle.Bold;
            holoHeaderTxt.color = new Color(0.25f, 0.80f, 1.0f);

            var holoRows = new TextMesh[4];
            string[] initialRows = new string[] {
                "[CLEAN ROOM  (0,0)] Target: SAFE(0)  | z = -1.00 -> y = -1.0 [FAIL]",
                "[BIO-LEAK    (0,1)] Target: ALARM(1) | z = -1.00 -> y = -1.0 [FAIL]",
                "[RADIATION   (1,0)] Target: ALARM(1) | z = -1.00 -> y = -1.0 [FAIL]",
                "[DUAL HAZARD (1,1)] Target: ALARM(1) | z = -1.00 -> y = -1.0 [FAIL]"
            };
            for (int r = 0; r < 4; r++)
            {
                var rowGo = new GameObject($"Diagnostic_Row_{r + 1}");
                rowGo.transform.SetParent(hologramGo.transform, false);
                rowGo.transform.localPosition = new Vector3(-0.95f, 0.34f - (r * 0.18f), -0.04f);
                var rowTxt = rowGo.AddComponent<TextMesh>();
                rowTxt.text = initialRows[r];
                rowTxt.fontSize = 20;
                rowTxt.characterSize = 0.022f;
                rowTxt.color = new Color(0.95f, 0.35f, 0.35f);
                holoRows[r] = rowTxt;
            }

            var holoHintGo = new GameObject("Diagnostic_Hint");
            holoHintGo.transform.SetParent(hologramGo.transform, false);
            holoHintGo.transform.localPosition = new Vector3(-0.95f, -0.52f, -0.04f);
            var holoHintTxt = holoHintGo.AddComponent<TextMesh>();
            holoHintTxt.text = "Rotate sensitivity dials & insert Step Crystal to calibrate all 4 hazard scenarios.";
            holoHintTxt.fontSize = 18;
            holoHintTxt.characterSize = 0.020f;
            holoHintTxt.color = new Color(0.98f, 0.70f, 0.15f);

            var hologramVisual = hologramGo.AddComponent<DiagnosticHologramVisual>();
            var holoSO = new SerializedObject(hologramVisual);
            holoSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            holoSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            holoSO.FindProperty("displayRoot").objectReferenceValue = hologramGo.transform;
            holoSO.FindProperty("hologramBacklight").objectReferenceValue = holoLight;
            holoSO.FindProperty("displayScreenRenderer").objectReferenceValue = holoBoard.GetComponent<Renderer>();
            holoSO.FindProperty("headerTextMesh").objectReferenceValue = holoHeaderTxt;
            var holoRowsProp = holoSO.FindProperty("rowTextMeshes");
            holoRowsProp.arraySize = 4;
            for (int r = 0; r < 4; r++) holoRowsProp.GetArrayElementAtIndex(r).objectReferenceValue = holoRows[r];
            holoSO.FindProperty("diagnosticFeedbackTextMesh").objectReferenceValue = holoHintTxt;
            holoSO.FindProperty("showScreenOverlay").boolValue = false;
            holoSO.ApplyModifiedProperties();

            // --- Planetary AI Consciousness Holosphere (SYNAPSE-GPT Core) ---
            CreatePlanetaryAICoreHologram(envRoot.transform, mats, chamberController, neuralState);

            // --- Automated Kinetic Defense Sentry & Hazard Turret ---
            CreateDefenseSentryTurret(envRoot.transform, mats, chamberController, neuralState);

            // --- The Awakening Blast Doors & Energy Portal Gateway ---
            var gateGo = new GameObject("AwakeningBlastDoors");
            gateGo.transform.position = new Vector3(0, 0, 8.50f);

            var leftPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPortal.name = "BlastDoor_LeftWing";
            leftPortal.transform.SetParent(gateGo.transform, false);
            leftPortal.transform.localPosition = new Vector3(-2.8f, 3.5f, 0);
            leftPortal.transform.localScale = new Vector3(3.0f, 7.0f, 0.6f);
            leftPortal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(leftPortal.transform, new Vector3(0.48f, 0, 0.52f), new Vector3(0.04f, 0.95f, 0.02f), mats.GunMetal);

            var rightPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPortal.name = "BlastDoor_RightWing";
            rightPortal.transform.SetParent(gateGo.transform, false);
            rightPortal.transform.localPosition = new Vector3(2.8f, 3.5f, 0);
            rightPortal.transform.localScale = new Vector3(3.0f, 7.0f, 0.6f);
            rightPortal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(rightPortal.transform, new Vector3(-0.48f, 0, 0.52f), new Vector3(0.04f, 0.95f, 0.02f), mats.GunMetal);

            var gateCurtainGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateCurtainGo.name = "PortalEnergyCurtain";
            gateCurtainGo.transform.SetParent(gateGo.transform, false);
            gateCurtainGo.transform.localPosition = new Vector3(0, 3.5f, 0);
            gateCurtainGo.transform.localScale = new Vector3(2.6f, 6.8f, 0.04f);
            gateCurtainGo.GetComponent<Renderer>().sharedMaterial = mats.PortalCurtain;
            UnityEngine.Object.DestroyImmediate(gateCurtainGo.GetComponent<Collider>());

            var gateLightGo = new GameObject("GateAuraLight");
            gateLightGo.transform.SetParent(gateGo.transform, false);
            gateLightGo.transform.localPosition = new Vector3(0, 3.5f, -0.5f);
            var gateLight = gateLightGo.AddComponent<Light>();
            gateLight.type = LightType.Point;
            gateLight.range = 7.0f;
            gateLight.intensity = 0.35f;
            gateLight.color = new Color(0.90f, 0.45f, 0.12f);

            var gatewayController = gateGo.AddComponent<GatewayController>();
            var gateVisual = gateGo.AddComponent<AwakeningGateVisual>();

            var gvSO = new SerializedObject(gateVisual);
            gvSO.FindProperty("gatewayController").objectReferenceValue = gatewayController;
            gvSO.FindProperty("leftPortalWing").objectReferenceValue = leftPortal.transform;
            gvSO.FindProperty("rightPortalWing").objectReferenceValue = rightPortal.transform;
            gvSO.FindProperty("gateAuraLight").objectReferenceValue = gateLight;
            gvSO.ApplyModifiedProperties();

            var gcSO = new SerializedObject(gatewayController);
            gcSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            gcSO.ApplyModifiedProperties();

            resetterSO.Update();
            resetterSO.FindProperty("gatewayController").objectReferenceValue = gatewayController;
            resetterSO.ApplyModifiedProperties();

            // --- Portal-Style Stasis Awakening Pod (Docked behind player) ---
            var podRoot = new GameObject("StasisAwakeningPod");
            podRoot.transform.position = new Vector3(0, 0, -4.8f);

            var podShell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            podShell.name = "StasisPod_Frame";
            podShell.transform.SetParent(podRoot.transform);
            podShell.transform.localPosition = new Vector3(0, 1.6f, 0);
            podShell.transform.localScale = new Vector3(2.4f, 3.2f, 1.8f);
            podShell.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var podInner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            podInner.name = "StasisPod_Interior";
            podInner.transform.SetParent(podRoot.transform);
            podInner.transform.localPosition = new Vector3(0, 1.6f, 0);
            podInner.transform.localScale = new Vector3(2.0f, 3.0f, 1.4f);
            podInner.GetComponent<Renderer>().sharedMaterial = mats.CpuSubstrate;

            // Stasis Glass Sliding Doors (Retracted to sides so camera is never blocked)
            var doorLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeft.name = "StasisDoor_Left";
            doorLeft.transform.SetParent(podRoot.transform);
            doorLeft.transform.localPosition = new Vector3(-1.4f, 1.6f, 0.92f);
            doorLeft.transform.localScale = new Vector3(1.0f, 2.8f, 0.06f);
            doorLeft.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;

            var doorRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorRight.name = "StasisDoor_Right";
            doorRight.transform.SetParent(podRoot.transform);
            doorRight.transform.localPosition = new Vector3(1.4f, 1.6f, 0.92f);
            doorRight.transform.localScale = new Vector3(1.0f, 2.8f, 0.06f);
            doorRight.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;

            var podLightGo = new GameObject("StasisPod_AwakeningLight");
            podLightGo.transform.SetParent(podRoot.transform);
            podLightGo.transform.localPosition = new Vector3(0, 2.6f, 0);
            var podLight = podLightGo.AddComponent<Light>();
            podLight.type = LightType.Point;
            podLight.range = 4.0f;
            podLight.intensity = 0.5f;
            podLight.color = new Color(0.20f, 0.65f, 0.95f);

            // --- Facility Signboard ---
            var signboardGo = new GameObject("Chamber01_WallSignboard");
            signboardGo.transform.position = new Vector3(-6.85f, 3.5f, 2.0f);
            signboardGo.transform.rotation = Quaternion.Euler(0, 90f, 0);

            var signBacking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBacking.name = "SignboardBacking";
            signBacking.transform.SetParent(signboardGo.transform);
            signBacking.transform.localPosition = Vector3.zero;
            signBacking.transform.localScale = new Vector3(2.4f, 1.3f, 0.04f);
            signBacking.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(signBacking.transform, new Vector3(0, 0, 0.52f), new Vector3(1.01f, 1.01f, 0.02f), mats.GunMetal);

            var signNumGo = new GameObject("Sign_ChamberNumber");
            signNumGo.transform.SetParent(signboardGo.transform);
            signNumGo.transform.localPosition = new Vector3(0, 0.32f, -0.04f);
            var numTextMesh = signNumGo.AddComponent<TextMesh>();
            numTextMesh.text = "CHAMBER 01";
            numTextMesh.fontSize = 26;
            numTextMesh.characterSize = 0.026f;
            numTextMesh.fontStyle = FontStyle.Bold;
            numTextMesh.alignment = TextAlignment.Center;
            numTextMesh.anchor = TextAnchor.MiddleCenter;
            numTextMesh.color = new Color(0.25f, 0.80f, 1.0f);

            var signSubGo = new GameObject("Sign_Subtitle");
            signSubGo.transform.SetParent(signboardGo.transform);
            signSubGo.transform.localPosition = new Vector3(0, -0.05f, -0.04f);
            var subTextMesh = signSubGo.AddComponent<TextMesh>();
            subTextMesh.text = "SYNAPSE-GPT PERCEPTION GATE // SECTOR 01 OR-CLASSIFIER";
            subTextMesh.fontSize = 17;
            subTextMesh.characterSize = 0.019f;
            subTextMesh.alignment = TextAlignment.Center;
            subTextMesh.anchor = TextAnchor.MiddleCenter;
            subTextMesh.color = Color.white;

            var signLeds = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                var led = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                led.name = $"TargetLED_{i + 1}";
                led.transform.SetParent(signboardGo.transform);
                led.transform.localPosition = new Vector3(-0.65f + (i * 0.44f), -0.38f, -0.04f);
                led.transform.localScale = new Vector3(0.10f, 0.02f, 0.10f);
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
            lr1.SetPosition(0, new Vector3(0, 0.02f, -4.4f));
            lr1.SetPosition(1, new Vector3(0, 0.02f, -3.0f));
            lr1.SetPosition(2, new Vector3(0, 0.02f, -1.60f));
            lr1.startWidth = 0.012f;
            lr1.endWidth = 0.012f;
            traceLines[0] = lr1;

            var line2Go = new GameObject("Trace_ConsoleToGate");
            line2Go.transform.SetParent(tracesGo.transform);
            var lr2 = line2Go.AddComponent<LineRenderer>();
            lr2.sharedMaterial = mats.BusTraceGlow;
            lr2.positionCount = 3;
            lr2.SetPosition(0, new Vector3(0, 0.02f, -1.35f));
            lr2.SetPosition(1, new Vector3(0, 0.02f, 3.5f));
            lr2.SetPosition(2, new Vector3(0, 0.02f, 8.4f));
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
            subtitleHoloGo.transform.position = new Vector3(0, 2.30f, -0.60f);
            subtitleHoloGo.transform.rotation = Quaternion.Euler(12f, 0, 0);

            var subText = subtitleHoloGo.AddComponent<TextMesh>();
            subText.text = "";
            subText.fontSize = 24;
            subText.characterSize = 0.015f;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAlignment.Center;
            subText.anchor = TextAnchor.MiddleCenter;
            subText.color = new Color(0.2f, 0.95f, 1.0f, 1.0f);

            // --- Facility AI Voice Announcer ---
            var announcer = controllersGo.AddComponent<FacilityAIVoiceAnnouncer>();
            var annSO = new SerializedObject(announcer);
            annSO.FindProperty("onboardingController").objectReferenceValue = onboardingController;
            annSO.FindProperty("worldSubtitleTextMesh").objectReferenceValue = subText;
            annSO.ApplyModifiedProperties();

            // --- XR Origin Rig & Camera (VR & Desktop Ready) ---
            var xrOriginGo = new GameObject("XR Origin (VR Rig)");
            xrOriginGo.transform.position = new Vector3(0, 0, -3.20f);

            var cameraOffsetGo = new GameObject("Camera Offset");
            cameraOffsetGo.transform.SetParent(xrOriginGo.transform, false);

            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraOffsetGo.transform, false);
            cameraGo.transform.localPosition = new Vector3(0, 1.70f, 0);
            cameraGo.transform.localRotation = Quaternion.Euler(4.5f, 0, 0);
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            cam.fieldOfView = 80f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();

            // Controller Hand anchors for VR
            var leftHandGo = new GameObject("Left Hand Controller");
            leftHandGo.transform.SetParent(cameraOffsetGo.transform, false);
            leftHandGo.transform.localPosition = new Vector3(-0.25f, 1.25f, 0.35f);

            var rightHandGo = new GameObject("Right Hand Controller");
            rightHandGo.transform.SetParent(cameraOffsetGo.transform, false);
            rightHandGo.transform.localPosition = new Vector3(0.25f, 1.25f, 0.35f);

            // Astronaut Engineer HUD
            var hudComponent = cameraGo.AddComponent<SciFiEngineerHUD>();
            var hudSO = new SerializedObject(hudComponent);
            hudSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            hudSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            hudSO.FindProperty("onboardingController").objectReferenceValue = onboardingController;
            hudSO.ApplyModifiedProperties();

            // Desktop Input Fallback
            var desktopFallback = cameraGo.AddComponent<DesktopInputFallback>();
            var dfSO = new SerializedObject(desktopFallback);
            dfSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            dfSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            dfSO.FindProperty("levelResetter").objectReferenceValue = levelResetter;
            dfSO.FindProperty("activationSocket").objectReferenceValue = socketInteractor;
            dfSO.FindProperty("showHUD").boolValue = false;
            dfSO.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register in Build Settings
            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Level01SceneBuilder] Level 1 generated and saved successfully to '{ScenePath}'.");
        }

        private static void CreateMainframeRoomShell(Transform parent, LevelMaterials mats)
        {
            var roomRoot = new GameObject("MainframeBoundingBoxShell");
            roomRoot.transform.SetParent(parent);

            // Floor Silicon / Dark Obsidian Platform
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "MainframeFloor_Platform";
            floor.transform.SetParent(roomRoot.transform);
            floor.transform.position = new Vector3(0, -0.25f, 4.5f);
            floor.transform.localScale = new Vector3(14.0f, 0.5f, 20.0f);
            floor.GetComponent<Renderer>().sharedMaterial = mats.CpuSubstrate;

            // Floor Outer Trim (Subtle dark titanium trim)
            CreatePlatformTrim(floor.transform, new Vector3(0, 0.51f, 0.49f), new Vector3(0.98f, 0.015f, 0.005f), mats.GunMetal);
            CreatePlatformTrim(floor.transform, new Vector3(0, 0.51f, -0.49f), new Vector3(0.98f, 0.015f, 0.005f), mats.GunMetal);
            CreatePlatformTrim(floor.transform, new Vector3(0.49f, 0.51f, 0), new Vector3(0.005f, 0.015f, 0.98f), mats.GunMetal);
            CreatePlatformTrim(floor.transform, new Vector3(-0.49f, 0.51f, 0), new Vector3(0.005f, 0.015f, 0.98f), mats.GunMetal);

            // Left Wall
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "MainframeWall_Left";
            leftWall.transform.SetParent(roomRoot.transform);
            leftWall.transform.position = new Vector3(-7.0f, 3.75f, 4.5f);
            leftWall.transform.localScale = new Vector3(0.4f, 7.5f, 20.0f);
            leftWall.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(leftWall.transform, new Vector3(0.52f, 0, 0), new Vector3(0.015f, 0.015f, 0.98f), mats.GunMetal);

            // Left Wall Subtle Vertical Light Channels
            for (int p = 0; p < 4; p++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"WallLightPillar_L_{p + 1}";
                pillar.transform.SetParent(roomRoot.transform);
                pillar.transform.position = new Vector3(-6.78f, 3.75f, -2.0f + (p * 4.5f));
                pillar.transform.localScale = new Vector3(0.04f, 6.0f, 0.08f);
                pillar.GetComponent<Renderer>().sharedMaterial = mats.WallPillarAccent;
                UnityEngine.Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }

            // Right Wall
            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "MainframeWall_Right";
            rightWall.transform.SetParent(roomRoot.transform);
            rightWall.transform.position = new Vector3(7.0f, 3.75f, 4.5f);
            rightWall.transform.localScale = new Vector3(0.4f, 7.5f, 20.0f);
            rightWall.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(rightWall.transform, new Vector3(-0.52f, 0, 0), new Vector3(0.015f, 0.015f, 0.98f), mats.GunMetal);

            // Right Wall Subtle Vertical Light Channels
            for (int p = 0; p < 4; p++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"WallLightPillar_R_{p + 1}";
                pillar.transform.SetParent(roomRoot.transform);
                pillar.transform.position = new Vector3(6.78f, 3.75f, -2.0f + (p * 4.5f));
                pillar.transform.localScale = new Vector3(0.04f, 6.0f, 0.08f);
                pillar.GetComponent<Renderer>().sharedMaterial = mats.WallPillarAccent;
                UnityEngine.Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }

            // Back Wall (Behind player)
            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "MainframeWall_Back";
            backWall.transform.SetParent(roomRoot.transform);
            backWall.transform.position = new Vector3(0, 3.75f, -5.5f);
            backWall.transform.localScale = new Vector3(14.0f, 7.5f, 0.4f);
            backWall.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            // Ceiling (Dark Titanium Panels with subtle gunmetal trims)
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "MainframeCeiling";
            ceiling.transform.SetParent(roomRoot.transform);
            ceiling.transform.position = new Vector3(0, 7.5f, 4.5f);
            ceiling.transform.localScale = new Vector3(14.0f, 0.4f, 20.0f);
            ceiling.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(ceiling.transform, new Vector3(0, -0.52f, 0), new Vector3(0.96f, 0.01f, 0.96f), mats.GunMetal);
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
                new Vector3(-3.2f, 0.012f, 4.5f), new Vector3(0.015f, 0.005f, 16f),
                new Vector3(3.2f, 0.012f, 4.5f), new Vector3(0.015f, 0.005f, 16f),
                new Vector3(-1.2f, 0.012f, 4.5f), new Vector3(0.012f, 0.005f, 14f),
                new Vector3(1.2f, 0.012f, 4.5f), new Vector3(0.012f, 0.005f, 14f),
                new Vector3(0f, 0.012f, 0.0f), new Vector3(10f, 0.005f, 0.015f),
                new Vector3(0f, 0.012f, 4.0f), new Vector3(10f, 0.005f, 0.015f),
                new Vector3(0f, 0.012f, 7.5f), new Vector3(10f, 0.005f, 0.015f)
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
                float x = side * 5.6f;
                for (int i = 0; i < 4; i++)
                {
                    float z = -1.0f + (i * 3.5f);

                    var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chip.name = $"IC_MemoryRegister_{((side + 1) * 3) + i:D2}";
                    chip.transform.SetParent(hardwareRoot.transform);
                    chip.transform.position = new Vector3(x, 0.10f, z);
                    chip.transform.localScale = new Vector3(0.9f, 0.20f, 1.2f);
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
                    heatSink.transform.position = new Vector3(x + (side * 0.6f), 0.30f, z);
                    heatSink.transform.localScale = new Vector3(0.30f, 0.60f, 1.1f);
                    heatSink.GetComponent<Renderer>().sharedMaterial = mats.HeatSinkFins;
                }
            }

            return ledsList.ToArray();
        }

        private static void CreateDataParticles(Transform parent, Material particleMat)
        {
            var partGo = new GameObject("DataStreamParticles");
            partGo.transform.SetParent(parent);
            partGo.transform.position = new Vector3(0, 2.2f, 3.0f);

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
            shape.scale = new Vector3(8.0f, 3.0f, 8.0f);

            var rend = partGo.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = particleMat;
        }

        private static LevelMaterials GenerateMaterials()
        {
            var mats = new LevelMaterials();

            // 1. Deep Obsidian Titanium Plating & Silicon Substrate (Standard PBR with sleek metallic finish)
            mats.DarkPlating = CreateOrUpdateMaterial("Mat_Mainframe_DarkPlating", new Color(0.06f, 0.07f, 0.10f), 0.85f, 0.65f);
            mats.CpuSubstrate = CreateOrUpdateMaterial("Mat_Mainframe_Substrate", new Color(0.08f, 0.10f, 0.14f), 0.55f, 0.50f);

            // 2. Hardware IC Materials
            mats.HeatSinkFins = CreateOrUpdateMaterial("Mat_Mainframe_HeatSink", new Color(0.12f, 0.14f, 0.18f), 0.85f, 0.70f);
            mats.MemoryChip = CreateOrUpdateMaterial("Mat_Mainframe_MemoryChip", new Color(0.05f, 0.06f, 0.08f), 0.70f, 0.60f);

            // 3. Subtle Sophisticated Bus-Traces & Wall Pillars (Soft Cyan/Slate glow, not blinding)
            mats.BusTraceGlow = CreateOrUpdateMaterial("Mat_Mainframe_BusTrace", new Color(0.12f, 0.16f, 0.24f), 0.30f, 0.70f, new Color(0.15f, 0.35f, 0.60f) * 0.25f);
            mats.WallPillarAccent = CreateOrUpdateMaterial("Mat_Mainframe_WallPillar", new Color(0.14f, 0.18f, 0.26f), 0.30f, 0.70f, new Color(0.35f, 0.50f, 0.75f) * 0.30f);

            // 4. Synaptic Axons (Clean Luminous Axons)
            mats.AxonPositive = CreateOrUpdateUnlitMaterial("Mat_Axon_Positive", new Color(0.22f, 0.75f, 0.98f, 0.75f));
            mats.AxonNegative = CreateOrUpdateUnlitMaterial("Mat_Axon_Negative", new Color(0.95f, 0.30f, 0.38f, 0.75f));
            mats.AxonNeutral = CreateOrUpdateMaterial("Mat_Axon_Neutral", new Color(0.18f, 0.22f, 0.30f, 0.25f), 0.40f, 0.40f, isTransparent: true);

            // 5. Rich Futuristic Node Glows (Warm Solar Amber, Cyber Violet, Laser Mint Emerald, Ruby Coral)
            mats.GlowCyan = CreateOrUpdateMaterial("Mat_Glow_Cyan", new Color(0.20f, 0.75f, 0.98f), 0.20f, 0.90f, new Color(0.20f, 0.75f, 0.98f) * 1.2f);
            mats.GlowAmber = CreateOrUpdateMaterial("Mat_Glow_Amber", new Color(0.98f, 0.62f, 0.08f), 0.20f, 0.90f, new Color(0.98f, 0.60f, 0.08f) * 1.2f);
            mats.GlowEmerald = CreateOrUpdateMaterial("Mat_Glow_Emerald", new Color(0.08f, 0.90f, 0.50f), 0.20f, 0.90f, new Color(0.08f, 0.85f, 0.48f) * 1.2f);
            mats.GlowViolet = CreateOrUpdateMaterial("Mat_Glow_Violet", new Color(0.60f, 0.30f, 0.95f), 0.20f, 0.90f, new Color(0.55f, 0.25f, 0.90f) * 1.2f);
            mats.GlowCoral = CreateOrUpdateMaterial("Mat_Glow_Coral", new Color(0.95f, 0.35f, 0.35f), 0.20f, 0.90f, new Color(0.95f, 0.32f, 0.32f) * 1.2f);

            // 6. Deep Smoky Holographic Glass & Quantum Portal Aperture
            mats.GlassHologram = CreateOrUpdateMaterial("Mat_Mainframe_GlassHologram", new Color(0.04f, 0.06f, 0.10f, 0.90f), 0.80f, 0.85f, new Color(0.02f, 0.06f, 0.12f) * 0.3f, isTransparent: true);
            mats.CorePlasma = CreateOrUpdateUnlitMaterial("Mat_Mainframe_CorePlasma", new Color(0.20f, 0.75f, 0.98f, 0.85f));
            mats.PortalCurtain = CreateOrUpdateMaterial("Mat_Mainframe_PortalCurtain", new Color(0.04f, 0.05f, 0.08f, 0.92f), 0.80f, 0.80f, new Color(0.18f, 0.25f, 0.55f) * 0.25f, isTransparent: true);

            mats.GunMetal = CreateOrUpdateMaterial("Mat_Mainframe_GunMetal", new Color(0.14f, 0.17f, 0.22f), 0.90f, 0.75f);
            mats.GunAccent = CreateOrUpdateMaterial("Mat_Mainframe_GunAccent", new Color(0.20f, 0.24f, 0.32f), 0.75f, 0.70f, new Color(0.10f, 0.25f, 0.40f) * 0.3f);
            mats.StanchionGlow = CreateOrUpdateMaterial("Mat_Mainframe_StanchionGlow", new Color(0.15f, 0.40f, 0.70f), 0.20f, 0.80f, new Color(0.15f, 0.40f, 0.70f) * 0.8f);

            return mats;
        }

        private static Material CreateOrUpdateMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emissionColor = null, bool isTransparent = false)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");

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
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 2900; // Render before TextMesh (queue 3000) so text is always crisp and visible
            }
            else
            {
                mat.SetFloat("_Mode", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrUpdateUnlitMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

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
            var presets = new BrokenConfigurationSO[6];

            // Preset A: w1=0, w2=0, b=-1, Linear, Cable 1 Disconnected
            presets[0] = CreateOrUpdatePreset("BrokenConfig_A", "Preset A", 0.0, 0.0, -1.0, ActivationType.Linear, true, false, "Cold initial state with disconnected X1 conduit.");

            // Preset B: w1=-0.5, w2=1.0, b=0, Step, Cable 2 Disconnected
            presets[1] = CreateOrUpdatePreset("BrokenConfig_B", "Preset B", -0.5, 1.0, 0.0, ActivationType.Step, false, true, "Negative W1 corruption and disconnected X2 conduit.");

            // Preset C: w1=1.0, w2=1.0, b=-2.0, ReLU, All Connected
            presets[2] = CreateOrUpdatePreset("BrokenConfig_C", "Preset C", 1.0, 1.0, -2.0, ActivationType.ReLU, false, false, "Severe negative bias under-activation with incompatible ReLU module.");

            // Preset D: w1=0.5, w2=0.5, b=0.0, Linear, Both Disconnected
            presets[3] = CreateOrUpdatePreset("BrokenConfig_D", "Preset D", 0.5, 0.5, 0.0, ActivationType.Linear, true, true, "Weak synapses with both input conduits disconnected.");

            // Preset E: w1=-1.0, w2=-1.0, b=1.0, Step, Cable 1 Disconnected
            presets[4] = CreateOrUpdatePreset("BrokenConfig_E", "Preset E", -1.0, -1.0, 1.0, ActivationType.Step, true, false, "Inverted negative weights and high bias error.");

            // Preset F: w1=0.0, w2=0.0, b=0.0, Step, All Connected
            presets[5] = CreateOrUpdatePreset("BrokenConfig_F", "Preset F", 0.0, 0.0, 0.0, ActivationType.Step, false, false, "Zero-energy dormant state with Step activation socketed.");

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
            var planetRoot = new GameObject("PlanetaryAICore_Hologram");
            planetRoot.transform.SetParent(parent, false);
            planetRoot.transform.position = new Vector3(0, 4.80f, 6.00f);

            // 1. Central AI Mind Sphere
            var sphereGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereGo.name = "PlanetaryCore_Sphere";
            sphereGo.transform.SetParent(planetRoot.transform, false);
            sphereGo.transform.localPosition = Vector3.zero;
            sphereGo.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
            sphereGo.GetComponent<Renderer>().sharedMaterial = mats.GlowViolet;
            UnityEngine.Object.DestroyImmediate(sphereGo.GetComponent<Collider>());

            // 2. Inner Gyroscopic Ring
            var innerRingGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRingGo.name = "InnerOrbitalRing";
            innerRingGo.transform.SetParent(planetRoot.transform, false);
            innerRingGo.transform.localPosition = Vector3.zero;
            innerRingGo.transform.localScale = new Vector3(1.85f, 0.015f, 1.85f);
            innerRingGo.transform.localRotation = Quaternion.Euler(35f, 20f, 0);
            innerRingGo.GetComponent<Renderer>().sharedMaterial = mats.BusTraceGlow;
            UnityEngine.Object.DestroyImmediate(innerRingGo.GetComponent<Collider>());

            // 3. Outer Gyroscopic Ring
            var outerRingGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRingGo.name = "OuterOrbitalRing";
            outerRingGo.transform.SetParent(planetRoot.transform, false);
            outerRingGo.transform.localPosition = Vector3.zero;
            outerRingGo.transform.localScale = new Vector3(2.40f, 0.015f, 2.40f);
            outerRingGo.transform.localRotation = Quaternion.Euler(-25f, 45f, 30f);
            outerRingGo.GetComponent<Renderer>().sharedMaterial = mats.WallPillarAccent;
            UnityEngine.Object.DestroyImmediate(outerRingGo.GetComponent<Collider>());

            // 4. Core Aura Light
            var coreLight = planetRoot.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 8.0f;
            coreLight.intensity = 0.65f;
            coreLight.color = new Color(0.95f, 0.25f, 0.25f);

            // 5. Billboard Text Container
            var textContainer = new GameObject("Planetary_BillboardTexts");
            textContainer.transform.SetParent(planetRoot.transform, false);
            textContainer.transform.localPosition = new Vector3(0, 1.30f, 0);

            var headerGo = new GameObject("Planetary_Header");
            headerGo.transform.SetParent(textContainer.transform, false);
            headerGo.transform.localPosition = new Vector3(0, 0.35f, 0);
            var headerTxt = headerGo.AddComponent<TextMesh>();
            headerTxt.text = "GLOBAL FOUNDATION AI // SYNAPSE-GPT CORE";
            headerTxt.fontSize = 24;
            headerTxt.characterSize = 0.024f;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAlignment.Center;
            headerTxt.anchor = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(0.95f, 0.30f, 0.30f);

            var barGo = new GameObject("Planetary_IntegrityBar");
            barGo.transform.SetParent(textContainer.transform, false);
            barGo.transform.localPosition = new Vector3(0, 0.05f, 0);
            var barTxt = barGo.AddComponent<TextMesh>();
            barTxt.text = "INTEGRITY: [████░░░░░░░░░░░░░░░░] 20% [CRITICAL COGNITIVE DRIFT]";
            barTxt.fontSize = 20;
            barTxt.characterSize = 0.022f;
            barTxt.fontStyle = FontStyle.Bold;
            barTxt.alignment = TextAlignment.Center;
            barTxt.anchor = TextAnchor.MiddleCenter;
            barTxt.color = new Color(0.98f, 0.65f, 0.10f);

            var detailsGo = new GameObject("Planetary_StatusDetails");
            detailsGo.transform.SetParent(textContainer.transform, false);
            detailsGo.transform.localPosition = new Vector3(0, -0.30f, 0);
            var detailsTxt = detailsGo.AddComponent<TextMesh>();
            detailsTxt.text = "Sector 01 Sensory Perceptron corrupted — Global perception offline";
            detailsTxt.fontSize = 16;
            detailsTxt.characterSize = 0.018f;
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

        private static DefenseSentryVisual CreateDefenseSentryTurret(Transform parent, LevelMaterials mats, ChamberController chamberController, NeuralState neuralState)
        {
            var sentryRoot = new GameObject("AutomatedDefenseSentry");
            sentryRoot.transform.SetParent(parent, false);
            sentryRoot.transform.position = new Vector3(0, 0, 1.70f);

            // 1. Heavy Base Pedestal
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGo.name = "SentryBase_Pedestal";
            baseGo.transform.SetParent(sentryRoot.transform, false);
            baseGo.transform.localPosition = new Vector3(0, 0.20f, 0);
            baseGo.transform.localScale = new Vector3(0.85f, 0.20f, 0.85f);
            baseGo.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(baseGo.transform, new Vector3(0, 0.52f, 0), new Vector3(1.04f, 0.04f, 1.04f), mats.GunMetal);

            // 2. Swivel Yaw Head (Torso)
            var yawHeadGo = new GameObject("Sentry_YawHead");
            yawHeadGo.transform.SetParent(sentryRoot.transform, false);
            yawHeadGo.transform.localPosition = new Vector3(0, 0.55f, 0);

            var headArmor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headArmor.name = "ArmorChassis";
            headArmor.transform.SetParent(yawHeadGo.transform, false);
            headArmor.transform.localPosition = Vector3.zero;
            headArmor.transform.localScale = new Vector3(0.42f, 0.40f, 0.46f);
            headArmor.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;

            // 3. Central Core Plasma Sphere (Decision Engine)
            var corePlasma = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            corePlasma.name = "SentryCore_Plasma";
            corePlasma.transform.SetParent(yawHeadGo.transform, false);
            corePlasma.transform.localPosition = new Vector3(0, 0.05f, -0.22f);
            corePlasma.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            var corePlasmaRend = corePlasma.GetComponent<Renderer>();
            corePlasmaRend.sharedMaterial = mats.CorePlasma;
            UnityEngine.Object.DestroyImmediate(corePlasma.GetComponent<Collider>());

            // Status Strobe on Head
            var strobe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            strobe.name = "StatusStrobe";
            strobe.transform.SetParent(yawHeadGo.transform, false);
            strobe.transform.localPosition = new Vector3(0, 0.24f, 0);
            strobe.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
            var strobeRend = strobe.GetComponent<Renderer>();
            strobeRend.sharedMaterial = mats.GlowCoral;
            UnityEngine.Object.DestroyImmediate(strobe.GetComponent<Collider>());

            // 4. Pitch Gimbal & Dual Plasma Cannons
            var pitchGimbalGo = new GameObject("Sentry_PitchGimbal");
            pitchGimbalGo.transform.SetParent(yawHeadGo.transform, false);
            pitchGimbalGo.transform.localPosition = new Vector3(0, 0.12f, 0.18f);

            var leftBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftBarrel.name = "LeftBarrel";
            leftBarrel.transform.SetParent(pitchGimbalGo.transform, false);
            leftBarrel.transform.localPosition = new Vector3(-0.16f, 0, 0.22f);
            leftBarrel.transform.localScale = new Vector3(0.06f, 0.24f, 0.06f);
            leftBarrel.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            leftBarrel.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;

            var rightBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightBarrel.name = "RightBarrel";
            rightBarrel.transform.SetParent(pitchGimbalGo.transform, false);
            rightBarrel.transform.localPosition = new Vector3(0.16f, 0, 0.22f);
            rightBarrel.transform.localScale = new Vector3(0.06f, 0.24f, 0.06f);
            rightBarrel.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            rightBarrel.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;

            var leftMuzzleGo = new GameObject("LeftMuzzle");
            leftMuzzleGo.transform.SetParent(leftBarrel.transform, false);
            leftMuzzleGo.transform.localPosition = new Vector3(0, 1.0f, 0);

            var rightMuzzleGo = new GameObject("RightMuzzle");
            rightMuzzleGo.transform.SetParent(rightBarrel.transform, false);
            rightMuzzleGo.transform.localPosition = new Vector3(0, 1.0f, 0);

            // 5. Targeting LineRenderers
            var laserGo = new GameObject("TargetingLaserBeam");
            laserGo.transform.SetParent(sentryRoot.transform, false);
            var laserLr = laserGo.AddComponent<LineRenderer>();
            laserLr.sharedMaterial = mats.CorePlasma;
            laserLr.useWorldSpace = true;
            laserLr.positionCount = 2;
            laserLr.startWidth = 0.06f;
            laserLr.endWidth = 0.08f;
            laserLr.enabled = false;

            var scanGo = new GameObject("ScanConeBeam");
            scanGo.transform.SetParent(sentryRoot.transform, false);
            var scanLr = scanGo.AddComponent<LineRenderer>();
            scanLr.sharedMaterial = mats.GlassHologram;
            scanLr.useWorldSpace = true;
            scanLr.positionCount = 2;
            scanLr.startWidth = 0.03f;
            scanLr.endWidth = 0.35f;
            scanLr.enabled = false;

            // 6. Lights
            var sentryLightGo = new GameObject("SentrySpotlight");
            sentryLightGo.transform.SetParent(yawHeadGo.transform, false);
            sentryLightGo.transform.localPosition = new Vector3(0, 0.2f, 0.25f);
            var sentrySpot = sentryLightGo.AddComponent<Light>();
            sentrySpot.type = LightType.Spot;
            sentrySpot.range = 10f;
            sentrySpot.spotAngle = 45f;
            sentrySpot.intensity = 2.0f;
            sentrySpot.color = new Color(1.0f, 0.25f, 0.25f);

            var muzzleFlashGo = new GameObject("MuzzleFlashLight");
            muzzleFlashGo.transform.SetParent(pitchGimbalGo.transform, false);
            muzzleFlashGo.transform.localPosition = new Vector3(0, 0, 0.45f);
            var muzzleFlash = muzzleFlashGo.AddComponent<Light>();
            muzzleFlash.type = LightType.Point;
            muzzleFlash.range = 4.0f;
            muzzleFlash.intensity = 0f;
            muzzleFlash.enabled = false;

            // 7. Sentry Visual Component
            var sentryVisual = sentryRoot.AddComponent<DefenseSentryVisual>();
            var svSO = new SerializedObject(sentryVisual);
            svSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            svSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            svSO.FindProperty("turretBase").objectReferenceValue = sentryRoot.transform;
            svSO.FindProperty("turretYawHead").objectReferenceValue = yawHeadGo.transform;
            svSO.FindProperty("turretPitchGimbal").objectReferenceValue = pitchGimbalGo.transform;
            svSO.FindProperty("leftMuzzle").objectReferenceValue = leftMuzzleGo.transform;
            svSO.FindProperty("rightMuzzle").objectReferenceValue = rightMuzzleGo.transform;
            svSO.FindProperty("sentryCorePlasma").objectReferenceValue = corePlasmaRend;
            svSO.FindProperty("statusStrobeRenderer").objectReferenceValue = strobeRend;
            svSO.FindProperty("targetingLaser").objectReferenceValue = laserLr;
            svSO.FindProperty("scanConeBeam").objectReferenceValue = scanLr;
            svSO.FindProperty("sentrySpotlight").objectReferenceValue = sentrySpot;
            svSO.FindProperty("muzzleFlashLight").objectReferenceValue = muzzleFlash;
            svSO.ApplyModifiedProperties();

            return sentryVisual;
        }

        private static List<DataTargetReceptor> CreateDataTargetPods(Transform parent, LevelMaterials mats)
        {
            var podsRoot = new GameObject("DataTargetReceptorPods");
            podsRoot.transform.SetParent(parent);

            var receptors = new List<DataTargetReceptor>();

            var cases = new[]
            {
                new { Index = 0, Label = "Friendly Drone (0,0)", Title = "Friendly Supply Drone", Role = "SAFE ALLY (DO NOT ENGAGE)", X1 = 0.0, X2 = 0.0, Expected = 0.0, Spawn = new Vector3(-2.8f, 1.2f, 7.0f), Perimeter = new Vector3(-2.8f, 1.2f, 2.6f) },
                new { Index = 1, Label = "Biohazard Pod (0,1)",   Title = "Biohazard Toxin Canister", Role = "LETHAL HAZARD (INTERCEPT Y=1)", X1 = 0.0, X2 = 1.0, Expected = 1.0, Spawn = new Vector3(-0.95f, 1.2f, 7.2f), Perimeter = new Vector3(-0.95f, 1.2f, 2.8f) },
                new { Index = 2, Label = "Radiation Drone (1,0)",  Title = "Rogue Radiation Drone", Role = "LETHAL HAZARD (INTERCEPT Y=1)", X1 = 1.0, X2 = 0.0, Expected = 1.0, Spawn = new Vector3(0.95f, 1.2f, 7.2f), Perimeter = new Vector3(0.95f, 1.2f, 2.8f) },
                new { Index = 3, Label = "Dual-Breach Core (1,1)", Title = "Overloaded Dual-Breach Core", Role = "CRITICAL BREACH (INTERCEPT Y=1)", X1 = 1.0, X2 = 1.0, Expected = 1.0, Spawn = new Vector3(2.8f, 1.2f, 7.0f), Perimeter = new Vector3(2.8f, 1.2f, 2.6f) }
            };

            foreach (var c in cases)
            {
                var podGo = new GameObject($"DataTargetPod_Case{c.Index + 1}");
                podGo.transform.SetParent(podsRoot.transform);
                podGo.transform.position = c.Spawn;

                // Floor approach corridor rail
                var railGo = new GameObject($"CorridorRail_Case{c.Index + 1}");
                railGo.transform.SetParent(podsRoot.transform);
                var railLr = railGo.AddComponent<LineRenderer>();
                railLr.sharedMaterial = mats.BusTraceGlow;
                railLr.useWorldSpace = true;
                railLr.positionCount = 2;
                railLr.SetPosition(0, new Vector3(c.Spawn.x, 0.02f, c.Spawn.z));
                railLr.SetPosition(1, new Vector3(c.Perimeter.x, 0.02f, c.Perimeter.z));
                railLr.startWidth = 0.025f;
                railLr.endWidth = 0.035f;

                // Pedestal base at perimeter dock
                var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pedestal.name = "PerimeterDock";
                pedestal.transform.SetParent(podsRoot.transform, false);
                pedestal.transform.position = new Vector3(c.Perimeter.x, 0.22f, c.Perimeter.z);
                pedestal.transform.localScale = new Vector3(0.44f, 0.22f, 0.44f);
                pedestal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
                CreatePlatformTrim(pedestal.transform, new Vector3(0, 0.52f, 0), new Vector3(1.05f, 0.04f, 1.05f), mats.GunMetal);

                // Floating Target 3D Object
                var targetBodyRoot = new GameObject("FloatingTargetBody");
                targetBodyRoot.transform.SetParent(podGo.transform, false);
                targetBodyRoot.transform.localPosition = Vector3.zero;

                Renderer coreRend = null;
                Renderer subRend = null;

                if (c.Index == 0)
                {
                    // Case 0: Friendly Drone
                    var droneBody = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    droneBody.name = "DroneSphere";
                    droneBody.transform.SetParent(targetBodyRoot.transform, false);
                    droneBody.transform.localScale = new Vector3(0.26f, 0.26f, 0.26f);
                    coreRend = droneBody.GetComponent<Renderer>();
                    coreRend.sharedMaterial = mats.GlowEmerald;

                    var visor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visor.name = "DroneVisor";
                    visor.transform.SetParent(targetBodyRoot.transform, false);
                    visor.transform.localPosition = new Vector3(0, 0.04f, 0.12f);
                    visor.transform.localScale = new Vector3(0.24f, 0.04f, 0.08f);
                    visor.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                    subRend = visor.GetComponent<Renderer>();
                    subRend.sharedMaterial = mats.GlowCyan;
                    UnityEngine.Object.DestroyImmediate(visor.GetComponent<Collider>());
                }
                else if (c.Index == 1)
                {
                    // Case 1: Biohazard Canister
                    var canGlass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    canGlass.name = "BioCanisterGlass";
                    canGlass.transform.SetParent(targetBodyRoot.transform, false);
                    canGlass.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
                    subRend = canGlass.GetComponent<Renderer>();
                    subRend.sharedMaterial = mats.GlassHologram;

                    var toxicCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    toxicCore.name = "ToxicCore";
                    toxicCore.transform.SetParent(targetBodyRoot.transform, false);
                    toxicCore.transform.localScale = new Vector3(0.16f, 0.20f, 0.16f);
                    coreRend = toxicCore.GetComponent<Renderer>();
                    coreRend.sharedMaterial = mats.GlowAmber;
                    UnityEngine.Object.DestroyImmediate(toxicCore.GetComponent<Collider>());
                }
                else if (c.Index == 2)
                {
                    // Case 2: Radiation Sentry Drone
                    var radBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    radBox.name = "RadiationCore";
                    radBox.transform.SetParent(targetBodyRoot.transform, false);
                    radBox.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
                    radBox.transform.localRotation = Quaternion.Euler(45f, 45f, 0);
                    coreRend = radBox.GetComponent<Renderer>();
                    coreRend.sharedMaterial = mats.GlowCoral;

                    var radRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    radRing.name = "WarningRing";
                    radRing.transform.SetParent(targetBodyRoot.transform, false);
                    radRing.transform.localScale = new Vector3(0.32f, 0.02f, 0.32f);
                    subRend = radRing.GetComponent<Renderer>();
                    subRend.sharedMaterial = mats.CorePlasma;
                    UnityEngine.Object.DestroyImmediate(radRing.GetComponent<Collider>());
                }
                else
                {
                    // Case 3: Dual-Breach Overload Core
                    var coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    coreSphere.name = "DualCoreSphere";
                    coreSphere.transform.SetParent(targetBodyRoot.transform, false);
                    coreSphere.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                    coreRend = coreSphere.GetComponent<Renderer>();
                    coreRend.sharedMaterial = mats.GlowViolet;

                    var halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    halo.name = "PlasmaHalo";
                    halo.transform.SetParent(targetBodyRoot.transform, false);
                    halo.transform.localScale = new Vector3(0.36f, 0.015f, 0.36f);
                    halo.transform.localRotation = Quaternion.Euler(30f, 60f, 0);
                    subRend = halo.GetComponent<Renderer>();
                    subRend.sharedMaterial = mats.CorePlasma;
                    UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());
                }

                // Aura Point Light
                var lightGo = new GameObject("AuraLight");
                lightGo.transform.SetParent(podGo.transform, false);
                lightGo.transform.localPosition = Vector3.zero;
                var auraLight = lightGo.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.range = 2.8f;
                auraLight.intensity = 1.6f;
                auraLight.color = new Color(0.20f, 0.75f, 0.98f);

                // Receptor Component
                var receptor = podGo.AddComponent<DataTargetReceptor>();
                var recSO = new SerializedObject(receptor);
                recSO.FindProperty("caseIndex").intValue = c.Index;
                recSO.FindProperty("caseLabel").stringValue = c.Label;
                recSO.FindProperty("targetTitle").stringValue = c.Title;
                recSO.FindProperty("threatRole").stringValue = c.Role;
                recSO.FindProperty("inputX1").doubleValue = c.X1;
                recSO.FindProperty("inputX2").doubleValue = c.X2;
                recSO.FindProperty("expectedOutput").doubleValue = c.Expected;
                recSO.ApplyModifiedProperties();

                // Visual Component
                var visual = podGo.AddComponent<DataTargetVisual>();
                var visSO = new SerializedObject(visual);
                visSO.FindProperty("receptor").objectReferenceValue = receptor;
                visSO.FindProperty("floatingTargetBody").objectReferenceValue = targetBodyRoot.transform;
                visSO.FindProperty("coreRenderer").objectReferenceValue = coreRend;
                visSO.FindProperty("subRenderer").objectReferenceValue = subRend;
                visSO.FindProperty("auraLight").objectReferenceValue = auraLight;
                visSO.FindProperty("spawnPosition").vector3Value = c.Spawn;
                visSO.FindProperty("perimeterPosition").vector3Value = c.Perimeter;
                visSO.ApplyModifiedProperties();

                receptors.Add(receptor);
            }

            // Holographic Containment Shield Barrier in front of player's console
            var shieldBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shieldBarrier.name = "StationContainmentShieldBarrier";
            shieldBarrier.transform.SetParent(podsRoot.transform, false);
            shieldBarrier.transform.position = new Vector3(0, 1.25f, -0.65f);
            shieldBarrier.transform.localScale = new Vector3(7.5f, 2.5f, 0.04f);
            shieldBarrier.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;
            UnityEngine.Object.DestroyImmediate(shieldBarrier.GetComponent<Collider>());

            return receptors;
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
