using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 4's place (ADR-023): the odds fair in the late afternoon, for prediction. Striped tents line both sides of the road, a big
    /// wheel turns behind them on the right, and a string of pennants crosses over the way. Dressing only.
    /// </summary>
    public sealed class OddsWorld : MonoBehaviour
    {
        private Transform _wheel;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();
            Color white = new Color(0.98f, 0.97f, 0.94f, 0f);

            // Tents, left and right, set back from the bets.
            Vector3[] tents = { new Vector3(-13f, 0f, 12f), new Vector3(-16f, 0f, 22f), new Vector3(-12.5f, 0f, 31f), new Vector3(13.5f, 0f, 13f), new Vector3(12.5f, 0f, 26f) };
            for (int i = 0; i < tents.Length; i++) Level2Scenery.Tent(m, tents[i], 2.4f + 0.5f * Fx.Rand01(i + 40), Level2Scenery.Bright[i % 6], white);

            // The big wheel's stand, on the right behind the tents.
            Vector3 hub = new Vector3(44f, 12f, 56f); // far right, behind the tents, clear of the bets
            Color steel = new Color(0.92f, 0.92f, 0.96f, 0f);
            foreach (float s in new[] { -1f, 1f })
            {
                // Two legs from the hub down to the ground, 5 m out on either side, in front of and behind the wheel.
                m.Block(hub + new Vector3(-2.5f, -6f, s * 1.2f), new Vector3(0.35f, 13f, 0.35f), Quaternion.Euler(0f, 0f, 22.6f), steel);
                m.Block(hub + new Vector3(2.5f, -6f, s * 1.2f), new Vector3(0.35f, 13f, 0.35f), Quaternion.Euler(0f, 0f, -22.6f), steel);
            }

            // Strings of pennants between the tents on both sides (never behind the bets).
            foreach (float s in new[] { -1f, 1f })
            {
                for (int i = 0; i < 14; i++)
                {
                    float z = 10f + i * 1.3f;
                    float y = 5.2f - 0.7f * Mathf.Sin(i / 13f * Mathf.PI);
                    m.Block(new Vector3(s * 10.5f, y, z), new Vector3(0.03f, 0.6f, 0.5f), Quaternion.Euler(45f, 0f, 0f), Level2Scenery.Bright[i % 6]);
                }

                foreach (float z in new[] { 9.4f, 27.6f }) m.Post(new Vector3(s * 10.5f, 0f, z), 0.1f, 5.6f, 6, white, white);
            }
            Level2Scenery.Sign(m, transform, new Vector3(12.5f, 0f, 16f), new Vector2(3.4f, 1.1f), "THE ODDS FAIR", new Color(1f, 0.85f, 0.35f, 0f), new Color(0.6f, 0.15f, 0.3f));
            m.Build(transform, "Fair", kit.Solid);

            // The wheel itself: a rim of spokes and twelve gondolas, turning slowly.
            var w = new SolidMesh();
            for (int k = 0; k < 12; k++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, k * 30f);
                w.Block(turn * new Vector3(0f, 4.5f, 0f), new Vector3(0.18f, 9f, 0.18f), turn, steel);
                w.Block(turn * new Vector3(0f, 9f, 0f), new Vector3(4.8f, 0.3f, 0.3f), turn, steel);
                w.Box(turn * new Vector3(0f, 9f, 0f) + Vector3.down * 1.2f, new Vector3(1.2f, 1.1f, 1.2f), Level2Scenery.Bright[k % 6], Level2Scenery.Bright[k % 6]);
            }

            w.Ball(Vector3.zero, Vector3.one * 0.8f, steel, 8, 4);
            _wheel = w.Build(transform, "BigWheel", kit.Solid).transform;
            _wheel.localPosition = hub;
            _wheel.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (_wheel != null) _wheel.localRotation = Quaternion.Euler(0f, 0f, Time.time * 6f);
        }
    }
}
