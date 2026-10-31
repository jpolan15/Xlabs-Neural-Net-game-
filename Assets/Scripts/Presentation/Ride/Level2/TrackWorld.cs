using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The bike path of Level 2 (ADR-023), from the dock to the end: a country road with a dashed white line, gravel verges, round trees and
    /// firs, street lamps and green hills far off, under the daylight sky whose meadow is the ground. It is built once when the scene loads
    /// and stays on for the whole ride (one mesh, one draw call), so the road never ends between stops. Where a stop shows its explainer and
    /// game the roadside is kept clear. Dressing only.
    /// </summary>
    public sealed class TrackWorld : MonoBehaviour
    {
        [Tooltip("Where each stop's rider sits along the road (z): the roadside ahead of it is kept clear for the boards and games.")]
        [SerializeField] private float[] stops = new float[0];
        [Tooltip("A longer stretch kept clear (z from, to): the story road, whose forks run on past its stop.")]
        [SerializeField] private Vector2 longClear = new Vector2(1f, 0f);
        [Tooltip("The road runs from here to there (z).")]
        [SerializeField] private Vector2 extent = new Vector2(-40f, 520f);

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();
            Road(m);
            Roadside(m);
            m.Build(transform, "Countryside", kit.Solid);
        }

        /// <summary>How close to a stop's view (metres from the road's middle) anything tall may stand at this z.</summary>
        private float Clear(float z)
        {
            float clear = 9.5f;
            foreach (float s in stops)
            {
                if (z > s - 6f && z < s + 30f) clear = Mathf.Max(clear, 13f);
            }

            if (z > longClear.x && z < longClear.y) clear = Mathf.Max(clear, 13f);
            return clear;
        }

        private bool NearStop(float z)
        {
            foreach (float s in stops)
            {
                if (z > s - 6f && z < s + 26f) return true;
            }

            return z > longClear.x && z < longClear.y;
        }

        private void Road(SolidMesh m)
        {
            Color gravel = new Color(0.86f, 0.78f, 0.62f, 0f);
            for (float z = extent.x; z < extent.y; z += 10f)
            {
                float z1 = Mathf.Min(z + 10f, extent.y);
                Flat(m, -1.35f, 1.35f, z, z1, 0.004f, Level2Scenery.Asphalt);
                Flat(m, -1.75f, -1.35f, z, z1, 0.003f, gravel);
                Flat(m, 1.35f, 1.75f, z, z1, 0.003f, gravel);
                Flat(m, -1.2f, -1.12f, z, z1, 0.008f, Level2Scenery.Paint);
                Flat(m, 1.12f, 1.2f, z, z1, 0.008f, Level2Scenery.Paint);
            }

            for (float z = extent.x; z < extent.y; z += 3.2f) Flat(m, -0.06f, 0.06f, z, z + 1.6f, 0.009f, Level2Scenery.Paint);
        }

        private static void Flat(SolidMesh m, float x0, float x1, float z0, float z1, float y, Color c)
        {
            m.Quad(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.up, c);
        }

        private void Roadside(SolidMesh m)
        {
            int seed = 0;
            for (float z = extent.x; z < extent.y; z += 6.5f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    seed++;
                    float r1 = Fx.Rand01(seed * 7 + 1), r2 = Fx.Rand01(seed * 7 + 2), r3 = Fx.Rand01(seed * 7 + 3);
                    if (r3 < 0.25f) continue;
                    float zz = z + (r1 - 0.5f) * 4f;
                    float x = side * (Clear(zz) + r2 * 9f);
                    if (r1 < 0.3f) Level2Scenery.Fir(m, new Vector3(x, 0f, zz), 1f + r2 * 0.6f);
                    else Level2Scenery.Tree(m, new Vector3(x, 0f, zz), 1f + r2 * 0.5f, seed);
                    if (r2 > 0.55f && !NearStop(zz)) Level2Scenery.Flowers(m, new Vector3(side * (3.2f + r3 * 2f), 0f, zz), new Vector2(1.2f, 2.2f), 7, seed * 13);
                }
            }

            // Street lamps, left and right in turn, away from where the boards stand.
            int k = 0;
            for (float z = extent.x + 8f; z < extent.y; z += 22f, k++)
            {
                if (NearStop(z)) continue;
                float side = k % 2 == 0 ? -1f : 1f;
                Level2Scenery.Lamp(m, new Vector3(side * 2.4f, 0f, z), -side);
            }

            // Green hills far off on both sides, and a second row of firs in front of them.
            for (float z = extent.x; z < extent.y + 80f; z += 45f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    seed++;
                    float r1 = Fx.Rand01(seed * 5 + 1), r2 = Fx.Rand01(seed * 5 + 2);
                    Color hill = Color.Lerp(Level2Scenery.Leaf, Level2Scenery.LeafWarm, r2 * 0.6f);
                    Level2Scenery.Hill(m, new Vector3(side * (70f + 40f * r1), -2f, z + r2 * 30f), new Vector3(34f + 20f * r2, 14f + 12f * r1, 30f + 14f * r1), hill);
                    for (int f = 0; f < 3; f++) Level2Scenery.Fir(m, new Vector3(side * (32f + 6f * f + 8f * r2), 0f, z + f * 11f + r1 * 6f), 1.6f + 0.5f * Fx.Rand01(seed + f));
                }
            }
        }
    }
}
