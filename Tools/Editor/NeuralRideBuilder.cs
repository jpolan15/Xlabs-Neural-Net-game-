using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Builds Level 1, "How a machine learns to read a digit" (TASK_LEVEL1_DIGITS_RESTRUCTURE.md): theme, materials,
    /// the Pod prefab, and Assets/Scenes/NeuralRide.unity as build scene 0, with the track and the eight regions built
    /// from the baked MNIST data (Assets/_Project/Data/Level1/level1_digits.json), so what the rider sees is what Core
    /// computes and the edit-mode captures show the real thing. Every visible object ends up under the Pod, the Track
    /// or a Region_N root, and the build fails if anything else renders, a panel is blocked, the audio is too loud, the
    /// narration's numbers disagree with the bake, or a set piece has no model sheet. Development tooling only.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string ScenePath = "Assets/Scenes/NeuralRide.unity";
        private const string PrefabDir = "Assets/Prefabs/NeuralRide";
        private const string MatDir = "Assets/Materials/NeuralRide";
        private const string ThemePath = "Assets/ScriptableObjects/RideTheme.asset";
        private const string IconDir = "Assets/Materials/Icon_Chamber01_";
        private const string VoiceDir = "Assets/_Project/Audio/Voice/";
        private const string MusicPath = "Assets/_Project/Audio/Music/telstar.wav";
        private const string RigPrefab = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static RideTheme _theme;
        private static TMP_FontAsset _font;
        private static Level1Data _data;
        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        [MenuItem("Convergence/Build Neural Ride")]
        public static void Build()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null) throw new FileNotFoundException("LiberationSans SDF missing at " + FontPath);
            foreach (string dir in new[] { PrefabDir, MatDir })
            {
                if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);
            }

            AssetDatabase.Refresh();
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level1Data.AssetPath);
            if (dataAsset == null) throw new FileNotFoundException(Level1Data.AssetPath + " is missing. Run ArtSource/training/bake_level1.py.");
            _data = Level1Data.Parse(dataAsset.text);

            ImportMusic();
            ImportAudio();
            _theme = LoadTheme();
            Mats.Clear();
            MakeMaterials();
            BuildNarration();
            AssertNarrationMatchesBake();

            foreach (string stale in new[] { PrefabDir + "/Station.prefab", PrefabDir + "/Track.prefab" })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(stale) != null) AssetDatabase.DeleteAsset(stale);
            }

            GameObject pod = SavePrefab(BuildPod(), PrefabDir + "/Pod.prefab");
            BuildScene(pod, dataAsset);
            Debug.Log("[NeuralRideBuilder] Built " + ScenePath);
        }

        // ---------- assets ----------

        private static RideTheme LoadTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<RideTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<RideTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.amber = Hex("#FFB547");
            theme.paper = Hex("#FBF6EC");
            theme.ink = Hex("#1B1B2F");
            theme.pillar = Hex("#F5F7FF");
            theme.networkBlue = Hex("#4FC3FF");
            theme.negativeWeight = Hex("#B388FF");
            theme.wrongRed = Hex("#FF3B3B");
            theme.rewindMagenta = Hex("#FF2BD6");
            theme.valleyPink = Hex("#FF6F91");
            theme.valleyViolet = Hex("#6B2D8C");
            theme.gold = Hex("#FFD166");
            theme.right = Hex("#66FF9E");
            theme.networkDim = 0.3f;

            // Each region its own sky and its topic's name on the dash. No region's sky is orange (orange = press this).
            // The dock is the blackboard's own dark navy (a shade lighter), so the lit page reads like 3Blue1Brown's figures
            // on the dark and the unfold does not change the sky.
            theme.regions = new[]
            {
                Look("MEET PIXEL", "#0E1118", "#0E1118", 0.010f),
                Look("PIXELS", "#030407", "#030407", 0.018f),
                Look("NEURON", "#140B04", "#1C1006", 0.020f),
                Look("NEURAL NETWORK", "#061024", "#061024", 0.011f),
                Look("LOSS", "#1C0407", "#2C060A", 0.014f),
                Look("BACKPROPAGATION", "#16041C", "#22062C", 0.013f),
                Look("GRADIENT DESCENT", "#3B1640", "#6B2D5E", 0.009f),
                Look("TRAINING", "#140F04", "#1E1606", 0.011f),
            };

            // The explainers' blackboard and palette (TASK_LEVEL1_V4_EXPLAINERS section 2).
            theme.blackboard = Look("", "#0B0E14", "#0B0E14", 0.002f);
            theme.explainBlue = Hex("#58C4DD");
            theme.explainYellow = Hex("#F4D345");
            theme.explainGreen = Hex("#83C167");
            theme.explainRed = Hex("#FC6255");
            theme.explainWhite = Hex("#ECECEC");
            theme.explainGrey = Hex("#888888");
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static RideTheme.RegionLook Look(string tag, string background, string fog, float density)
        {
            return new RideTheme.RegionLook { tag = tag, background = Hex(background), fog = Hex(fog), fogDensity = density };
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static void ImportMusic()
        {
            var importer = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
            if (importer == null) { Debug.LogWarning("[NeuralRideBuilder] No music at " + MusicPath + "; the ride will be silent."); return; }
            var s = importer.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.Streaming && s.compressionFormat == AudioCompressionFormat.Vorbis && !s.preloadAudioData) return;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.preloadAudioData = false;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        private static Texture2D GlowTexture()
        {
            string path = MatDir + "/Glow.png";
            if (!File.Exists(path))
            {
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * a;
                        tex.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                }

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // The dark lever slot with its dim cyan border, as one texture, so a lever slot is one draw instead of two.
        private static Texture2D SlotTexture()
        {
            string path = MatDir + "/SlotFrame.png";
            if (!File.Exists(path))
            {
                const int w = 32, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                        bool border = edge < 5;
                        Color c = border ? new Color(_theme.cyan.r * 0.35f, _theme.cyan.g * 0.35f, _theme.cyan.b * 0.35f, 0.9f) : new Color(0.02f, 0.03f, 0.05f, 1f);
                        tex.SetPixel(x, y, c);
                    }
                }

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private enum Kind { Opaque, Alpha, Additive }

        private static Material M(string name, Color c, Kind kind = Kind.Opaque, Texture tex = null)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = Shader.Find("Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", c);
            m.SetTexture("_BaseMap", tex);
            if (kind == Kind.Opaque)
            {
                m.SetFloat("_Surface", 0);
                m.SetFloat("_ZWrite", 1);
                m.SetOverrideTag("RenderType", "Opaque");
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = -1;
            }
            else
            {
                bool add = kind == Kind.Additive;
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", add ? 2 : 0);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", add ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", add ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.SetFloat("_Cull", 0);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                // Glows draw before the text and icons on them; transparent objects sort by the distance of their centres.
                int offset = name == "Plate" ? -10 : name.StartsWith("Glow") ? -20 : 0;
                m.SetFloat("_QueueOffset", offset);
                m.renderQueue = (int)RenderQueue.Transparent + offset;
            }

            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        // Convergence/HoloLit: unlit cost, with a fixed key light and a Fresnel rim for depth. Opaque parts only.
        private static Material H(string name, Color c, float shade = 0.85f, float rim = 0.55f, Color? rimColor = null, Color? emission = null)
        {
            return Lit(name, "Convergence/HoloLit", c, shade, rim, rimColor, emission);
        }

        private static Material Lit(string name, string shaderName, Color c, float shade, float rim, Color? rimColor, Color? emission)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new FileNotFoundException("Shader " + shaderName + " is missing. Is it imported from Assets/Shaders?");
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetColor("_BaseColor", c);
            m.SetColor("_RimColor", rimColor ?? _theme.cyan);
            m.SetFloat("_RimStrength", rim);
            m.SetFloat("_Shade", shade);
            m.SetColor("_Emission", emission ?? Color.black);
            m.renderQueue = -1;
            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        // Convergence/HoloTiles: HoloLit with vertex colour, for the combined tile and pillar meshes.
        private static Material Tiles(string name, Color baseColor, float glow, Color rim, float rimStrength = 0.35f)
        {
            Material m = Lit(name, "Convergence/HoloTiles", baseColor, 0.7f, rimStrength, rim, Color.black);
            m.SetFloat("_Glow", glow);
            return m;
        }

        private static Material Icon(string name, string file, Color tint)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconDir + file + ".png");
            return M("Icon_" + name, tint, Kind.Alpha, tex);
        }

        private static void MakeMaterials()
        {
            RideTheme t = _theme;
            Texture2D glow = GlowTexture();
            // The pod: HoloLit, depth from a key light and a rim, no real-time lights.
            H("PodDark", new Color(0.075f, 0.105f, 0.175f), 1f, 1.1f);
            H("PodMid", new Color(0.14f, 0.19f, 0.30f), 1f, 1.0f);
            M("Screen", new Color(0.01f, 0.03f, 0.07f));
            M("SlotFrame", Color.white, Kind.Alpha, SlotTexture());
            H("Cyan", t.cyan, 0.30f, 0.5f, null, t.cyan * 0.30f);
            H("CyanDim", t.cyan * 0.35f, 0.30f, 0.3f);
            H("Amber", t.amber, 0.30f, 0.5f, t.amber, t.amber * 0.35f);
            H("Orange", t.orange, 0.45f, 0.65f, new Color(1f, 0.78f, 0.5f), t.orange * 0.15f);
            H("Locked", t.locked, 0.50f, 0.4f, t.locked * 1.5f);
            H("White", t.offWhite, 0.30f, 0.3f, Color.white, t.offWhite * 0.2f);
            H("Slate", new Color(0.10f, 0.12f, 0.17f), 0.9f, 0.5f, t.locked);
            M("GlowCyan", new Color(t.cyan.r, t.cyan.g, t.cyan.b, 0.55f), Kind.Additive, glow);
            M("GlowOrange", new Color(t.orange.r, t.orange.g, t.orange.b, 0.7f), Kind.Additive, glow);
            M("GlowAmber", new Color(t.amber.r, t.amber.g, t.amber.b, 0.7f), Kind.Additive, glow);
            M("GlowWhite", new Color(1f, 1f, 1f, 0.8f), Kind.Additive, glow);
            M("GlowPink", new Color(t.valleyPink.r, t.valleyPink.g, t.valleyPink.b, 0.6f), Kind.Additive, glow);
            M("GlowGold", new Color(t.gold.r, t.gold.g, t.gold.b, 0.7f), Kind.Additive, glow);
            M("Plate", new Color(0.01f, 0.03f, 0.07f, 0.82f), Kind.Alpha);
            // Reading boards are solid: the networks' additive lines draw after transparent plates and would cross the text.
            M("PlateSolid", new Color(0.012f, 0.032f, 0.072f));
            M("PlateWarm", new Color(0.07f, 0.045f, 0.02f));
            Icon("Check", "Check", t.right);
            Icon("Cross", "Cross", t.wrongRed);

            // Level 1.
            Tiles("Tiles", Color.white, 0.55f, t.cyan, 0.25f);
            Tiles("TilesBright", Color.white, 0.9f, Color.white, 0.2f);
            Tiles("FieldTiles", new Color(0.85f, 0.88f, 0.92f), 0.22f, t.explainBlue, 0.55f);
            H("NeuronAmber", t.amber * 0.8f, 0.5f, 0.9f, Color.white, t.amber * 0.55f);
            H("SumFires", t.amber, 0.3f, 0.4f, Color.white, t.amber * 0.6f);
            H("SumQuiet", new Color(0.30f, 0.33f, 0.40f), 0.6f, 0.3f, t.locked);
            H("LineMark", Color.white, 0.2f, 0.2f, Color.white, Color.white * 0.6f);
            H("Deck", new Color(0.12f, 0.06f, 0.12f), 0.9f, 0.6f, t.valleyPink);
            H("HopPost", t.valleyPink, 0.4f, 0.5f, Color.white, t.valleyPink * 0.5f);
            H("Sun", new Color(1f, 0.55f, 0.65f), 0f, 0f, Color.white, new Color(1f, 0.45f, 0.6f));
            MakePageMaterials();
            MakeGameMaterials();
            MakeNetworkMaterials();
            MakeValleyMaterials();
            MakeExplainerMaterials();
            AssetDatabase.SaveAssets();
        }

        // ---------- helpers ----------

        private static GameObject Make(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, string mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mats[mat];
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        private static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, string mat)
        {
            return Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size.x, size.y, 1f), mat);
        }

        /// <summary>A solid board with a back: a thin box, so text on its face is hidden from behind instead of reading mirrored.</summary>
        private static GameObject Board(string name, Transform parent, Vector3 pos, Vector2 size, string mat, float depth = 0.03f)
        {
            return Prim(PrimitiveType.Cube, name, parent, pos + new Vector3(0, 0, depth * 0.5f + 0.005f), new Vector3(size.x, size.y, depth), mat);
        }

        private static TextMeshPro Text(string name, Transform parent, Vector3 pos, string text, float meters, Color color,
            float width = 1f, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var go = Make(name, parent, pos);
            var t = go.AddComponent<TextMeshPro>();
            t.font = _font;
            t.fontSize = meters * 10f;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = new Vector2(width, meters * 3f);
            t.text = text;
            return t;
        }

        // A solid dark tag behind a floating label, sized for the widest text it will show. It is a thin box, the label's
        // child, so it follows the label, writes depth (the networks' additive lines stop at its edge) and hides the
        // label from behind.
        private static void LabelBacking(TextMeshPro label, string widest, float pad, string mat = "PlateSolid")
        {
            string shown = label.text;
            label.text = widest;
            label.ForceMeshUpdate();
            Bounds b = label.textBounds;
            label.text = shown;
            label.ForceMeshUpdate();
            float m = label.fontSize * 0.1f * pad;
            Board("Backing", label.transform, new Vector3(b.center.x, b.center.y, 0.01f), new Vector2(b.size.x + 2f * m, b.size.y + 2f * m), mat, 0.02f);
        }

        private static void Set(Object target, string prop, System.Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(prop);
            if (p == null) throw new System.InvalidOperationException(target.GetType().Name + " has no field " + prop);
            apply(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ref(Object target, string prop, Object value) => Set(target, prop, p => p.objectReferenceValue = value);

        private static void Refs(Object target, string prop, IList<Object> values) => Set(target, prop, p =>
        {
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });

        private static void Float(Object target, string prop, float value) => Set(target, prop, p => p.floatValue = value);
        private static void Int(Object target, string prop, int value) => Set(target, prop, p => p.intValue = value);

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return asset;
        }

        private static Material Mat(string name) => Mats[name];

        // ---------- scene ----------

        private static void BuildScene(GameObject podPrefab, TextAsset dataAsset)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Layout layout = Layout.Make(_data);
            _layout = layout;

            RideTheme.RegionLook dock = _theme.Region(0);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = dock.fogDensity;
            RenderSettings.fogColor = dock.fog;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _theme.voidNavy;

            var pod = (GameObject)PrefabUtility.InstantiatePrefab(podPrefab);
            pod.transform.position = layout.LegA[0];

            var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
            if (rigAsset == null) throw new FileNotFoundException("XRI Starter Assets rig missing at " + RigPrefab);
            Transform anchor = pod.transform.Find("SeatAnchor");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, anchor);
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
            Camera head = rig.GetComponentInChildren<Camera>();
            head.tag = "MainCamera";
            head.clearFlags = CameraClearFlags.SolidColor;
            head.backgroundColor = dock.background;
            head.nearClipPlane = 0.03f;
            head.farClipPlane = 160f;   // the Valley and the networks are big
            if (head.GetComponent<AudioListener>() == null) head.gameObject.AddComponent<AudioListener>();

            // Quest 2 budget: no depth or opaque texture, no HDR, no post-processing on the ride camera (per-camera override).
            var headData = head.GetUniversalAdditionalCameraData();
            headData.requiresDepthTexture = false;
            headData.requiresColorTexture = false;
            headData.renderPostProcessing = false;
            headData.antialiasing = AntialiasingMode.None;
            head.allowHDR = false;
            head.allowMSAA = true;
            Ref(pod.GetComponent<PodSeat>(), "rig", rig.transform);

            // The rewind's wires hang far out in the network: the rig's rays must reach them.
            foreach (var ray in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true))
            {
                ray.maxRaycastDistance = Mathf.Max(ray.maxRaycastDistance, 60f);
            }

            new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();

            // The director, the data, and the moments (Gameplay), before the regions whose views listen to them.
            var directorObject = new GameObject("RideDirector");
            var source = directorObject.AddComponent<Level1DataSource>();
            Ref(source, "data", dataAsset);
            var moments = new Moments
            {
                Draw = MakeMoment<DrawMoment>(directorObject.transform, "Draw", source),
                Paint = MakeMoment<WeightPaintMoment>(directorObject.transform, "WeightPaint", source),
                Plug = MakeMoment<PlugMoment>(directorObject.transform, "Plug", source),
                Descent = Make("Descent", directorObject.transform, Vector3.zero).AddComponent<DescentMoment>(),
                Forward = MakeMoment<ForwardPassMoment>(directorObject.transform, "ForwardPassUntrained", source),
                Blame = MakeMoment<BlameMoment>(directorObject.transform, "Blame", source),
                Valley = MakeMoment<ValleyRunMoment>(directorObject.transform, "ValleyRun", source),
                Right = MakeMoment<ForwardPassMoment>(directorObject.transform, "ForwardPassTrained", source),
            };
            Set(moments.Right, "trained", p => p.boolValue = true);
            Set(moments.Right, "revealLines", p =>
            {
                p.arraySize = 3;
                p.GetArrayElementAtIndex(0).intValue = (int)RideLine.RightSays;
                p.GetArrayElementAtIndex(1).intValue = (int)RideLine.RightTraining;
                p.GetArrayElementAtIndex(2).intValue = (int)RideLine.RightAccuracy;
            });
            Set(moments.Valley, "hopDistances", p =>
            {
                p.arraySize = layout.HopDistances.Length;
                for (int i = 0; i < layout.HopDistances.Length; i++) p.GetArrayElementAtIndex(i).floatValue = layout.HopDistances[i];
            });

            var rd = directorObject.AddComponent<RideDirector>();
            var dash = pod.GetComponent<DashScreenView>();

            BuildTrackRails(layout);
            var regions = new GameObject[8];
            regions[0] = BuildPage(layout, rd);
            regions[1] = BuildGrid(layout);
            regions[2] = BuildNeuronRegion(layout, moments.Paint);
            regions[3] = BuildNetworkRegion(layout, rd, dash, moments.Forward);
            regions[4] = BuildWrongRegion(layout, rd, dash, regions[3]);
            regions[5] = BuildBlameRegion(layout, rd, dash, moments.Blame);
            regions[6] = BuildValleyRegion(layout, rd, dash, moments.Valley);
            regions[7] = BuildRightRegion(layout, rd, dash, moments.Right);

            WireDirector(rd, pod, layout, moments, regions);
            WirePod(pod, rd, moments);
            BuildPixel(pod, rd, dash, moments);
            Ref(rd.GetComponentInChildren<NeuronExplainer>(true), "paintGrid", pod.transform.Find("GameTable/WeightGrid"));
            WireGames(pod, rd, moments, rig);
            ConfigureGames(moments);
            BuildViewpoints(layout, regions);
            BuildBoot(pod, rd, regions);

            AssertOnlyRideRootsRender();
            AssertRideAudioLevels(pod);
            AssertRideSfxLevels(pod);
            RideSightlines.Assert();
            RideModelSheets.AssertCoverage(); // every model the builder makes has a multi-angle sheet
            RideBudget.Report();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            SetBuildScenes();
        }

        /// <summary>
        /// The boot gate (RideBoot): black over the eyes and no sound until everything that is hidden at the start (the regions,
        /// the game table, the stamp, the end card) has been drawn once and every clip loaded, so none of that stalls the ride.
        /// </summary>
        private static void BuildBoot(GameObject pod, RideDirector rd, GameObject[] regions)
        {
            var boot = pod.AddComponent<RideBoot>();
            Ref(boot, "director", rd);
            Ref(boot, "library", _library);
            Ref(boot, "sfx", pod.GetComponent<RideSfxView>());
            Ref(boot, "coverMaterial", Mat("BootCover"));
            var roots = new List<Object>(regions);
            foreach (string path in new[] { "GameTable", "Stamp", "EndCard", "Popup" })
            {
                Transform t = pod.transform.Find(path);
                if (t != null) roots.Add(t.gameObject);
            }

            Refs(boot, "warmRoots", roots);
        }

        private sealed class Moments
        {
            public DrawMoment Draw;
            public WeightPaintMoment Paint;
            public PlugMoment Plug;
            public DescentMoment Descent;
            public ForwardPassMoment Forward;
            public BlameMoment Blame;
            public ValleyRunMoment Valley;
            public ForwardPassMoment Right;
        }

        private static T MakeMoment<T>(Transform parent, string name, Level1DataSource source) where T : RideMoment
        {
            var moment = Make(name, parent, Vector3.zero).AddComponent<T>();
            Ref(moment, "source", source);
            return moment;
        }

        private static void AssertOnlyRideRootsRender()
        {
            var bad = new List<string>();
            foreach (Component c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include))
            {
                if (!(c is Renderer) && !(c is TMP_Text)) continue;
                string root = c.transform.root.name;
                if (root == "Pod" || root == "Track" || root.StartsWith("Region_")) continue;
                bad.Add(c.name + " under " + root);
            }

            if (bad.Count > 0) throw new System.InvalidOperationException("Visible objects outside Pod, Track, or a Region_N root: " + string.Join("; ", bad));
        }

        private static void AssertRideAudioLevels(GameObject root)
        {
            var bad = new List<string>();
            foreach (RideMusic music in root.GetComponentsInChildren<RideMusic>(true))
            {
                var so = new SerializedObject(music);
                float volume = so.FindProperty("volume").floatValue;
                float duckedVolume = so.FindProperty("duckedVolume").floatValue;
                if (volume > 0.2f) bad.Add(music.gameObject.name + " volume " + volume + " is above 0.2");
                if (duckedVolume > volume) bad.Add(music.gameObject.name + " duckedVolume " + duckedVolume + " is above volume " + volume);
            }

            if (bad.Count > 0) throw new System.InvalidOperationException("Ride music is too loud: " + string.Join("; ", bad) + ". Keep RideMusic volume at or below 0.2 and duckedVolume at or below volume (set in BuildPod).");
        }

        private static void SetBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.path != ScenePath) scenes.Add(s);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
