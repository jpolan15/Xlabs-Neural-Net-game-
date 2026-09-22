# Task: Intuitive Step-by-Step Environmental Affordances and Dynamic Physical Feedback

Status: completed
Owner: Gameplay / Presentation / Editor Tools Agent
Created: 2026-09-21
Updated: 2026-09-21

## Description

Eliminates ambiguity, information overload, and console errors in Level 1 ("The Awakening Gate").
Introduces step-gated spotlight guidance across physical interactables (cables, dials, crystal socket, clock lever), a sleek single-objective HUD strip ("NEXT ▶"), live mathematical term highlights on the pedagogy board, 4 physical DataTargetReceptor pods in the environment, and fixes the null puzzle evaluator exception.

## Allowed paths

- Assets/Scripts/Gameplay/ChamberController.cs
- Assets/Scripts/Gameplay/ChamberOnboardingController.cs
- Assets/Scripts/Presentation/SciFiEngineerHUD.cs
- Assets/Scripts/Presentation/NeuronPedagogyHologramVisual.cs
- Assets/Scripts/Presentation/DiagnosticHologramVisual.cs
- Assets/Scripts/Presentation/FacilityAIVoiceAnnouncer.cs
- Tools/Editor/Level01SceneBuilder.cs
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# and untouched)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/**

## Acceptance criteria

- [x] Fix `ArgumentNullException: puzzle` bug in `ChamberController.cs` by null-coalescing `_puzzle`.
- [x] Add step-gated physical lighting and pulsing animations to `ChamberOnboardingController.cs`.
- [x] Replace cluttered HUD with a sleek, non-intrusive "NEXT ▶" action strip in `SciFiEngineerHUD.cs`.
- [x] Add real-time parameter change detection and pedagogical insights to `NeuronPedagogyHologramVisual.cs`.
- [x] Enhance high-contrast VR row formatting in `DiagnosticHologramVisual.cs`.
- [x] Prevent repeated audio dialogue spam in `FacilityAIVoiceAnnouncer.cs`.
- [x] Generate 4 physical `DataTargetReceptor` pods and wire interactive lights in `Tools/Editor/Level01SceneBuilder.cs`.
- [x] Boundary validation (`Validate-CoreBoundaries.ps1`) passes with 0 violations.
- [x] Repository layout validation (`Validate-RepositoryLayout.ps1`) passes with 0 violations.
- [x] Core unit test suite (`Run-CoreUnitTests.ps1`) passes 41/41 tests.
- [x] Scene builds cleanly in Unity Editor with 0 errors.
