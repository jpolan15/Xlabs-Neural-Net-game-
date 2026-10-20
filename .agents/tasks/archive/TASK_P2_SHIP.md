# Task: Phase 2 — The ship

Status: in-progress
Owner: Presentation / Tools/Editor
Created: 2026-09-29
Updated: 2026-09-29

## Description

Replace the primitive hull with Kenney module prefabs the scene only instantiates. Add a corridor and three rooms, a CC0 starfield, repair-clearing damage, bloom, and A.U.R.A. subtitles.

## Allowed paths

- Tools/Editor/Level01SceneBuilder.cs
- Tools/Editor/ShipLayout.cs
- Assets/_Project/Prefabs/Ship/**
- Assets/_Project/Audio/**
- Assets/Materials/Sky_Starfield*
- Assets/ThirdParty/AGENTS.md
- Assets/Scripts/Presentation/ShipDamageVisual.cs
- Assets/Scripts/Presentation/AuraSubtitles.cs
- Assets/Scripts/Presentation/FloatingAsteroidField.cs

## Forbidden paths

- Assets/Samples/**
- Packages/**
- ProjectSettings/**
- Assets/Scripts/Core/**

## Acceptance criteria

- [ ] The hull in the scene is a prefab instance composed from Kenney modules.
- [ ] Bridge, corridor, sensor bay, observatory, and engine room connect, and walls keep colliders.
- [ ] RenderSettings.skybox is a starfield. The cubemap license is recorded.
- [ ] Sparks, flicker, and a breach patch clear when repairs complete.
- [ ] One realtime key light and a URP volume with bloom.
- [ ] CC0 Kenney sounds live under Assets/_Project/Audio, with world-space A.U.R.A. subtitles.
- [ ] Handoff report filed.

## Blocked on

Phase 1 premise lock.

## Handoff notes

Draw-call and triangle budgets are checked in the Editor after the scene rebuild. A person has not walked the ship over Link.
