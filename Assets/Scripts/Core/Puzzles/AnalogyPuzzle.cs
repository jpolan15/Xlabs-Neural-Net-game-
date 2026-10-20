using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Stop 4 (Word Space). The rider carries an arrow (from→to) and puts its tail on a word; Core works out
    /// where the tip lands with <see cref="WordSpace.Nearest"/>. A round is won when the nearest word is the answer
    /// and the tip is within <see cref="ReachRadius"/> of it (so a wrong arrow that merely ends up closest still misses).
    /// The puzzle passes only when every round is won.
    /// </summary>
    public sealed class AnalogyPuzzle
    {
        /// <summary>How close (in word-space units) the tip must land to light a word.</summary>
        public const double ReachRadius = 0.15;

        private readonly bool[] _solved;

        /// <summary>The word space the puzzle uses.</summary>
        public WordSpace Space { get; }

        /// <summary>The rounds, in order.</summary>
        public IReadOnlyList<WordAnalogy> Rounds { get; }

        /// <summary>Rounds won so far.</summary>
        public int SolvedCount
        {
            get
            {
                int n = 0;
                foreach (bool s in _solved) if (s) n++;
                return n;
            }
        }

        /// <summary>True only when every round is won (100 %).</summary>
        public bool Passed => SolvedCount == _solved.Length;

        /// <summary>Creates a puzzle. Every word in every round must be in the space.</summary>
        public AnalogyPuzzle(WordSpace space, IReadOnlyList<WordAnalogy> rounds)
        {
            Space = space ?? throw new ArgumentNullException(nameof(space));
            if (rounds == null) throw new ArgumentNullException(nameof(rounds));
            if (rounds.Count == 0) throw new ArgumentException("Need at least one round.", nameof(rounds));
            foreach (WordAnalogy a in rounds)
            {
                if (a == null) throw new ArgumentException("Round cannot be null.", nameof(rounds));
                foreach (string w in new[] { a.From, a.To, a.Query, a.Answer })
                {
                    if (!space.Contains(w)) throw new ArgumentException($"'{w}' is not in the word space.", nameof(rounds));
                }
            }

            Rounds = rounds;
            _solved = new bool[rounds.Count];
        }

        /// <summary>The "Beat the clock" puzzle on the ride's table.</summary>
        public static AnalogyPuzzle CreateClockRound() => new AnalogyPuzzle(WordSpace.CreateRideTable(), WordSpace.ClockAnalogies);

        /// <summary>
        /// The word the tip lands on when the arrow from→to has its tail on <paramref name="tail"/>.
        /// The three words that define the move are skipped, as in every word-analogy test.
        /// </summary>
        public string Landing(string from, string to, string tail)
        {
            return Space.Nearest(Space.CarryArrow(from, to, tail), new[] { from, to, tail });
        }

        /// <summary>True when the arrow's tip, tail on <paramref name="tail"/>, is within <see cref="ReachRadius"/> of <paramref name="word"/>.</summary>
        public bool WithinReach(string from, string to, string tail, string word)
        {
            return Convergence.Core.Math.VectorMath.Distance(Space.CarryArrow(from, to, tail), Space.VectorOf(word)) <= ReachRadius;
        }

        /// <summary>
        /// The rider tries round <paramref name="round"/> with the arrow they carried. Returns true when the tip
        /// lands on that round's answer; the round then stays won.
        /// </summary>
        public bool Try(int round, string from, string to, string tail)
        {
            if (round < 0 || round >= _solved.Length) throw new ArgumentOutOfRangeException(nameof(round));
            WordAnalogy a = Rounds[round];
            bool hit = tail == a.Query && Landing(from, to, tail) == a.Answer && WithinReach(from, to, tail, a.Answer);
            if (hit) _solved[round] = true;
            return hit;
        }

        /// <summary>Clears every round.</summary>
        public void Reset() => Array.Clear(_solved, 0, _solved.Length);
    }
}
