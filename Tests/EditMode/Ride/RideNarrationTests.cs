using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Convergence.EditorTools;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;

namespace Convergence.Tests.EditMode.Ride
{
    /// <summary>
    /// Checks the data Level 1 runs on (the RideScript and NarrationLibrary the builder writes from RideNarrationData,
    /// the region looks), the stability of the narration line ids, the narrator's numbers against the bake, and the
    /// sightline and model-sheet validators on the built scene. They read the built assets, so run
    /// Convergence > Build Neural Ride first on a fresh clone. (Replaces the v3 tests of the intro's AURA beats, the
    /// three briefings and the chapter cards, which Level 1 cut; see TASK_LEVEL1_DIGITS_RESTRUCTURE, progress log. The
    /// line lists follow the v4 script, TASK_LEVEL1_V4_EXPLAINERS section 3: Level 1's lines are 64 and up now.)
    /// </summary>
    public class RideNarrationTests
    {
        private const string LibraryPath = "Assets/ScriptableObjects/NarrationLibrary.asset";
        private const string ScriptPath = "Assets/ScriptableObjects/RideScript.asset";
        private const string ThemePath = "Assets/ScriptableObjects/RideTheme.asset";
        private const string ScenePath = "Assets/Scenes/NeuralRide.unity";

        // Lines said by moments, not by a sequence (RideNarrationData.MomentLines).
        private static readonly RideLine[] MomentLines =
        {
            RideLine.DrawBigger, RideLine.DrawDoneNudge, RideLine.DrawDone, RideLine.ZapIntro, RideLine.ZapWrong,
            RideLine.NeuronLitForOther, RideLine.NeuronMissedThree, RideLine.NeuronSolved,
            RideLine.BackHurry, RideLine.BackDone, RideLine.StopClear,
            RideLine.TrainTooBig, RideLine.TrainTooSmall, RideLine.TrainGood, RideLine.TrainDone,
            RideLine.LearnedPainted, RideLine.LearnedHonest, RideLine.RightYours, RideLine.RightYes, RideLine.RightStudy, RideLine.RightNowYes, RideLine.RightAccuracy, RideLine.RightLoop,
            RideLine.StumpYourTurn, RideLine.StumpRight, RideLine.StumpWrong, RideLine.StumpDone
        };

        /// <summary>The first line of the v6 script; 1 to 118 are reserved (v3, the first Level 1 script, v4 and v5).</summary>
        private const RideLine FirstLine = RideLine.Threes;

        private static NarrationLibrary Library()
        {
            var library = AssetDatabase.LoadAssetAtPath<NarrationLibrary>(LibraryPath);
            Assert.IsNotNull(library, "NarrationLibrary.asset is missing. Run Convergence > Build Neural Ride.");
            return library;
        }

        private static RideScript Script()
        {
            var script = AssetDatabase.LoadAssetAtPath<RideScript>(ScriptPath);
            Assert.IsNotNull(script, "RideScript.asset is missing. Run Convergence > Build Neural Ride.");
            return script;
        }

        private static IEnumerable<RideLine> Level1Lines()
        {
            foreach (RideLine line in Enum.GetValues(typeof(RideLine)))
            {
                if ((int)line >= (int)FirstLine) yield return line;
            }
        }

        private static string Spoken(NarrationLibrary library, RideLine line)
        {
            Assert.IsTrue(library.TryGet(line, out NarrationLibrary.Entry entry), line + " has no entry");
            var words = new List<string>();
            foreach (NarrationLibrary.Segment s in entry.segments) words.Add(s.text);
            return string.Join(" ", words).ToLowerInvariant();
        }

        [Test]
        public void EveryLevel1Line_HasASubtitleAndADuration()
        {
            NarrationLibrary library = Library();
            foreach (RideLine line in Level1Lines())
            {
                Assert.IsTrue(library.TryGet(line, out NarrationLibrary.Entry entry), line + " has no entry in the narration library");
                Assert.IsNotEmpty(entry.tag, line + " has no speaker tag");
                Assert.IsNotNull(entry.segments, line + " has no subtitle segments");
                Assert.Greater(entry.segments.Length, 0, line + " has no subtitle segments");
                foreach (NarrationLibrary.Segment segment in entry.segments)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(segment.text), line + " has an empty subtitle segment");
                    Assert.Greater(segment.weight, 0f, line + " has a subtitle segment with no weight");
                }

                Assert.Greater(entry.TotalSeconds, 0.5f, line + " has no duration");
            }
        }

        [Test]
        public void EveryLevel1Line_HasAVoiceClipMatchingTheScript()
        {
            // The builder binds a clip only when its sidecar says the approved words, so a missing clip here means a
            // line was re-worded without re-rendering (ArtSource/voice/README.md), or a file was deleted.
            NarrationLibrary library = Library();
            foreach (RideLine line in Level1Lines())
            {
                Assert.IsTrue(library.TryGet(line, out NarrationLibrary.Entry entry), line + " has no entry in the narration library");
                Assert.IsNotNull(entry.clip, line + " has no voice clip. Render it with ArtSource/voice/render_voices.py, install, and rebuild the ride.");
                Assert.Greater(entry.clip.length, 0.5f, line + "'s clip is empty");
                Assert.AreEqual(1, entry.clip.channels, line + "'s clip should be mono");
            }
        }

        [Test]
        public void TheCutStory_IsNotInTheLibrary()
        {
            // No AURA, JFK or Apollo left in the ride, and none of the first Level 1 script: the old lines keep their
            // numbers but have no entry and no subtitle.
            NarrationLibrary library = Library();
            foreach (RideLine line in Enum.GetValues(typeof(RideLine)))
            {
                if ((int)line < (int)FirstLine) Assert.IsFalse(library.TryGet(line, out _), line + " (reserved, before 119) is still in the library");
            }

            foreach (NarrationLibrary.Entry entry in library.entries)
            {
                Assert.AreEqual(NarrationLibrary.Speaker.Guide, entry.speaker, entry.line + ": Level 1 has one narrator, the Guide");
                string text = Spoken(library, entry.line);
                foreach (string cut in new[] { "aura", "apollo", "kennedy", "moon" }) StringAssert.DoesNotContain(cut, text, entry.line + " mentions the cut story");
            }
        }

        [Test]
        public void NarrationLineIds_AreNeverRenumbered()
        {
            // Voice files are named vo_ride_NN.wav after these numbers, so they are a contract. Append only.
            Assert.AreEqual(1, (int)RideLine.Launch);
            Assert.AreEqual(15, (int)RideLine.CourseLocked);
            Assert.AreEqual(16, (int)RideLine.ArchivalMoon);
            Assert.AreEqual(24, (int)RideLine.ApolloLiftoff);
            Assert.AreEqual(31, (int)RideLine.GuideMakeItFire);
            Assert.IsFalse(Enum.IsDefined(typeof(RideLine), 23), "Id 23 is reserved and intentionally unused");
            Assert.AreEqual(32, (int)RideLine.PageLook);
            Assert.AreEqual(42, (int)RideLine.V3NeuronYourTurn);
            Assert.AreEqual(49, (int)RideLine.WrongItSays);
            Assert.AreEqual(56, (int)RideLine.ValleyGradientDescent);
            Assert.AreEqual(61, (int)RideLine.V3RightAccuracy);
            Assert.AreEqual(63, (int)RideLine.RightNextLevel);

            // v4 and v5, appended from 64, reserved since v6 (V5 prefix).
            Assert.AreEqual(64, (int)RideLine.V5Threes);
            Assert.AreEqual(68, (int)RideLine.V5PixelsSquares);
            Assert.AreEqual(76, (int)RideLine.V5NeuronYourTurn);
            Assert.AreEqual(84, (int)RideLine.V5WrongSays);
            Assert.AreEqual(93, (int)RideLine.V5DescentHill);
            Assert.AreEqual(101, (int)RideLine.V5RightAccuracy);
            Assert.AreEqual(105, (int)RideLine.V5TokensNext);
            Assert.AreEqual(118, (int)RideLine.V5DescentGameSlow);

            // v6, appended from 119.
            Assert.AreEqual(119, (int)RideLine.Threes);
            Assert.AreEqual(123, (int)RideLine.PixelsSquares);
            Assert.AreEqual(135, (int)RideLine.NeuronYourTurn);
            Assert.AreEqual(143, (int)RideLine.WrongSays);
            Assert.AreEqual(152, (int)RideLine.DescentHill);
            Assert.AreEqual(164, (int)RideLine.RightAccuracy);
            Assert.AreEqual(173, (int)RideLine.TokensNext);

            // v7, appended from 174.
            Assert.AreEqual(174, (int)RideLine.IntroStops);
            Assert.AreEqual(177, (int)RideLine.LearnedHonest);
        }

        [Test]
        public void RideScript_BeatsMatchTheLibrary_AndEveryLineIsUsed()
        {
            NarrationLibrary library = Library();
            RideScript script = Script();
            var used = new HashSet<RideLine>(MomentLines) { script.dockLine };
            CheckSequence(library, script.intro, "intro", used);
            Assert.AreEqual(10, script.sequences.Length, "One sequence per explain or play stop (RideNarrationData.Sequences)");
            for (int i = 0; i < script.sequences.Length; i++) CheckSequence(library, script.sequences[i], script.sequences[i].name, used);
            foreach (RideLine line in Level1Lines()) Assert.IsTrue(used.Contains(line), line + " is never said");
            foreach (RideLine line in MomentLines) Assert.Greater(script.SecondsOf(line), 0.5f, line + " has no seconds in the script");
        }

        private static void CheckSequence(NarrationLibrary library, RideScript.Sequence sequence, string name, HashSet<RideLine> used)
        {
            Assert.IsFalse(sequence.IsEmpty, name + " has no lines");
            foreach (RideScript.Beat beat in sequence.beats)
            {
                Assert.IsTrue(library.TryGet(beat.line, out NarrationLibrary.Entry entry), name + " plays " + beat.line + ", which the library lacks");
                Assert.AreEqual(entry.TotalSeconds, beat.seconds, 0.01f, name + " paces " + beat.line + " differently from the library. Rebuild the ride.");
                used.Add(beat.line);
            }
        }

        [Test]
        public void Narration_FitsTheRide()
        {
            // Level 1 is about five to six minutes; the talk is most of it, broken up by the puzzle, the race, the blame and the drop.
            NarrationLibrary library = Library();
            RideScript script = Script();
            float all = 0f;
            foreach (NarrationLibrary.Entry e in library.entries) all += e.TotalSeconds;
            Assert.LessOrEqual(all, 270f, "All the narration together should stay under four and a half minutes");
            foreach (RideScript.Sequence s in script.sequences) Assert.LessOrEqual(s.TotalSeconds, 40f, s.name + " runs over 40 s; a first-time rider drifts");
        }

        [Test]
        public void Intro_GetsTheRidersHandOnGoInUnder25Seconds()
        {
            RideScript script = Script();
            Assert.LessOrEqual(script.intro.TotalSeconds + script.SecondsOf(script.dockLine), 25f, "The intro and the dock line run too long before GO");
            Assert.AreEqual(RideLine.PullGo, script.dockLine);
        }

        [Test]
        public void NarratorNumbers_ComeFromTheBake()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level1Data.AssetPath);
            Level1Data data = Level1Data.Parse(asset.text);
            NarrationLibrary library = Library();
            Assert.AreEqual(32, data.File.live.batch, "Re-baked? Update TrainYourTurn's words to the new batch.");
            StringAssert.Contains("thirty-two real digits", Spoken(library, RideLine.TrainYourTurn));
            Assert.AreEqual(500, data.File.live.testLabels.Length, "Re-baked? Update RightAccuracy's words to the new test set.");
            StringAssert.Contains("five hundred digits", Spoken(library, RideLine.RightAccuracy));
            StringAssert.Contains("almost nine out of ten", Spoken(library, RideLine.RightAccuracy));
            Assert.That(data.File.live.targetAccuracy, Is.InRange(0.8, 0.9), "almost nine out of ten");
            Assert.AreEqual(944, 16 * 49 + 10 * 16);
            StringAssert.Contains("nine hundred and forty-four weights", Spoken(library, RideLine.NetworkWire));
            Assert.AreEqual(970, Convergence.Core.Training.DigitNetwork.ParameterCount);
            StringAssert.Contains("nine hundred and seventy numbers", Spoken(library, RideLine.StumpDone));
        }

        [Test]
        public void EveryRegion_HasItsOwnLookAndTag()
        {
            var theme = AssetDatabase.LoadAssetAtPath<RideTheme>(ThemePath);
            Assert.IsNotNull(theme);
            Assert.AreEqual(8, theme.regions.Length, "Eight regions, 0 the dock to 7 the trained network");
            var tags = new HashSet<string>();
            var skies = new HashSet<Color>();
            foreach (RideTheme.RegionLook look in theme.regions)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(look.tag));
                Assert.IsTrue(tags.Add(look.tag), "Two regions share the tag " + look.tag);
                Assert.IsTrue(skies.Add(look.background), look.tag + " has the same sky as another region");
                Assert.Greater(look.fogDensity, 0f);
                Color.RGBToHSV(look.background, out float h, out float sat, out float v);
                bool orange = h > 0.04f && h < 0.11f && sat > 0.6f && v > 0.5f;
                Assert.IsFalse(orange, look.tag + "'s sky is orange; orange means 'you can press this'");
            }
        }

        [Test]
        public void RegionTags_AreTopicNames_WithNoNumbers()
        {
            var theme = AssetDatabase.LoadAssetAtPath<RideTheme>(ThemePath);
            string[] topics = { "PIXELS", "NEURON", "NEURAL NETWORK", "LOSS", "BACKPROPAGATION", "GRADIENT DESCENT", "TRAINING" };
            for (int r = 1; r <= 7; r++) Assert.AreEqual(topics[r - 1], RegionTagView.TagFor(theme, r), "region " + r);
            foreach (RideTheme.RegionLook look in theme.regions)
            {
                StringAssert.DoesNotMatch("[0-9·]", look.tag, "an elegant title, no numbering");
            }

            // v7 (owner: "the scenes shouldn't be all black"): every region is a lit sky, dark enough at eye level for
            // white text; far things sink into its horizon; explaining dims the region's own sky, never to black.
            foreach (RideTheme.RegionLook look in theme.regions)
            {
                Color.RGBToHSV(look.skyHorizon, out _, out _, out float value);
                Assert.Greater(value, 0.08f, look.tag + ": the horizon is lit, not a black void");
                Assert.Less(value, 0.5f, look.tag + ": the horizon stays dark enough for white text");
                Assert.AreEqual(look.skyHorizon, look.fog, look.tag + ": the fog matches the horizon");
                Assert.LessOrEqual(look.skyGlow.a, 0.75f, look.tag + ": the horizon glow never washes out the text");
            }

            Assert.That(theme.blackboardDim, Is.InRange(0.4f, 0.85f), "explaining dims the region's sky, never to black");
        }

        [Test]
        public void BuiltRide_HasOneExplainerPerBeat_InTheSegmentOrder()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var explainers = new List<Convergence.Presentation.Ride.Explainers.Explainer>();
                foreach (GameObject root in scene.GetRootGameObjects()) explainers.AddRange(root.GetComponentsInChildren<Convergence.Presentation.Ride.Explainers.Explainer>(true));
                string[] beats = { "DockExplainer", "PixelsExplainer", "NeuronExplainer", "NetworkExplainer", "LossExplainer", "BackpropExplainer", "DescentExplainer", "TrainingExplainer", "TokensExplainer" };
                Assert.AreEqual(beats.Length, explainers.Count, "E0 to E8, one component each (a missing script shows up here)");
                int last = -2;
                for (int i = 0; i < beats.Length; i++)
                {
                    Assert.AreEqual(beats[i], explainers[i].GetType().Name);
                    Assert.GreaterOrEqual(explainers[i].ExplainSegment, last, beats[i] + " explains after the beat before it");
                    Assert.GreaterOrEqual(explainers[i].LastSegment, explainers[i].ExplainSegment, beats[i] + " unfolds by a later segment");
                    last = explainers[i].ExplainSegment;
                }

                Assert.IsNotNull(explainers[0].Content, "E0 has its pieces");
                Assert.IsNotNull(explainers[1].Content, "E1 has its pieces");
                Assert.IsFalse(explainers[0].Content.activeSelf, "explainer pieces are saved off; the explainer shows them");
                Assert.IsNotNull(explainers[0].Content.transform.Find("PixelAnchor"), "E0 keeps an anchor for Pixel (WP2)");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void SegmentAt_FollowsTheLineFromFirstClauseToLast()
        {
            var entry = new NarrationLibrary.Entry
            {
                segments = new[]
                {
                    new NarrationLibrary.Segment { text = "one", weight = 1f },
                    new NarrationLibrary.Segment { text = "two", weight = 2f },
                    new NarrationLibrary.Segment { text = "three", weight = 1f }
                }
            };

            Assert.AreEqual("one", NarrationLibrary.SegmentAt(entry, 0f));
            Assert.AreEqual("two", NarrationLibrary.SegmentAt(entry, 0.5f));
            Assert.AreEqual("three", NarrationLibrary.SegmentAt(entry, 1f));
            Assert.AreEqual(string.Empty, NarrationLibrary.SegmentAt(default, 0.5f));
        }

        [Test]
        public void RayTriangle_HitsOnlyBetweenTheEyeAndTheTarget()
        {
            Vector3 a = new Vector3(-1, -1, 5), b = new Vector3(1, -1, 5), c = new Vector3(0, 1, 5);
            Assert.IsTrue(RideSightlines.RayTriangle(Vector3.zero, Vector3.forward, a, b, c, out float hit));
            Assert.AreEqual(5f, hit, 1e-4f);
            Assert.IsFalse(RideSightlines.RayTriangle(Vector3.zero, new Vector3(3, 0, 1).normalized, a, b, c, out _), "A ray that misses the triangle");
            Assert.IsTrue(RideSightlines.RayTriangle(new Vector3(0, 0, 10), Vector3.back, a, b, c, out float back), "Two-sided: hits from behind too");
            Assert.AreEqual(5f, back, 1e-4f);
        }

        private static GameObject Root(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }

            Assert.Fail(name + " is missing from the built ride");
            return null;
        }

        [Test]
        public void ABlockerInFrontOfThePanel_IsCaught()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Transform panel = Root(scene, "Region_2").transform.Find("Panel");
                Assert.IsNotNull(panel, "Region_2/Panel is missing from the built ride");

                // A cube on the sight line from the seat to the why panel, in the region that is live at the neuron stop.
                GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(blocker, scene);
                blocker.transform.SetParent(panel.parent, false);
                blocker.transform.localPosition = new Vector3(0f, 1.25f, 1.6f);
                blocker.transform.localScale = Vector3.one * 0.5f;
                blocker.name = "TestBlocker";

                var problems = RideSightlines.FindBlockers(scene);
                Assert.IsTrue(problems.Exists(line => line.Contains("Panel panel") && line.Contains("TestBlocker")),
                    "The validator must report the why panel hidden by TestBlocker. It said: " + string.Join(" | ", problems));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuiltRide_HasNoBlockedPanelsAndNoUnreadableText()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var problems = RideSightlines.FindBlockers(scene);
                Assert.IsEmpty(problems, "Sightline check:\n  - " + string.Join("\n  - ", problems));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuiltRide_EveryModelHasASheetAndEverySheetFindsItsModel()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var uncovered = RideModelSheets.FindUncovered(scene);
                Assert.IsEmpty(uncovered, "Ride objects without a model sheet:\n  - " + string.Join("\n  - ", uncovered));
                var missing = RideModelSheets.FindMissingPaths(scene);
                Assert.IsEmpty(missing, "Model sheet paths the ride no longer has:\n  - " + string.Join("\n  - ", missing));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ASetPieceWithoutASheet_IsCaught()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject region = Root(scene, "Region_2");

                // A new set piece in a region, and a new region root, neither listed in RideModelSheets.Models.
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(piece, scene);
                piece.transform.SetParent(region.transform, false);
                piece.name = "TestSetPiece";

                var extra = new GameObject("Region_9");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(extra, scene);
                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.transform.SetParent(extra.transform, false);
                hill.name = "TestHill";

                var uncovered = RideModelSheets.FindUncovered(scene);
                Assert.Contains("Region_2/TestSetPiece", uncovered, "It said: " + string.Join(" | ", uncovered));
                Assert.Contains("Region_9/TestHill", uncovered, "It said: " + string.Join(" | ", uncovered));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuiltRide_IsWithinTheQuest2BudgetAtEveryViewpoint()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                List<string> lines = RideBudget.Measure(scene, out List<string> over);
                Assert.Greater(lines.Count, 8, "Every viewpoint is measured");
                Assert.IsEmpty(over, string.Join("\n", lines));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
