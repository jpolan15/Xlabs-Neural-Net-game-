using System.Collections.Generic;
using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 5's place: dawn over a sea of cloud, and a long gold road across it. Soft clouds drift under the road, a low sun rises ahead, and the
    /// road's edge lights run on into the distance. Dressing only.
    /// </summary>
    public sealed class RoadWorld : MonoBehaviour
    {
        private GlowField _clouds, _sun;
        private RibbonField _road;
        private static readonly Color Gold = new Color(1f, 0.82f, 0.4f);

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var lines = new List<Shape>();
            foreach (float x in new[] { -1.5f, 1.5f })
            {
                lines.Add(new Shape
                {
                    kind = Shape.Kind.Line, position = new Vector3(x, -0.05f, -20f), end = new Vector3(x, -0.05f, 140f), size = new Vector2(0.09f, 0.09f),
                    color = Fx.A(Gold, 0.95f), round = 1f, rotation = Quaternion.Euler(90f, 0f, 0f),
                });
            }

            for (float z = -20f; z < 140f; z += 4f)
            {
                lines.Add(new Shape
                {
                    kind = Shape.Kind.Line, position = new Vector3(0f, -0.05f, z), end = new Vector3(0f, -0.05f, z + 1.6f), size = new Vector2(0.14f, 0.14f),
                    color = new Color(0.5f, 1f, 0.85f, 0.7f), round = 1f, rotation = Quaternion.Euler(90f, 0f, 0f),
                });
            }

            kit.Batch(transform, "Road", lines, -3);
            _road = RibbonField.Create(transform, "RoadGlow", 1, kit.Beam, -4);
            _clouds = GlowField.Create(transform, "Clouds", 26, kit.Glow, -5);
            _sun = GlowField.Create(transform, "Sun", 3, kit.Glow, -6);
            var dust = new GameObject("Pollen").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(90, new Vector3(10f, 3.5f, 22f), new Vector3(0f, 3f, 24f), 0.1f, 0.2f, new Color(1f, 0.9f, 0.6f, 0.9f), new Color(0.6f, 1f, 0.9f, 0.9f));
        }

        private void Update()
        {
            if (_clouds == null) return;
            _road.Set(0, new Vector3(0f, -0.04f, -20f), new Vector3(0f, -0.04f, 140f), 0f, 3.4f, new Color(1f, 0.8f, 0.4f, 0.22f));
            for (int i = 0; i < 26; i++)
            {
                float r1 = Fx.Rand01(i * 3 + 1), r2 = Fx.Rand01(i * 3 + 2), r3 = Fx.Rand01(i * 3 + 3);
                float x = (r1 * 2f - 1f) * 34f + Mathf.Sin(Time.time * 0.05f + i) * 2f;
                if (Mathf.Abs(x) < 5f) x += Mathf.Sign(x == 0f ? 1f : x) * 8f;
                float z = -10f + r2 * 130f;
                Color c = Color.Lerp(new Color(1f, 0.85f, 0.7f), new Color(0.6f, 1f, 0.95f), r3);
                c.a = 0.16f + 0.1f * r3;
                _clouds.Set(i, new Vector3(x, -3.5f - r3 * 3f, z), new Vector2(18f + 14f * r3, 6f + 5f * r2), c);
            }

            _sun.Set(0, new Vector3(0f, 6f, 150f), 70f, new Color(1f, 0.8f, 0.45f, 0.3f));
            _sun.Set(1, new Vector3(0f, 6f, 149f), 26f, new Color(1f, 0.95f, 0.75f, 0.6f));
            _sun.Set(2, new Vector3(0f, 2f, 140f), new Vector2(170f, 12f), new Color(1f, 0.7f, 0.5f, 0.2f));
        }
    }
}
