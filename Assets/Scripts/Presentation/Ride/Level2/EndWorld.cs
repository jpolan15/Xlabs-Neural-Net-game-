using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The finish of Level 2 (ADR-023): a finish arch with a chequered banner over the road at dusk, a crowd of flags, and balloons that
    /// keep rising into the sunset sky. Dressing only.
    /// </summary>
    public sealed class EndWorld : MonoBehaviour
    {
        private const int Balloons = 14;
        private Transform _balloons;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();
            Color white = new Color(0.98f, 0.97f, 0.94f, 0f), ink = new Color(0.12f, 0.13f, 0.18f, 0f);

            // The finish arch, behind the finale's board, and its chequered banner.
            foreach (float s in new[] { -1f, 1f }) m.Post(new Vector3(s * 5f, 0f, 26f), 0.3f, 7.4f, 8, white, white);
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    m.Block(new Vector3(-4.75f + i * 0.5f, 6.6f + j * 0.5f, 26f), new Vector3(0.5f, 0.5f, 0.2f), Quaternion.identity, (i + j) % 2 == 0 ? white : ink);
                }
            }

            // Flags on poles along both sides.
            for (int i = 0; i < 8; i++)
            {
                float s = i % 2 == 0 ? -1f : 1f;
                Vector3 b = new Vector3(s * (9.5f + (i / 2) * 1.5f), 0f, 14f + (i / 2) * 4f);
                m.Post(b, 0.06f, 4.4f, 5, white, white);
                m.Block(b + new Vector3(-s * 0.55f, 4.0f, 0f), new Vector3(1.0f, 0.65f, 0.03f), Quaternion.identity, Level2Scenery.Bright[i % 6]);
            }

            m.Build(transform, "Finish", kit.Solid);

            _balloons = new GameObject("Balloons").transform;
            _balloons.SetParent(transform, false);
            for (int i = 0; i < Balloons; i++)
            {
                var one = new SolidMesh();
                one.Ball(Vector3.zero, new Vector3(0.45f, 0.55f, 0.45f), Level2Scenery.Bright[i % 6], 7, 5);
                one.Block(new Vector3(0f, -0.9f, 0f), new Vector3(0.02f, 0.7f, 0.02f), Quaternion.identity, white);
                one.Build(_balloons, "Balloon" + i, kit.Solid);
            }
        }

        private void Update()
        {
            if (_balloons == null) return;
            for (int i = 0; i < _balloons.childCount; i++)
            {
                float r1 = Fx.Rand01(i * 3 + 1), r2 = Fx.Rand01(i * 3 + 2);
                float t = Mathf.Repeat(Time.time * (0.05f + 0.03f * r2) + r1, 1f);
                float side = i % 2 == 0 ? -1f : 1f;
                _balloons.GetChild(i).localPosition = new Vector3(side * (9f + 10f * r2) + Mathf.Sin(Time.time * 0.6f + i) * 0.6f, -1f + t * 22f, 20f + r1 * 20f); // beside and behind the big screen
            }
        }
    }
}
