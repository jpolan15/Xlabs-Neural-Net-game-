# Task: Phase 3 — Chamber 01 first fun slice

Status: in-progress
Owner: Gameplay / XR / Presentation
Created: 2026-09-29
Updated: 2026-09-29

## Description

Make the sensor-bay OR puzzle the first fun slice: A.U.R.A. objective, visible sensor pings, haptic detent dials, failure hints that do not reveal weights, and a repair payoff. Asteroid drift replaces the purge timer. The owner asked for Phases 4 and 5 in the same pass; the Link playtest is still outstanding.

## Allowed paths

- Assets/Scripts/Gameplay/ChamberController.cs
- Assets/Scripts/Gameplay/ChamberOnboardingController.cs
- Assets/Scripts/Gameplay/FailureHintDirector.cs
- Assets/Scripts/Gameplay/AsteroidDrift.cs
- Assets/Scripts/Presentation/CanopyShutters.cs
- Assets/Scripts/Presentation/WorldSpaceHud.cs
- Assets/Scripts/Presentation/WristObjective.cs
- Assets/Scripts/XR/XRInteractableBridge.cs
- Tools/Editor/Level01SceneBuilder.cs
- Tests/PlayMode/Gameplay/Level01IntegrationTests.cs
- Tests/PlayMode/XR/SensorPingTests.cs

## Forbidden paths

- Assets/Samples/**
- Assets/Scripts/Core/**
- ProjectSettings/**

## Acceptance criteria

- [ ] Wrist objective and A.U.R.A. line state the OR beacon goal.
- [ ] Four sensor cases light from DiagnosticReport. Dials keep haptic detents. Cables, crystal, and lever still raise gameplay events.
- [ ] Three failure hints explain the cause and do not state the weights.
- [ ] Solving opens shutters, restores lights, and unlocks the sensor-bay door.
- [ ] The ship drifts toward the asteroids with no death state.
- [ ] PlayMode tests cover the loop and an XR select event.
- [ ] Handoff report filed.

## Blocked on

Phase 2 ship layout. Link playtest by the owner is still open; later phases proceed because the owner asked to finish the handoff.

## Handoff notes

Do not hardcode the OR solution. Evaluation stays in PuzzleEvaluator.
