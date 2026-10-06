using System;
using System.Collections.Generic;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Data for one chamber: cases, copy, and a reference network that the tests evaluate.
    /// </summary>
    public sealed class CurriculumPuzzle
    {
        /// <summary>Stable id, for example chamber01.</summary>
        public string Id { get; }

        /// <summary>Short title shown as data, not as a verdict.</summary>
        public string Title { get; }

        /// <summary>Names of the inputs, in case-vector order.</summary>
        public IReadOnlyList<string> InputNames { get; }

        /// <summary>Name of the single output.</summary>
        public string OutputName { get; }

        /// <summary>Cases and required activation. Evaluation uses this object unchanged.</summary>
        public PuzzleDefinition Definition { get; }

        /// <summary>Zero for a single neuron. Two for the XOR wall.</summary>
        public int HiddenUnitCount { get; }

        /// <summary>One prompt at a time. Each line is a verb plus an object.</summary>
        public IReadOnlyList<string> Prompts { get; }

        /// <summary>Lines revealed only after a pass.</summary>
        public IReadOnlyList<string> TermReveals { get; }

        /// <summary>When the next chamber or the hidden layer may open.</summary>
        public string UnlockRule { get; }

        /// <summary>Single-neuron reference as w1, w2, bias. Empty when a hidden layer is required.</summary>
        public IReadOnlyList<double> ReferenceWeights { get; }

        /// <summary>Controls the player may touch.</summary>
        public IReadOnlyList<string> AvailableControls { get; }

        /// <summary>
        /// Creates a curriculum row. Names and prompts are required. Reference weights are three numbers or empty.
        /// </summary>
        public CurriculumPuzzle(
            string id,
            string title,
            IReadOnlyList<string> inputNames,
            string outputName,
            PuzzleDefinition definition,
            int hiddenUnitCount,
            IReadOnlyList<string> prompts,
            IReadOnlyList<string> termReveals,
            string unlockRule,
            IReadOnlyList<double> referenceWeights,
            IReadOnlyList<string> availableControls)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required.", nameof(id));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (inputNames == null || inputNames.Count != definition.RequiredInputs)
            {
                throw new ArgumentException("Input name count must match the puzzle inputs.", nameof(inputNames));
            }
            if (hiddenUnitCount < 0) throw new ArgumentOutOfRangeException(nameof(hiddenUnitCount));
            if (referenceWeights != null && referenceWeights.Count != 0 && referenceWeights.Count != 3)
            {
                throw new ArgumentException("Reference weights are w1, w2, and bias, or empty.", nameof(referenceWeights));
            }

            Id = id;
            Title = title ?? string.Empty;
            InputNames = inputNames;
            OutputName = string.IsNullOrWhiteSpace(outputName) ? "Out" : outputName;
            Definition = definition;
            HiddenUnitCount = hiddenUnitCount;
            Prompts = prompts ?? new string[0];
            TermReveals = termReveals ?? new string[0];
            UnlockRule = unlockRule ?? string.Empty;
            ReferenceWeights = referenceWeights ?? new double[0];
            AvailableControls = availableControls ?? new string[0];
        }
    }

    /// <summary>
    /// The chamber rows and the checks that prove their reference solutions.
    /// </summary>
    public static class CurriculumCatalog
    {
        /// <summary>Chamber 01. Fire on comet, rock, and both. Hold the drone.</summary>
        public static CurriculumPuzzle Chamber01()
        {
            return Row(
                "chamber01",
                "The Awakening Gate",
                PuzzleDefinition.CreateORGatePuzzle(),
                0,
                new[] { "Plug in the ROCK wire.", "Now ICE.", "Turn the dial." },
                new[] { "That wire strength is called a weight.", "This dial is the bias." },
                "Opens when every case passes.",
                new[] { 1.0, 1.0, -0.5 });
        }

        /// <summary>Chamber 02. Fire only when both inputs are on.</summary>
        public static CurriculumPuzzle Chamber02()
        {
            var cases = new[]
            {
                new TestCase(new[] { 0.0, 0.0 }, new[] { 0.0 }, "(0, 0) -> 0"),
                new TestCase(new[] { 0.0, 1.0 }, new[] { 0.0 }, "(0, 1) -> 0"),
                new TestCase(new[] { 1.0, 0.0 }, new[] { 0.0 }, "(1, 0) -> 0"),
                new TestCase(new[] { 1.0, 1.0 }, new[] { 1.0 }, "(1, 1) -> 1")
            };
            var definition = new PuzzleDefinition(cases, 2, 1, ActivationType.Step, 1.0, "Both or nothing");
            return Row(
                "chamber02",
                "Both or nothing",
                definition,
                0,
                new[] { "Only BOTH should fire." },
                new[] { "The dial is how much evidence it needs." },
                "Opens when every case passes.",
                new[] { 1.0, 1.0, -1.5 });
        }

        /// <summary>Chamber 03. Fire on rock and not on ice.</summary>
        public static CurriculumPuzzle Chamber03()
        {
            var cases = new[]
            {
                new TestCase(new[] { 0.0, 0.0 }, new[] { 0.0 }, "(0, 0) -> 0"),
                new TestCase(new[] { 0.0, 1.0 }, new[] { 0.0 }, "(0, 1) -> 0"),
                new TestCase(new[] { 1.0, 0.0 }, new[] { 1.0 }, "(1, 0) -> 1"),
                new TestCase(new[] { 1.0, 1.0 }, new[] { 0.0 }, "(1, 1) -> 0")
            };
            var definition = new PuzzleDefinition(cases, 2, 1, ActivationType.Step, 1.0, "Rock but not ice");
            return Row(
                "chamber03",
                "Rock but not ice",
                definition,
                0,
                new[] { "Fire at rock, never at ice." },
                new[] { "A negative wire holds it back." },
                "Opens when every case passes.",
                new[] { 1.0, -1.0, -0.5 });
        }

        /// <summary>Chamber 04. XOR. One neuron cannot pass.</summary>
        public static CurriculumPuzzle Chamber04()
        {
            return Row(
                "chamber04",
                "One line can't split these",
                PuzzleDefinition.CreateXorPuzzle(),
                2,
                new[] { "Fire at comet or rock, not both." },
                new[] { "Stacking neurons makes a layer." },
                "After 90 s unsolved, circuit slots open.",
                new double[0]);
        }

        /// <summary>All four rows in play order.</summary>
        public static IReadOnlyList<CurriculumPuzzle> All()
        {
            return new[] { Chamber01(), Chamber02(), Chamber03(), Chamber04() };
        }

        /// <summary>
        /// Evaluates a hypothetical single neuron. Callers that already have a network should use PuzzleEvaluator.Evaluate.
        /// </summary>
        public static PuzzleEvaluation EvaluateSingleNeuron(PuzzleDefinition puzzle, double w1, double w2, double bias)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            var network = NetworkModel.CreateSingleNeuronNetwork(2, new[] { w1, w2 }, bias, puzzle.RequiredActivation);
            return PuzzleEvaluator.Evaluate(network, puzzle);
        }

        /// <summary>
        /// True when some grid point passes. The XOR row must return false.
        /// Grid is inclusive from min to max.
        /// </summary>
        public static bool SingleNeuronGridSolvable(PuzzleDefinition puzzle, double min, double max, double step)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            if (step <= 0.0) throw new ArgumentOutOfRangeException(nameof(step));
            for (double w1 = min; w1 <= max + 1e-9; w1 += step)
            {
                for (double w2 = min; w2 <= max + 1e-9; w2 += step)
                {
                    for (double bias = min; bias <= max + 1e-9; bias += step)
                    {
                        if (EvaluateSingleNeuron(puzzle, w1, w2, bias).Passed) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// XOR reference: OR and AND hidden units, output fires on OR and not AND.
        /// </summary>
        public static NetworkModel CreateXorReferenceNetwork()
        {
            var hiddenOr = new NeuronModel(new[] { 1.0, 1.0 }, -0.5, ActivationType.Step);
            var hiddenAnd = new NeuronModel(new[] { 1.0, 1.0 }, -1.5, ActivationType.Step);
            var output = new NeuronModel(new[] { 1.0, -1.0 }, -0.5, ActivationType.Step);
            var hidden = new LayerModel(new[] { hiddenOr, hiddenAnd });
            var final = new LayerModel(new[] { output });
            return new NetworkModel(new[] { hidden, final });
        }

        /// <summary>Null when every reference check passes. Otherwise the first failure.</summary>
        public static string RunReferenceChecks()
        {
            foreach (var puzzle in All())
            {
                if (puzzle.HiddenUnitCount == 0)
                {
                    if (puzzle.ReferenceWeights.Count != 3) return puzzle.Id + " missing reference weights";
                    PuzzleEvaluation eval = EvaluateSingleNeuron(
                        puzzle.Definition,
                        puzzle.ReferenceWeights[0],
                        puzzle.ReferenceWeights[1],
                        puzzle.ReferenceWeights[2]);
                    if (!eval.Passed) return puzzle.Id + " reference did not pass";
                }
            }

            if (SingleNeuronGridSolvable(Chamber04().Definition, -2.0, 2.0, 0.5))
            {
                return "XOR was solved by one neuron";
            }

            PuzzleEvaluation hidden = PuzzleEvaluator.Evaluate(CreateXorReferenceNetwork(), Chamber04().Definition);
            if (!hidden.Passed) return "XOR hidden reference did not pass";

            PuzzleEvaluation zero = EvaluateSingleNeuron(Chamber01().Definition, 0.0, 0.0, 0.0);
            if (zero.Passed) return "zero weights passed chamber 01";

            PuzzleEvaluation boundary = EvaluateSingleNeuron(Chamber01().Definition, 1.0, 0.0, 0.0);
            if (boundary.Diagnostics[0].CalculatedZ != 0.0) return "boundary z was not 0";
            if (boundary.Diagnostics[0].ActualOutput < 0.5) return "step boundary at z 0 did not fire";

            PuzzleEvaluation negative = EvaluateSingleNeuron(Chamber03().Definition, 1.0, -1.0, -0.5);
            if (!negative.Passed) return "negative weight reference failed";
            return null;
        }

        static CurriculumPuzzle Row(
            string id,
            string title,
            PuzzleDefinition definition,
            int hidden,
            string[] prompts,
            string[] reveals,
            string unlock,
            double[] weights)
        {
            return new CurriculumPuzzle(
                id,
                title,
                new[] { "ROCK", "ICE" },
                "FIRE",
                definition,
                hidden,
                prompts,
                reveals,
                unlock,
                weights,
                new[] { "w1", "w2", "bias" });
        }
    }
}
