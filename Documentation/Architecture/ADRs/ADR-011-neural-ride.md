# ADR-011: The Neural Ride Replaces the Chamber 01 Console

| Field | Value |
|---|---|
| ID | ADR-011 |
| Date | 2026-10-06 |
| Status | Accepted |
| Deciders | Design, Gameplay, XR, Presentation |

## Context

Level 01 was one dark, open room with one console carrying many systems: dozens of small labels, two holograms, a broken-shader magenta cube, a blown-out emissive sphere (HDR emission in Gamma color space), and a HUD that printed developer errors. Six concepts shared one scene. Playtesters skipped the equations because the equations were the only teaching, and earlier task logs record that captures were never judged.

## Decision

The live game becomes **the Neural Ride**: the player sits in a probe pod on a rail inside the ship AI's neural core. Each stop teaches one idea with one new control. The experience is `Assets/Scenes/NeuralRide.unity`, build scene 0. `Level01_AwakeningGate` stays in the repository and in the build list, but is no longer the first scene.

- **Stop 1, One Signal:** weight × input against a trigger. One lever.
- **Stop 2, Two Signals:** a weighted sum (OR). Three levers. The ICE pipe starts wired backwards.
- **Stop 3, It Learns:** loss and gradient descent. The player pulls LEARN and chooses a learning rate (slow, good, crazy). A marble rolls down a loss landscape while the weight levers move by themselves.
- The equation assembles itself from the numbers the player set, after each stop.
- Anything the player can touch is orange. Nothing else is.

### Architecture

- **Core is unchanged in meaning.** Two small pure-C# additions in `Core/Training`: `BatchGradientDescent` (full-batch update for one neuron, built on the existing `NetworkModel.Forward/Backward` and the loss functions) and `LossLandscape` (samples the mean loss over a grid of two weights). `StochasticGradientDescent.ComputeLoss/ComputeGradient` became `internal` so both can reuse them. No formula changed, so `FORMULAS.md` is unchanged. EditMode tests: `BatchGradientDescentTests`.
- **Gameplay (`Gameplay/Ride/`):** `RideDirector` (stops, Catmull-Rom path via `RidePath`, yaw-only pod motion), `StationController` (loads the Chamber 01 catalog row, evaluates every lever change through `PuzzleEvaluator.Evaluate`, runs the trainer step by step), `RideControlChannel` / `RideControlSet` (the dash values).
- **XR (`XR/Ride/`):** `PodLeverInteractable` is the one control type, an XRI interactable on one axis that raises a `UnityEvent<float>` the scene wires to the channel. `PodSeat` parents the XRI Starter rig into the pod and disables locomotion providers (ADR-007). `DesktopSeat` is the mouse fallback: hold right mouse to look, left-drag a lever.
- **Presentation (`Presentation/Ride/`):** views only. They read events and channel values from Gameplay and never call `Evaluate`. Colors and display numbers live in the `RideTheme` ScriptableObject.
- **Tooling:** `Tools/Editor/NeuralRideBuilder.cs` builds the prefabs (`Assets/Prefabs/NeuralRide/`), materials (`Assets/Materials/NeuralRide/`), the theme, and the scene. It fails if any renderer or text sits outside the Pod, a Station, or the Track.
- `NeuralState` and `LiveEvaluationRelay` are not used by the ride: they are bound to `ChamberController`, so the ride's `StationController` evaluates directly through `PuzzleEvaluator`. They stay for Level 01.

### Decisions inside this change

- **Linear color space** (approved by the user on 2026-10-06; `ProjectSettings/AGENTS.md` already required it). Level 01 materials may look different.
- **Narration** is AI-synthesized (Windows OneCore voice, `vo_ride_01` to `vo_ride_15` under `Assets/_Project/Audio/Voice/`) so the ride does not wait on recordings. Recordings can replace them by filename and a builder re-run.
- **Music:** `Assets/_Project/Audio/Music/telstar.wav`, supplied by the user who stated it is licensed for this project. It streams as Vorbis. Licensing is the user's responsibility and is not verified in this repository.
- **Equation text is ASCII** (`>=`, `<-`, `×`) because the project's TextMeshPro font has no Greek letters or arrows.

## Consequences

- One scene replaces the console. The ride fits the Quest 2 budget by showing one stop at a time (`StationReveal`) and fogging the far tunnel.
- Backpropagation runs in the stop 3 trainer but is not yet taught: with one neuron it is a single step. Hidden layers (the XOR stop) are the stretch goal.
- Not yet done: ship intro and outro scenes, the 3-question quiz, tunneling vignette, comfort settings, device measurements.
