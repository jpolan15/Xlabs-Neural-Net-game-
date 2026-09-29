# Level 1 — The Awakening Gate

## Overview

- **Scene**: `Assets/Scenes/Level01_AwakeningGate.unity`
- **Builder**: `Convergence/Build Level 1 — The Awakening Gate`
- **Device**: Quest 2, OpenXR, XR Interaction Toolkit. Desktop play uses `DesktopWalk` until a headset is present.
- **Puzzle**: one neuron, two beacon inputs, bias, step activation, OR truth table.

## Story

An asteroid strike drops the ship out of hyperspace. A.U.R.A. has lost her weights. Chamber 01 is the sensor array. Later rooms are the observatory and the engine bay. Earth is the ending, not the first puzzle.

## The four pings

| Case | Radio \(x_1\) | Light \(x_2\) | Target | Canonical margin \(w=1,1\), \(b=-0.5\) |
|:---:|:---:|:---:|:---:|:---|
| Quiet | 0 | 0 | 0 | \(z = -0.5 \to 0\) |
| Radio | 0 | 1 | 1 | \(z = 0.5 \to 1\) |
| Light | 1 | 0 | 1 | \(z = 0.5 \to 1\) |
| Both | 1 | 1 | 1 | \(z = 1.5 \to 1\) |

The canonical margin is the math check. The player is not shown it as a recipe. Three failure hints name the missed case and whether the sum was too weak or too strong. They do not state a weight.

## What the player touches

- Two cables. A disconnected cable forces that input to 0.
- W1, W2, and bias. They write three different numbers. Nothing auto-sets the bias from a weight.
- A step crystal. Linear or ReLU cannot pass.
- A lever. It runs `PuzzleEvaluator` on all four rows.
- XRI select is the interaction. Dials twist with haptic detents while held. Desktop walk calls the same `Activate` and `Step` methods.

## The picture

`NeuronTopologyVisualizer` reads the live `NetworkModel`. For this chamber that is two inputs, a bias, one neuron, and one output. The root stays at `(0, 1.55, 0.55)` with pivot scale `0.85`. The rest pose is captured on the first Update.

## The ship

The hull is the prefab `Assets/_Project/Prefabs/Ship/PF_Ship_Hull.prefab`, built from Kenney modules. A corridor behind the sensor-bay door leads to the observatory and the engine room. Walls keep colliders. Sparks, a flickering light, and a breach patch clear when a repair lands. The skybox is the generated starfield `Assets/Materials/Sky_Starfield`. One directional light stays realtime. The other lights are baked, and a global volume carries bloom.

## What this level does not contain

No sentry, no neural gun, no motherboard room, no Synapse-GPT line.
