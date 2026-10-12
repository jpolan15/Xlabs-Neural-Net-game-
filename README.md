# PROJECT: CONVERGENCE — Level 1: How a Machine Learns to Read a Digit

A Quest 2 VR ride through a real neural network. You ride *inside* the network ("be the signal") and watch it learn
to read a handwritten **3**: pixels → one neuron → a network → a wrong guess → backpropagation (the cart rolls
backwards) → gradient descent (a real coaster drop down the error) → the right answer. Decision record:
[ADR-016](Documentation/Architecture/ADRs/ADR-016-level1-digits-ride.md).

## What is real

- The network (49 pixels → 16 hidden → 10 answers) was trained offline on all **60,000 MNIST** training digits by
  `ArtSource/training/bake_level1.py`. It reads **92 %** of 10,000 digits it never saw. Untrained, it calls the 3 a **1**.
- Every light in the network is what the engine-independent C# core (`Convergence.Core.*`) computes from those
  weights. The neuron puzzle is graded by `Core.Puzzles.WeightPaintPuzzle`, never by the visuals.
- The Valley's ground is the real loss along the real training path; its first big step really overshoots.

## The ride (about 5 minutes, one narrator)

| # | Region | You |
|---|---|---|
| 0 | The Page: a handwritten 3 | Pull the orange lever to dive in |
| 1 | The Grid: 784 pixels as pillars, shrunk to 49 numbers | Watch |
| 2 | One Neuron | **Paint the weights** (tap squares) so it fires for 1s and not for 0s |
| 3 | The Network | Send the 3 through and watch the light |
| 4 | Wrong | It says "1" |
| 5 | Rewind: the cart reverses, wires burn by blame | **Point at the brightest wires** to nudge them |
| 6 | The Valley: height = how wrong | The ride drops downhill by itself (gradient descent) |
| 7 | Right: the trained gold network says 3 | Teaser: the same machinery with words |

## How to run

1. Open in **Unity 6 (6000.6.0f1)**, scene `Assets/Scenes/NeuralRide.unity`, press **Play**.
2. Desktop: hold **right mouse** to look around, **left-click/drag** levers, click weight squares and glowing wires.
3. Operator keys (with or without a headset): **Space** skip a line / hurry a moment, **R** restart for the next
   rider, **G** gentle mode (half the drop, slower travel).
4. Rebuild the scene after any content change: menu **Convergence → Build Neural Ride**.

## Project structure

- `Assets/Scripts/Core/`: pure C# math, network, training, puzzles (no Unity references).
- `Assets/Scripts/Gameplay/Ride/`: segment director, ride path; `Level1/` loads the bake and runs the moments.
- `Assets/Scripts/Presentation/Ride/`: network, grid, blame, valley views, dash subtitles, sound.
- `Assets/Scripts/XR/Ride/`: levers, weight cells, blame wires, desktop seat.
- `Assets/_Project/Data/Level1/level1_digits.json`: the baked network and data.
- `Assets/_Project/Audio/Voice/`: narration (Kokoro-82M `bm_fable`, synthetic, Apache-2.0; humans to record for Ignite).
- `Tools/Editor/`: the scene builder, sightline/budget checks, seat captures, model sheets.
- `Documentation/Design/TEAM_BRIEFING.md`: plain-English briefing for presenting.

Credits: digits MNIST (Y. LeCun and C. Cortes, CC BY-SA 3.0); sound effects Kenney (CC0); music "Telstar".
