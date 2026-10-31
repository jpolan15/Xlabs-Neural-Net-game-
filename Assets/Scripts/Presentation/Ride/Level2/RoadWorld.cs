using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 5's place (ADR-023): farm country at sunset, where the story road forks. Striped fields and hay bales on both sides, a farmhouse
    /// and a red barn far off, and a signpost at the start of the forks. The forks themselves are the game's (StoryView). Dressing only.
    /// </summary>
    public sealed class RoadWorld : MonoBehaviour
    {
        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();

            // Fields in stripes, well clear of the lanes.
            Color[] crops = { new Color(0.86f, 0.74f, 0.36f, 0f), new Color(0.56f, 0.72f, 0.32f, 0f), new Color(0.74f, 0.6f, 0.36f, 0f) };
            foreach (float s in new[] { -1f, 1f })
            {
                for (int row = 0; row < 14; row++)
                {
                    float x0 = s * (11f + row * 1.6f), x1 = x0 + s * 1.3f;
                    Color c = crops[(row + (s > 0 ? 1 : 0)) % 3];
                    m.Quad(new Vector3(Mathf.Min(x0, x1), 0.02f, -10f), new Vector3(Mathf.Max(x0, x1), 0.02f, -10f), new Vector3(Mathf.Max(x0, x1), 0.02f, 110f), new Vector3(Mathf.Min(x0, x1), 0.02f, 110f), Vector3.up, c);
                }
            }

            // Hay bales: rolls lying in the fields.
            for (int i = 0; i < 12; i++)
            {
                float s = i % 2 == 0 ? -1f : 1f;
                float x = s * (13f + 12f * Fx.Rand01(i + 60)), z = 6f + i * 8f;
                for (int k = 0; k < 10; k++)
                {
                    float a0 = k * 36f;
                    m.Block(new Vector3(x, 0.75f, z), new Vector3(1.6f, 0.48f, 0.48f), Quaternion.Euler(a0, 0f, 0f), new Color(0.88f, 0.74f, 0.4f, 0f));
                }
            }

            // The farm, far off: a farmhouse on the left, a red barn and a silo on the right.
            Level2Scenery.House(m, new Vector3(-34f, 0f, 60f), new Vector3(7f, 4f, 8f), Level2Scenery.RoofBlue, 1f);
            Color barn = new Color(0.82f, 0.26f, 0.22f, 0f);
            m.Box(new Vector3(36f, 0f, 70f), new Vector3(10f, 6f, 12f), barn, barn);
            m.Roof(new Vector3(36f, 6f, 70f), new Vector3(10f, 0f, 12f), 3.6f, new Color(0.36f, 0.3f, 0.32f, 0f), barn);
            m.Post(new Vector3(44f, 0f, 76f), 2.2f, 11f, 10, Level2Scenery.Stone, Level2Scenery.Stone);
            m.Ball(new Vector3(44f, 11f, 76f), new Vector3(2.2f, 1.6f, 2.2f), Level2Scenery.Stone, 10, 5, 0.5f);

            // The signpost where the story begins.
            // On the verge left of the lanes, so it never stands between the rider and a gate.
            m.Post(new Vector3(-8f, 0f, 7f), 0.1f, 3.2f, 6, Level2Scenery.Wood, Level2Scenery.Wood);
            m.Block(new Vector3(-7.2f, 2.8f, 7f), new Vector3(1.8f, 0.45f, 0.08f), Quaternion.Euler(0f, 0f, 6f), Level2Scenery.Wall);
            m.Block(new Vector3(-8.7f, 2.25f, 7f), new Vector3(1.6f, 0.45f, 0.08f), Quaternion.Euler(0f, 0f, -6f), Level2Scenery.Wall);
            Level2Scenery.Words(transform, "SignA", "STORIES  >", 0.2f, new Color(0.25f, 0.5f, 0.3f), new Vector3(-7.2f, 2.85f, 6.94f), 1.7f);
            Level2Scenery.Words(transform, "SignB", "<  FARM", 0.2f, new Color(0.7f, 0.2f, 0.4f), new Vector3(-8.7f, 2.25f, 6.94f), 1.5f);
            m.Build(transform, "Farm", kit.Solid);
        }
    }
}
