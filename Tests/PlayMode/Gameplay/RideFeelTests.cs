using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.Tests.PlayMode.Gameplay
{
    /// <summary>
    /// How the ride feels in the first seconds and in the rider's hands: it starts behind a black fade and sound-off
    /// while hidden things are warmed up, then lifts and begins; and Pixel can be petted by a fingertip that rubs inside
    /// its forgiving volume, not only by a laser. These go through the same inputs a rider does (a fingertip body moving
    /// in the world, a laser press) and never set a state directly.
    /// </summary>
    [TestFixture]
    public class RideFeelTests
    {
        private readonly List<string> _errors = new List<string>();
        private RideDirector _director;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _errors.Clear();
            Application.logMessageReceived += CollectError;
        }

        private void CollectError(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.Contains("MCP-FOR-UNITY")) return;
            _errors.Add(type + ": " + message);
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived -= CollectError;
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            if (_director != null)
            {
                _director.StopAllCoroutines();
                _director.enabled = false;
            }

            if (_errors.Count > 0) Assert.Fail("Errors logged during the ride:\n" + string.Join("\n", _errors));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what, float realSeconds = 60f)
        {
            float end = Time.realtimeSinceStartup + realSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        /// <summary>A fingertip stand-in: a kinematic trigger body that says which hand it is (what XR's fingertip does).</summary>
        private sealed class FakeTip : MonoBehaviour, IHandTip
        {
            public int Hand { get; set; }
            public Vector3 Position => transform.position;
        }

        [UnityTest]
        public IEnumerator Boot_StartsBlackAndSilent_WarmsHiddenThings_ThenLiftsAndTheRideBegins()
        {
            LogAssert.ignoreFailingMessages = true;
            AudioListener.volume = 1f;
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;
            _director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            var boot = UnityEngine.Object.FindAnyObjectByType<RideBoot>();
            Assert.IsNotNull(boot, "The pod carries the boot gate");

            // At the start: the eyes are covered, the sound is off, and the ride waits at its first frame.
            Assert.IsTrue(boot.IsBooting, "the boot is still running in the first frames");
            Assert.IsTrue(_director.StartHeld, "the ride waits for the boot");
            Assert.AreEqual(0f, AudioListener.volume, "no sound while it warms up");
            Transform cover = Camera.main.transform.Find("BootCover");
            Assert.IsNotNull(cover, "a black cover sits in front of the eyes");
            Assert.AreNotEqual(RideState.Intro, _director.State, "the intro has not started");

            yield return WaitFor(() => !boot.IsBooting, "the boot to finish", 60f);
            yield return null;

            Assert.IsFalse(_director.StartHeld, "the ride is released");
            Assert.Greater(AudioListener.volume, 0f, "sound is back");
            Assert.IsNull(Camera.main.transform.Find("BootCover"), "the black cover is gone");
            Assert.AreEqual(RideState.Intro, _director.State, "the ride begins with its intro only after the fade");
            // Everything switched on for the warm-up is off again, exactly as the ride expects it: the stowed table, the stamp, the
            // end card, and only the dock's regions (the streamer's own mask) showing.
            Transform pod = GameObject.Find("Pod").transform;
            foreach (string path in new[] { "GameTable", "Stamp", "EndCard" })
            {
                Assert.IsFalse(pod.Find(path).gameObject.activeSelf, path + " is hidden again after the warm-up");
            }

            var streamer = UnityEngine.Object.FindAnyObjectByType<RegionStreamer>();
            int mask = streamer.MaskFor(-1);
            int regions = 0;
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.parent != null || !t.name.StartsWith("Region_", StringComparison.Ordinal)) continue;
                int n = int.Parse(t.name.Substring(7));
                regions++;
                Assert.AreEqual((mask & (1 << n)) != 0, t.gameObject.activeSelf, t.name + " shows only if the dock's mask says so");
            }

            Assert.AreEqual(8, regions, "all eight regions were found");

            Assert.Greater(boot.BootSeconds, 0.5f, "the fade takes real time");
        }

        [UnityTest]
        public IEnumerator Pixel_ARubbingFingertipPetsIt_AndTheCooldownKeepsItFromSpamming()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("NeuralRide");
            yield return null;
            yield return null;
            _director = UnityEngine.Object.FindAnyObjectByType<RideDirector>();
            var boot = UnityEngine.Object.FindAnyObjectByType<RideBoot>();
            if (boot != null) yield return WaitFor(() => !boot.IsBooting, "the boot to finish", 60f);
            yield return null;
            _director.SkipNarration();
            yield return WaitFor(() => _director.State == RideState.Dock, "the dock after the intro", 30f);
            var pixel = UnityEngine.Object.FindAnyObjectByType<PixelBuddyView>();
            pixel.Meet();
            yield return new WaitForSecondsRealtime(0.5f);

            Transform zone = pixel.transform.Find("Body/PetZone");
            Assert.IsNotNull(zone, "Pixel has a petting volume");
            var volume = zone.GetComponent<SphereCollider>();
            Assert.IsTrue(volume.isTrigger, "it is a trigger, so a hand that stops short still counts");
            Assert.GreaterOrEqual(volume.radius, 0.2f * 1.6f, "at least 1.6x Pixel's head (about 0.2 m in its own space)");

            var hand = new GameObject("FakeFingertip");
            var body = hand.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            hand.AddComponent<SphereCollider>().radius = 0.012f;
            hand.GetComponent<SphereCollider>().isTrigger = true;
            hand.AddComponent<FakeTip>().Hand = RideHand.Right;
            try
            {
                // The hand sits to one side of Pixel's head and strokes up and down across it: sideways travel, not a push toward
                // it. Pixel floats and bobs, so every position is taken from where its head is now.
                Vector3 across = Vector3.up;
                Vector3 At(float along) => zone.position + zone.right * 0.12f + across * (along - 0.1f);

                IEnumerator Settle(float along)
                {
                    hand.transform.position = At(along);
                    yield return new WaitForFixedUpdate();
                    yield return new WaitForFixedUpdate();
                }

                yield return Settle(0f);
                int before = pixel.PetReactions;

                // A small touch is not a rub: it stays inside 8 cm of travel.
                yield return Settle(0.03f);
                Assert.AreEqual(before, pixel.PetReactions, "a tap inside the volume is not a rub");

                // A rub: sideways travel past 8 cm.
                for (int i = 0; i < 8; i++)
                {
                    hand.transform.position = At(0.03f + 0.02f * (i + 1));
                    yield return new WaitForFixedUpdate();
                }

                yield return null;
                Assert.AreEqual(before + 1, pixel.PetReactions, "rubbing sideways pets Pixel once");

                // Rubbing on straight away does not start another reaction until the cooldown has passed.
                for (int i = 0; i < 8; i++)
                {
                    hand.transform.position = At(0.19f - 0.02f * (i + 1));
                    yield return new WaitForFixedUpdate();
                }

                Assert.AreEqual(before + 1, pixel.PetReactions, "the cooldown keeps a long rub from stacking reactions");

                yield return new WaitForSeconds(1.4f); // game time, the clock the cooldown runs on, so a slow frame rate cannot shorten it
                yield return Settle(0.03f);
                for (int i = 0; i < 8; i++)
                {
                    hand.transform.position = At(0.03f + 0.02f * (i + 1));
                    yield return new WaitForFixedUpdate();
                }

                yield return null;
                Assert.AreEqual(before + 2, pixel.PetReactions, "after the cooldown a rub is a new pet");

                // The laser path (point and press) pets too, once its own cooldown has passed.
                yield return new WaitForSeconds(1.4f); // game time, the clock the cooldown runs on, so a slow frame rate cannot shorten it
                pixel.Pet();
                Assert.AreEqual(before + 3, pixel.PetReactions, "a laser press pets Pixel");
                Assert.AreEqual(PixelBuddyView.Mood.Happy, pixel.CurrentMood, "a petted Pixel is happy");
            }
            finally
            {
                UnityEngine.Object.Destroy(hand);
            }
        }
    }
}
