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

        /// <summary>
        /// Level 2's skies (Convergence/DaySky, ADR-023): a day passing on the bike path, never Level 1's night. Morning at the dock, bright
        /// noon at the chopper and the observatory, golden hour at the theatre, late afternoon at the fair, sunset on the story road, dusk at
        /// the finish. Per stop: zenith, horizon (= fog), sun colour and halo strength, meadow colour and strength, cloud cover, fog density.
        /// </summary>
        private static RideTheme.RegionLook[] Level2Regions() => new[]
        {
            Sky("WORD MACHINE", "#3E8EF0", "#CFE6FF", "#FFE2A8", 0.60f, "#78BE5A", 1f, 0.55f, 0.006f),
            Sky("TOKENS", "#4A95F2", "#DDEFFF", "#FFE9B0", 0.55f, "#86C45A", 1f, 0.45f, 0.006f),
            Sky("MEANING", "#2F6FDF", "#C2DEFF", "#FFF0C8", 0.50f, "#6CB866", 1f, 0.35f, 0.005f),
            Sky("ATTENTION", "#5279D6", "#FFE0B0", "#FFB45E", 0.80f, "#9DBE5E", 1f, 0.60f, 0.006f),
            Sky("PREDICTION", "#6C74D4", "#FFD0BE", "#FF9A70", 0.80f, "#94B860", 1f, 0.60f, 0.006f),
            Sky("WRITING", "#5A62C2", "#FFBE94", "#FF8A55", 0.90f, "#8CAE5E", 1f, 0.70f, 0.007f),
            Sky("THE END", "#3F4AA8", "#FFAE8C", "#FFC27A", 0.90f, "#789E5A", 1f, 0.70f, 0.007f),
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
            // Soft, not additive: additive light vanishes against Level 2's bright sky (ADR-023).
            Particles("L2Ring", Color.white, false, ring);
            Particles("L2Beam", Color.white, false, beam);
            MakeBikeMaterials();
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
            RenderSettings.skybox = DaySkyMaterial();
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
            Ref(kit, "glow", Mat("L2Soft"));
            Ref(kit, "ring", Mat("L2Ring"));
            Ref(kit, "beam", Mat("L2Beam"));
            Ref(kit, "shapes", Mat("ExplainerFlat"));
            Ref(kit, "solid", Mat("L2Solid"));
            Ref(kit, "font", _font);
            Ref(kit, "serif", ExplainerFont());
            Ref(kit, "outline", OutlinedFont(_font, "L2TextOutline"));
            Ref(kit, "serifOutline", OutlinedFont(ExplainerFont(), "L2SerifOutline"));
            Ref(kit, "theme", _theme);

            // The bike path from the dock to the end, always on (the roadside clears where each stop shows its boards).
            var track = new GameObject("Track").AddComponent<TrackWorld>();
            Set(track, "stops", p =>
            {
                p.arraySize = StopZ.Length;
                for (int i = 0; i < StopZ.Length; i++) p.GetArrayElementAtIndex(i).floatValue = StopZ[i];
            });
            Set(track, "extent", p => p.vector2Value = new Vector2(StopZ[0] - 40f, StopZ[StopZ.Length - 1] + 90f));
            Set(track, "longClear", p => p.vector2Value = new Vector2(StopZ[5] - 6f, StopZ[5] + 80f)); // the three forks run 48 m past the stop

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
            BuildPixel2(pod, rd, dash, regions[6]);
            WireTable2(pod, rd);
            WireLasers(rig);
            BuildBoot(pod, rd, regions);

            AssertOnlyRideRootsRender();
            AssertRideAudioLevels(pod);
            AssertRideSfxLevels(pod);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scene2Path);
            SetBuildScenes2();
        }

        /// <summary>
        /// Pixel rides Level 2 too, upgraded (BuildPixel with _level2), and a big copy of it stands on the finish line's screen for the
        /// finale (owner playtest 2026-11-10: the end should show Pixel itself, not a flat face). OutroExplainer drives the copy.
        /// </summary>
        private static void BuildPixel2(GameObject pod, RideDirector rd, DashScreenView dash, GameObject finish)
        {
            PixelBuddyView pixel = BuildPixel(pod, rd, dash, null);
            GameObject portrait = Object.Instantiate(pixel.transform.Find("Body").gameObject, finish.transform);
            portrait.name = "PixelPortrait";
            Object.DestroyImmediate(portrait.GetComponent<AudioSource>());
            portrait.transform.localPosition = new Vector3(-3.4f, 2.3f, 7.6f);
            portrait.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            portrait.transform.localScale = Vector3.one * 4.2f;
            var outro = rd.GetComponentInChildren<OutroExplainer>(true);
            Ref(outro, "portrait", portrait.transform);
            Ref(outro, "portraitFace", portrait.transform.Find("Face").GetComponent<Convergence.Presentation.Ride.Explainers.ShapeBatch>());
            Ref(outro, "portraitLock", portrait.transform.Find("Chat/Lock"));
            Ref(outro, "portraitBubble", portrait.transform.Find("Chat/Bubble").GetComponent<Renderer>());
            Ref(outro, "bubbleOpen", Mat("PixelBubbleOpen"));
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
            Game(spot, "BE THE ATTENTION", "Hold your laser on the words that explain the glowing word", RideLine.AttDone, 120f);
            Game(bet, "GUESS LIKE THE MACHINE", "Which word comes next? More chips = more likely", RideLine.PredDone, 140f);
            Game(road, "STEER THE STORY", "Guess the machine's favourite next word", RideLine.GenDone, 120f);
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

        private static void SetBuildScenes2() => OrderBuildScenes();

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

            // Each explainer hears the lines of its stop's sequences (Level2NarrationData), so a new line's cues always reach it.
            RideLine[] Of(params int[] sequences)
            {
                var all = new List<RideLine>();
                foreach (int i in sequences) all.AddRange(Level2NarrationData.Sequences[i].Lines);
                return all.ToArray();
            }

            // The dock: the intro's lines.
            regions[0].AddComponent<DockWorld>();
            Explainer<WordDockExplainer>(Level2NarrationData.IntroLines, -1, S2Tokens, 0);

            // Stop 1: tokens.
            regions[1].AddComponent<TokenWorld>();
            Explainer<TokenizeExplainer>(Of(S2Tokens, S2Slice), S2Tokens, S2Slice, 1);
            var sliceView = Make("SliceView", regions[1].transform, Vector3.zero).AddComponent<SliceView>();
            Ref(sliceView, "moment", slice);
            Ref(sliceView, "stage", regions[1].transform);

            // Stop 2: meaning.
            regions[2].AddComponent<GalaxyWorld>();
            Explainer<MeaningExplainer>(Of(S2Meaning, S2Sort), S2Meaning, S2Sort, 2);
            var galaxyView = Make("GalaxyView", regions[2].transform, Vector3.zero).AddComponent<GalaxyView>();
            Ref(galaxyView, "moment", galaxy);
            Ref(galaxyView, "stage", regions[2].transform);

            // Stop 3: attention.
            regions[3].AddComponent<HallWorld>();
            Explainer<AttentionExplainer>(Of(S2Attention, S2Spot), S2Attention, S2Spot, 3);
            var spotView = Make("SpotlightView", regions[3].transform, Vector3.zero).AddComponent<SpotlightView>();
            Ref(spotView, "moment", spot);
            Ref(spotView, "stage", regions[3].transform);

            // Stop 4: prediction.
            regions[4].AddComponent<OddsWorld>();
            Explainer<OddsExplainer>(Of(S2Prediction, S2Bet), S2Prediction, S2Bet, 4);
            var predictView = Make("PredictView", regions[4].transform, Vector3.zero).AddComponent<PredictView>();
            Ref(predictView, "moment", bet);
            Ref(predictView, "stage", regions[4].transform);

            // Stop 5: writing.
            regions[5].AddComponent<RoadWorld>();
            Explainer<WritingExplainer>(Of(S2Writing, S2Road), S2Writing, S2Road, 5);
            var storyView = Make("StoryView", regions[5].transform, Vector3.zero).AddComponent<StoryView>();
            Ref(storyView, "moment", road);
            Ref(storyView, "stage", regions[5].transform);

            // The finale: Pixel speaks the rider's story.
            regions[6].AddComponent<EndWorld>();
            var outro = Explainer<OutroExplainer>(Of(S2Finale), S2Finale, S2Finale, 6);
            Ref(outro, "session", session);
            Set(outro, "keepContent", p => p.boolValue = true); // the finale stays up behind the end card
        }
    }
}
