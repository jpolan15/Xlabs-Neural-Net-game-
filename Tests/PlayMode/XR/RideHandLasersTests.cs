using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Convergence.Gameplay.Ride;
using Convergence.XR.Ride;

namespace Convergence.Tests.PlayMode.XR
{
    /// <summary>
    /// The hand lasers on a real controller rig: each controller carries its own fingertip trigger 9 cm ahead on the
    /// beam's axis (HandPolarityController). On the Quest (2026-10-27) every beam stopped on it, so nothing could be drawn,
    /// shot or grabbed; the desk tests never saw it because the desk mouse has no fingertip.
    /// </summary>
    [TestFixture]
    public class RideHandLasersTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            RideAim.Set(RideHand.Left, Vector3.zero, Vector3.zero, false);
            RideAim.Set(RideHand.Right, Vector3.zero, Vector3.zero, false);
        }

        private (Transform left, Transform right) MakeRig()
        {
            _root = new GameObject("TestRig");
            _root.SetActive(false);
            Transform Hand(string name, int hand, float x)
            {
                var t = new GameObject(name).transform;
                t.SetParent(_root.transform, false);
                t.localPosition = new Vector3(x, 1.2f, 0f); // tracked: not at the origin
                var polarity = t.gameObject.AddComponent<HandPolarityController>();
                typeof(HandPolarityController).GetField("hand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(polarity, hand);
                return t;
            }

            Transform left = Hand("Left Controller", RideHand.Left, -0.2f);
            Transform right = Hand("Right Controller", RideHand.Right, 0.2f);
            var lasers = _root.AddComponent<RideHandLasers>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(RideHandLasers).GetField("leftHand", flags).SetValue(lasers, left);
            typeof(RideHandLasers).GetField("rightHand", flags).SetValue(lasers, right);
            _root.SetActive(true);
            return (left, right);
        }

        [UnityTest]
        public IEnumerator Beam_PassesItsOwnFingertip_AndReachesWhatItPointsAt()
        {
            (Transform left, Transform right) = MakeRig();
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.SetParent(_root.transform, false);
            target.transform.localPosition = new Vector3(0.2f, 1.2f, 2f);
            target.transform.localScale = new Vector3(0.6f, 0.6f, 0.05f);
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;

            Assert.IsTrue(RideAim.TryGet(RideHand.Right, out Vector3 origin, out Vector3 point), "the right laser is live");
            Assert.That(Vector3.Distance(origin, point), Is.EqualTo(2f - 0.025f).Within(0.05f),
                "the right beam stops on the target 2 m ahead, not on its own fingertip (7.8 cm)");

            // The left hand points across, straight through the right hand's fingertip: the other hand never blocks a beam either.
            Vector3 tipOfRight = right.TransformPoint(new Vector3(0f, 0f, 0.09f));
            left.rotation = Quaternion.LookRotation(tipOfRight - left.position);
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;
            Assert.IsTrue(RideAim.TryGet(RideHand.Left, out origin, out point));
            Assert.Greater(Vector3.Distance(origin, point), 1f, "the left beam passes the right hand's fingertip (0.41 m away)");
        }
    }
}
