# Task: Ride v4 P6, Stop 4, Tokens / Word Space

Status: ready
Owner: Gameplay / XR / Presentation / Tools
Created: 2026-10-20
Updated: 2026-10-20

## Description

Phase P6 of Ride v4 (ADR-016, `Documentation/Architecture/ADRs/ADR-016-ride-v4-neuron-to-agents.md`). One PR per phase, branched from `jonathan/polishing-level-1`. Do P3 to P8 one at a time.

## Allowed paths

- `Assets/Scripts/Gameplay/Ride/**`
- `Assets/Scripts/XR/Ride/**`
- `Assets/Scripts/Presentation/Ride/**`
- `Tools/Editor/NeuralRideBuilder*.cs`
- `Tests/EditMode/Ride/**`
- `Tests/PlayMode/**`
- Assets/_Project/Audio/** (new narration lines only)

## Forbidden paths

- Anything not listed above. `Assets/Scenes/NeuralRide.unity` is only ever written by the builder.

## Acceptance criteria

- [ ] sort the stars, carry the arrow, beat the clock; `AnalogyPuzzle` 3/3
- [ ] Clip → Handoff → Minigame → Payoff; win decided only by Core
- [ ] Nobody touching anything still reaches the end (timeouts), except GO
- [ ] PlayMode tests for the stop
- [ ] Captures at 1.15 / 1.30 / 1.45 m (clip moment + minigame moment)
- [ ] `RideBudget` lower than before this phase; `RideSightlines` green; orange only on grabbables

## Blocked on

The previous phase being merged.

## Handoff notes

Report every command and its exact output. Say plainly what was not verified (headset, ears, hands).
