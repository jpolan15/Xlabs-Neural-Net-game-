# ADR-022: Level 2, the Word Machine: A Ride Inside a Real Language Model

| Field | Value |
|---|---|
| ID | ADR-022 |
| Date | 2026-10-30 |
| Status | Accepted (built, played end to end by a bot and by a PlayMode test rider on desktop; not yet ridden on a headset) |
| Deciders | Project owner (brief: "a ride inside the LLM where you see stuff working", mini-games close to Level 1's but different), after the ML teacher's review of Level 1 |
| Builds on | ADR-017 (explainers), ADR-018 (mini-games), ADR-020 (one thread), ADR-021 (a lit world at every stop). Their rules hold. |

## Context

Level 1 ends on "NEXT: LEVEL 2 - WORDS". The teacher called Level 1 amazing but fast for anyone who has never met AI, and found the
backprop game (drag glowing wires onto the 3) hard to read. Level 2 has to keep Level 1's rhythm (a short explainer unfolds, then a
mini-game), look and sound like the same ride, and not repeat Level 1's games. It must also stand up to an ML teacher: no invented numbers.

## Decision

1. **Five stops, one real model.** Tokens, meaning, attention, prediction, writing, with a chat-window dock and a finale where Pixel says the
   rider's own story. Every number comes from one baked file (`level2_words.json`) made by `ArtSource/training/bake_level2.py` from
   GPT-2 small (124M, MIT): BPE tokens and ids, the PCA of real token embeddings (768 numbers squashed to 3), the cosine neighbours, the
   real attention head (layer 5, head 4, chosen by search over all 144 heads), next-word odds, temperature-scaled odds and the odds of
   every fork of the story road. The game reads the file and validates it (`Level2Data.Parse`); nothing is hardcoded to look right.
2. **Five games, none a repeat.** Chop words at their glowing seams (`SliceMoment`, a swipe game); carry words into their neighbourhood of
   the word galaxy (`GalaxyMoment`, a carry-and-drop game); hold the laser on the words that matter and your attention shares (softmax)
   fill up (`SpotlightMoment`); bet ten chips on the next word against the machine (`PredictMoment`); shoot the word you want at three
   forks and learn how unlikely your story was (`StoryMoment`). Each is a `RideGame` (score, stars, STOP CLEAR, pull GO), ends in a win
   the rider earns, and has a path to its end that needs nobody (hurry, or give-up help for one star).
3. **Pure games, drawn separately.** Gameplay owns rules and takes input through `PickTarget` (laser events) and `RideAim`; Presentation
   draws (`GlowField`, `RibbonField`, `ShapeBatch`, TextMeshPro) and never decides correctness. One MonoBehaviour per file.
4. **The explanation and the game are on screen together.** The play segment says its instruction while the game already runs
   (`Segment.arrivalWithMoment`), so the rider never stares at an empty stage while being told what to do.
5. **Same ride, same builder.** `NeuralRideBuilder.Level2.cs` builds `WordMachine.unity` with the Level 1 pod (a second prefab `PodL2` with
   Level 2's end card), the Level 1 lasers, table, director, atmosphere and sky (own theme `RideTheme_L2`, lit horizons, none orange).
   Narration ids 200 to 241 in `Level2NarrationData.cs`; the voice tool reads both files. GO on the Level 1 end card loads Level 2, and
   Level 2's loads Level 1 (`FinaleView.nextScene`).
6. **Level 1 follow-up for the teacher.** Pauses are 2.2 times longer; explain stops hold their finished picture 3.5 s; five plain-words
   lines (why 49 pixels, what a neuron judges, what a weight is, what blame is, what descent is); the loss line defines the loss. The
   backprop game coaches itself: a ghost hand drags the first loose wire onto a gold ring labelled "DROP ON THE 3" after 4 s of no action.
7. **Tested without a headset.** EditMode: the bake's contents and honesty (attention agrees with the words a person would name, softmax
   shares sum to one, the story tree is complete), game rules driven through their picks, the galaxy kept in the rider's gaze and its
   neighbourhoods more than two drop cones apart, the built scene. PlayMode: the whole ride played by a test rider; every game must
   be won by the rider, GO must wait after each, the story must reach the finale, no error may be logged.

## Consequences

- A quick, perfect test rider finishes Level 2 in about 7 minutes (3.6 of them speech); a person takes 8 to 10, long for a booth. Each
  stop can be skipped with Space and every game hurried with Shift+Space.
- The voice is the temporary Kokoro one (Whisper-checked). The owner dislikes synthetic voices; a human recording is the planned fix.
- Not verified: headset feel (aiming at word sizes at 8 m, the galaxy picture's comfort, draw calls on a Quest 2; Level 2's stops use
  additive glow quads and one text object per word, which the budget tool has not yet counted on a headset).
