# Headset Test Workflow — Convergence

This document lists tests that require a physical Meta Quest 2 headset.  
**Simulator does not prove headset correctness.**

---

## Why the Simulator is insufficient for these tests

Meta XR Simulator provides simulated imagery and synthetic spatial entities. It does not reproduce:
- Real Quest 2 display latency and refresh timing
- Mobile GPU and CPU thermal constraints
- Battery drain behavior
- Physical arm reach and grip fatigue
- Room-scale boundary and guardian behavior
- Actual hand tracking quality from camera images
- Real controller IMU drift and haptic motor response

---

## Tests requiring a physical Quest 2

| Test | What to verify |
|---|---|
| Reach distance | Can the player comfortably reach all interactables in each chamber? |
| Grip ergonomics | Are dial, socket, and lever interactions comfortable for 15–20 min sessions? |
| Cable snap feel | Does the haptic buzz on socket snap feel responsive and satisfying? |
| Room-scale movement | Does locomotion feel safe and comfortable within a typical room boundary? |
| Fast physics / throwing | Do interactables behave correctly when moved quickly? |
| Hand tracking quality | Do hand-tracking interactions register correctly at chamber interaction distances? |
| Actual frame timing | Does the game maintain target frame rate under full chamber visual load? |
| Thermal behavior | Does the device throttle during extended play? Does performance degrade? |
| Battery behavior | What is session length at typical play intensity? |
| Standalone vs. Link | Do both standalone and PCVR Link modes produce equivalent interaction behavior? |

---

## Headset build procedure

1. Run all EditMode and PlayMode tests first (see `Documentation/Operations/DESKTOP_WORKFLOW.md`).
2. Confirm the Core boundary validator passes: `pwsh Tools/Validation/Validate-CoreBoundaries.ps1`
3. In Unity: **File → Build Settings → Android → Build and Run**.
4. Deploy to connected Quest 2 (developer mode enabled, ADB authorized).
5. Alternatively, use PCVR Link / Air Link for Editor-connected testing.

---

## Performance targets (Quest 2)

> [!NOTE]
> These are guidance targets, not hard shipping gates. Record actual measurements.

| Metric | Target |
|---|---|
| Frame rate | ≥ 72 Hz sustained during normal gameplay |
| GPU frame time | ≤ 13.9 ms per frame at 72 Hz |
| Draw calls per frame | < 100 (batched) |
| GC allocations in Update | 0 bytes in steady state |
| Texture memory | Within Quest 2 budget (~3 GB addressable) |

Record actual measurements in the release task, not here. Do not commit performance targets to this document as pass/fail gates without a corresponding automated test.

---

## Headset-only findings

Document findings from headset sessions in `.agents/tasks/` as follow-up tasks, not in this document.
