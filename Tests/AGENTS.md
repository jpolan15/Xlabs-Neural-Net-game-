# Tests — Scope Rules

This directory contains all automated tests for the Convergence project.  
Test code does not ship in the game binary.

## Directory layout

| Path | Purpose | Runner |
|---|---|---|
| `Tests/EditMode/` | Pure C# and Core layer unit tests | Unity Test Runner (EditMode) |
| `Tests/EditMode/Core/` | `Core.Math`, `Core.Neural`, `Core.Training`, `Core.Puzzles` | Unity Test Runner / headless |
| `Tests/EditMode/Puzzles/` | Puzzle evaluation and diagnostic tests | Unity Test Runner |
| `Tests/PlayMode/` | Scene, Gameplay, and XR integration tests | Unity Test Runner (PlayMode) |
| `Tests/PlayMode/Gameplay/` | Chamber state machine and facility tests | Unity Test Runner |
| `Tests/PlayMode/XR/` | Interaction event and locomotion tests | Unity Test Runner |
| `Tests/AgentScenarios/` | Smoke tests driven by Meta XR Operator (optional) | XR Operator via MCP |
| `Tests/Results/` | Test result XML output (do not commit) | — |

## Rules

- Do not delete a failing test to make validation pass.
- Do not claim a test passed without running it.
- Tests in `Tests/EditMode/Core/` must run without the Unity Editor.
- Tests in `Tests/PlayMode/` must not require a headset.
- `Tests/AgentScenarios/` tests require XR Operator; skip gracefully when unavailable.
- New Core features require EditMode tests before the feature is considered complete.
- Test assembly names: `Convergence.Tests.EditMode`, `Convergence.Tests.PlayMode`.

## Test strategy

See `Documentation/Testing/TEST_STRATEGY.md` for the full three-tier strategy, invariant list, and validation commands.

## Migration note

Tests previously located in `Assets/Scripts/Tests/` are being migrated here. Do not add new tests to the old location.
