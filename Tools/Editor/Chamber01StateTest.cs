using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Convergence.Presentation;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Steps AccentTimeline passed, failed, passed. Writes JSON the verify script reads.
    /// Does not enter Play Mode and does not leave objects in the scene.
    /// </summary>
    public static class Chamber01StateTest
    {
        public static void Write()
        {
            Color color = AccentTimeline.Amber;
            float time = 0f;
            const float dt = 0.05f;
            color = Run(color, true, 3f, ref time, dt, out _);
            bool cyan = Distance(color, AccentTimeline.Cyan) < 0.2f;
            color = Run(color, false, 3f, ref time, dt, out int peaks);
            bool amber = Distance(color, AccentTimeline.Amber) < 0.2f;
            color = Run(color, true, 3f, ref time, dt, out _);
            bool cyanAgain = Distance(color, AccentTimeline.Cyan) < 0.2f;
            float flashHz = peaks / 4f;
            int damage = UnityEngine.Object.FindObjectsByType<StateBoundDamage>(FindObjectsInactive.Exclude).Length;
            bool pass = cyan && amber && cyanAgain && flashHz <= 3f;

            var builder = new StringBuilder();
            builder.Append("{\n");
            builder.Append("  \"passed\": ").Append(pass ? "true" : "false").Append(",\n");
            builder.Append("  \"reachedCyan\": ").Append(cyan ? "true" : "false").Append(",\n");
            builder.Append("  \"returnedAmber\": ").Append(amber ? "true" : "false").Append(",\n");
            builder.Append("  \"reachedCyanAgain\": ").Append(cyanAgain ? "true" : "false").Append(",\n");
            builder.Append("  \"flashHz\": ").Append(flashHz.ToString("0.###", CultureInfo.InvariantCulture)).Append(",\n");
            builder.Append("  \"damageElements\": ").Append(damage.ToString(CultureInfo.InvariantCulture)).Append("\n");
            builder.Append("}\n");

            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root, "Documentation", "Design", "captures", "state_test.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, builder.ToString());
            Debug.Log("[Chamber01StateTest] pass=" + pass + " flashHz=" + flashHz.ToString("0.###", CultureInfo.InvariantCulture) + " damage=" + damage);
        }

        static Color Run(Color color, bool passed, float seconds, ref float time, float dt, out int peaks)
        {
            peaks = 0;
            float previous = 1f;
            bool rising = false;
            int steps = Mathf.RoundToInt(seconds / dt);
            for (int i = 0; i < steps; i++)
            {
                color = AccentTimeline.Step(color, passed, dt, time, out float emission);
                time += dt;
                if (!passed)
                {
                    if (emission > previous) rising = true;
                    else if (rising && emission < previous)
                    {
                        peaks++;
                        rising = false;
                    }
                }

                previous = emission;
            }

            return color;
        }

        static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }
    }
}
