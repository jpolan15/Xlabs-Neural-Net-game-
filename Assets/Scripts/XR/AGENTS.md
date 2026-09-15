# XR Layer Rules

XR converts physical interaction into gameplay commands.  
Assembly: `Convergence.XR`.

## Allowed

- OpenXR APIs (via `Unity.XR.*`)
- XR Interaction Toolkit 3.x (`XRBaseInteractor`, `XRBaseInteractable`, `XRInteractionManager`, etc.)
- Unity Input System (`InputAction`, `InputActionMap`, `InputActionReference`)
- Haptic dispatch through XRI (`SendHapticImpulse`)
- Meta-specific adapters and features, **but only under `XR/Meta/`**

## Forbidden

- Puzzle scoring or evaluation — delegate to Core layer via Gameplay command
- Neural-network mathematics — delegate to Core
- Direct mutation of Core layer state
- Hardcoded chamber completion or target values
- Scene or facility state management — delegate to Gameplay
- `Meta.*` or `Oculus.*` calls outside of `XR/Meta/` subdirectory

## Command contract

Every interaction must produce a **testable command or event** to Gameplay:

```csharp
// Examples — exact names to be confirmed when implemented
OnWeightChanged(SocketId socket, float newValue)
OnCableConnected(SocketId from, SocketId to)
OnActivationInserted(SocketId socket, ActivationType type)
OnForwardPassTriggered()
OnBiasAdjusted(NeuronId neuron, float delta)
```

XR must not call Gameplay methods directly — use C# events or an interface.

## Meta-specific adapter rules (`XR/Meta/`)

- All `Meta.*` and `Oculus.*` references are confined to this subdirectory.
- The adapter must expose only standard C# events or Unity events upward to `Convergence.XR`.
- Core, Gameplay, and Presentation must not import from `XR/Meta/`.

## Interaction constraints (Quest 2)

- Minimum snap radius: 15–20 cm.
- Use stepped detents (0.5 or 1.0 increments) with optional fine mode.
- No per-frame GC allocations in `Update`.
- Spline cables: max 24 segments, no ragdoll physics.

## Required tests

Tests location: `Tests/PlayMode/XR/`

- Each interaction event fires with correct payload when the interaction completes.
- Interactions work with XR Device Simulator (desktop) without a headset.
- No interaction directly unlocks doors or advances chambers.


