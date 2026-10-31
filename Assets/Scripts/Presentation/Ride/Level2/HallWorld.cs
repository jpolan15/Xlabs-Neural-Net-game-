using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 3's place (ADR-023): an open-air theatre at golden hour, for attention. The bike stops in the audience; the stage's dark curtain
    /// hangs right behind the words (so they read crisp against it), red side curtains and a valance with the theatre's name frame it, and
    /// two spotlight towers swing their lamps across the stage, the way the rider's laser picks the words that matter. When the bike rolls
    /// on, the curtain flies up and the bike rides through the stage's aisle. Dressing only.
    /// </summary>
    public sealed class HallWorld : MonoBehaviour
    {
        private Transform[] _lamps;
        private Transform _curtain;
        private float _lift;
        private Camera _camera;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();

            // The stage: a low wooden floor under the words, the dark curtain behind them in folds, side curtains, the valance.
            Color backdrop = new Color(0.2f, 0.1f, 0.2f, 0f), red = new Color(0.8f, 0.16f, 0.24f, 0f), gold = new Color(0.95f, 0.75f, 0.3f, 0f);
            // The stage floor in two halves: the bike's road runs up the aisle between them.
            foreach (float s in new[] { -1f, 1f }) m.Box(new Vector3(s * 6.3f, 0f, 11f), new Vector3(8.6f, 0.45f, 9f), Level2Scenery.Wood, Fx.Mul(Level2Scenery.Wood, 0.8f));

            foreach (float s in new[] { -1f, 1f })
            {
                m.Block(new Vector3(s * 10.6f, 5.1f, 15.2f), new Vector3(1.6f, 10.2f, 0.4f), Quaternion.Euler(0f, s * 12f, 0f), red);
                m.Block(new Vector3(s * 11.6f, 5.3f, 15.6f), new Vector3(0.6f, 10.6f, 0.6f), Quaternion.identity, gold);
            }

            // The valance sits high, above the explainer's title.
            m.Block(new Vector3(0f, 10.6f, 15.1f), new Vector3(23.4f, 1.3f, 0.4f), Quaternion.identity, red);
            Level2Scenery.Words(transform, "Theatre", "THE ATTENTION THEATRE", 0.62f, new Color(1f, 0.88f, 0.55f), new Vector3(0f, 10.6f, 14.85f), 12f);

            // The audience: low rows of red seats on both sides of the aisle the bike rides up.
            for (int row = 0; row < 3; row++)
            {
                foreach (float s in new[] { -1f, 1f })
                {
                    for (int k = 0; k < 4; k++)
                    {
                        Vector3 p = new Vector3(s * (3.0f + k * 0.9f), 0f, 4.8f - row * 0.9f); // low and near: never across the game's bottom line
                        m.Box(p, new Vector3(0.75f, 0.36f, 0.6f), red, Fx.Mul(red, 0.8f));
                        m.Box(p + new Vector3(0f, 0f, 0.3f), new Vector3(0.75f, 0.6f, 0.1f), red, Fx.Mul(red, 0.75f));
                    }
                }
            }

            // The two spotlight towers.
            Color steel = new Color(0.3f, 0.33f, 0.4f, 0f);
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 b = new Vector3(s * 13.5f, 0f, 9f);
                foreach (Vector2 leg in new[] { new Vector2(-0.6f, -0.6f), new Vector2(0.6f, -0.6f), new Vector2(-0.6f, 0.6f), new Vector2(0.6f, 0.6f) })
                {
                    m.Post(b + new Vector3(leg.x, 0f, leg.y), 0.08f, 8f, 4, steel, steel);
                }

                for (int k = 1; k < 5; k++) m.Box(b + Vector3.up * (k * 1.8f), new Vector3(1.4f, 0.08f, 1.4f), steel, steel);
                m.Box(b + Vector3.up * 8f, new Vector3(1.8f, 0.2f, 1.8f), steel, steel);
            }

            m.Build(transform, "Theatre", kit.Solid);

            // The curtain, in folds, on its own so it can fly up.
            var curtain = new SolidMesh();
            for (int i = 0; i < 20; i++)
            {
                float x = -9.5f + i;
                curtain.Block(new Vector3(x, 5.1f, 15.6f + (i % 2) * 0.12f), new Vector3(1.02f, 10.2f, 0.2f), Quaternion.identity, Fx.Mul(backdrop, i % 2 == 0 ? 1f : 0.82f));
            }

            _curtain = curtain.Build(transform, "Curtain", kit.Solid).transform;

            _lamps = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var lamp = new SolidMesh();
                lamp.Block(new Vector3(0f, 0f, 0.4f), new Vector3(0.7f, 0.7f, 1.1f), Quaternion.identity, steel);
                lamp.Ball(new Vector3(0f, 0f, 0.98f), new Vector3(0.32f, 0.32f, 0.08f), new Color(1f, 0.95f, 0.75f, 1f), 8, 4);
                _lamps[i] = lamp.Build(transform, "Spot" + i, kit.Solid).transform;
                _lamps[i].localPosition = new Vector3((i == 0 ? -1f : 1f) * 13.5f, 8.6f, 9f);
            }
        }

        private void Update()
        {
            if (_lamps == null) return;

            // The bike has left its seat in the audience: the curtain flies up out of its way (and comes down if the ride restarts).
            if (_camera == null) _camera = Camera.main;
            float eyeZ = _camera != null ? transform.InverseTransformPoint(_camera.transform.position).z : 0f;
            _lift = Mathf.MoveTowards(_lift, eyeZ > 1.2f ? 1f : 0f, Time.deltaTime * 0.6f);
            _curtain.localPosition = Vector3.up * 11f * Fx.Smooth(_lift);

            for (int i = 0; i < 2; i++)
            {
                Vector3 aim = new Vector3(Mathf.Sin(Time.time * 0.5f + i * 2f) * 5f, 2.6f, 10f + Mathf.Cos(Time.time * 0.4f + i) * 2f);
                _lamps[i].localRotation = Quaternion.LookRotation(aim - _lamps[i].localPosition, Vector3.up);
            }
        }
    }
}
