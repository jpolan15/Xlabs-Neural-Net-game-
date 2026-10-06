# PROJECT: CONVERGENCE
> **A tactile, hypothesis-driven educational VR puzzle game teaching real Neural Network and AI architecture for Meta Quest 2.**

---

## Overview

**Convergence** puts the player on the bridge of the starship *Neural* after an asteroid strike tears out its neural cables. In Level 1 the player rewires a single neuron that is the brain of the ship's automatic point-defense laser: it must fire at rocks and ice and let the friendly repair drone dock. Later chambers teach hidden layers, gradient descent, and attention to get the ship home. Rather than tweaking arbitrary numbers on a spreadsheet, the player physically diagnoses failures, balances weighted signals, and installs non-linear activation filters.

Built for **Meta Quest 2** using **Unity 6 (6000.6.0f1)** and the **XR Interaction Toolkit (XRI 3.x)**.

---

## Core Pillars

1. **Real Mathematics**: Every cable, dial, and chamber executes genuine deep learning formulas ($y = f(\sum w_i x_i + b)$). No fake or scripted results.
2. **Diagnostic Feedback**: Failures provide rich diagnostic telemetry (which test case failed, whether a ReLU clamped a negative value, which weight contributed most error).
3. **Forgiving VR Ergonomics**: No frustrating millimeter dial tuning. Snapped splines, detent clicks, ghost outcome previews, and coarse/fine modes.
4. **Decoupled Architecture**: All mathematics and puzzle evaluation logic reside in pure C# (`Core/`), completely separated from Unity Engine and XR presentation.

---

## Project Structure

- `Assets/Scripts/Core/`: Pure C# simulation (Math, Neural, Puzzles, Training). No UnityEngine dependencies.
- `Assets/Scripts/XR/`: Meta Quest 2 interaction mechanics (cables, rotary dials, cartridges, haptics).
- `Assets/Scripts/Visualization/`: Spline beams, particle flow, holographic decision boundaries, loss meters.
- `Assets/Scripts/Gameplay/`: Facility state, blast doors, chamber progression.
- `Assets/Scripts/Tests/`: Unit tests for mathematical rigor and puzzle evaluators.
- `Assets/Puzzles/`: Authored puzzle specifications and test suites (Chambers 1–4).
- `.agents/tasks/`: Active task tracking board.

---

## Agent Guidelines

Before contributing, all agents must read [AGENTS.md](file:///c:/Users/Panda/Downloads/neural%20game/AGENTS.md) and the respective `AGENTS.md` contract file within their target folder.
