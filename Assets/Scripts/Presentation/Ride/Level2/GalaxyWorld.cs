using UnityEngine;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 2's place (ADR-023): the observatory on the hill, where the word galaxy hangs in the open air. A white dome with its slot open
    /// stands far ahead behind the galaxy, a telescope on a little platform points at the words, and hot-air balloons drift on both sides,
    /// high up, so the space around the rider stays clear for the forty words. Dressing only.
    /// </summary>
    public sealed class GalaxyWorld : MonoBehaviour
    {
        private Transform[] _balloons;
        private Vector3[] _balloonHome;

        private void Awake()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();

            // The observatory on its hill, ahead and off to the right of the road (the road runs past it, never through it).
            Vector3 o = new Vector3(34f, 0f, 80f);
            Level2Scenery.Hill(m, o + new Vector3(0f, -6f, 0f), new Vector3(20f, 12f, 16f), Level2Scenery.Leaf);
            m.Post(o + new Vector3(0f, 4.5f, 0f), 6.2f, 4.5f, 16, Level2Scenery.Wall, Level2Scenery.Wall);
            m.Ball(o + new Vector3(0f, 9f, 0f), new Vector3(6.6f, 6.0f, 6.6f), new Color(0.94f, 0.95f, 0.98f, 0f), 16, 8, 0.5f);
            m.Block(o + new Vector3(-2.5f, 12.2f, -4.6f), new Vector3(1.6f, 5.5f, 0.3f), Quaternion.Euler(-35f, -30f, 0f), new Color(0.2f, 0.24f, 0.36f, 0f));
            Level2Scenery.Words(transform, "Observatory", "WORD OBSERVATORY", 0.7f, new Color(0.3f, 0.36f, 0.7f), o + new Vector3(0f, 6.6f, -6.4f), 12f);

            // The telescope on its platform at the side of the road.
            m.Post(new Vector3(-11.5f, 0f, 14f), 1.6f, 0.6f, 10, Level2Scenery.Stone, Level2Scenery.Stone);
            m.Post(new Vector3(-11.5f, 0.6f, 14f), 0.14f, 1.6f, 6, new Color(0.3f, 0.32f, 0.38f, 0f), Level2Scenery.Stone);
            m.Block(new Vector3(-11.2f, 2.5f, 14.4f), new Vector3(0.42f, 0.42f, 2.6f), Quaternion.Euler(-30f, 30f, 0f), new Color(0.36f, 0.42f, 0.86f, 0f));
            m.Build(transform, "Observatory", kit.Solid);

            // Hot-air balloons, high on both sides: a striped envelope and a basket.
            _balloons = new Transform[4];
            _balloonHome = new Vector3[4];
            Vector3[] at = { new Vector3(-42f, 24f, 22f), new Vector3(46f, 27f, 34f), new Vector3(-58f, 28f, 52f), new Vector3(60f, 22f, 48f) }; // outside the galaxy's cone
            for (int i = 0; i < 4; i++)
            {
                var b = new SolidMesh();
                Color a = Level2Scenery.Bright[i % 6], c = Level2Scenery.Bright[(i + 3) % 6];
                for (int s = 0; s < 4; s++)
                {
                    float r = 3.2f - Mathf.Abs(s - 1.5f) * 0.5f;
                    b.Post(new Vector3(0f, s * 1.3f, 0f), r, 1.3f, 12, s % 2 == 0 ? a : c, a, 3.2f - Mathf.Abs(s + 1 - 1.5f) * 0.5f);
                }

                b.Ball(new Vector3(0f, 5.2f, 0f), new Vector3(2.45f, 1.6f, 2.45f), a, 12, 5, 0.5f);
                b.Post(new Vector3(0f, -1.6f, 0f), 1.4f, 1.6f, 8, a, a, 2.3f);
                b.Box(new Vector3(0f, -3.2f, 0f), new Vector3(1.1f, 0.9f, 1.1f), Level2Scenery.Wood, Level2Scenery.Wood);
                _balloons[i] = b.Build(transform, "Balloon" + i, kit.Solid).transform;
                _balloonHome[i] = at[i];
                _balloons[i].localPosition = at[i];
            }
        }

        private void Update()
        {
            if (_balloons == null) return;
            for (int i = 0; i < _balloons.Length; i++)
            {
                _balloons[i].localPosition = _balloonHome[i] + new Vector3(Mathf.Sin(Time.time * 0.11f + i) * 1.5f, Mathf.Sin(Time.time * 0.23f + i * 2f) * 0.8f, 0f);
            }
        }
    }
}
