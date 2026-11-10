using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Hub;
using Convergence.Presentation.Hub;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Level2;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Builds the level hub (ADR-024): Assets/Scenes/LevelHub.unity, build scene 0, so the game starts there. A seat with the XR rig
    /// (the ride's lasers, the desk mouse, the seat that re-centres the eye) on a dark, starry plaza (Level 1's night sky), and the hub view, which builds one card
    /// per entry of Assets/ScriptableObjects/LevelCatalog.asset when the scene loads. The catalog is filled with Level 1 and Level 2 the
    /// first time and never overwritten, so levels added by hand stay. Needs Level 2 built first (its materials and theme). Development
    /// tooling only.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string HubScenePath = "Assets/Scenes/LevelHub.unity";
        private const string CatalogPath = "Assets/ScriptableObjects/LevelCatalog.asset";

        [MenuItem("Convergence/Build Level Hub")]
        public static void BuildHub()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null) throw new FileNotFoundException("LiberationSans SDF missing at " + FontPath);
            var theme = AssetDatabase.LoadAssetAtPath<RideTheme>(Theme2Path);
            if (theme == null) throw new FileNotFoundException(Theme2Path + " is missing: build Level 2 first (Convergence/Build Word Machine).");
            Material HubMat(string name)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");
                if (m == null) throw new FileNotFoundException(MatDir + "/" + name + ".mat is missing: build Level 2 first.");
                return m;
            }

            // After the new scene: opening it unloads assets nothing references yet, which left the scene's catalog reference empty.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LevelCatalog catalog = HubCatalog();
            theme = AssetDatabase.LoadAssetAtPath<RideTheme>(Theme2Path);
            // The night: Level 1's sky shader (stars, a horizon glow, a grid floor); the hub view sets its colours when it loads.
            Color night = new Color(0.07f, 0.1f, 0.24f);
            var look = new RideTheme.RegionLook { background = night, fog = night, fogDensity = 0.012f };
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = night;
            RenderSettings.skybox = SkyMaterial();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = theme.voidNavy;

            // The seat: the rig on its anchor, re-centred to a seated eye 1.3 m up (as in the pod), the desk mouse, the hand lasers.
            var seat = new GameObject("Seat");
            Transform anchor = Make("SeatAnchor", seat.transform, Vector3.zero).transform;
            var seatScript = seat.AddComponent<PodSeat>();
            Ref(seatScript, "seatAnchor", anchor);
            var desk = seat.AddComponent<DesktopSeat>();
            Float(desk, "startPitch", 3f); // the cards are at eye height, not on a dash
            GameObject rig = MountRig(seat, look);
            rig.GetComponentInChildren<Camera>().farClipPlane = 300f;
            new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            Mats.Clear();
            Mats["Laser"] = HubMat("Laser");
            WireLasers(rig);

            // The hub: Level 2's kit (fonts, shapes, solids) and the view that builds the meadow and the cards.
            var hub = new GameObject("LevelHub");
            var kit = hub.AddComponent<Level2Kit>();
            Ref(kit, "glow", HubMat("L2Soft"));
            Ref(kit, "ring", HubMat("L2Ring"));
            Ref(kit, "beam", HubMat("L2Beam"));
            Ref(kit, "shapes", HubMat("ExplainerFlat"));
            Ref(kit, "solid", HubMat("L2Solid"));
            Ref(kit, "font", _font);
            Ref(kit, "serif", ExplainerFont());
            Ref(kit, "outline", HubMat("L2TextOutline"));
            Ref(kit, "serifOutline", HubMat("L2SerifOutline"));
            Ref(kit, "theme", theme);

            var sfx = hub.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            var music = Make("Music", hub.transform, Vector3.zero).AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.loop = true;
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);

            var view = hub.AddComponent<LevelHubView>();
            Ref(view, "catalog", catalog);
            Ref(view, "coverMaterial", HubMat("BootCover"));
            Ref(view, "sfx", sfx);
            Ref(view, "music", music);
            Ref(view, "hoverClip", AssetDatabase.LoadAssetAtPath<AudioClip>(KenneyDir + "click_001.wav"));
            Ref(view, "chooseClip", AssetDatabase.LoadAssetAtPath<AudioClip>(KenneyDir + "confirmation_001.wav"));

            if (new SerializedObject(view).FindProperty("catalog").objectReferenceValue == null) throw new System.InvalidOperationException("The hub lost its catalog reference.");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), HubScenePath);
            OrderBuildScenes();
            Debug.Log("[NeuralRideBuilder] Built " + HubScenePath + " with " + catalog.Levels.Length + " level cards.");
        }

        /// <summary>The catalog, filled with the two levels the first time (never overwritten: levels added by hand stay).</summary>
        private static LevelCatalog HubCatalog()
        {
            LevelCatalog catalog = LoadOrCreate<LevelCatalog>(CatalogPath);
            if (catalog.Levels.Length > 0) return catalog;
            catalog.SetLevels(new[]
            {
                new LevelCatalog.Entry
                {
                    badge = "LEVEL 1", title = "HOW A MACHINE\nLEARNS TO SEE",
                    tagline = "Ride a pod through a real neural network. Paint a 3, blast the 3s, then train it yourself.",
                    stops = new[] { "PIXELS", "NEURON", "NETWORK", "BACKPROP", "TRAINING" },
                    scene = "NeuralRide", minutes = 12f, accent = new Color(0.31f, 0.76f, 1f),
                },
                new LevelCatalog.Entry
                {
                    badge = "LEVEL 2", title = "INSIDE THE\nWORD MACHINE",
                    tagline = "Bike through a real GPT-2. Chop words, sort a galaxy of meaning, bet on the next word, steer a story.",
                    stops = new[] { "TOKENS", "MEANING", "ATTENTION", "PREDICTION", "WRITING" },
                    scene = "WordMachine", minutes = 12f, accent = new Color(1f, 0.82f, 0.38f),
                },
                new LevelCatalog.Entry
                {
                    badge = "LEVEL 3", title = "COMING\nSOON",
                    tagline = "A new ride is being built. Ask what you would like to see inside an AI next!",
                    scene = "", accent = new Color(0.62f, 0.45f, 1f),
                },
            });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(CatalogPath);
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath); // the saved asset, so the scene's reference to it holds
        }

        /// <summary>
        /// The hub first (the game starts there), then Level 1 and Level 2, then anything else already in the build. Every level builder
        /// calls this, so rebuilding one level never pushes the hub out of scene 0.
        /// </summary>
        private static void OrderBuildScenes()
        {
            var front = new List<string>();
            if (File.Exists(HubScenePath)) front.Add(HubScenePath);
            if (File.Exists(ScenePath)) front.Add(ScenePath);
            if (File.Exists(Scene2Path)) front.Add(Scene2Path);
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in front) scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (!front.Contains(s.path)) scenes.Add(s);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
