# Agent Workflow — Convergence

This is the operating procedure for all agents working in this repository.  
Keep context small: read only what the change needs.

---

## Before editing

1. Read the section of the active task you are working on (see root `AGENTS.md`), not the whole file.
2. Read the nearest directory `AGENTS.md` and `.asmdef` for code you touch.
3. Search for an existing implementation and the tests that cover it.
4. Out of the task's allowed paths? Stop and file a handoff.

Look things up on demand: `.agents/PROJECT_MAP.md` (where), `.agents/DECISIONS_INDEX.md` (why).

---

## While editing

- Make the smallest coherent change that satisfies the acceptance criteria.
- Preserve public APIs unless the task explicitly requires changing them.
- Do not mix refactoring with feature work in the same commit.
- Do not update documentation that is outside your allowed paths.
- Do not install or upgrade packages without creating or updating an ADR.
- Do not reference Unity APIs (`UnityEngine`, `MonoBehaviour`) from Core assemblies.
- Do not reference Meta SDK types from Core, Gameplay, or Presentation.
- Do not add a second interaction rig or input abstraction.

---

## After editing

1. **Compile**: Confirm the affected assembly compiles without errors or warnings.
2. **EditMode tests**: Run relevant EditMode tests when Core logic changes.
3. **PlayMode tests**: Run PlayMode tests when scene, XR, or Gameplay behavior changes.
4. **Boundary validation**: Run `pwsh Tools/Validation/Validate-CoreBoundaries.ps1` when any Core file changes.
5. **Layout validation**: Run `pwsh Tools/Validation/Validate-RepositoryLayout.ps1` when directories or instruction files change.
6. **Update the task file**: Mark the task as complete or update status.
7. **Write the handoff report** using `.agents/templates/HANDOFF_TEMPLATE.md`.
8. Report: every command run, exact output, changed files, test results, known limitations.

---

## Crossing subsystem boundaries

If your task requires a change outside your allowed paths:

1. Stop editing.
2. Document the required cross-boundary change in the task file.
3. Create a new task in `.agents/tasks/BACKLOG.md` for the other subsystem.
4. File a handoff using `.agents/templates/HANDOFF_TEMPLATE.md`.
5. Do not make the cross-boundary change yourself.

---

## Prohibited at all times

- Claim a test passed without running it.
- Delete a test to make validation green.
- Hardcode puzzle completion, scores, or door states.
- Describe a file or directory location that does not yet exist as if it is canonical.
- Modify `ProjectSettings/` without Config Agent approval.
- Modify generated Unity files (`.meta`, GUIDs, serialized scenes) by hand.
- Touch `Packages/` without recording the reason in an ADR and updating `Documentation/Architecture/ADRs/`.

---

## Definitions

| Term | Meaning |
|---|---|
| Allowed path | A path explicitly listed in the task's `Allowed paths:` field |
| Forbidden path | A path explicitly listed or implied outside the task scope |
| Subsystem boundary | The line between any two top-level assemblies |
| ADR | Architecture Decision Record in `Documentation/Architecture/ADRs/` |
| Handoff | A structured report filed when work crosses a subsystem boundary |
