using System;
using System.Collections.Generic;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Level 1's "plug the strings" game. The answers start from the network's real scores (logits) for a digit.
    /// Each string is a real hidden neuron with its real brightness for that digit; plugging it into an answer adds a
    /// strong wire of weight <see cref="PlugWeight"/> from that neuron to that answer, so the answer's score grows by
    /// brightness × weight. The game is won when the wanted answer has the top score, the same argmax the forward pass
    /// reads. Plugging into a wrong answer makes that answer stronger, which is the lesson.
    /// </summary>
    public sealed class PlugPuzzle
    {
        private readonly double[] _baseLogits;
        private readonly double[] _brightness;
        private readonly int[] _plugged;

        public int AnswerCount => _baseLogits.Length;
        public int StringCount => _brightness.Length;
        public int Wanted { get; }
        public double PlugWeight { get; }

        public PlugPuzzle(IReadOnlyList<double> baseLogits, IReadOnlyList<double> stringBrightness, int wanted, double plugWeight)
        {
            if (baseLogits == null) throw new ArgumentNullException(nameof(baseLogits));
            if (stringBrightness == null) throw new ArgumentNullException(nameof(stringBrightness));
            if (baseLogits.Count < 2) throw new ArgumentException("Needs at least two answers.", nameof(baseLogits));
            if (wanted < 0 || wanted >= baseLogits.Count) throw new ArgumentOutOfRangeException(nameof(wanted));
            _baseLogits = new double[baseLogits.Count];
            for (int k = 0; k < _baseLogits.Length; k++) _baseLogits[k] = baseLogits[k];
            _brightness = new double[stringBrightness.Count];
            for (int s = 0; s < _brightness.Length; s++) _brightness[s] = stringBrightness[s];
            _plugged = new int[_brightness.Length];
            for (int s = 0; s < _plugged.Length; s++) _plugged[s] = -1;
            Wanted = wanted;
            PlugWeight = plugWeight;
        }

        public double Brightness(int s) => _brightness[s];

        /// <summary>The answer string s is plugged into, or -1 when it hangs loose.</summary>
        public int PluggedInto(int s) => _plugged[s];

        /// <summary>Plugs string s into answer k (-1 unplugs it).</summary>
        public void Plug(int s, int answer)
        {
            if (s < 0 || s >= _plugged.Length) throw new ArgumentOutOfRangeException(nameof(s));
            if (answer < -1 || answer >= _baseLogits.Length) throw new ArgumentOutOfRangeException(nameof(answer));
            _plugged[s] = answer;
        }

        public double Logit(int k)
        {
            double z = _baseLogits[k];
            for (int s = 0; s < _plugged.Length; s++) if (_plugged[s] == k) z += _brightness[s] * PlugWeight;
            return z;
        }

        /// <summary>Softmax of the scores: what the answer neurons glow with.</summary>
        public double[] Probabilities()
        {
            var p = new double[_baseLogits.Length];
            double max = double.NegativeInfinity;
            for (int k = 0; k < p.Length; k++) max = System.Math.Max(max, p[k] = Logit(k));
            double sum = 0;
            for (int k = 0; k < p.Length; k++) sum += p[k] = System.Math.Exp(p[k] - max);
            for (int k = 0; k < p.Length; k++) p[k] /= sum;
            return p;
        }

        /// <summary>The answer with the top score (lowest index on a tie).</summary>
        public int Guess
        {
            get
            {
                int best = 0;
                for (int k = 1; k < _baseLogits.Length; k++) if (Logit(k) > Logit(best)) best = k;
                return best;
            }
        }

        public bool Passed => Guess == Wanted;

        /// <summary>How far the wanted answer still trails the top other answer (0 or less once it leads).</summary>
        public double Gap
        {
            get
            {
                double other = double.NegativeInfinity;
                for (int k = 0; k < _baseLogits.Length; k++) if (k != Wanted) other = System.Math.Max(other, Logit(k));
                return other - Logit(Wanted);
            }
        }

        /// <summary>The brightest string not yet in the wanted answer; -1 when passed.</summary>
        public int Hint()
        {
            if (Passed) return -1;
            int best = -1;
            for (int s = 0; s < _plugged.Length; s++)
            {
                if (_plugged[s] == Wanted) continue;
                if (best < 0 || _brightness[s] > _brightness[best]) best = s;
            }

            return best;
        }
    }
}
