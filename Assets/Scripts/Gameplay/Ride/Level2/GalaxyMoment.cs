using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// Stop 2, meaning: the word galaxy. GPT-2 gives every token a point in a space of 768 numbers; the bake squashed the
    /// points of forty words down to three numbers (a flat picture of related words clustering) and this game shows that picture
    /// around the rider. Four neighbourhoods (animals, food, vehicles, feelings) glow with three of their words already in place; two more
    /// of each float loose in front. The rider points at a loose word, holds the trigger, and carries it to where it belongs by
    /// aiming at that neighbourhood, then lets go: it settles into its own real place in the cluster, or bounces back. Gameplay
    /// only: the view draws the stars, the clouds and the words; the positions come from the bake.
    /// </summary>
    public sealed class GalaxyMoment : Level2Game
    {
        /// <summary>A word of the galaxy.</summary>
        public sealed class Item
        {
            public string Word;
            public string Category;
            public bool Seed;
            public bool Placed;
            public Vector3 Home;
            public Vector3 Float;
            public Vector3 Position;
            public int Carrier = -1;
            public bool Rejected;
            public float RejectedAt = -10f;
        }

        /// <summary>One neighbourhood.</summary>
        public sealed class Cluster
        {
            public string Category;
            public Vector3 Centre;
            public int Needed;
            public int Placed;
            public bool Complete;
        }

        /// <summary>
        /// The picture is turned to show the four neighbourhoods apart (searched against the bake: the nearest two neighbourhoods
        /// are about 28 degrees apart seen from the pod, and the whole picture fits within 40 degrees either side of the gaze). A turn only;
        /// no point moves relative to another.
        /// </summary>
        public static readonly Vector3 ViewEuler = new Vector3(20f, 300f, -40f);

        public static readonly Vector3 GalaxyCentre = new Vector3(0f, 2.4f, 10f);
        public static readonly Vector3 MapScale = new Vector3(8.4f, 4.32f, 5.04f);

        /// <summary>Where a baked point sits in the stage (the pod's seat is the origin, +z ahead).</summary>
        public static Vector3 Map(Vector3 data) => GalaxyCentre + Vector3.Scale(Quaternion.Euler(ViewEuler) * data, MapScale);

        [SerializeField] private Level2DataSource source;
        [SerializeField] private Transform stage;
        [Tooltip("How far out a carried word rides on the laser, metres from the hand.")]
        [SerializeField] private float carryDistance = 7f;
        [Tooltip("A word let go within this many degrees of a neighbourhood's centre is dropped into it.")]
        [SerializeField] private float coneDegrees = 11f;
        [SerializeField] private int placePoints = 20;
        [SerializeField] private int wrongPoints = -5;
        [SerializeField] private float hintAfter = 7f;

        private readonly List<Item> _items = new List<Item>();
        private readonly List<Cluster> _clusters = new List<Cluster>();
        private readonly int[] _carrying = { -1, -1 };
        private bool _hurry;
        private bool _helped;
        private bool _prepared;
        private float _lastAction;
        private int _streak;
        private int _mistakes;
        private bool _saidWrong;
        private float _nextAuto;

        public IReadOnlyList<Item> Items => _items;
        public IReadOnlyList<Cluster> Clusters => _clusters;
        public bool IsLive { get; private set; }
        public Transform Stage => stage;
        public int Mistakes => _mistakes;
        public int Streak => _streak;
        public int LooseTotal { get; private set; }
        public int LoosePlaced { get; private set; }

        /// <summary>The loose word the game would help with next (the view pulses it and its neighbourhood), or -1.</summary>
        public int HintItem
        {
            get
            {
                if (!IsLive || Time.time - _lastAction < hintAfter) return -1;
                for (int i = 0; i < _items.Count; i++) if (!_items[i].Seed && !_items[i].Placed && _items[i].Carrier < 0) return i;
                return -1;
            }
        }

        public event Action<int, int> Picked;
        public event Action<int> Released;
        public event Action<int, int> Placed;
        public event Action<int, int> RejectedBy;
        public event Action<int> ClusterCompleted;

        /// <summary>Lays out the galaxy from the bake (once): the views build their stars from it before the game opens.</summary>
        public void Prepare()
        {
            if (_prepared || source == null) return;
            _prepared = true;
            Level2Data data = source.Data;
            _items.Clear();
            _clusters.Clear();
            int loose = 0;
            for (int c = 0; c < Level2Data.Neighbourhoods.Length; c++)
            {
                string category = Level2Data.Neighbourhoods[c];
                L2Centroid centre = data.Centre(category);
                var cluster = new Cluster { Category = category, Centre = Map(new Vector3(centre.x, centre.y, centre.z)), Needed = Level2Data.LoosePerNeighbourhood };
                _clusters.Add(cluster);
                List<L2GalaxyWord> words = data.Neighbourhood(category);
                for (int w = 0; w < Level2Data.SeedsPerNeighbourhood + Level2Data.LoosePerNeighbourhood; w++)
                {
                    L2GalaxyWord g = words[w];
                    var item = new Item
                    {
                        Word = g.word,
                        Category = category,
                        Seed = w < Level2Data.SeedsPerNeighbourhood,
                        Home = Map(new Vector3(g.x, g.y, g.z)),
                    };
                    item.Placed = item.Seed;
                    if (item.Seed) cluster.Placed++;
                    _items.Add(item);
                    if (!item.Seed) loose++;
                }
            }

            LooseTotal = loose;

            // The loose words float in front of the rider, spread over the arc, in an order that mixes the neighbourhoods.
            var looseItems = new List<Item>();
            foreach (Item item in _items) if (!item.Seed) looseItems.Add(item);
            looseItems.Sort((a, b) => string.CompareOrdinal(a.Word, b.Word));
            for (int i = 0; i < looseItems.Count; i++)
            {
                float u = looseItems.Count == 1 ? 0.5f : i / (float)(looseItems.Count - 1);
                float az = Mathf.Lerp(-34f, 34f, u) * Mathf.Deg2Rad;
                float el = (6f + 9f * Mathf.Sin(i * 2.3f)) * Mathf.Deg2Rad;
                float r = 6.2f + 0.7f * Mathf.Cos(i * 1.7f);
                looseItems[i].Float = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)) * r + new Vector3(0f, 1.3f, 0f);
            }

            foreach (Item item in _items) item.Position = item.Seed ? item.Home : item.Float;
        }

        public override void Hurry()
        {
            _hurry = true;
            _helped = true;
        }

        protected override int StarsEarned() => _mistakes == 0 ? 3 : _mistakes <= 2 ? 2 : 1;

        protected override bool PlayedItself => _helped;

        private void OnEnable() => RideAim.Released += OnReleased;

        private void OnDisable()
        {
            RideAim.Released -= OnReleased;
            _carrying[0] = _carrying[1] = -1;
        }

        private static int Side(int hand) => hand == RideHand.Left ? 0 : 1;

        // ---------- the picks ----------

        public override bool PickHit(int index, int hand)
        {
            if (!IsLive || index < 0 || index >= _items.Count) return false;
            Item item = _items[index];
            if (item.Seed || item.Placed || item.Carrier >= 0) return false;
            int side = Side(hand);
            if (_carrying[side] >= 0) Drop(side);
            _carrying[side] = index;
            item.Carrier = side;
            _lastAction = Time.time;
            Picked?.Invoke(index, side);
            RideSounds.Play("tick", 1.4f);
            return true;
        }

        private void OnReleased(int hand)
        {
            if (hand != RideHand.Left && hand != RideHand.Right) return;
            Drop(Side(hand));
        }

        private void Drop(int side)
        {
            int index = _carrying[side];
            if (index < 0) return;
            _carrying[side] = -1;
            Item item = _items[index];
            item.Carrier = -1;
            _lastAction = Time.time;
            int hand = side == 0 ? RideHand.Left : RideHand.Right;
            int cluster = TryRay(hand, out Ray ray) ? ClusterUnder(ray) : -1;
            if (cluster < 0)
            {
                Released?.Invoke(index);
                return;
            }

            if (_clusters[cluster].Category == item.Category) Place(index, cluster, hand);
            else Reject(index, cluster);
        }

        /// <summary>The neighbourhood a laser points at (the nearest centre within the cone), or -1.</summary>
        public int ClusterUnder(Ray worldRay)
        {
            if (stage == null) return -1;
            Vector3 o = stage.InverseTransformPoint(worldRay.origin);
            Vector3 d = stage.InverseTransformDirection(worldRay.direction);
            int best = -1;
            float bestAngle = coneDegrees;
            for (int c = 0; c < _clusters.Count; c++)
            {
                float angle = Vector3.Angle(d, _clusters[c].Centre - o);
                if (angle >= bestAngle) continue;
                bestAngle = angle;
                best = c;
            }

            return best;
        }

        private void Place(int index, int cluster, int hand)
        {
            Item item = _items[index];
            item.Placed = true;
            LoosePlaced++;
            Cluster c = _clusters[cluster];
            c.Placed++;
            _streak++;
            int points = placePoints + 10 * Mathf.Min(_streak - 1, 3);
            AddScore(points, stage != null ? stage.TransformPoint(item.Home) : item.Home);
            Placed?.Invoke(index, cluster);
            RideSounds.Play("plugIn", 1f);
            RideAim.Buzz(hand, 0.7f, 0.1f);
            if (!c.Complete && c.Placed >= Level2Data.SeedsPerNeighbourhood + c.Needed)
            {
                c.Complete = true;
                ClusterCompleted?.Invoke(cluster);
                RideSounds.Play("solved", 1f);
            }
        }

        private void Reject(int index, int cluster)
        {
            Item item = _items[index];
            item.Rejected = true;
            item.RejectedAt = Time.time;
            _mistakes++;
            _streak = 0;
            AddScore(wrongPoints, stage != null ? stage.TransformPoint(item.Float) : item.Float);
            RejectedBy?.Invoke(index, cluster);
            RideSounds.Play("wrong", 1f);
            if (!_saidWrong && Director != null)
            {
                _saidWrong = true;
                Director.Say(RideLine.EmbWrong);
            }
        }

        // ---------- the game ----------

        private void Update()
        {
            if (!IsLive) return;
            for (int side = 0; side < 2; side++)
            {
                int i = _carrying[side];
                if (i < 0) continue;
                int hand = side == 0 ? RideHand.Left : RideHand.Right;
                if (!TryRay(hand, out Ray ray) || stage == null) continue;
                _items[i].Position = stage.InverseTransformPoint(ray.origin + ray.direction * carryDistance);
            }
        }

        /// <summary>The word's own place for the view to settle it into, once placed.</summary>
        public int IndexOf(string word) => _items.FindIndex(i => i.Word == word);

        protected override IEnumerator Play()
        {
            Prepare();
            _hurry = false;
            _helped = false;
            _mistakes = 0;
            _streak = 0;
            _saidWrong = false;
            LoosePlaced = 0;
            foreach (Item item in _items)
            {
                item.Placed = item.Seed;
                item.Carrier = -1;
                item.Position = item.Seed ? item.Home : item.Float;
            }

            foreach (Cluster c in _clusters)
            {
                c.Placed = Level2Data.SeedsPerNeighbourhood;
                c.Complete = false;
            }

            _carrying[0] = _carrying[1] = -1;
            _lastAction = Time.time;
            IsLive = true;
            float opened = Time.time;
            while (LoosePlaced < LooseTotal)
            {
                Progress = LooseTotal == 0 ? 0f : LoosePlaced / (float)LooseTotal;
                SetGoal("Carry each loose word home:  " + LoosePlaced + " of " + LooseTotal);
                if (!_hurry && Time.time - opened > giveUpAfter)
                {
                    _hurry = true;
                    _helped = true;
                }

                if (_hurry && Time.time >= _nextAuto)
                {
                    _nextAuto = Time.time + 0.7f;
                    for (int i = 0; i < _items.Count; i++)
                    {
                        Item item = _items[i];
                        if (item.Seed || item.Placed) continue;
                        int cluster = _clusters.FindIndex(c => c.Category == item.Category);
                        if (item.Carrier >= 0) _carrying[item.Carrier] = -1;
                        item.Carrier = -1;
                        Place(i, cluster, -1);
                        break;
                    }
                }

                yield return null;
            }

            IsLive = false;
            yield return new WaitForSeconds(0.8f);
        }
    }
}
