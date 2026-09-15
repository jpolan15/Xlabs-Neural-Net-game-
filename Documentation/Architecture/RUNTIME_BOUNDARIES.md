# Runtime Boundaries — Convergence

This document defines what is and is not part of the shipped game binary.

---

## What ships

| Component | Location | Notes |
|---|---|---|
| Core mathematics | `Assets/Scripts/Core/Math/` | Pure C#; no Unity |
| Neural simulation | `Assets/Scripts/Core/Neural/` | Pure C#; no Unity |
| Training algorithms | `Assets/Scripts/Core/Training/` | Pure C#; no Unity |
| Puzzle evaluators | `Assets/Scripts/Core/Puzzles/` | Pure C#; no Unity |
| Chamber gameplay logic | `Assets/Scripts/Gameplay/` | MonoBehaviour; references Core |
| XR interaction | `Assets/Scripts/XR/` | XRI 3.x + Unity Input System |
| Meta-specific adapters | `Assets/Scripts/XR/Meta/` | Isolated; optional at runtime |
| Presentation layer | `Assets/Scripts/Presentation/` | Visualization, audio, haptics |
| Scene content | `Assets/Scenes/` | Assembled by Scene Assembly Agent |
| Puzzle data | `Assets/Puzzles/` | Test cases and configuration |
| Audio assets | `Assets/AudioAssets/` | Current location (not yet migrated) |

---

## What does NOT ship

The following are **development-only** components. They must never be referenced by Core, Gameplay, XR, or Presentation assemblies.

| Component | Location | Purpose |
|---|---|---|
| Meta XR Operator | External tool | Experimental agent-driven testing; optional |
| Meta XR Simulator | External tool / Package | Desktop OpenXR runtime for iteration |
| MCP server / client | `Tools/AgentBridge/` | Connects coding agents to running sessions |
| Dependency validators | `Tools/Validation/` | CI/pre-commit enforcement |
| Build scripts | `Tools/Editor/Build/` | Release packaging |
| Project map generator | `Tools/Editor/ProjectMap/` | Generates `.agents/PROJECT_MAP.md` |
| EditMode tests | `Tests/EditMode/` | Pure unit tests |
| PlayMode tests | `Tests/PlayMode/` | Integration tests |
| Agent smoke tests | `Tests/AgentScenarios/` | XR Operator smoke scenarios |
| All documentation | `Documentation/` | Architecture, design, math, testing |
| Agent instructions | `.agents/` | Workflow, tasks, templates |

---

## Meta XR Operator — classification

Meta XR Operator is **experimental development tooling**. It:
- operates at the OpenXR layer,
- can inspect and control a running session in Editor, Simulator, or headset,
- is not required to build the game,
- is not required to run the game,
- is not required for Core, Gameplay, XR, or Presentation tests,
- must not be a dependency of any shipped assembly.

Reference: `Documentation/Operations/XR_OPERATOR.md`

---

## Enforcement

The file `Tools/Validation/Validate-CoreBoundaries.ps1` verifies that no shipped Core assembly contains forbidden development-tooling references.

The file `Tools/Validation/Validate-RepositoryLayout.ps1` verifies that non-shipping directories are not referenced in shipped `.asmdef` files.
