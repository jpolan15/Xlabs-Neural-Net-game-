using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Pure C# immutable definition of a puzzle, including required input/output dimensions,
    /// required activation type, accuracy threshold, and test cases.
    /// </summary>
    public sealed class PuzzleDefinition
    {
        private readonly TestCase[] _testCases;

        /// <summary>
        /// Test cases that must be evaluated.
        /// </summary>
        public IReadOnlyList<TestCase> TestCases => _testCases;

        /// <summary>
        /// Expected number of inputs to the network (e.g. 2 for Level 1 OR-gate).
        /// </summary>
        public int RequiredInputs { get; }

        /// <summary>
        /// Expected number of outputs from the network (e.g. 1 for Level 1 OR-gate).
        /// </summary>
        public int RequiredOutputs { get; }

        /// <summary>
        /// Required activation function for the puzzle (e.g. Step for Level 1).
        /// </summary>
        public ActivationType RequiredActivation { get; }

        /// <summary>
        /// Required binary accuracy to pass the puzzle (1.0 = 100% required for Level 1).
        /// </summary>
        public double AccuracyThreshold { get; }

        /// <summary>
        /// Descriptive title of the puzzle chamber.
        /// </summary>
        public string ChamberTitle { get; }

        public PuzzleDefinition(
            IEnumerable<TestCase> testCases,
            int requiredInputs,
            int requiredOutputs,
            ActivationType requiredActivation = ActivationType.Step,
            double accuracyThreshold = 1.0,
            string chamberTitle = "The Awakening Gate")
        {
            if (testCases == null)
            {
                throw new ArgumentNullException(nameof(testCases));
            }
            if (requiredInputs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredInputs), "RequiredInputs must be > 0.");
            }
            if (requiredOutputs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredOutputs), "RequiredOutputs must be > 0.");
            }
            if (accuracyThreshold <= 0.0 || accuracyThreshold > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(accuracyThreshold), "AccuracyThreshold must be between 0.0 and 1.0.");
            }

            var list = new List<TestCase>();
            foreach (var tc in testCases)
            {
                if (tc == null)
                {
                    throw new ArgumentException("TestCase cannot be null.", nameof(testCases));
                }
                if (tc.InputLength != requiredInputs)
                {
                    throw new ArgumentException(
                        $"TestCase '{tc.Label}' input dimension {tc.InputLength} does not match required inputs {requiredInputs}."
                    );
                }
                if (tc.OutputLength != requiredOutputs)
                {
                    throw new ArgumentException(
                        $"TestCase '{tc.Label}' output dimension {tc.OutputLength} does not match required outputs {requiredOutputs}."
                    );
                }
                list.Add(tc);
            }

            if (list.Count == 0)
            {
                throw new ArgumentException("Puzzle must contain at least one test case.", nameof(testCases));
            }

            _testCases = list.ToArray();
            RequiredInputs = requiredInputs;
            RequiredOutputs = requiredOutputs;
            RequiredActivation = requiredActivation;
            AccuracyThreshold = accuracyThreshold;
            ChamberTitle = chamberTitle ?? string.Empty;
        }

        /// <summary>
        /// Canonical factory for Level 1 — The Awakening Gate (OR-gate perceptron).
        /// Truth table:
        /// (0, 0) -> 0
        /// (0, 1) -> 1
        /// (1, 0) -> 1
        /// (1, 1) -> 1
        /// Requires Step activation and 100% binary accuracy.
        /// </summary>
        public static PuzzleDefinition CreateORGatePuzzle()
        {
            var testCases = new TestCase[]
            {
                new TestCase(new double[] { 0.0, 0.0 }, new double[] { 0.0 }, "(0, 0) -> 0"),
                new TestCase(new double[] { 0.0, 1.0 }, new double[] { 1.0 }, "(0, 1) -> 1"),
                new TestCase(new double[] { 1.0, 0.0 }, new double[] { 1.0 }, "(1, 0) -> 1"),
                new TestCase(new double[] { 1.0, 1.0 }, new double[] { 1.0 }, "(1, 1) -> 1")
            };

            return new PuzzleDefinition(
                testCases: testCases,
                requiredInputs: 2,
                requiredOutputs: 1,
                requiredActivation: ActivationType.Step,
                accuracyThreshold: 1.0,
                chamberTitle: "The Awakening Gate"
            );
        }
    }
}
