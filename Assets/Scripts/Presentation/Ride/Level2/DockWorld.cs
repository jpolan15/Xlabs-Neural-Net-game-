using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The dock of Level 2 (ADR-023): a sunny morning at the start of the bike path. A start arch over the road says where we are going,
    /// a windmill turns on the hill to the right, a cottage and its bike shed sit on the left. Dressing only.
    /// </summary>
    public sealed class DockWorld : MonoBehaviour
    {
        private Transform _blades;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();

            // The start arch, past the chat window, with its banner.
            Color post = new Color(0.98f, 0.97f, 0.93f, 0f);
            foreach (float s in new[] { -1f, 1f }) m.Post(new Vector3(s * 3.6f, 0f, 24f), 0.22f, 5.2f, 8, post, post);
            m.Block(new Vector3(0f, 5.0f, 24f), new Vector3(8.2f, 1.2f, 0.25f), Quaternion.identity, new Color(1f, 0.42f, 0.32f, 0f));
            Level2Scenery.Words(transform, "ArchWords", "LEVEL 2  -  THE WORD MACHINE", 0.55f, Color.white, new Vector3(0f, 5.0f, 23.8f), 7.8f);
            for (int i = 0; i < 9; i++)
            {
                float x = -3.2f + i * 0.8f;
                m.Block(new Vector3(x, 4.15f - 0.12f * Mathf.Sin(i / 8f * Mathf.PI), 24f), new Vector3(0.36f, 0.42f, 0.03f), Quaternion.Euler(0f, 0f, 45f), Level2Scenery.Bright[i % Level2Scenery.Bright.Length]);
            }

            // The cottage and its bike shed on the left.
            Level2Scenery.House(m, new Vector3(-14f, 0f, 14f), new Vector3(5f, 3.2f, 6f), Level2Scenery.RoofRed, 1f);
            m.Box(new Vector3(-10.2f, 0f, 21f), new Vector3(2.6f, 2.2f, 3.6f), Level2Scenery.Wood, Level2Scenery.Wood);
            m.Roof(new Vector3(-10.2f, 2.2f, 21f), new Vector3(2.6f, 0f, 3.6f), 0.7f, Level2Scenery.RoofBlue, Level2Scenery.Wood);
            Level2Scenery.Flowers(m, new Vector3(-10f, 0f, 9f), new Vector2(1.6f, 2.5f), 14, 3);

            // The windmill on its hill to the right.
            Level2Scenery.Hill(m, new Vector3(22f, -3f, 40f), new Vector3(16f, 7f, 14f), Level2Scenery.LeafWarm);
            m.Post(new Vector3(20f, 3.2f, 38f), 2.0f, 9f, 10, Level2Scenery.Wall, Level2Scenery.Wall, 1.3f);
            m.Post(new Vector3(20f, 12.2f, 38f), 1.5f, 1.8f, 10, Level2Scenery.RoofRed, Level2Scenery.RoofRed, 0f);
            m.Build(transform, "Dock", kit.Solid);

            var blades = new SolidMesh();
            for (int k = 0; k < 4; k++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, k * 90f);
                blades.Block(turn * new Vector3(0f, 3.4f, 0f), new Vector3(0.18f, 6.6f, 0.1f), turn, Level2Scenery.Wood);
                blades.Block(turn * new Vector3(0.55f, 3.8f, 0f), new Vector3(0.95f, 5.0f, 0.05f), turn, Level2Scenery.Wall);
            }

            blades.Ball(Vector3.zero, Vector3.one * 0.5f, Level2Scenery.RoofRed, 6, 4);
            _blades = blades.Build(transform, "Blades", kit.Solid).transform;
            _blades.localPosition = new Vector3(20f, 11f, 35.6f);
            _blades.localRotation = Quaternion.Euler(0f, -25f, 0f);
        }

        private void Update()
        {
            if (_blades != null) _blades.localRotation = Quaternion.Euler(0f, -25f, Time.time * 24f);
        }
    }
}
