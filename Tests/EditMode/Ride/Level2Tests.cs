using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride;

namespace Convergence.Tests.EditMode.Ride
{
    /// <summary>
    /// Level 2, "Inside the Word Machine" (TASK_LEVEL2_WORD_MACHINE): the baked GPT-2 data keeps its promises, the games' rules hold when
    /// they are driven through their own picks, the narration and the built scene are what the ride needs. The data is real model output,
    /// so these tests also guard against a bad re-bake (ArtSource/training/bake_level2.py).
    /// </summary>
    public class Level2Tests
    {
        private const string LibraryPath = "Assets/ScriptableObjects/NarrationLibrary_L2.asset";
        private const string ScriptPath = "Assets/ScriptableObjects/RideScript_L2.asset";
        private const string ScenePath = "Assets/Scenes/WordMachine.unity";

        private static Level2Data Data()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level2Data.AssetPath);
            Assert.IsNotNull(asset, Level2Data.AssetPath + " is missing. Run ArtSource/training/bake_level2.py.");
            return Level2Data.Parse(asset.text);
        }

        // ---------- the bake ----------

        [Test]
        public void TheBake_IsTheRealGpt2_AndKeepsItsPromises()
        {
            Level2Data data = Data();
            StringAssert.Contains("GPT-2", data.File.meta.model);
            Assert.AreEqual(50257, data.File.meta.vocab, "GPT-2's vocabulary");
            Assert.AreEqual(768, data.File.meta.dims, "the narrator says seven hundred and sixty-eight numbers a word");
            Assert.That(data.File.meta.parameters, Is.InRange(120000000, 128000000), "the narrator says one hundred and twenty-four million");
            Assert.AreEqual(970, Convergence.Core.Training.DigitNetwork.ParameterCount, "the narrator compares Pixel's 970 numbers");
        }

        [Test]
        public void SliceWords_AreTheModelsRealSplits()
        {
            Level2Data data = Data();
            Assert.AreEqual(1, data.Word("cat").tokens.Length, "a common word stays whole");
            Assert.AreEqual(1, data.Word("unbelievable").tokens.Length, "a long word GPT-2 still knows whole");
            Assert.AreEqual(2, data.Word("jellyfish").tokens.Length);
            Assert.GreaterOrEqual(data.Word(Level2Data.BossWord).tokens.Length, 8, "the boss is chopped into many pieces");
            Assert.AreEqual(11, data.Word(Level2Data.BossWord).tokens.Length, "the narrator says eleven tokens");
            int whole = 0, chopped = 0;
            foreach (string w in Level2Data.SliceWords) if (data.Word(w).tokens.Length == 1) whole++; else chopped++;
            Assert.GreaterOrEqual(whole, 3, "some words must be left alone");
            Assert.GreaterOrEqual(chopped, 5, "and most must be chopped");
            foreach (string w in Level2Data.SliceWords)
            {
                L2Word baked = data.Word(w);
                Assert.AreEqual(w, string.Concat(Array.ConvertAll(baked.tokens, t => t.Trim())), w + "'s pieces spell the word");
            }
        }

        [Test]
        public void ThreadSentence_IsTokenisedAsTheExplainerShowsIt()
        {
            Level2Data data = Data();
            Assert.AreEqual("Because the cat was hungry, it chased the mouse.", data.File.thread.text);
            string joined = string.Concat(Array.ConvertAll(data.File.thread.tokens, t => t.text));
            Assert.AreEqual(data.File.thread.text, joined, "the tokens spell the sentence");
            Assert.IsNotNull(Array.Find(data.File.thread.tokens, t => t.text == " cat" && t.id == 3797), "GPT-2's id for ' cat'");
        }

        [Test]
        public void EveryPredictionRound_OffersTheTrueWord_WithHonestOdds()
        {
            Level2Data data = Data();
            var go = new GameObject("PredictTest");
            try
            {
                var source = go.AddComponent<Level2DataSource>();
                source.Use(data);
                var moment = go.AddComponent<PredictMoment>();
                new SerializedObject(moment).FindProperty("source").objectReferenceValue = source;
                var so = new SerializedObject(moment);
                so.FindProperty("source").objectReferenceValue = source;
                so.ApplyModifiedPropertiesWithoutUndo();
                moment.Prepare();
                for (int r = 0; r < Level2Data.PredictPrompts.Length; r++)
                {
                    PredictMoment.Offer offer = moment.OfferAt(r);
                    Assert.IsNotNull(offer, "round " + r);
                    float sum = 0f;
                    foreach (float p in offer.Odds) sum += p;
                    Assert.AreEqual(1f, sum, 0.01f, offer.Prompt + ": the odds add up to one (with the rest)");
                    Assert.AreEqual(Level2Data.Clean(offer.Truth), offer.Words[offer.TruthIndex], offer.Prompt + ": the true word is on offer");
                    Assert.That(offer.TruthOdds, Is.EqualTo(offer.Odds[offer.TruthIndex]).Within(1e-4f));
                    for (int i = 1; i < offer.Words.Length; i++) Assert.LessOrEqual(string.CompareOrdinal(offer.Words[i - 1], offer.Words[i]), 0, "alphabetical, so the order never gives the answer away");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Predictions_HaveTheShapesTheNarratorDescribes()
        {
            Level2Data data = Data();
            L2Prediction easy = data.Prediction("Once upon a");
            Assert.Greater(easy.truthP, 0.95f, "a sure thing first");
            L2Prediction hard = data.Prediction(Level2Data.ExplainPrompt);
            Assert.That(hard.truthP, Is.InRange(0.005f, 0.02f), "the narrator says 'hungry' only got one percent");
            Assert.Greater(hard.truthRank, 5, "and it was not among the top few");
            Assert.Greater(hard.top.Length, 5);
            Assert.Less(hard.top[0].p, 0.2f, "no clear winner");
        }

        [Test]
        public void TheAttentionRounds_AgreeWithTheRealHead()
        {
            Level2Data data = Data();
            foreach (int r in Level2Data.SpotlightRounds)
            {
                L2Attention a = data.Attention(r);
                float onAnswers = 0f;
                foreach (int i in a.answers) onAnswers += a.weights[i];
                Assert.Greater(onAnswers, 0.35f, a.sentence + ": the real head puts real weight on the words a person would name");
            }

            L2Attention shown = data.Attention(Level2Data.ExplainAttention);
            Assert.AreEqual("creature", shown.words[shown.query]);
            Assert.AreEqual(3, shown.weights.Length, "a word can only look back");
        }

        [Test]
        public void TheGalaxy_KeepsItsNeighbourhoodsApart_AsSeenFromThePod()
        {
            Level2Data data = Data();
            var go = new GameObject("GalaxyTest");
            try
            {
                var source = go.AddComponent<Level2DataSource>();
                source.Use(data);
                var moment = go.AddComponent<GalaxyMoment>();
                var so = new SerializedObject(moment);
                so.FindProperty("source").objectReferenceValue = source;
                so.ApplyModifiedPropertiesWithoutUndo();
                moment.Prepare();
                Assert.AreEqual(Level2Data.Neighbourhoods.Length, moment.Clusters.Count);
                Assert.AreEqual(Level2Data.Neighbourhoods.Length * Level2Data.LoosePerNeighbourhood, moment.LooseTotal);
                Vector3 eye = new Vector3(0f, 1.3f, 0f);
                for (int a = 0; a < moment.Clusters.Count; a++)
                {
                    Vector3 toA = moment.Clusters[a].Centre - eye;
                    Assert.Greater(toA.z, 5f, moment.Clusters[a].Category + " is ahead of the rider");
                    for (int b = a + 1; b < moment.Clusters.Count; b++)
                    {
                        float apart = Vector3.Angle(toA, moment.Clusters[b].Centre - eye);
                        // The drop cone is 11 degrees either side of a centre: two neighbourhoods further apart than two cones never share one.
                        Assert.Greater(apart, 22f, moment.Clusters[a].Category + " and " + moment.Clusters[b].Category + " must be easy to tell apart with a laser");
                    }
                }

                // Every word's own place is nearer its own neighbourhood than any other (what the game calls right is what the picture shows),
                // and the whole picture sits inside the rider's comfortable gaze: not more than 40 degrees to a side, nor 12 below or 25 above the horizon.
                foreach (GalaxyMoment.Item item in moment.Items)
                {
                    Vector3 seen = item.Home - eye;
                    float azimuth = Mathf.Atan2(seen.x, seen.z) * Mathf.Rad2Deg;
                    float elevation = Mathf.Atan2(seen.y, Mathf.Sqrt(seen.x * seen.x + seen.z * seen.z)) * Mathf.Rad2Deg;
                    Assert.Less(Mathf.Abs(azimuth), 40f, item.Word + " is too far to the side");
                    Assert.That(elevation, Is.InRange(-12f, 25f), item.Word + " is too high or too low");
                    GalaxyMoment.Cluster own = null;
                    foreach (GalaxyMoment.Cluster c in moment.Clusters) if (c.Category == item.Category) own = c;
                    Assert.IsNotNull(own, item.Word + " has a neighbourhood");
                    foreach (GalaxyMoment.Cluster other in moment.Clusters)
                    {
                        if (other == own) continue;
                        Assert.Less(Vector3.Distance(item.Home, own.Centre), Vector3.Distance(item.Home, other.Centre), item.Word + " sits in its own neighbourhood");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TheStoryRoad_HasEveryForkAndLane_AndTheSafeLaneIsTheLikeliest()
        {
            Level2Data data = Data();
            Assert.AreEqual(3, data.File.story.forks);
            Assert.AreEqual("My robot loves to", data.File.story.start);
            var queue = new Queue<string>();
            queue.Enqueue("");
            int seen = 0;
            while (queue.Count > 0)
            {
                string path = queue.Dequeue();
                L2StoryNode node = data.StoryNode(path);
                Assert.IsNotNull(node, "fork '" + path + "'");
                seen++;
                for (int k = 1; k < 4; k++) Assert.GreaterOrEqual(node.lanes[0].p, node.lanes[k].p * 0.999f, "lane 0 is the safe word at '" + path + "'");
                if (path.Length + 1 < data.File.story.forks) for (int k = 0; k < 4; k++) queue.Enqueue(path + k);
            }

            Assert.AreEqual(1 + 4 + 16, seen);
        }

        // ---------- the rules of the games, driven through their picks ----------

        private sealed class Harness : IDisposable
        {
            public readonly GameObject Root = new GameObject("Level2Harness");
            public readonly Transform Stage;
            public readonly Level2DataSource Source;

            public Harness()
            {
                Stage = new GameObject("Stage").transform;
                Stage.SetParent(Root.transform, false);
                Source = Root.AddComponent<Level2DataSource>();
                Source.Use(Data());
            }

            public T Add<T>() where T : Component
            {
                T moment = Root.AddComponent<T>();
                var so = new SerializedObject(moment);
                if (so.FindProperty("source") != null) so.FindProperty("source").objectReferenceValue = Source;
                if (so.FindProperty("stage") != null) so.FindProperty("stage").objectReferenceValue = Stage;
                so.ApplyModifiedPropertiesWithoutUndo();
                return moment;
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
        }

        [Test]
        public void SeamOffsets_PlaceTheSeamsBetweenTheLetters()
        {
            string[] tokens = { " jelly", "fish" };
            float left = SliceMoment.SeamOffset(tokens, 0, 0.5f);
            Assert.AreEqual((5 - 9 * 0.5f) * SliceMoment.Advance * 0.5f, left, 1e-4f, "after 'jelly' of 'jellyfish'");
            string[] three = { " roller", "co", "aster" };
            Assert.Less(SliceMoment.SeamOffset(three, 0, 0.5f), SliceMoment.SeamOffset(three, 1, 0.5f), "seams run left to right");
        }

        [Test]
        public void TheLevel2Script_Exists_AndEveryLineHasItsVoice()
        {
            var library = AssetDatabase.LoadAssetAtPath<NarrationLibrary>(LibraryPath);
            var script = AssetDatabase.LoadAssetAtPath<RideScript>(ScriptPath);
            Assert.IsNotNull(library, LibraryPath + " is missing. Run Convergence > Build Word Machine (Level 2).");
            Assert.IsNotNull(script, ScriptPath + " is missing.");
            Assert.GreaterOrEqual(library.entries.Length, 49, "the 47 lines of Level 2 and the two it shares with Level 1");
            foreach (RideLine id in new[] { RideLine.L2Hello, RideLine.TokChop, RideLine.EmbMath, RideLine.AttGo, RideLine.PredOdds, RideLine.GenTemp, RideLine.OutEnd, RideLine.PullGo, RideLine.StopClear })
            {
                Assert.IsTrue(library.TryGet(id, out _), id + " has no entry in the Level 2 library");
            }

            foreach (NarrationLibrary.Entry entry in library.entries)
            {
                Assert.IsNotNull(entry.clip, entry.line + " has no voice clip. Render it with ArtSource/voice/render_voices.py, install, and rebuild.");
                Assert.Greater(entry.segments.Length, 0, entry.line + " has no subtitle segments");
                Assert.AreEqual(NarrationLibrary.Speaker.Guide, entry.speaker);
            }

            Assert.AreEqual(11, script.sequences.Length, "one sequence per explain or play stop, and the finale");
            Assert.IsFalse(script.intro.IsEmpty);
            float all = 0f;
            foreach (NarrationLibrary.Entry e in library.entries) all += e.TotalSeconds;
            // v3 (owner playtest 2026-11-10) asked for more explaining: how tokens are chosen (the glue list) and king - man + woman on
            // the real numbers. Those two stops grew to about 70 s, the whole script to about 480 s; the caps follow, with a little room.
            Assert.LessOrEqual(all, 500f, "all the narration of Level 2, with the reactions (v2, ADR-023: pauses about twice as long; v3: the owner's clearer explanations)");
            foreach (RideScript.Sequence s in script.sequences) Assert.LessOrEqual(s.TotalSeconds, 75f, s.name + " runs long");
        }

        [Test]
        public void Level2Lines_NeverReuseALevel1Number()
        {
            Assert.AreEqual(200, (int)RideLine.L2Hello);
            Assert.AreEqual(209, (int)RideLine.TokDone);
            Assert.AreEqual(224, (int)RideLine.AttDone);
            Assert.AreEqual(241, (int)RideLine.OutEnd);
            Assert.AreEqual(246, (int)RideLine.OutCareful);
            Assert.AreEqual(184, (int)RideLine.PixelsWhy49);
            Assert.AreEqual(188, (int)RideLine.DescentFog);
        }

        [Test]
        public void TheBuiltLevel2Scene_HasTheRideAndEveryGame()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                RideDirector director = null;
                var moments = new List<RideMoment>();
                var explainers = new List<Convergence.Presentation.Ride.Explainers.Explainer>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (director == null) director = root.GetComponentInChildren<RideDirector>(true);
                    moments.AddRange(root.GetComponentsInChildren<RideMoment>(true));
                    explainers.AddRange(root.GetComponentsInChildren<Convergence.Presentation.Ride.Explainers.Explainer>(true));
                }

                Assert.IsNotNull(director, "the scene has a RideDirector");
                Assert.AreEqual(11, director.SegmentCount, "five stops, each explained then played, and the finale");
                string[] games = { "SliceMoment", "GalaxyMoment", "SpotlightMoment", "PredictMoment", "StoryMoment" };
                foreach (string g in games) Assert.IsTrue(moments.Exists(m => m.GetType().Name == g), g + " is in the scene");
                string[] explain = { "WordDockExplainer", "TokenizeExplainer", "MeaningExplainer", "AttentionExplainer", "OddsExplainer", "WritingExplainer", "OutroExplainer" };
                foreach (string e in explain) Assert.IsTrue(explainers.Exists(x => x.GetType().Name == e), e + " is in the scene (a missing script shows up here)");
                foreach (var seg in director.Segments)
                {
                    Assert.GreaterOrEqual(seg.to, 0f);
                }

                // Every line an explainer's stop says must reach that explainer, or its steps for the line's cues fire late, all at once.
                var script = AssetDatabase.LoadAssetAtPath<RideScript>(ScriptPath);
                var byName = new Dictionary<string, int[]>
                {
                    { "MeaningExplainer", new[] { 2, 3 } }, { "AttentionExplainer", new[] { 4, 5 } }, { "OutroExplainer", new[] { 10 } },
                };
                foreach (var x in explainers)
                {
                    if (!byName.TryGetValue(x.GetType().Name, out int[] sequences)) continue;
                    SerializedProperty lines = new SerializedObject(x).FindProperty("lines");
                    var heard = new List<int>();
                    for (int i = 0; i < lines.arraySize; i++) heard.Add(lines.GetArrayElementAtIndex(i).intValue);
                    foreach (int q in sequences)
                    {
                        foreach (RideScript.Beat beat in script.sequences[q].beats) Assert.Contains((int)beat.line, heard, x.GetType().Name + " does not hear " + beat.line);
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TheBuiltLevel2Scene_IsABikeRideInDaylight()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject pod = null, track = null;
                Convergence.Presentation.Ride.Level2.Level2Kit kit = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "Pod") pod = root;
                    if (root.name == "Track") track = root;
                    if (kit == null) kit = root.GetComponentInChildren<Convergence.Presentation.Ride.Level2.Level2Kit>(true);
                }

                Assert.IsNotNull(pod, "the rider's Pod");
                var bike = pod.GetComponent<Convergence.Presentation.Ride.Level2.BikeView>();
                Assert.IsNotNull(bike, "Level 2's pod is a bike (ADR-023)");
                foreach (string part in new[] { "frontWheel", "rearWheel", "crank", "bars" })
                {
                    Assert.IsNotNull(new SerializedObject(bike).FindProperty(part).objectReferenceValue, "the bike's " + part + " is wired");
                }

                Assert.IsNull(pod.transform.Find("Tub"), "no Level 1 cockpit tub on the bike");
                Assert.IsNotNull(track, "the bike path runs the whole ride (Track)");
                Assert.IsNotNull(track.GetComponent<Convergence.Presentation.Ride.Level2.TrackWorld>());
                Assert.IsNotNull(kit, "the scene has the Level 2 kit");
                Assert.IsNotNull(new SerializedObject(kit).FindProperty("outline").objectReferenceValue, "outlined text, readable on a bright sky");
                // RenderSettings are the active scene's: make Level 2's active to read its sky.
                UnityEngine.SceneManagement.Scene previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                string sky = RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "";
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
                Assert.AreEqual("Convergence/DaySky", sky, "Level 2's sky is daylight");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void AStoryEndsWithDots_WhenItsLastWordCannotEndASentence()
        {
            Assert.IsTrue(StoryMoment.EndsOpen("a"), "talk like a ...");
            Assert.IsTrue(StoryMoment.EndsOpen("The"));
            Assert.IsFalse(StoryMoment.EndsOpen("me"), "play with me.");
            Assert.IsFalse(StoryMoment.EndsOpen("games"));
        }

        [Test]
        public void TheLevel2Theme_IsDaylightWithAMeadow_NoneOrange()
        {
            var theme = AssetDatabase.LoadAssetAtPath<RideTheme>("Assets/ScriptableObjects/RideTheme_L2.asset");
            Assert.IsNotNull(theme);
            Assert.AreEqual(7, theme.regions.Length, "the dock, five stops and the end");
            Assert.AreEqual(1f, theme.blackboardFloor, "the meadow stays green while a board explains");
            foreach (RideTheme.RegionLook look in theme.regions)
            {
                Color.RGBToHSV(look.skyHorizon, out float h, out float s, out float v);
                Assert.Greater(v, 0.6f, look.tag + ": a daylight horizon; Level 2 must never look like Level 1's night (owner, 2026-10-31)");
                Color.RGBToHSV(look.skyTop, out _, out _, out float top);
                Assert.Greater(top, 0.35f, look.tag + ": a blue sky overhead, not black");
                Assert.GreaterOrEqual(look.floor.a, 0.9f, look.tag + ": the bike rides over a meadow");
                bool orange = h > 0.04f && h < 0.11f && s > 0.6f && v > 0.5f;
                Assert.IsFalse(orange, look.tag + " must not be orange (orange means press this)");
            }
        }
    }
}
