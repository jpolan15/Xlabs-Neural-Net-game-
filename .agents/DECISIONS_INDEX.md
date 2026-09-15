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
