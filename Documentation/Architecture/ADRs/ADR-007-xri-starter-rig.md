# ADR-007: XRI Starter Assets Rig Replaces the Custom Pointer Rig

| Field | Value |
|---|---|
| ID | ADR-007 |
| Date | 2026-09-29 |
| Status | Accepted |
| Deciders | Architecture, XR |

## Context

Level 01 shipped with a hand-built rig: an empty "XR Origin (VR Rig)" transform, a camera with no `TrackedPoseDriver`, and a custom `VRControllerPointerInteractor` that read legacy `InputDevices`, raycast with `Physics.Raycast`, and moved the rig with `transform.position +=`. `DesktopInputFallback` also wrote the camera rotation every frame whenever its one-time XR check in `Awake` ran before OpenXR was ready.

Playtest over Quest Link showed the direct consequences: the view snapped back to a fixed pose, locomotion passed through walls, and the right controller could not reliably point at or use anything. The rig also violated ADR-005, which makes XR Interaction Toolkit the sole interaction authority.

## Decision

- Every playable scene uses the XRI 3.6 Starter Assets prefab `Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab`, placed by the scene builder with `PrefabUtility.InstantiatePrefab`.
- Head tracking comes from the prefab's `TrackedPoseDriver`. No script writes the camera transform.
- Locomotion (continuous move, snap turn, teleport) runs through XRI locomotion providers and the rig's `CharacterController`, so wall colliders block the player.
- Pointing, grabbing and UI use the prefab's Near-Far interactors. First-party interactables expose XRI events (`XRSimpleInteractable` / `XRGrabInteractable`) through `XRInteractableBridge` and never raycast themselves.
- World UI uses world-space canvases with `TrackedDeviceGraphicRaycaster`. `OnGUI` is never used for player-facing UI.
- `VRControllerPointerInteractor` is removed. `DesktopInputFallback` is removed from the rig; desktop testing uses the XR Device Simulator or Meta XR Simulator (ADR-005).
- The rig's tracking origin is Floor.
- First-party gameplay behaviours (dials, sliders, lever, cable, socket, terminal, target receptor) keep their existing public methods and events. `XRInteractableBridge` (an `XRSimpleInteractable`) is added to each of them by the scene builder. Dials and sliders respond to wrist twist while selected (one detent per 18 degrees) and to a quick tap; all other controls act on select. The builder also adds a padded collider (minimum 14 cm) to any control smaller than that.
- Android is configured for Quest builds: `XRGeneralSettingsPerBuildTarget` has an Android entry with the OpenXR loader, and the OpenXR Android features `MetaQuestFeature` and `OculusTouchControllerProfile` are enabled.

## Consequences

- Head tracking, collision-aware locomotion and ray interaction work over Link and on device with no custom code.
- Interactables are testable through XRI events in PlayMode without a headset.
- The Starter Assets sample becomes a runtime dependency of the scene. It must not be deleted or modified; upgrades of XRI require re-importing the matching sample and re-running the scene builder.

## Supersedes

Nothing. Implements ADR-005.
