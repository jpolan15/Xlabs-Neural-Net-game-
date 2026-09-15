# Assembly Dependency Rules — Convergence

This document is the machine-readable source of truth for which assembly may reference which.  
The `Tools/Validation/Validate-CoreBoundaries.ps1` and `Validate-RepositoryLayout.ps1` scripts enforce these rules.

---

## Allowed reference graph

```
Convergence.Core.Math         → (nothing; pure C# only)
Convergence.Core.Neural       → Convergence.Core.Math
Convergence.Core.Training     → Convergence.Core.Math, Convergence.Core.Neural
Convergence.Core.Puzzles      → Convergence.Core.Math, Convergence.Core.Neural, Convergence.Core.Training
Convergence.Gameplay          → Convergence.Core.* (all four)
Convergence.XR                → Convergence.Gameplay, Unity.XR.Interaction.Toolkit, Unity.InputSystem
Convergence.Presentation      → Convergence.Gameplay, Convergence.Core.*
Convergence.EditorTools       → any (Editor-only; excluded from builds)
```

---

## Forbidden references — Core assemblies

The following must never appear in any `Convergence.Core.*` assembly definition or source file:

| Forbidden reference | Reason |
|---|---|
| `UnityEngine` | Engine dependency; breaks headless compilation |
| `UnityEngine.UI` | Engine dependency |
| `Unity.XR.*` | XR platform dependency |
| `UnityEngine.XR.*` | XR platform dependency |
| `Meta.*` | Meta SDK dependency |
| `Oculus.*` | Meta/Oculus SDK dependency |
| `MonoBehaviour` | Unity lifecycle dependency |
| `ScriptableObject` | Unity serialization dependency |
| `GameObject` | Unity scene dependency |
| `Convergence.Gameplay` | Upward reference — forbidden |
| `Convergence.XR` | Upward reference — forbidden |
| `Convergence.Presentation` | Upward reference — forbidden |
| `Convergence.EditorTools` | Editor-only; must not be in runtime path |

---

## Forbidden references — Gameplay assembly

| Forbidden reference | Reason |
|---|---|
| `Meta.*` | Meta SDK; use adapter under XR/Meta/ |
| `Oculus.*` | Meta SDK; use adapter under XR/Meta/ |
| `Unity.XR.*` | XR input; must go through XR layer commands |

---

## Forbidden references — XR assembly

| Forbidden reference | Reason |
|---|---|
| Puzzle mathematics | Must remain in Core |
| Direct Core state mutation | Must go through Gameplay commands |

---

## Validation procedure

1. Check `.asmdef` references for each assembly against the allowed graph above.
2. Scan `Assets/Scripts/Core/**/*.cs` for forbidden `using` directives.
3. Fail if any `Convergence.Core.*` assembly is missing (do not pass on empty directories).
4. Fail if any forbidden reference is found in any `.asmdef` file.
5. Report the file and line number of every violation.

---

## Updating these rules

Any change to this table requires:
1. A new or updated ADR in `Documentation/Architecture/ADRs/`.
2. An entry in `.agents/DECISIONS_INDEX.md`.
3. Validation script update if new patterns are introduced.
4. Config Agent sign-off if package versions change.
