> **DEPRECATED** — This document is superseded by `Documentation/Architecture/OVERVIEW.md`.
> Do not update this file. Update the canonical document instead.
> This file will be removed in a future cleanup task.

# Technical Architecture (Legacy)


## Decoupled Multi-Layer Design

```
┌────────────────────────────────────────────────────────────────┐
│               Core Layer (Pure C# / Zero Engine)               │
│  Math ──► Neural Models ──► Puzzle Evaluator ──► Training      │
└───────────────────────────────┬────────────────────────────────┘
                                │ State & Telemetry Events
                                ▼
┌────────────────────────────────────────────────────────────────┐
│                      Unity Engine Layer                        │
│                                                                │
│   XR Interaction Toolkit 3.x        Visualization & Shaders    │
│   - Spline Magnetic Cables          - Particle Signal Beams    │
│   - Tactile Rotary Dials            - Glass Plasma Chambers    │
│   - Cartridge Sockets               - 2D Boundary Laser Grid   │
│   - Quest Touch Haptics             - Multi-Case Loss Meters   │
│                 │                                │             │
│                 └──────────► Gameplay ◄──────────┘             │
│                       - Blast Doors & Alarms                   │
│                       - Facility State Machine                 │
└────────────────────────────────────────────────────────────────┘
```

## Assembly Definitions
- `Convergence.Core`: Standalone, no external references.
- `Convergence.XR`: References `Convergence.Core`, `Unity.XR.Interaction.Toolkit`, `Unity.InputSystem`.
- `Convergence.Visualization`: References `Convergence.Core`, `Unity.Splines`, `Unity.TextMeshPro`.
- `Convergence.Gameplay`: References `Convergence.Core`, `Convergence.XR`, `Convergence.Visualization`.
- `Convergence.Tests`: NUnit test runner.
