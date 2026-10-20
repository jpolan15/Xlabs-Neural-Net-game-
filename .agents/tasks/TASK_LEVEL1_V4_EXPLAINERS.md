# TASK — Level 1 v4: "Teach Pixel to read" (explainers that unfold into the level)

| Field | Value |
|---|---|
| Status | **Active.** Written 2026-10-20 from the owner's direction. Branch `jonathan/level1-explainers` (from `jonathan/polishing-level-1`). |
| Builds on | `TASK_LEVEL1_DIGITS_RESTRUCTURE.md` (the digits ride, ADR-016): same track, bake, Core puzzle, director, builder, tools. Its infrastructure rules still apply (generated scene, sightlines, seat captures, model sheets, audio levels). |
| Goal | Elegant and fun. **A 4-year-old follows it, and an adult leaves knowing how a neural network learns.** |

> Read section 1 and the section for your work package. Don't read the old handoffs. Code beats stale prose.

---

## 1. What the owner asked for (2026-10-20)

- "Make sure it's really telling the story." Use **classic examples**: the ones from the textbooks and 3Blue1Brown.
- "3D, 3Blue1Brown-type animations as explanations. Then it breaks from the animation into the level, which takes place
  *in* the animation and helps you understand further."
- "Elegant and fun." "Easy for even a 4-year-old to understand."
- **Name every topic** (pixel, neuron, weights, layer, neural network, loss, backpropagation, gradient descent,
  training, tokens), but explain each one **simply**. No "grown-ups call this…" talk, no lecture.
- They want the **whole story of AI** told across the game. Level 1 = neural networks (the base of everything).
  Its finale explains **tokens** as the bridge. Level 2 (section 9) does tokens → chatbots.

## 2. The format: every section is Explain → Unfold → Play

1. **Explain (the blackboard).** The cart stops in calm darkness. A 3B1B-style animation plays big around the rider,
   synced to the narration's `{cue}` tags: clean shapes, smooth moves, one idea at a time.
2. **Unfold.** The animation's own pieces fly apart and grow to full size, and *become* the region: the 7×7 grid
   you just watched becomes the pixel field you fly over; the weight grid becomes the puzzle on the dash; the little
   network diagram becomes the giant network you ride along; the ball-on-a-curve becomes the Valley you drop into.
   The rider must *see* the explainer object turn into the place. That continuity is the point.
3. **Play.** A short, forgiving minigame or ride moment inside what was just explained.

### Look (3Blue1Brown-inspired, our own)

| Thing | Rule |
|---|---|
| Blackboard | Near-black navy (`#0B0E14`), very faint blue coordinate grid far away, no fog colour cast. Region geometry hidden until its unfold. |
| Palette | BLUE `#58C4DD` (default strokes), YELLOW `#F4D345` (emphasis/"look here"), GREEN `#83C167` (+, yes, right), RED `#FC6255` (−, no, wrong, blame), WHITE `#ECECEC` text, GREY `#888888` secondary. **Orange stays reserved for grabbable things.** |
| Motion | Every move eases with 3B1B's `smooth` curve (sigmoid-shaped smoothstep), 0.6–1.5 s. Cascades stagger 0.03–0.1 s per item (`LaggedStart`). No linear snaps, no pops, nothing faster than 0.3 s except sparkle feedback. |
| Primitives | `Write` (text strokes in: per-character fade + rise), `Create` (lines grow from start to end), `FadeIn(shift)`, `Transform` (move/scale/recolour one object into another), `Indicate` (scale 1.15 + yellow flash, back), `Flash` (radial sparkle), `GrowFromCenter`. |
| Text | Few words, large. Topic names appear as an elegant title (`NEURON`, `WEIGHTS`, …) when the narrator says them, and stay as the region's tag. One small classic formula is OK where it's the real thing (e.g. `w₁a₁ + w₂a₂ + … + w₄₉a₄₉`); kids can ignore it. |
| Depth (VR) | Explainers are 3D, not a flat screen: content 3–8 m away, inside a ±35° cone from the seat, with a little parallax between layers. Nothing passes closer than 1.2 m to the eyes. Readable at 1.15 / 1.30 / 1.45 m eye heights. |
| Budget | Quest 2: every viewpoint ≤ 100 draw calls / 100k tris (the existing `RideSightlines` report). Instance or combine repeated items (grid cells, wires). |

### Feel (fun for a 4-year-old)

- Every touch answers within 0.1 s with sound + motion + light. Success = chime + sparkle `Flash` + Pixel celebrates.
- Nobody gets stuck: hints after 8 s idle (Pixel flies to and points at the next cell or wire), and the ride finishes the task itself after a timeout (the existing `WeightPaintMoment` auto-follow pattern).
- Green = yes/right, red = no/wrong, yellow = look here, orange = grab me. Same everywhere.

### Pixel (the buddy)

A small friendly robot drone, our own design from `ArtSource/scout_drone/concept/sheet.png` (porcelain-white rounded
body, cyan face screen with two eyes and a mouth, two side rings, two little antennae). **Pixel is the one learning:
the network is Pixel's brain.** Pixel beeps (no voice; one narrator). Floats 1.2–2 m beside the pod in view, looks at
what the narrator is talking about, and has expressions: happy, curious, confused, sad, excited/celebrate, thinking.

## 3. The story and script (owner direction 2026-10-20; locked for this build)

One narrator (Guide). Short sentences. `{cue}` opens the clause it belongs to; `[pX]` is a pause of X s. Numbers that
come from the bake (untrained guess "one", sixty thousand, ninety-two) must be checked against
`level1_digits.json` by the builder, as now. Classic example for each beat in the right column.

### E0 — Dock: "What is this?"  *(classic: 3B1B's opening, many sloppy 3s)*
| Line | Text |
|---|---|
| Threes | `{threes}Look! [p0.3] Lots of threes. [p0.3] All different. [p0.3] And you know every one.` |
| MeetPixel | `{pixel}This is Pixel. [p0.3] Pixel is a little robot, [p0.2] and Pixel can't read yet.` |
| PixelBrain | `{brain}Let's go inside Pixel's brain, [p0.2] and teach it to read threes.` |
| PullGo | `{go}Pull the orange lever. [p0.2] Let's go!` |

Explainer: a cloud of ~20 real MNIST threes (bake them) drifts around the rider; each says "3" the same way; one hero 3
glides to centre. `{pixel}` Pixel pops up and waves. `{brain}` a glowing outline of a brain/network appears inside
Pixel's head and the camera "dives" toward it (the GO launch flies into it). Unfold → the hero 3 becomes the Page.

### E1 — Pixels  *(classic: the digit on a grid, each square showing its brightness number)*
| Line | Text |
|---|---|
| PixelsSquares | `{grid}To a computer, [p0.2] a picture is tiny squares. [p0.3] They're called pixels.` |
| PixelsNumbers | `{numbers}Every pixel is just a number. [p0.3] {zero}Dark is zero. [p0.3] {one}Bright is one.` |
| PixelsFortyNine | `{shrink}We'll use seven by seven. [p0.3] {column}That's forty-nine numbers. [p0.3] It's all Pixel ever sees.` |
| PixelsFly | `{unfold}Let's fly over them!` |

Explainer: a grid draws itself over the hero 3 (`Create`), squares fill, each shows its value 0.0–1.0 (`Write`,
cascaded); `{zero}` indicates a dark one, `{one}` a bright one; `{shrink}` the 28×28 blurs/merges into 7×7 (real
downsample from the bake); `{column}` the 49 squares fly into one tall column of 49 circles (the input layer, used
again in E3). Unfold → the 7×7 grows into the pixel relief/field; pillar height = brightness; the rider flies low over it.

### E2 — One neuron  *(classic: neuron = a thing that holds a number; weights drawn as a green/red pixel pattern; weighted sum then a threshold)*
| Line | Text |
|---|---|
| NeuronHolds | `{neuron}This circle is a neuron. [p0.3] It holds one number. [p0.3] {glow}The bigger the number, [p0.2] the brighter it glows.` |
| NeuronWires | `{wires}It's wired to every pixel. [p0.3] Each wire has a weight.` |
| NeuronGreenRed | `{green}Green weights say: [p0.2] I like ink here. [p0.3] {red}Red weights say: [p0.2] no ink here, please.` |
| NeuronAdds | `{add}The neuron adds it all up. [p0.3] {line}If the total is big enough, [p0.2] it lights up!` |
| NeuronYourTurn | `{paint}Your turn! [p0.2] Make it light up for ones, [p0.2] but not for zeros. [p0.3] Tap a square to paint it.` |
| NeuronLitForZero | `Oops, it lit up for a zero. [p0.3] Zeros are round. [p0.2] Paint the round part red.` |
| NeuronMissedOne | `A one stayed dark. [p0.3] Ones are a line down the middle. [p0.2] Paint it green.` |
| NeuronSolved | `You did it! [p0.3] You taught a neuron a shape.` |

Explainer: one circle; its fill brightness animates with a number inside (0.2 → 0.9). `{wires}` 49 thin lines `Create`
from the input column to it. `{green}/{red}` the wires recolour and the weights fold back onto the 7×7 as a
green/red pattern laid over the digit (3B1B's classic picture). `{add}` products fly into a glass/bar that fills; the
formula `w₁a₁ + … + w₄₉a₄₉` writes itself small beside it; `{line}` a yellow line on the bar; the fill crosses it and
the neuron lights. Unfold → the weight pattern flies onto the dash as the puzzle grid; the neuron grows into the big
amber neuron ahead. Play = existing paint-the-weights (Core `WeightPaintPuzzle`), redone for kids (section 6, WP4).
The "TOTAL/LINE 2.0" numbers on the panel become the filling glass with the line (honest: it *is* the sum and threshold).

### E3 — The network  *(classic: 3B1B's layered diagram, input column → hidden layer → 10 outputs labelled 0–9)*
| Line | Text |
|---|---|
| NetworkTeam | `{copy}One neuron finds one shape. [p0.3] {layer}So line up lots of them. [p0.3] That's a layer.` |
| NetworkAnswers | `{answers}At the end, [p0.2] ten answer neurons, [p0.2] zero to nine.` |
| NetworkWire | `{connect}Wire them all together, [p0.2] and it's a neural network. [p0.3] This is Pixel's brain!` |
| NetworkSend | `{race}Let's send our three through. [p0.3] Watch the lights!` |

Explainer: the E2 neuron copies itself into a column of 16 (`LaggedStart`), `LAYER` title; 10 outputs labelled 0–9;
`{connect}` all edges `Create` in a cascade (real baked weights: colour by sign, opacity by size). Unfold → the diagram
scales up into the giant network the track runs along. Play/moment = the existing forward pass (real activations).

### E4 — Wrong, and the loss  *(classic: random weights give garbage; the cost = sum of squared differences from what we wanted)*
| Line | Text |
|---|---|
| WrongSays | `{guess}Pixel says... [p0.5] one?` |
| WrongRandom | `{wrong}Wrong! [p0.3] Pixel never learned. [p0.3] Its weights are random.` |
| WrongWanted | `{want}We wanted the three bright, [p0.2] and all the others dark.` |
| WrongLoss | `{oops}Add up how far off each answer is. [p0.3] That's the loss. [p0.3] A big loss means a big mistake.` |

Explainer: beside the 10 real output brightnesses, the wanted ones appear (3 = 1, rest = 0); red bars show each gap;
`{oops}` the bars stack into one tall red bar labelled `LOSS` (the real initial loss). Pixel: confused face, sad beep.

### E5 — Backpropagation  *(classic: 3B1B's "nudges" — the 3 wants to go up, the others down; that wish flows back through the weights)*
| Line | Text |
|---|---|
| BackWish | `{wish}The three wants to be brighter. [p0.3] {dim}The one wants to be dimmer.` |
| BackFlows | `{back}That wish flows backwards, [p0.2] through every wire. [p0.3] That's backpropagation.` |
| BackHot | `{hot}The wires that caused this mistake glow hot.` |
| BackGrab | `{grab}Grab the hot wires, [p0.2] and nudge them!` |
| BackDone | `Nice! [p0.3] Now every wire knows which way to change.` |

Explainer: green up-arrow on output 3, red down-arrows on the others (sizes = real gradient at the outputs); `{back}`
the arrows travel backwards along the wires to the hidden layer and on to the inputs (`LaggedStart` reverse wave);
`{hot}` wires glow by real blame magnitude. Unfold → the cart reverses along the network's axis (existing Rewind).
Play = existing blame wires, kid version: touch/point at a hot wire → it cools to blue with a sparkle; ride auto-finishes.

### E6 — Gradient descent  *(classic: a ball rolling down a curve; step size; overshoot)*
| Line | Text |
|---|---|
| DescentHill | `{hill}Think of the loss as a hill. [p0.3] {ball}High up means lots of mistakes. [p0.3] Down low means hardly any.` |
| DescentStep | `{slope}Feel which way is down, [p0.2] and take a step. [p0.3] That's gradient descent.` |
| DescentTooBig | `{bigstep}Take too big a step, [p0.2] and you fly past the bottom. [p0.3] {smallsteps}Small steps settle in.` |
| DescentRealHill | `{landscape}And now, the real hill. [p0.3] Hold on tight!` |
| DescentOvershoot | `Whoa! [p0.3] Too far!` |
| DescentSettle | `Small steps now. [p0.3] Down... [p0.3] and down... [p0.3] and down.` |

Explainer: a 2D curve `Create`s; a ball sits high; `{slope}` a tangent arrow shows downhill; the ball steps down;
`{bigstep}` a huge step overshoots up the far side; `{smallsteps}` small steps settle; `{landscape}` the curve sweeps
around into the 3D Valley (the real loss ribbon). Unfold → the Valley; the drop follows the real baked loss.

### E7 — Right, and training  *(classic: train on MNIST's 60,000; test accuracy on unseen digits)*
| Line | Text |
|---|---|
| RightSays | `{right}Same three. [p0.3] New weights. [p0.3] Pixel says... [p0.5] three! [p0.3] Yes!` |
| RightTraining | `{practice}Pixel practised on sixty thousand digits. [p0.3] That's called training.` |
| RightAccuracy | `{accuracy}Now Pixel reads ninety-two out of a hundred, [p0.2] even ones it has never seen.` |

Explainer: gold network, the 3 lights; Pixel celebrates. `{practice}` a fast stream of real digits flows through the
network with a counter climbing to 60,000 and the loss bar shrinking; `{accuracy}` a 10×10 grid of 100 test digits,
92 tick green.

### E8 — Tokens (the bridge to Level 2)  *(classic: a sentence split into tokens; each token → a vector; predict the next word)*
| Line | Text |
|---|---|
| TokensWords | `{sentence}Words work the same way. [p0.3] {tokens}We chop a sentence into pieces called tokens.` |
| TokensNumbers | `{vectors}Each token becomes a list of numbers, [p0.2] just like pixels.` |
| TokensPredict | `{predict}A giant network guesses the next word. [p0.3] {mat}That's how a chatbot talks.` |
| TokensNext | `{next}Next time, [p0.2] Pixel learns words! [p0.3] See you there.` |

Explainer: "The cat sat on the" `Write`s; `{tokens}` it splits into coloured chips `The | cat | sat | on | the`;
`{vectors}` each chip unfolds into a short column of numbers; `{predict}` they flow into a network silhouette;
`{mat}` "mat" writes itself at the end with a soft flash. Then the end card (credits as now).

## 4. Architecture (rules from `AGENTS.md` hold)

- **Explainer kit**: `Assets/Scripts/Presentation/Ride/Explainers/`: rate functions (`smooth`, `there_and_back`,
  `rush_into/from`), tween primitives (section 2) and a small timeline/sequencer that runs steps when a narration cue
  fires (`DashScreenView`/`NarrationLibrary` already publish cues; reuse that, don't invent a second clock).
  Presentation only: it displays, never decides correctness. Pure math (rate functions) in a testable static class.
- **One explainer component per beat** (E0–E8), each built by the builder (`Tools/Editor/NeuralRideBuilder.Explainers.cs`)
  from the bake where it shows data. No hand edits to `NeuralRide.unity`.
- **Stage + unfold**: during a segment's explain, the region's geometry is hidden and the atmosphere is the blackboard;
  the unfold animates explainer pieces into the region's real transforms, then reveals the region. Skip (Space / long
  GO hold) jumps every explainer to its end state instantly and correctly.
- **Pixel**: builder-made model (`NeuralRideBuilder.Pixel.cs`) + `PixelBuddyView` (Presentation) with expressions,
  look-at, follow-the-pod float, point-at-target, reactions to cues and to moment events. Gets a `RideModelSheets.Models` entry.
- **Narration**: new `RideLine` ids appended from **64** (32–63 stay reserved, never reused). `RideNarrationData` holds the
  section 3 lines; sequences/segments in `NeuralRideBuilder.Flow.cs`. Voice: Kokoro Guide (`bm_fable`, ADR-012 pipeline),
  rendered and verified with `ArtSource/voice/render_voices.py`; the owner listens later.
- **Tests**: EditMode for the rate functions and sequencer; PlayMode that each explainer reaches its end state from play and from skip, and the whole ride completes. Never delete failing tests; fix or report.

## 5. Definition of done (whole task)

- Ride plays dock → end on desktop with no errors; every explainer plays, unfolds and its play part works; skip works.
- EditMode + PlayMode green (MCP-for-Unity bridge errors ignored as today). Builder green: sightlines clear at 3 heights, every viewpoint ≤ 100 draw calls / 100k tris, model-sheet coverage.
- Seat captures + Play Mode captures of every explainer at 3 moments (start, middle, unfolded) reviewed by `vr-qa`: no blockers.
- Voice rendered for every line, WER ≤ 10 %, −16 LUFS, −1 dBTP, installed and bound.
- `Validate-CoreBoundaries.ps1` and `Validate-RepositoryLayout.ps1` exit 0. ADR-017 written; PROJECT_MAP/DECISIONS_INDEX updated.
- One commit per work package with the real test output in the message body or the progress log below.

## 6. Work packages (one subagent each, **one Unity agent at a time**)

| WP | Job | Done when |
|---|---|---|
| WP1 | **Story + kit + stage.** New lines/ids/sequences/segments for E0–E8; explainer kit + sequencer + stage/unfold mechanism + skip; E0 and E1 fully built as the quality reference. Old Level-1 text panels that the explainers replace are removed. | Ride plays end to end with new subtitles (silent clips OK); E0/E1 look right in Play Mode captures; tests green; committed. |
| WP2 | **Pixel.** Model, expressions, float/look/point, reactions for all beats (uses cues). | Model sheet reviewed; Pixel visible and reacting in captures of every region; committed. |
| WP3 | **E2 + E3** explainers and unfolds; kid-friendly paint puzzle (glass meter, hint glow by colour, Pixel points, celebration). | Puzzle solvable by a tap-happy kid and auto-finishes; captures; committed. |
| WP4 | **E4 + E5** explainers; blame wires kid version (touch → cool to blue, sparkle). | Captures; tests; committed. |
| WP5 | **E6 + E7 + E8** explainers, Valley unfold, training stream, tokens finale. | Captures; tests; committed. |
| WP6 | **Voice** (parallel, no Unity): render + verify all new lines; install at the end. | Verify table in the log. |
| WP7 | **QA**: `vr-qa` review of everything; fix blockers; final full run, validators, ADR-017, docs. | Section 5 all true. |

## 7. Comfort and honesty (unchanged from ADR-016)

Everything that glows in a network is computed from the baked weights; the puzzle is judged in `Core.Puzzles`.
Fixed cockpit frame; reverse and drop keep their limits; `G` gentle mode stays.

## 8. Progress log

(newest last; each WP adds commands run and real results)

### 2026-10-20 — WP1: story + kit + stage, E0 and E1 built (Claude)

**Built.**
- **Script and flow.** `RideLine` 64–105 for every section-3 line (1–63 reserved; the four v3 names v4 reuses are now
  `V3NeuronYourTurn`, `V3NeuronMissedOne`, `V3NeuronSolved`, `V3RightAccuracy`). `RideNarrationData` holds the
  section-3 script verbatim, intro E0 (Threes, MeetPixel, PixelBrain), dock line PullGo, 10 sequences, and
  `MomentLines`. The bake check now expects "says... one", "sixty thousand" (RightTraining) and "ninety-two out of a
  hundred". Moments rewired: WeightPaint → NeuronLitForZero/NeuronMissedOne/NeuronSolved, Blame → BackDone, Valley →
  DescentOvershoot/DescentSettle, trained pass → RightSays, RightTraining, RightAccuracy. Fourteen segments
  (`NeuralRideBuilder.Flow`; numbers = `Seg*` in `NeuralRideBuilder.Explainers`): 0 E1 explain (+2.5 s hold for the
  unfold), 1 fly over the field, 2 E2 explain, 3 your turn + paint, 4 E3 explain, 5 send + forward pass, 6 E4, 7 E5
  explain, 8 rewind + blame, 9 back to the switch, 10 E6 explain at the rim, 11 Valley, 12 trained pass (E7), 13 E8.
  Same track and legs; E1's stop is z = 16, the neuron stop moved to z = 64, the pixel field is at z 31–52.
  `RideDirector.Segment.hold` (skippable) added. Region tags are topic names in CMU Serif (PIXELS, NEURON → WEIGHTS on
  {wires}, NEURAL NETWORK, LOSS, BACKPROPAGATION, GRADIENT DESCENT, TRAINING → TOKENS on {sentence}).
- **Kit** (`Assets/Scripts/Presentation/Ride/Explainers/`): `Rate` (smooth, rush_into, rush_from, there_and_back,
  lingering, Lagged; manim's definitions), `Tween`/`Timeline` (game clock), `Anim` (Write, Create, FadeIn/FadeOut with
  shift, FadeTo, TransformShape, MoveTo, Indicate, Flash, GrowFromCenter, GrowUp, FadeRenderers, Tint, FadeText,
  LaggedStart/Each), `ShapeBatch` + `Convergence/ExplainerFlat` shader (many anti-aliased rounded shapes/lines/pictures
  in one draw), and `Explainer` (steps keyed to cues of its own lines via `DashScreenView.SegmentShown`, to a clause, or
  to `@begin`/`@arrive`; a later cue completes missed steps; skip / leaving its segments → `JumpToEnd`; `ResetToStart`).
- **Stage + unfold**: an explainer keeps its region's pieces off while it explains and holds the blackboard
  (`RegionAtmosphere.HoldBlackboard`: #0B0E14, no colour cast, faint BLUE grid `Pod/Blackboard` 7 m below); its unfold
  step plays the declared `unfolds` (Morph: piece flies into the region object's pose/scale and is replaced by it;
  Grow; GrowUp; Show) with a cascade, then releases the sky.
- **E0** (dock): 20 real MNIST test threes (new `threes` in the bake, `bake_level1.py` re-run: only `meta.date` and
  `threes` changed) in one atlas around the rider, BLUE lines to one CMU "3", the crowd steps back, the hero 3 glides
  to the centre, and on {go} it flies out, darkens to ink and becomes the Page's ink while the sheet grows behind it.
  `PixelAnchor` + `StepRan` hooks for Pixel (WP2). PageView's ink kept its 7.5 m size (it was being reset to 1 m).
- **E1** (pixels): the 3 waits in the dark beyond the page, a 28×28 grid draws over it, it becomes squares, PIXELS,
  a lens lifts a real 5×5 patch with its numbers, {zero}/{one} Indicate, the 28×28 merge into the bake's real 7×7,
  49 circles fly into a column (the input layer), and on {unfold} the 7×7 lies down and grows into the pillar field
  (`Region_1/PixelField`, 7 row meshes, height = brightness) the ride flies over. Old 28×28 wall, "49 NUMBERS" wall and
  the "the cat sat on the mat" teaser removed.
- **E2–E8**: one component each with its stage and unfold cue (E2 {paint}: Neuron + Panel grow; E3 {race}: Network
  grows; E5 {back}: Blame shows; E6 {landscape}: Ground rises, Deck/Sign/Sun grow; E4/E7/E8 no stage yet).
- Font: CMU Serif SDF + Italic SDF (static 512² atlases, LiberationSans fallback for ₁ and ≈; CMU has neither, so
  write `w<sub>1</sub>`), credit added to `RideNarrationData.Credits`.
- Tools: `RideLiveShot.ShootWhen/AutoPilot/Status` (plans shots by explainer step, survive the Play Mode reload);
  `RideBudget` counts an explainer's pieces only in its own segments; sightlines ignore ExplainerFlat overlays.

**Results.**
- Builder (`NeuralRideBuilder.Build()`): "Narration: 42 lines, 233 s spoken in all; intro 18 s. Voice clips bound: 0 of
  42." RideSightlines: "Clear at 1.15, 1.30 and 1.45 m for every stop; every panel is inside the viewing cone and the
  text is readable." RideBudget (final build): dock 41 draws / 3,656 tris; pixels 75 / 5,608; pixel_flyover 66 /
  6,250; neuron 63 / 6,060; network 55 / 16,782; wrong 57 / 16,812; blame 57 / 30,082; rim 63 / 23,702;
  valley_mid_drop and valley_floor 61 / 30,804; right 61 / 30,804; end 57 / 24,326 (all within budget).
- EditMode (MCP run_tests): 129 total, 128 passed, 0 failed, 1 skipped:
  `EveryLevel1Line_HasAVoiceClipMatchingTheScript` is `[Ignore]`d until WP6 installs the v4 voice (stated in the test).
- PlayMode (all): 26 total, 26 passed (RideDirectorTests 13 incl. new Explain_StopsInTheDark_ThenUnfoldsThePixels
  IntoTheField, SkippingTheIntro_JumpsTheDockExplainerToThePage, Ride_RunsDockToEnd_WhenTheOperatorSkipsEverything;
  the end-to-end test now also checks every explainer finished).
- `Validate-RepositoryLayout.ps1` exit 0 (46 passed, 0 violations); `Validate-CoreBoundaries.ps1` exit 0 (Core untouched).
- Captures: Play Mode `Documentation/Design/captures/level1_v4/` (e0_1…e0_5, e1_1…e1_11, eye 1.15/1.45 variants);
  seat sheets re-shot in `Documentation/Design/captures/level1/` (old grid/grid_approach/finale_words removed).
  vr-qa review of the first set (blockers: dock sky popping to neutral grey with a brown desk, threes clipped beyond
  ±35°, GO lever out of view): fixed the first two (dock sky is navy, desk dark, threes inside ±42°) and its
  should-fixes for the {zero} highlight, the merge finishing late, the dash tag size and blown-out pillars; captures
  re-shot and checked after the fixes. Tests above re-run on the final build (same results).

**Open.**
- Voice for lines 64–105 (WP6); until then silent reading time.
- Pixel (WP2) is not there: E0's {pixel}/{brain} steps only fire `StepRan`; `Region_0/Explainer_E0/PixelAnchor`.
- E2–E8 explain on an empty blackboard (WP3–WP5). The neuron stop's old panel and the end card still have the v3 look.
- GO lever sits low-left on the dash; at a level gaze it is outside a 70° capture when "Pull the orange lever" is
  said (the hint arrow is on it). Worth a look in WP2/WP7.
- The 7×7 3 is soft (real 4×4 averages) and the field is seen at a low angle; it reads as a relief, not as a 3.

### 2026-10-20 — WP6: voice for lines 64–105 (Claude)

- Rendered with `render_voices.py` (Kokoro `bm_fable`, speed 0.9; `uv run --python 3.13 --with kokoro-onnx==0.6.1 ...`),
  verified with Whisper `small.en` (venv `~/.cache/archival/venv`), installed into `Assets/_Project/Audio/Voice/`.
- Verify fixes in `words()`: a literal backspace byte had broken the "a hundred" = "100" rule; US/UK "practiced";
  "back propagation" joined back to the script word.
- Three lines re-worded because the voice could not say them clearly (tested 10+ spellings each):
  PixelBrain "teach it to read threes." (was "read a three": heard "Rader 3"), BackHot "caused **this** mistake"
  (the voice dropped the -d), DescentRealHill "**And** now, the real hill." (heard "It's now").
- `verify`: 40 of 42 at ≤ 10 % WER. Two flag only in context: PixelBrain (heard "RAID 3s"; its second clause alone
  is heard "and teach it to read threes.") and WrongSays "Pixel says... one?" (the "one?" is there, louder than the
  rest; transcribed alone it is "1"). Both checked by transcribing the clause by itself; owner to confirm by ear.
- Builder: "Narration: 42 lines, 242 s spoken in all; intro 18 s. Voice clips bound: 42 of 42." Sightlines clear.
- `EveryLevel1Line_HasAVoiceClipMatchingTheScript` un-ignored. EditMode (MCP run_tests): 129 total, 129 passed.
- The warmer second voice for comparison was not rendered (the earlier job left none); bm_fable stays.

## 9. Level 2 outline (later, not this task): "Pixel learns words"

Tokens (chopping text) → embeddings (each token is a point; similar meanings sit close: classic king − man + woman ≈ queen)
→ attention (words look at each other to decide meaning: "bank" by a river vs. money) → predict the next token
(the network outputs a probability for every token) → training on huge amounts of text → a chatbot is that, repeated
one token at a time. Same Explain → Unfold → Play format, same Pixel.
