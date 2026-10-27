# ADR-021: Level 1 v7: Blast the 3s, the Intro Roadmap, and What Training Learned

| Field | Value |
|---|---|
| ID | ADR-021 |
| Date | 2026-10-27 |
| Status | Accepted (built and tested on desktop; not yet ridden on a headset) |
| Deciders | Project owner, after an ML-teacher review of v6 and a grilling session (`.agents/tasks/TASK_LEVEL1_V7_DEMO.md`) |
| Builds on | ADR-020 (one thread, live training). Its rules hold. |

## Context

Teacher demo on 2026-10-28, with the teacher in the headset. The owner's sister found the ride "more like a lecture"
and asked for a gun and things to shoot while driving. The review found the intro never says what the rider will learn
or do, and the hidden layer is only ever described ("each one can learn a different stroke"), never shown, though the
rider's own trained weights are in memory. Two script lines were wrong for a teacher: the loss "adds up how far off"
(it is softmax cross-entropy), and hidden neurons "learn strokes" (they learn blotches). The v5 game sounds (laser
shot, pop, win) had no clips, so they were silent.

## Decision

1. **Blast the 3s.** The travel game shoots real handwritten MNIST test digits (bake `targets`: 12 threes, 2 each of
   8, 5, 2, 9, 7, 0, chosen as ones the trained network reads with p > 0.9) instead of noise specks. A 3 scores 10 ×
   a streak multiplier (x2 after three in a row, max x4); another digit costs 10 and the streak; an escaped 3 ends the
   streak. The rider is the classifier Pixel will learn to be. Targets still flying when the pod stops fade out
   without counting. The rule lives in Gameplay (`ZapGame`); colour never tells a 3 from another digit.
2. **One draw for all targets.** `BlasterView` (Presentation) draws every target as one picture quad of one batch
   (atlas `TargetsAtlas.png`, white ink with a cyan halo), adds a bolt down the laser per shot while the digits fly,
   and a pixel burst on a hit. Its renderers are on only while something shows, so stops keep their draw budget.
3. **Intro roadmap.** The dock says what the rider builds and why (a real network, the kind behind face unlock and
   chatbots, trained to read their handwriting) and shows the five stops on a track, each lit as it is named, plus
   "blast the 3s on the way". The intro still reaches GO in under 25 s (22 s). The roadmap switches itself off once it
   fades on GO, and the budget tool counts it at the dock only.
4. **What training learned.** E7 opens with the rider's hand-painted neuron beside the sixteen hidden neurons of the
   network the rider trained, each a 7×7 of its real weights (green +, red −, scaled to that neuron's largest), and the
   honest line: nobody told them what to look for; they are not neat strokes, but together they read handwriting.
5. **Script fixes.** The loss line says "measure how far off" (true of cross-entropy and of the gap picture shown);
   the hidden layer "will learn to look for something different". New lines 174 to 177; temporary Kokoro voice.
6. **A lit world at every stop** (owner: "the scenes shouldn't be all black"). The camera clears to one procedural
   sky (`Assets/Shaders/RideSky.shader`: a zenith-to-horizon gradient, a glow band, static stars, an infinite grid
   floor 7 m below the eye with per-eye depth), one draw. Each region has its own colours in `RideTheme`
   (dawn blue, cyan, amber, deep blue, red, violet, sunset, gold); `RegionAtmosphere` blends them and sets the fog to
   the horizon. While an explainer explains, the sky is the region's own, dimmed to 0.78, floor at half strength;
   the near-black 3Blue1Brown blackboard and its pod-relative grid are retired.
7. **Game sounds wired.** fire, pop, plugIn, win and winShimmer get Kenney clips leveled by measured LUFS; the blaster
   shot plays only while digits fly (a soft click elsewhere).

## Consequences

- Travel legs are now a scored classification game; the score means something (3s blasted, mistakes).
- The training game keeps v6's three buttons for the demo. A live step-size dial and a race to 85 % is the planned
  next step (the gradient picks the direction; the rider only the step size).
- Not verified: headset feel (aiming at moving digits with the laser, the size of the 7×7 weights at 4 m, and the
  floor grid scrolling under the pod: comfort).
