# PROJECT: CONVERGENCE (v2.0 — The Neural Ride)

> 🚀 **UPDATED VERSION**: Re-architected under **ADR-011** from the legacy stationary console chamber into **The Neural Ride** — an immersive VR pod rail ride directly through the core of the ship's neural network, complete with original custom human voice narration and real mathematical backpropagation.

---

## Overview

You are a passenger aboard the starship *Neural*. The ship's AI, **A.U.R.A.**, has degraded its targeting systems. To restore connection, you board an automated probe pod and travel down a glowing rail directly into the AI's glowing neural core. 

At three distinct stops, you teach it fundamental machine learning concepts through tactile physical controls on your pod dashboard:

1. **Stop 1 — One Signal (Weight × Input vs. Threshold)**: Adjust the `TRIGGER` threshold lever so the single neuron targets oncoming space rocks while sparing friendly repair drones. Assembles the formula: $\text{fire if } 1 \times \text{rock} \ge 0.5$.
2. **Stop 2 — Two Signals (Linear Combination / OR Gate)**: Tune `ROCK`, `ICE`, and `TRIGGER` levers. Fix an ice sensor wired backwards (negative weight) to create an OR logic gate that properly classifies multi-sensor inputs.
3. **Stop 3 — It Learns (Loss Landscape & Gradient Descent)**: Engage `LEARN` and select a learning rate (*slow*, *good*, or *crazy*). A marble rolls down a 3D contour error hill while the dash weight levers physically move on their own via genuine mathematical gradient descent. Choosing "crazy" demonstrates learning rate overshoot as it flings off the landscape!
4. **Outro**: Defensive targeting fully restored; ship sets course for home.

---

## Key Features & Changes from v1

* **The Neural Ride (ADR-011)**: Replaces the old static console room with a smooth, comfortable rail ride featuring Catmull-Rom easing and yaw-only rotation to eliminate VR motion sickness.
* **Original Custom Human Voiceovers**: All 15 in-game dialogue lines feature custom recorded human voice acting with synchronized dashboard subtitles and intelligent single-channel queueing (no overlapping audio).
* **Intuitive "Orange Rule"**: Anything interactable on the dashboard is orange. Everything else is non-interactive.
* **Real Mathematics (No Scripting)**: Driven by engine-independent C# (`Convergence.Core.Neural` and `Convergence.Core.Training`). Puzzle solutions and gradient descent steps are evaluated live by real math.
* **Dual Input Support**:
  * **VR (Meta Quest 2)**: Full XR Interaction Toolkit support with haptic feedback ticks on lever detents.
  * **Desktop Fallback**: Hold **Right-Click** to look around the cockpit, **Left-Click + drag** levers to interact.

---

## Quick Demo / How to Run

1. Open the project in **Unity 6 (6000.6.0f1)**.
2. Open scene **`Assets/Scenes/NeuralRide.unity`** (Build Scene 0).
3. Press **Play**.
4. Pull the orange **GO** lever to launch into the neural core.
5. At Stop 1, drag the **TRIGGER** lever down to **0.5** to tune the neuron!

---

## Project Structure

- `Assets/Scenes/NeuralRide.unity`: Active main game scene.
- `Assets/Scripts/Core/`: Pure C# simulation (Math, Neural, Training, Puzzles). Zero Unity dependencies.
- `Assets/Scripts/Gameplay/Ride/`: Station orchestration, Catmull-Rom pod motion, and puzzle evaluation.
- `Assets/Scripts/Presentation/Ride/`: Dash screens, subtitles, live 3D loss landscapes, and audio playback.
- `Assets/Scripts/XR/Ride/`: Lever interactables, desktop mouse fallback, and pod seating.
- `Assets/_Project/Audio/Voice/`: 15 custom human-recorded WAV voiceover files.
- `Documentation/Architecture/ADRs/ADR-011-neural-ride.md`: Architectural decision record for the Neural Ride.
- `Documentation/Design/TEAM_BRIEFING.md`: Plain-English presentation briefing and demo guide.
