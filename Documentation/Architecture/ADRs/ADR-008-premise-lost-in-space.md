# ADR-008: Lost-in-Space Premise Replaces the Sentry and Synapse-GPT Story

| Field | Value |
|---|---|
| ID | ADR-008 |
| Date | 2026-09-29 |
| Status | Accepted |
| Deciders | Design, Gameplay |

## Context

Level 1 had accumulated a second story on top of the neuron puzzle. Copy talked about Synapse-GPT, a defense sentry, a neural gun, and a motherboard room, while the voice line and the math were about an OR gate. The simulation on the pedestal was a 3-4-2 drawing. The network the player was actually tuning was one neuron with two inputs.

## Decision

An asteroid strike knocks the ship out of hyperspace. The navigation AI, A.U.R.A., has lost its trained weights. The player is alone with it. Each chamber teaches one real ML concept and changes the ship:

1. Chamber 01, sensor array: perceptron OR. Power and shutters.
2. Chamber 02, spectrum filter: XOR and a hidden layer. Telescope comes online.
3. Telescope photo mode, then Chamber 03: label photos EARTH or NOT EARTH, train, fail honestly on a bad set.
4. Chamber 04, nav computer: attention over log tokens. Then the jump, and Earth fills the window.

Cut from the live game: sentry defense, the neural gun, the motherboard room, Synapse-GPT, and any HUD copy that says "calibrate sentry" while the voice talks about Earth.

The pedestal visualizer reads the live `NetworkModel`. W1, W2, and bias are independent controls. Earth is not loaded as the Chamber 01 puzzle.

## Consequences

- Older task files that specify sentry waves or the neural gun are dropped. They are not marked done.
- Chamber 01 stays an OR truth table. The later chambers are the only place Earth, XOR, training, and attention appear.
- `PuzzleEvaluator` has to score more than a single binary output before Chamber 03 and Chamber 04 can be graded honestly. That work is the Core training change, not a special case in the scene.

## Supersedes

The live-game portions of the sentry, gun, and Synapse-GPT narrative. Historical task files stay in `.agents/tasks/` as a record.
