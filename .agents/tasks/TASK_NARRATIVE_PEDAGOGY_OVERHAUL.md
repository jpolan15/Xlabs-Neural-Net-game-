# Task: Narrative and Pedagogical Overhaul for Round 1 (Level 1)

Status: completed
Owner: Presentation / Gameplay / Level Design Agent
Created: 2026-09-21
Updated: 2026-09-21

## Description

Overhauls Round 1 ("The Awakening Gate") to make it simple, engaging, and pedagogically rich for beginners with zero AI background.
Grounds the puzzle in a tangible facility emergency: "The Sector 01 Hazard Containment Purge".
Explains clearly WHY we upload data into the neural net, WHY we need activation functions, and WHAT weights, bias, and sensor inputs represent in the real world.

## Allowed paths

- Assets/Scripts/Presentation/**
- Assets/Scripts/Gameplay/**
- Tools/Editor/Level01SceneBuilder.cs
- Documentation/Design/LEVEL_DESIGN_L01.md
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# and untouched)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/**

## Acceptance criteria

- [x] `EngineerFieldManualVisual.cs` updated with 5 intuitive pages covering the story mission, why we test data packets, volume/sensitivity weights, noise bias, and live sensor calculation.
- [x] `DiagnosticHologramVisual.cs` updated with concrete room scenarios (Clean Room, Bio-Leak, Radiation, Dual Hazard) and live double-loop diagnostic feedback.
- [x] `NeuronPedagogyHologramVisual.cs` updated with the Hazard Classifier architecture breakdown and plain-English sensor explanations.
- [x] `SciFiEngineerHUD.cs` updated with the emergency story header, scenario status badges, and descriptive tooltips.
- [x] `ClassicNeuralNetwork3DVisualizer.cs` updated with clear 3D billboard layer tags.
- [x] `Level01SceneBuilder.cs` in `Tools/Editor/` updated with matching console, signboard, and hologram labels (duplicate in `Assets/Editor/` cleaned up).
- [x] `LEVEL_DESIGN_L01.md` updated with complete narrative scenario and pedagogical guidelines.
- [x] Boundary validation (`Validate-CoreBoundaries.ps1`) passes with 0 violations.
- [x] Repository layout validation (`Validate-RepositoryLayout.ps1`) passes with 0 violations.
- [x] Unit tests pass 41/41 (100%).
