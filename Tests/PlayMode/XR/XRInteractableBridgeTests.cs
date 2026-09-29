using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Gameplay;
using Convergence.XR;

namespace Convergence.Tests.PlayMode.XR
{
    [TestFixture]
    public class XRInteractableBridgeTests
    {
        private GameObject _root;
        private XRInteractionManager _manager;
        private NeuralState _neuralState;
        private XRDirectInteractor _interactor;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot_XR");
            _manager = _root.AddComponent<XRInteractionManager>();
            _neuralState = _root.AddComponent<NeuralState>();

            var interactorGo = new GameObject("TestInteractor");
            interactorGo.transform.SetParent(_root.transform, false);
            interactorGo.AddComponent<SphereCollider>().isTrigger = true;
            interactorGo.AddComponent<Rigidbody>().isKinematic = true;
            _interactor = interactorGo.AddComponent<XRDirectInteractor>();
            _interactor.interactionManager = _manager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private XRInteractableBridge CreateBridge<T>(out T gameplayComponent) where T : Component
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(_root.transform, false);
            gameplayComponent = go.AddComponent<T>();
            var bridge = go.AddComponent<XRInteractableBridge>();
            bridge.interactionManager = _manager;
            return bridge;
        }

        [UnityTest]
        public IEnumerator SelectOnCable_TogglesConnectionThroughGameplayEvent()
        {
            var bridge = CreateBridge(out CableInteractable cable);
            yield return null;

            bool eventFired = false;
            bool connectedInEvent = false;
            cable.OnConnectionStateChanged += (index, connected) => { eventFired = true; connectedInEvent = connected; };
            bool before = cable.IsConnected;

            _manager.SelectEnter((IXRSelectInteractor)_interactor, (IXRSelectInteractable)bridge);
            yield return null;

            Assert.IsTrue(eventFired, "Selecting a cable must raise its gameplay event.");
            Assert.AreNotEqual(before, cable.IsConnected);
            Assert.AreEqual(cable.IsConnected, connectedInEvent);
        }

        [UnityTest]
        public IEnumerator QuickTapOnWeightSlider_AdvancesExactlyOneDetent()
        {
            var bridge = CreateBridge(out KineticWeightSliderInteractor slider);
            yield return null;

            double before = slider.CurrentWeight;
            int eventCount = 0;
            slider.OnSliderAdjusted += (index, value) => eventCount++;

            _manager.SelectEnter((IXRSelectInteractor)_interactor, (IXRSelectInteractable)bridge);
            yield return null;
            _manager.SelectExit((IXRSelectInteractor)_interactor, (IXRSelectInteractable)bridge);
            yield return null;

            Assert.AreEqual(1, eventCount, "A tap must produce exactly one slider adjustment.");
            Assert.AreEqual(before + 0.5, slider.CurrentWeight, 1e-6);
        }
    }
}
