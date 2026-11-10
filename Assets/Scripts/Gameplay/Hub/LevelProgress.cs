using UnityEngine;

namespace Convergence.Gameplay.Hub
{
    /// <summary>
    /// The scoreboard the hub shows (ADR-024): per level (by scene name) how many rides were finished, the best stars of one ride and the
    /// high score. Kept in PlayerPrefs on the device, so a booth's headset remembers the best ride of the day. The hub's operator can
    /// wipe it (Backspace on the desk) before a new group.
    /// </summary>
    public static class LevelProgress
    {
        public struct Record
        {
            public int Finished;
            public int BestStars;
            public int MaxStars;
            public int HighScore;
            public int LastStars;
            public int LastScore;

            public bool Ridden => Finished > 0;
        }

        private const string Prefix = "convergence.level.";
        private const string ListKey = "convergence.levels";

        /// <summary>The scoreboard changed (a ride was recorded or the board was wiped).</summary>
        public static event System.Action Changed;

        private static string Key(string level, string field) => Prefix + level + "." + field;

        public static Record Get(string level)
        {
            if (string.IsNullOrEmpty(level)) return default;
            return new Record
            {
                Finished = PlayerPrefs.GetInt(Key(level, "finished"), 0),
                BestStars = PlayerPrefs.GetInt(Key(level, "bestStars"), 0),
                MaxStars = PlayerPrefs.GetInt(Key(level, "maxStars"), 0),
                HighScore = PlayerPrefs.GetInt(Key(level, "highScore"), 0),
                LastStars = PlayerPrefs.GetInt(Key(level, "lastStars"), 0),
                LastScore = PlayerPrefs.GetInt(Key(level, "lastScore"), 0),
            };
        }

        /// <summary>A finished ride. Returns true when it beat the level's high score (or its best stars).</summary>
        public static bool RecordFinish(string level, int stars, int maxStars, int score)
        {
            if (string.IsNullOrEmpty(level)) return false;
            Record before = Get(level);
            bool best = stars > before.BestStars || score > before.HighScore;
            PlayerPrefs.SetInt(Key(level, "finished"), before.Finished + 1);
            PlayerPrefs.SetInt(Key(level, "bestStars"), Mathf.Max(before.BestStars, stars));
            PlayerPrefs.SetInt(Key(level, "maxStars"), Mathf.Max(before.MaxStars, maxStars));
            PlayerPrefs.SetInt(Key(level, "highScore"), Mathf.Max(before.HighScore, score));
            PlayerPrefs.SetInt(Key(level, "lastStars"), stars);
            PlayerPrefs.SetInt(Key(level, "lastScore"), score);
            string list = PlayerPrefs.GetString(ListKey, "");
            if (System.Array.IndexOf(list.Split('|'), level) < 0) PlayerPrefs.SetString(ListKey, list.Length == 0 ? level : list + "|" + level);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return best;
        }

        /// <summary>Forgets one level's record.</summary>
        public static void Clear(string level)
        {
            if (string.IsNullOrEmpty(level)) return;
            foreach (string field in new[] { "finished", "bestStars", "maxStars", "highScore", "lastStars", "lastScore" }) PlayerPrefs.DeleteKey(Key(level, field));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Wipes the whole scoreboard (every level ever recorded), for a new group at the booth.</summary>
        public static void ClearAll()
        {
            foreach (string level in PlayerPrefs.GetString(ListKey, "").Split('|'))
            {
                if (level.Length > 0) Clear(level);
            }

            PlayerPrefs.DeleteKey(ListKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
