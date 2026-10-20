# Task: Point-defense feedback, voice lines, and formula panel

Status: complete
Owner: Presentation
Created: 2026-10-03
Updated: 2026-10-03

## Description

Make the point-defense loop readable and satisfying in VR without moving any decision out of Gameplay. Add `PointDefenseVisual` (laser beam, explosion, scan-gate flash, hull-hit sparks, red flash, haptics, live ROCK / ICE / FIRE console lamps) and `VoiceLinePlayer` (one voice at a time, interrupt rules, cooldowns, banner text from the `.txt` transcripts). Clean up `DataTargetVisual`, `AuraSubtitles`, `AudioHookManager`, and `WorldSpaceHud` so there is one banner writer, one formula panel, and no `OnGUI` text. Full spec is in `HANDOFF_ASTEROID_DEFENSE.md`, Step 3.

## Allowed paths

- `Assets/Scripts/Presentation/PointDefenseVisual.cs` (new)
- `Assets/Scripts/Presentation/VoiceLinePlayer.cs` (new)
- `Assets/Scripts/Presentation/DataTargetVisual.cs`
- `Assets/Scripts/Presentation/AuraSubtitles.cs`
- `Assets/Scripts/Presentation/AudioHookManager.cs`
- `Assets/Scripts/Presentation/WorldSpaceHud.cs`
- `Assets/_Project/Audio/Voice/**` (read only; the `.txt` files are the text source of truth)
- `.meta` files Unity generates for the new scripts

## Forbidden paths

- `Assets/Scripts/Core/**`
- `Assets/Scripts/Gameplay/**` (request new events through `TASK_ASTEROID_DEFENSE.md`)
- `Assets/Scripts/XR/**`
- `Assets/Scripts/Presentation/Convergence.Presentation.asmdef` (do not add an XRI reference)
- `ProjectSettings/**`, `Packages/**`

## Acceptance criteria

- [ ] Presentation never calls `PuzzleEvaluator`, never decides correctness, and never drives a state machine.
- [ ] No per-frame allocation in any `Update` (no LINQ, no string building, no `CaseDiagnostic.Inputs` per frame; that property clones an array).
- [ ] Haptics use `UnityEngine.XR.InputDevices.GetDeviceAtXRNode(...).SendHapticImpulse(0u, amp, dur)`, as `Assets/Scripts/XR/CableInteractable.cs` does.
- [ ] No camera shake.
- [ ] `DataTargetVisual` has no `OnGUI`. The body stays hidden after a vaporize until the next `Launch`, and is hidden whenever the receptor is not in flight.
- [ ] `AuraSubtitles` plays no clip per line and holds hints while the teleprompter or voice player is busy (keeps the latest pending line).
- [ ] `AudioHookManager.playEvaluationSounds` exists (default true).
- [ ] `WorldSpaceHud` shows `FIRE = step(w1·ROCK + w2·ICE + b)` with live values. Case labels are Drone / Comet / Rock / Both (index 1 = ICE, index 2 = ROCK).
- [ ] `VoiceLinePlayer` follows the trigger table and interrupt rules in the handoff.
- [ ] Unity console shows no compile errors.
- [ ] Handoff report filed.

## Blocked on

The director events from `TASK_ASTEROID_DEFENSE.md`. `DataTargetVisual` also needs `DataTargetReceptor.IsInFlight`.

## Handoff notes

`TASK_BRIDGE_DECLUTTER.md` wires these components and the voice clips in the scene builder. Section 2 of the handoff lists the Kenney clip names.
