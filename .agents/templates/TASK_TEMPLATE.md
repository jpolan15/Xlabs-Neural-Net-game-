# Task Template

Copy this file to `.agents/tasks/` and rename it to describe the task.  
Fill in every field before an agent begins work.

---

```markdown
# Task: [Short descriptive title]

Status: ready | in-progress | blocked | complete
Owner: [Subsystem name, e.g. Core/Puzzles]
Created: YYYY-MM-DD
Updated: YYYY-MM-DD

## Description

[One paragraph describing what needs to be done and why.]

## Allowed paths

- [Path pattern 1, e.g. Assets/Scripts/Core/Math/**]
- [Path pattern 2]

## Forbidden paths

- [Path pattern that must not be touched, e.g. Assets/Scripts/XR/**]
- [Path pattern 2]

## Acceptance criteria

- [ ] [Criterion 1: specific, testable]
- [ ] [Criterion 2]
- [ ] Tests cover positive, negative, zero, and boundary values.
- [ ] No forbidden dependencies introduced.
- [ ] Handoff report filed.

## Blocked on

[None / Description of blocker and which task unblocks this]

## Handoff notes

[Context for the next agent or subsystem that depends on this task]
```
