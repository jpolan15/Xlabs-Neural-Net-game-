using UnityEngine;
using UnityEngine.SceneManagement;

namespace Convergence.Gameplay.Hub
{
    /// <summary>
    /// Getting to and from the level hub (ADR-024). Every level can go back to the hub: H on the desk, or holding the left controller's
    /// menu button (XR turns those into <see cref="GoToHub"/>). When the hub scene is not in the build, nothing happens, so a level built
    /// on its own keeps working.
    /// </summary>
    public static class LevelHubNav
    {
        public const string HubScene = "LevelHub";

        public static bool HubAvailable => Application.CanStreamedLevelBeLoaded(HubScene);

        public static bool InHub => SceneManager.GetActiveScene().name == HubScene;

        /// <summary>Loads the hub. Returns false when already there or when the hub is not in the build.</summary>
        public static bool GoToHub()
        {
            if (InHub || !HubAvailable) return false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(HubScene);
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
