# Task: Opening impact sequence

Status: complete
Owner: Presentation
Created: 2026-10-03
Updated: 2026-10-03

## Description

Play the asteroid strike that opens Level 1. On `OnOpeningBriefing`: boom, red lights, sparks, axons dark around 5–7s, the asteroid field slides in around 10–12s, and four alarm pulses. No camera shake. Lights turn blue on `OnRestored`. Cables start disconnected because of the opening preset.

## Allowed paths

- `Assets/Scripts/Presentation/ShipImpactSequence.cs`
- `Assets/Scripts/Presentation/NeuronMachineVisual.cs`
- `Assets/Scripts/Presentation/RetroAudioSynthesizer.cs`
- `Assets/Scripts/Presentation/FloatingAsteroidField.cs`
- `Assets/Scripts/Presentation/ShipDamageVisual.cs`

## Forbidden paths

- `Assets/Scripts/Core/**`
- `Assets/Scripts/Gameplay/**`
- `Assets/Scripts/XR/**`
- `ProjectSettings/**`
- `Packages/**`

## Acceptance criteria

- [ ] The strike is driven only by `OnOpeningBriefing`.
- [ ] The camera never moves.
- [ ] Lights turn blue on `OnRestored`.
- [ ] Unity console shows no compile errors.

## Blocked on

None.

## Handoff notes

Wiring lives in `TASK_BRIDGE_DECLUTTER.md`.
