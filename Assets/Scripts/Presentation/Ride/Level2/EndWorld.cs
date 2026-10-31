using System.Collections.Generic;
using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The place after the last stop: a night of slow curtains of colour (an aurora) over the road's end, with stars. Dressing only.
    /// </summary>
    public sealed class EndWorld : MonoBehaviour
    {
        private RibbonField _curtains;
        private GlowField _glows;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            Level2Scenery.Rails(transform, -40f, 40f, new Color(0.7f, 0.6f, 1f, 0.7f));
            _curtains = RibbonField.Create(transform, "Aurora", 9, kit.Beam, -3);
            _glows = GlowField.Create(transform, "Glows", 4, kit.Glow, -4);
            var dust = new GameObject("Stars").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(140, new Vector3(18f, 8f, 24f), new Vector3(0f, 4f, 20f), 0.11f, 0.15f, new Color(0.8f, 0.8f, 1f, 0.9f), new Color(1f, 0.85f, 0.7f, 0.9f));
        }

        private void Update()
        {
            if (_curtains == null) return;
            Color[] tint = { new Color(0.4f, 1f, 0.8f), new Color(0.6f, 0.5f, 1f), new Color(1f, 0.5f, 0.8f) };
            for (int i = 0; i < 9; i++)
            {
                float x = -24f + i * 6f;
                float sway = Mathf.Sin(Time.time * 0.3f + i * 0.9f) * 3f;
                Color c = tint[i % 3];
                c.a = 0.18f + 0.08f * Mathf.Sin(Time.time * 0.5f + i);
                _curtains.Set(i, new Vector3(x, 14f, 46f), new Vector3(x + sway, 1f, 44f + Mathf.Sin(i) * 6f), 0f, 3.2f + 1.4f * Mathf.Sin(i * 1.3f), c);
            }

            _glows.Set(0, new Vector3(0f, 12f, 70f), 80f, new Color(0.6f, 0.5f, 1f, 0.16f));
            _glows.Set(1, new Vector3(-20f, 8f, 60f), 40f, new Color(0.3f, 1f, 0.8f, 0.1f));
            _glows.Set(2, new Vector3(22f, 10f, 64f), 44f, new Color(1f, 0.5f, 0.8f, 0.1f));
        }
    }
}
