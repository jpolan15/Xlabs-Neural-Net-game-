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
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;
using Convergence.Presentation.Ride.Level2;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Builds Level 2, "Inside the Word Machine" (TASK_LEVEL2_WORD_MACHINE, ADR-022): Assets/Scenes/WordMachine.unity, build scene 1.
    /// The same pod, rig, director, narration pipeline and games base as Level 1, and one straight track through seven places: the dock
    /// and five stops, tokens, meaning, attention, prediction and writing, then the end. Everything in the places (the worlds, the
    /// explainers, the games' boards and effects) is built in code when the scene loads from the real GPT-2 bake
    /// (Assets/_Project/Data/Level2/level2_words.json); this file only places it and wires it, so the scene stays small and the look can
    /// be changed in C# without rebuilding. Development tooling only.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string Scene2Path = "Assets/Scenes/WordMachine.unity";
        private const string Theme2Path = "Assets/ScriptableObjects/RideTheme_L2.asset";
        private const string Script2Path = "Assets/ScriptableObjects/RideScript_L2.asset";
        private const string Library2Path = "Assets/ScriptableObjects/NarrationLibrary_L2.asset";
        private const string Pod2Path = PrefabDir + "/PodL2.prefab";

        /// <summary>Where each place sits along the track (z), dock first, then the five stops, then the end.</summary>
        private static readonly float[] StopZ = { 0f, 62f, 132f, 202f, 272f, 342f, 440f };

        // The segments of the ride, by index (RideDirector).
        private const int S2Tokens = 0, S2Slice = 1, S2Meaning = 2, S2Sort = 3, S2Attention = 4, S2Spot = 5, S2Prediction = 6, S2Bet = 7,
            S2Writing = 8, S2Road = 9, S2Finale = 10;

        [MenuItem("Convergence/Build Word Machine (Level 2)")]
        public static void BuildLevel2()
        {
            _level2 = true;
            try
            {
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (_font == null) throw new FileNotFoundException("LiberationSans SDF missing at " + FontPath);
                foreach (string dir in new[] { PrefabDir, MatDir })
                {
                    if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);
                }

                AssetDatabase.Refresh();
                var level1Asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level1Data.AssetPath);
                if (level1Asset == null) throw new FileNotFoundException(Level1Data.AssetPath + " is missing (Level 2 shares Level 1's materials).");
                _data = Level1Data.Parse(level1Asset.text);
                var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level2Data.AssetPath);
                if (dataAsset == null) throw new FileNotFoundException(Level2Data.AssetPath + " is missing. Run ArtSource/training/bake_level2.py.");
                Level2Data.Parse(dataAsset.text); // fails here, loudly, if the bake is off

                ImportMusic();
                ImportAudio();
                _theme = LoadTheme();
                Mats.Clear();
                MakeMaterials();
                MakeLevel2Materials();
                BuildNarration2();

                GameObject pod = SavePrefab(BuildPod(), Pod2Path);
                BuildScene2(pod, dataAsset);
                Debug.Log("[NeuralRideBuilder] Built " + Scene2Path);
            }
            finally
            {
                _level2 = false;
            }
        }

        private static RideTheme.RegionLook[] Level2Regions() => new[]
        {
            Sky("WORD MACHINE", "#0A0620", "#23124F", "#8A5CFF", 0.55f, "#7C4DFF", 0.30f, 1.2f, 0.010f),
            Sky("TOKENS", "#0B0603", "#33190A", "#FF9A2E", 0.50f, "#FFB347", 0.30f, 0.5f, 0.012f),
            Sky("MEANING", "#04041A", "#1A1050", "#6C4DFF", 0.60f, "#9B7BFF", 0.25f, 1.6f, 0.008f),
            Sky("ATTENTION", "#040B18", "#0B2F4F", "#29D3FF", 0.50f, "#36E0FF", 0.30f, 0.8f, 0.010f),
            Sky("PREDICTION", "#12041F", "#4A1440", "#FF4F9A", 0.60f, "#FF5FA8", 0.35f, 0.6f, 0.010f),
            Sky("WRITING", "#08141F", "#1B4B55", "#FFD36B", 0.55f, "#6DFFC9", 0.25f, 1.0f, 0.009f),
            Sky("THE END", "#060818", "#22204D", "#FFE08A", 0.50f, "#A98BFF", 0.30f, 1.4f, 0.009f),
        };

        // ---------- materials ----------

        private static Texture2D L2Texture(string file, int width, int height, System.Func<float, float, Color> pixel)
        {
            string path = MatDir + "/" + file;
            if (!File.Exists(path))
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++) tex.SetPixel(x, y, pixel((x + 0.5f) / width, (y + 0.5f) / height));
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

        private static void MakeLevel2Materials()
        {
            // A ring of light, soft on both edges.
            Texture2D ring = L2Texture("L2Ring.png", 128, 128, (u, v) =>
            {
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                float a = Mathf.SmoothStep(0.62f, 0.8f, d) * (1f - Mathf.SmoothStep(0.86f, 1f, d));
                a += 0.12f * (1f - Mathf.SmoothStep(0f, 0.8f, d));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            });

            // A beam: bright in the middle of its width, soft at both edges.
            Texture2D beam = L2Texture("L2Beam.png", 8, 64, (u, v) =>
            {
                float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v * 2f - 1f)), 1.5f);
                return new Color(1f, 1f, 1f, a);
            });
            Particles("L2Ring", Color.white, true, ring);
            Particles("L2Beam", Color.white, true, beam);
        }

        // ---------- narration ----------

        private static void BuildNarration2()
        {
            _library = LoadOrCreate<NarrationLibrary>(Library2Path);
            _script = LoadOrCreate<RideScript>(Script2Path);

            var entries = new List<NarrationLibrary.Entry>();
            var byLine = new Dictionary<RideLine, NarrationLibrary.Entry>();
            foreach (RideNarrationData.Line line in Level2NarrationData.All())
            {
                if (byLine.ContainsKey(line.Id)) throw new System.InvalidOperationException("Level2NarrationData lists " + line.Id + " twice.");
                var entry = new NarrationLibrary.Entry
                {
                    line = line.Id,
                    speaker = line.Speaker,
                    tag = line.Tag,
                    clip = FindVoiceClip(line),
                    pauseAfter = line.PauseAfter,
                    readSeconds = RideNarrationData.ReadSeconds(line),
                    segments = RideNarrationData.Segments(line.Text, out _),
                };
                if (entry.clip != null) ApplyMeasuredTiming(line, entry.segments);
                entries.Add(entry);
                byLine[line.Id] = entry;
            }

            _library.entries = entries.ToArray();
            EditorUtility.SetDirty(_library);

            _script.intro = ToSequence("Intro", Level2NarrationData.IntroLines, byLine);
            _script.dockLine = Level2NarrationData.DockLine;
            var sequences = new RideScript.Sequence[Level2NarrationData.Sequences.Length];
            for (int i = 0; i < sequences.Length; i++) sequences[i] = ToSequence(Level2NarrationData.Sequences[i].Name, Level2NarrationData.Sequences[i].Lines, byLine);
            _script.sequences = sequences;
            _script.lines = entries.ConvertAll(e => new RideScript.Beat { line = e.line, seconds = e.TotalSeconds }).ToArray();
            EditorUtility.SetDirty(_script);

            float total = 0f;
            foreach (NarrationLibrary.Entry e in entries) total += e.TotalSeconds;
            Debug.Log("[NeuralRideBuilder] Level 2 narration: " + entries.Count + " lines, " + total.ToString("0") + " s spoken in all; intro "
                + _script.intro.TotalSeconds.ToString("0") + " s. Voice clips bound: " + entries.FindAll(e => e.clip != null).Count + " of " + entries.Count + ".");
        }

        // ---------- the scene ----------

        /// <summary>The XR rig on the pod's seat, its camera set for the Quest 2 budget (as Level 1's), and the pod's seat pointed at it.</summary>
        private static GameObject MountRig(GameObject pod, RideTheme.RegionLook dock)
        {
            var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
            if (rigAsset == null) throw new FileNotFoundException("XRI Starter Assets rig missing at " + RigPrefab);
            Transform anchor = pod.transform.Find("SeatAnchor");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, anchor);
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
            Camera head = rig.GetComponentInChildren<Camera>();
            head.tag = "MainCamera";
            head.clearFlags = CameraClearFlags.Skybox;
            head.backgroundColor = dock.background;
            head.nearClipPlane = 0.03f;
            head.farClipPlane = 220f;
            if (head.GetComponent<AudioListener>() == null) head.gameObject.AddComponent<AudioListener>();
            var headData = head.GetUniversalAdditionalCameraData();
            headData.requiresDepthTexture = false;
            headData.requiresColorTexture = false;
            headData.renderPostProcessing = false;
            headData.antialiasing = AntialiasingMode.None;
            head.allowHDR = false;
            head.allowMSAA = true;
            Ref(pod.GetComponent<PodSeat>(), "rig", rig.transform);
            foreach (var ray in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true))
            {
                ray.maxRaycastDistance = Mathf.Max(ray.maxRaycastDistance, 60f);
            }

            return rig;
        }

        private static T MakeL2Moment<T>(Transform parent, string name, Level2DataSource source, Level2Session session, Transform stage) where T : RideMoment
        {
            var moment = Make(name, parent, Vector3.zero).AddComponent<T>();
            var so = new SerializedObject(moment);
            if (so.FindProperty("source") != null) Ref(moment, "source", source);
            if (so.FindProperty("session") != null) Ref(moment, "session", session);
            if (so.FindProperty("stage") != null) Ref(moment, "stage", stage);
            return moment;
        }

        private static void BuildScene2(GameObject podPrefab, TextAsset dataAsset)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RideTheme.RegionLook dock = _theme.Region(0);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = dock.fogDensity;
            RenderSettings.fogColor = dock.fog;
            RenderSettings.skybox = SkyMaterial();
            RegionAtmosphere.ApplySky(dock, _theme);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _theme.voidNavy;

            var pod = (GameObject)PrefabUtility.InstantiatePrefab(podPrefab);
            pod.name = "Pod"; // the ride's tools and checks look for the object by this name
            pod.transform.position = new Vector3(0f, 0f, StopZ[0]);
            GameObject rig = MountRig(pod, dock);
            new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();

            // The seven places, empty: the views are built into them when the scene loads.
            var regions = new GameObject[StopZ.Length];
            for (int i = 0; i < regions.Length; i++) regions[i] = RegionRoot(i, new Pose(new Vector3(0f, 0f, StopZ[i]), Quaternion.identity));

            // The director's object holds the data, the kit, the rider's run and the five games.
            var host = new GameObject("RideDirector");
            var source = host.AddComponent<Level2DataSource>();
            Ref(source, "data", dataAsset);
            var session = host.AddComponent<Level2Session>();
            var kit = host.AddComponent<Level2Kit>();
            Ref(kit, "glow", Mat("Spark"));
            Ref(kit, "ring", Mat("L2Ring"));
            Ref(kit, "beam", Mat("L2Beam"));
            Ref(kit, "shapes", Mat("ExplainerFlat"));
            Ref(kit, "solid", Mat("Tiles"));
            Ref(kit, "font", _font);
            Ref(kit, "serif", ExplainerFont());
            Ref(kit, "theme", _theme);

            var games = new GameObject[0];
            var slice = MakeL2Moment<SliceMoment>(host.transform, "Slice", source, session, regions[1].transform);
            var galaxy = MakeL2Moment<GalaxyMoment>(host.transform, "Galaxy", source, session, regions[2].transform);
            var spot = MakeL2Moment<SpotlightMoment>(host.transform, "Spotlight", source, session, regions[3].transform);
            var bet = MakeL2Moment<PredictMoment>(host.transform, "Predict", source, session, regions[4].transform);
            var road = MakeL2Moment<StoryMoment>(host.transform, "Story", source, session, regions[5].transform);
            ConfigureGames2(slice, galaxy, spot, bet, road);

            var rd = host.AddComponent<RideDirector>();
            var dash = pod.GetComponent<DashScreenView>();

            WireDirector2(rd, pod, new RideMoment[] { slice, galaxy, spot, bet, road }, regions);
            WirePod2(pod, rd);
            BuildStopViews(host, rd, dash, regions, slice, galaxy, spot, bet, road, source, session);
            WireTable2(pod, rd);
            WireLasers(rig);
            BuildBoot(pod, rd, regions);

            AssertOnlyRideRootsRender();
            AssertRideAudioLevels(pod);
            AssertRideSfxLevels(pod);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scene2Path);
            SetBuildScenes2();
        }

        private static void ConfigureGames2(SliceMoment slice, GalaxyMoment galaxy, SpotlightMoment spot, PredictMoment bet, StoryMoment road)
        {
            void Game(RideGame g, string title, string goal, RideLine win, float giveUp)
            {
                Set(g, "title", p => p.stringValue = title);
                Set(g, "goal", p => p.stringValue = goal);
                Set(g, "winLine", p => p.intValue = (int)win);
                Set(g, "giveUpAfter", p => p.floatValue = giveUp);
            }

            Game(slice, "CHOP THE WORDS", "Swipe through the glowing seams", RideLine.TokDone, 120f);
            Game(galaxy, "SORT THE GALAXY", "Carry each loose word home", RideLine.EmbDone, 130f);
            Game(spot, "BE THE ATTENTION", "Look at the words that matter", RideLine.AttDone, 120f);
            Game(bet, "PLACE YOUR CHIPS", "Spread ten chips over the next word", RideLine.PredDone, 140f);
            Game(road, "STEER THE STORY", "Shoot the word you want next", RideLine.GenDone, 120f);
        }

        private static void WireDirector2(RideDirector rd, GameObject pod, RideMoment[] moments, GameObject[] regions)
        {
            var points = new List<Vector3>();
            for (float z = StopZ[0]; z <= StopZ[StopZ.Length - 1] + 30f; z += 6f) points.Add(new Vector3(0f, 0f, z));
            var path = new RidePath(points);
            float D(float z) => path.ClosestDistance(new Vector3(0f, 0f, z));

            const int A = 0;
            int R(params int[] ids)
            {
                int mask = 0;
                foreach (int r in ids) mask |= 1 << r;
                return mask;
            }

            // Travel / Arrival are Level2NarrationData.Sequences indices (-1: none).
            var specs = new List<SegmentSpec>
            {
                new SegmentSpec { Name = "T1 tokens: explain", Region = 1, Leg = A, To = Stop(1), Cruise = 9f, MinSeconds = 7f, Travel = -1, Arrival = 0, Hold = ExplainHold, Mask = R(0, 1) },
                new SegmentSpec { Name = "T1 tokens: chop the words", Region = 1, Leg = A, To = Stop(1), Cruise = 4f, Travel = -1, Arrival = 1, ArrivalWithMoment = true, Moment = moments[0], Mask = R(1) },
                new SegmentSpec { Name = "T2 meaning: explain", Region = 2, Leg = A, To = Stop(2), Cruise = 9f, MinSeconds = 7f, Travel = -1, Arrival = 2, Hold = ExplainHold, Mask = R(1, 2) },
                new SegmentSpec { Name = "T2 meaning: sort the galaxy", Region = 2, Leg = A, To = Stop(2), Cruise = 4f, Travel = -1, Arrival = 3, ArrivalWithMoment = true, Moment = moments[1], Mask = R(2) },
                new SegmentSpec { Name = "T3 attention: explain", Region = 3, Leg = A, To = Stop(3), Cruise = 9f, MinSeconds = 7f, Travel = -1, Arrival = 4, Hold = ExplainHold, Mask = R(2, 3) },
                new SegmentSpec { Name = "T3 attention: be the attention", Region = 3, Leg = A, To = Stop(3), Cruise = 4f, Travel = -1, Arrival = 5, ArrivalWithMoment = true, Moment = moments[2], Mask = R(3) },
                new SegmentSpec { Name = "T4 prediction: explain", Region = 4, Leg = A, To = Stop(4), Cruise = 9f, MinSeconds = 7f, Travel = -1, Arrival = 6, Hold = ExplainHold, Mask = R(3, 4) },
                new SegmentSpec { Name = "T4 prediction: place your chips", Region = 4, Leg = A, To = Stop(4), Cruise = 4f, Travel = -1, Arrival = 7, ArrivalWithMoment = true, Moment = moments[3], Mask = R(4) },
                new SegmentSpec { Name = "T5 writing: explain", Region = 5, Leg = A, To = Stop(5), Cruise = 9f, MinSeconds = 7f, Travel = -1, Arrival = 8, Hold = ExplainHold, Mask = R(4, 5) },
                new SegmentSpec { Name = "T5 writing: steer the story", Region = 5, Leg = A, To = Stop(5), Cruise = 4f, Travel = -1, Arrival = 9, ArrivalWithMoment = true, Moment = moments[4], Mask = R(5, 6) },
                new SegmentSpec { Name = "Finale", Region = 6, Leg = A, To = Stop(6), Cruise = 7f, MinSeconds = 8f, Travel = -1, Arrival = 10, Hold = 2f, Mask = R(5, 6) },
            };

            Ref(rd, "pod", pod.transform);
            Ref(rd, "controls", pod.GetComponent<RideControlSet>());
            Ref(rd, "script", _script);
            Float(rd, "speechPadSeconds", 1.0f);
            Set(rd, "legs", p =>
            {
                p.arraySize = 1;
                SerializedProperty e = p.GetArrayElementAtIndex(0);
                e.FindPropertyRelative("name").stringValue = "A: the machine, dock to end";
                SerializedProperty pts = e.FindPropertyRelative("points");
                pts.arraySize = points.Count;
                for (int k = 0; k < points.Count; k++) pts.GetArrayElementAtIndex(k).vector3Value = points[k];
                e.FindPropertyRelative("gentleFrom").floatValue = -1f;
                e.FindPropertyRelative("gentleBaseY").floatValue = 0f;
            });

            Set(rd, "segments", p =>
            {
                p.arraySize = specs.Count;
                for (int i = 0; i < specs.Count; i++)
                {
                    SegmentSpec s = specs[i];
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("name").stringValue = s.Name;
                    e.FindPropertyRelative("region").intValue = s.Region;
                    e.FindPropertyRelative("leg").intValue = s.Leg;
                    e.FindPropertyRelative("to").floatValue = D(s.To.position.z);
                    e.FindPropertyRelative("cruise").floatValue = s.Cruise;
                    e.FindPropertyRelative("minSeconds").floatValue = s.MinSeconds;
                    e.FindPropertyRelative("travel").intValue = s.Travel;
                    e.FindPropertyRelative("arrival").intValue = s.Arrival;
                    e.FindPropertyRelative("moment").objectReferenceValue = s.Moment;
                    e.FindPropertyRelative("arrivalWithMoment").boolValue = s.ArrivalWithMoment;
                    e.FindPropertyRelative("hold").floatValue = s.Hold;
                }
            });

            var streamer = rd.gameObject.AddComponent<RegionStreamer>();
            Ref(streamer, "director", rd);
            Refs(streamer, "roots", regions);
            Int(streamer, "dockMask", R(0, 1));
            Set(streamer, "segmentMasks", p =>
            {
                p.arraySize = specs.Count;
                for (int i = 0; i < specs.Count; i++) p.GetArrayElementAtIndex(i).intValue = specs[i].Mask;
            });

            var atmosphere = rd.gameObject.AddComponent<RegionAtmosphere>();
            Ref(atmosphere, "director", rd);
            Ref(atmosphere, "theme", _theme);
            Ref(atmosphere, "grid", pod.transform.Find("Blackboard").GetComponent<Renderer>());
        }

        private static Pose Stop(int index) => new Pose(new Vector3(0f, 0f, StopZ[index]), Quaternion.identity);

        /// <summary>Wires the pod's views to the scene's director (the prefab cannot hold scene references).</summary>
        private static void WirePod2(GameObject pod, RideDirector director)
        {
            Ref(pod.GetComponent<DashScreenView>(), "director", director);
            Ref(pod.GetComponent<HintArrowView>(), "director", director);
            Ref(pod.GetComponent<FinaleView>(), "director", director);
            Ref(pod.GetComponent<RideMusic>(), "director", director);
            Ref(pod.GetComponent<RegionTagView>(), "director", director);
            Ref(pod.GetComponent<RideSfxView>(), "director", director);
        }

        /// <summary>The game table as Level 2's score board: its title, goal, timer and progress, the stamp, the popups and the subtitles on it.</summary>
        private static void WireTable2(GameObject pod, RideDirector rd)
        {
            Transform table = pod.transform.Find("GameTable");
            var mirror = pod.AddComponent<SubtitleMirror>();
            Ref(mirror, "source", pod.transform.Find("Dash/Subtitle").GetComponent<TextMeshPro>());
            Ref(mirror, "target", table.Find("Subtitle").GetComponent<TextMeshPro>());

            var tableView = pod.AddComponent<GameTableView>();
            Ref(tableView, "director", rd);
            Ref(tableView, "table", table);
            Ref(tableView, "frame", table.Find("Frame").GetComponent<Renderer>());
            Ref(tableView, "frameLive", Mat("Orange"));
            Ref(tableView, "frameIdle", Mat("TableFrame"));

            var hud = pod.AddComponent<GameHudView>();
            Transform stamp = pod.transform.Find("Stamp");
            Ref(hud, "director", rd);
            Ref(hud, "title", table.Find("Title").GetComponent<TextMeshPro>());
            Ref(hud, "goal", table.Find("Goal").GetComponent<TextMeshPro>());
            Ref(hud, "timer", table.Find("Timer").GetComponent<TextMeshPro>());
            Ref(hud, "score", table.Find("Score").GetComponent<TextMeshPro>());
            Ref(hud, "progressFill", table.Find("ProgressFill"));
            Ref(hud, "stamp", stamp.gameObject);
            Ref(hud, "stampText", stamp.Find("Text").GetComponent<TextMeshPro>());
            var stars = new List<Object>();
            for (int i = 0; i < 3; i++) stars.Add(stamp.Find("Star" + i).GetComponent<Renderer>());
            Refs(hud, "stars", stars);
            Ref(hud, "starLit", Mat("StarLit"));
            Ref(hud, "starDim", Mat("StarDim"));
            Ref(hud, "confetti", stamp.Find("Confetti").GetComponent<ParticleSystem>());
            Ref(hud, "popup", pod.transform.Find("Popup").GetComponent<TextMeshPro>());
            Ref(hud, "popupAnchor", table.Find("PopupAnchor"));
        }

        /// <summary>A laser on each hand and the hand colours, as in Level 1 (the rig's own beams are turned off, so there is one per hand).</summary>
        private static void WireLasers(GameObject rig)
        {
            Transform offset = rig.transform.Find("Camera Offset");
            var lasers = rig.AddComponent<RideHandLasers>();
            Ref(lasers, "leftHand", offset.Find("Left Controller"));
            Ref(lasers, "rightHand", offset.Find("Right Controller"));
            Ref(lasers, "laserMaterial", Mat("Laser"));
            foreach (var (side, hand) in new[] { ("Left Controller", RideHand.Left), ("Right Controller", RideHand.Right) })
            {
                var polarity = offset.Find(side).gameObject.AddComponent<HandPolarityController>();
                Int(polarity, "hand", hand);
                Ref(polarity, "orbMaterial", Mat("Laser"));
            }

            foreach (string side in new[] { "Left Controller", "Right Controller" })
            {
                Transform nearFar = offset.Find(side + "/Near-Far Interactor");
                if (nearFar == null) continue;
                foreach (LineRenderer line in nearFar.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
                foreach (MonoBehaviour b in nearFar.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (b.GetType().Name == "CurveVisualController") b.enabled = false;
                }
            }
        }

        private static void SetBuildScenes2()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(Scene2Path, true) };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.path != ScenePath && s.path != Scene2Path) scenes.Add(s);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- the places ----------

        private static T AddExplainer2<T>(Transform explainers, RideDirector rd, DashScreenView dash, RegionAtmosphere atmosphere, Level2DataSource source,
            RideLine[] lines, int explain, int last, Transform anchor) where T : Level2Explainer
        {
            var e = explainers.gameObject.AddComponent<T>();
            Ref(e, "director", rd);
            Ref(e, "dash", dash);
            Ref(e, "atmosphere", atmosphere);
            Ref(e, "theme", _theme);
            Set(e, "lines", p =>
            {
                p.arraySize = lines.Length;
                for (int i = 0; i < lines.Length; i++) p.GetArrayElementAtIndex(i).intValue = (int)lines[i];
            });
            Int(e, "explainSegment", explain);
            Int(e, "lastSegment", last);
            Set(e, "blackboard", p => p.boolValue = true);
            Ref(e, "anchor", anchor);
            Ref(e, "source", source);
            return e;
        }

        private static void BuildStopViews(GameObject host, RideDirector rd, DashScreenView dash, GameObject[] regions, SliceMoment slice, GalaxyMoment galaxy,
            SpotlightMoment spot, PredictMoment bet, StoryMoment road, Level2DataSource source, Level2Session session)
        {
            var atmosphere = rd.GetComponent<RegionAtmosphere>();
            Transform explainers = Make("Explainers", host.transform, Vector3.zero).transform;
            T Explainer<T>(RideLine[] lines, int explain, int last, int region) where T : Level2Explainer
                => AddExplainer2<T>(explainers, rd, dash, atmosphere, source, lines, explain, last, regions[region].transform);

            // The dock: the intro's lines and the dock's own.
            regions[0].AddComponent<DockWorld>();
            Explainer<WordDockExplainer>(new[] { RideLine.L2Hello, RideLine.L2Chatbot, RideLine.L2Stops }, -1, S2Tokens, 0);

            // Stop 1: tokens.
            regions[1].AddComponent<TokenWorld>();
            Explainer<TokenizeExplainer>(new[] { RideLine.TokChop, RideLine.TokNumbers, RideLine.TokRare, RideLine.TokSliceGo }, S2Tokens, S2Slice, 1);
            var sliceView = Make("SliceView", regions[1].transform, Vector3.zero).AddComponent<SliceView>();
            Ref(sliceView, "moment", slice);
            Ref(sliceView, "stage", regions[1].transform);

            // Stop 2: meaning.
            regions[2].AddComponent<GalaxyWorld>();
            Explainer<MeaningExplainer>(new[] { RideLine.EmbName, RideLine.EmbSquash, RideLine.EmbSpace, RideLine.EmbMath, RideLine.EmbSortGo }, S2Meaning, S2Sort, 2);
            var galaxyView = Make("GalaxyView", regions[2].transform, Vector3.zero).AddComponent<GalaxyView>();
            Ref(galaxyView, "moment", galaxy);
            Ref(galaxyView, "stage", regions[2].transform);

            // Stop 3: attention.
            regions[3].AddComponent<HallWorld>();
            Explainer<AttentionExplainer>(new[] { RideLine.AttMeaning, RideLine.AttLookBack, RideLine.AttShares, RideLine.AttReal, RideLine.AttGo }, S2Attention, S2Spot, 3);
            var spotView = Make("SpotlightView", regions[3].transform, Vector3.zero).AddComponent<SpotlightView>();
            Ref(spotView, "moment", spot);
            Ref(spotView, "stage", regions[3].transform);

            // Stop 4: prediction.
            regions[4].AddComponent<OddsWorld>();
            Explainer<OddsExplainer>(new[] { RideLine.PredJob, RideLine.PredOdds, RideLine.PredUnsure, RideLine.PredLearn, RideLine.PredGo }, S2Prediction, S2Bet, 4);
            var predictView = Make("PredictView", regions[4].transform, Vector3.zero).AddComponent<PredictView>();
            Ref(predictView, "moment", bet);
            Ref(predictView, "stage", regions[4].transform);

            // Stop 5: writing.
            regions[5].AddComponent<RoadWorld>();
            Explainer<WritingExplainer>(new[] { RideLine.GenLoop, RideLine.GenTemp, RideLine.GenGo }, S2Writing, S2Road, 5);
            var storyView = Make("StoryView", regions[5].transform, Vector3.zero).AddComponent<StoryView>();
            Ref(storyView, "moment", road);
            Ref(storyView, "stage", regions[5].transform);

            // The finale: Pixel speaks the rider's story.
            regions[6].AddComponent<EndWorld>();
            var outro = Explainer<OutroExplainer>(new[] { RideLine.OutTalk, RideLine.OutScale, RideLine.OutEnd }, S2Finale, S2Finale, 6);
            Ref(outro, "session", session);
        }
    }
}
