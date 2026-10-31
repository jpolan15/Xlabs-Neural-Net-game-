using System.Collections.Generic;
using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 4's place: a neon city at sunset, the city of odds. Towers of every height line both sides, windows lit in pink and cyan, and a
    /// great striped sun sits on the horizon ahead. Dressing only.
    /// </summary>
    public sealed class OddsWorld : MonoBehaviour
    {
        private GlowField _sky;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            Level2Scenery.Rails(transform, -30f, 80f, new Color(1f, 0.45f, 0.75f, 0.8f));

            var solids = new SolidMesh();
            for (int i = 0; i < 16; i++)
            {
                float z = 6f + i * 3.4f;
                foreach (float side in new[] { -1f, 1f })
                {
                    float r1 = Fx.Rand01(i * 5 + (side > 0 ? 1 : 2)), r2 = Fx.Rand01(i * 7 + (side > 0 ? 3 : 4)), r3 = Fx.Rand01(i * 11 + (side > 0 ? 5 : 6));
                    float x = side * (8.2f + 4f * r1 + (i % 3) * 1.2f);
                    float h = 4f + 12f * r2 * r2 + 1.5f;
                    float w = 1.8f + 2.2f * r3;
                    Color roof = side < 0 ? new Color(1f, 0.55f, 0.85f, 1f) : new Color(0.45f, 0.9f, 1f, 1f);
                    solids.Box(new Vector3(x, 0f, z), new Vector3(w, h, w), roof, new Color(0.22f, 0.08f, 0.25f, 0.1f + 0.2f * r3));
                    // A lit band of windows.
                    solids.Box(new Vector3(x, h * 0.35f, z), new Vector3(w + 0.06f, 0.14f, w + 0.06f), roof, Fx.Mul(roof, 0.9f));
                    solids.Box(new Vector3(x, h * 0.7f, z), new Vector3(w + 0.06f, 0.1f, w + 0.06f), roof, Fx.Mul(roof, 0.7f));
                }
            }

            solids.Build(transform, "City", kit.Solid);

            // The sun: a disc with dark bars across its lower half.
            var sun = new List<Shape>
            {
                Level2Kit.Square(new Vector3(0f, 11f, 120f), new Vector2(46f, 46f), new Color(1f, 0.45f, 0.5f, 0.95f), 1f),
                Level2Kit.Square(new Vector3(0f, 11f, 119.5f), new Vector2(36f, 36f), new Color(1f, 0.78f, 0.45f, 0.95f), 1f),
            };
            for (int k = 0; k < 8; k++)
            {
                float y = 11f - 3f - k * 2.4f;
                sun.Add(Level2Kit.Square(new Vector3(0f, y, 119f), new Vector2(50f, 0.35f + 0.22f * k), new Color(0.2f, 0.06f, 0.22f, 1f), 0f));
            }

            kit.Batch(transform, "Sun", sun, -4);
            _sky = GlowField.Create(transform, "SunGlow", 3, kit.Glow, -5);
            var dust = new GameObject("Sparkle").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(80, new Vector3(10f, 3f, 16f), new Vector3(0f, 3.5f, 20f), 0.1f, 0.2f, new Color(1f, 0.6f, 0.85f, 0.9f), new Color(0.6f, 0.95f, 1f, 0.9f));
        }

        private void Update()
        {
            if (_sky == null) return;
            _sky.Set(0, new Vector3(0f, 11f, 124f), 90f, new Color(1f, 0.4f, 0.55f, 0.28f));
            _sky.Set(1, new Vector3(0f, 8f, 124f), 140f, new Color(0.8f, 0.3f, 0.8f, 0.16f));
            _sky.Set(2, new Vector3(0f, 3f, 100f), new Vector2(160f, 14f), new Color(1f, 0.55f, 0.65f, 0.18f + 0.04f * Mathf.Sin(Time.time * 0.6f)), 0f);
        }
    }
}
