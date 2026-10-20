# Task: Ride v4 P2, Clip pipeline: 6 Manim clips, ClipScreenView, handoff

Status: ready
Owner: Art / Presentation / Gameplay / Tools
Created: 2026-10-20
Updated: 2026-10-20

## Description

Phase P2 of Ride v4 (ADR-016, `Documentation/Architecture/ADRs/ADR-016-ride-v4-neuron-to-agents.md`). One PR per phase, branched from `jonathan/polishing-level-1`. Do P3 to P8 one at a time.

## Allowed paths

- `ArtSource/clips/**`
- `Assets/_Project/Video/**`
- `Assets/Scripts/Presentation/Ride/ClipScreenView.cs`
- `Assets/Scripts/Presentation/Ride/ClipHandoffView.cs`
- `Assets/Scripts/Gameplay/Ride/RideTypes.cs`
- `Assets/Scripts/Gameplay/Ride/StationController.cs`
- `Assets/Scripts/Gameplay/Ride/RideScript.cs`
- `Tools/Editor/NeuralRideBuilder*.cs`
- `Tests/PlayMode/**`
- Packages/manifest.json (only `com.unity.modules.video`, per ADR-016)
- `ArtSource/AGENTS.md`, `.agents/PROJECT_MAP.md` (list the two new folders)

## Forbidden paths

- Anything not listed above. `Assets/Scenes/NeuralRide.unity` is only ever written by the builder.

## Acceptance criteria

- [ ] 6 clips (`01_neuron` to `06_agent`), ~20 s, H.264, ≤ 2 MB, palette per ADR-016, never orange
- [ ] `StationPhase.Clip` waits on clip seconds from `RideScript`; Space / 2 s GO hold skips
- [ ] Clip screen renderer off outside `Clip` (0 draw calls in minigames)
- [ ] All 6 clips play in Play Mode at each stop; `RideBudget` not worse

## Blocked on

None

## Handoff notes

Report every command and its exact output. Say plainly what was not verified (headset, ears, hands).
