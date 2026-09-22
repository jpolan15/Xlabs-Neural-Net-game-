# Task: Live Wave Defense & Purge Countdown Overhaul

Status: completed
Owner: Full-Stack VR Gameplay / Presentation Agent
Created: 2026-09-22
Updated: 2026-09-22

## Description

Implements an active 90-second emergency purge countdown timer, station shield integrity mechanics, physically advancing target drone/canister waves along approach corridors, real-time mid-flight sentry plasma intercepts, and screen-shaking breach and friendly-fire penalties.

## Allowed paths

- Assets/Scripts/Gameplay/**
- Assets/Scripts/Presentation/**
- Assets/Scripts/XR/**
- Tools/Editor/**
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# and untouched)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/**

## Acceptance criteria

- [x] Update `DataTargetReceptor.cs` with approach progress ($0.0 \to 1.0$), speed, and breach states.
- [x] Update `ChamberController.cs` with live 90s purge countdown, 100% shield integrity, wave loop, breach/friendly-fire damage penalties, and reset handling.
- [x] Update `DataTargetVisual.cs` to physically move targets along approach rails, spawn explosions on intercept, and trigger shield impact shockwaves on breach.
- [x] Update `DefenseSentryVisual.cs` to dynamically aim at and intercept moving target positions mid-flight.
- [x] Update `SciFiEngineerHUD.cs` with glowing emergency purge countdown clock, shield integrity bar, target distance meters, and screen shake on damage.
- [x] Update `Level01SceneBuilder.cs` with 4 approach rail tracks and containment shield barrier in `Level01_AwakeningGate.unity`.
- [x] Boundary validation (`Validate-CoreBoundaries.ps1`) passes with 0 violations.
- [x] Repository layout validation (`Validate-RepositoryLayout.ps1`) passes with 0 violations.
- [x] Core unit test suite (`Run-CoreUnitTests.ps1`) passes 41/41 tests.
