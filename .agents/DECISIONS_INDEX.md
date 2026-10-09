# Decision Index

This file is an index only. It links to canonical ADRs.  
Do not copy ADR content here. Do not make decisions here.  
Canonical decisions live in `Documentation/Architecture/ADRs/`.

---

| ID | Decision summary | Status | Canonical document |
|---|---|---|---|
| ADR-001 | Neural mathematics separated from Unity Engine | Accepted | [`ADR-001-core-separation.md`](../Documentation/Architecture/ADRs/ADR-001-core-separation.md) |
| ADR-002 | Magnetic-snap Catmull-Rom splines instead of physics ragdoll cables | Accepted | [`ADR-002-spline-cables.md`](../Documentation/Architecture/ADRs/ADR-002-spline-cables.md) |
| ADR-003 | Multi-case diagnostics instead of single scalar loss | Accepted | [`ADR-003-multi-case-diagnostics.md`](../Documentation/Architecture/ADRs/ADR-003-multi-case-diagnostics.md) |
| ADR-004 | Task-appropriate loss functions per chamber type | Accepted | [`ADR-004-loss-functions.md`](../Documentation/Architecture/ADRs/ADR-004-loss-functions.md) |
| ADR-005 | XR Interaction Toolkit is the sole interaction authority | Accepted | [`ADR-005-xr-stack.md`](../Documentation/Architecture/ADRs/ADR-005-xr-stack.md) |
| ADR-006 | Align package versions with Unity 6000.6.0f1 built-in distributions | Accepted | [`ADR-006-package-versions-unity6.md`](../Documentation/Architecture/ADRs/ADR-006-package-versions-unity6.md) |
| ADR-007 | XRI Starter Assets rig replaces the custom pointer rig | Accepted | [`ADR-007-xri-starter-rig.md`](../Documentation/Architecture/ADRs/ADR-007-xri-starter-rig.md) |
| ADR-008 | Lost-in-space premise; sentry, gun, and Synapse-GPT leave the live game | Accepted | [`ADR-008-premise-lost-in-space.md`](../Documentation/Architecture/ADRs/ADR-008-premise-lost-in-space.md) |
| ADR-009 | MCP for Unity added as Editor-only development tooling | Accepted | [`ADR-009-unity-mcp-tooling.md`](../Documentation/Architecture/ADRs/ADR-009-unity-mcp-tooling.md) |
| ADR-010 | Chamber 01 neuron drives an automatic point-defense laser; OR table and Core unchanged | Accepted | [`ADR-010-automated-point-defense.md`](../Documentation/Architecture/ADRs/ADR-010-automated-point-defense.md) |
| ADR-011 | The Neural Ride (pod on a rail through the AI core) replaces the Chamber 01 console as the live game | Accepted | [`ADR-011-neural-ride.md`](../Documentation/Architecture/ADRs/ADR-011-neural-ride.md) |
| ADR-012 | Ride voice cast (original Guide and AURA, archival JFK and NASA), no celebrity voice clones, music level policy, Asset Forge pipeline and `ArtSource/` | Proposed | [`ADR-012-ride-voices-audio-assets.md`](../Documentation/Architecture/ADRs/ADR-012-ride-voices-audio-assets.md) |
| ADR-013 | Ride v3 rendering: HoloLit shader, the two-mesh network, per-camera Quest overrides, angle-based panel layout enforced by a sightline check | Accepted | [`ADR-013-ride-v3-rendering.md`](../Documentation/Architecture/ADRs/ADR-013-ride-v3-rendering.md) |
| ADR-014 | Narration you can see ({cue} tags → explainer panel, network and arrows act out each clause), a stop waits for its punchline, the dock's hands-on weight, solid reading screens, sound effects levelled by measured loudness, credits | Accepted | [`ADR-014-ride-narration-you-can-see.md`](../Documentation/Architecture/ADRs/ADR-014-ride-narration-you-can-see.md) |
| ADR-015 | Hands-on opening (intro beats that wait for a lever or time out: wake AURA, make a neuron fire; JFK cold open), and the network as the star (weight hierarchy in the link shader, dim unlit network, scaffolding on its own dim mesh, `_Reveal`) | Accepted | [`ADR-015-ride-interactive-opening.md`](../Documentation/Architecture/ADRs/ADR-015-ride-interactive-opening.md) |

---

## Non-blocking deferred alternatives

| Topic | Status | Notes |
|---|---|---|
| Meta XR Interaction SDK adoption | Deferred | Re-evaluate only if a concrete feature cannot be implemented through XRI |
| `Assets/AudioAssets/` → `Assets/_Project/Audio/` migration | Deferred | Separate Unity Editor task; do not document as existing until moved |
| In-game tutor / runtime AI | Deferred | Create `Assets/Scripts/Gameplay/RuntimeAI/` only when feature is confirmed |

---

## How to add a new decision

1. Create `Documentation/Architecture/ADRs/ADR-NNN-short-name.md` using the ADR template.
2. Add a row to this table.
3. Update `Documentation/Architecture/DEPENDENCY_RULES.md` if the decision affects assembly references.
4. Run `pwsh Tools/Validation/Validate-RepositoryLayout.ps1` to confirm layout is consistent.
