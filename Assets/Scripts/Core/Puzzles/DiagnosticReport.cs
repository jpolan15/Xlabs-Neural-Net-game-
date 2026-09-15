using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Detailed diagnostic information for an individual test case evaluation.
    /// </summary>
    public sealed class CaseDiagnostic
    {
        private readonly double[] _inputs;

        /// <summary>
        /// Input vector evaluated. Returns a defensive clone.
        /// </summary>
        public double[] Inputs => (double[])_inputs.Clone();

        /// <summary>
        /// Expected ground-truth target output.
        /// </summary>
        public double ExpectedOutput { get; }

        /// <summary>
        /// Calculated pre-activation sum z = sum(w_i * x_i) + b.
        /// </summary>
        public double CalculatedZ { get; }

        /// <summary>
        /// Actual activated output a = f(z).
        /// </summary>
        public double ActualOutput { get; }

        /// <summary>
        /// Whether the actual output matched the expected target within classification semantics.
        /// </summary>
        public bool IsCorrect { get; }

        /// <summary>
        /// Test case label (e.g. "(0, 1) -> 1").
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Explanatory diagnostic message.
        /// </summary>
        public string Explanation { get; }

        public CaseDiagnostic(
            double[] inputs,
            double expectedOutput,
            double calculatedZ,
            double actualOutput,
            bool isCorrect,
            string label,
            string explanation)
        {
            _inputs = inputs != null ? (double[])inputs.Clone() : Array.Empty<double>();
            ExpectedOutput = expectedOutput;
            CalculatedZ = calculatedZ;
            ActualOutput = actualOutput;
            IsCorrect = isCorrect;
            Label = label ?? string.Empty;
            Explanation = explanation ?? string.Empty;
        }
    }

    /// <summary>
    /// Comprehensive evaluation report returned by PuzzleEvaluator.
    /// Immutable result containing per-case diagnostics and overall pass/fail status.
    /// </summary>
    public sealed class PuzzleEvaluation
    {
        private readonly CaseDiagnostic[] _diagnostics;

        /// <summary>
        /// True if and only if accuracy meets or exceeds the threshold AND the required activation is active.
        /// </summary>
        public bool Passed { get; }

        /// <summary>
        /// Binary classification accuracy in range [0.0, 1.0].
        /// </summary>
        public double Accuracy { get; }

        /// <summary>
        /// Total number of evaluated test cases.
        /// </summary>
        public int TotalCases { get; }

        /// <summary>
        /// Number of correctly evaluated test cases.
        /// </summary>
        public int PassedCases { get; }

        /// <summary>
        /// Whether the active activation type matches the puzzle's required activation type.
        /// </summary>
        public bool ActivationMatches { get; }

        /// <summary>
        /// The network's current active activation function.
        /// </summary>
        public ActivationType ActiveActivation { get; }

        /// <summary>
        /// The activation function required to solve the puzzle.
        /// </summary>
        public ActivationType RequiredActivation { get; }

        /// <summary>
        /// Per-test-case diagnostic breakdowns.
        /// </summary>
        public IReadOnlyList<CaseDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// High-level summary of the evaluation result for holographic or UI display.
        /// </summary>
        public string Summary { get; }

        public PuzzleEvaluation(
            bool passed,
            double accuracy,
            int totalCases,
            int passedCases,
            bool activationMatches,
            ActivationType activeActivation,
            ActivationType requiredActivation,
            IEnumerable<CaseDiagnostic> diagnostics,
            string summary)
        {
            Passed = passed;
            Accuracy = accuracy;
            TotalCases = totalCases;
            PassedCases = passedCases;
            ActivationMatches = activationMatches;
            ActiveActivation = activeActivation;
            RequiredActivation = requiredActivation;
            _diagnostics = diagnostics != null ? new List<CaseDiagnostic>(diagnostics).ToArray() : Array.Empty<CaseDiagnostic>();
            Summary = summary ?? string.Empty;
        }
    }
}
