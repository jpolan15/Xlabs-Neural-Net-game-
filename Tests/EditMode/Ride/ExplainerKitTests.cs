using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Tests.EditMode.Ride
{
    /// <summary>
    /// The explainer kit of Level 1 v4 (TASK_LEVEL1_V4_EXPLAINERS section 4): the rate functions against manim's
    /// definitions, tweens and their timeline, the shape batch, and the sequencer: steps run on their narration cues
    /// in order, a missed cue's step is completed at once, JumpToEnd lands in the final state with the region back,
    /// and a reset puts everything back.
    /// </summary>
    public class ExplainerKitTests
    {
        // ---------- rate functions ----------

        [Test]
        public void Smooth_RunsFromZeroToOne_SymmetricAndMonotonic()
        {
            Assert.AreEqual(0f, Rate.Smooth(0f), 1e-6f);
            Assert.AreEqual(1f, Rate.Smooth(1f), 1e-6f);
            Assert.AreEqual(0.5f, Rate.Smooth(0.5f), 1e-6f);
            float last = 0f;
            for (int i = 1; i <= 100; i++)
            {
                float t = i / 100f;
                float v = Rate.Smooth(t);
                Assert.GreaterOrEqual(v, last, "smooth never goes back at t = " + t);
                Assert.AreEqual(1f - v, Rate.Smooth(1f - t), 1e-5f, "smooth is symmetric at t = " + t);
                last = v;
            }

            // manim's smooth (inflection 10) is slow at the ends: at a tenth of the time it has done well under a tenth.
            Assert.Less(Rate.Smooth(0.1f), 0.03f);
            Assert.AreEqual(0f, Rate.Smooth(-1f), 1e-6f, "clamped below");
            Assert.AreEqual(1f, Rate.Smooth(2f), 1e-6f, "clamped above");
        }

        [Test]
        public void ThereAndBack_PeaksInTheMiddle_AndReturns()
        {
            Assert.AreEqual(0f, Rate.ThereAndBack(0f), 1e-6f);
            Assert.AreEqual(1f, Rate.ThereAndBack(0.5f), 1e-6f);
            Assert.AreEqual(0f, Rate.ThereAndBack(1f), 1e-6f);
            Assert.AreEqual(Rate.ThereAndBack(0.2f), Rate.ThereAndBack(0.8f), 1e-6f);
        }

        [Test]
        public void RushIntoAndRushFrom_AreTheHalvesOfSmooth()
        {
            Assert.AreEqual(0f, Rate.RushInto(0f), 1e-6f);
            Assert.AreEqual(1f, Rate.RushInto(1f), 1e-5f);
            Assert.Less(Rate.RushInto(0.5f), 0.5f, "rush_into starts slowly");
            Assert.AreEqual(0f, Rate.RushFrom(0f), 1e-5f);
            Assert.AreEqual(1f, Rate.RushFrom(1f), 1e-5f);
            Assert.Greater(Rate.RushFrom(0.5f), 0.5f, "rush_from leaves fast");
            Assert.AreEqual(1f - Rate.RushInto(0.3f), Rate.RushFrom(0.7f), 1e-5f, "they mirror each other");
        }

        [Test]
        public void Lingering_ArrivesAtEightyPercentAndStays()
        {
            Assert.AreEqual(0.5f, Rate.Lingering(0.4f), 1e-6f);
            Assert.AreEqual(1f, Rate.Lingering(0.8f), 1e-6f);
            Assert.AreEqual(1f, Rate.Lingering(0.95f), 1e-6f);
        }

        [Test]
        public void Lagged_StaggersItems_AndTheLastEndsExactlyAtOne()
        {
            const int n = 5;
            const float lag = 0.5f;
            Assert.AreEqual(1f, Rate.Lagged(1f, n - 1, n, lag), 1e-6f);
            Assert.AreEqual(0f, Rate.Lagged(0.1f, n - 1, n, lag), 1e-6f, "the last item has not started yet");
            Assert.Greater(Rate.Lagged(0.1f, 0, n, lag), 0f, "the first item has");
            for (int i = 1; i < n; i++) Assert.LessOrEqual(Rate.Lagged(0.4f, i, n, lag), Rate.Lagged(0.4f, i - 1, n, lag));
            Assert.AreEqual(0.3f, Rate.Lagged(0.3f, 0, 1, lag), 1e-6f, "one item is just linear time");
        }

        // ---------- tweens ----------

        [Test]
        public void Tween_WaitsForItsDelay_EasesAndEndsExactly()
        {
            float value = -1f;
            int begun = 0, ended = 0;
            var t = new Tween(2f, v => value = v, Rate.Linear, () => begun++, () => ended++).After(1f);
            var timeline = new Timeline();
            timeline.Play(t);
            Assert.AreEqual(-1f, value, "nothing before the delay");
            timeline.Update(1.5f);
            Assert.AreEqual(1, begun);
            Assert.AreEqual(0.25f, value, 1e-5f, "a quarter of the way at 0.5 s of 2 s");
            timeline.Update(5f);
            Assert.AreEqual(1f, value, 1e-6f);
            Assert.AreEqual(1, ended);
            Assert.IsTrue(timeline.IsIdle);
        }

        [Test]
        public void Timeline_CompleteAll_JumpsEveryTweenToItsEnd()
        {
            float a = 0f, b = 0f, flash = 0.5f;
            var timeline = new Timeline();
            timeline.Play(new Tween(1f, v => a = v));
            timeline.Play(new Tween(1f, v => b = 3f * v).After(10f));
            timeline.Play(new Tween(1f, v => flash = v, Rate.ThereAndBack));
            timeline.Update(0.3f);
            timeline.CompleteAll();
            Assert.AreEqual(1f, a, 1e-6f);
            Assert.AreEqual(3f, b, 1e-6f, "even a tween still waiting for its delay ends");
            Assert.AreEqual(0f, flash, 1e-6f, "an Indicate ends where it started");
            Assert.IsTrue(timeline.IsIdle);
        }

        // ---------- shapes ----------

        [Test]
        public void ShapeBatch_DrawsEveryVisibleShapeAsOneQuad_InOneMesh()
        {
            var go = new GameObject("TestShapes", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var batch = go.AddComponent<ShapeBatch>();
                batch.SetShapes(new[]
                {
                    new Shape { position = Vector3.zero, size = Vector2.one },
                    new Shape { position = Vector3.right, size = Vector2.one, round = 1f },
                    new Shape { kind = Shape.Kind.Line, position = Vector3.zero, end = Vector3.up, size = new Vector2(0.01f, 0.01f) },
                    new Shape { position = Vector3.left, size = Vector2.one, alpha = 0f },
                });
                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(12, mesh.vertexCount, "three visible shapes, four corners each; the invisible one is skipped");
                batch[3].alpha = 1f;
                batch[2].grow = 0f;
                batch.Rebuild();
                Assert.AreEqual(12, mesh.vertexCount, "a line not yet grown draws nothing; the shown square does");
                batch.ResetToHome();
                batch.Rebuild();
                Assert.AreEqual(12, mesh.vertexCount);
                Assert.AreEqual(0f, batch[3].alpha, "reset goes back to the builder's layout");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---------- the sequencer ----------

        private sealed class ProbeExplainer : Explainer
        {
            public readonly List<string> Ran = new List<string>();
            public float First = -1f, Second = -1f;

            public void Configure(GameObject region, Transform piece, Transform target, GameObject shown)
            {
                lines = new[] { RideLine.PixelsSquares, RideLine.PixelsNumbers };
                explainSegment = 0;
                lastSegment = 1;
                blackboard = false;
                content = shown;
                hidden = new[] { region };
                unfolds = new[]
                {
                    new Unfold { piece = piece, target = target, style = UnfoldStyle.Morph, duration = 1f },
                };
            }

            protected override void Declare(Steps s)
            {
                s.At("grid", () =>
                {
                    Ran.Add("grid");
                    return new[] { new Tween(1f, v => First = v, Rate.Linear) };
                });
                s.AtClause(RideLine.PixelsSquares, 2, () =>
                {
                    Ran.Add("clause");
                    return null;
                });
                s.At("numbers", () =>
                {
                    Ran.Add("numbers");
                    return new[] { new Tween(2f, v => Second = v, Rate.Linear) };
                });
                s.Unfold("zero");
            }
        }

        private static NarrationLibrary.Segment Clause(params string[] cues) => new NarrationLibrary.Segment { text = "x", weight = 1f, cues = cues };

        private GameObject _root;
        private ProbeExplainer _probe;
        private GameObject _region, _content;
        private Transform _piece, _target;

        [SetUp]
        public void MakeProbe()
        {
            _root = new GameObject("ProbeRoot");
            _region = new GameObject("Region");
            _region.transform.SetParent(_root.transform);
            _target = new GameObject("Target").transform;
            _target.SetParent(_region.transform);
            _target.localPosition = new Vector3(0f, 0f, 10f);
            _target.localScale = Vector3.one * 4f;
            _content = new GameObject("Content");
            _content.transform.SetParent(_root.transform);
            _piece = new GameObject("Piece").transform;
            _piece.SetParent(_content.transform);
            _probe = _root.AddComponent<ProbeExplainer>();
            _probe.Configure(_region, _piece, _target, _content);
            _probe.ResetToStart();
        }

        [TearDown]
        public void DropProbe() => Object.DestroyImmediate(_root);

        [Test]
        public void AtStart_TheRegionIsDark_AndNothingRan()
        {
            Assert.IsFalse(_region.activeSelf, "the region's pieces stay off until the unfold");
            Assert.IsFalse(_content.activeSelf, "the explainer is not on screen before its segment");
            Assert.AreEqual(0, _probe.StepsRun);
            Assert.AreEqual(4, _probe.StepCount);
        }

        [Test]
        public void Cues_RunTheirSteps_InOrder_OnTheNarrationClock()
        {
            _probe.HearClause(RideLine.PixelsSquares, 0, Clause("grid"));
            Assert.IsTrue(_probe.IsShowing, "hearing its line puts the explainer on screen");
            Assert.IsTrue(_content.activeSelf);
            CollectionAssert.AreEqual(new[] { "grid" }, _probe.Ran);
            _probe.Tick(0.5f);
            Assert.AreEqual(0.5f, _probe.First, 1e-4f, "the step's tween plays on the game clock");

            _probe.HearClause(RideLine.PixelsSquares, 1, Clause());
            Assert.AreEqual(1, _probe.StepsRun, "a clause with no cue of a step runs nothing");
            _probe.HearClause(RideLine.WrongSays, 0, Clause("numbers"));
            Assert.AreEqual(1, _probe.StepsRun, "a cue from another beat's line is not this explainer's");

            _probe.HearClause(RideLine.PixelsSquares, 2, Clause());
            CollectionAssert.AreEqual(new[] { "grid", "clause" }, _probe.Ran, "a step keyed to a clause runs on that clause");
            Assert.AreEqual(0.5f, _probe.First, 1e-4f, "a heard step does not cut the one still playing");
            _probe.Tick(0.6f);
            Assert.AreEqual(1f, _probe.First, 1e-6f);
        }

        [Test]
        public void ALaterCue_CompletesTheStepsWhoseCuesWereMissed()
        {
            _probe.HearClause(RideLine.PixelsNumbers, 0, Clause("numbers"));
            CollectionAssert.AreEqual(new[] { "grid", "clause", "numbers" }, _probe.Ran);
            Assert.AreEqual(1f, _probe.First, 1e-6f, "the missed step ran and completed");
            Assert.AreEqual(0f, _probe.Second, 1e-6f, "the heard step plays from its start");
            Assert.IsFalse(_region.activeSelf, "the unfold has not come yet");
        }

        [Test]
        public void TheUnfold_MorphsThePieceIntoTheRegion_ThenTheRegionIsBack()
        {
            _probe.HearClause(RideLine.PixelsNumbers, 1, Clause("zero"));
            Assert.IsTrue(_region.activeSelf, "the unfold turns the region's pieces on");
            Assert.IsFalse(_target.gameObject.activeSelf, "the morph target waits for its piece");
            _probe.Tick(0.5f);
            Assert.Greater(_piece.position.z, 0.5f, "the piece flies toward the target");
            Assert.Less(_piece.position.z, 9.5f);
            _probe.Tick(1f);
            Assert.IsTrue(_target.gameObject.activeSelf, "the target takes the piece's place");
            Assert.IsFalse(_piece.gameObject.activeSelf);
            Assert.AreEqual(4f, _piece.localScale.x, 1e-4f, "the piece ended at the target's scale");
            _probe.Tick(0.1f);
            Assert.IsTrue(_probe.HasUnfolded);
            Assert.IsTrue(_probe.IsFinished, "every step ran and every tween finished");
            Assert.IsFalse(_content.activeSelf, "the explainer leaves; the place stays");
        }

        [Test]
        public void JumpToEnd_LandsInTheFinalState_AndResetPutsItBack()
        {
            _probe.HearClause(RideLine.PixelsSquares, 0, Clause("grid"));
            _probe.Tick(0.2f);
            _probe.JumpToEnd();
            Assert.IsTrue(_probe.IsFinished);
            Assert.AreEqual(4, _probe.StepsRun);
            Assert.AreEqual(1f, _probe.First, 1e-6f);
            Assert.AreEqual(1f, _probe.Second, 1e-6f);
            Assert.IsTrue(_region.activeSelf && _target.gameObject.activeSelf, "the region is whole");
            Assert.AreEqual(new Vector3(0f, 0f, 10f), _target.localPosition, "at its real place");
            Assert.AreEqual(Vector3.one * 4f, _target.localScale);
            Assert.IsFalse(_piece.gameObject.activeSelf);

            _probe.HearClause(RideLine.PixelsSquares, 0, Clause("grid"));
            Assert.AreEqual(4, _probe.StepsRun, "a finished explainer ignores late cues");

            _probe.ResetToStart();
            Assert.AreEqual(0, _probe.StepsRun);
            Assert.IsFalse(_probe.IsFinished);
            Assert.IsFalse(_region.activeSelf, "dark again");
            Assert.AreEqual(Vector3.zero, _piece.localPosition, "the piece is home");
            Assert.AreEqual(Vector3.one * 4f, _target.localScale);
        }
    }
}
