# TASK — Level 1 v7: ML-teacher review, the agreed learning path, and what to build

Builds on v6 (`TASK_LEVEL1_V6_TRAIN.md`, ADR-020). Written 2026-10-27 from a full Play Mode run of v6 (16 seat shots,
autopilot, operator skips) and a grilling session with the owner. **Owner presents to the class teacher on 2026-10-28**
(tomorrow); Ignite (April 2027) gets the full redesign.

## 1. Decisions (owner, 2026-10-27)

| Question | Decision |
|---|---|
| Student vs teacher | **Student wins, teacher vetoes**: fun and pace decide, but nothing may teach something false. |
| Scope | **Level 1 polished + a 60–90 s Level 2 teaser** (tokens, next word). Full Level 2 later. |
| Length | **~6 min core + an optional endless bonus** (operator can cut it when the line is long). |
| Look | **Step inside it**: explainers start as 3B1B diagrams, then grow around the rider (inside the network, a real terrain). |
| Lessons | Agent's call, "most fun version": all four fixes in section 3, done visually, no lecture. |
| Train game | **Race with a step dial**: continuous learning rate, change it mid-run, fewest steps to 85 % scores best. |
| Backprop game | **Twist the hot weights**: glowing weights with up/down gradient arrows; twist before they cool; the 3's bar climbs. |
| Score | **Stump-Pixel leaderboard** (endless bonus, daily board beside the booth). |
| Pixel | **Pixel IS the network**: its face shows the live confidence; always faces the rider. |
| L2 teaser | **Zoom out, then next word**: 970 weights shrink to a speck in a chatbot-sized network; a sentence builds token by token with top-guess bars. |
| Voice | "Use a fun voice, we can change it later, make it good." Human recording or ElevenLabs stay open for Ignite. |
| Playtest | Owner only for the teacher demo; kids 10–16 before Ignite. |

## 2. Teacher review of v6 (what stays, what was wrong)

Keep: real MNIST, real network, real SGD; the rider's 3 carried end to end; honest end card ("87 % right on 500 new
digits, you trained it on 7,680"); live loss/accuracy curves; weight painting (you are the learning algorithm).

Wrong or missing: labels/supervised learning never named; generalization is one line, and "Pixel studies your 3" is
presented like a test; loss line describes squared error while the code uses cross-entropy; "drag the wire to the 3"
implies backprop rewires (it only turns weights up/down); no reveal of what the 16 hidden neurons learned; the Valley
claims to be "the" landscape (970 dimensions, one slice); the descent graph says "lots of mistakes", the voice says "loss".

Play Mode bugs: "TEACH ONE NEURON" overlaps "SCORE"; "STOP CLEAR" and the stars overlap the neuron sky panel; Pixel
covers "4/8 RIGHT"; backprop and tokens explainer panels sit on the world network's labels; the subtitle peeks over the
hunt board; Pixel turns its back during explainers; magenta noise specks fly through explainer lines; the Valley does not
move while you train (a static pink half-pipe); explainers are flat panels in a void. ~40 % hands-on, ~7.5 min.

## 3. The agreed learning path (Ignite target)

| # | Stop | Concept (named) | Game | Wow |
|---|---|---|---|---|
| 0 | Dock | the problem | — | a wall of 3s; Pixel can't read one; "let's build Pixel's brain" |
| 1 | Pixels | pixel, input | Draw your 3 | your strokes become 49 glowing numbers |
| 2 | Data (travel, ~10 s) | dataset, label, supervised learning | — | a river of labeled digits flows past ("written by Census staff and high-school students") |
| 3 | Neuron | weight, bias, sigmoid, activation | Paint the weights | the neuron fires for 3s only, by your hand |
| 4 | Network | layer, forward pass, softmax | Send your 3 | you sit inside the net as the signal flies past; Pixel looks confused |
| 5 | Loss | loss (cross-entropy: how surprised by the right answer) | — | one number hangs over everything |
| 6 | Backprop | chain rule, gradient | Twist the hot weights | blame flows back through you; the 3's bar climbs |
| 7 | Descent | gradient descent, learning rate, batch | Race with the step dial | the terrain reshapes and the pod rides the live loss; huge blows up |
| 8 | Test | generalization, test set | Pixel reads your 3 | "87 % on 500 digits it never saw"; a miss becomes "practice = fine-tuning" |
| 9 | Reveal | hidden features | — | the 16 hidden neurons turn to show what training painted (callback to stop 3) |
| 10 | Loop | training loop | — | guess, loss, backprop, step, repeat |
| B | Bonus | errors, confidence | Stump Pixel, endless, leaderboard | — |
| 11 | Teaser | tokens, scale, next-token prediction | — | zoom out to billions of weights; a sentence writes itself |

## 4. Phase 0 — teacher demo, 2026-10-28 (low risk, no new systems)

- [ ] T1 Layout bugs from section 2 (boards in `Tools/Editor/NeuralRideBuilder.Games.cs`, explainer placement, Pixel spot).
- [ ] T2 Pixel always faces the rider; no noise specks while explainer lines play.
- [ ] T3 Honesty lines inside existing beats (`Tools/Editor/RideNarrationData.cs`): labels line, cross-entropy wording,
      "one slice of 970 directions", hunt says nudge up/down, "studies your 3" said as fine-tuning, graph axis LOSS.
- [ ] T4 Fun voice for all lines (text changes silence old clips: a clip plays only when its sidecar matches), Whisper
      verify, install, rebuild.
- [ ] T5 Stretch: hidden-neuron reveal at the right stop (16 7x7 weight images from the live weights).
- [ ] T6 EditMode + PlayMode, seat captures + vr-qa, one full desktop run. Then freeze, commit, demo from that commit.

## 5. Phases 1–3 — Ignite

1. Games: train race dial, twist-the-weights backprop, Pixel-is-the-network face, stump leaderboard; then kids playtest.
2. Step inside: network and Valley explainers grow around the rider; the terrain follows the live loss.
3. Pace to ~6 min / ≥ 60 % hands-on, the Level 2 teaser, the final voice (human or ElevenLabs).

## 6. Progress log

### 2026-10-27 — review and decisions (Claude)
- Play Mode run of v6 through every stop (autopilot GO, operator hurry); findings and decisions above. Nothing built yet.
