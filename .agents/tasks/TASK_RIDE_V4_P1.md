# Task: Ride v4 P1, Core math for the new stops

Status: complete
Owner: Core
Created: 2026-10-20
Updated: 2026-10-20

## Description

Phase P1 of Ride v4 (ADR-016, `Documentation/Architecture/ADRs/ADR-016-ride-v4-neuron-to-agents.md`). One PR per phase, branched from `jonathan/polishing-level-1`. Do P3 to P8 one at a time.

## Allowed paths

- `Assets/Scripts/Core/**`
- `Tests/EditMode/Core/**`
- `Documentation/Mathematics/FORMULAS.md`

## Forbidden paths

- Anything not listed above. `Assets/Scenes/NeuralRide.unity` is only ever written by the builder.

## Acceptance criteria

- [x] `VectorMath`: Add, Sub, Dot, Cosine, Nearest(target, table, exclude)
- [x] `WordSpace`: fixed word vectors; every listed analogy resolves by `Nearest`; clusters tighter than between-cluster distance
- [x] `NextTokenModel`: embeddings + one attention head + readout + Softmax(temperature); "it" attends most to "rock"; T=0.2 picks "laser"; T=2 spreads; probabilities sum to 1
- [x] `XorPuzzle` (2-2-1 step net via `NetworkModel`), `AnalogyPuzzle`, `AttentionPuzzle`, `AgentStepEvaluator`: each passes only at 100 %
- [x] `ManualDescentScore`: player loss vs trainer loss
- [x] EditMode green, `Validate-CoreBoundaries.ps1` green, no UnityEngine in Core

## Blocked on

None

## Handoff notes

Report every command and its exact output. Say plainly what was not verified (headset, ears, hands).
