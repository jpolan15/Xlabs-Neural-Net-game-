using System;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Pure C# immutable definition of a test case for neural evaluation.
    /// Performs defensive array copies on construction to guarantee immutability.
    /// </summary>
    public sealed class TestCase
    {
        private readonly double[] _inputs;
        private readonly double[] _expectedOutputs;

        /// <summary>
        /// Input feature vector. Returns a defensive clone.
        /// </summary>
        public double[] Inputs => (double[])_inputs.Clone();

        /// <summary>
        /// Expected target output vector. Returns a defensive clone.
        /// </summary>
        public double[] ExpectedOutputs => (double[])_expectedOutputs.Clone();

        /// <summary>
        /// Human-readable label for diagnostics and UI display (e.g. "(0, 1) -> 1").
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Number of input features.
        /// </summary>
        public int InputLength => _inputs.Length;

        /// <summary>
        /// Number of expected target outputs.
        /// </summary>
        public int OutputLength => _expectedOutputs.Length;

        /// <summary>
        /// Constructs an immutable test case.
        /// </summary>
        public TestCase(double[] inputs, double[] expectedOutputs, string label = "")
        {
            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }
            if (expectedOutputs == null)
            {
                throw new ArgumentNullException(nameof(expectedOutputs));
            }
            if (inputs.Length == 0)
            {
                throw new ArgumentException("Inputs array cannot be empty.", nameof(inputs));
            }
            if (expectedOutputs.Length == 0)
            {
                throw new ArgumentException("ExpectedOutputs array cannot be empty.", nameof(expectedOutputs));
            }

            _inputs = (double[])inputs.Clone();
            _expectedOutputs = (double[])expectedOutputs.Clone();
            Label = label ?? string.Empty;
        }

        /// <summary>
        /// Reads an input by index without array allocation.
        /// </summary>
        public double GetInput(int index) => _inputs[index];

        /// <summary>
        /// Reads an expected output by index without array allocation.
        /// </summary>
        public double GetExpectedOutput(int index) => _expectedOutputs[index];
    }
}
