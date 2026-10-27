# TASK — Level 1 v6: train a real brain (one connected game)

Builds on v5 (`TASK_LEVEL1_V5_GAMEPLAY.md`, ADR-018/019: lasers, win beat, game table, explainers stay).
Owner, 2026-10-26: "go ahead and build ... make it fun game". Decision record: ADR-020.

## 1. Why (owner + sister, from the Oct 21 headset ride of v4, and the v5 code)

- Not a full Level 1; gradient descent "just thrown in"; nothing connects. In code: the drawn 3 is discarded (every
  later stop reads the baked hero 3), the network never learns in front of you (the Valley replays a baked run),
  bias/sigmoid/what hidden neurons look for/the training loop/learning rate/test digits are missing.
- Games are click-a-cell-or-button; descent "too slow and nothing happens"; voice and pace too slow, too childish.
- Wanted: feels like a 3Blue1Brown / graduate-ML YouTube video, fast; a fun game that flows.

## 2. Design: one thread — your 3, a real network, trained live by you

| # | Stop | Game (hands) | Honest because |
|---|---|---|---|
| E1 | Pixels | **Draw your 3**: hold trigger, draw on the canvas; it becomes 49 numbers live | Core `DigitCanvas`: MNIST-style crop/scale/centre, 4×4 pooling like the bake |
| E2 | Neuron | **Spray the weights**: hold trigger and sweep (right +, left −) until it fires for 3s only | Core `WeightPaintPuzzle` (v5) |
| E3/E4 | Network, loss | **Throw your 3 in**: shoot your card; light races through the untrained net; it guesses (randomly) | Core forward pass on *your* digit with the bake's initial weights |
| E5 | Backprop | **Hot-wire hunt** (v5 drag) on *your* digit's real gradients; each drop is a real step on the live weights | Core backward pass |
| E6 | Descent | **TRAIN**: three step-size buttons (tiny / good / huge); hold one to run real mini-batch SGD on real MNIST; the pod rides the Valley by the live loss; huge blows up (undo), tiny crawls; win at ≥ 85 % test accuracy | Core `LiveTrainer`, same math as the bake |
| E7 | Right | **Pixel reads your 3** with the weights you trained; if it misses, it studies your 3 (real extra steps) | same |
| G | Bonus | **Stump Pixel**: it asks for digits, you draw, it guesses live with its confidence | same |
| E8 | Tokens | explainer only | — |

Cut: plug-the-strings (manual fix) and roll-it-down (toy hill), replaced by the live training game.
Target ≤ 7 min, ≥ 60 % hands-on. Narration: 3Blue1Brown-style (calm, curious, precise, real terms: activation,
weight, bias, sigmoid, layer, loss, backpropagation, chain rule, gradient, learning rate), no baby talk.

Live training numbers (Python sim, 3,000 balanced train digits, batch 32, from the bake's init, 2026-10-26):
lr 0.5 → 48 % at 100 steps; lr 4 → 83 % at 200, 89 % at 600; lr 40 → loss 21, stuck at 10 %.

## 3. Voice

Owner hates every synthetic voice tried (Kokoro; four Qwen3-TTS designs in `ArtSource/voice/out/v6_samples`).
Wants a 3Blue1Brown / graduate-ML feel. Do not clone a real person. Options put to the owner: record it (owner or a
friend; pipeline cleans and levels it) or ElevenLabs (owner's account). v6 ships a **temporary** Kokoro render so
timings and captions work; swapping is re-render + install + rebuild.

## 4. Work packages

- **WP1 Data + Core**: bake `live` section (balanced train/test subsets as bytes, three step sizes, checked by a
  simulated run); `Core.Training.DigitNet`/`LiveTrainer` (alloc-free, matches `NetworkModel`); `Core.Puzzles.DigitCanvas`.
  EditMode tests: parity with `NetworkModel`, convergence/divergence, canvas normalization, a drawn 3 reads as 3.
- **WP2 Gameplay**: `Level1Session` (your digit, live weights); Draw v6; spray paint; forward/blame/read on the session;
  `TrainMoment`; `ReadMoment`; `StumpMoment`. Flow v6.
- **WP3 XR**: held-trigger strokes and spray in `RideHandLasers` + `DesktopSeat`; hold-to-train.
- **WP4 Presentation**: canvas ink + live 7×7; network meshes follow your digit and live weights; train board (loss
  curve, accuracy, buttons); stump board (prompt, ten bars).
- **WP5 Script + temp voice**: lines 119+ (old ids reserved), keep explainer cue names.
- **WP6 Build + verify**: builder, rebuild, EditMode + PlayMode (every game won through its inputs), seat captures, vr-qa.
- **WP7 Docs**: ADR-020, PROJECT_MAP, memory.

## 5. Progress log

### 2026-10-26 — plan (Claude)
- v5 committed (`3701a07`): EditMode 136/136, PlayMode 30/30, never ridden on a headset. Plan above.

### 2026-10-26 — v6 built (Claude)
- WP1: bake `live` section (rates 0.15 / 4 / 40; good hits 85 % at step 240, tiny 41 % at 300, huge peaks at loss 21;
  everything else in the JSON byte-identical). Core `DigitNetwork`, `LiveTrainer`, `DigitCanvas` (+ 12 EditMode tests:
  backprop parity 1e-12, bake replay, canvas MNIST normalization, ten hand-drawn digits read correctly).
- WP2-WP4: `Level1Session`; draw (canvas, DONE/CLEAR), spray paint, send card, hunt on the rider's digit, `TrainMoment`,
  Pixel reads your 3 (studies it if needed), `StumpMoment`; `IRideStrokeTarget` in lasers and desk seat; `GameButton`;
  views `DrawBoardView`, `SendCardView`, `TrainBoardView`, `NetworkLiveView`, `TrainedSignView`, `BlameView.Reaim`.
  Plug and descent games deleted (their Core puzzles and EditMode tests kept).
- WP5: script v6 (55 lines, 119-173, 235 s), temp voice Kokoro af_heart 1.1, 55/55 pass Whisper (worst 9.1 %; reworded
  "ink here", "loss fall", "your three", "Pixel a brain"). `ArtSource/voice/RECORDING_SCRIPT.md` for a human narrator.
- WP6: build OK; sightlines clear (new boards 8 mm off the table plate); budget within 100 everywhere (pixels 99, rim 99).
  EditMode 148/148; PlayMode 29/30 then the one failure (a 25x timing race in the valley test, exposed by the faster
  plunge) fixed and RideDirectorTests 14/14. The games test plays every v6 game through its inputs and raycasts from the
  eye to each control. Live play-mode screenshots checked: draw board, training board (blow-up spike, curves), bonus round.
- Training speed set to 25 steps/s (the good rate won in ~5 s at 40/s; now ~8 s).
- Not done: headset ride; the real voice (owner choosing: record or ElevenLabs); vr-qa review of the new boards.

### 2026-10-26 — pace (Claude)
- Ride length estimate (builder travel + sequence lines): travel 101 s, lines 158 s, about 7.5 to 8 min with the games,
  roughly 40 % hands-on. Longest dead stretch: the straight climb out of the Valley to E7 (38 s at cruise 5). Cruise
  raised there (5 → 9) and on the E3 approach (7 → 8); noise specks (zap) still run on every travel leg.

### 2026-10-26 — vr-qa review of the v6 boards, fixes (Claude)
- Text was 0.8-1.3 degrees (too small): board text now 0.024-0.032 m em (button labels 0.025-0.026, verdict 0.028,
  LOSS/ACCURACY/LEARNING RATE 0.024). "training steps" and "hold one to train" removed; the empty plot says "HOLD A STEP
  SIZE TO START TRAINING" and the empty canvas "HOLD TRIGGER AND DRAW" (both hide at the first step / stroke; same TMP
  count). Step labels "SMALL / MEDIUM / HUGE" + rate; step buttons 0.15 × 0.07 at 0.082 pitch.
- Contrast: every button label white; held step green (0.08, 0.5, 0.28), DONE-ready green (0.05, 0.5, 0.25), idle pulse
  darker, lit CLEAR ×1.2.
- Layout: canvas left 12 mm and readout right 6 mm (15 mm clear of the canvas edge); bar digits 15 × 25 mm; LOSS and
  ACCURACY above the plot plate (the spike no longer runs through them).
- `GameTableView` lifts or lowers the table by the rider's eye offset from 1.30 m (clamped 1.10-1.50) each time it rises,
  so the boards sit 34 degrees down for every rider.
- Bonus round: digit walls out to ±10.5 m, 8 columns (end 13 m ahead), gold dimmed, so they no longer cover the network's
  pixels and answer labels; "10 ANSWERS" moved 1.8 m left, clear of Pixel's antennae.
- Not judged by the review: headset sharpness, laser hit reliability on a real controller, the lit send card.
