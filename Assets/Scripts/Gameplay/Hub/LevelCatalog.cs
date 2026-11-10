using System;
using UnityEngine;

namespace Convergence.Gameplay.Hub
{
    /// <summary>
    /// The levels the hub offers, in order (ADR-024). One entry per level: its scene (a scene in the build settings, by name), what the
    /// card says and its colour. Adding a level is adding an entry here (Assets/ScriptableObjects/LevelCatalog.asset) and its scene to the
    /// build; an entry with no scene shows as "coming soon". The hub builds its cards from this list when it loads, so nothing is rebuilt.
    /// </summary>
    [CreateAssetMenu(menuName = "Convergence/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("The badge on the card (\"LEVEL 2\").")]
            public string badge = "LEVEL";
            [Tooltip("The level's name, big. A \\n splits it over two lines.")]
            public string title = "";
            [Tooltip("One or two short sentences: what the rider does.")]
            [TextArea] public string tagline = "";
            [Tooltip("The stops, in order, as the level's intro names them.")]
            public string[] stops = new string[0];
            [Tooltip("The scene's name in the build settings. Empty: the card says COMING SOON.")]
            public string scene = "";
            [Tooltip("About how long a ride takes, minutes (0: not shown).")]
            public float minutes;
            public Color accent = Color.white;
        }

        [SerializeField] private Entry[] levels = new Entry[0];

        public Entry[] Levels => levels ?? new Entry[0];

        /// <summary>True when the entry names a scene that is in the build, so the hub can load it.</summary>
        public static bool IsPlayable(Entry entry) => entry != null && !string.IsNullOrEmpty(entry.scene) && Application.CanStreamedLevelBeLoaded(entry.scene);

        /// <summary>Replaces the list (the builder's first fill, and tests).</summary>
        public void SetLevels(Entry[] entries) => levels = entries ?? new Entry[0];
    }
}
