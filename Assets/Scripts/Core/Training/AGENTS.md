# Gradient Descent & Training Agent

## Mission

Implement deterministic backpropagation, gradient calculation, optimizer algorithms, and readable hyperparameter failure modes for automated training puzzles.

## Owns

- Backpropagation algorithm implementations.
- Stochastic and Batch Gradient Descent optimizers.
- Configurable hyperparameters: learning rate ($\eta$), batch size, epochs, momentum, input normalization.
- Training failure mode detectors:
  - Exploding gradients / weight oscillation (learning rate too high).
  - Stalled convergence / plateaus (learning rate too low / vanishing gradients).
  - Dead neurons (ReLU bias pushed too far negative).
  - Feature dominance (unnormalized inputs causing one dial to dominate).

## Allowed Dependencies

- `Convergence.Core.Math`
- `Convergence.Core.Neural`
- `System`
- `System.Collections.Generic`

## Forbidden Dependencies

- `UnityEngine`
- `MonoBehaviour`

## Educational Invariants

- Training must be **deterministic**: using identical seeds and hyperparameter inputs produces identical step-by-step weight updates.
- Training is a tool configured by the player, not an unexplainable cutscene.
- Step-by-step telemetry must be emitted so visualization agents can animate backprop pulses and physical knob rotations.

## Testing Requirements

- Verify single neuron regression converges to target weight within tolerance.
- Verify XOR 2-layer network converges with ReLU and appropriate learning rate.
- Verify high learning rate produces detectable oscillation/explosion.

## Completion Report

Report:
- Algorithms and optimizers implemented.
- Convergence test benchmarks.
- Determinism verification.
