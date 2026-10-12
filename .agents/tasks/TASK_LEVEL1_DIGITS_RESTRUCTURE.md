# TASK — Level 1 restructure: "How a machine learns to read a digit"

| Field | Value |
|---|---|
| Status | **Active.** Written 2026-10-12 from an alignment interview with the project owner. |
| Owner | Project owner (Jonathan) + Claude. Solo build. |
| Deadline 1 | **2026-10-20: teacher demo.** A fully presentable Level 1 he can put on in the headset and look at everything. |
| Deadline 2 | **April 2027: Ignite** (public live demo). Level 1 polished, human voices, later levels as stretch. |
| Supersedes | The *content* of `TASK_IGNITE_RIDE_V3_MASTER_PLAN.md` (JFK/Apollo/AURA story, 1943/1958/1960 stops). Its *infrastructure* rules still apply: generated scene via the builder, sightline validator, seat-view captures, audio levels, Asset Forge gates. |

> Read this whole file before touching the ride. If you want to do something differently, write the reason here and ask.
>
> **Executing agent:** the step-by-step build to a finished Level 1 (work packages, architecture, tooling, draft
> script, definition of done) is in `.agents/tasks/HANDOFF_LEVEL1_FULL_BUILD.md`.

---

## 1. Why we are restructuring (what the owner said)

- "I saw we were talking about JFK, I was so confused." The ride stacked four stories (Moon mission, broken ship AI,
  a 1943→1960 history timeline, a rock-zapping game) on top of one idea. A stranger met a president, a ship AI and four
  scientists before the lesson.
- "I don't really feel like I'm learning much." The ride stopped at one neuron. It never showed hidden layers, the
  network being wrong, backprop or real gradient descent.
- "Pull a lever" is not a puzzle. Each interactive part should make you feel you are figuring something out.
- "Avoid AI slop. Every section needs its own unique design so you can tell you passed a section."
- "I want it to be the star of the show at Ignite."

## 2. Decisions (locked 2026-10-12)

| Topic | Decision |
|---|---|
| Takeaway | Zero to the idea: one neuron → a network (input, hidden weights, output) → it's wrong at first → backprop → gradient descent → it's right. |
| Long arc | **Level 1 = handwritten digits (MNIST).** Later levels build up to LLMs (same machinery, reading words). Level 1 ends with that teaser. |
| Shape | **One continuous ride.** No chopped levels or title-card breaks inside Level 1. Sections blend like biomes, and each has its own look + sound so you know you crossed into a new one. |
| Frame | **"Be the signal."** No spaceship, no AURA, no character. You ride *inside* the network, and the world is the network. |
| Inputs | Real-AI data: handwritten digit images, shown honestly as pixel brightness numbers ("how info goes in"). |
| Teaching pattern | Every section: **explain it first, then a minigame that matches the explanation.** |
| Gradient descent | **The game controls the drop**, not the player. It's a real coaster drop down the error hill. |
| History | Light touch, modern-focused: a small date tag at most, no narration about history. **JFK, Apollo audio and AURA are cut.** |
| Audience | General public, any age, some never in VR. Zero jargon without an immediate picture. |
| Voice | AI narrator (Kokoro "Guide") now, for drafting and the teacher demo. Record humans for Ignite once the script is locked. One narrator. |
| Length | Unknown for Ignite; target **~5–6 min** for Level 1 and keep a skip. |
| Team | One person + Claude. Plans must fit that. |

## 3. The Level 1 journey

One continuous track. Each row is a region with its own palette, lighting, music layer and motion.

| # | Region (look) | Explain (narrator, short) | Minigame / moment |
|---|---|---|---|
| 0 | **The Page**: warm paper, ink, daylight | "You can read this number instantly. A computer can't. All it gets is numbers." A big handwritten **3** lifts off the page and breaks into a grid. | Pull GO to dive into the image. |
| 1 | **The Grid**: black void, glowing white pixel pillars | "Every pixel becomes one number: dark is 0, ink is 1. This 3 is just a list of brightness numbers." | Ride low over the pixel field; pillar height = brightness. (Watch, ~20 s.) |
| 2 | **The Neuron**: one huge amber neuron, warm glow | "A neuron looks at the pixels. Each connection has a weight: **+** means *I want ink here*, **−** means *I don't*. It adds it all up. Past the line? It fires." | **Paint the weights.** A 7×7 weight grid. Goal: fire for **1**s, not for **0**s. Test digits roll through. Each pass/fail shows *why* (which painted cells summed to what). |
| 3 | **The Network**: zoom out, vast cool-blue layered web | "One neuron can spot one pattern. Wire many together in layers: pixels in, a hidden layer in the middle, ten answers out, 0 to 9." | Send the 3 through: light races layer to layer. Rider sees hidden neurons light up. |
| 4 | **Wrong**: the network flickers red | "It guessed… 8. It's never been taught. Its weights are random." | Short beat, a dramatic fail. |
| 5 | **Rewind**: magenta/red, the cart **reverses** | "So we go backwards. The error flows back through every wire. The more a wire caused the mistake, the brighter it burns. That's **backpropagation**." | **Find the blame:** point at the wires burning brightest and they get nudged. (Cut line: watch-only.) |
| 6 | **The Valley**: terrain of the error landscape, sunset | "All that blame tells us which way is downhill. Lower = less wrong. That's **gradient descent**." | **Automatic coaster drop.** First step too big: overshoot up the far slope. Then steps settle into the valley. |
| 7 | **Right**: back in the network, now steady gold | Forward again: "…3." Zoom out: "Do this with 60,000 digits and it reads handwriting. Do it with words, and billions of weights…" | Teaser for the LLM level: the digits dissolve into words. End card. |

Honesty rules (it's an educational game):
- The network in regions 3–7 is a **real tiny network trained offline on real MNIST**, for example 7×7 downsampled
  input → 8–16 hidden → 10 outputs. Weights are baked as data, and the "wrong" pass uses the untrained initial weights.
  No faked lights: what glows is what the math computes.
- The neuron puzzle is evaluated in `Core.Puzzles` (Unity-free), never in Presentation.

## 4. What we keep, replace, cut

| Keep (infrastructure) | Replace (content) | Cut |
|---|---|---|
| `NeuralRideBuilder*` generating the scene; `RideDirector` + `RideScript` pacing; `NarrationLibrary` + `render_voices.py` pipeline; `PodSeat` eye-height; levers/XRI; `RideSightlines`, `RideSeatCaptures`, `RideLiveShot`; `LossLandscapeView` (as the base for the Valley); `NetworkDiagramView`/`NeuralCoreView` (as a base for the network); music mixer + level checks | Script (`RideNarrationData.cs`); stops 1–3 (trigger / OR-gate pipes / learning-rate lever) → regions 0–7 above; chapter cards → small region tags | JFK + Apollo clips, AURA voice and glitch filter, rock/drone/ice story, 1943/1958/1960 narration |

## 5. One-week schedule (to 2026-10-20)

Every day ends with a build, a seat-view capture, model sheets of every model that is new or changed (section 6) and a
note in this file. **Cut lines** say what to drop if a day slips.

| Day | Work | Done when |
|---|---|---|
| Mon 10-13 | Owner approves the script (Claude drafts it, owner edits). Bake the tiny MNIST net + sample digits (offline Python → data asset). Greybox the whole track with 8 regions in the builder, with old stops removed. | Full ride rideable end to end in grey, with placeholder text. |
| Tue 10-14 | Region 2: the paint-the-weights puzzle (Core evaluator + tests, XRI poke grid, test digits rolling through with the why-readout). | Puzzle solvable in the Simulator; EditMode tests green. |
| Wed 10-15 | Regions 3–4: real forward pass visual + the wrong guess. Regions 0–1: page → pixel grid. | Same digit gives the same lights as the baked math. |
| Thu 10-16 | Regions 5–6: reverse ride + blame wires; Valley drop with overshoot. | Drop follows the real loss curve of the baked training run. |
| Fri 10-17 | Each region's look (palette, fog, lighting, music layer); render the AI narration; region 7 finale. | Captures of all 8 regions look clearly different. Every region's model sheets reviewed, nothing on the "fix" list left. |
| Sat 10-18 | Headset pass: sightlines at 1.15/1.30/1.45 m, `vr-qa` review, comfort check on the reverse + drop. Fix. | `vr-qa` lists no blockers. |
| Sun 10-19 | Buffer + full rehearsal in the headset, operator skip works. | Owner rides it twice without help. |
| Mon 10-20 | **Teacher demo.** | |

Cut lines, in order: (1) backprop "find the blame" becomes watch-only; (2) region 0 Page merges into region 1;
(3) region looks become palette + fog only; (4) the finale teaser becomes a single end card.

**Comfort:** a reversing cart and a coaster drop are the top motion-sickness risks for first-time VR users. Keep a
fixed cockpit frame in view, limit drop acceleration, and have a "gentle" setting ready for the demo.

## 6. Model sheets: every model, from every side

Owner's ask (2026-10-12): every model the ride uses is rendered from different angles, so the scene can look amazing
from anywhere, not only from the seat. The ride is going on video, and a camera (or a rider turning around) sees sides
the seat captures never show.

- **Tool:** `Convergence/Capture Model Sheets` (`Tools/Editor/RideModelSheets.cs`). One PNG per model in
  `Documentation/Design/captures/model_sheets/`, 12 tiles: 8 views around it (front, every 45°), top, a low hero 3/4,
  the rider's eye at 1.30 m, and the model in the world with the ride's fog. The first 10 show the model alone on the
  ride background; the header gives triangles, renderers, materials and size. Single tiles go in `<id>/`, a table in
  `index.md`. It renders copies in preview scenes, so the open scene is never changed.
  CLI: `unity command eval "return Convergence.EditorTools.RideModelSheets.Capture();"` (or pass ids: `"pod,network"`).
- **Edit mode vs Play Mode:** edit mode shows the saved scene. Geometry a view makes only at run time (today: the loss
  landscape surface, the pipes) shows as saved, so build region geometry **in the builder** from the baked data
  wherever possible. For anything that only exists at run time, call `Capture` in Play Mode at that moment (the header
  says PLAY MODE).
- **Coverage is enforced:** every renderable child of a ride root (`Pod`, `Track`, `Station_N`, and the coming
  `Region_N`) must be in `RideModelSheets.Models` (or `NotModels` with a reason). The builder calls `AssertCoverage()`,
  so a new set piece without a sheet fails the build. When a region is built, its set pieces get entries in the same
  change.
- **Review:** after a sheet changes, look at every tile and list what is wrong from any side: holes, flat backs,
  mirrored text, parts floating loose, glow that hides the shape, look-alike regions. The `vr-qa` subagent does the
  second read on Fri and Sat.
- **Baseline (2026-10-12, old v3 stops, 16 sheets):** what the sheets already show, to avoid in the new regions:
  1. Labels and panels are flat with no back: from behind they read mirrored (`neuron_core`, `finale`). Fine for
     a panel only the seat sees; not for anything a camera can circle.
  2. The loss landscape is a one-sided placeholder plane in the saved scene and vanishes from behind
     (`loss_landscape`); the Valley must be real builder geometry, closed on all sides the camera can see.
  3. From outside, the big network is a thin trail of specks with one huge orb at the end (`network_lit`). It only
     reads from inside. Region 3 should read as layers from a distance too, for the video wide shots.
  4. The pod and the test objects hold up from all sides (`pod`, `drone`, `comet`).

## 7. Asset Forge

Paused for the teacher demo. Regions are built from shaders/procedural geometry first (faster, no slop risk). After
10-20, pick at most one hero asset per region and run it through Gates A–C.

## 8. Open questions (ask the owner, don't guess)

- What exactly does the teacher grade or look for on 10-20?
- Is a Quest 2 available for daily testing this week, or only on Sat?
- Music per region: which tracks (license must allow a public demo)?
- Ignite slot length and how many riders: decide the skip/length later.

## 9. Progress log

### 2026-10-12 — WP1 gates (owner answers)

- Script (handoff section 9): **approved as drafted**. Numbers come from the bake.
- Commits: owner delegated ("whatever you need to make the best version"): one commit per work package, no pushes.
- Music: **keep `telstar.wav`** (owner's choice). Licence risk for the public Ignite demo stays open: if it is the 1962
  Tornados recording, it needs written permission or a CC0 swap before April 2027.
- MNIST download: approved. Headset days and the teacher's criteria: not asked yet (owner prefers fewer questions);
  ask before WP12.

### 2026-10-12 — WP2 data bake

`uv run --with numpy python -I ArtSource/training/bake_level1.py --out Assets/_Project/Data/Level1/level1_digits.json`
(MNIST MD5s verified; 68 KB JSON):

| check | value |
|---|---|
| test accuracy (10,000 unseen digits) | **92.50 %** (narration: "92 percent", rounded down) |
| training | 3001 full-batch gradient-descent steps on all **60,000** training digits; lr 10 for step 1, then 2.0 |
| loss: start / after step 1 / end | 2.5316 / **3.5813 (overshoot +1.05)** / 0.2673 |
| hero 3 | test #1845; untrained guess **1** (p 0.29); trained p(3) 0.9998 |
| puzzle | line 2.0; empty 4/8, all-plus 4/8, reference 8/8 (margin 0.66); hints solve from empty in 6 moves |
| valley | ribbon 193 × 9 of real loss; centre = training loss exactly (gap 0); walls rise +0.04 … +0.44 |
| big first step | 2.532 → dips to 2.325 → climbs to 3.581 (the bottom it flew past is real) |
| finale | 55/60 of the sampled test digits right |
| hops (step:real steps:loss) | 0:0:2.532; 1:1:3.581; 3:2:2.785; 8:5:2.343; 20:12:2.139; 50:30:1.603; 120:70:0.819; 250:130:0.540; 500:250:0.440; 900:400:0.378; 1500:600:0.325; 2200:700:0.291; 3001:801:0.267 |

Script values: `{WRONG_GUESS}` = **one**, `{TRAIN_COUNT}` = **60,000**, `{TEST_ACCURACY}` = **92**.

**Deviation from the handoff (Valley):** the handoff asked for a PCA plane through the trained weights. Measured on
this run it puts the start 0.82 and the overshoot 1.90 loss units *below* their real loss (PCA explains 98.5 % of the
weight motion, but the loss is steep in the 1.5 % it drops). A plane through start, step 1 and the end is exact at
those three points but still 0.85 off mid-path. So the Valley is a **ribbon**: each row is a real point on the
training path (straight along the one big step, then the recorded steps placed by distance travelled), each column
steps sideways along one fixed random direction. The track (middle column) is exactly the real training loss, the
walls are real losses beside it, and the cart can sit on the terrain without any gap.

### 2026-10-12 — WP3 Core

- `Core.Math.LossFunctions.CategoricalCrossEntropy` (+ gradient), `Core.Neural.NetworkModel.CreateDense` (flat
  row-major weights, layer-wide softmax), `Core.Puzzles.WeightPaintPuzzle` (evaluate, per-cell contribution, first
  wrong digit, hint computed from the evaluator).
- `Gameplay/Ride/Level1/Level1Data.cs`: JsonUtility DTO → Core models, shape checks.
- Tests: `WeightPaintPuzzleTests` (6), `CategoricalCrossEntropyTests` (7), `Level1BakeTests` (8: golden activations
  ≤ 1e-6 for both weight sets, wrong guess / right 3, finale predictions, reference/empty/all-plus, hints from empty
  and from 25 random states, blame = finite-difference gradient and stable, valley hops on the ribbon centre).
- EditMode **110/110** passed; `Validate-CoreBoundaries.ps1`: 17 files, 0 violations, exit 0.
