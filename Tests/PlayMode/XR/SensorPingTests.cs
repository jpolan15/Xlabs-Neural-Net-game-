using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Core.Neural;
using Convergence.Gameplay;
using Convergence.XR;

namespace Convergence.Tests.PlayMode.XR
{
    [TestFixture]
    public class SensorPingTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator SelectOnVoyageButton_InvokesTheVoyageAction()
        {
            _root = new GameObject("VoyageButtonTest");
            var manager = _root.AddComponent<XRInteractionManager>();
            var neural = _root.AddComponent<NeuralState>();
            var chamber = _root.AddComponent<ChamberController>();
            var voyage = _root.AddComponent<VoyageDirector>();

            var buttonGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonGo.transform.SetParent(_root.transform, false);
            var button = buttonGo.AddComponent<VoyageButton>();
            var bridge = buttonGo.AddComponent<XRInteractableBridge>();
            bridge.interactionManager = manager;

            var interactorGo = new GameObject("Interactor");
            interactorGo.transform.SetParent(_root.transform, false);
            interactorGo.AddComponent<SphereCollider>().isTrigger = true;
            interactorGo.AddComponent<Rigidbody>().isKinematic = true;
            var interactor = interactorGo.AddComponent<XRDirectInteractor>();
            interactor.interactionManager = manager;

            // Move the chamber onto the XOR leg by solving OR first, then install a hidden layer.
            neural.SetActivation(ActivationType.Step);
            neural.SetCableConnected(0, true);
            neural.SetCableConnected(1, true);
            neural.SetWeight(0, 1.0);
            neural.SetWeight(1, 1.0);
            neural.SetBias(-0.5);
            chamber.TriggerForwardPass();
            yield return null;

            manager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)bridge);
            yield return null;

            Assert.IsNull(neural.Network.SingleNeuron, "Installing the hidden layer must leave the single-neuron network.");
            Assert.AreEqual(2, neural.Network.Layers.Count);
            Assert.IsNotNull(button);
            Assert.IsNotNull(voyage);
        }
    }
}
