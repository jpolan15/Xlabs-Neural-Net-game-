# Tools — Scope Rules

This directory contains **development-only** tooling.  
**Nothing here is assumed to ship in the game binary.**

## Directory layout

| Path | Purpose |
|---|---|
| `Tools/Validation/` | PowerShell validators for Core boundaries and repository layout |
| `Tools/Editor/` | Unity Editor tooling: build scripts, project map generation |
| `Tools/Editor/Build/` | Release packaging scripts |
| `Tools/Editor/ProjectMap/` | Script to regenerate `.agents/PROJECT_MAP.md` |
| `Tools/AgentBridge/` | MCP server and XR Operator integration (not a runtime dependency) |
| `Tools/AgentBridge/README.md` | Setup instructions for the MCP server |

## Rules

- Nothing in `Tools/` is referenced by `Core`, `Gameplay`, `XR`, or `Presentation` assemblies.
- Tools must fail loudly with actionable error messages — never silently succeed on a bad state.
- Tools must document required external services or prerequisites.
- Tools should include a dry-run mode where practical (`-WhatIf` or `-DryRun` flag).
- Do not modify project assets from within a tool unless the tool explicitly states it will do so.
- Do not add runtime game logic here.

## Validation scripts

| Script | When to run |
|---|---|
| `Validate-CoreBoundaries.ps1` | Any time a `Core.*` file changes |
| `Validate-RepositoryLayout.ps1` | Any time directories or instruction files change |

Both scripts must exit 0 before reporting a task as complete.  
See the scripts themselves for full documentation of their checks.
