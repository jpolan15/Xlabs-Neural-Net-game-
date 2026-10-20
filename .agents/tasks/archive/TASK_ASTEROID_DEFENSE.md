# Task: Asteroid point-defense gameplay (Level 1)

Status: complete
Owner: Gameplay
Created: 2026-10-03
Updated: 2026-10-03

## Description

Make the Chamber 01 neuron the brain of the ship's automatic point-defense laser (ADR-010). Add a tick-driven `AsteroidDefenseDirector` that launches waves of the four OR-case objects (Repair Drone, Icy Comet, Rocky Asteroid, Rock-and-Ice Chunk), scans each one through the chamber, and turns the honest `PuzzleEvaluator` result into vaporize / dock / breach / friendly-fire outcomes. Add the `ChamberController`, `DataTargetReceptor`, and `GatewayController` hooks the director needs, without breaking the existing behavior when the new flags are off. Full design and the numbered findings are in `HANDOFF_ASTEROID_DEFENSE.md`, Steps 2 and 5.

## Allowed paths

- `Assets/Scripts/Gameplay/AsteroidDefenseDirector.cs` (new)
- `Assets/Scripts/Gameplay/ChamberController.cs`
- `Assets/Scripts/Gameplay/DataTargetReceptor.cs`
- `Assets/Scripts/Gameplay/GatewayController.cs`
- `Assets/Scripts/Gameplay/ChamberOnboardingController.cs` (hint text and the Completed guard only; the opening event belongs to `TASK_OPENING_RECORDING.md`)
- `Assets/Scripts/Gameplay/FailureHintDirector.cs`
- `Tests/PlayMode/Gameplay/AsteroidDefenseTests.cs` (new)
- `Tests/PlayMode/Gameplay/Level01IntegrationTests.cs` (only if a test must be adapted to a new default; never delete a test)
- `.meta` files Unity generates for the new scripts

## Forbidden paths

- `Assets/Scripts/Core/**`
- `Assets/Scripts/XR/**`
- `Assets/Scripts/Presentation/**` (see `TASK_DEFENSE_FEEDBACK.md`)
- `Assets/Scripts/Gameplay/VoyageDirector.cs` (known limitation, report it only)
- `ProjectSettings/**`, `Packages/**`
- Any `.asmdef` file

## Acceptance criteria

- [ ] With every new flag at its default, existing behavior and `Level01IntegrationTests` are unchanged.
- [ ] `ChamberController.SetExternalWaveDirector(true)` stops the built-in wave loop and skips `EvaluateTacticalOutcomes`.
- [ ] Soft reroute: at 0 hull with `enableSoftReroute`, hull goes to `rerouteRestoreFraction` (0.6) of max, `OnHullRerouted` fires, no purge, network settings kept.
- [ ] `ReplaySingleCase(int, PuzzleDefinition)` evaluates honestly against the given puzzle and never changes `_hasSolved`, `LastEvaluation`, the phase, or fires `OnEvaluationComplete` / `OnPuzzleSolved`.
- [ ] `DataTargetReceptor` has `IsInFlight`, `Launch()`, `Retire()` (default in flight for back-compat). `ResetReceptor` does not touch `IsInFlight`. Default titles: Repair Drone, Icy Comet, Rocky Asteroid, Rock-and-Ice Chunk.
- [ ] `GatewayController.deferOpenToDirector` (default false) suppresses the open on `OnPuzzleSolved`.
- [ ] Director: `Tick(float dt)` is public and `Update` only calls it. Outcomes are classified from `CaseDiagnostic.ActualOutput >= 0.5` versus `ExpectedOutput`. No case index is hardcoded as a threat.
- [ ] Director captures the OR puzzle in `Start` and uses it for the victory swarm even after `VoyageDirector` swaps in XOR.
- [ ] Onboarding does not re-enter `Completed` on every passing scan. Hint and failure strings use ROCK / ICE / FIRE language and never state a weight value.
- [ ] New PlayMode tests (listed in the handoff, Step 5) pass. Existing PlayMode and EditMode tests still pass.
- [ ] `pwsh Tools/Validation/Validate-CoreBoundaries.ps1` exits 0.
- [ ] No forbidden dependencies introduced.
- [ ] Handoff report filed.

## Blocked on

None. `HANDOFF_ASTEROID_DEFENSE.md` Step 1 (process files) is complete.

## Handoff notes

Presentation subscribes to the director's events (`OnWaveStarted`, `OnObjectLaunched`, `OnScan`, `OnOutcome`, `OnVictoryStarted`, `OnVictoryComplete`, `OnRestored`) and to `ChamberController.OnHullRerouted`. Keep those signatures stable once published; `TASK_DEFENSE_FEEDBACK.md` and `TASK_OPENING_IMPACT.md` depend on them. The scene builder wiring is `TASK_BRIDGE_DECLUTTER.md`.
