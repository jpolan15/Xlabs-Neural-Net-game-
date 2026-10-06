using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Tests.PlayMode.Gameplay
{
    [TestFixture]
    public class AsteroidDefenseTests
    {
        GameObject _root;
        NeuralState _neural;
        ChamberController _chamber;
        GatewayController _gateway;
        AsteroidDefenseDirector _director;
        int _solves;
        int _purges;
        bool _victoryCompleteWhileClosed;
        readonly List<DefenseOutcome> _drone = new List<DefenseOutcome>();
        readonly List<DefenseOutcome> _rock = new List<DefenseOutcome>();
        readonly List<CaseDiagnostic> _victoryScans = new List<CaseDiagnostic>();
        bool _victoryStarted;

        static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        [SetUp]
        public void SetUp()
        {
            _solves = 0;
            _purges = 0;
            _victoryCompleteWhileClosed = false;
            _victoryStarted = false;
            _drone.Clear();
            _rock.Clear();
            _victoryScans.Clear();

            _root = new GameObject("AsteroidDefenseTest");
            _neural = _root.AddComponent<NeuralState>();
            _root.AddComponent<PerformanceTracker>();
            _chamber = _root.AddComponent<ChamberController>();
            _gateway = _root.AddComponent<GatewayController>();
            SetField(_chamber, "neuralState", _neural);
            SetField(_chamber, "enableSandboxMode", false);
            SetField(_gateway, "chamberController", _chamber);

            for (int i = 0; i < 4; i++)
            {
                var pod = new GameObject("Pod" + i);
                pod.transform.SetParent(_root.transform);
                var receptor = pod.AddComponent<DataTargetReceptor>();
                SetField(receptor, "caseIndex", i);
                _chamber.RegisterTargetReceptor(receptor);
            }

            _director = _root.AddComponent<AsteroidDefenseDirector>();
            SetField(_director, "chamber", _chamber);
            SetField(_director, "gateway", _gateway);
            _director.Boot();

            _chamber.OnPuzzleSolved += () => _solves++;
            _chamber.OnEmergencyPurge += () => _purges++;
            _director.OnVictoryComplete += () => _victoryCompleteWhileClosed = !_gateway.IsOpen;
            _director.OnVictoryStarted += () => _victoryStarted = true;
            _director.OnOutcome += (receptor, outcome) =>
            {
                if (receptor.CaseIndex == 0) _drone.Add(outcome);
                if (receptor.CaseIndex == 2) _rock.Add(outcome);
            };
            _director.OnScan += (receptor, diag, fired) =>
            {
                if (_victoryStarted && diag != null) _victoryScans.Add(diag);
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        void ArmAndTick(float seconds, float step)
        {
            _director.NotifyOpeningBriefing();
            float t = 0f;
            while (t < seconds)
            {
                _director.Tick(step);
                t += step;
            }
        }

        void SetCorrectNetwork()
        {
            _neural.SetWeight(0, 1.0);
            _neural.SetWeight(1, 1.0);
            _neural.SetBias(-0.5);
            _neural.SetActivation(ActivationType.Step);
            _neural.SetCableConnected(0, true);
            _neural.SetCableConnected(1, true);
        }

        [UnityTest]
        public IEnumerator BrokenNetwork_ThreatsBreach_AndHullDrops()
        {
            yield return null;
            _neural.SetWeight(0, 0.0);
            _neural.SetWeight(1, 0.0);
            _neural.SetBias(-1.0);
            _neural.SetActivation(ActivationType.Linear);
            _neural.SetCableConnected(0, false);
            _neural.SetCableConnected(1, false);

            float before = _chamber.ShieldIntegrity;
            ArmAndTick(40f, 0.25f);

            Assert.Less(_chamber.ShieldIntegrity, before);
            Assert.AreEqual(0, _solves);
            Assert.AreEqual(0, _purges);
            yield break;
        }

        [UnityTest]
        public IEnumerator CorrectNetwork_RocksVaporize_DroneDocks_SolvesOnce()
        {
            yield return null;
            SetCorrectNetwork();
            SetField(_gateway, "deferOpenToDirector", true);
            ArmAndTick(90f, 0.25f);

            Assert.AreEqual(1, _solves);
            Assert.Contains(DefenseOutcome.Vaporized, _rock);
            Assert.Contains(DefenseOutcome.Docked, _drone);
            Assert.IsTrue(_gateway.IsOpen);
            Assert.IsTrue(_victoryCompleteWhileClosed);
            yield break;
        }

        [UnityTest]
        public IEnumerator HullZero_Reroutes_AndKeepsSettings()
        {
            yield return null;
            SetField(_chamber, "enableSoftReroute", true);
            _neural.SetWeight(0, 1.0);
            _neural.SetBias(-0.5);

            _chamber.ApplyShieldDamage(150f);

            Assert.AreEqual(0, _purges);
            Assert.AreEqual(60f, _chamber.ShieldIntegrity, 0.01f);
            Assert.AreEqual(1.0, _neural.Weight1, 0.001);
            _chamber.ApplyShieldDamage(10f);
            Assert.AreEqual(50f, _chamber.ShieldIntegrity, 0.01f);
            yield break;
        }

        [UnityTest]
        public IEnumerator ThreeOfFour_NeverSolves()
        {
            yield return null;
            _neural.SetWeight(0, 1.0);
            _neural.SetWeight(1, 0.0);
            _neural.SetBias(-0.5);
            _neural.SetActivation(ActivationType.Step);
            _neural.SetCableConnected(0, true);
            _neural.SetCableConnected(1, true);

            ArmAndTick(40f, 0.25f);
            Assert.AreEqual(0, _solves);
            Assert.IsFalse(_chamber.HasSolved);
            Assert.IsFalse(_gateway.IsOpen);
            yield break;
        }

        [UnityTest]
        public IEnumerator VictoryWaves_StayOnOr_AfterXorSwap()
        {
            yield return null;
            SetCorrectNetwork();
            _chamber.OnPuzzleSolved += () => _chamber.BeginPuzzle(PuzzleDefinition.CreateXorPuzzle());
            ArmAndTick(90f, 0.25f);

            Assert.AreEqual(1, _solves);
            Assert.Greater(_victoryScans.Count, 0);
            bool sawBothInputs = false;
            for (int i = 0; i < _victoryScans.Count; i++)
            {
                CaseDiagnostic diag = _victoryScans[i];
                double[] inputs = diag.Inputs;
                bool both = inputs.Length > 1 && inputs[0] >= 0.5 && inputs[1] >= 0.5;
                bool orFire = !(inputs[0] < 0.5 && inputs[1] < 0.5);
                Assert.AreEqual(orFire, diag.ExpectedOutput >= 0.5);
                if (both)
                {
                    sawBothInputs = true;
                    Assert.GreaterOrEqual(diag.ExpectedOutput, 0.5);
                }
            }
            Assert.IsTrue(sawBothInputs);
            yield break;
        }

        [UnityTest]
        public IEnumerator Gateway_StaysShut_UntilVictory_WhenDeferred()
        {
            yield return null;
            SetCorrectNetwork();
            SetField(_gateway, "deferOpenToDirector", true);
            _director.NotifyOpeningBriefing();

            bool openedEarly = false;
            float t = 0f;
            while (t < 90f && !_gateway.IsOpen)
            {
                _director.Tick(0.25f);
                t += 0.25f;
                if (_gateway.IsOpen && !_victoryCompleteWhileClosed) openedEarly = true;
            }

            Assert.IsFalse(openedEarly);
            Assert.IsTrue(_gateway.IsOpen);
            Assert.IsTrue(_victoryCompleteWhileClosed);
            yield break;
        }
    }
}
