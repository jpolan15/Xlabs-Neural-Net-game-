# ADR-017: Level 1 Explains Each Idea on a Blackboard, Then Unfolds Into It; Pixel Is the One Learning

| Field | Value |
|---|---|
| ID | ADR-017 |
| Date | 2026-10-20 |
| Status | Accepted (built; EditMode 129/129 and PlayMode 27/27 pass; voices pass the transcription check except two clauses checked by hand; not yet heard by the owner or ridden on a headset) |
| Deciders | Project owner (2026-10-20: 3Blue1Brown-style explanations that unfold into the level, simple enough for a 4-year-old, every topic named, a buddy); plan `.agents/tasks/TASK_LEVEL1_V4_EXPLAINERS.md` |
| Builds on | ADR-016 (the digits ride: track, bake, Core puzzle, director, builder, tools). Its rules hold. |

## Context

ADR-016's ride showed the real network but explained it with text panels and a voice. The owner wanted the classic
explanations (the ones in textbooks and 3Blue1Brown's videos), told so a small child follows them, and wanted the
explanation to *become* the place the rider then plays in.

## Decision

1. **Explain → Unfold → Play for every idea.** Nine explainers (E0 dock … E8 tokens), one `Explainer` component each
   (`Assets/Scripts/Presentation/Ride/Explainers/`). While one explains, the region's pieces are off and the sky is the
   blackboard (#0B0E14, faint distant BLUE grid). Its unfold step moves its own pieces into the region's real objects
   (Morph, Grow, GrowUp, Show), then gives the sky back. Skip (Space / long GO hold) jumps any explainer to its end.
2. **A small manim-like kit, our own.** Rate functions (`smooth`, `rush_into/from`, `there_and_back`), tweens on the
   game clock, primitives (Write, Create, FadeIn/Out, Transform, Indicate, Flash, Grow…), and `ShapeBatch`: many
   anti-aliased flat shapes in one draw call (Quest 2 budget). Steps are keyed to the narration's `{cue}` tags and
   clauses (`DashScreenView.SegmentShown`): no second clock.
3. **Every number shown is the bake's.** The neuron uses the puzzle's real 1 and reference weights (sum 3.54 against
   the line 2.0); the network diagram is the real 49-16-10 untrained network; the loss chart and the backprop arrows
   are the real untrained outputs and gradients; training shows the real count, loss ratio and accuracy. Exceptions,
   said in code: the gradient-descent curve and the token vectors are illustrations.
4. **The network diagram is the network.** E3 is laid out in the giant network's coordinates at one fifth of its size
   on the line from the rider's eye, so it covers the network exactly and its Morph unfold grows it along that line.
5. **Pixel**: a builder-made robot (`NeuralRideBuilder.Pixel`, `PixelBuddyView`, Presentation only) from our scout
   drone concept. Pixel is the one learning (the network is Pixel's brain); it beeps, never speaks (one narrator), shows
   six moods on a face screen drawn by a ShapeBatch, looks at the rider or ahead, points at the paint hint, and reacts
   to lines (`PixelMoods` table) and moment events. It rides in the pod's frame (no swinging), placed where the
   sightline check passes at every stop.
6. **Kid-simple words, real names.** Lines 64–105 (ids 32–63 stay reserved). Every topic is named (pixels, neuron,
   weights, layer, neural network, loss, backpropagation, gradient descent, training, tokens). Colours mean the same
   everywhere: GREEN yes/+, RED no/−, YELLOW look here, ORANGE grab me.

## Consequences

- Each beat costs one explainer component plus builder code; adding a beat means a component, its builder function,
  its model sheet and its lines. The builder fails on a missing model sheet, a blocked sightline or a view over budget.
- Field names of an `Explainer` subclass must not repeat the base class's (`hidden`, `content`, `lines`…): Unity's
  serializer silently keeps only one.
- The transcription check (Whisper) can drop a short word in context; such a clause is checked on its own and logged.
- Owner to do: listen to the voice and ride it on a headset.
