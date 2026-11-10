using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Convergence.Gameplay.Ride;

namespace Convergence.Gameplay.Hub
{
    /// <summary>
    /// Writes a finished ride to the hub's scoreboard (<see cref="LevelProgress"/>): the stars of every game and the ride's score, under
    /// the level's scene name. It needs no wiring: <see cref="LevelProgressBoot"/> puts one on every scene's <see cref="RideDirector"/>
    /// when the scene loads, so a level is scored the moment it is in the build, with no rebuild of its scene.
    /// </summary>
    public sealed class LevelProgressRecorder : MonoBehaviour
    {
        private readonly Dictionary<RideGame, int> _stars = new Dictionary<RideGame, int>();
        private readonly List<RideGame> _games = new List<RideGame>();
        private RideDirector _director;
        private bool _recorded;

        /// <summary>The last ride this session finished: its level, stars, the most it could earn, score and whether it set a record.</summary>
        public static string LastLevel { get; private set; }
        public static int LastStars { get; private set; }
        public static int LastMaxStars { get; private set; }
        public static int LastScore { get; private set; }
        public static bool LastWasBest { get; private set; }

        private void Start()
        {
            _director = GetComponent<RideDirector>();
            if (_director == null)
            {
                enabled = false;
                return;
            }

            foreach (RideGame game in FindObjectsByType<RideGame>(FindObjectsInactive.Include))
            {
                if (game.gameObject.scene != gameObject.scene) continue;
                RideGame captured = game;
                game.Won += (stars, byRider) => _stars[captured] = stars;
                _games.Add(game);
            }

            _director.RideCompleted += OnCompleted;
        }

        private void OnDestroy()
        {
            if (_director != null) _director.RideCompleted -= OnCompleted;
        }

        private void OnCompleted()
        {
            if (_recorded) return;
            _recorded = true;
            int stars = 0;
            foreach (int s in _stars.Values) stars += s;
            string level = gameObject.scene.name;
            LastLevel = level;
            LastStars = stars;
            LastMaxStars = _games.Count * 3;
            LastScore = _director.Score;
            LastWasBest = LevelProgress.RecordFinish(level, stars, LastMaxStars, LastScore);
        }
    }

    /// <summary>Puts a <see cref="LevelProgressRecorder"/> on each loaded scene's ride director.</summary>
    public static class LevelProgressBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) Attach(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach(scene);

        private static void Attach(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RideDirector director in root.GetComponentsInChildren<RideDirector>(true))
                {
                    if (director.GetComponent<LevelProgressRecorder>() == null) director.gameObject.AddComponent<LevelProgressRecorder>();
                }
            }
        }
    }
}
