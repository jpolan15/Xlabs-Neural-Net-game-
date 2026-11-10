using System;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    // The shapes of Assets/_Project/Data/Level2/level2_words.json, written by ArtSource/training/bake_level2.py from a real
    // GPT-2 (124M). JsonUtility reads them, so every field is public and every class serializable. Nothing here is computed:
    // the tokens, the odds, the embedding picture, the attention weights and the story road's probabilities are the model's.

    [Serializable] public sealed class L2Meta { public string model; public int vocab; public int layers; public int heads; public int dims; public int parameters; public string date; public string source; }
    [Serializable] public sealed class L2Token { public string text; public int id; }
    [Serializable] public sealed class L2Thread { public string text; public L2Token[] tokens; }
    [Serializable] public sealed class L2Word { public string word; public string[] tokens; public int[] ids; }
    [Serializable] public sealed class L2GalaxyWord { public string word; public string category; public float x; public float y; public float z; }
    [Serializable] public sealed class L2Centroid { public string category; public float x; public float y; public float z; }
    [Serializable] public sealed class L2Galaxy { public float[] explained; public L2Centroid[] centroids; public L2GalaxyWord[] words; }
    [Serializable] public sealed class L2Near { public string word; public float similarity; }
    [Serializable] public sealed class L2Analogy { public string a; public string b; public string c; public L2Near[] answers; }
    [Serializable] public sealed class L2Neighbours { public string word; public L2Near[] nearest; }
    [Serializable] public sealed class L2Attention { public string sentence; public int query; public int[] answers; public string[] words; public float[] weights; public int layer; public int head; }
    [Serializable] public sealed class L2Choice { public string word; public float p; public int id; }
    [Serializable] public sealed class L2Prediction { public string prompt; public string truth; public float truthP; public int truthRank; public float entropy; public L2Choice[] top; }
    [Serializable] public sealed class L2TempSetting { public float temperature; public L2Choice[] top; }
    [Serializable] public sealed class L2Temperature { public string prompt; public L2TempSetting[] settings; }
    [Serializable] public sealed class L2StoryNode { public string path; public string text; public L2Choice[] lanes; }
    [Serializable] public sealed class L2Story { public string start; public int forks; public L2StoryNode[] nodes; }
    /// <summary>One step of GPT-2's tokenizer gluing a word: the pieces after the step, and the glue rule it used (1 = the first of 50,000; 0 = the letters before any glue).</summary>
    [Serializable] public sealed class L2GlueStep { public string[] pieces; public int rule; }
    [Serializable] public sealed class L2Merge { public string word; public L2GlueStep[] steps; public int[] ids; }
    /// <summary>king - man + woman on eight of GPT-2's 768 embedding columns (bake_level2_extras.py), and queen's own numbers there.</summary>
    [Serializable] public sealed class L2AnalogyNumbers { public int[] columns; public float[] king; public float[] man; public float[] woman; public float[] result; public float[] queen; public float queenSimilarity; }

    [Serializable]
    public sealed class Level2File
    {
        public L2Meta meta;
        public L2Thread thread;
        public L2Word[] words;
        public L2Galaxy galaxy;
        public L2Analogy[] analogies;
        public L2Neighbours[] neighbours;
        public L2Attention[] attention;
        public L2Prediction[] predict;
        public L2Temperature temperature;
        public L2Story story;
        public L2Merge[] merges;
        public L2AnalogyNumbers analogyNumbers;
    }

    /// <summary>
    /// Level 2's baked data and the few lookups the games and explainers share. It checks the file's shapes and its own
    /// promises (every probability in 0 to 1, the true next word inside the offered choices, every story fork reachable) and throws
    /// with what is wrong, so a bad re-bake fails at load and not in the middle of a ride.
    /// </summary>
    public sealed class Level2Data
    {
        public const string AssetPath = "Assets/_Project/Data/Level2/level2_words.json";

        /// <summary>The words the slicer serves, in order; the boss comes last.</summary>
        public static readonly string[] SliceWords =
        {
            "cat", "jellyfish", "pizza", "skateboard", "robot", "hamburger", "unbelievable", "watermelon",
            "tokenization", "rollercoaster", "astronaut", "backpropagation", "chatbot",
        };

        public const string BossWord = "supercalifragilisticexpialidocious";

        /// <summary>The words the tokens stop glues together piece by piece: a common one (one token) and a rarer one (left in pieces).</summary>
        public const string CommonGlueWord = "the";
        public const string RareGlueWord = "chatbot";

        /// <summary>The prompts of the odds game, in play order (a sure thing first, a coin flip last).</summary>
        public static readonly string[] PredictPrompts =
        {
            "Once upon a", "Peanut butter and", "In the morning I drink a cup of", "Roses are red, violets are",
        };

        /// <summary>The prompt the odds explainer shows (the sentence the whole level rides on).</summary>
        public const string ExplainPrompt = "The cat chased the mouse because it was";

        /// <summary>The neighbourhoods of the sorting game, and how many words of each start in place and are left to carry.</summary>
        public static readonly string[] Neighbourhoods = { "animals", "food", "vehicles", "feelings" };
        public const int SeedsPerNeighbourhood = 3;
        public const int LoosePerNeighbourhood = 2;

        /// <summary>
        /// The words in play in each neighbourhood, seeds first and the ones left to carry last. Picked against the bake as the
        /// five that stand furthest apart from the pod's seat, so every word is easy to aim at.
        /// </summary>
        private static readonly string[][] Picks =
        {
            new[] { "cat", "rabbit", "horse", "lion", "fish" },
            new[] { "rice", "cheese", "banana", "pizza", "apple" },
            new[] { "truck", "train", "plane", "bike", "bus" },
            new[] { "angry", "scared", "tired", "proud", "calm" },
        };

        /// <summary>The attention game's rounds, as indexes into the baked attention list.</summary>
        public static readonly int[] SpotlightRounds = { 0, 2, 1 };

        /// <summary>The attention round the explainer shows (a fluffy blue creature).</summary>
        public const int ExplainAttention = 3;

        public Level2File File { get; }

        private readonly Dictionary<string, L2StoryNode> _story = new Dictionary<string, L2StoryNode>();

        private Level2Data(Level2File file)
        {
            File = file;
            foreach (L2StoryNode node in file.story.nodes) _story[node.path] = node;
        }

        /// <summary>Parses the baked JSON and checks it. Throws when anything is missing or off.</summary>
        public static Level2Data Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Level 2 data is empty.", nameof(json));
            var file = JsonUtility.FromJson<Level2File>(json);
            Validate(file);
            return new Level2Data(file);
        }

        private static void Validate(Level2File f)
        {
            void Need(bool ok, string what)
            {
                if (!ok) throw new InvalidOperationException("Level 2 data: " + what + ". Re-run ArtSource/training/bake_level2.py.");
            }

            Need(f != null && f.meta != null && f.thread != null && f.words != null && f.galaxy != null && f.attention != null && f.predict != null && f.temperature != null && f.story != null, "a section is missing");
            Need(f.thread.tokens.Length > 4, "the thread sentence has too few tokens");
            foreach (L2Word w in f.words) Need(w.tokens.Length >= 1 && w.tokens.Length == w.ids.Length, w.word + ": tokens and ids differ");
            foreach (string w in SliceWords) Need(Array.Exists(f.words, x => x.word == w), "the slicer word " + w + " is not baked");
            Need(Array.Exists(f.words, x => x.word == BossWord && x.tokens.Length >= 8), "the boss word is not baked, or is not a many-token word");

            Need(f.galaxy.words.Length >= 20, "the galaxy has too few words");
            foreach (string n in Neighbourhoods)
            {
                int count = 0;
                foreach (L2GalaxyWord g in f.galaxy.words) if (g.category == n) count++;
                Need(count >= SeedsPerNeighbourhood + LoosePerNeighbourhood, "the neighbourhood " + n + " has only " + count + " words");
                Need(Array.Exists(f.galaxy.centroids, c => c.category == n), "no centre for " + n);
                foreach (string pick in Picks[Array.IndexOf(Neighbourhoods, n)])
                    Need(Array.Exists(f.galaxy.words, g => g.category == n && g.word == pick), "the pick '" + pick + "' is not baked in " + n);
            }

            foreach (int r in SpotlightRounds) CheckAttention(f, r, Need);
            CheckAttention(f, ExplainAttention, Need);

            foreach (string p in PredictPrompts) CheckPrediction(f, p, Need);
            CheckPrediction(f, ExplainPrompt, Need);

            Need(f.merges != null && f.merges.Length >= 2, "the glue steps are missing (run ArtSource/training/bake_level2_extras.py)");
            Need(Merge(f, CommonGlueWord) != null && Last(Merge(f, CommonGlueWord)).pieces.Length == 1, "the common glue word is not one token");
            Need(Merge(f, RareGlueWord) != null && Last(Merge(f, RareGlueWord)).pieces.Length >= 2, "the rare glue word is not several tokens");
            foreach (L2Merge m in f.merges)
            {
                Need(m.steps.Length >= 2 && m.steps[0].rule == 0 && m.ids.Length == Last(m).pieces.Length, "the glue steps of " + m.word + " have the wrong shape");
                for (int i = 1; i < m.steps.Length; i++) Need(m.steps[i].rule > m.steps[i - 1].rule && m.steps[i].rule <= 50000, m.word + ": glue rules must climb, 1 to 50,000");
            }

            L2AnalogyNumbers an = f.analogyNumbers;
            Need(an != null && an.columns != null && an.columns.Length >= 4, "the king - man + woman numbers are missing (run bake_level2_extras.py)");
            int cols = an.columns.Length;
            Need(an.king.Length == cols && an.man.Length == cols && an.woman.Length == cols && an.result.Length == cols && an.queen.Length == cols, "the analogy rows differ in length");
            for (int i = 0; i < cols; i++) Need(Mathf.Abs(an.king[i] - an.man[i] + an.woman[i] - an.result[i]) < 0.005f, "the analogy sum is off in column " + i);
            Need(an.queenSimilarity > 0f && an.queenSimilarity <= 1f, "queen's similarity is outside 0 to 1");

            Need(f.temperature.settings.Length >= 3, "too few temperatures");
            Need(f.story.forks >= 2, "the story has too few forks");
            var paths = new HashSet<string>();
            foreach (L2StoryNode n in f.story.nodes)
            {
                Need(n.lanes.Length == 4, "story node '" + n.path + "' has " + n.lanes.Length + " lanes, not 4");
                foreach (L2Choice c in n.lanes) Need(c.p > 0f && c.p < 1f && !string.IsNullOrEmpty(c.word), "story node '" + n.path + "' has a bad lane");
                paths.Add(n.path);
            }

            Need(paths.Contains(""), "the story has no first fork");
            foreach (L2StoryNode n in f.story.nodes)
            {
                if (n.path.Length + 1 >= f.story.forks) continue;
                for (int k = 0; k < 4; k++) Need(paths.Contains(n.path + k), "the story fork '" + n.path + "' has no node for lane " + k);
            }
        }

        private static L2Merge Merge(Level2File f, string word) => Array.Find(f.merges, m => m.word == word);

        private static L2GlueStep Last(L2Merge m) => m.steps[m.steps.Length - 1];

        /// <summary>The glue steps baked for a word (the tokens stop shows two).</summary>
        public L2Merge Merge(string word) => Merge(File, word);

        private static void CheckAttention(Level2File f, int r, Action<bool, string> need)
        {
            need(r >= 0 && r < f.attention.Length, "attention round " + r + " is missing");
            L2Attention a = f.attention[r];
            need(a.words.Length > a.query && a.weights.Length == a.query && a.answers.Length >= 1, "attention round " + r + " has the wrong shape (the weights cover the words before the query)");
            float sum = 0f;
            foreach (float w in a.weights) sum += w;
            need(Mathf.Abs(sum - 1f) < 0.02f, "attention round " + r + " weights add up to " + sum);
            foreach (int i in a.answers) need(i >= 0 && i < a.query, "attention round " + r + " names an answer after the query word");
        }

        private static void CheckPrediction(Level2File f, string prompt, Action<bool, string> need)
        {
            L2Prediction p = Array.Find(f.predict, x => x.prompt == prompt);
            need(p != null, "no prediction for '" + prompt + "'");
            need(p.top.Length >= 8, "'" + prompt + "' has too few choices");
            float sum = 0f;
            foreach (L2Choice c in p.top)
            {
                need(c.p >= 0f && c.p <= 1f, "'" + prompt + "' has a probability outside 0 to 1");
                sum += c.p;
            }

            need(sum <= 1.001f, "'" + prompt + "' choices add up to more than 1");
            need(p.truthP > 0f && p.truthRank >= 1, "'" + prompt + "' has no truth");
        }

        /// <summary>A baked slicer word by its spelling.</summary>
        public L2Word Word(string word) => Array.Find(File.words, w => w.word == word);

        /// <summary>The prediction baked for a prompt.</summary>
        public L2Prediction Prediction(string prompt) => Array.Find(File.predict, p => p.prompt == prompt);

        /// <summary>One round of the attention game (or the explainer's).</summary>
        public L2Attention Attention(int index) => File.attention[index];

        /// <summary>The story road's fork reached by choosing these lanes (a digit per fork), or null past the last fork.</summary>
        public L2StoryNode StoryNode(string path) => _story.TryGetValue(path, out L2StoryNode n) ? n : null;

        /// <summary>The words of a neighbourhood: the picks in play first (seeds, then the loose ones), then the rest in the baked order.</summary>
        public List<L2GalaxyWord> Neighbourhood(string category)
        {
            var list = new List<L2GalaxyWord>();
            int n = Array.IndexOf(Neighbourhoods, category);
            if (n >= 0)
            {
                foreach (string pick in Picks[n])
                {
                    L2GalaxyWord w = Array.Find(File.galaxy.words, g => g.category == category && g.word == pick);
                    if (w != null) list.Add(w);
                }
            }

            foreach (L2GalaxyWord w in File.galaxy.words) if (w.category == category && !list.Contains(w)) list.Add(w);
            return list;
        }

        public L2Centroid Centre(string category) => Array.Find(File.galaxy.centroids, c => c.category == category);

        /// <summary>The temperature setting nearest to t.</summary>
        public L2TempSetting Temperature(float t)
        {
            L2TempSetting best = File.temperature.settings[0];
            foreach (L2TempSetting s in File.temperature.settings) if (Mathf.Abs(s.temperature - t) < Mathf.Abs(best.temperature - t)) best = s;
            return best;
        }

        /// <summary>A token as the rider reads it: no leading space.</summary>
        public static string Clean(string token) => token.Trim();
    }
}
