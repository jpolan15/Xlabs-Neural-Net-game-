# Core Layer Rules

This directory contains **engine-independent C# logic only**.  
Assemblies: `Convergence.Core.Math`, `Convergence.Core.Neural`, `Convergence.Core.Training`, `Convergence.Core.Puzzles`.

## Allowed

- Pure C# types and records
- Deterministic calculations
- Explicit data structures
- Standard .NET libraries: `System`, `System.Collections.Generic`, `System.Linq`
- Exceptions or result objects for invalid input
- Interfaces and abstract types

## Forbidden

- `using UnityEngine` (any namespace)
- `using UnityEngine.UI`
- `using Unity.XR` or `using UnityEngine.XR`
- `using Meta.` or `using Oculus.`
- `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Transform`
- `UnityEvent`, `AddressableAssets`, any Unity-specific attribute
- Rendering, audio, scene, or prefab references
- References to `Convergence.Gameplay`, `Convergence.XR`, or `Convergence.Presentation`

## Required

- Every public calculation must have EditMode tests in `Tests/EditMode/Core/`.
- Tests must cover positive, negative, zero, and boundary values.
- When changing a formula, update `Documentation/Mathematics/FORMULAS.md` and add or update the test.
- Record numerical tolerances in `Documentation/Mathematics/FORMULAS.md`.
- All public APIs must have XML doc comments.

## Validation

Before reporting completion:
```powershell
pwsh Tools/Validation/Validate-CoreBoundaries.ps1
```

This must exit 0. If it exits non-zero, do not report the task as complete.


