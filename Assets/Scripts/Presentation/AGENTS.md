# Presentation Layer Rules

Presentation observes state and displays it. It must not decide puzzle correctness.  
Assembly: `Convergence.Presentation`.

> **Migration note**: Code currently in `Assets/Scripts/Visualization/` and `Assets/Scripts/Audio/` will be consolidated here as those directories are migrated. Do not add new visualization or audio code to the old locations.

## Allowed

- `MonoBehaviour`, `ScriptableObject`, scene references
- Reading `PuzzleEvaluation` results to drive visuals
- `UnityEngine.Audio`, particle systems, shaders, splines
- Haptic dispatch via XRI (`SendHapticImpulse`)
- Subscribing to C# events from Gameplay layer
- TextMeshPro, UI Toolkit

## Forbidden

- Calling `PuzzleEvaluator.Evaluate()` — Presentation never triggers evaluation
- Modifying Core layer state
- Making chamber pass/fail decisions
- Direct `Meta.*` or `Oculus.*` SDK calls
- Driving gameplay state machines

## Rules

- Presentation reacts to events and state; it never initiates them.
- All visual parameters (colors, thresholds, display ranges) must be in ScriptableObjects, not hardcoded.
- No per-frame GC allocations in `Update` paths.

## Tests

Tests for Presentation behavior: `Tests/PlayMode/` (visual state reactions only).
