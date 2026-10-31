using System.Collections.Generic;
using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 3's place: a hall of light. Two rows of tall columns with glowing caps, arches of light overhead that stream past, shafts of
    /// light between the columns, and a floor of lanes under the rails. Dressing only.
    /// </summary>
    public sealed class HallWorld : MonoBehaviour
    {
        private GlowField _arches, _shafts;
        private static readonly Color Cyan = new Color(0.25f, 0.85f, 1f);

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            Level2Scenery.Rails(transform, -30f, 70f, Fx.A(Cyan, 0.8f));
            var solids = new SolidMesh();
            for (int i = 0; i < 8; i++)
            {
                float z = 3f + i * 5.5f;
                float h = 6.5f + 3f * Fx.Rand01(i + 21);
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * (6.8f + 1.2f * Fx.Rand01(i * 2 + (side > 0 ? 3 : 4)));
                    solids.Box(new Vector3(x, 0f, z), new Vector3(0.9f, h, 0.9f), new Color(0.6f, 0.95f, 1f, 1f), new Color(0.1f, 0.22f, 0.32f, 0.2f));
                    solids.Box(new Vector3(x, h * 0.55f, z), new Vector3(1.05f, 0.16f, 1.05f), new Color(0.4f, 0.9f, 1f, 1f), new Color(0.3f, 0.8f, 1f, 0.9f));
                    solids.Box(new Vector3(x, h, z), new Vector3(1.2f, 0.18f, 1.2f), new Color(0.8f, 1f, 1f, 1f), new Color(0.4f, 0.9f, 1f, 1f));
                }
            }

            solids.Build(transform, "Columns", kit.Solid);

            var lanes = new List<Shape>();
            for (int lane = -3; lane <= 3; lane++)
            {
                lanes.Add(new Shape
                {
                    kind = Shape.Kind.Line, position = new Vector3(lane * 1.8f, -0.05f, 2f), end = new Vector3(lane * 1.8f, -0.05f, 60f),
                    size = new Vector2(0.035f, 0.035f), color = Fx.A(Cyan, 0.22f), round = 1f, rotation = Quaternion.Euler(90f, 0f, 0f),
                });
            }

            kit.Batch(transform, "Lanes", lanes, -3);

            var dust = new GameObject("Motes").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(90, new Vector3(9f, 4f, 18f), new Vector3(0f, 3.5f, 20f), 0.1f, 0.22f, new Color(0.5f, 0.95f, 1f, 0.9f), new Color(0.9f, 1f, 1f, 0.9f));
            _arches = GlowField.Create(transform, "Arches", 8, kit.Ring, -1);
            _shafts = GlowField.Create(transform, "Shafts", 8, kit.Glow, -2);
        }

        private void Update()
        {
            if (_arches == null) return;
            for (int i = 0; i < 8; i++)
            {
                float z = 4f + Mathf.Repeat(i * 6f - Time.time * 2.2f, 48f);
                float k = Mathf.InverseLerp(4f, 12f, z) * Mathf.InverseLerp(52f, 36f, z);
                _arches.Set(i, new Vector3(0f, 4.2f, z), new Vector2(15f, 15f), new Color(0.3f, 0.9f, 1f, 0.35f * k), 0f);
                float x = (i % 2 == 0 ? -1f : 1f) * (4.2f + 1.2f * Fx.Rand01(i));
                _shafts.Set(i, new Vector3(x, 4.5f, 12f + i * 3.2f), new Vector2(0.9f, 11f), new Color(0.4f, 0.9f, 1f, 0.07f + 0.04f * Mathf.Sin(Time.time * 0.8f + i)), 0f);
            }
        }
    }
}
