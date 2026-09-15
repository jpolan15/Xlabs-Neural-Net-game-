using System;
using System.IO;
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
    /// Assembles the complete playable level, generates ScriptableObject presets A through F,
    /// sets up cosmic deep-space environment, URP materials, gyroscopic neuron machinery,
    /// first-person viewmodel Neural Pulse Gun, astronaut engineer HUD, and registers the scene.
    /// </summary>
    public static class Level01SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Level01_AwakeningGate.unity";
        private const string PresetsFolder = "Assets/Puzzles/Chamber01";
        private const string MaterialsFolder = "Assets/Materials";

        public struct LevelMaterials
        {
            public Material DarkPlating;
            public Material GlowCyan;
            public Material GlowAmber;
            public Material GlowEmerald;
            public Material GlowViolet;
            public Material GlassHologram;
            public Material CorePlasma;
            public Material PortalCurtain;
            public Material Asteroid;
            public Material GunMetal;
            public Material GunAccent;
            public Material StanchionGlow;
            public Material Planet;
            public Material PlanetRings;
        }

        [MenuItem("Convergence/Build Level 1 — The Awakening Gate")]
        public static void BuildLevel01()
        {
            Debug.Log("[Level01SceneBuilder] Beginning automated generation of Level 1 — The Awakening Gate...");

            // 1. Ensure target directories exist
            EnsureDirectories();

            // 2. Generate Broken Configuration ScriptableObject Presets
            var presets = GenerateBrokenPresets();

            // 3. Generate Cohesive URP Sci-Fi Materials Palette
            var mats = GenerateMaterials();

            // 4. Assemble Unity Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Environment & Cosmic Deep Space ---
            var envRoot = new GameObject("Environment");

            // Stellar Sun (Key Light)
            var sunGo = new GameObject("DirectionalLight_StellarSun");
            sunGo.transform.SetParent(envRoot.transform);
            var sunLight = sunGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(0.85f, 0.92f, 1.0f);
            sunLight.intensity = 1.15f;
            sunGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // Ambient Cosmic Nebula Fill Light
            var fillGo = new GameObject("DirectionalLight_NebulaFill");
            fillGo.transform.SetParent(envRoot.transform);
            var fillLight = fillGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.18f, 0.08f, 0.32f);
            fillLight.intensity = 0.45f;
            fillGo.transform.rotation = Quaternion.Euler(-40f, 145f, 0f);

            // Main Observatory Platform
            var platformMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformMain.name = "FloatingPlatform_Main";
            platformMain.transform.SetParent(envRoot.transform);
            platformMain.transform.position = new Vector3(0, -0.25f, 0);
            platformMain.transform.localScale = new Vector3(10f, 0.5f, 10f);
            platformMain.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            // Platform Perimeter Trims (Glowing Circuit Inlays)
            CreatePlatformTrim(envRoot.transform, new Vector3(0, 0.02f, 5.0f), new Vector3(10f, 0.04f, 0.15f), mats.GlowCyan);
            CreatePlatformTrim(envRoot.transform, new Vector3(0, 0.02f, -5.0f), new Vector3(10f, 0.04f, 0.15f), mats.GlowCyan);
            CreatePlatformTrim(envRoot.transform, new Vector3(-5.0f, 0.02f, 0), new Vector3(0.15f, 0.04f, 10f), mats.GlowCyan);
            CreatePlatformTrim(envRoot.transform, new Vector3(5.0f, 0.02f, 0), new Vector3(0.15f, 0.04f, 10f), mats.GlowCyan);

            // Perimeter Safety Stanchions
            CreateSafetyStanchions(envRoot.transform, mats);

            // Elevated Approach Bridge to Awakening Gate
            var platformApproach = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformApproach.name = "FloatingPlatform_Approach";
            platformApproach.transform.SetParent(envRoot.transform);
            platformApproach.transform.position = new Vector3(0, -0.25f, 10.5f);
            platformApproach.transform.localScale = new Vector3(4.2f, 0.5f, 11f);
            platformApproach.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            // Power Rails along Approach Bridge
            CreatePlatformTrim(envRoot.transform, new Vector3(-1.8f, 0.02f, 10.5f), new Vector3(0.12f, 0.04f, 11f), mats.GlowCyan);
            CreatePlatformTrim(envRoot.transform, new Vector3(1.8f, 0.02f, 10.5f), new Vector3(0.12f, 0.04f, 11f), mats.GlowCyan);

            // Floating Asteroids in Void
            var asteroidFieldGo = new GameObject("CosmicAsteroidField");
            asteroidFieldGo.transform.SetParent(envRoot.transform);
            var asteroidField = asteroidFieldGo.AddComponent<FloatingAsteroidField>();
            var afSO = new SerializedObject(asteroidField);
            afSO.FindProperty("asteroidMaterial").objectReferenceValue = mats.Asteroid;
            afSO.ApplyModifiedProperties();
            asteroidField.InitializeField();

            // Cosmic Stardust Particles
            CreateStardustParticles(envRoot.transform, mats.GlowCyan);

            // Distant Celestial Ringed Planet
            CreateCelestialPlanet(envRoot.transform, mats);

            // --- Gameplay Controllers Root ---
            var controllersGo = new GameObject("GameplayControllers");
            var neuralState = controllersGo.AddComponent<NeuralState>();
            var performanceTracker = controllersGo.AddComponent<PerformanceTracker>();
            var chamberController = controllersGo.AddComponent<ChamberController>();
            var levelResetter = controllersGo.AddComponent<LevelResetter>();
            var audioHookManager = controllersGo.AddComponent<AudioHookManager>();

            // Set private serialized fields using SerializedObject
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

            var chamberSO = new SerializedObject(chamberController);
            chamberSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            chamberSO.FindProperty("performanceTracker").objectReferenceValue = performanceTracker;
            chamberSO.FindProperty("initialPreset").objectReferenceValue = presets[0];
            chamberSO.ApplyModifiedProperties();

            // --- Central Alien Neuron Machine ---
            var neuronMachineGo = new GameObject("NeuronMachine");
            neuronMachineGo.transform.position = new Vector3(0, 0, 3.5f);

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(neuronMachineGo.transform);
            pedestal.transform.localPosition = new Vector3(0, 0.4f, 0);
            pedestal.transform.localScale = new Vector3(2.5f, 0.4f, 2.5f);
            pedestal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "ConvergenceCore_Sphere";
            coreSphere.transform.SetParent(neuronMachineGo.transform);
            coreSphere.transform.localPosition = new Vector3(0, 1.4f, 0);
            coreSphere.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            coreSphere.GetComponent<Renderer>().sharedMaterial = mats.CorePlasma;

            var coreLight = coreSphere.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 8.5f;
            coreLight.intensity = 2.8f;
            coreLight.color = new Color(0.1f, 0.9f, 1.0f);

            // Gyroscopic Orbital Rings around Core
            var gyroGo = new GameObject("GyroscopicRings");
            gyroGo.transform.SetParent(neuronMachineGo.transform);
            gyroGo.transform.localPosition = new Vector3(0, 1.4f, 0);

            var outerRingGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRingGo.name = "OuterRing";
            outerRingGo.transform.SetParent(gyroGo.transform);
            outerRingGo.transform.localPosition = Vector3.zero;
            outerRingGo.transform.localScale = new Vector3(1.65f, 0.035f, 1.65f);
            outerRingGo.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            var outerCol = outerRingGo.GetComponent<Collider>();
            if (outerCol != null) UnityEngine.Object.DestroyImmediate(outerCol);

            var innerRingGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRingGo.name = "InnerRing";
            innerRingGo.transform.SetParent(gyroGo.transform);
            innerRingGo.transform.localPosition = Vector3.zero;
            innerRingGo.transform.localScale = new Vector3(1.35f, 0.03f, 1.35f);
            innerRingGo.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;
            var innerCol = innerRingGo.GetComponent<Collider>();
            if (innerCol != null) UnityEngine.Object.DestroyImmediate(innerCol);

            var gyroscopicRings = gyroGo.AddComponent<GyroscopicRings>();
            var gyroSO = new SerializedObject(gyroscopicRings);
            gyroSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            gyroSO.FindProperty("innerRing").objectReferenceValue = innerRingGo.transform;
            gyroSO.FindProperty("outerRing").objectReferenceValue = outerRingGo.transform;
            gyroSO.ApplyModifiedProperties();

            var machineVisual = neuronMachineGo.AddComponent<NeuronMachineVisual>();

            // W1 Regulator Dial
            var w1Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w1Go.name = "WeightRegulator_W1";
            w1Go.transform.SetParent(neuronMachineGo.transform);
            w1Go.transform.localPosition = new Vector3(-0.9f, 0.85f, 0.5f);
            w1Go.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            w1Go.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            var w1Interactor = w1Go.AddComponent<WeightRegulatorInteractor>();
            var w1SO = new SerializedObject(w1Interactor);
            w1SO.FindProperty("socketIndex").intValue = 0;
            w1SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            w1SO.ApplyModifiedProperties();

            // W2 Regulator Dial
            var w2Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w2Go.name = "WeightRegulator_W2";
            w2Go.transform.SetParent(neuronMachineGo.transform);
            w2Go.transform.localPosition = new Vector3(0.9f, 0.85f, 0.5f);
            w2Go.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            w2Go.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            var w2Interactor = w2Go.AddComponent<WeightRegulatorInteractor>();
            var w2SO = new SerializedObject(w2Interactor);
            w2SO.FindProperty("socketIndex").intValue = 1;
            w2SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            w2SO.ApplyModifiedProperties();

            // Bias Dial
            var biasGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            biasGo.name = "BiasDial";
            biasGo.transform.SetParent(neuronMachineGo.transform);
            biasGo.transform.localPosition = new Vector3(0, 0.85f, 0.9f);
            biasGo.transform.localScale = new Vector3(0.45f, 0.1f, 0.45f);
            biasGo.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            var biasInteractor = biasGo.AddComponent<BiasDialInteractor>();
            var biasSO = new SerializedObject(biasInteractor);
            biasSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            biasSO.ApplyModifiedProperties();

            // Activation Socket & Physical Crystals
            var socketGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            socketGo.name = "ActivationCrystalSocket";
            socketGo.transform.SetParent(neuronMachineGo.transform);
            socketGo.transform.localPosition = new Vector3(0, 0.85f, -0.6f);
            socketGo.transform.localScale = new Vector3(0.35f, 0.15f, 0.35f);
            socketGo.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            var socketInteractor = socketGo.AddComponent<ActivationSocketInteractor>();
            var socketSO = new SerializedObject(socketInteractor);
            socketSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            socketSO.ApplyModifiedProperties();

            // Dynamic 3D Activation Crystal Visual
            var crystalRootGo = new GameObject("ActiveCrystalVisual");
            crystalRootGo.transform.SetParent(socketGo.transform);
            crystalRootGo.transform.localPosition = new Vector3(0, 1.5f, 0);

            var linearCry = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            linearCry.name = "Crystal_Linear";
            linearCry.transform.SetParent(crystalRootGo.transform);
            linearCry.transform.localPosition = Vector3.zero;
            linearCry.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
            linearCry.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(linearCry.GetComponent<Collider>());

            var reluCry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reluCry.name = "Crystal_ReLU";
            reluCry.transform.SetParent(crystalRootGo.transform);
            reluCry.transform.localPosition = Vector3.zero;
            reluCry.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            reluCry.transform.localRotation = Quaternion.Euler(25f, 45f, 0);
            reluCry.GetComponent<Renderer>().sharedMaterial = mats.GlowAmber;
            UnityEngine.Object.DestroyImmediate(reluCry.GetComponent<Collider>());

            var stepCry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepCry.name = "Crystal_Step";
            stepCry.transform.SetParent(crystalRootGo.transform);
            stepCry.transform.localPosition = Vector3.zero;
            stepCry.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            stepCry.transform.localRotation = Quaternion.Euler(0, 45f, 45f);
            stepCry.GetComponent<Renderer>().sharedMaterial = mats.GlowEmerald;
            UnityEngine.Object.DestroyImmediate(stepCry.GetComponent<Collider>());

            var sigCry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sigCry.name = "Crystal_Sigmoid";
            sigCry.transform.SetParent(crystalRootGo.transform);
            sigCry.transform.localPosition = Vector3.zero;
            sigCry.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);
            sigCry.GetComponent<Renderer>().sharedMaterial = mats.GlowViolet;
            UnityEngine.Object.DestroyImmediate(sigCry.GetComponent<Collider>());

            // Default initial activation in Preset A is Linear
            linearCry.SetActive(true);
            reluCry.SetActive(false);
            stepCry.SetActive(false);
            sigCry.SetActive(false);

            var crystalLight = crystalRootGo.AddComponent<Light>();
            crystalLight.type = LightType.Point;
            crystalLight.range = 2.5f;
            crystalLight.intensity = 1.8f;
            crystalLight.color = Color.cyan;

            var crystalVisual = crystalRootGo.AddComponent<ActivationCrystalVisual>();
            var crySO = new SerializedObject(crystalVisual);
            crySO.FindProperty("neuralState").objectReferenceValue = neuralState;
            crySO.FindProperty("linearCrystalModel").objectReferenceValue = linearCry;
            crySO.FindProperty("reluCrystalModel").objectReferenceValue = reluCry;
            crySO.FindProperty("stepCrystalModel").objectReferenceValue = stepCry;
            crySO.FindProperty("sigmoidCrystalModel").objectReferenceValue = sigCry;
            crySO.FindProperty("crystalGlowLight").objectReferenceValue = crystalLight;
            crySO.ApplyModifiedProperties();

            // Cable 1
            var cable1Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable1Go.name = "NeuralCable_1";
            cable1Go.transform.SetParent(neuronMachineGo.transform);
            cable1Go.transform.localPosition = new Vector3(-1.4f, 0.4f, 0);
            cable1Go.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            cable1Go.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            var cable1Interactable = cable1Go.AddComponent<CableInteractable>();
            var c1SO = new SerializedObject(cable1Interactable);
            c1SO.FindProperty("cableIndex").intValue = 0;
            c1SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            c1SO.ApplyModifiedProperties();

            // Cable 2
            var cable2Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cable2Go.name = "NeuralCable_2";
            cable2Go.transform.SetParent(neuronMachineGo.transform);
            cable2Go.transform.localPosition = new Vector3(1.4f, 0.4f, 0);
            cable2Go.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            cable2Go.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            var cable2Interactable = cable2Go.AddComponent<CableInteractable>();
            var c2SO = new SerializedObject(cable2Interactable);
            c2SO.FindProperty("cableIndex").intValue = 1;
            c2SO.FindProperty("neuralState").objectReferenceValue = neuralState;
            c2SO.ApplyModifiedProperties();

            // Link visual fields
            var mvSO = new SerializedObject(machineVisual);
            mvSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            mvSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            mvSO.FindProperty("w1RegulatorDial").objectReferenceValue = w1Go.transform;
            mvSO.FindProperty("w2RegulatorDial").objectReferenceValue = w2Go.transform;
            mvSO.FindProperty("biasDial").objectReferenceValue = biasGo.transform;
            mvSO.FindProperty("corePulseLight").objectReferenceValue = coreLight;
            mvSO.FindProperty("cable1Renderer").objectReferenceValue = cable1Go.GetComponent<Renderer>();
            mvSO.FindProperty("cable2Renderer").objectReferenceValue = cable2Go.GetComponent<Renderer>();
            mvSO.ApplyModifiedProperties();

            // --- The Awakening Gate ---
            var gateGo = new GameObject("AwakeningGate");
            gateGo.transform.position = new Vector3(0, 0, 16.5f);

            var leftPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPortal.name = "PortalWing_Left";
            leftPortal.transform.SetParent(gateGo.transform);
            leftPortal.transform.localPosition = new Vector3(-2.2f, 4.0f, 0);
            leftPortal.transform.localScale = new Vector3(4.0f, 8.0f, 1.0f);
            leftPortal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            var rightPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPortal.name = "PortalWing_Right";
            rightPortal.transform.SetParent(gateGo.transform);
            rightPortal.transform.localPosition = new Vector3(2.2f, 4.0f, 0);
            rightPortal.transform.localScale = new Vector3(4.0f, 8.0f, 1.0f);
            rightPortal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;

            // Translucent Energetic Gate Curtain
            var gateCurtainGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateCurtainGo.name = "PortalEnergyCurtain";
            gateCurtainGo.transform.SetParent(gateGo.transform);
            gateCurtainGo.transform.localPosition = new Vector3(0, 4.0f, 0);
            gateCurtainGo.transform.localScale = new Vector3(4.2f, 7.8f, 0.08f);
            gateCurtainGo.GetComponent<Renderer>().sharedMaterial = mats.PortalCurtain;
            UnityEngine.Object.DestroyImmediate(gateCurtainGo.GetComponent<Collider>());

            var gateLightGo = new GameObject("GateAuraLight");
            gateLightGo.transform.SetParent(gateGo.transform);
            gateLightGo.transform.localPosition = new Vector3(0, 4.0f, -0.5f);
            var gateLight = gateLightGo.AddComponent<Light>();
            gateLight.type = LightType.Point;
            gateLight.range = 15.0f;
            gateLight.intensity = 2.0f;
            gateLight.color = new Color(0.9f, 0.25f, 0.1f);

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

            // Hook GatewayController into LevelResetter
            resetterSO.Update();
            resetterSO.FindProperty("gatewayController").objectReferenceValue = gatewayController;
            resetterSO.ApplyModifiedProperties();

            // --- Diagnostic Hologram Display Board ---
            var hologramGo = new GameObject("DiagnosticHologram");
            hologramGo.transform.position = new Vector3(2.4f, 1.8f, 2.5f);
            hologramGo.transform.rotation = Quaternion.Euler(0, -35f, 0);

            var holoBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            holoBoard.name = "HologramPanel";
            holoBoard.transform.SetParent(hologramGo.transform);
            holoBoard.transform.localPosition = Vector3.zero;
            holoBoard.transform.localScale = new Vector3(2.2f, 1.4f, 0.04f);
            holoBoard.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;

            var hologramVisual = hologramGo.AddComponent<DiagnosticHologramVisual>();
            var holoSO = new SerializedObject(hologramVisual);
            holoSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            holoSO.ApplyModifiedProperties();

            // --- Tools Pedestals ---
            var leftPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPedestal.name = "ToolsPedestal_Left";
            leftPedestal.transform.position = new Vector3(-1.8f, 0.35f, 2.0f);
            leftPedestal.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            leftPedestal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(leftPedestal.transform, new Vector3(0, 0.51f, 0), new Vector3(1.02f, 0.03f, 1.02f), mats.GlowCyan);

            var pulseToolGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pulseToolGo.name = "NeuralPulseTool";
            pulseToolGo.transform.position = new Vector3(-1.8f, 0.88f, 2.0f);
            pulseToolGo.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            pulseToolGo.transform.rotation = Quaternion.Euler(45f, 0, 0);
            pulseToolGo.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            var pulseTool = pulseToolGo.AddComponent<NeuralPulseToolInteractor>();

            var rightPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPedestal.name = "ToolsPedestal_Right";
            rightPedestal.transform.position = new Vector3(1.8f, 0.35f, 2.0f);
            rightPedestal.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            rightPedestal.GetComponent<Renderer>().sharedMaterial = mats.DarkPlating;
            CreatePlatformTrim(rightPedestal.transform, new Vector3(0, 0.51f, 0), new Vector3(1.02f, 0.03f, 1.02f), mats.GlowCyan);

            var arcBladeGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arcBladeGo.name = "ArcBlade";
            arcBladeGo.transform.position = new Vector3(1.8f, 0.88f, 2.0f);
            arcBladeGo.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
            arcBladeGo.transform.rotation = Quaternion.Euler(45f, 0, 0);
            arcBladeGo.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;
            var arcBlade = arcBladeGo.AddComponent<ArcBladeInteractor>();

            // --- Player Rig & Camera ---
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0, 1.7f, -1.5f);
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            cam.fieldOfView = 65f;
            cameraGo.AddComponent<AudioListener>();

            // First-Person Viewmodel Gun Rig
            CreateGunViewmodel(cameraGo, coreSphere.transform, chamberController, pulseTool, mats);

            // Astronaut Engineer HUD
            var hudComponent = cameraGo.AddComponent<SciFiEngineerHUD>();
            var hudSO = new SerializedObject(hudComponent);
            hudSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            hudSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            hudSO.ApplyModifiedProperties();

            // Desktop Input Fallback
            var desktopFallback = cameraGo.AddComponent<DesktopInputFallback>();
            var dfSO = new SerializedObject(desktopFallback);
            dfSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            dfSO.FindProperty("neuralState").objectReferenceValue = neuralState;
            dfSO.FindProperty("levelResetter").objectReferenceValue = levelResetter;
            dfSO.FindProperty("pulseTool").objectReferenceValue = pulseTool;
            dfSO.FindProperty("arcBlade").objectReferenceValue = arcBlade;
            dfSO.FindProperty("activationSocket").objectReferenceValue = socketInteractor;
            dfSO.FindProperty("showHUD").boolValue = false; // SciFiEngineerHUD provides HUD
            dfSO.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register in Build Settings
            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Level01SceneBuilder] Level 1 generated and saved successfully to '{ScenePath}'.");
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

        private static void CreateSafetyStanchions(Transform parent, LevelMaterials mats)
        {
            Vector3[] stanchionPositions = new Vector3[]
            {
                new Vector3(-4.8f, 0.4f, -4.8f),
                new Vector3(4.8f, 0.4f, -4.8f),
                new Vector3(-4.8f, 0.4f, 4.8f),
                new Vector3(4.8f, 0.4f, 4.8f),
                new Vector3(-4.8f, 0.4f, 0),
                new Vector3(4.8f, 0.4f, 0),
                new Vector3(0, 0.4f, -4.8f),
                new Vector3(0, 0.4f, 4.8f)
            };

            for (int i = 0; i < stanchionPositions.Length; i++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Stanchion_{i:D2}";
                post.transform.SetParent(parent);
                post.transform.localPosition = stanchionPositions[i];
                post.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
                post.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;

                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = "Cap";
                cap.transform.SetParent(post.transform);
                cap.transform.localPosition = new Vector3(0, 1.1f, 0);
                cap.transform.localScale = new Vector3(1.4f, 0.4f, 1.4f);
                cap.GetComponent<Renderer>().sharedMaterial = mats.StanchionGlow;
            }
        }

        private static void CreateStardustParticles(Transform parent, Material glowMat)
        {
            var dustGo = new GameObject("StardustParticles");
            dustGo.transform.SetParent(parent);
            dustGo.transform.localPosition = new Vector3(0, 2.0f, 4.0f);

            var ps = dustGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 10f;
            main.startSpeed = 0.08f;
            main.startSize = 0.04f;
            main.startColor = new Color(0.4f, 0.85f, 1.0f, 0.65f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 4f, 16f);

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var psRend = dustGo.GetComponent<ParticleSystemRenderer>();
            if (psRend != null && glowMat != null)
            {
                psRend.sharedMaterial = glowMat;
            }
        }

        private static void CreateCelestialPlanet(Transform parent, LevelMaterials mats)
        {
            var planetRoot = new GameObject("CelestialPlanet_ExoPrime");
            planetRoot.transform.SetParent(parent);
            planetRoot.transform.position = new Vector3(75.0f, 42.0f, 190.0f);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PlanetSphere";
            sphere.transform.SetParent(planetRoot.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = new Vector3(65.0f, 65.0f, 65.0f);
            sphere.GetComponent<Renderer>().sharedMaterial = mats.Planet;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());

            var rings = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rings.name = "PlanetaryRings";
            rings.transform.SetParent(planetRoot.transform);
            rings.transform.localPosition = Vector3.zero;
            rings.transform.localRotation = Quaternion.Euler(24.0f, 18.0f, -15.0f);
            rings.transform.localScale = new Vector3(145.0f, 0.2f, 145.0f);
            rings.GetComponent<Renderer>().sharedMaterial = mats.PlanetRings;
            UnityEngine.Object.DestroyImmediate(rings.GetComponent<Collider>());
        }

        private static void CreateGunViewmodel(
            GameObject cameraGo,
            Transform targetCore,
            ChamberController chamberController,
            NeuralPulseToolInteractor pulseTool,
            LevelMaterials mats)
        {
            var gunRootGo = new GameObject("NeuralGun_Viewmodel");
            gunRootGo.transform.SetParent(cameraGo.transform);
            gunRootGo.transform.localPosition = new Vector3(0.26f, -0.22f, 0.48f);
            gunRootGo.transform.localRotation = Quaternion.Euler(1.5f, -2.5f, 0f);

            // Pivot chassis for recoil and sway (Uniform scale 1,1,1 to avoid child shearing)
            var gunChassis = new GameObject("Chassis");
            gunChassis.transform.SetParent(gunRootGo.transform);
            gunChassis.transform.localPosition = Vector3.zero;
            gunChassis.transform.localRotation = Quaternion.identity;
            gunChassis.transform.localScale = Vector3.one;

            // 1. Receiver Chassis
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Receiver";
            receiver.transform.SetParent(gunChassis.transform);
            receiver.transform.localPosition = Vector3.zero;
            receiver.transform.localScale = new Vector3(0.055f, 0.065f, 0.22f);
            receiver.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            UnityEngine.Object.DestroyImmediate(receiver.GetComponent<Collider>());

            // 2. Pistol Grip Handle
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(gunChassis.transform);
            grip.transform.localPosition = new Vector3(0, -0.065f, -0.04f);
            grip.transform.localRotation = Quaternion.Euler(-22f, 0, 0);
            grip.transform.localScale = new Vector3(0.038f, 0.09f, 0.042f);
            grip.GetComponent<Renderer>().sharedMaterial = mats.GunMetal;
            UnityEngine.Object.DestroyImmediate(grip.GetComponent<Collider>());

            // 3. Glowing Plasma Coil Cell
            var plasmaCell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plasmaCell.name = "PlasmaCell";
            plasmaCell.transform.SetParent(gunChassis.transform);
            plasmaCell.transform.localPosition = new Vector3(0, 0.015f, 0.02f);
            plasmaCell.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            plasmaCell.transform.localScale = new Vector3(0.035f, 0.055f, 0.035f);
            plasmaCell.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(plasmaCell.GetComponent<Collider>());

            // 4. Upper & Lower Magnetic Accelerator Rails
            var upperRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperRail.name = "UpperRail";
            upperRail.transform.SetParent(gunChassis.transform);
            upperRail.transform.localPosition = new Vector3(0, 0.032f, 0.14f);
            upperRail.transform.localScale = new Vector3(0.032f, 0.012f, 0.16f);
            upperRail.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;
            UnityEngine.Object.DestroyImmediate(upperRail.GetComponent<Collider>());

            var lowerRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowerRail.name = "LowerRail";
            lowerRail.transform.SetParent(gunChassis.transform);
            lowerRail.transform.localPosition = new Vector3(0, -0.015f, 0.14f);
            lowerRail.transform.localScale = new Vector3(0.032f, 0.012f, 0.16f);
            lowerRail.GetComponent<Renderer>().sharedMaterial = mats.GunAccent;
            UnityEngine.Object.DestroyImmediate(lowerRail.GetComponent<Collider>());

            // 5. Central Emitter Core
            var barrelCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrelCore.name = "BarrelCore";
            barrelCore.transform.SetParent(gunChassis.transform);
            barrelCore.transform.localPosition = new Vector3(0, 0.008f, 0.14f);
            barrelCore.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            barrelCore.transform.localScale = new Vector3(0.018f, 0.07f, 0.018f);
            barrelCore.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(barrelCore.GetComponent<Collider>());

            // 6. Holographic Sight Display Screen
            var holoScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            holoScreen.name = "HoloScreen";
            holoScreen.transform.SetParent(gunChassis.transform);
            holoScreen.transform.localPosition = new Vector3(0, 0.038f, -0.08f);
            holoScreen.transform.localRotation = Quaternion.Euler(42f, 0, 0);
            holoScreen.transform.localScale = new Vector3(0.038f, 0.004f, 0.028f);
            holoScreen.GetComponent<Renderer>().sharedMaterial = mats.GlassHologram;
            UnityEngine.Object.DestroyImmediate(holoScreen.GetComponent<Collider>());

            // 7. Muzzle Point & Light
            var muzzlePointGo = new GameObject("MuzzlePoint");
            muzzlePointGo.transform.SetParent(gunChassis.transform);
            muzzlePointGo.transform.localPosition = new Vector3(0, 0.008f, 0.23f);

            var muzzleLight = muzzlePointGo.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 4.0f;
            muzzleLight.intensity = 0f;
            muzzleLight.color = new Color(0.0f, 0.95f, 1.0f);

            // 8. Laser Beam LineRenderer
            var laserBeam = gunRootGo.AddComponent<LineRenderer>();
            laserBeam.material = mats.GlowCyan;
            laserBeam.startWidth = 0.035f;
            laserBeam.endWidth = 0.012f;
            laserBeam.useWorldSpace = true;
            laserBeam.enabled = false;

            // 9. NeuralGunViewmodel Component
            var gunViewmodel = gunRootGo.AddComponent<NeuralGunViewmodel>();
            var gvSO = new SerializedObject(gunViewmodel);
            gvSO.FindProperty("chamberController").objectReferenceValue = chamberController;
            gvSO.FindProperty("gunChassis").objectReferenceValue = gunChassis.transform;
            gvSO.FindProperty("muzzlePoint").objectReferenceValue = muzzlePointGo.transform;
            gvSO.FindProperty("targetCore").objectReferenceValue = targetCore;
            gvSO.FindProperty("muzzleLight").objectReferenceValue = muzzleLight;
            gvSO.FindProperty("laserBeam").objectReferenceValue = laserBeam;
            gvSO.ApplyModifiedProperties();

            // Link muzzlePoint into NeuralPulseToolInteractor
            if (pulseTool != null)
            {
                var pulseToolSO = new SerializedObject(pulseTool);
                pulseToolSO.FindProperty("muzzlePoint").objectReferenceValue = muzzlePointGo.transform;
                pulseToolSO.ApplyModifiedProperties();
            }
        }

        private static LevelMaterials GenerateMaterials()
        {
            var mats = new LevelMaterials();

            // Dark Plating
            mats.DarkPlating = CreateOrUpdateMaterial("Mat_Space_DarkPlating", new Color(0.06f, 0.08f, 0.11f), 0.85f, 0.45f);

            // Emissive Signals & Activations
            mats.GlowCyan = CreateOrUpdateMaterial("Mat_Space_GlowCyan", new Color(0.0f, 0.8f, 1.0f), 0.2f, 0.9f, new Color(0.0f, 0.96f, 1.0f) * 2.5f);
            mats.GlowAmber = CreateOrUpdateMaterial("Mat_Space_GlowAmber", new Color(1.0f, 0.65f, 0.0f), 0.2f, 0.9f, new Color(1.0f, 0.7f, 0.0f) * 2.5f);
            mats.GlowEmerald = CreateOrUpdateMaterial("Mat_Space_GlowEmerald", new Color(0.0f, 0.9f, 0.45f), 0.2f, 0.9f, new Color(0.0f, 0.9f, 0.45f) * 2.8f);
            mats.GlowViolet = CreateOrUpdateMaterial("Mat_Space_GlowViolet", new Color(0.85f, 0.0f, 1.0f), 0.2f, 0.9f, new Color(0.85f, 0.0f, 1.0f) * 2.5f);

            // Translucent Glass Hologram
            mats.GlassHologram = CreateOrUpdateMaterial("Mat_Space_GlassHologram", new Color(0.02f, 0.15f, 0.25f, 0.45f), 0.1f, 0.8f, new Color(0.0f, 0.6f, 0.9f) * 0.8f, isTransparent: true);

            // Core Plasma
            mats.CorePlasma = CreateOrUpdateMaterial("Mat_Space_CorePlasma", new Color(0.0f, 0.85f, 1.0f), 0.1f, 0.95f, new Color(0.0f, 0.9f, 1.0f) * 3.2f);

            // Portal Curtain
            mats.PortalCurtain = CreateOrUpdateMaterial("Mat_Space_PortalCurtain", new Color(0.7f, 0.1f, 0.05f, 0.5f), 0.1f, 0.9f, new Color(0.8f, 0.15f, 0.05f) * 1.8f, isTransparent: true);

            // Asteroid Rock
            mats.Asteroid = CreateOrUpdateMaterial("Mat_Space_Asteroid", new Color(0.18f, 0.18f, 0.21f), 0.05f, 0.15f);

            // Gun Metal & Gun Accent
            mats.GunMetal = CreateOrUpdateMaterial("Mat_Space_GunMetal", new Color(0.09f, 0.11f, 0.14f), 0.92f, 0.65f);
            mats.GunAccent = CreateOrUpdateMaterial("Mat_Space_GunAccent", new Color(0.0f, 0.85f, 1.0f), 0.5f, 0.85f, new Color(0.0f, 0.9f, 1.0f) * 2.0f);

            // Stanchion Marker Glow
            mats.StanchionGlow = CreateOrUpdateMaterial("Mat_Space_StanchionGlow", new Color(0.0f, 0.9f, 1.0f), 0.1f, 0.8f, new Color(0.0f, 0.9f, 1.0f) * 2.0f);

            // Distant Planet & Planetary Rings
            mats.Planet = CreateOrUpdateMaterial("Mat_Space_Planet", new Color(0.12f, 0.08f, 0.22f), 0.1f, 0.85f, new Color(0.2f, 0.1f, 0.35f) * 0.8f);
            mats.PlanetRings = CreateOrUpdateMaterial("Mat_Space_PlanetRings", new Color(0.3f, 0.45f, 0.65f, 0.35f), 0.05f, 0.9f, new Color(0.1f, 0.3f, 0.5f) * 0.5f, isTransparent: true);

            return mats;
        }

        private static Material CreateOrUpdateMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emissionColor = null, bool isTransparent = false)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            // Set color for both Standard and URP
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor.Value);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            if (isTransparent)
            {
                // Standard shader transparency
                if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                // URP transparency properties
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f);
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
