# Desktop XR Test Workflow — Convergence

This document describes the **development-only** desktop testing workflow.  
This is not part of the runtime game architecture.

---

## Desktop-first policy

Test as early as possible without a headset. Escalate to hardware only when the test genuinely requires it.

| Layer | Test method | Requires headset? |
|---|---|---|
| Core mathematics | EditMode tests (NUnit) | No |
| Core puzzle evaluation | EditMode tests | No |
| Gameplay state machines | PlayMode tests (Unity Test Runner) | No |
| XR interaction | Meta XR Simulator | No |
| Agent smoke tests | Meta XR Operator + Simulator | No |
| Reach distance / comfort | Physical Quest 2 | **Yes** |
| Room-scale behavior | Physical Quest 2 | **Yes** |
| Frame timing / thermals | Physical Quest 2 | **Yes** |

---

## Test execution order

```
1. EditMode tests
   pwsh -Command "& 'unity-editor-path' -runTests -testPlatform EditMode"
   → All Core tests must pass before proceeding.

2. PlayMode tests
   pwsh -Command "& 'unity-editor-path' -runTests -testPlatform PlayMode"
   → Gameplay and XR tests must pass before Simulator testing.

3. Meta XR Simulator (XR interaction)
   → Open Unity Editor
   → Activate Meta XR Simulator as OpenXR runtime
   → Enter Play Mode
   → Use XR Device Simulator bindings or Simulator synthetic input

4. Meta XR Operator (agent smoke tests) — optional
   → See Documentation/Operations/XR_OPERATOR.md
   → Run AgentScenarios/ smoke tests
   → Operator is not required; skip if unavailable

5. Physical Quest 2 — headset-required tests only
   → See Documentation/Operations/HEADSET_WORKFLOW.md
```

---

## Simulator limitations

Meta XR Simulator provides a desktop OpenXR runtime with simulated headset, controller, hand, and synthetic environment input.

**Simulator does not prove headset correctness.** Specifically:
- Imagery is simulated, not rendered by a real display.
- Spatial entities come from a configured synthetic environment, not a room scan.
- Thermal and battery behavior is not simulated.
- Quest 2 frame timing and GPU constraints are not reproduced.
- Physical reach distance and ergonomic comfort cannot be evaluated.

Always confirm headset-required tests on a physical device before release. See `Documentation/Operations/HEADSET_WORKFLOW.md`.

---

## XR Device Simulator bindings (for Unity Editor without Operator)

| Action | Binding |
|---|---|
| Move (left controller) | W/A/S/D |
| Rotate (right controller) | Mouse drag |
| Trigger | Left mouse button |
| Grip | Right mouse button |
| Toggle manipulated device | Tab |

These bindings apply when the XR Device Simulator (built into XRI) is active, as distinct from the Meta XR Simulator runtime.
