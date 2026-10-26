# TASK — Level 1 v5: make it a game, not a lecture

Builds on `TASK_LEVEL1_V4_EXPLAINERS.md` (explainers, Pixel, script). v4 stays the content; v5 changes how it plays.
Status: **built 2026-10-21** (owner: "yes build make it fun"). Needs a headset pass.

## 1. Playtest findings (owner + one playtester, Quest, 2026-10-21)

| # | What happened | Cause found in code |
|---|---|---|
| 1 | GO podium sometimes normal, sometimes tiny; you look at the floor to play | `PodSeat` set eye height once, on the first tracked frame, and only if the XR subsystem existed at `OnEnable`. Headset still in a hand/on a table → wrong height sticks → dash far below. Dash itself sits at 0.66 m, tilted 45°, ~40° under the eye. |
| 2 | Pressed A to pull the lever (owner and playtester both) | Nothing reads A/X. Lever, cells, wires only take XRI *select* = grip. |
| 3 | Could not click the 7×7 grid at all | Same: trigger is XRI *activate*, not select; `XRSimpleInteractable` ignores it. Only grip or a finger poke works. |
| 4 | "Learn a one" while the story says threes | Neuron puzzle is ones vs zeros (`NeuronYourTurn`), untrained guess is "one", story is threes. |
| 5 | Puzzle "just passed on" | `WeightPaintMoment` auto-solves after `autoSolveAfter`; blame and valley auto-finish too. |
| 6 | Playtester tried to fix the network on the podium | Two play surfaces (dash grid vs world network), nothing says which. |
| 7 | Didn't know the level was beaten | No win beat: no fanfare, no "clear", no score. |
| 8 | Fly-over of the pixels felt like padding | E1 unfold flight (`PixelsFly`) adds ~10 s, teaches nothing new. |
| 9 | Gradient descent slow, no game | `ValleyRunMoment` is a coaster the game drives. |
| 10 | Voice robotic and slow | Kokoro `bm_fable` at speed 0.9, plus long `[pX]` pauses. |
| 11 | Wants to shoot things | Rays are UI-only; nothing to aim at between stops. |

## 2. Fixes that need no design call (start now)

- **F1 Seat** — done in code: `PodSeat` finds the subsystem lazily, re-seats on headset-on (user presence), recenter,
  and after 3 s more than 0.30 m off target. Needs a headset check.
- **F2 One console, eye level** — the dash becomes a floating console ~0.55 m ahead, top edge ~10° under the eye,
  tilt ~20°, everything you play on it within a 35° cone of straight ahead. Fixed world size (no scaling with anything).
  Seat captures + `vr-qa` at 3 eye heights.
- **F3 Hand buttons** — new `Assets/Scripts/XR/Ride/RideHandButtons.cs`: per hand, trigger **or** A/X presses whatever
  that hand's ray is pointing at (lever → pull, cell → paint, wire → zap, Pixel → pet). Grip keeps working. Visible
  laser on both hands, always on, with a hit dot.
- **F4 Two-hand paint** — right hand paints **green** (like ink), left hand paints **red** (no ink); same colour again
  clears. Core gets `WeightPaintPuzzle.Paint(cell, value)` style input (Gameplay sets, Core still judges).
- **F5 No silent auto-solve** — the hint escalates (ring → arrow → Pixel points) instead; operator Shift+Space stays.
- **F6 Cut `PixelsFly`** — E1 unfolds straight into the next stop.
- **F7 Win beat** for every stop: stamp "STOP 2 CLEAR!", stars (1–3 by time/moves), fanfare, confetti, Pixel dance,
  haptic buzz both hands. Then the GO lever lights for the next stop, so the player moves on by choice.
- **F8 Voice** — Guide to Kokoro `af_heart` (our AURA voice in v3: the most natural Kokoro voice by our own scores),
  speed ~1.05–1.1, and every `[pX]` pause cut by ~40 %. Samples in `ArtSource/voice/out/sample_af_heart_*`.
  Re-render after the script changes in section 3, not before.

## 3. Mini-games (proposed — owner to confirm)

One number everywhere: **3**. Every stop is Explain (short, ≤ 20 s) → Play (a real game with a goal) → Win beat.
Play is always on the console in front of you **or** a glowing target in the world your laser points at — never both
at once; the one in use pulses and the other dims.

| Stop | Game | Win | Honest because |
|---|---|---|---|
| Pixels | **Draw the 3**: laser-paint a 3 on the 7×7 (right = ink, left = erase) over a faint outline | Your drawing is close enough to the outline | Your drawing becomes the 49 input numbers used for the rest of the ride |
| Neuron | **Teach one neuron**: paint weights green/red so it lights for 3s and stays dark for the others (3 vs 0, 1, 7, real MNIST) | All digits right | Core `WeightPaintPuzzle`, re-baked for 3-vs-others |
| Network | **Plug the strings**: grab glowing wires from the hidden layer and plug them into the **3** socket; strong plugs make 3 brighter, wrong ones light other answers | 3 is the brightest answer | Real forward pass with the plugged weights |
| Backprop | **Hot-wire hunt**: laser the hot wires before they cool; bigger blame = more points | Hit the hottest N in time | Heat = real gradient magnitude |
| Gradient descent | **Roll it down**: you steer a ball down the loss hill; set step size with the lever, A to take a step; too big overshoots up the far side | Reach the bottom in ≤ par steps | Ball height = real loss of the real training path |
| Between stops | **Zap the noise**: stray noise specks drift past the pod; shoot them for points | Score only, never blocks | Noise is what training learns to ignore |

Total target: under 8 minutes, with at least 60 % of it spent playing.

## 4. Validation

EditMode tests for every new Core rule (paint by value, drawing match, plug scoring, step-size descent). Seat
captures + `vr-qa` after F2. Owner headset pass after F1–F8 and after each mini-game.

## 5. Progress log

### 2026-10-21 — findings, F1 (Claude)

- Findings above, from the code. F1 written (`Assets/Scripts/XR/Ride/PodSeat.cs`); not yet compiled or tested.
- Voice samples rendered (lines 64, 65, 74, 95; `af_heart` at 1.0 and 1.1) into `ArtSource/voice/out/sample_af_heart_*`.
- UnityMCP was not connected (ECONNREFUSED), so no scene rebuild or tests this session yet.

### 2026-10-21 — v5 built (Claude)

- All of F1-F8 and section 3 built; decision record ADR-018. Table: `Tools/Editor/NeuralRideBuilder.Games.cs`.
  Games: `RideGame` + `DrawMoment`, `WeightPaintMoment` (two brushes), `PlugMoment`, `BlameMoment` (timed hunt),
  `DescentMoment`, `ZapGame`. Core: `DrawPuzzle`, `PlugPuzzle`, `DescentGame`, `WeightPaintPuzzle.PaintValue`.
- Bake: neuron puzzle is now 4 threes vs 0/1/7/4, line 2.5, hints solve from empty in 4 moves; everything else in
  `level1_digits.json` byte-identical.
- Voice: af_heart 1.1, pauses x0.6; 54/54 lines pass Whisper (worst 10 %); NeuronGreenRed, BackHot and
  DescentOvershoot re-worded so the voice says them clearly. 204 s of speech (was 315 s).
- Table sits ~34 deg under a 1.30 m eye (was 54 deg); raising it more blocks the neuron panel at a 1.15 m eye.
- Checks: build OK, sightlines clear at 1.15/1.30/1.45, budget <= 98 draw calls; EditMode 136/136;
  PlayMode see final report; Core boundaries and repo layout validators pass. vr-qa: pass-with-fixes (re-seat
  now waits for a still head and no game; Pixel's spot near the output column left for the headset pass).
- Not done: headset ride; Pixel position; A-to-GO only works through `RideHandLasers` (needs the headset to test).
