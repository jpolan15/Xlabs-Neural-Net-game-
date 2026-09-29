# Task: Upgrade Level 1 with Official Unity Sample Assets and Premade 3D Models

Status: in-progress
Owner: Presentation / XR / Scene Agent
Created: 2026-09-22
Updated: 2026-09-22

## Description

Replaces procedural primitive geometry (cubes, cylinders, spheres) across Level 1 — The Awakening Gate with authentic pre-made 3D models, PBR textures, and physical interactive components imported from official Unity sample packages (com.unity.xr.interaction.toolkit Starter Assets, Hands Interaction Demo, and URP Package Samples).

## Allowed paths

- Assets/Samples/**
- Assets/Materials/**
- Assets/Prefabs/**
- Assets/Scripts/Presentation/**
- Assets/Scripts/XR/**
- Tools/Editor/Level01SceneBuilder.cs
- Tools/Build-Level01Scene.ps1
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# without Unity DLLs)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/** (Manifest and lock file must remain unchanged)

## Acceptance criteria

- [ ] Import official Unity XR and URP sample assets into `Assets/Samples/` (FBX models, PBR textures, audio).
- [ ] Replace crude primitive Neural Pulse Tool geometry with real `Primitive_Blaster.fbx` / `Blaser.prefab` viewmodel with recoil and laser VFX.
- [ ] Upgrade Tactile Console with `VirtualTabletop.fbx` / `TeleportAnchorPlatform.fbx` and `PushButton.fbx` with mechanical audio feedback.
- [ ] Replace squashed cylinder neural rings with smooth `Primitive_Torus.fbx` orbital gyroscopes.
- [ ] Upgrade Decision Activation Crystals to faceted geometric models (`Primitive_Pyramid.fbx`, `Primitive_Wedge.fbx`, `Primitive_Tapered_Cylinder.fbx`).
- [ ] Upgrade Automated Defense Sentry turret with dual heavy blaster barrels (`Primitive_Blaster_Long.fbx`) and high-tech swivel pedestal.
- [ ] Equip XR Origin rig with physical motion controller models (`UniversalController.fbx` / `XR Controller Left/Right.prefab`).
- [ ] Level01SceneBuilder cleanly builds the scene without errors or missing references.
- [ ] Validate-CoreBoundaries.ps1 passes with 0 violations.
- [ ] Validate-RepositoryLayout.ps1 passes with 0 violations.

## Blocked on

None

## Handoff notes

Asset integration maintains full backward compatibility with all existing game logic, interactors, and puzzle evaluation events.
