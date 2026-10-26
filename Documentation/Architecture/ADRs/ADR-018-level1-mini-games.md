# ADR-018: Every Level 1 Stop Is a Mini-Game on One Game Table, Played With Hand Lasers

| Field | Value |
|---|---|
| ID | ADR-018 |
| Date | 2026-10-21 |
| Status | Accepted (built; EditMode 136/136, PlayMode 28/28, sightlines clear, budget within 100 draw calls; not yet ridden on a headset) |
| Deciders | Project owner after the first headset playtest (2026-10-21: "feels like a lecture", A to pull the lever, can't click the grid, one number, grab the strings, clear wins, lasers, a real gradient-descent game); plan `.agents/tasks/TASK_LEVEL1_V5_GAMEPLAY.md` |
| Builds on | ADR-016 (the digits ride) and ADR-017 (explainers, Pixel). Their rules hold. |

## Context

The playtest found the ride played like a lecture: puzzles solved themselves on a timer, nothing said "you won",
the grid sat about 54 degrees under the eye, and only the grip button selected anything, so the trigger and A did
nothing. Eye height was set once, sometimes from a headset still in a hand, so the dash was "sometimes tiny".

## Decision

1. **`RideGame`** (Gameplay) is a `RideMoment` with a title, goal, progress, score and stars. Every stop's play is
   one: draw the 3, teach one neuron, plug the strings, the hot-wire hunt, roll it down. A game ends with a win beat
   ("STOP CLEAR!", 1-3 stars, its win line) and then waits for the rider to pull GO (`RideDirector.WaitForGo`, 12 s
   timeout). Nobody is stuck: after `giveUpAfter` (60-120 s) or an operator hurry the game plays itself out from
   Core's hints, and that win shows one star. Core decides every win (`DrawPuzzle`, `WeightPaintPuzzle`,
   `PlugPuzzle`, `DescentGame`, the real blame).
2. **One place to play: the game table.** It rises out of the dash to about 34 degrees under the eye while a game is
   live and sinks after (`GameTableView`), carrying the subtitles (`SubtitleMirror`) and one board at a time. The
   hot-wire hunt is the only game in the world; its board on the table says so. Checks count only the board a
   viewpoint lists (`RideViewpoint.HiddenGamePart`).
3. **Hand lasers** (`XR/Ride/RideHandLasers`): one beam per hand, right green / left red. The trigger or A/X/B/Y fires
   at whatever the beam hits through `IRideTarget` (Gameplay) or the existing interactables; with nothing hit, a press
   pulls GO when it is live. The right hand inks and paints green; the left erases and paints red. Grip and pokes
   still work through XRI. Between stops the rider zaps noise specks (`ZapGame`), which only add to the score.
4. **One number: 3.** The neuron puzzle is re-baked as threes vs. 0, 1, 7, 4. The fly-over of the pixels is cut.
5. **Voice:** Kokoro `af_heart` at 1.1 (our v3 AURA voice, the highest-rated by our earlier scores) replaces
   `bm_fable` at 0.9; every pause in the script is 40 % shorter. 204 s of speech, down from 315 s.
6. **`PodSeat`** finds the XR subsystem lazily and re-seats the eye on headset-on, on recenter, and after 3 s more than
   0.30 m off target.

## Consequences

- The ride waits for the rider after each win, so its length now depends on play; the timeouts bound it.
- The plug game is a simplified honest model (a plug is one strong wire on the real untrained logits), and the
  descent hill is an illustration; both are said so in `FORMULAS.md`.
- The game table blocks the dash screen while it is up, so it carries the subtitles itself.
