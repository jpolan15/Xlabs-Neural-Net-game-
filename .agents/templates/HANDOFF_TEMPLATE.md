# Handoff Report Template

File this report when:
- A task is complete and another subsystem depends on the output.
- A task cannot continue because a change is required outside the allowed paths.
- An unexpected finding blocks progress.

---

```markdown
# Handoff Report

## Agent / subsystem
[Name of the subsystem completing or blocking this handoff]

## Task
[Title or ID of the task file this report corresponds to]

## Status
complete | blocked | partial

## What was done

[Describe the work completed. Be specific about what changed.]

## Files changed

- [Full path to file 1]
- [Full path to file 2]

## Tests run

| Test | Command | Result |
|---|---|---|
| [Test name] | [Command run] | passed / failed |

## Validation run

| Validator | Command | Result |
|---|---|---|
| Core boundaries | `pwsh Tools/Validation/Validate-CoreBoundaries.ps1` | passed / failed / skipped |
| Repository layout | `pwsh Tools/Validation/Validate-RepositoryLayout.ps1` | passed / failed / skipped |

## Dependencies added

None / [Package name and version, with ADR reference]

## Known limitations

- [Limitation 1]
- [Limitation 2]

## Cross-boundary requests

[Describe any changes needed outside the task's allowed paths, and which subsystem should handle them.]

## Next task

[Title or path of the next task to unblock, if applicable]
```
