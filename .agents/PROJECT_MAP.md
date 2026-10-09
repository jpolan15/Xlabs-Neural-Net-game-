# Project Map — Convergence

Read this before scanning the repository. Each entry is a one-line description of the directory's purpose.

Last updated: 2026-09-14 (Phase 5 + 6 complete)

---

## Top-level directories

| Path | Purpose |
|---|---|
| `.agents/` | Agent-only: workflow, task tracking, decision index, templates, skills |
| `.claude/` | Claude Code config: `agents/` (Asset Forge subagents), `skills/` (entry points to `.agents/skills/`) |
| `ArtSource/` | Source art outside Unity's import: concept art, raw/clean meshes, verify renders, voice bake-off, the narration pipeline (`voice/`), archival clips (`archival/`), effect derivation (`sfx/`) (see `ArtSource/AGENTS.md`) |
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
| `.agents/tasks/` | Active task files: BACKLOG, BLOCKED, COMPLETED, IN_PROGRESS |
| `.agents/templates/TASK_TEMPLATE.md` | Standard task file format |
| `.agents/templates/HANDOFF_TEMPLATE.md` | Standard task report / handoff format |
| `.agents/skills/unity-cli/` | Unity CLI skill: drive the open Editor, capture, test, build |
| `.agents/skills/asset-forge/` | Concept art → multi-view → 3D → verified Unity import (gated pipeline) |

---

## `Assets/`

| Path | Purpose |
|---|---|
| `Assets/_Project/` | First-party assets: Audio, Materials, Prefabs, Scenes, ScriptableObjects |
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
| `Assets/Scripts/Gameplay/` | MonoBehaviour orchestration — chamber state, blast doors, facility. `Ride/` is the Neural Ride director (intro, briefings, outro, skip), `RideScript`, and stations |
| `Assets/Scripts/XR/` | XR input translation — converts interaction to gameplay commands |
| `Assets/Scripts/XR/Ride/` | Pod lever, pod seat (eye-height normalisation), and desktop fallback with operator keys: Space skips narration, R restarts (ADR-011) |
| `Assets/Scripts/XR/Meta/` | Meta-specific adapters only (isolated from Core and Gameplay) |
| `Assets/Scripts/Presentation/` | Visualization, audio, haptics — observes state, never decides it. `Ride/` holds the ride views, `NarrationLibrary`, `ChapterCardView` and `NeuralCoreView` (the big network) |
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
| `Tools/Editor/` | Unity Editor tooling: validation, build scripts, project map generation. The ride builder is split into partial files: `NeuralRideBuilder.cs` (pod, station, scene), `.Pod.cs` (tub, layout helpers), `.Narration.cs`, `.Network.cs`; plus `RideNarrationData.cs` (the script), `RideSightlines.cs` (sightline, cone and text check, and the draw-call report), `RideSeatCaptures.cs` and `RideLiveShot.cs` (screenshots) |
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
