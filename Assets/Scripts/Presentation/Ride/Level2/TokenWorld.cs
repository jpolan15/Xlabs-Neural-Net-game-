using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 1's place: the chopper, a long amber hall of pylons and gates over glowing rails, with embers in the air and a cutter
    /// beam sweeping across the way. Dressing only; it is on while the stop's region is.
    /// </summary>
    public sealed class TokenWorld : MonoBehaviour
    {
        private GlowField _beam;
        private static readonly Color Amber = new Color(1f, 0.64f, 0.2f);

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            Level2Scenery.Rails(transform, -30f, 70f, Fx.A(Amber, 0.8f));

            var solids = new SolidMesh();
            for (int i = 0; i < 9; i++)
            {
                float z = 4f + i * 4.6f;
                float h = 3.2f + 2.4f * Fx.Rand01(i + 7);
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * (5.6f + 1.4f * Fx.Rand01(i * 2 + (side > 0 ? 1 : 2)));
                    solids.Box(new Vector3(x, 0f, z), new Vector3(1.1f, h, 1.1f), new Color(1f, 0.78f, 0.35f, 0.9f), new Color(0.32f, 0.2f, 0.12f, 0.12f));
                    solids.Box(new Vector3(x, h, z), new Vector3(1.3f, 0.14f, 1.3f), new Color(1f, 0.9f, 0.6f, 1f), new Color(1f, 0.7f, 0.3f, 0.9f));
                }
            }

            solids.Build(transform, "Pylons", kit.Solid);

            var frames = new List<Shape>();
            foreach (float z in new[] { 15f, 25f, 35f, 45f })
            {
                Level2Scenery.Frame(frames, new Vector3(0f, 2.9f, z), new Vector2(11.2f, 5.8f), 0.07f, Fx.A(Amber, 0.55f));
            }

            for (int lane = -2; lane <= 2; lane++)
            {
                frames.Add(new Shape
                {
                    kind = Shape.Kind.Line, position = new Vector3(lane * 2.2f, -0.05f, 4f), end = new Vector3(lane * 2.2f, -0.05f, 60f),
                    size = new Vector2(0.04f, 0.04f), color = Fx.A(Amber, 0.28f), round = 1f, rotation = Quaternion.Euler(90f, 0f, 0f),
                });
            }

            kit.Batch(transform, "Gates", frames, -2);

            var dust = new GameObject("Embers").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(80, new Vector3(9f, 3.4f, 20f), new Vector3(0f, 2.6f, 22f), 0.1f, 0.3f, new Color(1f, 0.7f, 0.3f, 0.9f), new Color(1f, 0.95f, 0.7f, 0.9f));

            _beam = GlowField.Create(transform, "CutterBeam", 3, kit.Glow, -1);
        }

        private void Update()
        {
            if (_beam == null) return;
            float x = Mathf.Sin(Time.time * 0.9f) * 4.6f;
            _beam.Set(0, new Vector3(x, 2.8f, 19f), new Vector2(0.12f, 5.6f), new Color(1f, 0.85f, 0.5f, 0.55f), 0f);
            _beam.Set(1, new Vector3(x, 2.8f, 19f), new Vector2(0.9f, 6.2f), new Color(1f, 0.6f, 0.2f, 0.12f), 0f);
            _beam.Set(2, new Vector3(x, 0.15f, 19f), new Vector2(1.6f, 1.6f), new Color(1f, 0.8f, 0.4f, 0.4f), 0f);
        }
    }
}
