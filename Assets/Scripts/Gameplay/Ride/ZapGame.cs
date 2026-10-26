using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// The game between the stops: while the pod travels, specks of noise drift past it and the rider zaps them with the
    /// laser for points. It never blocks the ride and never touches a puzzle. The specks are a small pool of copies of
    /// one template (built by the scene builder) riding with the pod, so they stay in reach as it moves.
    /// </summary>
    public sealed class ZapGame : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private ZapSpeck template;
        [SerializeField] private int poolSize = 8;
        [SerializeField] private float spawnEvery = 0.9f;
        [SerializeField] private float lifeSeconds = 7f;
        [SerializeField] private int points = 10;
        [Tooltip("Where specks appear, relative to the pod: ahead, to the sides, above the floor.")]
        [SerializeField] private Vector2 ahead = new Vector2(3.5f, 7f);
        [SerializeField] private float side = 3f;
        [SerializeField] private Vector2 height = new Vector2(0.9f, 2.6f);

        private ZapSpeck[] _pool;
        private float _nextSpawn;
        private bool _saidIntro;
        private System.Random _random = new System.Random(7);

        /// <summary>The ride's zapped count.</summary>
        public int Zapped { get; private set; }

        /// <summary>A speck was zapped, where.</summary>
        public event Action<Vector3> SpeckZapped;

        /// <summary>A speck appeared, where.</summary>
        public event Action<Vector3> SpeckSpawned;

        private void Start()
        {
            if (template == null || director == null)
            {
                enabled = false;
                return;
            }

            template.gameObject.SetActive(false);
            _pool = new ZapSpeck[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                _pool[i] = Instantiate(template, director.Pod);
                _pool[i].name = "Speck" + i;
                _pool[i].Init(this);
                _pool[i].gameObject.SetActive(false);
            }
        }

        private bool Running => director.State == RideState.Traveling && director.ActiveMoment == null && director.SegmentIndex >= 0;

        private void Update()
        {
            if (_pool == null) return;
            if (!Running)
            {
                _nextSpawn = Time.time + 1.2f;
                return;
            }

            if (Time.time < _nextSpawn) return;
            _nextSpawn = Time.time + spawnEvery;
            foreach (ZapSpeck speck in _pool)
            {
                if (speck.gameObject.activeSelf) continue;
                Vector3 local = new Vector3(Range(-side, side), Range(height.x, height.y), Range(ahead.x, ahead.y));
                Vector3 drift = new Vector3(Range(-0.4f, 0.4f), Range(-0.15f, 0.15f), Range(-0.9f, -0.3f));
                speck.Launch(local, drift, lifeSeconds);
                SpeckSpawned?.Invoke(speck.transform.position);
                if (!_saidIntro)
                {
                    _saidIntro = true;
                    director.Say(RideLine.ZapIntro);
                }

                break;
            }
        }

        /// <summary>Called by a speck the laser hit.</summary>
        internal void OnZapped(ZapSpeck speck)
        {
            Zapped++;
            director.AddScore(points);
            SpeckZapped?.Invoke(speck.transform.position);
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);
    }
}
