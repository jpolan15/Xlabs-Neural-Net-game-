# Assets/Scripts — Scope Rules

All C# runtime source code for Convergence lives here.  
Code is organized by assembly layer. Each subdirectory has its own `AGENTS.md` with strict allowed/forbidden rules.

## Assembly layout

| Directory | Assembly | Layer | AGENTS.md |
|---|---|---|---|
| `Core/Math/` | `Convergence.Core.Math` | Runtime — Pure C# | `Core/AGENTS.md` |
| `Core/Neural/` | `Convergence.Core.Neural` | Runtime — Pure C# | `Core/AGENTS.md` |
| `Core/Training/` | `Convergence.Core.Training` | Runtime — Pure C# | `Core/AGENTS.md` |
| `Core/Puzzles/` | `Convergence.Core.Puzzles` | Runtime — Pure C# | `Core/AGENTS.md` |
| `Gameplay/` | `Convergence.Gameplay` | Runtime — MonoBehaviour | `Gameplay/AGENTS.md` |
| `XR/` | `Convergence.XR` | Runtime — XRI + Input System | `XR/AGENTS.md` |
| `XR/Meta/` | (within `Convergence.XR`) | Runtime — Meta adapters only | `XR/AGENTS.md` |
| `Presentation/` | `Convergence.Presentation` | Runtime — visualization, audio, haptics | *(no file yet)* |
| `Infrastructure/` | `Convergence.Infrastructure` | Runtime — events, telemetry, logging | *(no file yet)* |

## Dependency direction

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

Full rules: `Documentation/Architecture/DEPENDENCY_RULES.md`

## Rules

- Do not place development tooling or test infrastructure in `Assets/Scripts/`.
- Do not create a new top-level subdirectory here without updating `Documentation/Architecture/OVERVIEW.md`, `.agents/PROJECT_MAP.md`, and this file.
- Each new directory must have a `.asmdef` file before any code is added.
- The `Tests/` directory that previously existed under `Assets/Scripts/Tests/` is being migrated to the top-level `Tests/`. Do not add new tests there.

