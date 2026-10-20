using System;
using System.Collections.Generic;
using Convergence.Core.Math;

namespace Convergence.Core.Neural
{
    /// <summary>One candidate next word and its probability.</summary>
    public readonly struct TokenProbability
    {
        /// <summary>The word.</summary>
        public string Token { get; }

        /// <summary>Its probability, in [0, 1].</summary>
        public double Probability { get; }

        /// <summary>Creates a pair.</summary>
        public TokenProbability(string token, double probability)
        {
            Token = token;
            Probability = probability;
        }
    }

    /// <summary>
    /// A toy language model small enough to read: fixed token embeddings, one scaled dot-product attention
    /// head, a linear readout, then Softmax with temperature. All weights are hand-set, not trained.
    ///
    /// Embedding features: thing, person, plural, action, function word, tool, silly, pronoun.
    /// The head's query looks for the feature a word "needs" (a pronoun needs its kind of noun, a
    /// grammar word needs the verb), and each key is the word's own features. A word only looks back
    /// at earlier words (the first word looks at itself). The readout scores each word AURA can say
    /// from h = embedding(last) + attention output.
    /// </summary>
    public sealed class NextTokenModel
    {
        private const int Thing = 0, Person = 1, Plural = 2, Action = 3, Function = 4, Tool = 5, Silly = 6, Pronoun = 7;

        /// <summary>Embedding width.</summary>
        public const int EmbeddingSize = 8;

        /// <summary>How sharply a query picks its match. Larger is sharper.</summary>
        public const double QueryGain = 6.0;

        /// <summary>The one word that is a correct order after "rock ahead , fire the".</summary>
        public const string CorrectOrder = "laser";

        private static readonly string[] OrderPrompt = { "rock", "ahead", ",", "fire", "the" };

        private readonly Dictionary<string, double[]> _embeddings = new Dictionary<string, double[]>(StringComparer.Ordinal);
        private readonly string[] _outputs;
        private readonly double[][] _readout;
        private readonly double[,] _queryWeights;
        private readonly double[,] _keyWeights;

        /// <summary>The words the readout can say, in logit order.</summary>
        public IReadOnlyList<string> OutputVocabulary => _outputs;

        /// <summary>"rock ahead , fire the": the prompt of "Finish the order".</summary>
        public static IReadOnlyList<string> OrderPromptTokens => OrderPrompt;

        /// <summary>The three "Spotlight" sentences. The last token is the pronoun to resolve.</summary>
        public static IReadOnlyList<string[]> SpotlightSentences { get; } = new[]
        {
            new[] { "the", "rock", "is", "close", ",", "so", "fire", "at", "it" },
            new[] { "the", "pilot", "saw", "the", "rock", ",", "so", "she" },
            new[] { "the", "drones", "found", "the", "rock", ",", "so", "follow", "them" }
        };

        /// <summary>Creates the ride's fixed model.</summary>
        public NextTokenModel()
        {
            foreach (string w in new[] { "the", "a", "is", "so", "at", ",", ".", "ahead", "close", "tired", "home" })
            {
                Add(w, (Function, 1.0));
            }

            Add("rock", (Thing, 1.0));
            Add("asteroid", (Thing, 1.0));
            Add("drone", (Thing, 1.0));
            Add("rocks", (Plural, 1.0));
            Add("drones", (Plural, 1.0));
            Add("pilot", (Person, 1.0));
            Add("captain", (Person, 1.0));
            foreach (string w in new[] { "fire", "saw", "found", "follow", "steer", "scan" })
            {
                Add(w, (Action, 1.0));
            }

            Add("laser", (Tool, 1.0), (Thing, 0.3));
            Add("thrusters", (Tool, 1.0));
            Add("scanner", (Tool, 1.0), (Thing, 0.3));
            Add("banana", (Silly, 1.0), (Thing, 0.3));
            Add("it", (Pronoun, 1.0), (Thing, 1.0));
            Add("she", (Pronoun, 1.0), (Person, 1.0));
            Add("them", (Pronoun, 1.0), (Plural, 1.0));

            // Query/key space: [thing, person, plural, action]. A pronoun asks for its own kind of noun;
            // a grammar word asks for the verb.
            _queryWeights = new double[4, EmbeddingSize];
            _keyWeights = new double[4, EmbeddingSize];
            for (int i = 0; i < 3; i++)
            {
                _queryWeights[i, i] = QueryGain;
                _keyWeights[i, i] = 1.0;
            }

            _queryWeights[3, Function] = QueryGain;
            _keyWeights[3, Action] = 1.0;

            // Readout: how strongly "a verb was just said" votes for each word.
            _outputs = new[] { "laser", "it", "them", "thrusters", "scanner", "rock", "the", "banana" };
            double[] actionVote = { 4.6, 0.8, 0.3, 0.6, 0.4, 0.2, -1.0, -3.0 };
            _readout = new double[_outputs.Length][];
            for (int i = 0; i < _outputs.Length; i++)
            {
                _readout[i] = new double[EmbeddingSize];
                _readout[i][Action] = actionVote[i];
            }
        }

        /// <summary>True when the model knows the word.</summary>
        public bool Knows(string token) => token != null && _embeddings.ContainsKey(token);

        /// <summary>A copy of a word's embedding.</summary>
        public double[] Embedding(string token) => (double[])Lookup(token).Clone();

        /// <summary>
        /// Attention weights from the token at <paramref name="queryIndex"/> to every token in the sentence.
        /// Entries after the query, and the query itself (unless it is the first token), are 0. The row sums to 1.
        /// </summary>
        public double[] AttentionRow(IReadOnlyList<string> tokens, int queryIndex)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (queryIndex < 0 || queryIndex >= tokens.Count) throw new ArgumentOutOfRangeException(nameof(queryIndex));

            int n = tokens.Count;
            int d = _queryWeights.GetLength(0);
            var query = new double[1, d];
            var keys = new double[n, d];
            var values = new double[n, 1];
            double[] q = Project(_queryWeights, Lookup(tokens[queryIndex]));
            for (int j = 0; j < d; j++) query[0, j] = q[j];

            var keep = new bool[1, n];
            for (int k = 0; k < n; k++)
            {
                double[] key = Project(_keyWeights, Lookup(tokens[k]));
                for (int j = 0; j < d; j++) keys[k, j] = key[j];
                keep[0, k] = queryIndex == 0 ? k == 0 : k < queryIndex;
            }

            Attention.Result result = Attention.ScaledDotProduct(query, keys, values, keep);
            var row = new double[n];
            for (int k = 0; k < n; k++) row[k] = result.Weights[0, k];
            return row;
        }

        /// <summary>Index of the earlier word the token at <paramref name="queryIndex"/> attends to most.</summary>
        public int TopAttention(IReadOnlyList<string> tokens, int queryIndex)
        {
            double[] row = AttentionRow(tokens, queryIndex);
            int best = 0;
            for (int k = 1; k < row.Length; k++)
            {
                if (row[k] > row[best]) best = k;
            }

            return best;
        }

        /// <summary>Raw readout scores for the word after the sentence, in <see cref="OutputVocabulary"/> order.</summary>
        public double[] NextLogits(IReadOnlyList<string> tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (tokens.Count == 0) throw new ArgumentException("Need at least one token.", nameof(tokens));

            int last = tokens.Count - 1;
            double[] row = AttentionRow(tokens, last);
            double[] h = (double[])Lookup(tokens[last]).Clone();
            for (int k = 0; k < tokens.Count; k++)
            {
                if (row[k] == 0.0) continue;
                double[] e = Lookup(tokens[k]);
                for (int j = 0; j < EmbeddingSize; j++) h[j] += row[k] * e[j];
            }

            var logits = new double[_outputs.Length];
            for (int i = 0; i < _outputs.Length; i++) logits[i] = VectorMath.Dot(_readout[i], h);
            return logits;
        }

        /// <summary>Probabilities of each next word at a temperature (the WILD lever). Sums to 1.</summary>
        public double[] NextProbabilities(IReadOnlyList<string> tokens, double temperature)
        {
            return ActivationFunctions.Softmax(NextLogits(tokens), temperature);
        }

        /// <summary>The <paramref name="k"/> likeliest next words, likeliest first.</summary>
        public IReadOnlyList<TokenProbability> TopK(IReadOnlyList<string> tokens, double temperature, int k)
        {
            if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
            double[] p = NextProbabilities(tokens, temperature);
            var order = new List<int>();
            for (int i = 0; i < p.Length; i++) order.Add(i);
            order.Sort((a, b) => p[b] != p[a] ? p[b].CompareTo(p[a]) : a.CompareTo(b));
            var result = new List<TokenProbability>();
            for (int i = 0; i < k && i < order.Count; i++) result.Add(new TokenProbability(_outputs[order[i]], p[order[i]]));
            return result;
        }

        /// <summary>
        /// Picks the next word. <paramref name="uniform"/> is a random number in [0, 1) supplied by the caller,
        /// so the model stays deterministic and testable.
        /// </summary>
        public string Sample(IReadOnlyList<string> tokens, double temperature, double uniform)
        {
            if (uniform < 0.0 || uniform >= 1.0) throw new ArgumentOutOfRangeException(nameof(uniform));
            double[] p = NextProbabilities(tokens, temperature);
            double sum = 0.0;
            for (int i = 0; i < p.Length; i++)
            {
                sum += p[i];
                if (uniform < sum) return _outputs[i];
            }

            return _outputs[p.Length - 1];
        }

        private double[] Lookup(string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (!_embeddings.TryGetValue(token, out double[] e))
            {
                throw new ArgumentException($"Unknown token '{token}'.", nameof(token));
            }

            return e;
        }

        private static double[] Project(double[,] weights, double[] embedding)
        {
            int rows = weights.GetLength(0);
            var result = new double[rows];
            for (int r = 0; r < rows; r++)
            {
                double s = 0.0;
                for (int c = 0; c < embedding.Length; c++) s += weights[r, c] * embedding[c];
                result[r] = s;
            }

            return result;
        }

        private void Add(string token, params (int feature, double value)[] features)
        {
            var e = new double[EmbeddingSize];
            foreach ((int feature, double value) in features) e[feature] = value;
            _embeddings.Add(token, e);
        }
    }
}
