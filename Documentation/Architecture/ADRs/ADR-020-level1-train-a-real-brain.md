# ADR-020: Level 1 v6: Train a Real Brain (the Rider's Own 3, Trained Live)

| Field | Value |
|---|---|
| ID | ADR-020 |
| Date | 2026-10-26 |
| Status | Accepted (built; see the task file's progress log for the test runs; not yet ridden on a headset) |
| Deciders | Project owner ("go ahead and build ... make it fun game"), after the Oct 21 headset ride of v4 and a playtest by their sister |
| Builds on | ADR-018 (mini-games, game table, lasers) and ADR-019 (table pose, fingertips, wire dragging). Their rules hold. |

## Context

The owner found Level 1 incomplete, slow and disconnected ("gradient descent is kinda just thrown in"); their sister
found the descent "too slow and nothing even happens"; both wanted a fun game with the feel of a 3Blue1Brown or graduate
ML video. In the code: the 3 the rider drew was thrown away (every later stop read the bake's hero 3), the network
never learned in front of the rider (the Valley replayed an offline run), and bias, sigmoid, the training loop, the
learning rate and testing on unseen digits were missing or one line each. The games were clicks on cells and buttons.

Level 1's network is 49 → 16 → 10 (944 weights, 970 numbers with the biases). A mini-batch step of 32 digits is about
100 thousand multiply-adds, so the Quest can train it live at 40 steps a second.

## Decision

1. **One thread.** `Level1Session` holds the rider's digit and the one network they teach. The draw game writes the
   digit (Core `DigitCanvas`: strokes scaled into MNIST's 20×20 box, a soft pen, centre of mass to the middle, 4×4
   pooling, exactly the bake's `pool7`); the forward pass, the blame, the training game, Pixel's reading and the
   network diagrams all use it. If the rider never draws enough ink, the hero 3 stands in.
2. **Real training, live.** Core `DigitNetwork` + `LiveTrainer` (allocation-free; xorshift32 batches) is the bake's own
   step. The bake gains a `live` section: 3,000 balanced training digits, 500 balanced test digits, three step sizes
   (0.15, 4, 40), and a replay of each with the game's sampler (good reaches 85 % at step 240; tiny 41 % at 300; huge
   peaks at loss 21). EditMode tests hold the C# trainer to Core's backprop (1e-12) and to the bake's replay.
3. **The training game replaces "plug the strings" and "roll it down".** Hold a step-size button to train; the loss
   curve and the test accuracy are measured, not scripted; the huge step really blows the loss up and is undone (back
   to the best weights so far); the game is won at the bake's target accuracy; then a fast plunge through the Valley.
4. **Hold-to-act input.** `IRideStrokeTarget`: a held trigger (or A/X, or the mouse) draws on the canvas and sprays the
   weight grid cell by cell (a sweep paints, a fresh press on a cell of the same colour clears). `GameButton` passes a
   press to whichever of its games is live and the release back to it (hold buttons, shared DONE/CLEAR).
5. **Payoff and bonus.** Pixel reads the rider's own 3 with the weights they trained; if it misses, it studies that 3
   with real steps (half of each batch). Bonus round "stump Pixel": draw prompted digits, Pixel guesses live (ten bars).
6. **Script v6** (lines 119+, v4/v5 lines reserved with a V5 prefix): 3Blue1Brown-style, real terms (activation,
   weight, bias, sigmoid, layer, loss, backpropagation, chain rule, gradient, learning rate), no baby talk; the
   narrator's numbers are checked against the bake. Voice: a temporary Kokoro render; the owner rejected every
   synthetic voice tried and is choosing between recording a human narrator and ElevenLabs. Never clone a real person.
7. **Honest displays.** The network diagrams are rewritten at runtime from the session (`NetworkLiveView`), the hunt
   re-aims at the rider's own hottest wires (`BlameView.Reaim`, blame mesh in network order), the trained network's sign
   shows the rider's measured accuracy (`TrainedSignView`), and the finale walls no longer show the bake's answers.

## Consequences

- The Valley track is still the bake's training path (a victory ride after the rider's training), not the rider's path.
- A rider's drawing can be unusual enough that the trained network misreads it; the study step makes the payoff
  honest instead of scripted.
- Every new board stands 8 mm in front of the table plate (no z-fighting; the sightline check sees them). The pixels and
  rim viewpoints sit at 99 of 100 draw calls: the next addition there needs a merge first.
- Not validated on a Quest: hold-to-draw precision at 0.6 m, training speed on device, comfort of the faster travel.
