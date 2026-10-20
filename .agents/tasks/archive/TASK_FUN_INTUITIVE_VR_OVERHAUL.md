# Task: Fun, Intuitive, and VR-Ready Gameplay Overhaul

Status: completed
Owner: Full-Stack VR Agent
Created: 2026-09-22
Updated: 2026-09-22

## Description

Overhauls Convergence to replace prescriptive cheat sheets with discovery-based puzzle mechanics, eliminate all answer spoilers, make the game VR-ready on Quest 2 (XR Origin + XRI Simple Interactables + in-world UI instead of OnGUI), and add visceral juicy feedback (haptics, particles, sounds, progressive hints, victory celebration).

## Allowed paths

- Assets/Scripts/Gameplay/ChamberOnboardingController.cs
- Assets/Scripts/Gameplay/ChamberController.cs
- Assets/Scripts/Gameplay/PerformanceTracker.cs
- Assets/Scripts/Presentation/NeuronPedagogyHologramVisual.cs
- Assets/Scripts/Presentation/DiagnosticHologramVisual.cs
- Assets/Scripts/Presentation/EngineerFieldManualVisual.cs
- Assets/Scripts/Presentation/SciFiEngineerHUD.cs
- Assets/Scripts/Presentation/FacilityAIVoiceAnnouncer.cs
- Assets/Scripts/XR/WeightRegulatorInteractor.cs
- Assets/Scripts/XR/BiasDialInteractor.cs
- Assets/Scripts/XR/ClockPulseLeverInteractor.cs
- Assets/Scripts/XR/CableInteractable.cs
- Assets/Scripts/XR/ActivationSocketInteractor.cs
- Assets/Scripts/XR/DesktopInputFallback.cs
- Tools/Editor/Level01SceneBuilder.cs
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# and untouched)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/**

## Acceptance criteria

- [x] Strip answer spoilers from `ChamberOnboardingController.cs`, `EngineerFieldManualVisual.cs`, `NeuronPedagogyHologramVisual.cs`, `DiagnosticHologramVisual.cs`, and `SciFiEngineerHUD.cs`.
- [x] Implement progressive discovery onboarding (exploration guidance, A.U.R.A. progressive hints).
- [x] Implement visual energy bars and directional diagnostics (no numerical z-value cheat sheets).
- [x] Add per-case feedback and pulse count tracking to `ChamberController.cs` and `PerformanceTracker.cs`.
- [x] Add VR XRI interactable support to all XR interactor scripts (`WeightRegulatorInteractor`, `BiasDialInteractor`, `ClockPulseLeverInteractor`, `CableInteractable`, `ActivationSocketInteractor`).
- [x] Add controller haptics, detents, and audio feedback to XR interactors.
- [x] Upgrade desktop fallback (`DesktopInputFallback.cs`) with mouse-wheel adjustments and XR auto-detection.
- [x] Update `Level01SceneBuilder.cs` with XR Origin + XR Interaction Manager, VR-visible world-space Canvas for HUD/subtitles, XRI components, and particle effects.
- [x] Pass `Validate-CoreBoundaries.ps1` with 0 violations.
- [x] Pass `Validate-RepositoryLayout.ps1` with 0 violations.
- [x] Maintain all core unit tests passing (41/41).

