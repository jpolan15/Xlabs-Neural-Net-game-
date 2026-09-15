# ADR-003: Multi-Case Diagnostics Instead of Single Scalar Loss

| Field | Value |
|---|---|
| ID | ADR-003 |
| Date | 2026-09-10 |
| Status | Accepted |
| Deciders | Architecture, Gameplay |

## Context

Showing a single scalar loss value (e.g. `Loss: 0.42`) does not give the player actionable feedback on why their network failed. Players resort to blind guessing rather than hypothesis-driven adjustment, which undermines the educational goal of the game.

## Decision

Every puzzle evaluates against a multi-case test suite. The `PuzzleEvaluation` result includes:
- whether the overall puzzle passed,
- a numeric score,
- a human-readable `FailureReason` that identifies:
  - which specific test case failed,
  - whether a ReLU clamped a negative pre-activation,
  - which weight or bias contributed most to the error,
  - whether pre-activation energy tripped the reactor breaker.

## Puzzle contract

```csharp
public readonly record struct PuzzleEvaluation(
    bool Passed,
    double Score,
    string FailureReason
);
```

`FailureReason` must never be an empty string when `Passed` is false.

## Consequences

- Promotes genuine scientific inquiry and hypothesis testing.
- Players can diagnose and adapt rather than guess.
- Deeper alignment with how real deep learning debugging works.
- Requires each chamber to define a test suite, not just a target value.
- Puzzle data files must include multiple test cases with expected outputs.

## Supersedes

Nothing.
