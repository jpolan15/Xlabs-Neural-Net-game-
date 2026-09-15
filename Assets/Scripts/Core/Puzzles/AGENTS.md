# Puzzle Rules & Evaluation Agent

## Mission

Evaluate player-constructed network configurations against rigorous multi-case test suites, enforce physical facility constraints, and generate informative diagnostic failure reports.

## Owns

- `TestCase`: Input vector, expected target output vector, tolerance threshold, and operational label.
- `PuzzleDefinition`: Set of training test cases, unseen generalization test cases, energy/overload thresholds, allowed components, and hint progression.
- `PuzzleEvaluator`: Evaluates a `NetworkModel` against a `PuzzleDefinition`.
- `DiagnosticReport`: Detailed telemetry returned on failure (which test case failed, predicted vs target values, clamp status, dominant error contributor).
- `MasteryChallenge`: Optional constraints (e.g. zero bias, minimal neuron count, strict loss threshold).

## Allowed Dependencies

- `Convergence.Core.Math`
- `Convergence.Core.Neural`
- `System`
- `System.Collections.Generic`

## Forbidden Dependencies

- `UnityEngine`
- `MonoBehaviour`
- XR or Visual scripts

## Diagnostic Rules

Failure reports must NEVER be a generic "Try Again". The evaluator must diagnose:
1. **Failing Index**: Identify the exact test case that diverged.
2. **Polarity Check**: Indicate whether the signal had opposite sign of the requirement.
3. **Activation Clamp**: Report if a negative signal was zeroed by ReLU.
4. **Energy Overload**: Report if pre-activation energy exceeded facility reactor safety bounds.
5. **Generalization Gap**: Report if the player's weights overfit training cases but failed the unseen validation case.

## Testing Requirements

- Verify evaluator correctly flags passing models.
- Verify evaluator correctly pinpoints failing test cases.
- Verify generalization tests catch overfitted memorization.

## Completion Report

Report:
- Evaluator features added.
- Diagnostic conditions tested.
- Unit test suite results.
