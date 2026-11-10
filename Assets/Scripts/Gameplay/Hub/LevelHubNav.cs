using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Convergence.Gameplay.Hub
{
    /// <summary>
    /// Getting to and from the level hub (ADR-024). Every level can go back to the hub: H on the desk, or holding the left controller's
    /// menu button (XR turns those into <see cref="GoToHub"/>). When the hub scene is not in the build, nothing happens, so a level built
    /// on its own keeps working. Leaving a scene goes through <see cref="FadeOut"/> when the scene on show offers one, so the view goes
    /// to black before the switch instead of cutting.
    /// </summary>
    public static class LevelHubNav
    {
        public const string HubScene = "LevelHub";

        /// <summary>
        /// Set by the presentation of the scene on show: fades the view (and the sound) out, then calls the given action. Returns false
        /// when it cannot fade, and the scene then switches at once.
        /// </summary>
        public static Func<Action, bool> FadeOut;

        private static float _leavingUntil = float.NegativeInfinity;

        public static bool HubAvailable => Application.CanStreamedLevelBeLoaded(HubScene);

        public static bool InHub => SceneManager.GetActiveScene().name == HubScene;

        /// <summary>True from a <see cref="GoTo"/> until the next scene is in (a few seconds at most).</summary>
        public static bool Leaving => Time.realtimeSinceStartup < _leavingUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            FadeOut = null;
            _leavingUntil = float.NegativeInfinity;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _leavingUntil = float.NegativeInfinity;

        /// <summary>Goes to the hub. Returns false when already there or when the hub is not in the build.</summary>
        public static bool GoToHub() => !InHub && GoTo(HubScene);

        /// <summary>
        /// Leaves for a scene in the build, faded when the scene on show can fade. Returns false when the scene is not in the build. A
        /// second call while already leaving does nothing (and returns true).
        /// </summary>
        public static bool GoTo(string scene)
        {
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene)) return false;
            if (Leaving) return true;
            _leavingUntil = Time.realtimeSinceStartup + 5f;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            void Load() => SceneManager.LoadScene(scene);
            if (FadeOut == null || !FadeOut(Load)) Load();
            return true;
        }

        /// <summary>Starts loading a level the hub offered. Returns null when the scene is not in the build.</summary>
        public static AsyncOperation LoadLevel(string scene)
        {
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene)) return null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            return SceneManager.LoadSceneAsync(scene);
        }
    }
}
