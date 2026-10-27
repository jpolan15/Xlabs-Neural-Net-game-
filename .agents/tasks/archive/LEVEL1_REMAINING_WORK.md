# Level 1: what is done, what is left (status 2026-10-12, night: agent work complete; owner checks left)

Plan: `TASK_LEVEL1_DIGITS_RESTRUCTURE.md`. Full steps: `HANDOFF_LEVEL1_FULL_BUILD.md`. Branch `jonathan/polishing-level-1`.

## Done

| WP | State | Evidence |
|---|---|---|
| WP1 gates | Script approved as drafted; telstar.wav kept (owner); commits per WP; MNIST download OK | task file §9 |
| WP2 bake | `ArtSource/training/bake_level1.py` → `Assets/_Project/Data/Level1/level1_digits.json`. 92.5 % test accuracy, untrained guess **1**, overshoot +1.05 loss | committed `87d6812` |
| WP3 Core | `WeightPaintPuzzle`, categorical cross-entropy, `NetworkModel.CreateDense`, `Level1Data` + 21 tests | committed `87d6812` |
| WP4–WP10 (first full pass) | Segment `RideDirector` + moments (puzzle, forward pass, blame, valley run); all 8 regions built from the bake; switchback track; region streaming/atmosphere/tags; XR weight cells + blame wires; desk G = gentle mode | committed (WP4–WP10 commit) |
| Tools | Sightlines, seat captures (117 views in `Documentation/Design/captures/level1/`), budget per viewpoint, model-sheet registry all moved to Level 1 | builder green: sightlines clear, max 86 draw calls / 30k tris |
| Tests | EditMode **112/113** (the 1 failure = no voice clips yet, expected); PlayMode **23/23** | |

## Left to do (in order)

### 1. ~~Fix PlayMode test run~~ DONE (23/23, see task file §9)
- 8 new ride tests failed only because the MCP-for-Unity package logs `[WebSocket] Connection failed` as an Error in Play Mode (its server is down). In `Tests/PlayMode/Gameplay/RideDirectorTests.cs` add `[SetUp]`: `LogAssert.ignoreFailingMessages = true` + collect `Application.logMessageReceived` errors; in `[TearDown]` fail on any error **not** containing `MCP-FOR-UNITY`. Or start the MCP server before running.
- `SensorPingTests.SelectOnVoyageButton_InvokesTheVoyageAction` (old chamber code) fails; it passed on 10-03. Check whether it fails on `87d6812` too (likely pre-existing, not ride code). Don't delete it.
- Run: `unity command run_tests -- --mode playmode --async_tests`, poll `Temp/pipeline_test_status.json`.

### 2. ~~Commit WP4–WP10~~ DONE
One commit once PlayMode is green (note in task file that the 11 old stop tests were **replaced** by the 10 segment tests, and the old narration tests by Level 1 ones).

### 3. Visual polish — mostly DONE (2026-10-12 night)
- DONE: network — a 7×7 plate of lit squares behind the 49 input nodes; the 3 now reads from the seat (87 draw calls).
- DONE: rim — hop 1 (the overshoot) stretched from 7 m to 21 m (`Layout.FirstHopLength`, `Layout.RowZ`); the rim now
  shows a hill to climb, not a wall. Loss values unchanged; PlayMode 23/23 after the change.
- DONE: valley sign fits one line; model sheets re-shot (19, 19:11). The 15 stale sheets of cut models
  (chapter_card, comet, diagram, drone, equation, explainer, finale, ice_chunk, live_board, loss_landscape, network,
  network_lit, neuron_core, rock, scanner_arch) still sit in `model_sheets/`: delete them (the agent's delete was blocked).
- DONE (vr-qa review, 2026-10-12): sun raised (−16, 30, 115) so it shows over the crest from the rim; accuracy
  sign backing now covers both lines; "10 ANSWERS" and "49 PIXELS" tags moved off the 9 and the plate.
- Not changed (judged fine): network edges leave the frame only at yaw ±30 (a head turn shows them); valley sign
  clipped only at yaw ±30; accuracy sign is small from the valley floor but read at the `right` stop.
- Open: the dash shows "0 · THE PAGE" in every edit-mode capture (run-time text); confirm in Play Mode.

Original notes:
- Network diagram: rebuild + recapture after the last tweak (wires dimmed, bigger pixel nodes) and confirm the **3 is visible in the 49-pixel grid**.
- Rim view: the overshoot hill fills the view 7 m ahead (honest shape of the data). Decide: accept, or lengthen hop 1.
- Valley sign rect widened; recapture to confirm no wrap.
- Run model sheets: `unity command eval "return Convergence.EditorTools.RideModelSheets.Capture();"`; look at all 18 sheets (backs, mirrored text, floating parts, wide shots).

### 4. ~~WP11 narration~~ DONE — 32 lines, Kokoro `bm_fable` 0.9 (same Guide voice as v3), verify worst WER 9.1 %, bound 32/32 (`8f667df`)
- Render the 32 Level 1 lines (ids 32–63, Guide voice) with `ArtSource/voice/render_voices.py`, run its `verify` (WER ≤ 10 %, −16 LUFS, −1 dBTP), install, rebuild. This also turns the last EditMode test green.
- **Owner listens** (I can't judge audio).
- Music: telstar.wav kept by owner choice — licence must be confirmed before Ignite.
- Put the 8 region frames side by side in the task file (proof regions look different).

### 5. WP12 verification — agent part DONE (full recapture, two vr-qa passes, fixes in `bcfc2d1`, `d638cb2`; desktop Play Mode: subtitles and voice play in sync)
- Full recapture (done tooling), `vr-qa` subagent reads `Documentation/Design/captures/level1/*.jpg` + model sheets; fix blockers.
- Play once on desktop end to end (GO, paint, point at wires, G, Space, R) and in the Meta XR Simulator (poke + ray on the grid).
- **Owner, headset (Quest 2):** comfort on the reverse and the drop (gentle mode G), text readability, both hands, 72 fps in OVR Metrics. Ask: headset days this week? What will the teacher grade?

### 6. ~~WP13 docs~~ DONE (ADR-016, DECISIONS_INDEX, PROJECT_MAP, README, TEAM_BRIEFING; both validators exit 0)
`ADR-016` (digits restructure, ribbon Valley instead of PCA plane, diagram network, 40° branch instead of 25°, segment director), `.agents/DECISIONS_INDEX.md`, `.agents/PROJECT_MAP.md` (`ArtSource/training/`, `Assets/_Project/Data/`, `Gameplay/Ride/Level1/`), README/TEAM_BRIEFING, task file status, final handoff report with every command and output. Run `Validate-CoreBoundaries.ps1` + `Validate-RepositoryLayout.ps1` (via `powershell -NoProfile -ExecutionPolicy Bypass -File ...`).

## Deviations to record (owner should know)
- Valley = ribbon of real loss along the real training path (PCA plane misplaced the run by up to 1.9 loss units).
- Network = a 30 m-wide diagram facing the rider (axial walls read as a tangle); the "Wrong" beat is said from the same stop.
- Grid = relief walls beside the track (a floor of pillars was hidden by the dash).
- Branch to the Valley turns 40° (eased, ~6°/s), not 25°, to clear the diagram.

## Final handoff (2026-10-12, night)

Commits on `jonathan/polishing-level-1`: `ba746b4` WP4–WP10, `1a7503e` network plate + 21 m first hop, `bcfc2d1`
sun over the rim + sign backings, `8f667df` voices, `d638cb2` end card + neuron readout, then docs and the test
log-scope fix.

| Command | Result |
|---|---|
| `unity command run_tests -- --mode editmode --async_tests` | 113/113 passed |
| `unity command run_tests -- --mode playmode --async_tests` (twice) | 23/23 passed both times |
| `NeuralRideBuilder.Build()` | Sightlines clear at 1.15/1.30/1.45 m; every view within 100 draw calls / 100k tris (max 87, network); voice clips bound 32 of 32 |
| `render_voices.py render --lines 32..63 --out out/level1` | 32 clips, −16 LUFS, true peak ≤ −1.02 dBTP |
| `render_voices.py verify out/level1` | 32 clips, worst WER 9.1 % (line 32: "read" heard as "rate"), none over 10 % |
| `Validate-CoreBoundaries.ps1` | 17 files, 0 violations, exit 0 |
| `Validate-RepositoryLayout.ps1` | 46 checks, 0 violations, exit 0 |

Tooling changes: `render_voices.py` parses the speaker-less Level 1 lines and reads "92%" / "60,000" as spoken in the
word check. `RideDirectorTests` ignore MCP-for-Unity bridge errors in SetUp, body and TearDown (each has its own log
scope); any other error fails the test.

Left for the owner (agents cannot do these):
- Listen to the 32 lines (line 32 "You read it instantly" is the weakest by transcription).
- Headset (Quest 2): comfort on the reverse and the drop (G = gentle), text readability, both hands, 72 fps in OVR Metrics.
- `telstar.wav` licence before Ignite.
- Delete the 15 stale model sheets listed in section 3 (the agent's delete was blocked).
- vr-qa nits not acted on: the dock's orange lever sits low-left (the GO arrow hint points at it); the 60 wall
  digits at `right` compete a little with the gold network.
