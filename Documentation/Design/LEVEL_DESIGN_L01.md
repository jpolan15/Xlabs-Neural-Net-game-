# Level 1 — The Awakening Gate: Level Design Specification

## Overview

- **Level Identifier**: `Level01_AwakeningGate`
- **Scene File**: `Assets/Scenes/Level01_AwakeningGate.unity`
- **Core Concept**: Single-neuron binary classification (Hazard Classifier OR-gate Perceptron)
- **Target Platform**: Meta Quest 2 (OpenXR + XR Interaction Toolkit 3.x) with complete Desktop Keyboard/Mouse Fallback

---

## Narrative & Thematic Setting: *Preventing the Synapse-GPT Cognitive Collapse*

Deep within the quantum research mainframe `CONVERGENCE-01`, humanity hosts the master synaptic matrix of **SYNAPSE-GPT (The Global Foundation AI)** — the synthetic multi-modal intelligence that coordinates planetary healthcare, electrical power grids, orbital life support, and autonomous robotics worldwide.

A catastrophic cosmic electromagnetic storm has triggered a cascade of **Synaptic Drift and Perceptual Hallucinations** across the AI's core matrix.

In **Chamber 01 (The Sensory Gateway)**, the foundational **Perception Perceptron** has collapsed:
1. The AI can no longer distinguish between baseline sensory silence ($x_1=0, x_2=0$) and lethal radiation/bio-hazard alerts ($x_1=1, x_2=1$).
2. Blinded by corrupted sensory inputs, the AI is hallucinating phantom catastrophes, initiating emergency failsafe blackouts across Earth and sealing the **Sector 01 Blast Doors** (The Awakening Gate).
3. If the foundational perception neuron is not calibrated immediately, **SYNAPSE-GPT will suffer permanent cognitive meltdown**, bricking worldwide infrastructure and trapping the engineer forever!

As the Chief Neural Architect, you must physically reconnect the sensory feeds, tune the synaptic sensitivity weights, set the noise rejection threshold, slot the step activation crystal, and run diagnostic test pulses across 4 real-world facility scenarios to stabilize planetary AI consciousness.

---

## Experiential & Visual Mechanics (Not Just Walls of Text!)

### 1. Planetary AI Consciousness Holosphere (`PlanetaryAICoreHologram.cs`)
- A majestic floating planetary hologram projecting Earth and the AI Neural Mesh high above the gate.
- **Live Planetary AI Integrity Bar**: Starts at `18% [CRITICAL COGNITIVE DRIFT]`.
- **Dynamic Stabilization**: Reconnecting cables, inserting the Step crystal, and balancing weights physically calms the red error distortion into radiant cyber-emerald orbital rings, raising integrity to `100% [SYNAPSE-GPT STABILIZED]`.

### 2. Visualized 3D Forward Pass & Particle Packet Simulation (`ClassicNeuralNetwork3DVisualizer.cs`)
- When the player pulls the **Master Clock Lever** (or presses `Space`):
  - Physical glowing energy data packets surge from the 4 scenario projectors into the Input Nodes ($x_1, x_2$).
  - Synaptic lines illuminate with high-speed pulse particles displaying the weighted transfer $(w_1 x_1, w_2 x_2)$.
  - The energy sums in the central core $(z = w_1 x_1 + w_2 x_2 + b)$ and refracts through the glowing Decision Crystal.
  - On a correct classification, a targeted laser beam strikes the corresponding door receptor, turning it emerald green with a resonant chime.

---

## Puzzle Scenarios & Sensor Grounding

### Real-World Sensor Inputs:
- **Sensor 1 ($x_1$)**: **Radiation Spike Detector** ($0.0$ = Normal/Safe, $1.0$ = Radiation Detected)
- **Sensor 2 ($x_2$)**: **Bio-Hazard Leak Detector** ($0.0$ = Normal/Safe, $1.0$ = Toxin Leak Detected)

### The 4 Hazard Scenarios (Truth Table):

| Case | Scenario Name | Sensor 1 ($x_1$) [Radiation] | Sensor 2 ($x_2$) [BioLeak] | Required AI Decision | Target Output ($y$) | Canonical Margin ($w_1=1.0, w_2=1.0, b=-0.5$) |
|:---:|:---|:---:|:---:|:---|:---:|:---:|
| **1** | **Clean Room** | $0.0$ | $0.0$ | **ALL CLEAR / SAFE** (Keep doors open) | $0.0$ | $z = 0(1) + 0(1) - 0.5 = -0.50 \to \text{step}(z) = 0$ |
| **2** | **Bio-Hazard Leak** | $0.0$ | $1.0$ | **TRIGGER QUARANTINE** (Seal blast doors!) | $1.0$ | $z = 0(1) + 1(1) - 0.5 = +0.50 \to \text{step}(z) = 1$ |
| **3** | **Radiation Flare** | $1.0$ | $0.0$ | **TRIGGER QUARANTINE** (Seal blast doors!) | $1.0$ | $z = 1(1) + 0(1) - 0.5 = +0.50 \to \text{step}(z) = 1$ |
| **4** | **Dual Breach** | $1.0$ | $1.0$ | **TRIGGER QUARANTINE** (Seal blast doors!) | $1.0$ | $z = 1(1) + 1(1) - 0.5 = +1.50 \to \text{step}(z) = 1$ |

---

## Beginner Pedagogical Foundations

1. **Why do we upload/transmit test data packets?**
   - A neural network without data is just blank mathematical equations. We transmit recorded sensor packets representing all 4 facility conditions to verify the AI makes the correct decision in production.
2. **Why do we tune Sensitivity Weights ($w_1, w_2$)?**
   - Weights act as synaptic amplifiers (volume knobs). Setting $w_1 = 1.0$ instructs the AI that an alert from the Radiation sensor is critical enough by itself to overcome baseline resistance and trigger quarantine.
3. **Why do we tune Noise Bias ($b$)?**
   - Bias acts as a background noise filter / sensitivity threshold. Setting $b = -0.5$ creates a negative baseline buffer ($0+0-0.5 = -0.5 < 0$) so background static in a Clean Room does not trigger false alarms.
4. **Why do we need an Activation Function (Step Crystal)?**
   - Raw linear math generates continuous energy values (e.g. $-0.5$ or $+1.5$). The blast door quarantine mechanism requires a decisive binary switch ($0$ or $1$). The **Step Crystal** evaluates whether total energy $z \ge 0.0$ (Output $1.0 = \text{ALARM}$) or $z < 0.0$ (Output $0.0 = \text{SAFE}$).

---

## Equipment & Tactile Interactors

1. **Radiation & Bio-Hazard Conduits (`CableInteractable`)**:
   - Patch cables streaming live sensory data into inputs $x_1$ and $x_2$.
2. **Radiation Sensitivity Regulator (`WeightRegulatorInteractor` $w_1$)**:
   - Rotary dial governing $w_1$ with detents ($0.5$ step increments, range $[-2.0, 2.0]$).
3. **Bio-Hazard Sensitivity Regulator (`WeightRegulatorInteractor` $w_2$)**:
   - Rotary dial governing $w_2$ with detents ($0.5$ step increments, range $[-2.0, 2.0]$).
4. **Noise Filter Bias Ring (`BiasDialInteractor` $b$)**:
   - Central dial governing threshold offset $b$ with detents ($0.5$ step increments, range $[-2.0, 2.0]$).
5. **Decision Crystal Socket (`ActivationSocketInteractor`)**:
   - Receptacle accepting physical activation crystals. Inserting the Step crystal enables the binary threshold switch.
6. **Master Clock Cycle Lever (`ClockPulseLeverInteractor`)**:
   - Transmits the 4 test scenario packets through the circuit and triggers diagnostic verification.

---

## Broken Starting Presets

Six distinct broken configurations provide high replayability and test generalization:

| Preset | $w_1$ | $w_2$ | $b$ | Activation | Cable Status | Diagnostic State |
|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| **A** | 0.0 | 0.0 | -1.0 | Linear | Cable 1 disconnected | Cold startup; missing radiation line |
| **B** | -0.5 | 1.0 | 0.0 | Step | Cable 2 disconnected | Inhibitory weight error; missing bio line |
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
Awaken in Stasis Pod
→ Station AI announces Synapse-GPT cognitive drift emergency
→ Discover sealed Awakening Gate and glitching Planetary AI Holosphere (18% integrity)
→ Approach Hazard Classifier Workstation
→ Read In-World Field Manual Tablet (Mission & Sensor Guide)
→ Plug in Radiation & Bio-Hazard Conduits (Integrity rises)
→ Socket Step Decision Crystal (Sharp threshold established)
→ Calibrate Sensitivity Knobs: W1 = 1.0, W2 = 1.0, Bias = -0.5
→ Pull Clock Cycle Lever to transmit 4 sensor test packets
→ Watch 3D forward pass energy wave packets illuminate each door receptor
→ Planetary AI Holosphere reaches 100% Harmonic Convergence
→ Blast Doors disengage quarantine and slide open with radiant aura
→ Grade & Telemetry rank revealed
```
