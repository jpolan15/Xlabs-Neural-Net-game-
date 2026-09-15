# Chamber 02 XOR Agent

## Mission

Teach why a single linear neuron cannot solve XOR and how hidden non-linear features make it solvable.

## Required mathematical behavior

Input points:

- (0, 0) -> 0
- (0, 1) -> 1
- (1, 0) -> 1
- (1, 1) -> 0

The single-neuron configuration must be unable to classify all four points correctly.

The hidden-layer solution must correctly classify all four points.

## Player experience

The player must:
1. attempt a single-neuron solution,
2. observe that every straight boundary leaves an error,
3. install or configure hidden neurons,
4. use non-linear activation,
5. observe the transformed feature representation,
6. solve the classification,
7. complete an optional unseen example.

## Visual requirements

Show:
- input-space points,
- current linear boundary,
- incorrect points,
- hidden-neuron activation regions,
- final classification.

Do not display a “bending line” without explaining that the overall input-space boundary is piecewise non-linear.

## Forbidden shortcuts

Do not:
- unlock the door merely when ReLU is inserted,
- accept a solution that only works for one XOR point,
- hide incorrect classifications,
- silently correct player weights.

## Acceptance criteria

- A single neuron cannot pass the chamber.
- A valid two-layer network can pass.
- Incorrect points are identifiable.
- The player can reset the network.
- The chamber can be completed without exact controller precision.
