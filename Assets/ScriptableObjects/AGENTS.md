# Data & Configuration Agent (ScriptableObjects)

## Mission

Create, version, and manage data-driven assets that define puzzle test cases, hyperparameter presets, audio mixer profiles, and facility lore entries.

## Owns

- Directory structure under `Assets/ScriptableObjects/`:
  - `Puzzles/`: `Chamber01_Config.asset`, `Chamber02_XOR_Config.asset`, etc.
  - `Datasets/`: Training and validation datasets for Chamber 3.
  - `JournalEntries/`: Lore, schematics, and concept unlock data for the engineer's tablet.
  - `XRProfiles/`: Haptic duration and intensity curves, rotary detent angles.

## Invariants

- ScriptableObjects are pure data containers (`[CreateAssetMenu]`).
- Do NOT place complex runtime state or mutating gameplay logic inside ScriptableObjects.
- Puzzles configured through ScriptableObjects must instantiate pure C# `TestCase` and `PuzzleDefinition` instances during initialization so the evaluation engine remains decoupled from Unity.

## Completion Report

Report:
- ScriptableObject asset instances created or updated.
- Validation checks confirming no null references.
- Documentation of authored data schemas.
