# Task: Ride v4 P9, Polish from the v3.0 spec

Status: ready
Owner: Tools / Presentation / Config
Created: 2026-10-20
Updated: 2026-10-20

## Description

Phase P9 of Ride v4 (ADR-016, `Documentation/Architecture/ADRs/ADR-016-ride-v4-neuron-to-agents.md`). One PR per phase, branched from `jonathan/polishing-level-1`. Do P3 to P8 one at a time.

## Allowed paths

- `Tools/Editor/NeuralRideBuilder*.cs`
- `Assets/Shaders/**`
- `Assets/Scripts/Presentation/Ride/**`
- `Assets/Scripts/XR/Ride/**`
- ProjectSettings/EditorBuildSettings.asset (Level01 off the list only)

## Forbidden paths

- Anything not listed above. `Assets/Scenes/NeuralRide.unity` is only ever written by the builder.

## Acceptance criteria

- [ ] Audio import: music/archival Streaming + Vorbis 0.70; voice CompressedInMemory + loadInBackground; SFX DecompressOnLoad + loadInBackground
- [ ] One shared Mesh per shape in .Network / .Pod / .Explainer; instancing on materials
- [ ] Shader polish (NeuralLinks bloom/pulse/depth fade, NeuralNodes Fresnel + nucleus, HoloLit fwidth contours)
- [ ] Detent teeth at −1, 0, +0.5, +1, +1.5 with haptics
- [ ] Level01 off the build list, files and tests kept; `Resources/` untouched
- [ ] `RideBudget` ≤ 100 at every stop; Play Mode entry time measured before/after

## Blocked on

The previous phase being merged.

## Handoff notes

Report every command and its exact output. Say plainly what was not verified (headset, ears, hands).
