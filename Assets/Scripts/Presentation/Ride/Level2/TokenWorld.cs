using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 1's place (ADR-023): the chopper, a red sawmill by the road whose big saw blade spins on its wall, with piles of letter blocks
    /// (the pieces it cuts) stacked on the other side and a sign that names the stop. Dressing only; it is on while the stop's region is.
    /// </summary>
    public sealed class TokenWorld : MonoBehaviour
    {
        private Transform _saw;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();

            // The mill: a long red barn on the left, its gable end toward the road (the saw turns on it), a chimney, logs by the door.
            Color barn = new Color(0.82f, 0.26f, 0.22f, 0f);
            m.Box(new Vector3(-17f, 0f, 30f), new Vector3(12f, 5.2f, 7f), barn, barn);
            m.Roof(new Vector3(-17f, 5.2f, 30f), new Vector3(12f, 0f, 7f), 3.0f, new Color(0.36f, 0.3f, 0.32f, 0f), barn);
            m.Box(new Vector3(-20f, 6f, 31.5f), new Vector3(0.9f, 4f, 0.9f), Level2Scenery.Stone, Level2Scenery.Stone);
            m.Block(new Vector3(-10.95f, 1.6f, 28.4f), new Vector3(0.08f, 3.2f, 2.4f), Quaternion.identity, Level2Scenery.Wood);
            for (int i = 0; i < 6; i++)
            {
                float y = 0.3f + (i < 3 ? 0f : 0.55f), z = 21f + (i % 3) * 0.62f + (i < 3 ? 0f : 0.31f);
                m.Block(new Vector3(-9.6f, y, z + 3f), new Vector3(2.6f, 0.55f, 0.55f), Quaternion.identity, Level2Scenery.Wood);
            }

            // Letter blocks on the right: the pieces, piled up in bright colours.
            for (int i = 0; i < 26; i++)
            {
                float r1 = Fx.Rand01(i * 4 + 1), r2 = Fx.Rand01(i * 4 + 2), r3 = Fx.Rand01(i * 4 + 3);
                int layer = i < 14 ? 0 : i < 22 ? 1 : 2;
                float x = 12.5f + (i % 5) * 1.15f + layer * 0.5f + r1 * 0.2f, z = 20f + (i % 7) * 1.4f + layer * 0.6f;
                m.Block(new Vector3(x, 0.5f + layer * 1.0f, z), Vector3.one, Quaternion.Euler(0f, (r2 - 0.5f) * 30f, 0f), Level2Scenery.Bright[(int)(r3 * 6f) % 6]);
            }

            string letters = "TOKEN";
            for (int i = 0; i < letters.Length; i++)
            {
                Vector3 at = new Vector3(10.6f + i * 1.25f, 0.55f, 15f + i * 0.15f);
                m.Block(at, new Vector3(1.1f, 1.1f, 1.1f), Quaternion.identity, Level2Scenery.Bright[(i + 2) % 6]);
                Level2Scenery.Words(transform, "Letter" + i, letters[i].ToString(), 0.8f, Color.white, at + new Vector3(0f, 0.05f, -0.58f), 1f);
            }

            Level2Scenery.Sign(m, transform, new Vector3(-9.5f, 0f, 17f), new Vector2(3.4f, 1.1f), "THE CHOPPER", new Color(0.98f, 0.93f, 0.8f, 0f), new Color(0.75f, 0.2f, 0.16f));
            m.Build(transform, "Mill", kit.Solid);

            // The saw blade on the mill's front wall, turning toward the road.
            var saw = new SolidMesh();
            Color steel = new Color(0.82f, 0.86f, 0.9f, 0f);
            for (int t = 0; t < 16; t++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, t * 22.5f);
                saw.Block(Vector3.zero, new Vector3(0.6f, 3.4f, 0.06f), turn, steel);
                saw.Block(turn * new Vector3(0.18f, 1.75f, 0f), new Vector3(0.36f, 0.3f, 0.07f), turn * Quaternion.Euler(0f, 0f, 35f), steel);
            }

            saw.Ball(new Vector3(0f, 0f, -0.05f), new Vector3(0.35f, 0.35f, 0.12f), barn, 8, 4);
            _saw = saw.Build(transform, "Saw", kit.Solid).transform;
            _saw.localPosition = new Vector3(-10.8f, 3.0f, 31.6f);
        }

        private void Update()
        {
            if (_saw != null) _saw.localRotation = Quaternion.Euler(0f, 90f, Time.time * 160f);
        }
    }
}
