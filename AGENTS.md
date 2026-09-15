# AGENTS.md — Convergence Repository Rules

## Project

Convergence is an educational VR puzzle game built with Unity.

- Unity version: see `ProjectSettings/ProjectVersion.txt`
- Primary device: Meta Quest 2
- Runtime standard: OpenXR
- Render pipeline: URP
- Interaction authority: XR Interaction Toolkit
- Desktop test environment: Meta XR Simulator

## Agent startup procedure

Before changing any file:

1. Read this file.
2. Read `.agents/PROJECT_MAP.md`.
3. Read `.agents/WORKFLOW.md`.
4. Read the applicable task file in `.agents/tasks/`.
5. Read the nearest directory-level `AGENTS.md` for the files being changed.
6. Confirm the task's allowed paths before touching anything.

Do not scan the entire repository unless the task explicitly requires it.

## Source-of-truth rules

- Architecture rules → `Documentation/Architecture/`
- Architecture decisions (ADRs) → `Documentation/Architecture/ADRs/`
- Mathematical definitions → `Documentation/Mathematics/FORMULAS.md`
- Test requirements → `Documentation/Testing/TEST_STRATEGY.md`
- Development tooling → `Documentation/Operations/`
- Agent operating procedure → `.agents/WORKFLOW.md`
- Agent decision index → `.agents/DECISIONS_INDEX.md` (links to ADRs; does not copy them)
- Active task tracking → `.agents/tasks/`
- Code is authoritative over prose when prose is stale.

Do not create duplicate architecture documents.
Do not create new top-level directories without updating this file and `.agents/PROJECT_MAP.md`.

## Dependency rules

Allowed dependency direction (assembly graph):

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

- `Core.*` assemblies must not reference UnityEngine, MonoBehaviour, ScriptableObject, XR APIs, Meta SDK, Audio, or rendering APIs.
- `Core.*` must compile as ordinary C# without Unity DLLs.
- `Gameplay` may reference Core but must not implement neural-network mathematics.
- `XR` converts physical input into gameplay commands. It must not evaluate puzzles.
- `Presentation` observes state and displays it. It must not decide puzzle correctness.
- Meta-specific code must remain under `Assets/Scripts/XR/Meta/` or `Tools/`.
- Development tooling (MCP, XR Operator, validators) must not be referenced by Core, Gameplay, XR, or Presentation assemblies.

## Ownership rules

An agent may only modify files explicitly listed in the task's allowed paths.

If a change crosses subsystem boundaries:
1. Stop.
2. Describe the required handoff in the task file.
3. Do not silently modify another subsystem.

## Required validation

Before reporting completion:

1. Run the narrowest relevant tests.
2. Run `pwsh Tools/Validation/Validate-CoreBoundaries.ps1` when Core changes.
3. Run `pwsh Tools/Validation/Validate-RepositoryLayout.ps1` when directories or instruction files change.
4. Report every command run and its exact output.
5. Do not claim a test passed if it was not run.

## Prohibited behavior

Do not:

- Move files only to make the tree look cleaner without updating all references.
- Add UnityEngine references to `Core.*` assemblies.
- Hardcode puzzle completion in any layer.
- Add a second input or interaction abstraction alongside XRI.
- Modify package versions without recording the reason in an ADR.
- Modify generated Unity files manually unless explicitly required.
- Delete failing tests to make validation pass.
- Touch files outside the task's allowed paths.
- Describe a planned directory or file location as if it already exists.
