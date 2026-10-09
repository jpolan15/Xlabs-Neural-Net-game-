using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Builds the Neural Ride (ADR-011): theme asset, materials, Pod / Station / Track prefabs, and
    /// Assets/Scenes/NeuralRide.unity as build scene 0. Every visible object ends up under the Pod, a Station, or the
    /// Track, and the build fails if anything else renders. Run from the Convergence menu. Development tooling only.
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

        private static readonly Vector3[] PathPoints =
        {
            new Vector3(0, 0, 0), new Vector3(0, 0, 34), new Vector3(20, -2, 72), new Vector3(20, -4, 110), new Vector3(0, -4, 146)
        };

        private static readonly int[] StopPoints = { 1, 2, 3 };

        private static RideTheme _theme;
        private static TMP_FontAsset _font;
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
            ImportMusic();
            ImportAudio();
            _theme = LoadTheme();
            Mats.Clear();
            MakeMaterials();
            BuildNarration();

            GameObject pod = SavePrefab(BuildPod(), PrefabDir + "/Pod.prefab");
            GameObject station = SavePrefab(BuildStation(), PrefabDir + "/Station.prefab");
            GameObject track = SavePrefab(BuildTrack(), PrefabDir + "/Track.prefab");
            BuildScene(pod, station, track);
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

            // The pulse is short so a lever move changes what happens to the object in the arch right away.
            theme.pulseTravelSeconds = 0.7f;
            theme.landscapeSize = 3.2f;   // sized to sit inside the viewing cone from the seat
            theme.networkDim = 0.3f;      // the unlit part sits back, so the part the rider has lit stands out
            theme.stopFocus = 0.55f;      // at a stop the network steps back less, so it stays present
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static void ImportMusic()
        {
            var importer = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
            if (importer == null) { Debug.LogWarning("[NeuralRideBuilder] No music at " + MusicPath + "; the ride will be silent."); return; }
            var s = importer.defaultSampleSettings;
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
            if (kind != Kind.Opaque)
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
                // Panel plates and glows draw before the text and icons on them. Transparent objects sort by the distance of their
                // centres, so without this the side checks and the card year sort behind a wide plate and the plate dims them.
                int offset = name == "Plate" ? -10 : name.StartsWith("Glow") ? -20 : name == "CoreShell" ? -5 : 0;
                m.SetFloat("_QueueOffset", offset);
                m.renderQueue = (int)RenderQueue.Transparent + offset;
            }

            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        // Convergence/HoloLit (WP6): unlit cost, with a fixed key light and a Fresnel rim for depth. Opaque parts only.
        private static Material H(string name, Color c, float shade = 0.85f, float rim = 0.55f, Color? rimColor = null, Color? emission = null)
        {
            Shader holo = Shader.Find("Convergence/HoloLit");
            if (holo == null) throw new FileNotFoundException("Shader Convergence/HoloLit is missing. Is Assets/Shaders/HoloLit.shader imported?");
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(holo);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = holo;
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

        private static Material Icon(string name, string file, Color tint)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconDir + file + ".png");
            return M("Icon_" + name, tint, Kind.Alpha, tex);
        }

        private static void MakeMaterials()
        {
            RideTheme t = _theme;
            Texture2D glow = GlowTexture();
            // Solid parts get the HoloLit shader: depth from a key light and a rim, no real-time lights.
            H("PodDark", new Color(0.075f, 0.105f, 0.175f), 1f, 1.1f);
            H("PodMid", new Color(0.14f, 0.19f, 0.30f), 1f, 1.0f);
            H("Steel", new Color(0.26f, 0.34f, 0.47f), 1f, 0.8f);
            M("Screen", new Color(0.01f, 0.03f, 0.07f));
            M("SlotFrame", Color.white, Kind.Alpha, SlotTexture());
            H("Cyan", t.cyan, 0.30f, 0.5f, null, t.cyan * 0.30f);
            H("CyanDim", t.cyan * 0.35f, 0.30f, 0.3f);
            H("Amber", t.amber, 0.30f, 0.5f, t.amber, t.amber * 0.25f);
            H("Orange", t.orange, 0.45f, 0.65f, new Color(1f, 0.78f, 0.5f), t.orange * 0.15f);
            H("Locked", t.locked, 0.50f, 0.4f, t.locked * 1.5f);
            H("White", t.offWhite, 0.30f, 0.3f, Color.white, t.offWhite * 0.2f);
            H("TankDark", new Color(0.03f, 0.06f, 0.11f), 1f, 0.6f);
            H("FrameRight", new Color(0.04f, 0.30f, 0.34f), 0.4f, 0.4f);
            H("FrameWrong", new Color(0.40f, 0.28f, 0.03f), 0.4f, 0.4f, t.amber);
            H("RockBrown", new Color(0.50f, 0.42f, 0.36f), 1f, 0.55f, new Color(1f, 0.8f, 0.6f));
            H("RockLight", new Color(0.72f, 0.64f, 0.55f), 1f, 0.55f, new Color(1f, 0.85f, 0.7f));
            H("DroneBody", new Color(0.30f, 0.36f, 0.46f), 0.9f, 0.6f);
            H("DroneWhite", new Color(0.88f, 0.93f, 0.98f), 0.9f, 0.5f);
            H("Ice", new Color(0.62f, 0.92f, 1.00f), 0.5f, 0.6f, Color.white, new Color(0.15f, 0.3f, 0.35f));
            H("Violet", new Color(0.55f, 0.40f, 0.95f), 0.6f, 0.5f, t.violet);
            // Lines stay unlit: a line renderer has no useful normals.
            M("PipeCyan", t.cyan);
            M("PipeAmber", t.amber);
            M("PipeGrey", t.locked);
            M("LineWhite", t.offWhite);
            M("LineAmber", t.amber);
            M("GlowCyan", new Color(t.cyan.r, t.cyan.g, t.cyan.b, 0.55f), Kind.Additive, glow);
            M("GlowAmber", new Color(t.amber.r, t.amber.g, t.amber.b, 0.55f), Kind.Additive, glow);
            M("GlowOrange", new Color(t.orange.r, t.orange.g, t.orange.b, 0.7f), Kind.Additive, glow);
            M("GlowViolet", new Color(0.55f, 0.40f, 0.95f, 0.45f), Kind.Additive, glow);
            M("CoreShell", new Color(0.05f, 0.12f, 0.24f, 0.55f), Kind.Alpha);
            M("Plate", new Color(0.01f, 0.03f, 0.07f, 0.82f), Kind.Alpha);
            // The reading screens that float between the rider and the big network (chapter card, explainer, credits) are
            // solid: the network's additive shaders draw after transparent plates, so through a see-through plate its
            // bright lines crossed the text. An opaque plate writes depth and hides them. (The finale stays see-through:
            // its orb is meant to glow behind it.)
            M("PlateSolid", new Color(0.012f, 0.032f, 0.072f));
            M("Landscape", Color.white);
            Icon("Rock", "Rock", t.offWhite);
            Icon("Ice", "Ice", t.cyan);
            Icon("Drone", "Drone", t.offWhite);
            Icon("Comet", "Comet", t.cyan);
            Icon("Both", "Both", t.offWhite);
            Icon("Dial", "Dial", t.orange);
            Icon("Fire", "Fire", t.amber);
            Icon("Dash", "Dash", t.offWhite);
            Icon("Check", "Check", t.cyan);
            Icon("Cross", "Cross", t.amber);
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

        // A solid dark tag behind a floating label, sized for the widest text it will show. It is the label's child, so it
        // follows the label when a view moves it, and it writes depth, so the network's additive lines and the core's halo
        // stop at its edge instead of crossing or washing out the number (see the PlateSolid note in MakeMaterials).
        private static void LabelBacking(TextMeshPro label, string widest, float pad)
        {
            string shown = label.text;
            label.text = widest;
            label.ForceMeshUpdate();
            Bounds b = label.textBounds;
            label.text = shown;
            label.ForceMeshUpdate();
            float m = label.fontSize * 0.1f * pad; // pad in units of the type's height
            Quad("Backing", label.transform, new Vector3(b.center.x, b.center.y, 0.01f), new Vector2(b.size.x + 2f * m, b.size.y + 2f * m), "PlateSolid");
        }

        private static LineRenderer Line(string name, Transform parent, string mat, float width, bool world = false)
        {
            var go = Make(name, parent, Vector3.zero);
            var l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = Mats[mat];
            l.startWidth = width;
            l.endWidth = width;
            l.useWorldSpace = world;
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.numCapVertices = 4;
            return l;
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

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return asset;
        }

        private static Material Mat(string name) => Mats[name];

        // ---------- pod ----------

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
            Prim(PrimitiveType.Cylinder, "FloorRing", pod.transform, new Vector3(0, -0.045f, 0), new Vector3(2.75f, 0.01f, 2.75f), "Cyan");
            Prim(PrimitiveType.Cube, "SeatBack", pod.transform, new Vector3(0, 0.75f, -0.62f), new Vector3(0.62f, 0.9f, 0.1f), "PodMid");
            Prim(PrimitiveType.Cube, "SeatBase", pod.transform, new Vector3(0, 0.3f, -0.4f), new Vector3(0.62f, 0.12f, 0.5f), "PodMid");

            BuildTub(pod.transform);

            // Dash: tilted toward the seat. Local +Z points away from the player, so text and icons sit at negative Z.
            var dash = Make("Dash", pod.transform, new Vector3(0, 0.80f, 0.62f));
            dash.transform.localRotation = Quaternion.Euler(33f, 0, 0);
            // The trims are dim cyan: at full cyan the dash's edge was the brightest line in view, brighter than the network.
            Prim(PrimitiveType.Cube, "Panel", dash.transform, new Vector3(0, -0.02f, 0.02f), new Vector3(1.2f, 0.52f, 0.04f), "PodDark"); // 1.2 wide so the learning-rate labels right of the last lever stay on it
            Prim(PrimitiveType.Cube, "TrimTop", dash.transform, new Vector3(0, 0.235f, -0.004f), new Vector3(1.2f, 0.012f, 0.01f), "CyanDim");
            Prim(PrimitiveType.Cube, "TrimBottom", dash.transform, new Vector3(0, -0.275f, -0.004f), new Vector3(1.2f, 0.012f, 0.01f), "CyanDim");
            Quad("Screen", dash.transform, new Vector3(0, 0.15f, -0.004f), new Vector2(0.70f, 0.20f), "Screen");
            // The screen shows who is speaking, then up to three lines of subtitle. The dash is under 1 m from the eye,
            // so 0.031 m type is 1.7 degrees tall. Its bottom (0.063) stays above the highest lever knob (0.058, see MakeLever),
            // so a lever pushed to the top never covers the third line.
            TextMeshPro instruction = Text("Instruction", dash.transform, new Vector3(0, 0.207f, -0.012f), "Pull the orange lever!", 0.024f, _theme.offWhite, 0.66f, bold: true);
            TextMeshPro subtitle = Text("Subtitle", dash.transform, new Vector3(0, 0.128f, -0.012f), "", 0.031f, _theme.offWhite, 0.68f, bold: false);
            instruction.rectTransform.sizeDelta = new Vector2(0.66f, 0.04f);
            subtitle.rectTransform.sizeDelta = new Vector2(0.68f, 0.13f);

            var channels = new List<Object>();
            Lever action = MakeLever(dash.transform, RideControlIds.Action, "Action", -0.42f, 0f, null, channels);
            Text("ActionCaption", action.visual.transform, new Vector3(0, -0.165f, -0.012f), "GO!", 0.04f, _theme.orange, 0.2f);
            Lever rock = MakeLever(dash.transform, RideControlIds.Rock, "Rock", -0.17f, 0.5f, "Rock", channels);
            Lever ice = MakeLever(dash.transform, RideControlIds.Ice, "Ice", 0.0f, 0.5f, "Ice", channels);
            Lever trigger = MakeLever(dash.transform, RideControlIds.Trigger, "Trigger", 0.17f, 1f / 3f, "Dial", channels);
            Lever eta = MakeLever(dash.transform, RideControlIds.Eta, "Eta", 0.42f, 0f, null, channels);
            foreach (var pair in new[] { (0f, "slow"), (0.5f, "good"), (1f, "CRAZY") })
            {
                Text("Eta_" + pair.Item2, eta.visual.transform, new Vector3(0.1f, Mathf.Lerp(-0.1f, 0.1f, pair.Item1), -0.014f),
                    pair.Item2, 0.02f, _theme.offWhite, 0.14f, TextAlignmentOptions.Left);
            }

            var set = pod.AddComponent<RideControlSet>();
            Refs(set, "channels", channels);

            var speaker = Make("DashSpeaker", dash.transform, new Vector3(0, 0.15f, -0.05f)).AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0.6f;
            speaker.volume = 1f;

            var music = Make("Music", pod.transform, Vector3.zero).AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);

            // Finale panel: shown when the ride completes.
            // AURA's core (the orb) sits about 1.7 degrees under the eye at the end of the track, which is about -0.88 here, so
            // the text stacks above it and the orb glows in the open below, between the panel and the credits.
            var finale = Make("Finale", pod.transform, new Vector3(0, 2.1f, 2.6f));
            Quad("Glow", finale.transform, new Vector3(0, 0, 0.02f), new Vector2(5.0f, 1.8f), "GlowCyan");
            Quad("Plate", finale.transform, new Vector3(0, 0, 0.01f), new Vector2(3.4f, 1.2f), "Plate"); // see-through on purpose: the network shows through
            TextMeshPro finaleTitle = Text("Title", finale.transform, new Vector3(0, 0.42f, -0.01f), "YOU TAUGHT THE AI!", 0.3f, _theme.cyan, 3.2f);
            for (int i = 0; i < 3; i++) Quad("Check" + i, finale.transform, new Vector3((i - 1) * 0.85f, 0.0f, -0.01f), new Vector2(0.42f, 0.42f), "Icon_Check");
            var continued = Text("ToBeContinued", finale.transform, new Vector3(0, -0.4f, -0.01f), "TO BE CONTINUED...", 0.26f, _theme.amber, 3.2f);
            // The network's last connections converge on the orb and crossed both lines; a solid tag behind each line
            // stops them there, while the plate stays see-through so the network still shows around the words.
            LabelBacking(finaleTitle, "YOU TAUGHT THE AI!", 0.3f);
            LabelBacking(continued, "TO BE CONTINUED...", 0.3f);

            // Credits: every voice, music and sound source with its license (WP4). 2.05 m from the eye and 12 degrees below
            // it: under the orb's core (about 2 degrees down) and above the dash's top edge (about 19.6 degrees down, measured
            // in a rider-eye capture). Cap height 0.72 x 0.085 x 0.1 m = 0.061 m is 1.7 degrees there, over the ride's
            // 1.5 degree reading floor (the old 2.7 m and 0.075 gave 1.15). Four lines of at most 2.6 m stay inside the cone.
            var credits = Make("Credits", finale.transform, new Vector3(0, -1.225f, -0.6f));
            Quad("Plate", credits.transform, new Vector3(0, 0, 0.01f), new Vector2(2.9f, 0.5f), "PlateSolid");
            TextMeshPro creditText = Text("Text", credits.transform, new Vector3(0, 0, -0.01f), RideNarrationData.Credits, 0.085f, _theme.offWhite, 2.8f);
            creditText.rectTransform.sizeDelta = new Vector2(2.8f, 0.46f);
            creditText.lineSpacing = 10f;

            var finaleView = pod.AddComponent<FinaleView>();
            Ref(finaleView, "panel", finale);
            Ref(finaleView, "toBeContinued", continued.gameObject);
            Ref(finaleView, "credits", credits);

            // Chapter card: the year and title of the briefing that is playing, 5 m out and 11.5 degrees up.
            var card = Make("ChapterCard", pod.transform, Polar(0f, 11.5f, 5f));
            card.transform.localRotation = Face(0f, 11.5f);
            Quad("Glow", card.transform, new Vector3(0, 0, 0.03f), new Vector2(5.8f, 2.6f), "GlowCyan");
            Quad("Plate", card.transform, new Vector3(0, 0, 0.02f), new Vector2(4.8f, 2.0f), "PlateSolid");
            TextMeshPro cardYear = Text("Year", card.transform, new Vector3(0, 0.6f, -0.01f), "1943", 0.5f, _theme.cyan, 4.4f, bold: true);
            TextMeshPro cardTitle = Text("Title", card.transform, new Vector3(0, 0.06f, -0.01f), "THE FIRST NEURON", 0.34f, _theme.offWhite, 4.6f, bold: true);
            TextMeshPro cardBlurb = Text("Blurb", card.transform, new Vector3(0, -0.5f, -0.01f), "", 0.24f, _theme.offWhite, 4.4f);
            cardBlurb.rectTransform.sizeDelta = new Vector2(4.4f, 0.6f);
            var cardView = pod.AddComponent<ChapterCardView>();
            Ref(cardView, "library", _library);
            Ref(cardView, "card", card);
            Ref(cardView, "year", cardYear);
            Ref(cardView, "title", cardTitle);
            Ref(cardView, "blurb", cardBlurb);

            var hints = pod.AddComponent<HintArrowView>();
            var levers = new[] { action, rock, ice, trigger, eta };
            Set(hints, "arrows", p =>
            {
                p.arraySize = levers.Length;
                for (int i = 0; i < levers.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("id").stringValue = levers[i].channel.Id;
                    e.FindPropertyRelative("arrow").objectReferenceValue = levers[i].arrow;
                }
            });

            var screen = pod.AddComponent<DashScreenView>();
            Ref(screen, "instruction", instruction);
            Ref(screen, "subtitle", subtitle);
            Ref(screen, "voice", speaker);
            Ref(screen, "library", _library);
            Ref(screen, "theme", _theme);
            Ref(hints, "dash", screen); // "anything orange, you can grab": the arrow points at GO while it is said
            Ref(cardView, "dash", screen); // the outro card arrives with the 1969 line

            // What the Guide explains, shown while it is said: diagrams under the chapter card, and the dock's weight to play with.
            BuildExplainer(pod, screen, set);

            var seatScript = pod.AddComponent<PodSeat>();
            Ref(seatScript, "seatAnchor", seat);
            pod.AddComponent<DesktopSeat>();
            var musicPlayer = pod.AddComponent<RideMusic>();
            Ref(musicPlayer, "source", music);
            Ref(musicPlayer, "dash", screen);
            Set(musicPlayer, "volume", p => p.floatValue = 0.14f);
            Set(musicPlayer, "duckedVolume", p => p.floatValue = 0.05f);
            BuildSfx(pod, screen, cardView, set);
            return pod;
        }

        private static Lever MakeLever(Transform dash, string id, string name, float x, float zeroFraction, string icon, List<Object> channels)
        {
            const float half = 0.10f;
            var lever = new Lever { root = Make("Lever_" + name, dash, new Vector3(x, -0.062f, -0.01f)) }; // knob top at full travel: -0.062 + 0.10 + 0.02
            lever.visual = Make("Visual", lever.root.transform, Vector3.zero);
            Transform v = lever.visual.transform;
            Quad("Slot", v, new Vector3(0, 0, 0.003f), new Vector2(0.034f, half * 2f + 0.085f), "SlotFrame");
            if (zeroFraction >= 0f && id != RideControlIds.Action)
            {
                Quad("ZeroTick", v, new Vector3(0, Mathf.Lerp(-half, half, zeroFraction), -0.002f), new Vector2(0.075f, 0.005f), "Cyan");
            }

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

            if (icon != null) Quad("Icon", v, new Vector3(0, -half - 0.075f, -0.004f), new Vector2(0.075f, 0.075f), "Icon_" + icon);

            var channel = lever.root.AddComponent<RideControlChannel>();
            channel.SetId(id);
            lever.channel = channel;
            channels.Add(channel);

            var interactable = lever.root.AddComponent<PodLeverInteractable>();
            Ref(interactable, "channel", channel);
            Set(interactable, "halfTravel", p => p.floatValue = half);
            UnityEventTools.AddPersistentListener(interactable.UserMoved, channel.SetFromUser);

            var view = lever.root.AddComponent<PodLeverView>();
            Ref(view, "channel", channel);
            Ref(view, "handle", handle.transform);
            Ref(view, "handleRenderer", handle.transform.Find("HandleBody").GetComponent<Renderer>());
            Ref(view, "visualRoot", lever.visual);
            Ref(view, "liveMaterial", Mat("Orange"));
            Ref(view, "lockedMaterial", Mat("Locked"));
            Set(view, "halfTravel", p => p.floatValue = half);
            return lever;
        }

        // ---------- station ----------

        private static GameObject BuildStation()
        {
            var root = new GameObject("Station");
            var station = root.AddComponent<StationController>();
            var reveal = root.AddComponent<StationReveal>();
            var groups = new List<(GameObject, bool, bool, bool)>();

            // Neuron core (far layer) with the rock pipe.
            GameObject neuron = Make("Neuron", root.transform, Vector3.zero);
            // The core hangs where it sits in the viewing cone: 6.4 m ahead, 2.4 m up (10 degrees above the eye), 1.6 m across.
            Transform core = Make("Core", neuron.transform, new Vector3(0, DockHeight, DockForward)).transform;
            core.localScale = Vector3.one * 0.8f;
            Quad("CoreGlow", core, new Vector3(0, 0, 0.9f), new Vector2(6f, 6f), "GlowCyan");
            Prim(PrimitiveType.Sphere, "Shell", core, Vector3.zero, new Vector3(2.0f, 2.0f, 2.0f), "CoreShell");
            Transform tank = Make("Tank", core, Vector3.zero).transform;
            Prim(PrimitiveType.Cylinder, "TankGlass", tank, Vector3.zero, new Vector3(0.62f, 0.9f, 0.62f), "TankDark");
            var fill = Prim(PrimitiveType.Cube, "Fill", tank, Vector3.zero, new Vector3(0.40f, 0.02f, 0.40f), "Cyan");
            Prim(PrimitiveType.Cylinder, "ZeroMark", tank, Vector3.zero, new Vector3(0.66f, 0.004f, 0.66f), "White");
            var triggerLine = Prim(PrimitiveType.Cylinder, "TriggerLine", tank, Vector3.zero, new Vector3(0.78f, 0.016f, 0.78f), "White");
            // Labels are 0.30 m type at 6.4 m (2 degrees). The core is scaled 0.8, so the local size is 0.375.
            TextMeshPro sumLabel = Text("SumLabel", core, new Vector3(1.75f, 0.35f, -0.1f), "sum 0.0", 0.375f, _theme.cyan, 1.5f, TextAlignmentOptions.Left);
            TextMeshPro triggerLabel = Text("TriggerLabel", tank, new Vector3(-2.1f, 0f, -0.1f), "fire at 0.5", 0.375f, _theme.offWhite, 2.2f, TextAlignmentOptions.Right);
            // The cyan sum sat on the core's cyan halo and washed out; a solid tag keeps it crisp (vr-qa, session 3b).
            // The white "fire at" label reads fine without one, and every renderer here costs a draw call at the stops.
            LabelBacking(sumLabel, "sum -8.8", 0.35f);
            LineRenderer beam = Line("Beam", core, "PipeCyan", 0.07f, true);
            beam.enabled = false;
            var firedGlow = Quad("FiredGlow", core, new Vector3(0, 0, -0.2f), new Vector2(4.6f, 4.6f), "GlowCyan");
            firedGlow.SetActive(false);
            Transform portRock = Make("PortRock", core, new Vector3(-0.22f, -0.95f, 0)).transform;
            Transform portIce = Make("PortIce", core, new Vector3(0.22f, -0.95f, 0)).transform;

            var tankView = neuron.AddComponent<CoreTankView>();
            Ref(tankView, "station", station);
            Ref(tankView, "theme", _theme);
            Ref(tankView, "fillBar", fill.transform);
            Ref(tankView, "fillRenderer", fill.GetComponent<Renderer>());
            Ref(tankView, "positiveMaterial", Mat("Cyan"));
            Ref(tankView, "negativeMaterial", Mat("Amber"));
            Ref(tankView, "triggerLine", triggerLine.transform);
            Ref(tankView, "sumLabel", sumLabel);
            Ref(tankView, "triggerLabel", triggerLabel);
            Ref(tankView, "beam", beam);
            Ref(tankView, "beamOrigin", core);
            Ref(tankView, "firedGlow", firedGlow);

            // Scanner arch (mid layer) and data lane.
            GameObject stream = Make("Stream", root.transform, Vector3.zero);
            const float laneZ = 3.3f;
            // The scanner arch is low: nothing on it rises above 1.12 m, so it never crosses a sight line to the core,
            // the diagram, or the board, whatever the rider's height.
            Transform lane = Make("Lane", stream.transform, new Vector3(0, 0.72f, laneZ)).transform;
            Prim(PrimitiveType.Cube, "Belt", stream.transform, new Vector3(0, 0.40f, laneZ), new Vector3(8.2f, 0.03f, 0.5f), "CyanDim");
            Prim(PrimitiveType.Cube, "PostL", stream.transform, new Vector3(-0.8f, 0.75f, laneZ), new Vector3(0.09f, 0.70f, 0.09f), "Steel");
            Prim(PrimitiveType.Cube, "PostR", stream.transform, new Vector3(0.8f, 0.75f, laneZ), new Vector3(0.09f, 0.70f, 0.09f), "Steel");
            Prim(PrimitiveType.Cube, "Top", stream.transform, new Vector3(0, 1.105f, laneZ), new Vector3(1.7f, 0.03f, 0.09f), "Steel");
            Prim(PrimitiveType.Cube, "TopGlow", stream.transform, new Vector3(0, 1.085f, laneZ - 0.03f), new Vector3(1.6f, 0.01f, 0.02f), "Cyan");
            Quad("RockLamp", stream.transform, new Vector3(-0.8f, 0.98f, laneZ - 0.06f), new Vector2(0.30f, 0.30f), "Icon_Rock");
            var rockGlow = Quad("RockLampGlow", stream.transform, new Vector3(-0.8f, 0.98f, laneZ - 0.05f), new Vector2(0.8f, 0.8f), "GlowCyan");
            GameObject iceLamp = Make("IceLamp", root.transform, Vector3.zero);
            Quad("IceLampIcon", iceLamp.transform, new Vector3(0.8f, 0.98f, laneZ - 0.06f), new Vector2(0.30f, 0.30f), "Icon_Ice");
            var iceGlow = Quad("IceLampGlow", iceLamp.transform, new Vector3(0.8f, 0.98f, laneZ - 0.05f), new Vector2(0.8f, 0.8f), "GlowCyan");

            Transform templates = Make("Templates", stream.transform, Vector3.zero).transform;
            var items = new List<Object>
            {
                MakeDrone(templates), MakeComet(templates), MakeRock(templates, false), MakeRock(templates, true)
            };

            // Pipes.
            var rockPipe = MakePipe(neuron.transform, station, RideControlIds.Rock, "RockPipe", rockGlow.transform, portRock, new Vector3(-0.8f, 0.4f, 0), new Vector3(-1.2f, 1.9f, 4.8f)); // label above the arc, not on it, and clear of the core halo
            GameObject icePipeRoot = Make("IcePipeGroup", root.transform, Vector3.zero);
            var icePipe = MakePipe(icePipeRoot.transform, station, RideControlIds.Ice, "IcePipe", iceGlow.transform, portIce, new Vector3(0.8f, 0.4f, 0), new Vector3(1.2f, 1.9f, 4.8f));

            var streamView = stream.AddComponent<DataStreamView>();
            Ref(streamView, "station", station);
            Ref(streamView, "theme", _theme);
            Ref(streamView, "lane", lane);
            Refs(streamView, "itemPrefabs", items);
            Ref(streamView, "rockPipe", rockPipe);
            Ref(streamView, "icePipe", icePipe);
            Ref(streamView, "core", tankView);
            Ref(streamView, "rockLampGlow", rockGlow);
            Ref(streamView, "iceLampGlow", iceGlow);

            // Live board.
            // Board right, diagram left: both inside +-35 degrees, 3.8 m out, clear of the arch and the core's labels.
            GameObject board = Make("Board", root.transform, Polar(29f, 2f, 3.8f));
            board.transform.localRotation = Face(29f, 2f);
            board.transform.localScale = Vector3.one * 1.15f;
            var tiles = new LiveBoardView.Tile[4];
            string[] caseIcons = { "Icon_Drone", "Icon_Comet", "Icon_Rock", "Icon_Both" };
            Vector2[] slots = { new Vector2(-0.24f, 0.24f), new Vector2(0.24f, 0.24f), new Vector2(-0.24f, -0.24f), new Vector2(0.24f, -0.24f) };
            for (int i = 0; i < 4; i++)
            {
                var tileRoot = Make("Tile" + i, board.transform, new Vector3(slots[i].x, slots[i].y, 0));
                var frame = Quad("Frame", tileRoot.transform, new Vector3(0, 0, 0.004f), new Vector2(0.44f, 0.44f), "FrameRight");
                Quad("Case", tileRoot.transform, new Vector3(-0.09f, 0.07f, -0.002f), new Vector2(0.19f, 0.19f), caseIcons[i]);
                var action = Quad("Action", tileRoot.transform, new Vector3(0.1f, 0.07f, -0.002f), new Vector2(0.17f, 0.17f), "Icon_Fire");
                var status = Quad("Status", tileRoot.transform, new Vector3(0, -0.1f, -0.002f), new Vector2(0.17f, 0.17f), "Icon_Check");
                tiles[i] = new LiveBoardView.Tile { root = tileRoot, frame = frame.GetComponent<Renderer>(), action = action.GetComponent<Renderer>(), status = status.GetComponent<Renderer>() };
            }

            var boardView = board.AddComponent<LiveBoardView>();
            Ref(boardView, "station", station);
            Set(boardView, "tiles", p =>
            {
                p.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("root").objectReferenceValue = tiles[i].root;
                    e.FindPropertyRelative("frame").objectReferenceValue = tiles[i].frame;
                    e.FindPropertyRelative("action").objectReferenceValue = tiles[i].action;
                    e.FindPropertyRelative("status").objectReferenceValue = tiles[i].status;
                }
            });
            Ref(boardView, "frameRight", Mat("FrameRight"));
            Ref(boardView, "frameWrong", Mat("FrameWrong"));
            Ref(boardView, "zapIcon", Mat("Icon_Fire"));
            Ref(boardView, "passIcon", Mat("Icon_Dash"));
            Ref(boardView, "rightIcon", Mat("Icon_Check"));
            Ref(boardView, "wrongIcon", Mat("Icon_Cross"));

            // Classic network diagram: inputs -> neuron -> output.
            GameObject diagram = Make("Diagram", root.transform, Polar(-29f, 2f, 3.8f));
            diagram.transform.localRotation = Face(-29f, 2f);
            diagram.transform.localScale = Vector3.one * 0.82f;
            Quad("Plate", diagram.transform, new Vector3(0, 0, 0.01f), new Vector2(1.7f, 1.05f), "PlateSolid"); // solid: network lines crossed its weight labels
            Renderer Node(string name, Transform parent, Vector3 pos, float size, string icon)
            {
                var disc = Prim(PrimitiveType.Cylinder, name, parent, pos, new Vector3(size, 0.01f, size), "Steel");
                disc.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                if (icon != null) Quad(name + "Icon", parent, pos + new Vector3(0, 0, -0.012f), Vector2.one * size * 0.7f, icon);
                return disc.GetComponent<Renderer>();
            }

            Vector3 rockAt = new Vector3(-0.58f, 0.27f, -0.004f), iceAt = new Vector3(-0.58f, -0.27f, -0.004f);
            Vector3 neuronAt = new Vector3(0.05f, 0f, -0.004f), outAt = new Vector3(0.62f, 0f, -0.004f);
            LineRenderer rockLine = Line("RockLine", diagram.transform, "PipeCyan", 0.03f);
            rockLine.positionCount = 2; rockLine.SetPosition(0, rockAt); rockLine.SetPosition(1, neuronAt);
            GameObject iceParts = Make("IceParts", diagram.transform, Vector3.zero);
            LineRenderer iceLine = Line("IceLine", iceParts.transform, "PipeCyan", 0.03f);
            iceLine.positionCount = 2; iceLine.SetPosition(0, iceAt); iceLine.SetPosition(1, neuronAt);
            LineRenderer outLine = Line("OutLine", diagram.transform, "LineWhite", 0.02f);
            outLine.positionCount = 2; outLine.SetPosition(0, neuronAt); outLine.SetPosition(1, outAt);
            Renderer rockNode = Node("RockNode", diagram.transform, rockAt, 0.26f, "Icon_Rock");
            Renderer iceNode = Node("IceNode", iceParts.transform, iceAt, 0.26f, "Icon_Ice");
            Node("NeuronNode", diagram.transform, neuronAt, 0.34f, null);
            Renderer outputNode = Node("OutputNode", diagram.transform, outAt, 0.26f, "Icon_Fire");
            // One line each, above the rock wire and below the ice wire (even at its thickest, weight 2), so no wire runs
            // through a number; inside the plate, so the plate never grows into the equation's sightline.
            TextMeshPro rockWeight = Text("RockWeight", diagram.transform, new Vector3(-0.28f, 0.40f, -0.02f), "×0.0", 0.21f, _theme.cyan, 0.6f);
            TextMeshPro iceWeight = Text("IceWeight", iceParts.transform, new Vector3(-0.28f, -0.40f, -0.02f), "×0.0", 0.21f, _theme.cyan, 0.6f);
            var diagramView = diagram.AddComponent<NetworkDiagramView>();
            Ref(diagramView, "station", station);
            Ref(diagramView, "theme", _theme);
            Ref(diagramView, "rockLine", rockLine);
            Ref(diagramView, "iceLine", iceLine);
            Ref(diagramView, "rockLabel", rockWeight);
            Ref(diagramView, "iceLabel", iceWeight);
            Ref(diagramView, "iceParts", iceParts);
            Ref(diagramView, "rockNode", rockNode);
            Ref(diagramView, "iceNode", iceNode);
            Ref(diagramView, "outputNode", outputNode);
            Ref(diagramView, "positiveLine", Mat("PipeCyan"));
            Ref(diagramView, "negativeLine", Mat("PipeAmber"));
            Ref(diagramView, "zeroLine", Mat("PipeGrey"));
            Ref(diagramView, "nodeOff", Mat("Steel"));
            Ref(diagramView, "nodeOn", Mat("Cyan"));

            // Equation.
            GameObject equation = Make("Equation", root.transform, Polar(0f, 15f, 4.0f));
            equation.transform.localRotation = Face(0f, 15f);
            var plate = Quad("Plate", equation.transform, new Vector3(0, 0, 0.01f), new Vector2(3.7f, 0.95f), "Plate");
            var eqText = Text("EquationText", equation.transform, new Vector3(0, 0, -0.01f), "", 0.18f, _theme.offWhite, 3.5f);
            eqText.rectTransform.sizeDelta = new Vector2(3.5f, 0.85f);
            var eqView = equation.AddComponent<EquationRevealView>();
            Ref(eqView, "station", station);
            Ref(eqView, "theme", _theme);
            Ref(eqView, "text", eqText);
            Ref(eqView, "plate", plate);

            // Loss landscape.
            GameObject land = Make("Landscape", root.transform, new Vector3(0, 1.55f, 5.4f));
            land.transform.localRotation = Quaternion.Euler(-52f, 0, 0);
            var surface = Make("Surface", land.transform, Vector3.zero);
            surface.AddComponent<MeshFilter>().sharedMesh = PlaceholderLandscape(_theme.landscapeSize); // replaced at runtime
            var surfaceRenderer = surface.AddComponent<MeshRenderer>();
            surfaceRenderer.sharedMaterial = Mat("Landscape");
            surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var marble = Prim(PrimitiveType.Sphere, "Marble", land.transform, Vector3.zero, Vector3.one * (_theme.marbleRadius * 2f), "White");
            LineRenderer trail = Line("Trail", land.transform, "LineWhite", 0.035f);
            LineRenderer arrow = Line("SlopeArrow", land.transform, "LineAmber", 0.05f);
            arrow.positionCount = 2;
            TextMeshPro lossLabel = Text("LossLabel", land.transform, new Vector3(0, 0.8f, -(_theme.landscapeSize * 0.5f + 0.2f)), "error", 0.26f, _theme.offWhite, 2f);
            lossLabel.transform.localRotation = Quaternion.Euler(52f, 0, 0);
            var landView = land.AddComponent<LossLandscapeView>();
            Ref(landView, "station", station);
            Ref(landView, "theme", _theme);
            Ref(landView, "surface", surface.GetComponent<MeshFilter>());
            Ref(landView, "surfaceRenderer", surfaceRenderer);
            Ref(landView, "marble", marble.transform);
            Ref(landView, "trail", trail);
            Ref(landView, "slopeArrow", arrow);
            Ref(landView, "lossLabel", lossLabel);

            groups.Add((neuron, true, true, false));
            groups.Add((icePipeRoot, false, true, false));
            groups.Add((iceLamp, false, true, false));
            groups.Add((stream, true, true, false));
            groups.Add((board, true, true, true));
            groups.Add((diagram, true, true, true));
            groups.Add((equation, true, true, true));
            groups.Add((land, false, false, true));
            Ref(reveal, "station", station);
            Set(reveal, "groups", p =>
            {
                p.arraySize = groups.Count;
                for (int i = 0; i < groups.Count; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("root").objectReferenceValue = groups[i].Item1;
                    e.FindPropertyRelative("oneSignal").boolValue = groups[i].Item2;
                    e.FindPropertyRelative("twoSignals").boolValue = groups[i].Item3;
                    e.FindPropertyRelative("learns").boolValue = groups[i].Item4;
                }
            });
            return root;
        }

        private static PipeFlowView MakePipe(Transform parent, StationController station, string channelId, string name, Transform from, Transform to, Vector3 bend, Vector3 labelAt)
        {
            var root = Make(name, parent, Vector3.zero);
            LineRenderer line = Line("Line", root.transform, "PipeCyan", 0.05f, true);
            var pulses = new List<Object>();
            for (int i = 0; i < 3; i++)
            {
                var pulse = Prim(PrimitiveType.Sphere, "Pulse" + i, root.transform, Vector3.zero, Vector3.one * 0.1f, "Cyan");
                pulse.SetActive(false);
                pulses.Add(pulse.GetComponent<Renderer>());
            }

            TextMeshPro label = Text("Label", root.transform, labelAt, "x0.0", 0.3f, _theme.cyan, 1.4f);
            var anchor = Make("LabelAnchor", root.transform, labelAt).transform;
            var view = root.AddComponent<PipeFlowView>();
            Ref(view, "station", station);
            Ref(view, "theme", _theme);
            Set(view, "channelId", p => p.stringValue = channelId);
            Ref(view, "line", line);
            Ref(view, "from", from);
            Ref(view, "to", to);
            Set(view, "bend", p => p.vector3Value = bend);
            Ref(view, "positiveMaterial", Mat("PipeCyan"));
            Ref(view, "negativeMaterial", Mat("PipeAmber"));
            Ref(view, "zeroMaterial", Mat("PipeGrey"));
            Refs(view, "pulseRenderers", pulses);
            Ref(view, "positivePulse", Mat("Cyan"));
            Ref(view, "negativePulse", Mat("Amber"));
            Ref(view, "label", label);
            Ref(view, "labelAnchor", anchor);
            return view;
        }

        private static GameObject MakeDrone(Transform parent)
        {
            // A friendly little quadcopter: white box body, dark visor with two cyan eyes, X arms, four spinning rotors.
            var d = Make("Drone", parent, Vector3.zero);
            Prim(PrimitiveType.Cube, "Body", d.transform, Vector3.zero, new Vector3(0.36f, 0.22f, 0.30f), "DroneWhite");
            Prim(PrimitiveType.Cube, "Visor", d.transform, new Vector3(0, 0.01f, -0.155f), new Vector3(0.28f, 0.13f, 0.02f), "TankDark");
            Prim(PrimitiveType.Sphere, "EyeL", d.transform, new Vector3(-0.065f, 0.01f, -0.17f), Vector3.one * 0.07f, "Cyan");
            Prim(PrimitiveType.Sphere, "EyeR", d.transform, new Vector3(0.065f, 0.01f, -0.17f), Vector3.one * 0.07f, "Cyan");
            foreach (float yaw in new[] { 45f, -45f })
            {
                var arm = Prim(PrimitiveType.Cube, "Arm", d.transform, new Vector3(0, 0.1f, 0), new Vector3(0.74f, 0.03f, 0.04f), "Steel");
                arm.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            }

            foreach (Vector2 corner in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            {
                var rotor = Prim(PrimitiveType.Cylinder, "Rotor", d.transform, new Vector3(corner.x * 0.26f, 0.14f, corner.y * 0.26f), new Vector3(0.26f, 0.006f, 0.26f), "Cyan");
                rotor.AddComponent<SpinBehaviour>();
            }

            d.SetActive(false);
            return d;
        }

        private static GameObject MakeComet(Transform parent)
        {
            var c = Make("Comet", parent, Vector3.zero);
            Prim(PrimitiveType.Sphere, "Core", c.transform, Vector3.zero, Vector3.one * 0.34f, "Ice");
            Prim(PrimitiveType.Sphere, "Shine", c.transform, new Vector3(-0.05f, 0.06f, -0.12f), Vector3.one * 0.14f, "White");
            Quad("Tail", c.transform, new Vector3(0.36f, 0, 0.02f), new Vector2(0.9f, 0.5f), "GlowCyan");
            c.SetActive(false);
            return c;
        }

        private static GameObject MakeRock(Transform parent, bool withIce)
        {
            var r = Make(withIce ? "Chunk" : "Rock", parent, Vector3.zero);
            Prim(PrimitiveType.Sphere, "Body", r.transform, Vector3.zero, new Vector3(0.50f, 0.40f, 0.44f), "RockBrown");
            Prim(PrimitiveType.Sphere, "BumpA", r.transform, new Vector3(0.14f, 0.12f, -0.12f), new Vector3(0.24f, 0.2f, 0.2f), "RockLight");
            Prim(PrimitiveType.Sphere, "BumpB", r.transform, new Vector3(-0.16f, -0.08f, 0.1f), new Vector3(0.2f, 0.18f, 0.2f), "RockLight");
            if (withIce)
            {
                var a = Prim(PrimitiveType.Cube, "Crystal1", r.transform, new Vector3(0.0f, 0.24f, -0.06f), new Vector3(0.1f, 0.26f, 0.1f), "Ice");
                a.transform.localRotation = Quaternion.Euler(20, 0, 25);
                var b = Prim(PrimitiveType.Cube, "Crystal2", r.transform, new Vector3(0.2f, 0.16f, 0.08f), new Vector3(0.08f, 0.2f, 0.08f), "Ice");
                b.transform.localRotation = Quaternion.Euler(-15, 30, -30);
            }

            r.SetActive(false);
            return r;
        }

        // The track is the big neural network: see NeuralRideBuilder.Network.cs (BuildTrack).

        // ---------- scene ----------

        private static void BuildScene(GameObject podPrefab, GameObject stationPrefab, GameObject trackPrefab)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var path = new RidePath(PathPoints);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogColor = _theme.voidNavy;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _theme.voidNavy;

            var trackInstance = (GameObject)PrefabUtility.InstantiatePrefab(trackPrefab);
            var pod = (GameObject)PrefabUtility.InstantiatePrefab(podPrefab);
            pod.transform.position = PathPoints[0];

            var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
            if (rigAsset == null) throw new FileNotFoundException("XRI Starter Assets rig missing at " + RigPrefab);
            Transform anchor = pod.transform.Find("SeatAnchor");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, anchor);
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
            Camera head = rig.GetComponentInChildren<Camera>();
            head.tag = "MainCamera";
            head.clearFlags = CameraClearFlags.SolidColor;
            head.backgroundColor = _theme.voidNavy;
            head.nearClipPlane = 0.03f;
            head.farClipPlane = 100f;   // the big network reaches far down the track
            if (head.GetComponent<AudioListener>() == null) head.gameObject.AddComponent<AudioListener>();

            // Quest 2 budget: the project's active URP asset is a desktop sample with the depth texture, the opaque texture
            // and HDR all on, which costs a full-screen copy each on a tile GPU. The ride needs none of them, so this camera
            // turns them off (a per-camera override; the shared asset and the other scenes are untouched).
            var headData = head.GetUniversalAdditionalCameraData();
            headData.requiresDepthTexture = false;
            headData.requiresColorTexture = false;
            headData.renderPostProcessing = false;
            headData.antialiasing = AntialiasingMode.None;
            head.allowHDR = false;
            head.allowMSAA = true;
            Ref(pod.GetComponent<PodSeat>(), "rig", rig.transform);

            new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();

            var director = new GameObject("RideDirector");
            var points = new List<Object>();
            for (int i = 0; i < PathPoints.Length; i++)
            {
                var pt = new GameObject("PathPoint" + i);
                pt.transform.SetParent(director.transform, false);
                pt.transform.position = PathPoints[i];
                points.Add(pt.transform);
            }

            var stops = new List<StationController>();
            for (int i = 0; i < StopPoints.Length; i++)
            {
                float d = path.DistanceAtPoint(StopPoints[i]);
                var st = (GameObject)PrefabUtility.InstantiatePrefab(stationPrefab);
                st.name = "Station_" + (i + 1);
                st.transform.position = path.Position(d);
                Vector3 h = path.Heading(d);
                st.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(h.x, h.z) * Mathf.Rad2Deg, 0);
                var sc = st.GetComponent<StationController>();
                Set(sc, "kind", p => p.enumValueIndex = i);
                stops.Add(sc);
            }

            var rd = director.AddComponent<RideDirector>();
            Ref(rd, "pod", pod.transform);
            Ref(rd, "controls", pod.GetComponent<RideControlSet>());
            Refs(rd, "pathPoints", points);
            Set(rd, "stops", p =>
            {
                p.arraySize = stops.Count;
                for (int i = 0; i < stops.Count; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("pointIndex").intValue = StopPoints[i];
                    e.FindPropertyRelative("station").objectReferenceValue = stops[i];
                }
            });
            Ref(pod.GetComponent<DashScreenView>(), "director", rd);
            Ref(pod.GetComponent<HintArrowView>(), "director", rd);
            Ref(pod.GetComponent<FinaleView>(), "director", rd);
            Ref(pod.GetComponent<RideMusic>(), "director", rd);
            Ref(pod.GetComponent<ChapterCardView>(), "director", rd);
            Ref(trackInstance.GetComponent<NeuralCoreView>(), "director", rd);
            Ref(trackInstance.GetComponent<NeuralCoreView>(), "dash", pod.GetComponent<DashScreenView>()); // the network acts out the narration
            Ref(rd, "script", _script);
            WireSfx(pod, rd, stops);
            WireExplainer(pod, rd);
            foreach (PodLeverInteractable lever in pod.GetComponentsInChildren<PodLeverInteractable>(true)) Ref(lever, "director", rd); // a pulse in the hand at the intro's hands-on beats

            AssertOnlyRideRootsRender();
            AssertRideAudioLevels(pod);
            AssertRideSfxLevels(pod);
            RideSightlines.Assert();
            RideBudget.Report();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            SetBuildScenes();
        }

        private static void AssertOnlyRideRootsRender()
        {
            var bad = new List<string>();
            foreach (Component c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include))
            {
                if (!(c is Renderer) && !(c is TMP_Text)) continue;
                string root = c.transform.root.name;
                if (root == "Pod" || root == "Track" || root.StartsWith("Station_")) continue;
                bad.Add(c.name + " under " + root);
            }

            if (bad.Count > 0) throw new System.InvalidOperationException("Visible objects outside Pod, Station, or Track: " + string.Join("; ", bad));
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

            if (bad.Count > 0) throw new System.InvalidOperationException("Ride music is too loud: " + string.Join("; ", bad) + ". Keep RideMusic volume at or below 0.2 and duckedVolume at or below volume (set in BuildPod in Tools/Editor/NeuralRideBuilder.cs).");
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
