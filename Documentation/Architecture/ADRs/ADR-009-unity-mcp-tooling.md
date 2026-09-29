# ADR-009: MCP for Unity as Development Tooling

| Field | Value |
|---|---|
| ID | ADR-009 |
| Date | 2026-09-29 |
| Status | Accepted |
| Deciders | Project owner, Tooling |

## Context

Agents working on Convergence could only edit files and run Unity in batchmode, which fails while the project is open in the Editor. Scene assembly, test runs and console inspection required a human in the loop. The project owner explicitly approved adding a third-party git-URL package to let agents drive the open Editor.

## Decision

- Add `com.coplaydev.unity-mcp` (MCP for Unity, MIT licence) from `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v9.7.3`, pinned to a release tag.
- The package is Editor tooling only. No `Convergence.*` runtime assembly may reference it.
- The MCP client connects over HTTP at `http://localhost:8080/mcp`. The Python server is launched by `uv`, which is a documented prerequisite (`Tools/AgentBridge/README.md`).

## Consequences

- Agents can inspect scenes, run menu items (for example the Level 01 builder), read the console and run tests against the live Editor.
- The dependency is a git URL, not a Unity Registry package. Upgrades require changing the pinned tag and updating this ADR.
- Builds for Quest are unaffected because the package contains only Editor assemblies.

## Supersedes

Nothing.
