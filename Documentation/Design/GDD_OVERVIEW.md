# Game Design Document: Project Convergence

## Premise

You are a passenger on the ship *Neural*. Its AI, A.U.R.A., has lost its targeting skills. You ride a small probe pod down a rail into A.U.R.A.'s glowing neural core and, one stop at a time, teach it three ideas. The goal: leave knowing, by feel, that an AI is just math, and that learning means rolling downhill. Decision record: ADR-011.

## Design pillars

1. **One idea per stop, one new control per stop.** Anything you can touch is orange. Nothing else is.
2. **Math is the payoff, not the instructions.** You feel a lever first. At the end of each stop the equation assembles itself from the numbers you just set.
3. **Data goes in, you watch what happens.** Objects float past a scanner, sensors light, pulses travel the pipes, the core fills, and it fires or does not.
4. **Enclosed and readable.** Near: the dash with up to five levers and one screen. Middle: the scanner arch and data stream. Far: the neuron core.
5. **Comfortable.** The pod moves slowly with eased stops and never pitches or rolls. Locomotion is off while seated.

Language rule: every instruction is short enough for a small child, with icons doing most of the work.

## The ride (about 10 minutes)

| Stop | Idea | Control | What you see |
|---|---|---|---|
| Dock | Start | Pull the orange GO lever | The pod drops into the core |
| 1. One Signal | Weight × input against a trigger | Slide TRIGGER down | Rocks get zapped. Too low and the friendly drone gets zapped too. Then `fire if 1 × rock >= 0.5` assembles |
| 2. Two Signals | A weighted sum (OR) | Three levers: ROCK, ICE, TRIGGER | The ICE pipe starts wired backwards and drains the core. A four-tile board shows zap or pass and right or wrong for every object at once |
| 3. It Learns | Loss and gradient descent | Pull LEARN, choose a learning rate | A marble rolls down a contour-banded error hill while the weight levers move by themselves. Too high a rate overshoots and flies off. Then `w <- w - 0.5 × slope` assembles |

Not built yet: ship intro and outro scenes, the 3-question quiz, credits, and the stretch stop for XOR and hidden layers (where backpropagation through layers becomes visible).

## Teaching map

- Stop 1: a neuron multiplies an input by a weight and compares it to a threshold.
- Stop 2: two weighted inputs added together decide an OR gate.
- Stop 3: error is a hill, the gradient is the slope, and training steps downhill. Under the hood the trainer runs real backpropagation through the neuron (`Forward`, then `Backward`) to get the slope.

## Audio

Narration is 15 AI-synthesized lines (`vo_ride_01` to `vo_ride_15`) spoken from the dash, with subtitles on the same screen. Background music streams from `Assets/_Project/Audio/Music/telstar.wav` and ducks under speech.

## Controls

- **Headset (Quest over Link):** point and grab the orange handles with the XRI Starter rig. Haptics tick at each detent.
- **Desktop fallback:** hold the right mouse button to look around the cockpit, left-click and drag a lever.

## Source of truth

Architecture: `Documentation/Architecture/ADRs/ADR-011-neural-ride.md`. Math: `Documentation/Mathematics/FORMULAS.md`. Level 01 design documents (`LEVEL_DESIGN_L01.md`, `ART_BIBLE_CHAMBER01.md`) describe the superseded console and are kept for reference.
