using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// How a case's outputs are marked correct.
    /// </summary>
    public enum ScoringMode
    {
        /// <summary>Each output is 1 when the prediction is at least 0.5, otherwise 0.</summary>
        Binary,

        /// <summary>Each output is correct when it lies within that output's absolute tolerance.</summary>
        Continuous
    }

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

        /// <summary>
        /// Binary classification or continuous targets.
        /// </summary>
        public ScoringMode Scoring { get; }

        private readonly double[] _outputTolerances;

        /// <summary>
        /// Absolute tolerance for continuous output <paramref name="index"/>. Binary scoring ignores this.
        /// </summary>
        public double ToleranceFor(int index)
        {
            if (_outputTolerances == null || index < 0 || index >= _outputTolerances.Length)
            {
                return 0.05;
            }

            return _outputTolerances[index];
        }

        public PuzzleDefinition(
            IEnumerable<TestCase> testCases,
            int requiredInputs,
            int requiredOutputs,
            ActivationType requiredActivation = ActivationType.Step,
            double accuracyThreshold = 1.0,
            string chamberTitle = "The Awakening Gate",
            ScoringMode scoring = ScoringMode.Binary,
            double[] outputTolerances = null)
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

            if (outputTolerances != null && outputTolerances.Length != requiredOutputs)
            {
                throw new ArgumentException("Output tolerance count must match required outputs.", nameof(outputTolerances));
            }

            _testCases = list.ToArray();
            RequiredInputs = requiredInputs;
            RequiredOutputs = requiredOutputs;
            RequiredActivation = requiredActivation;
            AccuracyThreshold = accuracyThreshold;
            ChamberTitle = chamberTitle ?? string.Empty;
            Scoring = scoring;
            _outputTolerances = outputTolerances == null ? null : (double[])outputTolerances.Clone();
        }

        /// <summary>
        /// Level 1 - The Awakening Gate (OR-gate perceptron).
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

        /// <summary>
        /// Replaced Level 1: Earth Location Puzzle (Space Theme).
        /// Requires a larger 16-input network (4x4 matrix representing an image of space)
        /// and 2 outputs representing the (X,Y) coordinates of Earth to blast off.
        /// </summary>
        public static PuzzleDefinition CreateEarthLocationPuzzle()
        {
            var testCases = new TestCase[]
            {
                new TestCase(new double[] { 0,0,0,0, 0,1,1,0, 0,1,1,0, 0,0,0,0 }, new double[] { 0.5, 0.5 }, "Earth Center"),
                new TestCase(new double[] { 1,1,0,0, 1,1,0,0, 0,0,0,0, 0,0,0,0 }, new double[] { 0.0, 1.0 }, "Earth Top Left"),
                new TestCase(new double[] { 0,0,0,0, 0,0,0,0, 0,0,1,1, 0,0,1,1 }, new double[] { 1.0, 0.0 }, "Earth Bottom Right")
            };

            return new PuzzleDefinition(
                testCases: testCases,
                requiredInputs: 16,
                requiredOutputs: 2,
                requiredActivation: ActivationType.Sigmoid,
                accuracyThreshold: 0.9,
                chamberTitle: "Navigation Repair",
                scoring: ScoringMode.Continuous,
                outputTolerances: new double[] { 0.15, 0.15 }
            );
        }

        /// <summary>
        /// Chamber 02. XOR is not linearly separable. A single neuron cannot pass.
        /// </summary>
        public static PuzzleDefinition CreateXorPuzzle()
        {
            var testCases = new TestCase[]
            {
                new TestCase(new double[] { 0.0, 0.0 }, new double[] { 0.0 }, "(0, 0) -> 0"),
                new TestCase(new double[] { 0.0, 1.0 }, new double[] { 1.0 }, "(0, 1) -> 1"),
                new TestCase(new double[] { 1.0, 0.0 }, new double[] { 1.0 }, "(1, 0) -> 1"),
                new TestCase(new double[] { 1.0, 1.0 }, new double[] { 0.0 }, "(1, 1) -> 0")
            };

            return new PuzzleDefinition(
                testCases: testCases,
                requiredInputs: 2,
                requiredOutputs: 1,
                requiredActivation: ActivationType.Step,
                accuracyThreshold: 1.0,
                chamberTitle: "Spectrum Filter"
            );
        }

        /// <summary>
        /// Held-out Earth / not-Earth photos. Features are blue ratio, white-cloud ratio, and brightness.
        /// A set that labels every blue body as Earth fails the ice giant.
        /// </summary>
        public static PuzzleDefinition CreateEarthPhotoPuzzle()
        {
            var testCases = new TestCase[]
            {
                new TestCase(new double[] { 0.45, 0.30, 0.55 }, new double[] { 1.0 }, "Earth"),
                new TestCase(new double[] { 0.35, 0.40, 0.42 }, new double[] { 1.0 }, "Earth clouds"),
                new TestCase(new double[] { 0.72, 0.08, 0.90 }, new double[] { 0.0 }, "Ice giant"),
                new TestCase(new double[] { 0.08, 0.05, 0.40 }, new double[] { 0.0 }, "Rust world")
            };

            return new PuzzleDefinition(
                testCases: testCases,
                requiredInputs: 3,
                requiredOutputs: 1,
                requiredActivation: ActivationType.Sigmoid,
                accuracyThreshold: 1.0,
                chamberTitle: "Earth Recognizer"
            );
        }
    }
}
