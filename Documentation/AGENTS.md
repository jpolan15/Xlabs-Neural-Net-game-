# Documentation — Scope Rules

This directory contains canonical reference documents for the Convergence project.  
Agents read these documents. They do not build game features from them.

## Directory layout

| Path | Purpose |
|---|---|
| `Architecture/` | Runtime game architecture — layers, assemblies, dependency rules |
| `Architecture/ADRs/` | Canonical Architecture Decision Records (source of truth for all decisions) |
| `Architecture/OVERVIEW.md` | Layer diagram, assembly graph, data flow |
| `Architecture/DEPENDENCY_RULES.md` | Machine-readable assembly reference table |
| `Architecture/RUNTIME_BOUNDARIES.md` | What ships vs. what does not |
| `Design/` | Game design documents |
| `Mathematics/FORMULAS.md` | Canonical mathematical definitions — update when changing Core formulas |
| `Operations/` | Development tooling: desktop workflow, headset workflow, XR Operator |
| `Testing/TEST_STRATEGY.md` | Three-tier test strategy and platform requirements |
| `Technical/` | Legacy documents (see deprecation notices; do not update) |

## Rules

- `Architecture/` is the canonical home for all architecture content. Do not duplicate it elsewhere.
- `Architecture/ADRs/` is the canonical home for all decisions. Do not copy ADR content into `.agents/` — link only.
- `Operations/` describes development tooling only. It is not part of the runtime architecture.
- When changing a Core formula, update `Mathematics/FORMULAS.md` in the same task.
- Every architectural decision must produce a new ADR file in `Architecture/ADRs/` and an entry in `.agents/DECISIONS_INDEX.md`.
- Do not create new subdirectories here without updating the root `AGENTS.md` and `.agents/PROJECT_MAP.md`.
