using System;
using System.Collections.Generic;
using Convergence.Core.Math;

namespace Convergence.Core.Neural
{
    /// <summary>"<see cref="From"/> is to <see cref="To"/> as <see cref="Query"/> is to <see cref="Answer"/>."</summary>
    public sealed class WordAnalogy
    {
        /// <summary>Tail of the example arrow, for example "man".</summary>
        public string From { get; }

        /// <summary>Tip of the example arrow, for example "woman".</summary>
        public string To { get; }

        /// <summary>Where the arrow is carried to, for example "king".</summary>
        public string Query { get; }

        /// <summary>The word the carried arrow should land on, for example "queen".</summary>
        public string Answer { get; }

        /// <summary>Creates an analogy. No argument may be null or empty.</summary>
        public WordAnalogy(string from, string to, string query, string answer)
        {
            From = Require(from, nameof(from));
            To = Require(to, nameof(to));
            Query = Require(query, nameof(query));
            Answer = Require(answer, nameof(answer));
        }

        /// <inheritdoc />
        public override string ToString() => $"{From}→{To} / {Query}→{Answer}";

        private static string Require(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("Word cannot be empty.", name);
            return value;
        }
    }

    /// <summary>
    /// A tiny hand-made word embedding: every word is a point in 3-D space (each axis in about [−1, 1]).
    /// Similar words sit in tight clusters, and each analogy is the same arrow inside its cluster,
    /// so "king − man + woman" lands next to "queen". The positions are also where the Word Space stop
    /// draws each orb, scaled to the room.
    /// </summary>
    public sealed class WordSpace
    {
        /// <summary>Number of numbers per word.</summary>
        public const int Dimensions = 3;

        private readonly List<string> _words = new List<string>();
        private readonly List<double[]> _vectors = new List<double[]>();
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string[]> _clusters = new List<string[]>();

        /// <summary>Every word, in table order.</summary>
        public IReadOnlyList<string> Words => _words;

        /// <summary>Every word's vector, in table order. Callers must not modify the rows.</summary>
        public IReadOnlyList<double[]> Vectors => _vectors;

        /// <summary>The groups of similar words, used by "Sort the stars".</summary>
        public IReadOnlyList<string[]> Clusters => _clusters;

        /// <summary>The analogy the clip and round 2 show: man→woman carried to king lands on queen.</summary>
        public static WordAnalogy RoyalAnalogy { get; } = new WordAnalogy("man", "woman", "king", "queen");

        /// <summary>The three "Beat the clock" analogies of round 3.</summary>
        public static IReadOnlyList<WordAnalogy> ClockAnalogies { get; } = new[]
        {
            new WordAnalogy("puppy", "dog", "kitten", "cat"),
            new WordAnalogy("Paris", "France", "Tokyo", "Japan"),
            new WordAnalogy("rock", "rocks", "drone", "drones")
        };

        /// <summary>
        /// The ride's fixed table. Inside each cluster the same arrow (for example "grown up" or "plural")
        /// is the same step, with a little hand-set jitter so answers are near, not exactly on, the arrow tip.
        /// </summary>
        public static WordSpace CreateRideTable()
        {
            var space = new WordSpace();

            // People: male → female is +x, ordinary → royal is +y.
            space.AddCluster(
                ("man", -0.78, 0.35, 0.00),
                ("woman", -0.46, 0.36, 0.01),
                ("king", -0.77, 0.72, 0.10),
                ("queen", -0.47, 0.70, 0.12));

            // Animals: young → grown is +y, dog → cat is +x.
            space.AddCluster(
                ("puppy", 0.44, 0.34, -0.10),
                ("dog", 0.45, 0.70, -0.08),
                ("kitten", 0.76, 0.35, -0.12),
                ("cat", 0.78, 0.72, -0.10));

            // Places: city → country is (0, −0.3, +0.2).
            space.AddCluster(
                ("Paris", -0.20, -0.42, 0.40),
                ("France", -0.21, -0.73, 0.61),
                ("Tokyo", 0.20, -0.44, 0.41),
                ("Japan", 0.19, -0.75, 0.62));

            // Space rocks: one → many is −z.
            space.AddCluster(
                ("rock", -0.60, -0.30, -0.30),
                ("rocks", -0.61, -0.31, -0.62),
                ("asteroid", -0.78, -0.50, -0.38),
                ("comet", -0.44, -0.56, -0.42));

            // Helpers: one → many is −z, the same step as rock → rocks.
            space.AddCluster(
                ("drone", 0.60, -0.30, -0.30),
                ("drones", 0.62, -0.30, -0.61),
                ("friend", 0.77, -0.55, -0.36),
                ("helper", 0.45, -0.54, -0.40));

            return space;
        }

        /// <summary>True when the table has this word (case-sensitive).</summary>
        public bool Contains(string word) => word != null && _index.ContainsKey(word);

        /// <summary>Table index of a word. Throws when the word is unknown.</summary>
        public int IndexOf(string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));
            if (!_index.TryGetValue(word, out int i))
            {
                throw new ArgumentException($"'{word}' is not in the word space.", nameof(word));
            }

            return i;
        }

        /// <summary>A copy of a word's vector.</summary>
        public double[] VectorOf(string word) => (double[])_vectors[IndexOf(word)].Clone();

        /// <summary>The arrow from one word to another: v(to) − v(from).</summary>
        public double[] Arrow(string from, string to) => VectorMath.Sub(_vectors[IndexOf(to)], _vectors[IndexOf(from)]);

        /// <summary>Where the arrow from→to lands when its tail is put on <paramref name="tail"/>: v(tail) + v(to) − v(from).</summary>
        public double[] CarryArrow(string from, string to, string tail) => VectorMath.Add(_vectors[IndexOf(tail)], Arrow(from, to));

        /// <summary>The word nearest a point, skipping <paramref name="exclude"/> (may be null). Null when all are excluded.</summary>
        public string Nearest(double[] point, IEnumerable<string> exclude = null)
        {
            HashSet<int> skip = null;
            if (exclude != null)
            {
                skip = new HashSet<int>();
                foreach (string w in exclude) skip.Add(IndexOf(w));
            }

            int i = VectorMath.Nearest(point, _vectors, skip);
            return i < 0 ? null : _words[i];
        }

        /// <summary>
        /// Solves "from is to to as query is to ?": the word nearest v(query) + v(to) − v(from),
        /// never one of the three words in the question (the usual rule for word analogies).
        /// </summary>
        public string Solve(string from, string to, string query)
        {
            return Nearest(CarryArrow(from, to, query), new[] { from, to, query });
        }

        /// <summary>Distance between two words.</summary>
        public double Distance(string a, string b) => VectorMath.Distance(_vectors[IndexOf(a)], _vectors[IndexOf(b)]);

        private void AddCluster(params (string word, double x, double y, double z)[] entries)
        {
            var names = new string[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                (string word, double x, double y, double z) = entries[i];
                if (_index.ContainsKey(word)) throw new InvalidOperationException($"Duplicate word '{word}'.");
                _index[word] = _words.Count;
                _words.Add(word);
                _vectors.Add(new[] { x, y, z });
                names[i] = word;
            }

            _clusters.Add(names);
        }
    }
}
