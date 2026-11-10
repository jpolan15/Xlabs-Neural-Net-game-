using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Convergence.Gameplay.Hub;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Hub;

namespace Convergence.Tests.PlayMode.Gameplay
{
    /// <summary>The level hub in Play Mode (ADR-024): it builds a card per level, a locked card stays put, a shot card loads its level, and
    /// a level goes back to the hub.</summary>
    [TestFixture]
    public class LevelHubPlayTests
    {
        private readonly List<string> _errors = new List<string>();

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
            Application.logMessageReceived -= CollectError;
            Time.timeScale = 1f;
            if (_errors.Count > 0) Assert.Fail("Errors logged in the hub:\n" + string.Join("\n", _errors));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what, float realSeconds)
        {
            float end = Time.realtimeSinceStartup + realSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheHub_ShowsACardPerLevel_AndAShotCardLoadsItsLevel_AndHGoesBack()
        {
            SceneManager.LoadScene(LevelHubNav.HubScene);
            yield return null;
            yield return null;
            var view = UnityEngine.Object.FindAnyObjectByType<LevelHubView>();
            Assert.IsNotNull(view, "LevelHubView in the hub scene");
            Assert.GreaterOrEqual(view.CardCount, 2, "a card per level");
            yield return new WaitForSeconds(0.5f);

            // The last card is the locked "coming soon" one: a shot shakes it and loads nothing.
            LevelCard locked = view.CardAt(view.CardCount - 1);
            Assert.IsTrue(locked.Hit(RideHand.Right), "a locked card still answers the shot");
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(view.IsLoading, "a locked card loads nothing");
            Assert.AreEqual(LevelHubNav.HubScene, SceneManager.GetActiveScene().name);

            // Card 2 is Level 2.
            Assert.IsTrue(view.CardAt(1).Hit(RideHand.Right), "Level 2's card takes the shot");
            Assert.IsTrue(view.IsLoading);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "WordMachine", "Level 2 to load from the hub", 30f);
            yield return new WaitForSeconds(1f);
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<RideDirector>(), "Level 2's ride is running");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<LevelProgressRecorder>(), "the scoreboard recorder rides along");

            Assert.IsTrue(LevelHubNav.GoToHub(), "a level can go back to the hub");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == LevelHubNav.HubScene, "the hub to load again", 30f);
            yield return null;
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<LevelHubView>());
            Assert.IsFalse(LevelHubNav.GoToHub(), "already in the hub");
        }
    }
}
