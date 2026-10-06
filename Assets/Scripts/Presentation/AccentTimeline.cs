using UnityEngine;

namespace Convergence.Presentation
{
    /// <summary>
    /// Crisis amber and success cyan. The unsolved pulse is 0.4 Hz and plus or minus 25 percent.
    /// </summary>
    public static class AccentTimeline
    {
        public static readonly Color Amber = new Color(0.98f, 0.62f, 0.08f, 1f);
        public static readonly Color Cyan = new Color(0.20f, 0.75f, 0.98f, 1f);

        public static Color Step(Color current, bool passed, float dt, float time, out float emission)
        {
            Color target = passed ? Cyan : Amber;
            float blend = 1f - Mathf.Exp(-1.2f * Mathf.Max(0f, dt));
            current = Color.Lerp(current, target, blend);
            float wave = Mathf.Sin(time * (2f * Mathf.PI * 0.4f));
            emission = passed ? 1f : 1f + (0.25f * wave);
            return current;
        }
    }
}
