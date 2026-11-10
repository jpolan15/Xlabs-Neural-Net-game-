# TASK: Level 2, the Word Machine (ride inside a language model)

Owner brief (2026-10-30, left for the evening): the teacher found Level 1's explanations great but fast for anyone who has never seen AI,
and the backprop game (drag the glowing wires onto the 3) not intuitive. Then: design Level 2, the ride teased at the end of Level 1,
as a ride *inside an LLM* where you see the machine working, with the same rhythm as Level 1 (a short explainer, then a mini-game),
different games, looking great and fun for Ignite. ADR: `Documentation/Architecture/ADRs/ADR-022-level2-word-machine.md`.

Owner brief v2 (2026-10-31): "Level 2 looks the same as Level 1; it can't be the same dark look." The school's VR room has an
exercise bike: Level 2 becomes a bike ride (the rider sits on the real bike and pedals, or on a chair). And "review it as a master AI
professor": the logic, long enough pauses, fun games that are not Level 1's. Built as ADR-023
(`Documentation/Architecture/ADRs/ADR-023-level2-bike-ride-and-review.md`).

Owner brief v3 (2026-11-10, presenting the same day, Level 2 demo the next): "make sure Level 2 is fully good and add a hub where I can
choose the levels I end up adding; retentive, fun, demo-ready; do not break existing features." Built as ADR-024
(`Documentation/Architecture/ADRs/ADR-024-level-hub.md`): `Assets/Scenes/LevelHub.unity` is build scene 0 (`Convergence/Build Level Hub`),
one card per entry of `Assets/ScriptableObjects/LevelCatalog.asset` (add a level: an entry plus its scene in the build), a per-device
scoreboard (best stars, high score, NEW RECORD) on the cards and on Level 2's end card, H (desk) or holding the left menu button 1.5 s
(headset) goes back to the hub from any level, and Level 2's final GO leads to the hub.

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
| End | `OutroExplainer`: Pixel says the rider's story; the loop recap; 970 vs 124 million vs hundreds of billions; "likely is not true, always check" | none | |

v2 (ADR-023): a daylight bike ride. `Convergence/DaySky` (sun, clouds, meadow; one look per stop, a day passing), the cargo bike `PodL2`
(`NeuralRideBuilder.Bike.cs`, `BikeView`: wheels and pedals follow the ride, bars steer at the story forks), one country road
(`Track/TrackWorld`) and a landmark per stop (`DockWorld` windmill, `TokenWorld` sawmill, `GalaxyWorld` observatory, `HallWorld` theatre,
`OddsWorld` fair, `RoadWorld` farm, `EndWorld` finish). Outlined text (`Level2Kit.outline`), soft glows, opaque boards. Script v2: four
new lines (242 bike, 243 the learned table, 244 attention blends meaning, 245 recap, 246 "always check") and corrected ones (why
tokens, low odds are not wrong, loss as surprise, loaded dice), pauses about twice as long; 47 lines re-voiced (Kokoro, temporary).

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
- Evidence: `Documentation/Design/captures/level2_v5/` (v2 live shots from a bot run, ADR-023).

## 3. How to run

- Desktop: open `Assets/Scenes/WordMachine.unity`, press Play. Mouse is the laser, Space skips a line, Shift+Space hurries a game, R restarts.
- Rebuild after code or data changes: `Convergence/Build Word Machine (Level 2)` (and `Convergence/Build Neural Ride` for Level 1).
- Re-bake the data: `uv run --with torch --with transformers --with numpy python -I ArtSource/training/bake_level2.py bake --out Assets/_Project/Data/Level2/level2_words.json`.
- Voices: edit `Level2NarrationData.cs`, then `render_voices.py render --lines N`, `verify`, `install` and rebuild. The system Python lacks
  the packages; run it through uv: `uv run --no-project --python 3.13 --with kokoro-onnx --with soundfile --with pyloudnorm --with scipy
  --with numpy --with faster-whisper python render_voices.py ...` from `ArtSource/voice/`.
- Tests live in the local package `com.convergence.tests` (`Tests/`): after editing one, `AssetDatabase.ImportAsset("Packages/com.convergence.tests/...")`
  or Unity keeps running the old build of it.
- A bot rider in Play Mode: `Convergence.EditorTools.Level2Bot.Start(0.9f)` and `RideLiveShot.AutoPilot(true, 3f)` (UnityMCP `execute_code`).
- Level 1 ends with GO lighting for "LEVEL 2"; Level 2 ends with GO for the level hub (`FinaleView.nextScene`, ADR-024).
- The hub: open `Assets/Scenes/LevelHub.unity`, press Play, click (or laser) a card. H goes back to the hub from any level; in the hub,
  Shift+Backspace wipes the scoreboard. Rebuild after hub code changes that touch the scene: `Convergence/Build Level Hub` (the cards
  themselves are built at load from the catalog, no rebuild needed).

### Playtest v2 (owner, on the Quest over Link)

1. Open `Assets/Scenes/WordMachine.unity`, press Play (or finish Level 1 and pull GO on its end card). About 10 to 12 minutes.
2. Watch for: does the first look say "new level" at once (sunny road, bike under you); can you read every board without leaning;
   are the pauses long enough to follow; does each game make sense from its one instruction; does the bike's steering at the story
   forks feel good or make you queasy; does anything block the road or a board.
3. Exercise bike: a stationary bike only, a spotter beside it, no standing on the pedals. The seat re-centres the eye height by itself
   (chair or bike). Hold the controllers; rest them on the bike's bars between games. The virtual pedals turn only while the ride moves.

### Playtest v3 (owner, hub + Level 2 demo)

1. Before the group: open `Assets/Scenes/LevelHub.unity`, press Play, Shift+Backspace to wipe the scoreboard (test rides write it).
2. In the headset: point at LEVEL 2, pull the trigger. Desk: click the card. The hub fades out and Level 2 boots.
3. Space skips a line, Shift+Space solves a game, H (desk) or holding the left menu button 1.5 s (headset) goes back to the hub.
4. At the end: the card shows the ride's points and stars; GO goes back to the hub, whose Level 2 card now says NEW RECORD.
5. Watch for: card text readable at 2.8 m, the laser hover tick, the menu-button hold not firing by accident.

## 4. Status

- [x] Level 1 pacing and backprop coach (built, EditMode tests green; the coach is verified in Play Mode only by the bot, not on a headset).
- [x] Level 2 built end to end, played start to finish by the bot and by `Level2RideTests`.
- [x] All 42 Level 2 lines voiced with the temporary Kokoro voice (WER under 10 % by Whisper). The owner dislikes synthetic voices: offer
      a human recording (`ArtSource/voice/RECORDING_SCRIPT.md` style) before Ignite.
- [x] v2 (ADR-023): daylight bike ride, reviewed and slower script, re-voiced, story game steers the bike; played end to end by the bot,
      EditMode tests green (Level2Tests, RideNarrationTests).
- [ ] Ridden on a Quest 2 (comfort of the galaxy view, laser aim at word sizes, draw-call budget of the countryside on the headset).
- [ ] Ridden on the school's exercise bike (does the virtual bike sit right under a real one; is the dash readable while pedalling).
- [ ] A human voice for Level 2.
- [x] Level hub (ADR-024): build scene 0, catalog-driven cards, scoreboard, way home; EditMode + PlayMode tests green; hub to Level 2
      and back played on desktop. Not yet seen in the headset (card size at 2.8 m, laser hover, menu-button hold).
- [ ] Ideas not built: a second attention head, a "guess the next word" duel against the machine, sharing the written story (QR) at the booth.
