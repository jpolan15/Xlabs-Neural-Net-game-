using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Convergence.EditorTools;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.Tests.EditMode.Ride
{
    /// <summary>
    /// Checks the data the narrated ride runs on (the RideScript and NarrationLibrary the builder writes from
    /// RideNarrationData), the stability of the narration line ids, and the sightline validator. They read the built
    /// assets, so run Convergence > Build Neural Ride first on a fresh clone.
    /// </summary>
    public class RideNarrationTests
    {
        private const string LibraryPath = "Assets/ScriptableObjects/NarrationLibrary.asset";
        private const string ScriptPath = "Assets/ScriptableObjects/RideScript.asset";
        private const string ScenePath = "Assets/Scenes/NeuralRide.unity";

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

        [Test]
        public void EveryRideLine_HasASubtitleAndADuration()
        {
            NarrationLibrary library = Library();
            foreach (RideLine line in Enum.GetValues(typeof(RideLine)))
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
        public void EveryRideLine_HasAVoiceClipMatchingTheScript()
        {
            // The builder binds a clip only when its sidecar says the approved words, so a missing clip here means a
            // line was re-worded without re-rendering (ArtSource/voice/README.md), or a file was deleted.
            NarrationLibrary library = Library();
            foreach (RideLine line in Enum.GetValues(typeof(RideLine)))
            {
                Assert.IsTrue(library.TryGet(line, out NarrationLibrary.Entry entry), line + " has no entry in the narration library");
                Assert.IsNotNull(entry.clip, line + " has no voice clip. Render it with ArtSource/voice/render_voices.py, install, and rebuild the ride.");
                Assert.Greater(entry.clip.length, 0.5f, line + "'s clip is empty");
                Assert.AreEqual(1, entry.clip.channels, line + "'s clip should be mono");
            }
        }

        [Test]
        public void NarrationLineIds_AreNeverRenumbered()
        {
            // Voice files are named vo_ride_NN.wav after these numbers, so they are a contract. Append only.
            Assert.AreEqual(1, (int)RideLine.Launch);
            Assert.AreEqual(2, (int)RideLine.NeuronIntro);
            Assert.AreEqual(5, (int)RideLine.WeightTimesInput);
            Assert.AreEqual(8, (int)RideLine.OrBuilt);
            Assert.AreEqual(11, (int)RideLine.GradientDescent);
            Assert.AreEqual(14, (int)RideLine.TargetingRestored);
            Assert.AreEqual(15, (int)RideLine.CourseLocked);
            Assert.AreEqual(16, (int)RideLine.ArchivalMoon);
            Assert.AreEqual(24, (int)RideLine.ApolloLiftoff);
            Assert.AreEqual(29, (int)RideLine.GuideTheProblem);
            Assert.AreEqual(30, (int)RideLine.GuideWakeAura);
            Assert.AreEqual(31, (int)RideLine.GuideMakeItFire);
            Assert.IsFalse(Enum.IsDefined(typeof(RideLine), 23), "Id 23 is reserved and intentionally unused");
        }

        [Test]
        public void RideScript_BeatsMatchTheLibrary()
        {
            NarrationLibrary library = Library();
            RideScript script = Script();
            CheckSequence(library, script.intro, "intro");
            Assert.AreEqual(3, script.briefings.Length, "One briefing per stop");
            for (int i = 0; i < script.briefings.Length; i++) CheckSequence(library, script.briefings[i], "briefing " + (i + 1));
            CheckSequence(library, script.outro, "outro");
        }

        private static void CheckSequence(NarrationLibrary library, RideScript.Sequence sequence, string name)
        {
            Assert.IsFalse(sequence.IsEmpty, name + " has no lines");
            foreach (RideScript.Beat beat in sequence.beats)
            {
                Assert.IsTrue(library.TryGet(beat.line, out NarrationLibrary.Entry entry), name + " plays " + beat.line + ", which the library lacks");
                Assert.AreEqual(entry.TotalSeconds, beat.seconds, 0.01f, name + " paces " + beat.line + " differently from the library. Rebuild the ride.");
            }
        }

        [Test]
        public void Sequences_FitTheDemoTimeBudget()
        {
            // Ignite: a line of strangers, about 8 minutes a person in all. The narration is a part of that.
            RideScript script = Script();
            Assert.That(script.intro.TotalSeconds, Is.InRange(45f, 75f), "The intro should be about a minute of talk, broken up by the rider's hands-on beats");
            float narration = script.intro.TotalSeconds + script.outro.TotalSeconds;
            foreach (RideScript.Sequence briefing in script.briefings)
            {
                Assert.LessOrEqual(briefing.TotalSeconds, 40f, "A briefing over 40 seconds loses a first-time rider");
                narration += briefing.TotalSeconds;
            }

            Assert.LessOrEqual(script.outro.TotalSeconds, 40f);
            Assert.LessOrEqual(narration, 240f, "Scripted narration should stay under four minutes");
        }

        [Test]
        public void Intro_PutsTheRidersHandsOnALever_EarlyAndOften()
        {
            // The hook: a first-timer at Ignite does something within about 15 seconds, and never listens for long
            // before the next thing to do (the last stretch ends at the dock, where the weight and GO are live).
            RideScript script = Script();
            var controlIds = new[] { RideControlIds.Action, RideControlIds.Eta, RideControlIds.Rock, RideControlIds.Ice, RideControlIds.Trigger };
            float talk = 0f;
            int handsOn = 0;
            foreach (RideScript.Beat beat in script.intro.beats)
            {
                if (beat.WaitsForRider)
                {
                    Assert.LessOrEqual(talk, handsOn == 0 ? 15f : 30f, "The rider waits " + talk.ToString("0.0") + " s before the hands-on beat on " + beat.line);
                    Assert.Contains(beat.waitFor, controlIds, beat.line + " waits on a control the dash does not have");
                    Assert.Greater(beat.waitTimeout, 0f, beat.line + " needs a timeout, or a rider who does nothing is stuck");
                    Assert.Greater(beat.waitAtLeast, beat.waitFrom, beat.line + " is done before the rider does anything");
                    handsOn++;
                    talk = 0f; // its own line is spoken while the rider acts
                    continue;
                }

                talk += beat.seconds;
            }

            Assert.GreaterOrEqual(handsOn, 2, "The intro should wake AURA and make a neuron fire, by hand");
            Assert.LessOrEqual(talk, 30f, "The talk after the last hands-on beat runs " + talk.ToString("0.0") + " s before the dock");
        }

        [Test]
        public void Chapters_CoverEveryStopAndTheOutro()
        {
            NarrationLibrary library = Library();
            Assert.AreEqual(4, library.chapters.Length, "Three stops plus the outro");
            foreach (NarrationLibrary.Chapter chapter in library.chapters)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(chapter.year));
                Assert.IsFalse(string.IsNullOrWhiteSpace(chapter.title));
                Assert.IsFalse(string.IsNullOrWhiteSpace(chapter.blurb));
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

        [Test]
        public void ABlockerInFrontOfThePanel_IsCaught()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject stream = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "Station_1") stream = root.transform.Find("Stream").gameObject;
                }

                Assert.IsNotNull(stream, "Station_1/Stream is missing from the built ride");

                // A one-metre cube on the sight line from the eye to the network diagram, in a group that is live at stop 1.
                GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(blocker, scene);
                blocker.transform.SetParent(stream.transform, false);
                blocker.transform.localPosition = new Vector3(-0.72f, 1.35f, 1.32f);
                blocker.transform.localScale = Vector3.one * 0.7f;
                blocker.name = "TestBlocker";

                var problems = RideSightlines.FindBlockers(scene);
                Assert.IsTrue(problems.Exists(line => line.Contains("network diagram panel") && line.Contains("TestBlocker")),
                    "The validator must report the diagram hidden by TestBlocker. It said: " + string.Join(" | ", problems));
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
    }
}
