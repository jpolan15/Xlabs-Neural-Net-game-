# Task: Modern Spaceship Bridge Visual Overhaul with Open-Source 3D Assets

Status: completed
Owner: Presentation / Scene Agent
Created: 2026-09-28
Updated: 2026-09-28

## Description

Replaces the crude procedural primitive cube walls and near-black environment of Level 1 with an authentic, modern open-source sci-fi spaceship bridge based on user-approved design (clean white composite panels, panoramic cockpit canopy framing ringed gas giant, ergonomic command consoles, vibrant lighting, and zero pitch-black void).

Uses open-source (CC0 Public Domain) 3D assets from Kenney Space Station and Space kits in `Assets/ThirdParty/Kenney/`.

## Allowed paths

- Assets/ThirdParty/**
- Assets/Materials/**
- Assets/Scenes/**
- Assets/Scripts/Presentation/**
- Tools/Editor/Level01SceneBuilder.cs
- Tools/Build-Level01Scene.ps1
- .agents/tasks/**

## Forbidden paths

- Assets/Scripts/Core/** (Core assemblies must remain pure C# and untouched)
- Tests/EditMode/Core/** (Core test suite must remain untouched)
- Packages/** (Manifest and lock file must remain unchanged)

## Acceptance criteria

- [x] Import open-source CC0 Kenney Space Station 3D models and textures into `Assets/ThirdParty/Kenney/SpaceStation/`.
- [x] Upgrade `Level01SceneBuilder.cs` to assemble the authentic modern spaceship bridge:
  - Clean molded white composite bulkheads (`wall.fbx`, `wall-corner-round.fbx`, `wall-pillar.fbx`).
  - Wide panoramic forward cockpit observation canopy framing ringed gas giant and stellar vista.
  - Dark rubberized deck with hazard boundary striping and tactical railing.
  - Ergonomic bridge flight consoles (`table-display.fbx`, `computer-screen.fbx`, `computer-wide.fbx`, `chair-armrest-headrest.fbx`).
  - Central holographic projection pedestal (`table-display-planet.fbx`) for the Earth AI core.
- [x] Overhaul lighting palette from dim near-black void to vibrant modern sci-fi illumination with crisp contrast and readability.
- [x] Mount all neural puzzle interactors (weight sliders, bias dial, activation crystal, target receptor pods) cleanly onto the new flight/engineering consoles.
- [x] Build the scene via `Tools/Build-Level01Scene.ps1` and verify clean execution with 0 errors.
- [x] Validate repository layout (`Validate-RepositoryLayout.ps1`) with 0 violations.
- [x] Validate core boundaries (`Validate-CoreBoundaries.ps1`) with 0 violations.
