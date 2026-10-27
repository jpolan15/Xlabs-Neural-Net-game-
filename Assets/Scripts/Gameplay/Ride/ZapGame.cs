using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// The game between the stops (v7, "blast the 3s"): while the pod travels, real handwritten digits fly at it and the
    /// rider blasts every 3 and spares the rest: the rider is the classifier Pixel will learn to be. A 3 scores, more
    /// with a streak; any other digit costs points and the streak; a 3 that gets away ends the streak. It never blocks
    /// the ride and never touches a puzzle. The targets are a small pool of copies of one template (built by the scene
    /// builder) riding with the pod, so they stay in reach as it moves; which picture each one shows is a cell of the
    /// builder's atlas of the bake's targets, whose labels the builder writes here.
    /// </summary>
    public sealed class ZapGame : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private ZapSpeck template;
        [SerializeField] private int poolSize = 10;
        [SerializeField] private float spawnEvery = 0.75f;
        [SerializeField] private float lifeSeconds = 6.5f;
        [SerializeField] private int points = 10;
        [SerializeField] private int wrongPoints = -10;

        [Tooltip("Each streak of this many 3s raises the multiplier by one, up to maxMultiplier.")]
        [SerializeField] private int streakPerStep = 3;
        [SerializeField] private int maxMultiplier = 4;

        [Tooltip("The digit to blast.")]
        [SerializeField] private int targetLabel = 3;

        [Tooltip("Share of targets that are the digit to blast.")]
        [Range(0f, 1f)] [SerializeField] private float targetShare = 0.45f;

        [Tooltip("The label of each picture the template can show (the builder's atlas cells, from the bake). Empty: every target is one to blast.")]
        [SerializeField] private int[] labels = new int[0];

        [Tooltip("Where targets appear, relative to the pod: ahead, to the sides, above the floor.")]
        [SerializeField] private Vector2 ahead = new Vector2(4.5f, 8f);
        [SerializeField] private float side = 2.6f;
        [SerializeField] private Vector2 height = new Vector2(1.0f, 2.4f);

        private ZapSpeck[] _pool;
        private float _nextSpawn;
        private bool _saidIntro;
        private bool _saidWrong;
        private System.Random _random = new System.Random(7);

        /// <summary>Every target hit (right or wrong).</summary>
        public int Zapped { get; private set; }

        /// <summary>3s blasted.</summary>
        public int Blasted { get; private set; }

        /// <summary>Other digits blasted.</summary>
        public int Mistakes { get; private set; }

        /// <summary>3s that flew past unblasted.</summary>
        public int Escaped { get; private set; }

        /// <summary>3s in a row (a mistake or an escaped 3 ends it).</summary>
        public int Streak { get; private set; }

        /// <summary>What the next 3 is worth, times the base points.</summary>
        public int Multiplier => Mathf.Min(maxMultiplier, 1 + Streak / Mathf.Max(1, streakPerStep));

        /// <summary>The multiplier the last blasted 3 scored with.</summary>
        public int LastMultiplier { get; private set; } = 1;

        public int TargetLabel => targetLabel;

        /// <summary>The pool of targets (empty before Start).</summary>
        public System.Collections.Generic.IReadOnlyList<ZapSpeck> Targets => _pool ?? System.Array.Empty<ZapSpeck>();

        /// <summary>True while targets fly (the pod travels between stops).</summary>
        public bool IsRunning => _pool != null && Running;

        /// <summary>A target was hit: where, whether it was a 3, the points it gave (negative for a mistake).</summary>
        public event Action<Vector3, bool, int> Shot;

        /// <summary>A 3 got away, from where.</summary>
        public event Action<Vector3> Missed;

        /// <summary>A target appeared, where.</summary>
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
                _pool[i].name = "Target" + i;
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
                foreach (ZapSpeck speck in _pool)
                {
                    if (speck.gameObject.activeSelf) speck.Retire(0.35f);
                }

                return;
            }

            if (Time.time < _nextSpawn) return;
            _nextSpawn = Time.time + spawnEvery;
            foreach (ZapSpeck speck in _pool)
            {
                if (speck.gameObject.activeSelf) continue;
                Vector3 local = new Vector3(Range(-side, side), Range(height.x, height.y), Range(ahead.x, ahead.y));
                Vector3 drift = new Vector3(Range(-0.3f, 0.3f), Range(-0.1f, 0.1f), Range(-1.1f, -0.5f));
                int picture = PickPicture(out int label);
                speck.Launch(local, drift, lifeSeconds, picture, label);
                SpeckSpawned?.Invoke(speck.transform.position);
                if (!_saidIntro)
                {
                    _saidIntro = true;
                    director.Say(RideLine.ZapIntro);
                }

                break;
            }
        }

        /// <summary>A picture: a 3 about targetShare of the time, otherwise another digit. -1 when there are no pictures.</summary>
        private int PickPicture(out int label)
        {
            label = -1;
            if (labels == null || labels.Length == 0) return -1;
            bool three = _random.NextDouble() < targetShare;
            int matches = 0;
            foreach (int l in labels) if ((l == targetLabel) == three) matches++;
            if (matches == 0) three = !three;
            int pick = _random.Next(matches == 0 ? labels.Length : matches);
            for (int i = 0; i < labels.Length; i++)
            {
                if ((labels[i] == targetLabel) != three) continue;
                if (pick-- == 0)
                {
                    label = labels[i];
                    return i;
                }
            }

            return -1;
        }

        /// <summary>True when this label is one to blast (a target with no picture always is).</summary>
        public bool IsTarget(int label) => label < 0 || label == targetLabel;

        /// <summary>Called by a target the laser hit.</summary>
        internal void OnShot(ZapSpeck speck, int hand)
        {
            Zapped++;
            bool right = IsTarget(speck.Label);
            int gained;
            if (right)
            {
                LastMultiplier = Multiplier;
                gained = points * LastMultiplier;
                Blasted++;
                Streak++;
            }
            else
            {
                gained = Mathf.Max(wrongPoints, -director.Score);
                Mistakes++;
                Streak = 0;
                RideAim.Buzz(hand, 0.9f, 0.2f);
                if (!_saidWrong)
                {
                    _saidWrong = true;
                    director.Say(RideLine.ZapWrong);
                }
            }

            director.AddScore(gained);
            Shot?.Invoke(speck.transform.position, right, gained);
        }

        /// <summary>Called by a target whose time ran out.</summary>
        internal void OnExpired(ZapSpeck speck)
        {
            if (speck.Label < 0 || speck.Label != targetLabel) return;
            Escaped++;
            Streak = 0;
            Missed?.Invoke(speck.transform.position);
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);
    }
}
