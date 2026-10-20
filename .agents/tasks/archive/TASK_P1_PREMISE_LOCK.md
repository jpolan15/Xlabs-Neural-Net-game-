# Task: Phase 1 — One premise

Status: in-progress
Owner: Design / Gameplay / Presentation
Created: 2026-09-29
Updated: 2026-09-29

## Description

Lock the lost-in-space premise (ADR-008). The live level teaches a one-neuron OR sensor, shows that neuron, and drops sentry, gun, motherboard, and Synapse-GPT copy. W1, W2, and bias stay independent.

## Allowed paths

- Documentation/Architecture/ADRs/ADR-008-premise-lost-in-space.md
- .agents/DECISIONS_INDEX.md
- .agents/tasks/BACKLOG.md
- .agents/tasks/COMPLETED.md
- .agents/tasks/IN_PROGRESS.md
- Documentation/Design/GDD_OVERVIEW.md
- Documentation/Design/LEVEL_DESIGN_L01.md
- Assets/Puzzles/Chamber01/AGENTS.md
- Assets/Puzzles/Chamber02_XOR/AGENTS.md
- Assets/Puzzles/Chamber03_Training/AGENTS.md
- Assets/Puzzles/Chamber04_Attention/AGENTS.md
- Assets/Scripts/Gameplay/NeuralState.cs
- Assets/Scripts/Gameplay/ChamberController.cs
- Assets/Scripts/Gameplay/ChamberOnboardingController.cs
- Assets/Scripts/Presentation/NeuronTopologyVisualizer.cs
- Assets/Scripts/Presentation/ClassicNeuralNetwork3DVisualizer.cs
- Assets/Scripts/Presentation/DefenseSentryVisual.cs
- Assets/Scripts/Presentation/NeuralGunViewmodel.cs
- Assets/Scripts/Presentation/MotherboardEnvironmentVisual.cs
- Assets/Scripts/Presentation/SciFiEngineerHUD.cs
- Assets/Scripts/Presentation/EngineerFieldManualVisual.cs
- Assets/Scripts/Presentation/PlanetaryAICoreHologram.cs
- Assets/Scripts/Presentation/WorldSpaceHud.cs
- Tools/Editor/Level01SceneBuilder.cs

## Forbidden paths

- Assets/Samples/**
- Packages/**
- ProjectSettings/**
- Assets/Scripts/Core/**

## Acceptance criteria

- [ ] ADR-008 exists and is indexed.
- [ ] Design docs and chamber stories match the lost-in-space arc. Math requirements in the chamber files stay.
- [ ] Sentry, gun, and motherboard types are not placed and the scripts are deleted.
- [ ] The visualizer draws the live network: two inputs, bias, one neuron, one output.
- [ ] SetUnifiedWeight does not write bias.
- [ ] Forward-pass paths do not fall back to CreateEarthLocationPuzzle.
- [ ] Sentry and gun tasks are marked dropped, not done.
- [ ] Layout validation passes.
- [ ] Handoff report filed.

## Blocked on

None.

## Handoff notes

Phase 2 builds the walkable ship. Do not wire the Earth puzzle until Phase 5.
