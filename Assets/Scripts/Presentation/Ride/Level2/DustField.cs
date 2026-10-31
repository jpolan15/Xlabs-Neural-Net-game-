using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Drifting motes of light around a place: the dust and stars every world has. Each mote wanders in its own slow loop and
    /// twinkles; one draw call. The place is the object's own space.
    /// </summary>
    public sealed class DustField : MonoBehaviour
    {
        [SerializeField] private int count = 90;
        [SerializeField] private Vector3 extent = new Vector3(14f, 6f, 14f);
        [SerializeField] private Vector3 centre = new Vector3(0f, 2f, 10f);
        [SerializeField] private float size = 0.12f;
        [SerializeField] private float speed = 0.25f;
        [SerializeField] private Color tint = new Color(0.7f, 0.85f, 1f, 0.8f);
        [SerializeField] private Color tint2 = new Color(1f, 0.8f, 0.9f, 0.8f);
        [SerializeField] private bool useRings;

        private GlowField _field;

        public void Configure(int motes, Vector3 spread, Vector3 middle, float motesSize, float drift, Color a, Color b)
        {
            count = motes;
            extent = spread;
            centre = middle;
            size = motesSize;
            speed = drift;
            tint = a;
            tint2 = b;
        }

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            _field = GlowField.Create(transform, "Dust", count, useRings ? kit.Ring : kit.Glow, -2);
        }

        private void Update()
        {
            if (_field == null) return;
            float t = Time.time * speed;
            for (int i = 0; i < count; i++)
            {
                float r1 = Fx.Rand01(i * 3 + 1), r2 = Fx.Rand01(i * 3 + 2), r3 = Fx.Rand01(i * 3 + 3);
                Vector3 p = centre + new Vector3(
                    (r1 * 2f - 1f) * extent.x + Mathf.Sin(t * (0.6f + r2) + i) * 0.6f,
                    (r2 * 2f - 1f) * extent.y + Mathf.Sin(t * (0.5f + r3) + i * 1.3f) * 0.5f,
                    (r3 * 2f - 1f) * extent.z + Mathf.Cos(t * (0.4f + r1) + i * 0.7f) * 0.6f);
                float twinkle = 0.45f + 0.55f * Mathf.Sin(Time.time * (0.8f + r1 * 2.2f) + i * 2.1f);
                Color c = Color.Lerp(tint, tint2, r2);
                c.a *= twinkle * twinkle;
                _field.Set(i, p, size * (0.5f + r3 * 1.4f), c);
            }
        }
    }
}
