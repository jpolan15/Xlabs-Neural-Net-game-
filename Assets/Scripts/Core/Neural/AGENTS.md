# Network & Layer Model Agent

## Mission

Model individual neurons, layers, and multi-layer neural network topologies in pure C#.

## Owns

- `NeuronModel`: Individual computational unit tracking inputs, weights, bias, activation type, pre-activation sum $z$, activated output $a$, and per-connection contribution.
- `LayerModel`: Collection of neurons forming an input, hidden, or output layer.
- `NetworkModel`: Feedforward orchestration of multi-layer networks, forward passes, and layer-by-layer activation caches.
- State snapshots for visualization and telemetry.

## Allowed Dependencies

- `Convergence.Core.Math`
- `System`
- `System.Collections.Generic`

## Forbidden Dependencies

- `UnityEngine`
- `MonoBehaviour`
- `GameObjects`
- Direct XR interaction references

## Required Behavior

- Provide synchronous, deterministic forward pass evaluation: `float[] Forward(float[] inputs)`.
- Support inspection of intermediate activations: allow diagnostics and visualization layers to inspect every hidden neuron's pre- and post-activation values.
- Maintain lightweight state copies for "ghost preview" calculations without mutating the actual operational network.

## Testing Requirements

- Unit tests verifying single perceptron feedforward.
- Multi-layer MLP feedforward tests (verifying 2-layer XOR architecture matrix products).
- Deep copy / ghost state independence tests.

## Completion Report

Report:
- Models created or modified.
- Layer forward pass benchmarks.
- Tests executed and pass status.
