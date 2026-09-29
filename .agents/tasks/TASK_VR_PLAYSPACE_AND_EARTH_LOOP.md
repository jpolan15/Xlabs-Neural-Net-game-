# Task: Headset playspace, free look, and Level 1 Earth-return loop

Status: complete
Owner: XR / Presentation / Gameplay prompts / Scene builder
Created: 2026-09-29
Updated: 2026-09-29

## Description

Headset playtest reported a build that is not playable: locomotion passes through the room, look snaps back to center, movement stays on the room's forward axis, and controller rays only hit what is already straight ahead. Level 1 must also read as a lost ship: upload Earth images, tune weights, and head home when the existing evaluator passes.

The playable puzzle stays the existing 2-input perceptron (empty space vs Earth land/atmosphere/full Earth). `CreateEarthLocationPuzzle` is a 16-input network the console cannot tune, so evaluation is not switched over to it.

## Allowed paths

- `Assets/Scripts/XR/**`
- `Assets/Scripts/Presentation/LostInSpaceMissionBoard.cs`
- `Assets/Scripts/Presentation/FacilityAIVoiceAnnouncer.cs`
- `Assets/Scripts/Presentation/SciFiEngineerHUD.cs`
- `Assets/Scripts/Presentation/DataTargetVisual.cs`
- `Assets/Scripts/Gameplay/ChamberOnboardingController.cs`
- `Tools/Editor/Level01SceneBuilder.cs`
- `.agents/tasks/**`

## Forbidden paths

- `Assets/Scripts/Core/**`
- `Tests/EditMode/Core/**`
- `Packages/**`
- `ProjectSettings/**`

## Acceptance criteria

- [x] Walking uses a body capsule against the existing wall, floor, and console colliders, and a room clamp if a query misses.
- [x] Headset yaw and pitch drive the camera and are not rewritten to center.
- [x] Thumbstick locomotion follows headset heading, including strafe.
- [x] Controller rays come from the tracked hand and ignore the rig's own colliders.
- [x] A world-space board states the lost-in-space alert, the Earth-image steps, live weights, and the heading-home beat only after the evaluator passes.
- [x] Desktop fallback still owns the camera when no XR session is running.
- [x] Core assemblies are unchanged.
- [ ] Headset playtest was not run in this environment.

## Blocked on

None

## Handoff notes

Scene `Assets/Scenes/Level01_AwakeningGate.unity` already contains `VRControllerPointerInteractor` and `DesktopInputFallback`. Runtime scripts repair tracking, collision, and the mission board without a scene rebuild. `Level01SceneBuilder` wires the same objects for the next rebuild.
