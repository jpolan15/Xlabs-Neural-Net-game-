using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Tests.PlayMode.Gameplay
{
    [TestFixture]
    public class Level01IntegrationTests
    {
        private GameObject _testRoot;
        private NeuralState _neuralState;
        private ChamberController _chamberController;
        private GatewayController _gatewayController;
        private PerformanceTracker _performanceTracker;
        private LevelResetter _levelResetter;

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestRoot_Level01");
            _neuralState = _testRoot.AddComponent<NeuralState>();
            _performanceTracker = _testRoot.AddComponent<PerformanceTracker>();
            _chamberController = _testRoot.AddComponent<ChamberController>();
            _gatewayController = _testRoot.AddComponent<GatewayController>();
            _levelResetter = _testRoot.AddComponent<LevelResetter>();

            // Author Preset A: w1=0, w2=0, b=-1, Linear, Cable 1 disconnected
            var presetA = ScriptableObject.CreateInstance<BrokenConfigurationSO>();
            SetField(presetA, "configId", "Preset_A");
            SetField(presetA, "initialW1", 0.0);
            SetField(presetA, "initialW2", 0.0);
            SetField(presetA, "initialBias", -1.0);
            SetField(presetA, "initialActivation", ActivationType.Linear);
            SetField(presetA, "cable1Disconnected", true);
            SetField(presetA, "cable2Disconnected", false);

            // Wire references
            SetField(_levelResetter, "chamberController", _chamberController);
            SetField(_levelResetter, "neuralState", _neuralState);
            SetField(_levelResetter, "gatewayController", _gatewayController);
            SetField(_levelResetter, "performanceTracker", _performanceTracker);
            SetField(_levelResetter, "activePreset", presetA);
            SetField(_levelResetter, "availablePresets", new BrokenConfigurationSO[] { presetA });

            SetField(_chamberController, "neuralState", _neuralState);
            SetField(_chamberController, "performanceTracker", _performanceTracker);
            SetField(_chamberController, "initialPreset", presetA);

            SetField(_gatewayController, "chamberController", _chamberController);

            // Initialize to Preset A
            presetA.ApplyTo(_neuralState);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_testRoot);
            }
        }

        /// <summary>
        /// Verified acceptance test for the complete Level 1 sequence:
        /// Arrival -> Step crystal inserted -> w1=1, w2=1, b=-0.5 -> 4 cases evaluated -> accuracy 1.0 ->
        /// PuzzleSolved emitted once -> Awakening phase entered -> gateway opens ->
        /// reset invoked -> gateway closes -> model and phase return to initial state.
        /// </summary>
        [Test]
        public void Level01_FullPlayableLoop_VerifiesCanonicalSequence()
        {
            // 1. Initial Arrival State
            Assert.AreEqual(ChamberPhase.Arrival, _chamberController.Phase);
            Assert.IsFalse(_gatewayController.IsOpen, "Gateway must be sealed at Arrival.");
            Assert.IsFalse(_chamberController.HasSolved);

            // Verify initial broken preset state (Preset A)
            Assert.AreEqual(0.0, _neuralState.Weight1);
            Assert.AreEqual(0.0, _neuralState.Weight2);
            Assert.AreEqual(-1.0, _neuralState.Bias);
            Assert.AreEqual(ActivationType.Linear, _neuralState.Activation);
            Assert.IsFalse(_neuralState.Cable1Connected, "Cable 1 must be disconnected initially.");

            // 2. Player repairs conduits: Reconnects Cable 1
            _neuralState.SetCableConnected(0, true);
            Assert.IsTrue(_neuralState.Cable1Connected);

            // 3. Step crystal inserted into activation socket
            _neuralState.SetActivation(ActivationType.Step);
            Assert.AreEqual(ActivationType.Step, _neuralState.Activation);

            // 4. Calibrate weights and bias: w1 = 1.0, w2 = 1.0, b = -0.5
            _neuralState.SetWeight(0, 1.0);
            _neuralState.SetWeight(1, 1.0);
            _neuralState.SetBias(-0.5);

            Assert.AreEqual(1.0, _neuralState.Weight1);
            Assert.AreEqual(1.0, _neuralState.Weight2);
            Assert.AreEqual(-0.5, _neuralState.Bias);

            // Track events emitted
            int puzzleSolvedEventCount = 0;
            _chamberController.OnPuzzleSolved += () => puzzleSolvedEventCount++;

            // 5. Fire Neural Pulse Tool (triggers evaluation)
            PuzzleEvaluation eval = _chamberController.TriggerForwardPass();

            // 6. Assert all 4 truth table cases evaluated with 100% accuracy
            Assert.IsNotNull(eval);
            Assert.AreEqual(4, eval.TotalCases);
            Assert.AreEqual(4, eval.PassedCases);
            Assert.AreEqual(1.0, eval.Accuracy, 1e-5);
            Assert.IsTrue(eval.Passed);

            // Assert exact margins:
            // (0, 0): z = -0.5 -> 0
            Assert.AreEqual(-0.5, eval.Diagnostics[0].CalculatedZ, 1e-5);
            Assert.AreEqual(0.0, eval.Diagnostics[0].ActualOutput, 1e-5);
            Assert.IsTrue(eval.Diagnostics[0].IsCorrect);

            // (0, 1): z =  0.5 -> 1
            Assert.AreEqual(0.5, eval.Diagnostics[1].CalculatedZ, 1e-5);
            Assert.AreEqual(1.0, eval.Diagnostics[1].ActualOutput, 1e-5);
            Assert.IsTrue(eval.Diagnostics[1].IsCorrect);

            // (1, 0): z =  0.5 -> 1
            Assert.AreEqual(0.5, eval.Diagnostics[2].CalculatedZ, 1e-5);
            Assert.AreEqual(1.0, eval.Diagnostics[2].ActualOutput, 1e-5);
            Assert.IsTrue(eval.Diagnostics[2].IsCorrect);

            // (1, 1): z =  1.5 -> 1
            Assert.AreEqual(1.5, eval.Diagnostics[3].CalculatedZ, 1e-5);
            Assert.AreEqual(1.0, eval.Diagnostics[3].ActualOutput, 1e-5);
            Assert.IsTrue(eval.Diagnostics[3].IsCorrect);

            // 7. Assert PuzzleSolved emitted ONCE
            Assert.AreEqual(1, puzzleSolvedEventCount, "PuzzleSolved must be emitted exactly once.");

            // 8. Assert Awakening phase entered and gateway opens
            Assert.AreEqual(ChamberPhase.Awakening, _chamberController.Phase);
            Assert.IsTrue(_gatewayController.IsOpen, "Gateway must open upon 100% accuracy evaluation.");

            // Fire a second pulse to confirm idempotency
            _chamberController.TriggerForwardPass();
            Assert.AreEqual(1, puzzleSolvedEventCount, "Subsequent evaluations must not re-trigger PuzzleSolved.");
            Assert.IsTrue(_gatewayController.IsOpen);

            // 9. Reset invoked via LevelResetter
            _levelResetter.ResetToActivePreset();

            // 10. Assert gateway closes, model and phase return to initial state
            Assert.IsFalse(_gatewayController.IsOpen, "Gateway must close upon reset.");
            Assert.AreEqual(ChamberPhase.Arrival, _chamberController.Phase, "Phase must return to Arrival.");
            Assert.IsFalse(_chamberController.HasSolved);

            // Verify model returned to Preset A initial state
            Assert.AreEqual(0.0, _neuralState.Weight1);
            Assert.AreEqual(0.0, _neuralState.Weight2);
            Assert.AreEqual(-1.0, _neuralState.Bias);
            Assert.AreEqual(ActivationType.Linear, _neuralState.Activation);
            Assert.IsFalse(_neuralState.Cable1Connected);
        }

        /// <summary>
        /// Critical Negative Gate Protection:
        /// Asserts that a configuration with 3/4 correct cases produces Accuracy = 0.75
        /// and does NOT open the gateway, does NOT enter Awakening, and does NOT emit PuzzleSolved.
        /// </summary>
        [Test]
        public void Level01_ThreeOfFourCorrect_DoesNotOpenGateway()
        {
            // Reconnect cables and insert Step crystal
            _neuralState.SetCableConnected(0, true);
            _neuralState.SetCableConnected(1, true);
            _neuralState.SetActivation(ActivationType.Step);

            // Set w1=0, w2=0, b=0:
            // (0,0)->1 (wrong), (0,1)->1 (correct), (1,0)->1 (correct), (1,1)->1 (correct) -> 3/4 correct
            _neuralState.SetWeight(0, 0.0);
            _neuralState.SetWeight(1, 0.0);
            _neuralState.SetBias(0.0);

            bool puzzleSolvedEmitted = false;
            _chamberController.OnPuzzleSolved += () => puzzleSolvedEmitted = true;

            PuzzleEvaluation eval = _chamberController.TriggerForwardPass();

            Assert.AreEqual(0.75, eval.Accuracy, 1e-5);
            Assert.IsFalse(eval.Passed, "3/4 solution must NOT pass.");
            Assert.IsFalse(puzzleSolvedEmitted, "PuzzleSolved must NOT be emitted for 3/4 correct.");
            Assert.IsFalse(_gatewayController.IsOpen, "Gateway must remain sealed.");
            Assert.AreNotEqual(ChamberPhase.Awakening, _chamberController.Phase);
        }

        /// <summary>
        /// Asserts that a disconnected conduit effectively sets that input to zero and fails the puzzle.
        /// </summary>
        [Test]
        public void Level01_DisconnectedConduit_PreventsCompletion()
        {
            // Set valid weights and Step activation, but leave Cable 1 disconnected
            _neuralState.SetActivation(ActivationType.Step);
            _neuralState.SetWeight(0, 1.0);
            _neuralState.SetWeight(1, 1.0);
            _neuralState.SetBias(-0.5);
            _neuralState.SetCableConnected(0, false); // DISCONNECTED

            PuzzleEvaluation eval = _chamberController.TriggerForwardPass();

            // When Cable 1 is disconnected, X1 is always 0.
            // Case (1, 0) sees inputs (0, 0) -> z = -0.5 -> 0, but target was 1.0!
            Assert.IsFalse(eval.Passed, "Disconnected conduit must prevent puzzle completion.");
            Assert.IsFalse(_gatewayController.IsOpen);
        }
    }
}
