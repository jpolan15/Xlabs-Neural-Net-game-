# Project Map — Convergence

Read this before scanning the repository. Each entry is a one-line description of the directory's purpose.

Last updated: 2026-10-21 (ride feel pass: table ergonomics, fingertips, wire dragging, Pixel petting, boot fade, ADR-019)

---

## Top-level directories

| Path | Purpose |
|---|---|
| `.agents/` | Agent-only: workflow, task tracking, decision index, templates, skills |
| `.claude/` | Claude Code config: `agents/` (Asset Forge subagents), `skills/` (entry points to `.agents/skills/`) |
| `ArtSource/` | Source art outside Unity's import: concept art, raw/clean meshes, verify renders, voice bake-off, the narration pipeline (`voice/`), archival clips (`archival/`), effect derivation (`sfx/`), the Level 1 MNIST bake (`training/`) (see `ArtSource/AGENTS.md`) |
| `Assets/` | All Unity asset content for the shipped game |
| `Builds/` | Release agent output; not committed to source control |
| `Documentation/` | Canonical architecture, mathematics, testing, and operations docs |
| `Packages/` | Unity package manifest and lock file |
| `ProjectSettings/` | Unity project settings (modify only with Config Agent approval) |
| `Tests/` | All automated tests — EditMode, PlayMode, AgentScenarios |
| `Tools/` | Development-only tooling: validators, build scripts, AgentBridge |

---

## `.agents/`

| Path | Purpose |
|---|---|
| `.agents/PROJECT_MAP.md` | This file |
| `.agents/WORKFLOW.md` | Agent operating procedure (before/during/after) |
| `.agents/DECISIONS_INDEX.md` | Index table linking to ADRs in Documentation/Architecture/ADRs/ |
| `.agents/tasks/` | Active task files: Level 1 plan + handoff, BACKLOG, BLOCKED, COMPLETED, IN_PROGRESS |
| `.agents/tasks/archive/` | Finished or superseded tasks and handoffs (history only, don't load) |
| `.agents/templates/TASK_TEMPLATE.md` | Standard task file format |
| `.agents/templates/HANDOFF_TEMPLATE.md` | Standard task report / handoff format |
| `.agents/skills/asset-forge/` | Concept art → multi-view → 3D → verified Unity import (gated pipeline) |

---

## `Assets/`

| Path | Purpose |
|---|---|
| `Assets/_Project/` | First-party assets: Audio, Materials, Prefabs, Scenes, ScriptableObjects |
| `Assets/_Project/Data/Level1/` | `level1_digits.json`: the baked Level 1 network (both weight sets), hero digit, puzzle, Valley ribbon and finale digits, written by `ArtSource/training/bake_level1.py` |
| `Assets/_Project/Audio/` | **Canonical** audio assets location — see `Assets/_Project/Audio/AGENTS.md`. Ride: `Voice/vo_ride_NN.wav` (+ `.txt` sidecars), `Sfx/` (derived loops and buzzer), `Kenney/` (CC0 effects), `Music/` |
| `Assets/AudioAssets/` | **Deprecated** — redirect to `Assets/_Project/Audio/` (no files; only AGENTS.md) |
| `Assets/Materials/` | Shared materials. `NeuralRide/` holds the ride's materials (HoloLit solids, unlit icons and lines, additive glows, the two network materials) |
| `Assets/Shaders/` | Hand-written URP shaders for the Neural Ride: `HoloLit`, `NeuralLinks`, `NeuralNodes` (ADR-013) |
| `Assets/Prefabs/` | Shared prefabs. `NeuralRide/` holds Pod, Station, and Track (built by `Tools/Editor/NeuralRideBuilder*.cs`); `NeuralRide/Meshes/` holds the generated tub and network meshes |
| `Assets/Puzzles/` | Per-chamber puzzle data: test cases, rules, configuration |
| `Assets/Scenes/` | Unity scene files. `NeuralRide.unity` is build scene 0 (ADR-011); `Level01_AwakeningGate.unity` is kept |
| `Assets/ScriptableObjects/` | Shared ScriptableObject data. `RideTheme`, `RideScript` (narration pacing) and `NarrationLibrary` (subtitles, clips, chapter cards) are written by the ride builder |
| `Assets/Samples/` | SDK sample imports (third-party; do not modify) |
| `Assets/ThirdParty/` | Third-party plugin assets (do not modify) |

## `Assets/Scripts/`

| Path | Purpose |
|---|---|
| `Assets/Scripts/Core/` | Engine-independent C# — Math, Neural, Training, Puzzles |
| `Assets/Scripts/Gameplay/` | MonoBehaviour orchestration — chamber state, blast doors, facility. `Ride/` is the Neural Ride segment director, `RideScript` and moments; `RideGame` is the mini-game base (score, stars, win, pull GO), `ZapGame` the noise specks; `Ride/Level1/` loads the bake (`Level1Data`) and holds Level 1's moments (draw, weight paint, plug, forward pass, blame hunt, descent, valley run) |
| `Assets/Scripts/XR/` | XR input translation — converts interaction to gameplay commands |
| `Assets/Scripts/XR/Ride/` | Hand lasers (`RideHandLasers`: trigger/A fires, right green / left red), `HandPolarityController` (hand colour, fingertip orb and touch trigger), pod lever, pod seat (eye-height normalisation), and desktop fallback with operator keys: Space skips narration, R restarts (ADR-011) |
| `Assets/Scripts/XR/Meta/` | Meta-specific adapters only (isolated from Core and Gameplay) |
| `Assets/Scripts/Presentation/` | Visualization, audio, haptics — observes state, never decides it. `Ride/` holds the ride views, `NarrationLibrary`, `PixelBuddyView` (the buddy, pettable), `BlameView` (the wires you drag to the 3), `RideBoot` (black fade + warm-up before the ride starts); `Ride/Explainers/` the explainer kit (Rate, Tween, Anim, ShapeBatch) and E0–E8 (ADR-017) |
| `Assets/Scripts/Infrastructure/` | Cross-cutting utilities: logging, events, telemetry contracts |

> Note: `Assets/Scripts/Visualization/` and `Assets/Scripts/Audio/` are being consolidated into `Assets/Scripts/Presentation/` in a future migration task. Do not move files yet.

---

## `Documentation/`

| Path | Purpose |
|---|---|
| `Documentation/Architecture/` | Runtime game architecture: layers, dependency rules, boundaries |
| `Documentation/Architecture/ADRs/` | Canonical Architecture Decision Records |
| `Documentation/Design/` | Game design documents |
| `Documentation/Mathematics/` | Canonical mathematical definitions and formulas |
| `Documentation/Operations/` | Development tooling: desktop workflow, headset workflow, XR Operator |
| `Documentation/Testing/` | Test strategy and requirements |
| `Documentation/Technical/` | Legacy technical docs (being migrated to Architecture/) |

---

## `Tests/`

| Path | Purpose |
|---|---|
| `Tests/EditMode/` | Pure C# and Core layer tests; no headset required |
| `Tests/EditMode/Core/` | Core.Math, Core.Neural, Core.Training, Core.Puzzles tests |
| `Tests/EditMode/Puzzles/` | Puzzle evaluation tests |
| `Tests/PlayMode/` | Scene, Gameplay, and XR tests; requires Unity playmode |
| `Tests/PlayMode/Gameplay/` | Chamber state machine and facility tests |
| `Tests/PlayMode/XR/` | Interaction and locomotion tests |
| `Tests/AgentScenarios/` | Smoke tests driven by Meta XR Operator (development tooling only) |

---

## `Tools/`

| Path | Purpose |
|---|---|
| `Tools/Editor/` | Unity Editor tooling: validation, build scripts, project map generation. The ride builder is split into partial files: `NeuralRideBuilder.cs` (pod, station, scene), `.Pod.cs` (tub, layout helpers), `.Narration.cs`, `.Network.cs`, `.Explainers.cs` (E0–E8), `.Pixel.cs` (the buddy); plus `RideNarrationData.cs` (the script), `RideSightlines.cs` (sightline, cone and text check, and the draw-call report), `RideSeatCaptures.cs` and `RideLiveShot.cs` (screenshots), `RideModelSheets.cs` (every model from every side; the builder checks each model has a sheet) |
| `Tools/Validation/` | PowerShell validators for boundaries and repository layout |
| `Tools/AgentBridge/` | MCP / XR Operator integration; NOT a runtime dependency |

> Unity MCP (`unityMCP` server, package `com.coplaydev.unity-mcp`) is external development tooling configured in `.cursor/mcp.json` once installed; it is not under `Tools/`. See root `AGENTS.md`.

---

## Assembly graph

```
Convergence.Core.Math
        ↓
Convergence.Core.Neural
        ↓
Convergence.Core.Training
        ↓
Convergence.Core.Puzzles
        ↓
Convergence.Gameplay
        ↓
Convergence.XR
Convergence.Presentation
```

`Convergence.EditorTools` — Editor-only assembly; no runtime references.  
`Convergence.Tests.EditMode` — Editor-only test assembly (Core layer).  
`Convergence.Tests.PlayMode` — PlayMode test assembly (Gameplay + XR).

**Status**: All `.asmdef` files created (Phase 5 complete). Unity will generate `.meta` files on next project open.

---

## What does NOT ship in the game binary

- Meta XR Operator
- MCP server or client code
- Meta XR Simulator
- `Tools/` directory
- `Tests/` directory
- `.agents/` directory
- `Documentation/` directory
