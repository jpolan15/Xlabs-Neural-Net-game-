using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Stop 5 (Attention + next word).
    /// Round 1 "Spotlight": the rider aims the pronoun at the word they think it means; it counts when that is
    /// the word <see cref="NextTokenModel"/> attends to most.
    /// Round 2 "Finish the order": AURA samples the word after "rock ahead , fire the"; only "laser" is a correct order.
    /// Passes only at 3/3 spotlights and <see cref="RequiredOrders"/> correct orders.
    /// </summary>
    public sealed class AttentionPuzzle
    {
        /// <summary>Correct orders needed in round 2.</summary>
        public const int RequiredOrders = 3;

        private readonly bool[] _spotlights;

        /// <summary>The model that decides.</summary>
        public NextTokenModel Model { get; }

        /// <summary>The spotlight sentences; the last token of each is the pronoun.</summary>
        public IReadOnlyList<string[]> Sentences { get; }

        /// <summary>Spotlight sentences the rider got right.</summary>
        public int SpotlightsCorrect
        {
            get
            {
                int n = 0;
                foreach (bool s in _spotlights) if (s) n++;
                return n;
            }
        }

        /// <summary>Correct orders AURA has written so far.</summary>
        public int CorrectOrders { get; private set; }

        /// <summary>Silly or wrong orders the ship refused.</summary>
        public int RefusedOrders { get; private set; }

        /// <summary>True when every spotlight and every required order is done.</summary>
        public bool Passed => SpotlightsCorrect == _spotlights.Length && CorrectOrders >= RequiredOrders;

        /// <summary>Creates the puzzle on the ride's model and sentences.</summary>
        public AttentionPuzzle() : this(new NextTokenModel(), NextTokenModel.SpotlightSentences) { }

        /// <summary>Creates the puzzle on a model and spotlight sentences.</summary>
        public AttentionPuzzle(NextTokenModel model, IReadOnlyList<string[]> sentences)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Sentences = sentences ?? throw new ArgumentNullException(nameof(sentences));
            if (sentences.Count == 0) throw new ArgumentException("Need at least one sentence.", nameof(sentences));
            foreach (string[] s in sentences)
            {
                if (s == null || s.Length < 2) throw new ArgumentException("Each sentence needs a word before the pronoun.", nameof(sentences));
            }

            _spotlights = new bool[sentences.Count];
        }

        /// <summary>Index of the word the pronoun of sentence <paramref name="sentence"/> really means.</summary>
        public int Referent(int sentence)
        {
            string[] s = SentenceAt(sentence);
            return Model.TopAttention(s, s.Length - 1);
        }

        /// <summary>The attention row the beams show after the rider aims.</summary>
        public double[] Beams(int sentence)
        {
            string[] s = SentenceAt(sentence);
            return Model.AttentionRow(s, s.Length - 1);
        }

        /// <summary>The rider aims the spotlight at token <paramref name="chosenIndex"/>. True when AURA agrees.</summary>
        public bool AimSpotlight(int sentence, int chosenIndex)
        {
            bool hit = chosenIndex == Referent(sentence);
            if (hit) _spotlights[sentence] = true;
            return hit;
        }

        /// <summary>True when the word is a correct order for the prompt.</summary>
        public static bool IsCorrectOrder(string word) => word == NextTokenModel.CorrectOrder;

        /// <summary>
        /// AURA writes one order at the WILD lever's temperature. <paramref name="uniform"/> is a random number in [0, 1).
        /// Returns the word she wrote; it counts toward the win only if it is correct.
        /// </summary>
        public string WriteOrder(double temperature, double uniform)
        {
            string word = Model.Sample(NextTokenModel.OrderPromptTokens, temperature, uniform);
            if (IsCorrectOrder(word)) CorrectOrders++;
            else RefusedOrders++;
            return word;
        }

        /// <summary>Clears all progress.</summary>
        public void Reset()
        {
            Array.Clear(_spotlights, 0, _spotlights.Length);
            CorrectOrders = 0;
            RefusedOrders = 0;
        }

        private string[] SentenceAt(int sentence)
        {
            if (sentence < 0 || sentence >= Sentences.Count) throw new ArgumentOutOfRangeException(nameof(sentence));
            return Sentences[sentence];
        }
    }
}
