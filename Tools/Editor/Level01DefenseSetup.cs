using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Convergence.Gameplay;
using Convergence.Presentation;

namespace Convergence.EditorTools
{
    public static partial class Level01SceneBuilder
    {
        static void WirePointDefense(
            GameObject controllers,
            ChamberController chamber,
            GatewayController gateway,
            NeuralState neural,
            ChamberOnboardingController onboarding,
            AudioHookManager audioHook,
            TextMesh banner,
            AudioSource voiceSource)
        {
            var director = controllers.AddComponent<AsteroidDefenseDirector>();
            var directorSO = new SerializedObject(director);
            directorSO.FindProperty("chamber").objectReferenceValue = chamber;
            directorSO.FindProperty("gateway").objectReferenceValue = gateway;
            directorSO.FindProperty("onboarding").objectReferenceValue = onboarding;
            directorSO.ApplyModifiedProperties();

            var tele = controllers.AddComponent<OpeningTeleprompter>();
            var teleSO = new SerializedObject(tele);
            teleSO.FindProperty("onboarding").objectReferenceValue = onboarding;
            teleSO.FindProperty("voiceSource").objectReferenceValue = voiceSource;
            teleSO.FindProperty("voiceClip").objectReferenceValue = LoadClip("vo_opening_neural_recording");
            teleSO.FindProperty("computerTick").objectReferenceValue = LoadClip("computerNoise_003");
            teleSO.FindProperty("banner").objectReferenceValue = banner;

            var bedGo = new GameObject("ShipBed");
            bedGo.transform.SetParent(controllers.transform, false);
            var bed = bedGo.AddComponent<AudioSource>();
            bed.spatialBlend = 0f;
            bed.loop = true;
            bed.playOnAwake = false;
            bed.volume = 0.18f;
            var bedClip = LoadClip("spaceEngineLow_001");
            teleSO.FindProperty("bedSource").objectReferenceValue = bed;
            teleSO.FindProperty("bedClip").objectReferenceValue = bedClip;
            teleSO.ApplyModifiedProperties();

            var voicePlayer = controllers.AddComponent<VoiceLinePlayer>();
            var voiceAudio = controllers.AddComponent<AudioSource>();
            voiceAudio.spatialBlend = 1f;
            voiceAudio.playOnAwake = false;
            var voiceSO = new SerializedObject(voicePlayer);
            voiceSO.FindProperty("voiceSource").objectReferenceValue = voiceAudio;
            voiceSO.FindProperty("banner").objectReferenceValue = banner;
            voiceSO.FindProperty("teleprompter").objectReferenceValue = tele;
            voiceSO.FindProperty("director").objectReferenceValue = director;
            voiceSO.FindProperty("chamber").objectReferenceValue = chamber;
            voiceSO.FindProperty("neuralState").objectReferenceValue = neural;
            string[] ids =
            {
                "vo_wave1_incoming", "vo_first_hit", "vo_cables_in", "vo_first_kill", "vo_shot_drone",
                "vo_hit_a", "vo_hit_b", "vo_hit_c", "vo_hull_half", "vo_reroute", "vo_stuck",
                "vo_solved", "vo_swarm", "vo_lesson", "vo_restored"
            };
            var slots = voiceSO.FindProperty("slots");
            slots.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                string assetPath = "Assets/_Project/Audio/Voice/" + ids[i] + ".txt";
                ReadVoice(assetPath, out string speech, out float start, out float end);
                var element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = ids[i];
                element.FindPropertyRelative("clip").objectReferenceValue = LoadClip(ids[i]);
                element.FindPropertyRelative("text").stringValue = speech;
                element.FindPropertyRelative("start").floatValue = start;
                element.FindPropertyRelative("end").floatValue = end;
            }
            voiceSO.ApplyModifiedProperties();

            var aura = banner != null ? banner.GetComponent<AuraSubtitles>() : null;
            if (aura != null)
            {
                var auraSO = new SerializedObject(aura);
                auraSO.FindProperty("teleprompter").objectReferenceValue = tele;
                auraSO.FindProperty("voicePlayer").objectReferenceValue = voicePlayer;
                auraSO.ApplyModifiedProperties();
            }

            var hookSO = new SerializedObject(audioHook);
            hookSO.FindProperty("cableClip").objectReferenceValue = LoadClip("impactMetal_000");
            hookSO.FindProperty("leverClip").objectReferenceValue = LoadClip("doorClose_000");
            hookSO.FindProperty("stepClip").objectReferenceValue = LoadClip("click_001");
            hookSO.FindProperty("successClip").objectReferenceValue = LoadClip("confirmation_001");
            hookSO.FindProperty("failureClip").objectReferenceValue = LoadClip("lowFrequency_explosion_001");
            hookSO.FindProperty("playEvaluationSounds").boolValue = false;
            hookSO.FindProperty("shipBedClip").objectReferenceValue = bedClip;
            hookSO.FindProperty("shipBedSource").objectReferenceValue = bed;
            hookSO.ApplyModifiedProperties();

            var impact = controllers.AddComponent<ShipImpactSequence>();
            var impactSO = new SerializedObject(impact);
            impactSO.FindProperty("onboarding").objectReferenceValue = onboarding;
            impactSO.FindProperty("director").objectReferenceValue = director;
            var neuron = GameObject.Find("ClassicNeuralNetwork_3D");
            if (neuron != null) impactSO.FindProperty("neuron").objectReferenceValue = neuron.GetComponent<NeuronMachineVisual>();
            var field = GameObject.Find("FloatingAsteroidField");
            if (field != null) impactSO.FindProperty("asteroidField").objectReferenceValue = field.transform;
            var sparks = GameObject.Find("EmergencySparkParticles");
            if (sparks != null) impactSO.FindProperty("sparks").objectReferenceValue = sparks.GetComponent<ParticleSystem>();
            impactSO.FindProperty("boomSource").objectReferenceValue = voiceAudio;
            impactSO.FindProperty("boomClip").objectReferenceValue = LoadClip("explosionCrunch_000");

            var synthGo = new GameObject("AlarmSynth");
            synthGo.transform.SetParent(controllers.transform, false);
            var synthAudio = synthGo.AddComponent<AudioSource>();
            synthAudio.spatialBlend = 0f;
            synthAudio.playOnAwake = false;
            synthAudio.volume = 0.4f;
            var synth = synthGo.AddComponent<RetroAudioSynthesizer>();
            var synthSO = new SerializedObject(synth);
            synthSO.FindProperty("volume").floatValue = 0f;
            synthSO.ApplyModifiedProperties();
            impactSO.FindProperty("synth").objectReferenceValue = synth;

            var alarmA = CreateAlarmLight("AlarmLight_Port", new Vector3(-2.2f, 2.4f, 2.2f));
            var alarmB = CreateAlarmLight("AlarmLight_Starboard", new Vector3(2.2f, 2.4f, 2.2f));
            var alarms = impactSO.FindProperty("alarmLights");
            alarms.arraySize = 2;
            alarms.GetArrayElementAtIndex(0).objectReferenceValue = alarmA;
            alarms.GetArrayElementAtIndex(1).objectReferenceValue = alarmB;
            impactSO.ApplyModifiedProperties();

            var defense = controllers.AddComponent<PointDefenseVisual>();
            var defenseSO = new SerializedObject(defense);
            defenseSO.FindProperty("director").objectReferenceValue = director;
            defenseSO.FindProperty("chamber").objectReferenceValue = chamber;
            var muzzle = GameObject.Find("LaserMuzzle");
            if (muzzle != null) defenseSO.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            var beamGo = new GameObject("DefenseBeam");
            beamGo.transform.SetParent(controllers.transform, false);
            var beam = beamGo.AddComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.startWidth = 0.06f;
            beam.endWidth = 0.02f;
            beam.enabled = false;
            var beamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Glow_Amber.mat");
            if (beamMat != null) beam.sharedMaterial = beamMat;
            defenseSO.FindProperty("beam").objectReferenceValue = beam;
            defenseSO.FindProperty("explosion").objectReferenceValue = CreateBurst("DefenseExplosion", new Color(1f, 0.55f, 0.15f));
            defenseSO.FindProperty("hullSparks").objectReferenceValue = sparks != null ? sparks.GetComponent<ParticleSystem>() : null;
            var gate = GameObject.Find("ScanGate");
            if (gate != null)
            {
                var renderers = gate.GetComponentsInChildren<Renderer>();
                var gateProp = defenseSO.FindProperty("scanGate");
                gateProp.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++) gateProp.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
            var console = GameObject.Find("TactileEngineeringWorkstation");
            Transform consoleTransform = console != null ? console.transform : controllers.transform;
            defenseSO.FindProperty("rockLamp").objectReferenceValue = CreateLamp(consoleTransform, "Lamp_ROCK", new Vector3(-0.32f, 1.05f, 0.2f), new Color(0.85f, 0.45f, 0.2f));
            defenseSO.FindProperty("iceLamp").objectReferenceValue = CreateLamp(consoleTransform, "Lamp_ICE", new Vector3(0f, 1.05f, 0.2f), new Color(0.45f, 0.8f, 1f));
            defenseSO.FindProperty("fireLamp").objectReferenceValue = CreateLamp(consoleTransform, "Lamp_FIRE", new Vector3(0.32f, 1.05f, 0.2f), new Color(1f, 0.25f, 0.15f));
            var flashes = defenseSO.FindProperty("flashLights");
            flashes.arraySize = 2;
            flashes.GetArrayElementAtIndex(0).objectReferenceValue = alarmA;
            flashes.GetArrayElementAtIndex(1).objectReferenceValue = alarmB;
            defenseSO.FindProperty("audioSource").objectReferenceValue = voiceAudio;
            defenseSO.FindProperty("laserClip").objectReferenceValue = LoadClip("laserLarge_001");
            defenseSO.FindProperty("explosionClip").objectReferenceValue = LoadClip("explosionCrunch_002");
            defenseSO.FindProperty("impactClip").objectReferenceValue = LoadClip("impactMetal_000");
            defenseSO.FindProperty("shieldClip").objectReferenceValue = LoadClip("forceField_001");
            defenseSO.ApplyModifiedProperties();
        }

        static Light CreateAlarmLight(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 0.2f;
            light.color = new Color(1f, 0.2f, 0.12f);
            return light;
        }

        static Light CreateLamp(Transform parent, string name, Vector3 localPos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.06f;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 0.8f;
            light.intensity = 0.15f;
            light.color = color;
            return light;
        }

        static ParticleSystem CreateBurst(string name, Color color)
        {
            var go = new GameObject(name);
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = color;
            main.startLifetime = 0.45f;
            main.startSpeed = 3.5f;
            main.startSize = 0.25f;
            main.playOnAwake = false;
            main.loop = false;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        static AudioClip LoadClip(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null && clip.name == name) return clip;
                }
            }
            Debug.LogWarning("[Level01SceneBuilder] Missing audio clip " + name);
            return null;
        }

        static void ReadVoice(string assetPath, out string speech, out float start, out float end)
        {
            speech = string.Empty;
            start = 0f;
            end = 2f;
            if (!File.Exists(assetPath)) return;
            string text = File.ReadAllText(assetPath).Replace("\r\n", "\n");
            Match match = Regex.Match(text, @"Speech:\s*([0-9.]+)\s*-\s*([0-9.]+)");
            if (match.Success)
            {
                start = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                end = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            }
            string[] parts = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) speech = parts[parts.Length - 1].Trim();
        }

        static List<DataTargetReceptor> CreateDataTargetPods(Transform parent, LevelMaterials mats)
        {
            var podsRoot = new GameObject("DataTargetReceptorPods");
            podsRoot.transform.SetParent(parent);

            var receptors = new List<DataTargetReceptor>();
            var cases = new[]
            {
                new { Index = 0, Label = "Drone (0,0)", Title = "Repair Drone", Role = "LET IT DOCK", X1 = 0.0, X2 = 0.0, Expected = 0.0, Spawn = new Vector3(-4.2f, 2.2f, 34f), Perimeter = new Vector3(-1.4f, 1.9f, 4.8f) },
                new { Index = 1, Label = "Comet (0,1)", Title = "Icy Comet", Role = "FIRE", X1 = 0.0, X2 = 1.0, Expected = 1.0, Spawn = new Vector3(-1.4f, 3.4f, 34f), Perimeter = new Vector3(-0.45f, 2.4f, 4.8f) },
                new { Index = 2, Label = "Rock (1,0)", Title = "Rocky Asteroid", Role = "FIRE", X1 = 1.0, X2 = 0.0, Expected = 1.0, Spawn = new Vector3(1.6f, 2.6f, 34f), Perimeter = new Vector3(0.45f, 2.1f, 4.8f) },
                new { Index = 3, Label = "Both (1,1)", Title = "Rock-and-Ice Chunk", Role = "FIRE", X1 = 1.0, X2 = 1.0, Expected = 1.0, Spawn = new Vector3(4.6f, 3.8f, 34f), Perimeter = new Vector3(1.4f, 2.55f, 4.8f) }
            };

            foreach (var c in cases)
            {
                var podGo = new GameObject($"DataTargetPod_Case{c.Index + 1}");
                podGo.transform.SetParent(podsRoot.transform);
                podGo.transform.position = c.Spawn;

                var body = new GameObject("FloatingTargetBody");
                body.transform.SetParent(podGo.transform, false);

                Renderer core = null;
                Renderer sub = null;
                Color aura = new Color(0.6f, 0.75f, 1f);
                if (c.Index == 0)
                {
                    core = MakeBody(body.transform, PrimitiveType.Capsule, "RepairDrone", new Vector3(0.35f, 0.28f, 0.35f), Quaternion.Euler(90f, 0f, 0f), mats.GlowCyan);
                    sub = MakeBody(body.transform, PrimitiveType.Cylinder, "DroneRing", new Vector3(0.7f, 0.02f, 0.7f), Quaternion.identity, mats.GlowEmerald);
                    aura = new Color(0.3f, 0.95f, 0.7f);
                }
                else if (c.Index == 1)
                {
                    core = MakeBody(body.transform, PrimitiveType.Sphere, "IcyComet", Vector3.one * 1.55f, Quaternion.identity, mats.EarthWater);
                    sub = MakeBody(body.transform, PrimitiveType.Sphere, "IceHalo", Vector3.one * 1.9f, Quaternion.identity, mats.EarthAtmosphere);
                    aura = new Color(0.55f, 0.85f, 1f);
                }
                else if (c.Index == 2)
                {
                    core = MakeBody(body.transform, PrimitiveType.Cube, "RockyAsteroid", new Vector3(1.7f, 1.35f, 1.55f), Quaternion.Euler(18f, 32f, 12f), mats.SpaceAsteroid);
                    sub = MakeBody(body.transform, PrimitiveType.Cube, "RockChip", new Vector3(0.55f, 0.4f, 0.45f), Quaternion.Euler(40f, 10f, 20f), mats.SpaceAsteroid);
                    sub.transform.localPosition = new Vector3(0.7f, 0.35f, 0.2f);
                    aura = new Color(0.85f, 0.45f, 0.22f);
                }
                else
                {
                    core = MakeBody(body.transform, PrimitiveType.Sphere, "ChunkRock", Vector3.one * 1.7f, Quaternion.identity, mats.SpaceAsteroid);
                    sub = MakeBody(body.transform, PrimitiveType.Sphere, "ChunkIce", Vector3.one * 1.05f, Quaternion.identity, mats.EarthWater);
                    sub.transform.localPosition = new Vector3(0.55f, 0.35f, 0.2f);
                    aura = new Color(0.75f, 0.85f, 1f);
                }

                var lightGo = new GameObject("AuraLight");
                lightGo.transform.SetParent(podGo.transform, false);
                var auraLight = lightGo.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.range = 4.5f;
                auraLight.intensity = 1.4f;
                auraLight.color = aura;

                var receptor = podGo.AddComponent<DataTargetReceptor>();
                var recSO = new SerializedObject(receptor);
                recSO.FindProperty("caseIndex").intValue = c.Index;
                recSO.FindProperty("caseLabel").stringValue = c.Label;
                recSO.FindProperty("targetTitle").stringValue = c.Title;
                recSO.FindProperty("threatRole").stringValue = c.Role;
                recSO.FindProperty("inputX1").doubleValue = c.X1;
                recSO.FindProperty("inputX2").doubleValue = c.X2;
                recSO.FindProperty("expectedOutput").doubleValue = c.Expected;
                recSO.FindProperty("isInFlight").boolValue = false;
                recSO.ApplyModifiedProperties();

                var visual = podGo.AddComponent<DataTargetVisual>();
                var visSO = new SerializedObject(visual);
                visSO.FindProperty("receptor").objectReferenceValue = receptor;
                visSO.FindProperty("floatingTargetBody").objectReferenceValue = body.transform;
                visSO.FindProperty("coreRenderer").objectReferenceValue = core;
                visSO.FindProperty("subRenderer").objectReferenceValue = sub;
                visSO.FindProperty("auraLight").objectReferenceValue = auraLight;
                visSO.FindProperty("spawnPosition").vector3Value = c.Spawn;
                visSO.FindProperty("perimeterPosition").vector3Value = c.Perimeter;
                visSO.ApplyModifiedProperties();
                receptors.Add(receptor);
            }

            var gate = new GameObject("ScanGate");
            gate.transform.SetParent(podsRoot.transform, false);
            gate.transform.position = new Vector3(0f, 2.2f, 15f);
            MakeGatePost(gate.transform, new Vector3(-3.2f, 0f, 0f), mats.GlowCyan);
            MakeGatePost(gate.transform, new Vector3(3.2f, 0f, 0f), mats.GlowCyan);
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "ScanBar";
            bar.transform.SetParent(gate.transform, false);
            bar.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            bar.transform.localScale = new Vector3(6.4f, 0.08f, 0.08f);
            bar.GetComponent<Renderer>().sharedMaterial = mats.GlowCyan;
            UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());

            var muzzle = new GameObject("LaserMuzzle");
            muzzle.transform.SetParent(podsRoot.transform, false);
            muzzle.transform.position = new Vector3(0f, 1.35f, 5.0f);
            return receptors;
        }

        static Renderer MakeBody(Transform parent, PrimitiveType type, string name, Vector3 scale, Quaternion rotation, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.transform.localRotation = rotation;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        const string CaptureKey = "Convergence.CaptureOpeningPreview";
        const string CaptureDir = "Logs/opening-preview";
        static readonly double[] ShotTimes = { 1.0, 7.0, 13.0 };
        static double _captureArmedAt;
        static int _nextShot;

        /// <summary>
        /// Batchmode entry point. Enters Play mode and saves three stills of the opening.
        /// </summary>
        public static void CaptureOpeningPreview()
        {
            SessionState.SetBool(CaptureKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Level01_AwakeningGate.unity");
            Debug.Log("[OpeningPreview] Scene loaded. Entering play mode.");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void ResumeOpeningCapture()
        {
            if (!SessionState.GetBool(CaptureKey, false)) return;
            EditorApplication.playModeStateChanged -= OnOpeningPlayState;
            EditorApplication.playModeStateChanged += OnOpeningPlayState;
            if (EditorApplication.isPlaying) ArmOpeningCapture();
        }

        static void OnOpeningPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) ArmOpeningCapture();
        }

        static void ArmOpeningCapture()
        {
            _captureArmedAt = EditorApplication.timeSinceStartup;
            _nextShot = 0;
            EditorApplication.update -= TickOpeningCapture;
            EditorApplication.update += TickOpeningCapture;
            Debug.Log("[OpeningPreview] Capture armed.");
        }

        static void TickOpeningCapture()
        {
            if (!SessionState.GetBool(CaptureKey, false))
            {
                EditorApplication.update -= TickOpeningCapture;
                return;
            }

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogWarning("[OpeningPreview] No graphics device. Cannot render the scene.");
                SessionState.SetBool(CaptureKey, false);
                EditorApplication.Exit(2);
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - _captureArmedAt;
            if (_nextShot < ShotTimes.Length && elapsed >= ShotTimes[_nextShot])
            {
                Directory.CreateDirectory(CaptureDir);
                string path = Path.Combine(CaptureDir, "opening-" + _nextShot + ".png");
                SaveCameraPng(path);
                Debug.Log("[OpeningPreview] Saved " + path + " at " + elapsed.ToString("0.0") + "s");
                _nextShot++;
            }

            if (_nextShot >= ShotTimes.Length || elapsed > 22.0)
            {
                EditorApplication.update -= TickOpeningCapture;
                SessionState.SetBool(CaptureKey, false);
                EditorApplication.Exit(0);
            }
        }

        static void SaveCameraPng(string path)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[OpeningPreview] No camera.");
                return;
            }

            const int width = 1280;
            const int height = 720;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }

        static void MakeGatePost(Transform parent, Vector3 local, Material material)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "ScanPost";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = local;
            post.transform.localScale = new Vector3(0.12f, 2.6f, 0.12f);
            post.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
        }
    }
}
