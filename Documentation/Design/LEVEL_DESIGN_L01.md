# Level 1 — The Awakening Gate

## Overview

- **Scene**: `Assets/Scenes/Level01_AwakeningGate.unity`
- **Builder**: `Convergence/Build Level 1 — The Awakening Gate`
- **Device**: Quest 2, OpenXR, XR Interaction Toolkit. Desktop play uses `DesktopWalk` until a headset is present.
- **Puzzle**: one neuron, two sensor inputs (ROCK, ICE), bias, step activation, OR truth table. Decision record: ADR-010.

## Story

An asteroid strikes the ship *Neural*. The player's own recording plays: "Warning, passengers of Neural. This is not a drill. Our ship has lost connection. It is your job to reconnect it, or we will be stuck in space forever." Sparks fly, the targeting cables pop out of the console, and the asteroid field slides into view through the canopy.

The ship has an automatic point-defense laser. Its brain is the neuron on the console. The player reconnects the cables and tunes the neuron so the laser fires at rocks and ice and lets the friendly repair drone dock. Earth is the ending, not the first puzzle.

## Opening timeline

| Time (s) | What happens |
|---:|---|
| 0.00 | Impact boom, red lights, sparks. Voice line starts. Stasis doors open at about 3 s. The player is never frozen. |
| 5–7 | Axons go dark. Cables spark and pop out (they start disconnected because of the opening preset). |
| 10–12 | Asteroid field slides into view. |
| 12.48, 13.72, 14.92, 16.20 | Alarm pulses on each "warning". |
| 16.72 | Banner clears. |
| 18.24 | Recording ends. The first wave and the next hint may start. |

No camera shake at any point.

## The four objects

Case order is fixed by `PuzzleDefinition.CreateORGatePuzzle()`.

| Index | Object | ROCK \(x_1\) | ICE \(x_2\) | FIRE target | Canonical margin \(w=1,1\), \(b=-0.5\) |
|:---:|:---|:---:|:---:|:---:|:---|
| 0 | Repair Drone | 0 | 0 | 0 | \(z = -0.5 \to 0\), docks |
| 1 | Icy Comet | 0 | 1 | 1 | \(z = 0.5 \to 1\), vaporized |
| 2 | Rocky Asteroid | 1 | 0 | 1 | \(z = 0.5 \to 1\), vaporized |
| 3 | Rock-and-Ice Chunk | 1 | 1 | 1 | \(z = 1.5 \to 1\), vaporized |

The canonical margin is the math check. The player is not shown it as a recipe. Failure hints name the missed object and whether the sum was too weak or too strong. They do not state a weight.

## The defense loop

- Objects spawn about 34 m out, travel about 8 s, and are scanned at about 65% of the trip (a glowing scan gate).
- Each scan asks the chamber to evaluate the live network with `PuzzleEvaluator`. The scan result decides the outcome:
  - Fired at a threat: laser vaporizes it.
  - Fired at the drone: friendly fire, small hull damage.
  - Held fire on the drone: it docks and restores a little hull.
  - Held fire on a threat: it hits the hull (about 12 damage).
- At 0 hull the ship reroutes power to 60%. Settings are kept. There is no game over.
- When every row passes, the puzzle is solved exactly once. A three-wave victory swarm replays the OR cases, the lesson line plays, lights turn blue, and the bridge door opens.

## What the player touches

- Two cables: ROCK SENSOR and ICE SENSOR. A disconnected cable forces that input to 0. Both start disconnected.
- W1, W2, and TRIGGER BIAS. They write three different numbers. Nothing auto-sets the bias from a weight.
- A step crystal. Linear or ReLU cannot pass.
- A SELF-TEST lever. It runs `PuzzleEvaluator` on all four rows and can solve the puzzle honestly.
- XRI select is the interaction. Dials twist with haptic detents while held. Desktop walk calls the same `Activate` and `Step` methods.

## The picture

`NeuronTopologyVisualizer` reads the live `NetworkModel`. For this chamber that is two inputs, a bias, one neuron, and one output. The root stays at `(0, 1.55, 0.55)` with pivot scale `0.85`. The rest pose is captured on the first Update.

One world-space formula panel shows `FIRE = step(w1·ROCK + w2·ICE + b)` with live values. Console lamps ROCK, ICE, and FIRE light on each scan.

## The ship

The hull is the prefab `Assets/_Project/Prefabs/Ship/PF_Ship_Hull.prefab`, built from Kenney modules. The canopy glass is at z = 3.5. The laser muzzle sits near the nose. A corridor behind the bridge door leads to the observatory and the engine room. Walls keep colliders. Sparks, a flickering light, and a breach patch clear when a repair lands. The skybox is the generated starfield `Assets/Materials/Sky_Starfield`. One directional light stays realtime. The other lights are baked, and a global volume carries bloom.

## What this level does not contain

No handheld gun, no aiming, no sentry turret the player operates, no motherboard room, no Synapse-GPT line.
