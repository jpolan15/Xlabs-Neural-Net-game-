using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The dock of Level 2: a violet night with a slow drift of light and a tunnel of rings ahead, the way into the machine.
    /// Dressing only.
    /// </summary>
    public sealed class DockWorld : MonoBehaviour
    {
        private GlowField _rings, _halo;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            Level2Scenery.Rails(transform, -30f, 60f, new Color(0.7f, 0.55f, 1f, 0.7f));
            var dust = new GameObject("Drift").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(110, new Vector3(12f, 4f, 22f), new Vector3(0f, 2.6f, 18f), 0.11f, 0.2f, new Color(0.75f, 0.65f, 1f, 0.9f), new Color(1f, 0.7f, 0.9f, 0.9f));
            _rings = GlowField.Create(transform, "Tunnel", 14, kit.Ring, -1);
            _halo = GlowField.Create(transform, "Halo", 2, kit.Glow, -2);
        }

        private void Update()
        {
            if (_rings == null) return;
            for (int i = 0; i < 14; i++)
            {
                // Rings stream toward the rider and loop: the machine breathing.
                float z = 14f + Mathf.Repeat(i * 4.2f - Time.time * 1.4f, 14f * 4.2f);
                float near = Mathf.InverseLerp(14f, 24f, z) * Mathf.InverseLerp(72f, 56f, z);
                Color c = Color.Lerp(new Color(0.55f, 0.4f, 1f), new Color(1f, 0.5f, 0.85f), i / 14f);
                c.a = 0.55f * near;
                _rings.Set(i, new Vector3(0f, 2.8f, z), new Vector2(12f + z * 0.15f, 12f + z * 0.15f), c, Time.time * 0.1f + i * 0.2f);
            }

            _halo.Set(0, new Vector3(0f, 2.8f, 80f), 36f, new Color(0.7f, 0.5f, 1f, 0.35f));
            _halo.Set(1, new Vector3(0f, 2.8f, 79f), 14f, new Color(1f, 0.8f, 1f, 0.35f));
        }
    }
}
