# Test Strategy — Convergence

This document defines the three-tier test structure and platform requirements.  
Agents must run the narrowest tier that covers the change. Escalate only when necessary.

---

## Tier 1 — EditMode Tests (Core layer)

**Location**: `Tests/EditMode/`  
**Runner**: Unity Test Runner (EditMode) or headless `dotnet test`  
**Requires headset**: No  
**Requires Unity Editor**: No (Core is pure C#)

### Coverage required

Every public method in `Core.*` assemblies must have:
- Positive case (expected output for valid input)
- Negative case (expected output for invalid or edge input)
- Zero / identity case
- Boundary values (min, max, tolerance edges)

### Invariants that must always pass

- Every activation function handles $z = 0$, $z > 100$, and $z < -100$ without NaN or overflow.
- Softmax output probabilities always sum to $1.0 \pm 10^{-6}$.
- `PuzzleEvaluation.Passed = false` whenever any test case in the suite fails.
- `PuzzleEvaluation.FailureReason` is never an empty string when `Passed = false`.
- Diagnostic output correctly identifies clamped ReLU activations.
- BCE and CCE never produce NaN (log-clamp is applied).

### Commands

```powershell
# Via Unity Editor batch mode
& "unity-editor-path" -projectPath . -runTests -testPlatform EditMode -testResults Tests/Results/EditMode.xml
```

---

## Tier 2 — PlayMode Tests (Gameplay / XR)

**Location**: `Tests/PlayMode/`  
**Runner**: Unity Test Runner (PlayMode)  
**Requires headset**: No  
**Requires Unity Editor**: Yes

### Coverage required

- Chamber state transitions advance only on valid `PuzzleEvaluation.Passed = true`.
- Blast doors do not open before the evaluator returns a result.
- XR interaction events (`OnWeightChanged`, `OnCableConnected`, `OnActivationInserted`) fire and are received by Gameplay.
- Chamber reset returns all state to initial conditions.

### Commands

```powershell
& "unity-editor-path" -projectPath . -runTests -testPlatform PlayMode -testResults Tests/Results/PlayMode.xml
```

---

## Tier 3 — Agent Scenarios (XR Operator smoke tests)

**Location**: `Tests/AgentScenarios/`  
**Runner**: Meta XR Operator via MCP  
**Requires headset**: No (Simulator) or Yes (headset validation)  
**Required**: No — skip if XR Operator is unavailable

### Purpose

Smoke tests that confirm a running game session responds correctly to scripted interactions. These are run after Tier 2 passes, as the last pre-headset step.

### Coverage required

- Chamber 01: cable connection triggers weight update.
- Chamber 02: correct XOR solution unlocks door; incorrect solution triggers diagnostic alarm.
- Smoke test: facility loads without errors.

### Setup

See `Documentation/Operations/XR_OPERATOR.md`.

---

## Platform requirements summary

| Test | EditMode | PlayMode | Simulator | Quest 2 headset |
|---|---|---|---|---|
| Core math correctness | ✅ Required | — | — | — |
| Puzzle evaluation | ✅ Required | — | — | — |
| Chamber state machines | — | ✅ Required | — | — |
| XR interaction events | — | ✅ Required | — | — |
| Full interaction feel | — | — | ✅ Recommended | — |
| Reach / comfort | — | — | — | ✅ Required |
| Frame timing / thermals | — | — | — | ✅ Required |

---

## Validation commands (CI)

```powershell
# Core boundary check
pwsh Tools/Validation/Validate-CoreBoundaries.ps1

# Repository layout check
pwsh Tools/Validation/Validate-RepositoryLayout.ps1

# EditMode tests
& "unity-editor-path" -projectPath . -runTests -testPlatform EditMode

# PlayMode tests
& "unity-editor-path" -projectPath . -runTests -testPlatform PlayMode
```

All four must pass before merging changes to main.

---

## Rules

- Do not delete a failing test to make CI green.
- Do not claim a test passed if it was not run.
- Simulator results do not prove headset correctness. See `Documentation/Operations/HEADSET_WORKFLOW.md`.
