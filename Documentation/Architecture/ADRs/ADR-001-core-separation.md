# ADR-001: Neural Mathematics Separated from Unity Engine

| Field | Value |
|---|---|
| ID | ADR-001 |
| Date | 2026-09-10 |
| Status | Accepted |
| Deciders | Architecture |

## Context

Tying neural network calculations directly to Unity GameObjects and MonoBehaviour scripts makes unit testing slow, hard to automate via CI or CLI, and prone to coupling rendering bugs with calculation bugs. It also prevents headless compilation and pure mathematical verification.

## Decision

All mathematics (`Core/Math`), neural models (`Core/Neural`), puzzle evaluation (`Core/Puzzles`), and training algorithms (`Core/Training`) are strictly pure C# classes with zero `UnityEngine` references. They must build cleanly via standard .NET compilers and be fully testable without Unity.

## Allowed dependencies in Core

- `System`
- `System.Collections.Generic`
- `System.Linq`
- Standard .NET libraries

## Consequences

- Fast automated unit tests that run without the Unity Editor.
- Guaranteed mathematical determinism independent of Unity's floating-point behavior.
- Clean assembly boundaries enforceable by static analysis.
- Code reuse possible across platforms without Unity.
- All public Core calculations must have corresponding EditMode tests.

## Enforcement

`Tools/Validation/Validate-CoreBoundaries.ps1` scans for forbidden `using` directives and validates `.asmdef` references.

## Supersedes

Nothing.
