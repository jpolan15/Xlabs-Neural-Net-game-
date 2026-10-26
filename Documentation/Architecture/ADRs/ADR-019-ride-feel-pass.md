# ADR-019: Ride Feel Pass: A Table You Can Look At, Fingertips, Wires You Drag, A Pet-able Pixel, A Boot Behind Black

| Field | Value |
|---|---|
| ID | ADR-019 |
| Date | 2026-10-21 |
| Status | Accepted (built; EditMode 136/136, PlayMode ride suites green (RideDirector 14, RideGames 1, RideFeel 2), sightlines clear, budget within 100 draw calls; not yet ridden on a headset) |
| Deciders | Project owner, from the second round of playtest notes (neck strain, weight cells not answering, backprop "just clicking", Pixel not pettable, a stutter at the start) |
| Builds on | ADR-018 (mini-games on the game table, hand lasers). Its rules hold. |

## Context

The notes asked for five things. Checked against the code, three of the causes the notes guessed were not the real ones:

- The game table (ADR-018) was 0.55 m ahead and tilted 59 degrees, so the board's lower edge reached 56 degrees under the
  eye (the 7×7 grid spans most of the table). That is the neck bend. Its top rim was already 14 degrees under the horizon.
- Weight cells did not answer the lasers or pokes because **the cells never held their router**: the builder assigned
  `WeightCellInteractable.grid` in code on a prefab instance, which the saved scene does not keep. The colliders were
  thin but not blocked, and the XR rig's ray mask already includes the Default layer.
- Backprop (the hot-wire hunt) was not a one-click gate. It was "shoot six wires", with a real gradient step each. The
  spatial metaphor was missing, not the real step.
- `NeuralRideBuilder` is an Editor tool that bakes the scene; nothing is built at runtime, so there was nothing to
  slice across frames. The start-up cost is scene load, first-draw shader and mesh work, font glyphs, and clip decoding.

## Decision

1. **The table sits where a rider can read it.** 0.72 m ahead, 0.89 m up, tilted 45 degrees, scaled 0.85, for the 1.30 m
   eye `PodSeat` sets. Its top rim is 15.7 degrees under the horizon (the track and the explainers show above it), the
   grid's centre 34 degrees down, the lowest edge 46 degrees (was 56), and the face within 12 degrees of square to the
   eye. `NeuralRideBuilder.AssertTableErgonomics` fails the build with the measured angles if a change breaks that.
   The title and score text grew to stay over the 1.5 degree legibility floor at the new distance.
2. **Cells answer.** The builder now records the cells' router as a serialized override. Each cell's collider is 3.6 cm
   deep and stands out in front of the tile. `HandPolarityController` (XR) is on each controller: right = + green
   (#2ECC71), left = − red (#E74C3C), a fingertip orb in that colour while a grid game is live, and a small kinematic
   trigger at the fingertip that presses the cell it reaches with that hand (a laser, an XRI poke and a touch together
   count once: the cell debounces). A painted cell flashes the brush's colour; the click is pitched by hand (green
   high, red lower, rub-out soft).
3. **Backprop is dragging.** `BlameMoment` keeps its real gradient step and its timeout help, and changes the input: a
   laser press grabs a hot wire (`Grab`), its loose end follows the laser projected onto the plane through the answer
   "3", and it snaps on when the end is within 3 degrees of the 3 (`Connected`, with the hand and the order). Letting go
   anywhere else drops the wire (`RideAim.Released`). Four wires, 40 s. `BlameView` draws the handles, the cable, the
   pulses of blame flowing back, the 3's glow and flash, and sparks, from one batched mesh (one draw call). The 3 rings
   a step higher for each wire (1.00x, 1.12x, 1.25x ...) and the hand gets a 0.75 buzz for 0.12 s. The snap is geometry in
   Gameplay, not a collider: a collider around the 3 would eat the lasers aimed at the wires behind it.
4. **Pixel can be petted.** Its trigger volume is 1.8× its head, above and ahead of it. A fingertip travelling 8 cm
   sideways inside it, or a laser press, starts a 1.5 s reaction: ^ ^ eyes, full blush and a pink-coral glow, a damped
   wiggle (`Sin(t·25)·8` degrees) with a bounce, a chirp (pitch 0.95 to 1.08) and a 0.25 purr in the petting hand.
   Reactions are 1.2 s apart at the closest, so a long rub chains instead of stacking sounds. Gameplay owns the
   interfaces (`IHandTip`, `IPettable`, `RideAim.Buzz`); Presentation never touches XR types.
5. **The ride boots behind black.** `RideBoot` covers the eyes and mutes at the first frame, and the director holds its
   start (`HoldStart`/`ReleaseStart`). Behind the black it switches on every hidden hierarchy and draws it from a small
   probe camera for two frames (shaders, pipeline states, meshes and glyphs reach the GPU), switches them back, loads
   every voice and effect clip, collects garbage, waits 0.25 s, fades out over 0.5 s, and only then releases the ride.
   It gives up after 25 s and starts anyway.

## Consequences

- The hot-wire hunt's voice lines still say "shoot the hot wires". The words are baked audio (Kokoro), so re-render
  `BackGrab` and `BackDone` if the owner wants them to say "drag". The on-screen text already says drag.
- The tests that start the ride now wait for the boot to lift first. `RideFeelTests` covers the boot and the petting.
- Device checks still to do: boot time and frame times on a Quest 2 (the probe camera matches the eye's format but the
  real saving is only measured there), the touch radius (1.2 cm; wider paints neighbours), and whether 3 degrees is the
  right snap at 30 m. All three are single serialized numbers.
- `Level 2` in the notes is not built; the backprop in Level 1 was taken to be what they meant.
