# Gameplay Layer Rules

Gameplay coordinates Core systems and Unity scene objects.  
Assembly: `Convergence.Gameplay`.

## Allowed

- `MonoBehaviour` and `ScriptableObject`
- Scene references and `GameObject` hierarchy
- Unity events and C# events
- References to all `Convergence.Core.*` assemblies
- State machines and chamber flow logic
- Reading results from `PuzzleEvaluation` to drive state transitions

## Forbidden

- Reimplementing neural-network mathematics — always call `PuzzleEvaluator.Evaluate()`
- Direct `Meta.*` or `Oculus.*` SDK calls — use adapter events from `XR/Meta/`
- Reading controller devices or input actions directly — XR layer sends commands
- Deciding visual styling, materials, or shader parameters — delegate to Presentation
- Calling network or cloud services directly

## Event contract

Gameplay **receives** commands from XR:
- `OnWeightChanged(float value)`
- `OnCableConnected(SocketId socket)`
- `OnActivationInserted(ActivationType type)`
- `OnForwardPassTriggered()`

Gameplay **sends** results to Presentation:
- `PuzzleEvaluation result` (via C# event or interface)
- Chamber state changes (`Investigating`, `Testing`, `Diagnosing`, `Converged`, `OverloadTrip`)

Gameplay must not directly manipulate Presentation transforms, materials, or audio sources.

## Required tests

- Chamber advances state only on `PuzzleEvaluation.Passed = true`.
- Chamber does not advance on partial or incorrect solutions.
- Reactor overload trip resets safely.
- Journal unlocks only on verified mastery.

Tests location: `Tests/PlayMode/Gameplay/`


## Owns

- `ChamberController`: State machine managing individual chamber lifecycle (`Investigating`, `Testing`, `Diagnosing`, `Converged`, `OverloadTrip`).
