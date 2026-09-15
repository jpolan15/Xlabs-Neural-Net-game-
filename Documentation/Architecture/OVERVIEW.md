# Runtime Architecture Overview — Convergence

This document describes the **shipped game's** runtime architecture only.  
Development tooling, Meta XR Operator, MCP, and the XR Simulator are not part of the runtime. They are documented in `Documentation/Operations/`.

---

## Design goal

The player experiments, makes predictions, observes failure, and improves the network.

Core principle: **never silently fake a result to open a door.**

Every door state change must flow through:

```
XR interaction
    ↓
Gameplay command
    ↓
Core model update
    ↓
Puzzle evaluator  (real mathematics)
    ↓
PuzzleEvaluation result
    ↓
Gameplay state transition
    ↓
Presentation (visualization / audio / haptics)
```

---

## Layer diagram

```
┌──────────────────────────────────────────────────────┐
│             Core Layer  (Pure C# — no Unity)         │
│                                                      │
│  Core.Math → Core.Neural → Core.Training             │
│                          → Core.Puzzles              │
└───────────────────────────────┬──────────────────────┘
                                │ telemetry events / result structs
                                ▼
┌──────────────────────────────────────────────────────┐
│                    Gameplay Layer                    │
│  Chamber state machines, facility manager,           │
│  blast doors, power grid, journal                    │
└───────────┬──────────────────────────┬───────────────┘
            │ commands                 │ state / results
            ▼                         ▼
┌─────────────────┐        ┌──────────────────────────┐
│   XR Layer      │        │   Presentation Layer      │
│  Interactors    │        │  Visualization, Audio,    │
│  Locomotion     │        │  Haptics                  │
│  Input mapping  │        │  (observes; never decides)│
└─────────────────┘        └──────────────────────────┘
```

---

## Assembly graph

```
Convergence.Core.Math
        ↓
Convergence.Core.Neural
        ↓
Convergence.Core.Training
        ↓
Convergence.Core.Puzzles
        ↓
Convergence.Gameplay
       ↙         ↘
Convergence.XR   Convergence.Presentation
```

`Convergence.EditorTools` — Editor-only; no direction in the runtime graph.

Full dependency rules: `Documentation/Architecture/DEPENDENCY_RULES.md`  
What ships vs. what does not: `Documentation/Architecture/RUNTIME_BOUNDARIES.md`

---

## Puzzle contract

Every chamber uses the same evaluator contract:

```csharp
public interface IPuzzleEvaluator<TInput, TResult>
{
    TResult Evaluate(TInput input);
}

public readonly record struct PuzzleEvaluation(
    bool Passed,
    double Score,
    string FailureReason
);
```

The evaluator returns real mathematics. The `FailureReason` identifies which test case failed and why — not just a scalar.

---

## Chambers

| Chamber | Concept taught | Loss function |
|---|---|---|
| Chamber 01 — Weighted Sum | Weighted sums, bias, activation | MSE |
| Chamber 02 — XOR | Binary classification, hidden layers | Binary Cross-Entropy |
| Chamber 03 — Training | Gradient descent, loss landscape | MSE / BCE (per sub-puzzle) |
| Chamber 04 — Attention | Attention, contextual prediction | Categorical Cross-Entropy |

Mathematical definitions: `Documentation/Mathematics/FORMULAS.md`

---

## Design rules

- Core must compile without Unity DLLs.
- Gameplay must not implement neural-network mathematics.
- XR must not evaluate puzzles.
- Presentation must not decide puzzle correctness.
- No layer may hardcode puzzle success.
- Diagnostic output must identify the failing test case and contributing factor.
