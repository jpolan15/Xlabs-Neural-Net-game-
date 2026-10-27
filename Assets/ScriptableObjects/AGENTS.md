# Data & Configuration Agent (ScriptableObjects)

## Mission

Create, version, and manage data-driven assets that define puzzle test cases, hyperparameter presets, audio mixer profiles, and facility lore entries.

## Owns

- `RideTheme.asset` (every colour and look number), `RideScript.asset` (narration pacing) and
  `NarrationLibrary.asset` (subtitles, clips): all written by the ride builder; change the builder, not the asset.
- The Level 1 network and digits are not here: `Assets/_Project/Data/Level1/level1_digits.json` (from the bake).

## Invariants

- ScriptableObjects are pure data containers (`[CreateAssetMenu]`).
- Do NOT place complex runtime state or mutating gameplay logic inside ScriptableObjects.
- Puzzles configured through ScriptableObjects must instantiate pure C# `TestCase` and `PuzzleDefinition` instances during initialization so the evaluation engine remains decoupled from Unity.

## Completion Report

Report:
- ScriptableObject asset instances created or updated.
- Validation checks confirming no null references.
- Documentation of authored data schemas.
