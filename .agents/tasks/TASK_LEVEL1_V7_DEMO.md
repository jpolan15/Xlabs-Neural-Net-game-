# TASK — Level 1 v7: teacher demo polish (blast the 3s, intro roadmap, what it learned)

Builds on v6 (`TASK_LEVEL1_V6_TRAIN.md`, ADR-020). Owner, 2026-10-27: teacher demo **2026-10-28**, the teacher wears the
headset. "Presentable and awesome", impresses an ML teacher, fun enough that it never feels like a lecture (sister: "it
didn't feel like a game, more like a lecture; give us a gun and things to shoot while driving"). Claude builds and
tests on desktop tonight; the owner rides it on the Quest before the presentation and fixes what breaks.

## 1. Teacher review (Claude, as an ML teacher, 2026-10-27)

Strong: a real network on real MNIST, real backprop and SGD on the rider's own 3 (3Blue1Brown chapters 1-3).
Gaps found: loss described as "add up how far off" (it is softmax + cross-entropy); labels / supervised learning never
named; generalization is one line, and E7's "studies your 3" trains on the test item; "each one can learn a different
stroke" is a hope, not what training does; the landscape has 970 directions; 944 weights vs 970 numbers unexplained.
Fun: the train game has one right button; SCORE means nothing; ~40 % hands-on. Look: mostly void and small boards.

## 2. Decisions (owner, grilled 2026-10-27)

| Question | Decision |
|---|---|
| Scope | Level 1 only, for tomorrow's teacher demo. Later levels and Ignite (April 2027) later. |
| Learner | A curious teen who'd quit a lecture, and an ML teacher who'd catch a wrong fact. |
| Gun | **Blast the 3s**: real handwritten digits fly at the pod on every travel leg; blast the 3s, spare the rest (you are the classifier). Blaster feel: bolt, buzz, pixel burst, combo. |
| Intro | Say quickly what we learn and what happens: build and train a real network to read your handwriting, the five stops, blast the 3s on the way. |
| Teaching fixes | **Show what it learned**: after training, the 16 hidden neurons' real weights as 7×7 images, next to the neuron the rider painted by hand. Plus (Claude's call) the loss line fixed to cross-entropy, and line 139 made honest. |
| Train game | Keep v6's three buttons for tomorrow. After the demo: a live step-size dial and a race to 85 % (honest: the gradient picks the direction, the rider only the step size). |
| Today | Claude builds and verifies on desktop (tests, Play Mode, seat captures, vr-qa); the owner rides tomorrow. |

## 3. Checklist (the live list: tick as you go; a future agent resumes from the first unticked box)

Data
- [x] Bake `targets` in `ArtSource/training/bake_level1.py`: 12 threes + 2 each of 8, 5, 2, 9, 7, 0 (28×28, clear:
      trained p > 0.9); rest of `level1_digits.json` byte-identical (only `meta.date` changed). `Level1Data.targets` + checks.

Blast the 3s (the gun)
- [x] `ZapGame`: digits fly at the pod; a 3 scores 10 × streak multiplier (x2 after 3 in a row, max x4); another digit
      −10 and ends the streak; an escaped 3 ends it; first mistake says `ZapWrong`. `ZapSpeck` carries picture + label.
- [x] `BlasterView` (new): all targets in one batched mesh facing the eye, a bolt down the laser per shot while the
      digits fly, a pixel burst (gold for a 3, red for a mistake).
- [x] `GameHudView`: "+20 x2" / "NOT A 3 -10" popups, scaled to stay readable at distance. `RideSfxView`: blaster
      shot during travel, soft click elsewhere; burst / buzzer on hit.
- [x] Bug fixed: the v5 game sounds (fire, pop, plugIn, win, winShimmer) had no clips (silent); `Cues` table in
      `NeuralRideBuilder.Audio.cs` now sets them (levels by measured LUFS).
- [x] Builder: targets atlas (`ExplainerTargets`), collider-only target template, `BlasterView` wiring, zap labels.

Intro
- [x] `DockExplainer`: roadmap steps ({stops}: five stops light as named; {blast}: "blast every 3"; fade on {go}).
- [x] Builder: roadmap pieces in `BuildDock`; `IntroStops` in the dock explainer's lines and `IntroLines`.

What it learned
- [x] `TrainingExplainer`: {learned} the rider's painted neuron, {sixteen} the 16 trained hidden neurons from the
      session's live W1 (green +, red −), {right} clears them.
- [x] `ForwardPassMoment` (trained) says `LearnedPainted`, `LearnedHonest` before `RightYours`.
- [x] Builder: `Learned` batch + titles in `BuildTraining`; the new lines in the E7 explainer's lines.

Script (`Tools/Editor/RideNarrationData.cs`, ids in `RideTypes.cs` 174-177)
- [x] 121 PixelBrain (what we build and why), 174 IntroStops, 130 ZapIntro (blast the 3s), 175 ZapWrong, 139 honest
      ("each will learn to look for something different"), 145 loss ("measure how far off", not "add up"), 176/177 reveal.
- [x] Tests that list lines / ids (`Tests/EditMode/Ride/RideNarrationTests.cs`) and the zap part of
      `Tests/PlayMode/Gameplay/RideGamesTests.cs` (a 3 scores, another digit costs).
- [x] Temp voice: lines 119 (shortened), 121, 130, 139, 145, 174-177 rendered (Kokoro af_heart 1.1) into
      `ArtSource/voice/out/level1_v7`, Whisper worst 7.4 %, installed, 59/59 bound; `RECORDING_SCRIPT.md` updated.

Verify
- [x] Build (`Convergence/Build Neural Ride`): sightlines clear; draw calls <= 100 everywhere (pixels 97, right 76) after the
      roadmap switches off when it fades and the blaster's renderers draw only while something shows; effect levels pass.
- [x] EditMode 148/148, PlayMode 30/30 on the final build (the games test blasts a 3: +10, then a non-3: -10).
- [x] Play Mode shots (`Documentation/Design/captures/level1_v7/live_*.png`): roadmap, flying digits, reveal. Fixed from
      them: Pixel covered TRAINING and the hero 3 covered the blast line (roadmap raised to 18.5 deg, narrower); targets
      kept flying through the next lesson (now retire when the pod stops); the reveal was small (now 4x4 on its own
      backing, 0.6 m nearer). Seat captures and the vr-qa subagent were not run: every v7 piece appears only at run
      time, so the edit-mode seat views are unchanged; Claude reviewed the live shots instead.
- [x] ADR-021, DECISIONS_INDEX, PROJECT_MAP, AGENTS.md (points here), memory.
- [x] Commit (one commit on `jonathan/level1-explainers`; not pushed).

Owner, before the demo (agents can't): ride it on the Quest. Check: can you hit the flying digits with the laser (too
fast? too small? -> `ZapGame` spawnEvery / drift, `BlasterView` pictureSize); are the roadmap and the 7x7 weight panel
readable; is the climb comfortable; listen to the temp voice. Operator keys: Space skips a line, Shift+Space hurries a
game, R restarts.

Next (after the demo, agreed): training game -> a live step-size dial and a race to 85 % (honest: the gradient picks the
direction, the rider only the step size), with a leaderboard for Ignite.

### 3b. Look pass and cleanup (owner, 2026-10-28 night: "scenes shouldn't be all black, make everything look awesome";
"clean up old stuff so new agents don't read unnecessary context; keep it readable and working")

Cleanup (deleted outright on the owner's word; everything stays in git history)
- [x] Stale task files deleted, with the whole `.agents/tasks/archive/` (33 old plans), `COMPLETED.md`, `BLOCKED.md`,
      unused art (`synapse_pod`, `neural_core`, `gradient_valley`, `archival`, `voice_bakeoff`), ADR-008/010/015 and the
      empty Chamber 02-04 puzzle folders (all in git history; v3 master plan, digits restructure, handoff, remaining-work);
      `IN_PROGRESS.md` and `BACKLOG.md` rewritten to today's state; every live reference (skills, agents, READMEs, AGENTS.md files) fixed.
- [x] `Documentation/Design`: `GDD_OVERVIEW.md` rewritten as the current Level 1 overview; superseded design docs
      (v3 story, Chamber 01 level design and art bible) removed; old capture folders of cut versions removed.
- [x] Nothing breaks: layout validator 46/0, recompile 0 errors, EditMode 148/148 (PlayMode runs with the look pass).
      Removed 319 files (v2/v3 captures, 18 orphan model sheets of cut models, 3 superseded design docs); 4 plans archived.
- [ ] Not tonight (code; risky before a demo): the old Chamber 01 / asteroid-defense editor tools and
      `Level01_AwakeningGate.unity` (still referenced by old tests). Remove after the demo with their tests.

Look (every stop its own lit world, not a black void; diagrams stay readable; Quest 2 budget holds)
- [x] `Convergence/RideSky` sky shader: vertical gradient, horizon glow, sparse stars, a faint grid floor far below
      (all in the sky pass: no extra objects); camera clears to it; capture cameras too.
- [x] `RideTheme` region looks gain sky colours (zenith, horizon, glow, floor, stars); `RegionAtmosphere` blends them;
      the explainers' "blackboard" becomes a dark tint of the region's own sky instead of near-black.
- [x] A palette per stop (dock dawn, pixels cyan, neuron amber, network deep blue, loss red, backprop violet,
      valley sunset, right gold).
- [x] Rebuild: sightlines and text readable, draw calls within 100, EditMode + PlayMode green.
- [x] Live shots of every stop at real lighting; fix what they show; commit.
      Evidence: `Documentation/Design/captures/level1_v7/sky_*.png` (dock, pixels, travel, neuron, network, loss, backprop,
      valley, right). Fixed from the first set: explaining stops were still dark and floorless, so the blackboard now
      dims the region's sky to 0.78 (was near-black) and keeps the floor at half strength; the old pod-relative grid is
      off (two grids at one depth). Build: sightlines clear, no stop over 100 draw calls (the sky is one draw); EditMode
      148/148 (the near-black blackboard test now checks every horizon is lit but dark enough for white text), PlayMode
      30/30. Not judged: how the moving floor grid feels in a headset (comfort; lower `floor` alpha in
      `NeuralRideBuilder.cs` if it is too strong).

### 3c. First Quest ride of v7 (owner, 2026-10-27, over Quest Link: "the games literally do not work")

Found live in the owner's Link session (UnityMCP reads the running headset game):
- [x] Every laser stopped 7.8 cm out on its own controller's fingertip trigger (HandPolarityController), so nothing
      could be drawn, shot, grabbed or pressed; shots landed only when the hand moved (the trigger's physics pose lags).
      `RideHandLasers` now skips both hands' colliders; fingertip on Ignore Raycast. PlayMode `RideHandLasersTests`.
- [x] Eye 2.59 m above the pod floor (target 1.30): PodSeat only normalised while `userPresence` read true, and Link
      reports false while worn, so every board sat 78 degrees down. Presence no longer gates; a far-off eye (> 0.6 m)
      re-seats within 0.75 s even mid-game; normalising also centres x/z on the seat (the owner sat 33 cm forward).
- [x] Aim help: a press that just misses fires the nearest flying digit, button or hot wire within 5 degrees.
- [x] "Blast every three" was cut off by the pixels stop: the director now waits for said lines before arrival lines.
      A stray press during the intro no longer latches GO (which skipped the intro after 2 s).
- [x] "What it learned" vanished after ~10 s (the {right} cue; not the joystick: locomotion is off): 6 s dwell added.
- [x] 3 detector legible: the eight test digits beside the grid (3s under GLOW, others under DARK), gold when the
      neuron fires, ringed green/red; title "3 DETECTOR"; line 135 rewritten.
- [x] Hints in every game (`GameCoachView`): a bouncing gold arrow on the next thing to aim at (draw box, then DONE;
      Core's hint cell, lit after 2.5 s; the card; the step buttons; up at the wires), hidden while a trigger is held;
      a faint 3 to trace; "BLAST THE 3s!" at the first digit of each leg.
- [x] Recap before the tokens teaser: "Quick recap. Five stops." then one numbered line per stop (178-183) with rows
      lighting on the E8 card; the end card lists 1-5. Voice: Kokoro af_heart 1.1 (temp), Whisper 0 % on all 7.
      Narration cap raised 270 -> 300 s for it (now 284 s).
- [x] Build (sightlines, text >= 1.5 degrees, draw calls) passes; EditMode 148/148, PlayMode 31/31 on the final build.
      Play Mode shots of the draw and 3-detector boards checked (arrow, trace 3, GLOW/DARK columns).
- [x] Owner rode it again over Link (2026-10-27, watched live: head view and status every 25 s): every game played
      and won by the rider, score 3000, no errors. Owner: "pretty good".

Second ride's notes (owner + what the live watch showed):
- [x] The 3 detector won with 4 green cells ("doesn't look like a 3"): the line is now 5.0 (was 2.5) and the reference
      a fuller green 3 (`PUZZLE_SHOWN_PLUS_CUT`); the smallest win is 8 cells in a 3 shape, hints take 9. Re-baked:
      only `puzzle.threshold`, `referenceSolution` and `hintMovesFromEmpty` changed (same eight digits).
- [x] Backprop "stuff covering each other": the wishes card hid the network (its layer names showed through it, and
      the backwards sparks ran behind it), then hung in the network through the rewind; flying digits crossed the
      network on the rewind. The card now fades on {back}, the layer names are off while it is up, and the rewind leg
      has no digits (`ZapGame.quietSegments`).
- [x] Seat: re-centres when 0.3 m forward/back/sideways too (head still, no game); the owner sat 42 cm back.
- [x] "HOT-WIRE HUNT" overlapped SCORE: now "WIRE HUNT". Recap rows bigger (0.19-0.2 m type at 5 m), shorter lines.
      The accuracy sign is hidden when the end card pops (it peeked over it).
- [x] Build passes; EditMode 148/148, PlayMode 31/31. `RideDirectorTests` puzzle gate now paints toward all-plus in an
      order that never passes on the way (a win sticks; at line 5.0 index order passed at cell 32). Play Mode shots
      checked: the card up (no labels through it), {back} (card gone, sparks over the network), the rewind (no
      digits), the recap rows, the end card. Not yet ridden on the Quest.

## 4. Progress log

### 2026-10-27 — review, grill, plan (Claude)
- Sections 1-3. Unity opened with `unity open .` (it was closed; UnityMCP down).

### 2026-10-28 (night) — v7 built (Claude)
- Everything in section 3 above, ticked with its evidence. Commands: bake `uv run --with numpy python -I
  ArtSource/training/bake_level1.py --out Assets/_Project/Data/Level1/level1_digits.json` (3 min); editor via the
  unity CLI (`unity recompile`, `unity cmd menu -- "Convergence/Build Neural Ride"`, `unity cmd run_tests --mode
  EditMode|PlayMode --async_tests true` + `unity cmd test_status`); voice `uv run --with numpy --with pyloudnorm --with
  soundfile --with scipy --with kokoro-onnx python render_voices.py render --lines ... --out out/level1_v7` (verify adds
  `--with faster-whisper`). Live shots: `RideLiveShot.ShootWhen` + `AutoPilot`, driven fast by a scratch eval that sets
  timeScale 3 and hurries each game.
