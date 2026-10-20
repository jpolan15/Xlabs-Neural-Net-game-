# Task: Ride v4 P0, Plan, ADR-016 and the palette fix

Status: complete
Owner: Docs / Presentation theme
Created: 2026-10-20
Updated: 2026-10-20

## Description

Phase P0 of Ride v4 (ADR-016, `Documentation/Architecture/ADRs/ADR-016-ride-v4-neuron-to-agents.md`). One PR per phase, branched from `jonathan/polishing-level-1`. Do P3 to P8 one at a time.

## Allowed paths

- `Documentation/Architecture/ADRs/ADR-016-ride-v4-neuron-to-agents.md`
- `.agents/**`
- `Assets/Scripts/Presentation/Ride/RideTheme.cs`
- `Assets/ScriptableObjects/RideTheme.asset`
- `Assets/Shaders/NeuralLinks.shader`

## Forbidden paths

- Anything not listed above. `Assets/Scenes/NeuralRide.unity` is only ever written by the builder.

## Acceptance criteria

- [x] ADR-016 written and listed in `DECISIONS_INDEX.md`
- [x] One task file per phase (P0 to P9) with allowed paths
- [x] Minus/drain yellow is `#FFEB00` in `RideTheme` and `NeuralLinks` (materials refresh on the next **Build Neural Ride**)
- [x] `Validate-RepositoryLayout.ps1` green

## Blocked on

None

## Handoff notes

Report every command and its exact output. Say plainly what was not verified (headset, ears, hands).
