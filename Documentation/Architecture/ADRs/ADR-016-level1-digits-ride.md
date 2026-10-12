# ADR-016: Level 1 as One Continuous Ride on Handwritten Digits

| Field | Value |
|---|---|
| ID | ADR-016 |
| Date | 2026-10-12 |
| Status | Accepted (built; EditMode 113/113 and PlayMode 23/23 pass; seat captures reviewed by `vr-qa`; voices pass the transcription check; not yet heard by the owner or ridden on a headset) |
| Deciders | Project owner (2026-10-12: the old story confused them and did not teach enough); plan `.agents/tasks/TASK_LEVEL1_DIGITS_RESTRUCTURE.md` |
| Supersedes | The story content of ADR-011 and ADR-015 (JFK cold open, AURA, rock/ice/drone stops). Their infrastructure (builder, director, narration pipeline, sightlines) stays. |

## Context

The v3 ride taught with a spaceship story (AURA, asteroid defence, 1943/58/60 history stops). The owner found it
confusing and too thin on how learning actually works. The ride must teach a general audience the whole idea, from
one neuron to gradient descent, with real data and no faked lights, by the teacher demo (2026-10-20) and the public
Ignite demo (April 2027).

## Decision

1. **One continuous ride, "be the signal"**, through 8 regions with their own look: Page, Grid, Neuron, Network,
   Wrong, Rewind (the cart reverses), Valley, Right. One narrator (the Guide, 32 lines, ids 32–63).
2. **Real data, baked offline.** `ArtSource/training/bake_level1.py` trains a 49-16-10 network on all 60,000 MNIST
   training digits (deterministic NumPy) and writes `Assets/_Project/Data/Level1/level1_digits.json`: both weight sets,
   the hero 3 (test #1845), the puzzle, the Valley ribbon and the finale's 60 test digits. 92.5 % test accuracy;
   untrained guess **1**. Everything that glows is what `Core` computes from those weights.
3. **Core does the math.** `Core.Puzzles.WeightPaintPuzzle` grades the neuron puzzle (with hints computed from the
   evaluator), `Core.Math.LossFunctions.CategoricalCrossEntropy`, `Core.Neural.NetworkModel.CreateDense`.
   Gameplay only loads data (`Gameplay/Ride/Level1/`) and sequences moments.
4. **Segment director.** `RideDirector` runs segments with moments (weight-paint puzzle, forward pass, blame,
   valley run) instead of stops. A moment ends only when its own rule says so; Space (`SkipNarration`) hurries any
   moment, G toggles gentle mode (half the drop).

### Deviations from the plan, measured

- **Valley = a ribbon, not a PCA plane.** A PCA plane through the trained weights put the start 0.82 and the
  overshoot 1.90 loss units below their real loss. Each ribbon row is a real point on the training path, each
  column a step along one fixed random direction; the centre row is the real training loss exactly.
- **First Valley hop is 21 m, the others 7 m** (`Layout.FirstHopLength`). At 7 m the overshoot (+1.05 loss = +5.2 m)
  stood as a wall in front of the rim. Heights are unchanged; only row spacing in that hop.
- **Network = a 30 m-wide diagram facing the rider** (49 pixels as a 7×7 grid with a lit-square plate behind it, 16
  hidden, 10 answers), not axial walls, which read as a tangle. The "Wrong" beat is said from the same stop.
- **Grid = relief walls beside the track**: a floor of pillars was hidden by the dash.
- **Branch to the Valley turns 40°** (eased, about 6°/s), not 25°, to clear the diagram.

## Consequences

- The v3 stop tests (11) were replaced by 10 segment tests (`RideDirectorTests`), and the v3 narration tests by
  Level 1 ones; none were deleted to pass.
- Voices are synthetic (Kokoro-82M `bm_fable`, Apache-2.0) until humans record the locked script for Ignite.
- Open before Ignite: the owner's listen and headset ride (comfort on the reverse and the drop), and the licence of
  `telstar.wav`.
