using System;
using System.Collections.Generic;
using Convergence.Core.Math;
using Convergence.Core.Neural;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Pure C# evaluator that tests a NetworkModel against a PuzzleDefinition.
    /// Evaluates true neural mathematics; never uses hardcoded or scripted completion.
    /// </summary>
    public static class PuzzleEvaluator
    {
        /// <summary>
        /// Evaluates the network against the puzzle's full test suite.
        /// Rejects shape mismatches with ArgumentException.
        /// Returns an immutable PuzzleEvaluation.
        /// </summary>
        public static PuzzleEvaluation Evaluate(NetworkModel network, PuzzleDefinition puzzle)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }
            if (puzzle == null)
            {
                throw new ArgumentNullException(nameof(puzzle));
            }

            // Strict shape validation
            if (network.InputCount != puzzle.RequiredInputs)
            {
                throw new ArgumentException(
                    $"Network input count {network.InputCount} does not match puzzle required inputs {puzzle.RequiredInputs}."
                );
            }
            if (network.OutputCount != puzzle.RequiredOutputs)
            {
                throw new ArgumentException(
                    $"Network output count {network.OutputCount} does not match puzzle required outputs {puzzle.RequiredOutputs}."
                );
            }

            // Identify active activation
            ActivationType activeActivation;
            if (network.SingleNeuron != null)
            {
                activeActivation = network.SingleNeuron.Activation;
            }
            else
            {
                var outputLayer = network.Layers[network.Layers.Count - 1];
                activeActivation = outputLayer.LayerActivationOverride ?? outputLayer.Neurons[0].Activation;
            }

            bool activationMatches = (activeActivation == puzzle.RequiredActivation);

            int totalCases = puzzle.TestCases.Count;
            var diagnostics = new List<CaseDiagnostic>(totalCases);
            double[] predictions = new double[totalCases];
            double[] targets = new double[totalCases];
            int correctCount = 0;

            for (int i = 0; i < totalCases; i++)
            {
                TestCase tc = puzzle.TestCases[i];
                double[] inputs = tc.Inputs;
                double[] outputs = network.Forward(inputs);

                double predicted = outputs[0];
                double target = tc.GetExpectedOutput(0);
                double z = network.SingleNeuron != null
                    ? network.SingleNeuron.LastZ
                    : network.Layers[network.Layers.Count - 1].Neurons[0].LastZ;

                predictions[i] = predicted;
                targets[i] = target;

                bool isCorrect = OutputsMatch(outputs, tc, puzzle);

                if (isCorrect)
                {
                    correctCount++;
                }

                string explanation;
                if (!activationMatches)
                {
                    explanation = $"Active crystal '{activeActivation}' does not match required activation '{puzzle.RequiredActivation}'.";
                }
                else if (isCorrect)
                {
                    explanation = $"Correct: z = {z:+0.00;-0.00;0.00} -> output {predicted:F0} matches target {target:F0}.";
                }
                else
                {
                    if (target == 1.0 && z < 0.0)
                    {
                        explanation = $"Signal too weak: z = {z:+0.00;-0.00;0.00} < 0. Output is 0, expected 1. Increase weights or bias.";
                    }
                    else if (target == 0.0 && z >= 0.0)
                    {
                        explanation = $"Over-activated: z = {z:+0.00;-0.00;0.00} >= 0. Output is 1, expected 0. Decrease weights or bias.";
                    }
                    else if (puzzle.Scoring == ScoringMode.Continuous)
                    {
                        explanation = DescribeContinuousMiss(outputs, tc, puzzle, z);
                    }
                    else
                    {
                        explanation = $"Output {predicted:F2} did not classify to expected target {target:F0}.";
                    }
                }

                diagnostics.Add(new CaseDiagnostic(
                    inputs: inputs,
                    expectedOutput: target,
                    calculatedZ: z,
                    actualOutput: predicted,
                    isCorrect: isCorrect,
                    label: tc.Label,
                    explanation: explanation
                ));
            }

            double accuracy = puzzle.Scoring == ScoringMode.Binary && puzzle.RequiredOutputs == 1
                ? LossFunctions.BinaryAccuracy(predictions, targets, threshold: 0.5)
                : (double)correctCount / totalCases;
            bool passed = (accuracy >= puzzle.AccuracyThreshold) && activationMatches;

            string summary;
            if (!activationMatches)
            {
                summary = $"Incompatible activation module: '{activeActivation}'. Insert '{puzzle.RequiredActivation}' crystal.";
            }
            else if (passed)
            {
                summary = $"Harmonic Convergence achieved! All {totalCases}/{totalCases} cases passed with 100% accuracy.";
            }
            else
            {
                summary = $"Calibration incomplete: {correctCount}/{totalCases} test cases verified ({accuracy * 100:F0}%).";
            }

            return new PuzzleEvaluation(
                passed: passed,
                accuracy: accuracy,
                totalCases: totalCases,
                passedCases: correctCount,
                activationMatches: activationMatches,
                activeActivation: activeActivation,
                requiredActivation: puzzle.RequiredActivation,
                diagnostics: diagnostics,
                summary: summary
            );
        }

        private static bool OutputsMatch(double[] outputs, TestCase tc, PuzzleDefinition puzzle)
        {
            for (int o = 0; o < outputs.Length; o++)
            {
                double expected = tc.GetExpectedOutput(o);
                if (puzzle.Scoring == ScoringMode.Continuous)
                {
                    if (System.Math.Abs(outputs[o] - expected) > puzzle.ToleranceFor(o))
                    {
                        return false;
                    }
                }
                else
                {
                    double binary = outputs[o] >= 0.5 ? 1.0 : 0.0;
                    if (binary != expected)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string DescribeContinuousMiss(double[] outputs, TestCase tc, PuzzleDefinition puzzle, double z)
        {
            for (int o = 0; o < outputs.Length; o++)
            {
                double expected = tc.GetExpectedOutput(o);
                double error = System.Math.Abs(outputs[o] - expected);
                double tolerance = puzzle.ToleranceFor(o);
                if (error > tolerance)
                {
                    return $"Output {o} is {outputs[o]:F2}, target {expected:F2}, tolerance {tolerance:F2}. z = {z:+0.00;-0.00;0.00}.";
                }
            }

            return "Continuous outputs missed their tolerances.";
        }
    }
}
