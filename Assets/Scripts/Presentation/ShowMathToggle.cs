using UnityEngine;

namespace Convergence.Presentation
{
    /// <summary>
    /// Shows or hides the formula line. Default is hidden. This does not judge the puzzle.
    /// </summary>
    public sealed class ShowMathToggle : MonoBehaviour
    {
        [SerializeField] private WorldSpaceHud hud;

        public void Toggle()
        {
            if (hud != null) hud.ToggleMath();
        }
    }
}
