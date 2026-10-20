using System;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Stop 3 (Layers). Rock alone → shoot, ice alone → shoot, both together → don't shoot: XOR.
    /// Round 1 uses one neuron with the ROCK, ICE and TRIGGER levers, and can never pass 4/4.
    /// Round 2 adds a helper neuron that fires on BOTH and sends a fixed −2 into the output, making a 2-2-1 network.
    /// A neuron fires when Σ wᵢxᵢ ≥ trigger, so its bias is −trigger.
    /// </summary>
    public static class XorPuzzle
    {
        /// <summary>The fixed weight of the helper's yellow pipe into the output.</summary>
        public const double HelperPipeWeight = -2.0;

        /// <summary>The output neuron's fixed trigger in round 2.</summary>
        public const double OutputTrigger = 0.5;

        /// <summary>The XOR truth table (inputs rock, ice).</summary>
        public static PuzzleDefinition Definition { get; } = PuzzleDefinition.CreateXorPuzzle();

        /// <summary>Round 1: one neuron, step activation.</summary>
        public static NetworkModel BuildSingleNeuron(double rockWeight, double iceWeight, double trigger)
        {
            return NetworkModel.CreateSingleNeuronNetwork(2, new[] { rockWeight, iceWeight }, -trigger, ActivationType.Step);
        }

        /// <summary>
        /// Round 2: hidden layer = [main neuron (rock, ice, trigger), helper neuron (1, 1, helperTrigger)],
        /// output = step(main + <see cref="HelperPipeWeight"/> × helper − <see cref="OutputTrigger"/>).
        /// </summary>
        public static NetworkModel BuildWithHelper(double rockWeight, double iceWeight, double trigger, double helperTrigger)
        {
            var main = new NeuronModel(new[] { rockWeight, iceWeight }, -trigger, ActivationType.Step);
            var helper = new NeuronModel(new[] { 1.0, 1.0 }, -helperTrigger, ActivationType.Step);
            var output = new NeuronModel(new[] { 1.0, HelperPipeWeight }, -OutputTrigger, ActivationType.Step);
            return new NetworkModel(new[] { new LayerModel(new[] { main, helper }), new LayerModel(new[] { output }) });
        }

        /// <summary>Evaluates round 1 settings against XOR.</summary>
        public static PuzzleEvaluation EvaluateSingleNeuron(double rockWeight, double iceWeight, double trigger)
        {
            return PuzzleEvaluator.Evaluate(BuildSingleNeuron(rockWeight, iceWeight, trigger), Definition);
        }

        /// <summary>Evaluates round 2 settings against XOR. Passes only at 4/4.</summary>
        public static PuzzleEvaluation EvaluateWithHelper(double rockWeight, double iceWeight, double trigger, double helperTrigger)
        {
            return PuzzleEvaluator.Evaluate(BuildWithHelper(rockWeight, iceWeight, trigger, helperTrigger), Definition);
        }

        /// <summary>
        /// The best case count any single neuron reaches over a grid of lever settings (inclusive).
        /// This is the proof the stop shows in round 1: it is 3, never 4.
        /// </summary>
        public static int BestSingleNeuronCases(double min, double max, double step)
        {
            if (step <= 0.0) throw new ArgumentOutOfRangeException(nameof(step));
            if (max < min) throw new ArgumentException("Max must not be below min.");
            int best = 0;
            for (double w1 = min; w1 <= max + 1e-9; w1 += step)
            {
                for (double w2 = min; w2 <= max + 1e-9; w2 += step)
                {
                    for (double t = min; t <= max + 1e-9; t += step)
                    {
                        int cases = EvaluateSingleNeuron(w1, w2, t).PassedCases;
                        if (cases > best) best = cases;
                    }
                }
            }

            return best;
        }
    }
}
