# TASK: Level 2, the Word Machine (ride inside a language model)

Owner brief (2026-10-30, left for the evening): the teacher found Level 1's explanations great but fast for anyone who has never seen AI,
and the backprop game (drag the glowing wires onto the 3) not intuitive. Then: design Level 2, the ride teased at the end of Level 1,
as a ride *inside an LLM* where you see the machine working, with the same rhythm as Level 1 (a short explainer, then a mini-game),
different games, looking great and fun for Ignite. ADR: `Documentation/Architecture/ADRs/ADR-022-level2-word-machine.md`.

## 1. What exists

Level 1 v7.1 (teacher feedback):

- Pace: `RideNarrationData.Breath = 2.2` multiplies every pause; E2 to E5 hold the finished picture `ExplainHold = 3.5 s` (Space skips);
  plain-words lines 184 to 188 (why 49 pixels, what a neuron judges, what a weight is, what blame is, what descent is); the loss line
  now says "how far off was it, that score is the loss".
- Backprop game: `BlameView` coaches by itself. After 4 s with no wire dragged a ghost hand carries the first loose wire to the 3
  (gold ring, "DROP ON THE 3", numbered steps); the cue line says where to drag; the card says "DRAG THE GLOWING WIRES ONTO THE 3!".

Level 2 (scene `Assets/Scenes/WordMachine.unity`, built by `Convergence/Build Word Machine (Level 2)`; 11 segments):

| Stop | Explainer (cues) | Game (moment) | Real GPT-2 data |
|---|---|---|---|
| Dock | `WordDockExplainer` chat window rushes past, stops, GPT-2 badge | none | thread sentence tokens |
| 1 Tokens | `TokenizeExplainer`: chop, name tags, rare words | `SliceMoment` chop words at their glowing seams, leave whole words, boss word | `words`: tokens and ids of 14 words |
| 2 Meaning | `MeaningExplainer`: name tag, 768 numbers squashed to 3, the galaxy, closest to cat, king - man + woman | `GalaxyMoment` carry loose words to their neighbourhood | `galaxy`, `neighbours`, `analogies` (PCA of real embeddings) |
| 3 Attention | `AttentionExplainer`: a word only looks back | `SpotlightMoment` hold the laser on the words that explain "it" (softmax shares), then the real head is shown | `attention` (layer 5, head 4) |
| 4 Prediction | `OddsExplainer`: scores to odds, the loop of learning | `PredictMoment` ten chips over the next word, beat the machine | `predict` (top words and odds) |
| 5 Writing | `WritingExplainer`: loop, temperature dial | `StoryMoment` shoot the word you want at 3 forks; the story's odds are the product of the lanes | `story`, `temperature` |
| End | `OutroExplainer`: Pixel says the rider's story; 970 vs 124 million vs hundreds of billions | none | |

Honesty rules (an ML teacher reviews this): every number shown comes from `Assets/_Project/Data/Level2/level2_words.json`, baked by
`ArtSource/training/bake_level2.py` from the real GPT-2 small (124M); attention is causal; a story's probability is the product of the
lane odds; kid-unsafe words are blocked on the road (`BLOCKED`, checked by eye on the baked tree); nothing is invented.

## 2. Where things live

- Gameplay (rules, no visuals): `Assets/Scripts/Gameplay/Ride/Level2/` (`Level2Data` + `Level2DataSource`, `Level2Session`, `Level2Game`,
  `PickTarget`, the five moments). Games take picks through `PickTarget` (laser events) and `RideAim`.
- Presentation (display only): `Assets/Scripts/Presentation/Ride/Level2/`, one MonoBehaviour per file (a second class in a file shows up as
  MISSING in the saved scene). Kit: `Level2Kit` (materials, fonts, shapes), `GlowField`, `RibbonField`, `ShapeBatch`.
- Builder: `Tools/Editor/NeuralRideBuilder.Level2.cs`, narration `Tools/Editor/Level2NarrationData.cs` (ids 200 to 241), bot
  `Tools/Editor/Level2Bot.cs`. Voices: `ArtSource/voice/render_voices.py` reads both narration files.
- Tests: `Tests/EditMode/Ride/Level2Tests.cs` (data, galaxy framing, rules, built scene), `Tests/PlayMode/Gameplay/Level2RideTests.cs`
  (the whole ride, played by a test rider, every game won by the rider).
- Evidence: `Documentation/Design/captures/level2_v1/` and `level2_v2/` (live shots from a bot run).

## 3. How to run

- Desktop: open `Assets/Scenes/WordMachine.unity`, press Play. Mouse is the laser, Space skips a line, Shift+Space hurries a game, R restarts.
- Rebuild after code or data changes: `Convergence/Build Word Machine (Level 2)` (and `Convergence/Build Neural Ride` for Level 1).
- Re-bake the data: `uv run --with torch --with transformers --with numpy python -I ArtSource/training/bake_level2.py bake --out Assets/_Project/Data/Level2/level2_words.json`.
- Voices: edit `Level2NarrationData.cs`, then `render_voices.py render --lines N`, `verify`, `install` (needs Python 3.13 with kokoro-onnx etc.; see
  `ArtSource/voice/README.md`) and rebuild.
- A bot rider in Play Mode: `Convergence.EditorTools.Level2Bot.Start(0.9f)` and `RideLiveShot.AutoPilot(true, 3f)` (UnityMCP `execute_code`).
- Level 1 ends with GO lighting for "LEVEL 2"; Level 2 ends with GO for Level 1 again (`FinaleView.nextScene`).

## 4. Status

- [x] Level 1 pacing and backprop coach (built, EditMode tests green; the coach is verified in Play Mode only by the bot, not on a headset).
- [x] Level 2 built end to end, played start to finish by the bot and by `Level2RideTests`.
- [x] All 42 Level 2 lines voiced with the temporary Kokoro voice (WER under 10 % by Whisper). The owner dislikes synthetic voices: offer
      a human recording (`ArtSource/voice/RECORDING_SCRIPT.md` style) before Ignite.
- [ ] Ridden on a Quest 2 (comfort of the galaxy view, laser aim at word sizes, draw-call budget on the headset).
- [ ] A human voice for Level 2.
- [ ] Ideas not built: a second attention head, a "guess the next word" duel against the machine, sharing the written story (QR) at the booth.
