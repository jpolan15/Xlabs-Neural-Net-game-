using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>
    /// Stop 5, writing: the story road. "My robot loves to ..." The road forks into four lanes, each a word GPT-2 could write next,
    /// and its real chance of writing it (the bake walked the model along every road). The rider shoots the gate of the word they
    /// want and the bike rides down it; the machine runs again on the longer sentence. Three forks make a story. A lane's score is how
    /// surprising it was (minus log2 of its probability, times ten): the safe word is easy and worth little, a wild word is a long shot
    /// and worth a lot. At the end the rider is told how often the machine would have written exactly that story on its own, which is
    /// the product of the odds they steered through. Gameplay only: the pod travels on the director's leg, and the view slides the
    /// scenery sideways to steer. v3 (owner playtest 2026-11-10: "super short", and unclear that the rider stands in for the dice):
    /// after each pick the ride holds for compareSeconds while the view shows the machine's own favourite at that fork and where the
    /// other lanes would have led, and the end shows the machine's favourite story and two others it could have written.
    /// </summary>
    public sealed class StoryMoment : Level2Game
    {
        public enum Phase { Waiting, Fork, Compare, Travelling, Reveal }

        [SerializeField] private Level2DataSource source;
        [SerializeField] private Level2Session session;
        [SerializeField] private float forkSeconds = 28f;
        [SerializeField] private float forkSpacing = 16f;
        [SerializeField] private float travelSeconds = 3.2f;
        [SerializeField] private float revealSeconds = 12f;
        [Tooltip("After a pick: how long the fork stays to compare the rider's word with the machine's favourite (lane 0).")]
        [SerializeField] private float compareSeconds = 3f;
        [SerializeField] private float surprisePoints = 10f;

        private int _chosen = -1;
        private int _chosenHand = RideHand.Right;
        private bool _hurry;
        private bool _helped;
        private bool _saidWild;
        private bool _saidSafe;
        private float _forkStarted;
        private readonly List<string> _words = new List<string>();
        private double _bits;
        private double _probability;

        public Phase Stage { get; private set; }
        public int Fork { get; private set; }
        public int ForkCount => source != null ? source.Data.File.story.forks : 3;
        public string Path { get; private set; } = "";
        public L2StoryNode Node { get; private set; }
        public string Start => source.Data.File.story.start;
        public string SoFar => _words.Count == 0 ? Start : Start + " " + string.Join(" ", _words);
        /// <summary>
        /// The story as told: a full stop after a word that can end a sentence, "..." after one that cannot ("talk like a ..."), because
        /// three words are where the ride stops, not where the machine would; it would keep going.
        /// </summary>
        public string FinishedText => SoFar + (EndsOpen(_words.Count > 0 ? _words[_words.Count - 1] : "") ? " ..." : ".");

        private static readonly HashSet<string> OpenEnds = new HashSet<string>
        {
            "a", "an", "the", "to", "with", "of", "my", "its", "his", "her", "their", "your", "our", "and", "or", "but", "in", "on", "at",
            "for", "from", "like", "about", "into", "by", "as", "than", "that", "be", "is", "are", "was", "very", "so", "too",
        };

        public static bool EndsOpen(string word) => OpenEnds.Contains(word.ToLowerInvariant());

        /// <summary>The fork reached along a path of lanes, or null past the last fork.</summary>
        public L2StoryNode NodeAt(string path) => source.Data.StoryNode(path);

        /// <summary>The machine's favourite lane at every fork: lane 0 holds the word it gave the best odds.</summary>
        public string FavouritePath => new string('0', ForkCount);

        /// <summary>The story told along a path of lanes (a digit per fork), ended the way FinishedText ends one; null if the path leaves the road.</summary>
        public string Tell(string path)
        {
            var words = new List<string>();
            for (int i = 0; i < path.Length; i++)
            {
                L2StoryNode node = source.Data.StoryNode(path.Substring(0, i));
                int lane = path[i] - '0';
                if (node == null || lane < 0 || lane >= node.lanes.Length) return null;
                words.Add(Level2Data.Clean(node.lanes[lane].word));
            }

            return Start + (words.Count > 0 ? " " + string.Join(" ", words) : "") + (EndsOpen(words.Count > 0 ? words[words.Count - 1] : "") ? " ..." : ".");
        }

        /// <summary>The chance the machine gives to writing exactly the story along a path (the product of its lanes' odds).</summary>
        public double ChanceOf(string path)
        {
            double p = 1.0;
            for (int i = 0; i < path.Length; i++) p *= source.Data.StoryNode(path.Substring(0, i)).lanes[path[i] - '0'].p;
            return p;
        }
        public double Bits => _bits;
        public double Probability => _probability;
        public int LastLane { get; private set; } = -1;
        public float LastP { get; private set; }
        public bool IsLive => Stage == Phase.Fork;
        public override float SecondsLeft => Stage == Phase.Fork ? Mathf.Max(0f, forkSeconds - (Time.time - _forkStarted)) : -1f;
        public float ForkSpacing => forkSpacing;

        /// <summary>A fork opened: its fork number and its node (the four lanes).</summary>
        public event Action<int, L2StoryNode> ForkOpened;

        /// <summary>A lane was chosen: the fork, the lane, its word, its chance, the points it was worth.</summary>
        public event Action<int, int, string, float, int> Chosen;

        /// <summary>The story is told: its text and how likely the machine was to write it.</summary>
        public event Action<string, double, double> Told;

        public override void Hurry()
        {
            _hurry = true;
            _helped = true;
        }

        protected override int StarsEarned() => _bits >= 15.0 ? 3 : _bits >= 10.0 ? 2 : 1;

        protected override bool PlayedItself => _helped;

        public override bool PickHit(int index, int hand)
        {
            if (Stage != Phase.Fork || index < 0 || index >= 4 || _chosen >= 0) return false;
            _chosen = index;
            _chosenHand = hand;
            return true;
        }

        protected override IEnumerator Play()
        {
            Level2Data data = source.Data;
            _words.Clear();
            Path = "";
            _bits = 0;
            _probability = 1;
            _hurry = false;
            _helped = false;
            _saidWild = _saidSafe = false;
            int forks = data.File.story.forks;
            for (Fork = 0; Fork < forks; Fork++)
            {
                Node = data.StoryNode(Path);
                _chosen = -1;
                _forkStarted = Time.time;
                Stage = Phase.Fork;
                ForkOpened?.Invoke(Fork, Node);
                SetGoal("Guess the machine's favourite next word  (" + (Fork + 1) + " of " + forks + ")");
                Progress = Fork / (float)forks;
                while (_chosen < 0)
                {
                    if (_hurry || Time.time - _forkStarted > forkSeconds)
                    {
                        _chosen = 0;
                        if (Time.time - _forkStarted > forkSeconds) _helped = true;
                    }

                    yield return null;
                }

                L2Choice lane = Node.lanes[_chosen];
                double bits = -Math.Log(lane.p, 2.0);
                _bits += bits;
                _probability *= lane.p;
                int points = Mathf.RoundToInt((float)bits * surprisePoints);
                _words.Add(Level2Data.Clean(lane.word));
                LastLane = _chosen;
                LastP = lane.p;
                AddScore(points, Vector3.zero);
                RideSounds.Play(lane.p < 0.03f ? "winShimmer" : "plugIn", 1f);
                RideAim.Buzz(_chosenHand, 0.7f, 0.1f);
                Chosen?.Invoke(Fork, _chosen, Level2Data.Clean(lane.word), lane.p, points);
                if (lane.p < 0.03f && !_saidWild && Director != null)
                {
                    _saidWild = true;
                    Director.Say(RideLine.GenWild);
                }
                else if (lane.p >= 0.2f && !_saidSafe && Director != null)
                {
                    _saidSafe = true;
                    Director.Say(RideLine.GenSafe);
                }

                Path += _chosen;
                // Hold at the fork: the view compares the pick with the machine's favourite and shows where the other lanes led.
                Stage = Phase.Compare;
                if (!_hurry) yield return new WaitForSeconds(compareSeconds);
                Stage = Phase.Travelling;
                yield return Director.TravelTo(Director.Distance + forkSpacing, _hurry ? 1.2f : travelSeconds);
            }

            Stage = Phase.Reveal;
            if (session != null) session.SetStory(FinishedText, _probability);
            Told?.Invoke(FinishedText, _bits, _probability);
            Progress = 1f;
            yield return new WaitForSeconds(_hurry ? 2f : revealSeconds);
            Stage = Phase.Waiting;
        }
    }
}
