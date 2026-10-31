using UnityEngine;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 2's place: open space. A deep violet night with slow clouds of colour far behind the words, drifting dust and a spiral of
    /// light, and the rails running on under the pod. Dressing only.
    /// </summary>
    public sealed class GalaxyWorld : MonoBehaviour
    {
        private GlowField _clouds, _arm;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            Level2Scenery.Rails(transform, -30f, 60f, new Color(0.62f, 0.5f, 1f, 0.7f));
            var dust = new GameObject("Dust").AddComponent<DustField>();
            dust.transform.SetParent(transform, false);
            dust.Configure(160, new Vector3(16f, 8f, 20f), new Vector3(0f, 3f, 14f), 0.1f, 0.18f, new Color(0.7f, 0.65f, 1f, 0.9f), new Color(0.6f, 0.9f, 1f, 0.9f));
            _clouds = GlowField.Create(transform, "Clouds", 7, kit.Glow, -3);
            _arm = GlowField.Create(transform, "Arm", 44, kit.Glow, -2);
        }

        private void Update()
        {
            if (_clouds == null) return;
            Color[] tint =
            {
                new Color(0.45f, 0.3f, 1f, 0.16f), new Color(1f, 0.35f, 0.7f, 0.12f), new Color(0.2f, 0.7f, 1f, 0.12f), new Color(0.6f, 0.4f, 1f, 0.14f),
                new Color(1f, 0.6f, 0.4f, 0.08f), new Color(0.3f, 1f, 0.8f, 0.08f), new Color(0.8f, 0.5f, 1f, 0.1f),
            };
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f + Time.time * 0.01f;
                _clouds.Set(i, new Vector3(Mathf.Sin(a) * 26f, 5f + Mathf.Cos(a * 1.7f) * 6f, 38f + Mathf.Cos(a) * 12f), Vector2.one * (30f + i * 3f), tint[i], Time.time * 0.01f * i);
            }

            // A spiral arm of tiny suns, turning very slowly, far behind the words.
            for (int i = 0; i < 44; i++)
            {
                float t = i / 44f;
                float angle = t * Mathf.PI * 3.2f + Time.time * 0.05f;
                float r = 6f + t * 22f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * r, 6f + Mathf.Sin(angle * 0.5f) * 1.5f, 46f + Mathf.Sin(angle) * r * 0.45f);
                Color c = Color.Lerp(new Color(0.6f, 0.7f, 1f), new Color(1f, 0.7f, 0.9f), t);
                c.a = 0.55f * (1f - t * 0.6f) * (0.7f + 0.3f * Mathf.Sin(Time.time * 1.5f + i));
                _arm.Set(i, p, 0.5f + (1f - t) * 0.7f, c);
            }
        }
    }
}
