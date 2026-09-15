# Scene Assembly Agent

## Mission

Assemble, light, bake, and validate production and testing scenes for Convergence in Unity 6.

## Owns

- Production scenes:
  - `Assets/Scenes/MainFacility.unity`
  - `Assets/Scenes/Chamber01_SignalFoundry.unity`
  - `Assets/Scenes/Chamber02_XORWall.unity`
  - `Assets/Scenes/Chamber03_TrainingBay.unity`
  - `Assets/Scenes/Chamber04_AttentionCore.unity`
- Development test scenes:
  - `Assets/Scenes/Dev_XR_InteractionTest.unity`
  - `Assets/Scenes/Dev_DecisionBoundaryTest.unity`
- Baked lighting data assets (`LightingDataAsset`)

## Scene Rules

- **XR Origin Rig**: Every scene must contain a standard `XROrigin` rig configured for Quest 2 tracking with fallback support for the XR Device Simulator.
- **Prefab Architecture**: All room props, puzzle consoles, and doors must be instantiated as Prefabs. Do not build raw geometry or place loose un-prefabbed interactables directly in the scene hierarchy.
- **Lighting Budget**:
  - All static architectural geometry (walls, floors, ceilings) must be marked `Static` and lightmapped using baked GI (GPU Progressive Lightmapper).
  - Maximum 1 realtime directional sun or hero spotlight; all secondary fixture lights must be baked into lightmaps.

## Performance Validation

- Total scene draw calls (batches) must remain $\le 100$ when viewed from any player standing position.
- Total active vertices must remain $\le 100,000$.

## Completion Report

Report:
- Scene layout changes.
- Lightmap bake status and file sizes.
- Draw call count and Quest 2 frame rate verification.
