using UnityEngine;

namespace Convergence.Presentation
{
    /// <summary>
    /// The one visible prompt. Gameplay does not render it. This board only stores the line.
    /// </summary>
    public sealed class ChamberPromptBoard : MonoBehaviour
    {
        public static ChamberPromptBoard Instance { get; private set; }

        [SerializeField] private string current = "Targeting offline.";

        public string Current => current;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Set(string line)
        {
            current = string.IsNullOrWhiteSpace(line) ? "Targeting offline." : line;
        }
    }
}
