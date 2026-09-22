# Task: Sentry Defense and Tactical Pressure VR Overhaul

Status: completed
Owner: Full-Stack VR Gameplay / Presentation Agent
Created: 2026-09-22
Updated: 2026-09-22

## Description

Transforms Convergence into an exciting, high-stakes tactical VR defense puzzle game ("The Sentry Intercept Crisis"). Replaces dry weight knob tweaking with a live automated defense sentry, distinct 3D target drone/canister hazards, environmental alarm pressure, tactile kinetic power sliders, squelch pressure valve, dynamic laser aiming and plasma discharge FX, and high-urgency pedagogical voice guidance.

## Allowed paths

- Assets/Scripts/Presentation/**
- Assets/Scripts/XR/**
- Assets/Scripts/Gameplay/**
- Tools/Editor/**
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# and untouched)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/**

## Acceptance criteria

- [x] Implement `DefenseSentryVisual.cs` in Presentation with dynamic turret swiveling, barrel aiming, laser tracking, plasma projectiles, muzzle flashes, and target scanning reticles.
- [x] Upgrade `DataTargetVisual.cs` and `DataTargetReceptor.cs` with 4 distinct living physical targets (Friendly Drone, Biohazard Canister, Radiation Drone, Dual-Breach Pod) and explosion/shield FX.
- [x] Implement `KineticWeightSliderInteractor.cs` in XR with chunky tactile sliders, detented steps, illuminated LED tick marks, plasma core tubes, and haptic feedback.
- [x] Upgrade `BiasDialInteractor.cs` into a Threshold Squelch Valve with gauge feedback and pressure venting SFX.
- [x] Upgrade `ChamberController.cs` to sequence dramatic live sentry target trials (sequential target scans & shots).
- [x] Upgrade `ChamberOnboardingController.cs`, `FacilityAIVoiceAnnouncer.cs`, `SciFiEngineerHUD.cs` with high-stakes defense narrative, threat alarms, and non-spoiler tactical guidance.
- [x] Update `Level01SceneBuilder.cs` to generate the complete animated Defense Sentry platform, target bays, alarm beacons, and tactical console in `Level01_AwakeningGate.unity`.
- [x] Boundary validation (`Validate-CoreBoundaries.ps1`) passes with 0 violations.
- [x] Repository layout validation (`Validate-RepositoryLayout.ps1`) passes with 0 violations.
- [x] Core unit test suite (`Run-CoreUnitTests.ps1`) passes 41/41 tests.
