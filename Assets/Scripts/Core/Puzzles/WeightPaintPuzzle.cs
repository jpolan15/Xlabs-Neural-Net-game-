using System;
using System.Collections.Generic;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Level 1's "paint the weights" puzzle: one neuron looks at a grid of pixels (0 = paper, 1 = ink). The rider
    /// paints each weight +1, 0 or −1. The neuron's total is Σ ink_i × weight_i; it fires when the total reaches the
    /// threshold. The puzzle is solved when every digit that should fire fires and every other digit does not.
    /// </summary>
    public sealed class WeightPaintPuzzle
    {
        private readonly double[][] _digits;
        private readonly bool[] _shouldFire;

        /// <summary>Number of weights (pixels per digit).</summary>
        public int CellCount { get; }

        /// <summary>Number of test digits.</summary>
        public int DigitCount => _digits.Length;

        /// <summary>The line the neuron's total must reach to fire.</summary>
        public double Threshold { get; }

        /// <summary>
        /// Creates the puzzle. Every digit needs <paramref name="digits"/>[0].Length pixel values.
        /// </summary>
        public WeightPaintPuzzle(double[][] digits, bool[] shouldFire, double threshold)
        {
            if (digits == null) throw new ArgumentNullException(nameof(digits));
            if (shouldFire == null) throw new ArgumentNullException(nameof(shouldFire));
            if (digits.Length == 0) throw new ArgumentException("The puzzle needs at least one digit.", nameof(digits));
            if (shouldFire.Length != digits.Length)
            {
                throw new ArgumentException("One should-fire flag per digit.", nameof(shouldFire));
            }
            if (double.IsNaN(threshold) || double.IsInfinity(threshold))
            {
                throw new ArgumentOutOfRangeException(nameof(threshold));
            }

            CellCount = digits[0]?.Length ?? 0;
            if (CellCount == 0) throw new ArgumentException("Digits need pixels.", nameof(digits));
            _digits = new double[digits.Length][];
            for (int d = 0; d < digits.Length; d++)
            {
                if (digits[d] == null || digits[d].Length != CellCount)
                {
                    throw new ArgumentException($"Digit {d} must have {CellCount} pixels.", nameof(digits));
                }
                _digits[d] = (double[])digits[d].Clone();
            }
            _shouldFire = (bool[])shouldFire.Clone();
            Threshold = threshold;
        }

        /// <summary>Pixel value of digit <paramref name="digit"/> at <paramref name="cell"/>.</summary>
        public double Pixel(int digit, int cell) => _digits[digit][cell];

        /// <summary>Whether digit <paramref name="digit"/> should make the neuron fire.</summary>
        public bool ShouldFire(int digit) => _shouldFire[digit];

        /// <summary>The value a tap moves a weight to: 0 → +1 → −1 → 0.</summary>
        public static int NextValue(int weight) => weight == 0 ? 1 : weight == 1 ? -1 : 0;

        /// <summary>The value a brush stroke moves a weight to: the brush's value (+1 green, −1 red), or 0 if it already has it.</summary>
        public static int PaintValue(int weight, int brush) => brush == 0 || weight == brush ? 0 : System.Math.Sign(brush);

        /// <summary>
        /// Runs every digit through the neuron with the given weights (each −1, 0 or +1).
        /// </summary>
        public WeightPaintResult Evaluate(IReadOnlyList<int> weights)
        {
            ValidateWeights(weights);
            var sums = new double[_digits.Length];
            var fired = new bool[_digits.Length];
            var correct = new bool[_digits.Length];
            int correctCount = 0;
            double margin = 0.0;
            for (int d = 0; d < _digits.Length; d++)
            {
                double sum = Sum(d, weights, -1, 0);
                sums[d] = sum;
                fired[d] = sum >= Threshold;
                correct[d] = fired[d] == _shouldFire[d];
                if (correct[d]) correctCount++;
                margin += _shouldFire[d] ? sum - Threshold : Threshold - sum;
            }
            var copy = new int[weights.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = weights[i];
            return new WeightPaintResult(this, copy, sums, fired, correct, correctCount, margin);
        }

        /// <summary>
        /// The one weight change that most improves the number of digits answered correctly, tie-broken by the
        /// biggest total margin (how far each digit sits on its right side of the line), then the lowest cell index,
        /// then +1 before −1 before 0. Computed from the evaluator alone, so it works from any state.
        /// Returns null when the puzzle is already solved.
        /// </summary>
        public WeightPaintHint? Hint(IReadOnlyList<int> weights)
        {
            ValidateWeights(weights);
            Score(weights, -1, 0, out int currentCorrect, out _);
            if (currentCorrect == _digits.Length)
            {
                return null;
            }

            int bestCell = -1, bestValue = 0, bestCorrect = -1;
            double bestMargin = double.NegativeInfinity;
            for (int cell = 0; cell < CellCount; cell++)
            {
                for (int option = 0; option < 3; option++)
                {
                    int value = option == 0 ? 1 : option == 1 ? -1 : 0;
                    if (weights[cell] == value) continue;
                    Score(weights, cell, value, out int c, out double m);
                    if (c > bestCorrect || (c == bestCorrect && m > bestMargin + 1e-12))
                    {
                        bestCorrect = c;
                        bestMargin = m;
                        bestCell = cell;
                        bestValue = value;
                    }
                }
            }
            return new WeightPaintHint(bestCell, bestValue);
        }

        private void Score(IReadOnlyList<int> weights, int overrideCell, int overrideValue, out int correct, out double margin)
        {
            correct = 0;
            margin = 0.0;
            for (int d = 0; d < _digits.Length; d++)
            {
                double sum = Sum(d, weights, overrideCell, overrideValue);
                if ((sum >= Threshold) == _shouldFire[d]) correct++;
                margin += _shouldFire[d] ? sum - Threshold : Threshold - sum;
            }
        }

        private double Sum(int digit, IReadOnlyList<int> weights, int overrideCell, int overrideValue)
        {
            double[] pixels = _digits[digit];
            double sum = 0.0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int w = i == overrideCell ? overrideValue : weights[i];
                sum += pixels[i] * w;
            }
            return sum;
        }

        private void ValidateWeights(IReadOnlyList<int> weights)
        {
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (weights.Count != CellCount)
            {
                throw new ArgumentException($"Expected {CellCount} weights, received {weights.Count}.", nameof(weights));
            }
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] < -1 || weights[i] > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), $"Weight {i} is {weights[i]}; weights are −1, 0 or +1.");
                }
            }
        }
    }

    /// <summary>
    /// What the neuron did with every digit for one set of weights.
    /// </summary>
    public sealed class WeightPaintResult
    {
        private readonly WeightPaintPuzzle _puzzle;
        private readonly int[] _weights;
        private readonly double[] _sums;
        private readonly bool[] _fired;
        private readonly bool[] _correct;

        internal WeightPaintResult(WeightPaintPuzzle puzzle, int[] weights, double[] sums, bool[] fired, bool[] correct,
            int correctCount, double totalMargin)
        {
            _puzzle = puzzle;
            _weights = weights;
            _sums = sums;
            _fired = fired;
            _correct = correct;
            CorrectCount = correctCount;
            TotalMargin = totalMargin;
        }

        /// <summary>The neuron's total Σ ink × weight for a digit.</summary>
        public double Sum(int digit) => _sums[digit];

        /// <summary>Whether the total reached the threshold.</summary>
        public bool Fired(int digit) => _fired[digit];

        /// <summary>Whether the neuron did what that digit needs.</summary>
        public bool Correct(int digit) => _correct[digit];

        /// <summary>One cell's share of the total: that pixel's ink × its weight.</summary>
        public double Contribution(int digit, int cell) => _puzzle.Pixel(digit, cell) * _weights[cell];

        /// <summary>How many digits the neuron got right.</summary>
        public int CorrectCount { get; }

        /// <summary>Σ over digits of how far each total sits on its right side of the line (negative = wrong side).</summary>
        public double TotalMargin { get; }

        /// <summary>True when every digit is right.</summary>
        public bool Passed => CorrectCount == _sums.Length;

        /// <summary>
        /// The first digit the neuron got wrong, or −1. <paramref name="firedWrongly"/> is true when it fired for a digit
        /// that should not fire, false when it stayed quiet for one that should.
        /// </summary>
        public int FirstWrong(out bool firedWrongly)
        {
            for (int d = 0; d < _correct.Length; d++)
            {
                if (!_correct[d])
                {
                    firedWrongly = _fired[d];
                    return d;
                }
            }
            firedWrongly = false;
            return -1;
        }
    }

    /// <summary>
    /// A suggested move: set <see cref="Cell"/> to <see cref="Value"/> (−1, 0 or +1).
    /// </summary>
    public readonly struct WeightPaintHint
    {
        /// <summary>The cell to change.</summary>
        public int Cell { get; }

        /// <summary>The value to give it.</summary>
        public int Value { get; }

        /// <summary>Creates a hint.</summary>
        public WeightPaintHint(int cell, int value)
        {
            Cell = cell;
            Value = value;
        }
    }
}
