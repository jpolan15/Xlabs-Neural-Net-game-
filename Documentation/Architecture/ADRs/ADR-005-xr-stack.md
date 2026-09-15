# ADR-005: XR Interaction Toolkit as the Sole Interaction Authority

| Field | Value |
|---|---|
| ID | ADR-005 |
| Date | 2026-09-14 |
| Status | Accepted |
| Deciders | Architecture, XR |

## Context

The project documentation previously referenced both Unity XR Interaction Toolkit (XRI) and Meta XR Interaction SDK / Building Blocks without committing to one as the authority. Mixing two interaction frameworks in the same scene creates competing interaction rigs, unpredictable input routing, and a maintainability burden. A single authority must be chosen.

Meta's Unity XR Interaction SDK path provides cross-platform interaction but does not expose every feature available through the full Meta XR Core SDK. Meta XR Operator, which is used for agent-driven testing, is experimental and must not create a runtime dependency.

## Decision

| Concern | Owner |
|---|---|
| Runtime standard | OpenXR |
| Input abstraction | Unity Input System |
| Interaction and locomotion | XR Interaction Toolkit 3.x |
| Rendering | URP |
| Meta-specific optional features | Adapter layer under `Assets/Scripts/XR/Meta/` |
| Desktop simulation | Meta XR Simulator |
| Agent-driven testing | Meta XR Operator (development tooling only) |
| Neural-network gameplay logic | Core assemblies (`Convergence.Core.*`) |

## Rules

- OpenXR is the runtime provider. No other XR plugin is used as the primary provider.
- Unity Input System is the only input abstraction used by Gameplay or Core code.
- XR Interaction Toolkit owns all interactors, interactables, locomotion, and haptic dispatch.
- Meta-specific APIs (`Meta.*`, `Oculus.*`) may only be accessed from `Assets/Scripts/XR/Meta/`.
- `Core.*`, `Gameplay`, and `Presentation` must not reference Meta SDK types.
- No scene may contain competing interaction rigs (e.g. both an XRI rig and a Meta Interaction rig).
- Meta XR Operator is development tooling. It must not be required by any shipped assembly.

## Non-blocking alternatives

- Re-evaluate Meta XR Interaction SDK adoption only if a concrete feature (hand tracking, scene understanding, passthrough) cannot be achieved through XRI. Any such adoption requires a new ADR.

## Consequences

- Single, well-defined input pipeline reduces debugging surface.
- Cross-platform correctness: XRI runs on any OpenXR-compliant device.
- Meta-only features remain behind an adapter and can be removed without touching Core or Gameplay.
- Agents have one clear boundary: `XR/Meta/` is the only place for vendor-specific code.

## Supersedes

Nothing. This is the first explicit XR stack decision.
