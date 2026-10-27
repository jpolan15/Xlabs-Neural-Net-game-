# AGENTS.md — Convergence

Educational VR puzzle game. Unity (`ProjectSettings/ProjectVersion.txt`), Meta Quest 2, OpenXR, URP, XR Interaction Toolkit.

## Current job

Level 1 digits ride (pixels → neuron → network → backprop → gradient descent). Ignite demo April 2027.
Plan: `.agents/tasks/TASK_LEVEL1_V6_TRAIN.md` (v6: your own 3, trained live, ADR-020), on `TASK_LEVEL1_V5_GAMEPLAY.md` (games, ADR-018) and `TASK_LEVEL1_V4_EXPLAINERS.md` (explainers + Pixel).
Read only the section you need; don't read whole handoffs up front. Old tasks are in `.agents/tasks/archive/` — ignore them.

## Where things live

Map: `.agents/PROJECT_MAP.md`. Workflow: `.agents/WORKFLOW.md`. ADRs: `Documentation/Architecture/ADRs/`.
Math: `Documentation/Mathematics/FORMULAS.md`. Raw art: `ArtSource/` (never `Assets/`). New 3D assets: `asset-forge` skill.
Code beats stale prose.

## Hard rules

- Assemblies: Core.Math → Neural → Training → Puzzles → Gameplay → XR / Presentation. `Core.*` has no UnityEngine.
- XR turns input into commands; Presentation only displays. Neither decides puzzle correctness.
- Meta code only in `Assets/Scripts/XR/Meta/` or `Tools/`. Dev tooling never referenced by runtime assemblies.
- Stay inside the task's allowed paths; no new top-level dirs; no package bumps without an ADR.
- Never hardcode puzzle completion or delete failing tests.

## Validation

1. Run the narrowest tests; report commands and real output.
2. Core changed → `pwsh Tools/Validation/Validate-CoreBoundaries.ps1`. Dirs/instruction files changed → `Validate-RepositoryLayout.ps1`.
3. Ride visuals changed → seat captures (`Convergence/Capture Ride Seat Views`) + `vr-qa` review; new model → `RideModelSheets.Models` entry.

Unity Editor open → use `UnityMCP` tools (batchmode can't open a locked project).
