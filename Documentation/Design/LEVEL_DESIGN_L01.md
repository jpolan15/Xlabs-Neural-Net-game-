# Level 1 — The Awakening Gate: Level Design Specification

## Overview

- **Level Identifier**: `Level01_AwakeningGate`
- **Scene File**: `Assets/Scenes/Level01_AwakeningGate.unity`
- **Core Concept**: Single-neuron binary classification (OR-gate perceptron)
- **Target Platform**: Meta Quest 2 (OpenXR + XR Interaction Toolkit 3.x) with complete Desktop Keyboard/Mouse Fallback

---

## Narrative & Thematic Setting

The player arrives on an isolated observation platform floating in deep space amidst a dormant alien computation megastructure. Before them stands the **Awakening Gate** — a colossal sealed portal pulsing with faint amber warning lights.

Between the player and the gate sits the **Neuron Machine**, an ancient cybernetic structure housing a living neural processing unit. The machine is damaged: its synaptic regulators are misaligned, its conduits are severed, and its activation socket holds an incompatible linear module. To awaken the machine and unlock the gateway, the player must physically repair and calibrate the circuit to solve the fundamental logic of an **OR gate**.

---

## Puzzle Mathematics

The neural unit simulates a single artificial neuron evaluating the Heaviside Step activation:

$$z = w_1 x_1 + w_2 x_2 + b$$
$$y = \text{step}(z) = \begin{cases} 1.0 & \text{if } z \ge 0.0 \\ 0.0 & \text{if } z < 0.0 \end{cases}$$

### The OR Truth Table

| Case | Input $x_1$ | Input $x_2$ | Target Output $y$ | Canonical Margin ($w_1=1.0, w_2=1.0, b=-0.5$) |
|:---:|:---:|:---:|:---:|:---:|
| 1 | 0.0 | 0.0 | 0.0 | $z = 0(1) + 0(1) - 0.5 = -0.50 \to \text{step}(z) = 0$ |
| 2 | 0.0 | 1.0 | 1.0 | $z = 0(1) + 1(1) - 0.5 = +0.50 \to \text{step}(z) = 1$ |
| 3 | 1.0 | 0.0 | 1.0 | $z = 1(1) + 0(1) - 0.5 = +0.50 \to \text{step}(z) = 1$ |
| 4 | 1.0 | 1.0 | 1.0 | $z = 1(1) + 1(1) - 0.5 = +1.50 \to \text{step}(z) = 1$ |

### Strict Evaluation Constraints

1. **100% Binary Accuracy Threshold**: The gate opens **only** when all 4 cases are simultaneously correct ($Accuracy = 1.0$).
2. **Negative Gate Protection**: A configuration with 3 out of 4 correct cases (e.g. $w_1=0, w_2=0, b=0$) produces $Accuracy = 0.75$ and strictly keeps the gateway sealed.
3. **Step Activation Requirement**: The evaluator checks that `ActivationType.Step` is physically active. Linear, ReLU, or Sigmoid modules will fail calibration even if numerical values align.
4. **Physical Conduit Continuity**: Disconnecting a conduit sets that input to zero ($x_i = 0$), forcing the player to physically reconnect broken cables.

---

## Player Equipment & Interactors

1. **Neural Pulse Tool (`NeuralPulseToolInteractor`)**:
   - Handheld calibration tool.
   - Pointing and pulling the trigger fires an energy beam into the neuron machine, triggering forward propagation and live diagnostic evaluation.
2. **Arc Blade (`ArcBladeInteractor`)**:
   - Radiant energy blade.
   - Striking broken conduits or tap nodes reconnects severed connections and adjusts detents.
3. **Synaptic Weight Regulators (`WeightRegulatorInteractor`)**:
   - Two rotary dials governing $w_1$ and $w_2$ with physical detents ($0.5$ step increments, range $[-2.0, 2.0]$).
4. **Bias Calibration Ring (`BiasDialInteractor`)**:
   - Central dial governing threshold offset $b$ with physical detents ($0.5$ step increments, range $[-2.0, 2.0]$).
5. **Activation Crystal Socket (`ActivationSocketInteractor`)**:
   - Receptacle accepting physical crystals. Inserting the Step crystal activates step thresholding.

---

## Broken Starting Presets

Six distinct broken configurations provide high replayability and test generalization:

| Preset | $w_1$ | $w_2$ | $b$ | Activation | Cable Status | Diagnostic State |
|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| **A** | 0.0 | 0.0 | -1.0 | Linear | Cable 1 disconnected | Cold startup; missing signal input |
| **B** | -0.5 | 1.0 | 0.0 | Step | Cable 2 disconnected | Inhibitory weight error; missing input 2 |
| **C** | 1.0 | 1.0 | -2.0 | ReLU | All connected | Severe negative bias; incorrect ReLU crystal |
| **D** | 0.5 | 0.5 | 0.0 | Linear | Both disconnected | Double severed conduit; weak synaptic weights |
| **E** | -1.0 | -1.0 | 1.0 | Step | Cable 1 disconnected | Inverted negative weights with positive bias |
| **F** | 0.0 | 0.0 | 0.0 | Step | All connected | Zero-energy dormant state |

---

## Performance Tracking & Grading

- **Time Elapsed**: Tracked from initial interaction to 100% convergence.
- **Pulse Count**: Number of diagnostic test pulses fired.
- **Failed Evaluations**: Number of incorrect attempts.
- **Ranks**:
  - **S Rank**: Solved in $\le 6$ pulses and $< 90$ seconds.
  - **A Rank**: Solved in $\le 12$ pulses.
  - **B Rank**: Solved (100% accuracy).
  - **C Rank**: Incomplete or below 100% accuracy.

---

## Playable Loop

```text
Enter alien world
→ discover sealed Awakening Gate
→ explore damaged alien neuron machine
→ inspect floating diagnostic hologram
→ use Arc Blade to reconnect severed conduits
→ cycle/socket Step activation crystal
→ tune synaptic regulators: w1 = 1.0, w2 = 1.0, b = -0.5
→ fire Neural Pulse Tool to test circuit
→ verify 100% binary accuracy on all 4 truth table cases
→ Convergence achieved! Alien intelligence awakens
→ Awakening Gate unseals with radiant energy wave
→ score rank revealed
→ reset crystal allows instant re-randomization and replay
```
