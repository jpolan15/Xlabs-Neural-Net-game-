# ADR-002: Magnetic-Snap Catmull-Rom Splines Instead of Physics Ragdoll Cables

| Field | Value |
|---|---|
| ID | ADR-002 |
| Date | 2026-09-10 |
| Status | Accepted |
| Deciders | Architecture, XR |

## Context

Simulated physical ragdoll cables — using joints and continuous collision — frequently tangle, cause physics jitter, clip through VR geometry, and consume significant CPU time on Meta Quest 2's mobile SoC. Reliable cable behavior is critical to XR comfort and puzzle usability.

## Decision

Synapse cables use elastic Catmull-Rom spline curves with magnetic snap volumes near valid sockets. When a cable endpoint is held within range of a socket, the cable snaps cleanly with tactile haptic feedback and a visual alignment pulse.

## Implementation constraints

- Maximum 16–24 spline segments per cable (LineRenderer performance budget).
- Minimum 15–20 cm snap radius to avoid requiring millimeter precision.
- Snap volumes use lightweight trigger colliders, not continuous physics mesh colliders.
- No ragdoll joints, no continuous collision detection on cable bodies.

## Consequences

- Zero cable tangles in practice.
- Stable rendering at Quest 2 target frame rate.
- Zero physics joint jitter or instability.
- Simpler XR interaction state machine (grab / in-range / snapped).
- Cable visual fidelity is approximated, not physically simulated.

## Supersedes

Nothing.
