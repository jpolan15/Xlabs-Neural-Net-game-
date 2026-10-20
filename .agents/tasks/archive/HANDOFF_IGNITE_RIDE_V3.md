# HANDOFF — Ride v3 "Ignite" (state at 2026-10-09, end of the build session)

> **Superseded in part by [`HANDOFF_IGNITE_RIDE_V3_PART2.md`](HANDOFF_IGNITE_RIDE_V3_PART2.md)** (voices, sound effects, the explainer panel, the speech hold; and the owner's newest priorities). Read part 2 first; this file still holds the part-1 build notes and traps.

For any agent picking this up. Plan of record: [`TASK_IGNITE_RIDE_V3_MASTER_PLAN.md`](TASK_IGNITE_RIDE_V3_MASTER_PLAN.md) (its section 10 checklist is ticked to match this file). Decisions: ADR-011 (ride), ADR-012 (voices, audio, Asset Forge, **Proposed**), ADR-013 (rendering, **Accepted**).

**Evidence rule used here.** *Verified* = observed in Unity this session (test results, console, or a screenshot that was looked at). *Written* = exists and compiles but nothing has exercised it. Nothing below is claimed beyond that.

---

## 0. State in ten lines

- Everything is **uncommitted** in the working tree on `main` (about 80 changed or new files). The human decides whether and how to commit.
- **Verified:** the project compiles; `Convergence > Build Neural Ride` runs and its checks pass (audio level, sightlines, budget report); the **whole ride plays end to end** in Play Mode through the Unity CLI (intro, skip, launch, briefing 1, stop 1, 2, 3, outro, finale); **EditMode 85/85** and **PlayMode ride tests 7/7** pass; both repository validators pass.
- **Not verified:** anything on a headset or a Quest build (shaders on Vulkan/multiview, `PodSeat` eye-height lift, real frame rate and draw calls), any audio (there is none yet), the telstar.wav license.
- **The ride runs on subtitles and music only.** The old robotic recordings are deliberately not bound (their text differs from the approved script). Two old clips (lines 8 and 11) still match and will play in the old voice until replaced.
- **What the rider now sees:** no posts or canopy (a low cockpit tub), panels placed by angle inside the viewing cone, a 7-layer glowing neural network to fly through, chapter cards, clause-by-clause subtitles with a speaker tag, a finale orb with portal rings.
- **Editor-measured cost:** 72 / 103 / 77 draw calls at stops 1 / 2 / 3 (budget 100), about 14k triangles, 22 to 27 SetPass calls. Stop 2 is at the limit; see task C1.
- **UnityMCP was down all session** (ECONNREFUSED). Everything was driven through the **Unity CLI** (`unity command ...`, package `com.unity.pipeline`), which works with the Editor open. See section 8.
- **`McpAutoConnect` now has an off switch** (`EditorPrefs` key `Convergence.McpAutoConnect.Disabled`). It was set during testing and **reset at the end**. If PlayMode tests fail with "Unhandled log message ... MCP-FOR-UNITY ... WebSocket Connection failed", set it (see section 8).
- The **Haiku subagents' work was audited** (section 7). Two of their files were replaced or fixed; the rest held up.
- Open for a human: voice pick and recording, music license, headset run, Quest measurement, the Asset Forge pilot.

---

## 1. How to resume

1. Read `AGENTS.md`, `.agents/PROJECT_MAP.md`, `.agents/WORKFLOW.md`, the master plan sections 0, 4 and 8, then this file.
2. Layer rules hold: Core is Unity-free; Gameplay never touches AudioClips or materials; Presentation observes and never decides correctness; XR converts input to commands; `Tools/` is dev-only. Raw art goes in `ArtSource/`, never `Assets/`.
3. **Every scene and prefab change goes through `Tools/Editor/NeuralRideBuilder*.cs` and a rebuild.** Hand edits are overwritten.
4. Loop (all through the Unity CLI, from the repo root; add `--format json --no-banner` when scripting):
   - compile: `unity command recompile`, then poll `unity command recompile_status` until `completed` (read `errors`)
   - build: `unity command menu -- --path "Convergence/Build Neural Ride"`, then read the console (`unity command console -- --tail 80 --level log`)
   - look: enter Play Mode (`unity command editor_play`), then render from the rider's eye with `Convergence.EditorTools.RideLiveShot.Shoot(path, eye, fov, yaw, pitch)` via `unity command eval`; or edit-mode seat views via the menu `Convergence/Capture Ride Seat Views`
   - test: `unity command run_tests -- --mode EditMode --filter RideNarrationTests`; PlayMode with `--async_tests true` then `unity command test_status` (results also land in `Temp/pipeline_test_status.json`)
5. `pwsh` is not installed: run validators with `powershell.exe -NoProfile -File Tools/Validation/<script>.ps1`. ffmpeg and Blender are not installed. Python 3.13 exists; Pillow does not.

---

## 2. What was built, and where

### Gameplay (`Assets/Scripts/Gameplay/Ride/`)
- `RideTypes.cs`: `RideLine` ids 1 to 15 unchanged, **appended** 16 to 22, 24 to 29 (**23 is reserved, unused**; a test guards the numbering); `RideState.Intro` appended.
- `RideScript.cs` (ScriptableObject): `intro`, `briefings[3]`, `outro`, each a list of `{line, seconds}`. Numbers only.
- `RideDirector.cs`: `Intro` state (GO is a hold-to-skip handle: hold 2 s), briefings play **during** each travel and the travel lasts at least as long (+1.5 s); the station does not begin until the briefing ends; outro after the last stop; `SkipNarration()`; smooth speed-up of time if a briefing is skipped mid-leg. Events: `Narrated`, `NarrationSkipped`, `BriefingStarted/Ended`, `OutroStarted`; also `IsNarrating`, `Progress01`. With no `RideScript` it behaves as the plain v2 ride.

### Presentation (`Assets/Scripts/Presentation/Ride/`)
- `NarrationLibrary.cs` (ScriptableObject): per line, speaker, tag, clip, pause, reading time and **subtitle segments** (one clause at a time); plus the four chapter cards.
- `DashScreenView.cs` (rewritten): plays scripted lines at once, queues station hints as before, shows `GUIDE` / `AURA` / `J.F.K., 1962` tags in speaker colours. **Bug fixed this session:** it used to clear its text in `Start()`, wiping the first intro line.
- `ChapterCardView.cs`: card pops in 2.8 s after a briefing starts (so it never overlaps the previous station's reveal) and out when it ends.
- `NeuralCoreView.cs`: drives the network shaders (light reach, ignite, flow clock, **focus**: the network steps back to `RideTheme.stopFocus` while a stop is live, flares for 2.5 s on a solve). `startLit` 0.22 so the intro has lit connections to point at.
- `NetworkDiagramView.cs`: weight label is now a small "weight" caption over a big number (it used to wrap onto the lines). `RideTheme.cs`: `aura`, `violet`, network and focus settings.

### XR (`Assets/Scripts/XR/Ride/`)
- `DesktopSeat.cs`: operator keys work with or without a headset: **Space = skip narration, R = restart the ride**.
- `PodSeat.cs`: lifts the rig so the rider's eye sits at 1.30 m, once tracking is live and on every tracking-origin update. **Written, unverified on a device.**

### Shaders (`Assets/Shaders/`, hand-written URP HLSL; ADR-013)
`HoloLit` (solid parts: view-space key light + Fresnel rim, opaque, SRP-batcher friendly), `NeuralLinks` (all connections, camera-facing ribbons built in the vertex shader, clamped to 1.6 to 4 px so far lines fade instead of shimmering, pulses racing forward), `NeuralNodes` (neurons, dust, haze, the finale orb as billboards). All three compile with no messages. Not yet run on a Quest.

### Builder and tools (`Tools/Editor/`)
- `NeuralRideBuilder.cs` (pod, dash, station, scene, materials, assertions) + partials `.Pod.cs` (low tub mesh, `Polar`/`Face` layout helpers), `.Narration.cs` (writes RideScript and NarrationLibrary; binds a voice clip **only if its `.txt` sidecar text matches the script**), `.Network.cs` (7 layers, 54 neurons, real baked forward pass, finale orb, haze, dust; 2 draw calls). **Meshes are recreated every build** (see section 5).
- `RideNarrationData.cs`: **the script text**, pauses, sequences and chapter cards (single source of truth).
- `RideSightlines.cs`: triangle-accurate sightline check from 1.15 / 1.30 / 1.45 m at every stop, only the live groups, plus the viewing-cone and text-size checks. **Fails the build** on a real problem (`ThrowOnProblems = true`). Also `RideBudget` (draw-call and triangle report; logs only, `ThrowOverBudget = false`).
- `RideSeatCaptures.cs` (edit-mode seat views, hides the chapter card now), `RideLiveShot.cs` (Play-mode eye render to PNG), `McpAutoConnect.cs` (off switch added).

### Layout (station-local, pod at origin, +Z forward, eye 1.30 m; all in `NeuralRideBuilder.cs`)
Core `(0, 2.4, 6.4)` scale 0.8; arch at z 3.3 with nothing above 1.12 m; diagram `Polar(-29°, +2°, 3.8 m)` scale 0.82; board `Polar(+29°, +2°, 3.8 m)` scale 1.15; equation `Polar(0°, +15°, 4.0 m)` (a transient reveal); landscape centre `(0, 1.55, 5.4)` size 3.2; chapter card `Polar(0°, +11.5°, 5 m)`; dash unchanged (exempt from the cone). Cone: panel centres within ±35° across and −20° to +22° up (edges +5° slack); text at least 1.5° (about 30 px on a Quest 2; the plan said 2°).

### Tests
- `Tests/EditMode/Ride/RideNarrationTests.cs` (9): every line has subtitle and duration; ids never renumbered; script matches library; time budgets; chapters; segment logic; ray-triangle geometry; **a mutation test** (a cube dropped in front of the diagram must be reported); the built scene is clean. EditMode asmdef now references Gameplay, Presentation, EditorTools.
- `Tests/PlayMode/Gameplay/RideDirectorTests.cs`: 4 existing tests updated (skip the intro first; scripted lines now arrive through `Narrated`) + 3 new (GO does not launch during the intro; holding GO skips; the briefing ends before the station goes live).

### Docs
ADR-012 (Proposed; stale lines fixed), **ADR-013** (new), `DECISIONS_INDEX`, `PROJECT_MAP`, `IN_PROGRESS`, `TEAM_BRIEFING` (new "Ride v3" section), `STORY_OF_AI.md`, the master-plan checklist, and `ArtSource/voice_bakeoff/` (README with render steps, `key.txt` blind map **keep it from the listener**, `VOICE_SPEC.md` template).

---

## 3. Verified facts and numbers

- Narration: intro 77 s; briefings 31 / 26 / 21 s; outro 29 s (estimated at 2.4 words per second plus the marked pauses; real clips replace the estimate). Briefing 1 is longer than the plan's 15 to 25 s because the approved line is 62 words.
- Network: 7 layers, 54 neurons, about 3,400 ribbon points, 667 billboards, 2 draw calls.
- Editor stats at the three stops: draws 72 / 103 / 77; SetPass 22 / 27 / 27; triangles about 14k (these include Editor overhead; measure on a headset).
- Sightline check: clear at all three eye heights for every stop, every panel in the cone, all text at least 1.5°.
- Seen in screenshots: no poles over any panel at 1.15 / 1.30 / 1.45 m; subtitle and speaker tag on the dash from the very first line; card year and finale text bright (the old dimness was a transparent-sorting bug, fixed with `_QueueOffset`); a finale orb, portal rings and converging beams behind "YOU TAUGHT THE AI!".

---

## 4. What is NOT done (in the order to do it)

**A. Needs the human**
1. **Voices.** Install ffmpeg (`winget install Gyan.FFmpeg`), render the bake-off per `ArtSource/voice_bakeoff/README.md` (blind A to D), pick, fill `VOICE_SPEC.md`; then generate all 28 lines **one sentence per call**, assembled with the marked pauses, −16 LUFS / −1 dBTP / mono / 48 kHz, named `vo_ride_NN.wav` with a `.txt` sidecar holding the **exact** text from `RideNarrationData.cs`. Rebuild: the log line `Voice clips bound: N of 28` should reach 28. Archival lines 16 (JFK, Rice 1962, JFK Library `JFKWHA-127-002`, public domain per the library) and 24 (NASA Apollo 11 liftoff, credit NASA) bind by filename and skip the text check. Replace line 24's placeholder subtitle `[ Apollo 11 liftoff ]` with the real words and set its duration. Delete or regenerate old `vo_ride_08` and `vo_ride_11`. List every source and license on the credits screen; then mark ADR-012 Accepted.
2. **Music.** Confirm the `telstar.wav` license in writing (it may be the 1962 Tornados recording) or swap in a CC0 track. Check voice over music at 50% Quest volume.
3. **Headset run (WP8):** Link play-through (grab every lever with either hand, nothing blocked standing or seated, `PodSeat` really puts the eye at 1.30 m, recenter works); a standalone Android build measured with OVR Metrics at **72 fps**; a fresh-classmate test; the desktop fallback as the backup.
4. Approve (or change) the script wording with the teacher; say whether and how to commit.

**B. Agent work, after the human items**
1. **Quest shader check:** a Development Android build (Vulkan, multiview). Look for pink materials, billboard problems per eye, fog on the additive shaders, the `unity_MatrixV` billboard maths. If anything fails, fix in `Assets/Shaders/`, rebuild, re-shoot.
2. **Draw calls with margin (task C1):** stop 2 is at 103 in the Editor. Cheap wins left: combine the drone/rock/chunk parts into one mesh per item (they have 3 to 10 renderers each), one chevron pair per hint arrow instead of two, merge board tile parts, hide the dash's unused levers' parts; then flip `RideBudget.ThrowOverBudget = true`. Measure with the Frame Debugger and OVR Metrics, not only `UnityStats`.
3. **Mixer:** `RideMixer.mixer` (Music / Voice / SFX) cannot be created from script; make it by hand in the Editor and route the three sources if wanted.
4. **Stretch from the plan not done:** a live Core `NetworkModel` forward pass (the network's activations are baked at build time), an SDF font with Greek glyphs, a particle starfield (dust lives in the network mesh), an AURA glitch filter (an audio-production step).
5. **Asset Forge pilot (WP7):** untouched. Run `.agents/skills/asset-forge/SKILL.md` on the repair drone (concept art → Gate A → mesh → Unity → Gate B IoU ≥ 0.80 per view → seat capture → Gate C with the human); stop after two failed attempts. Needs Blender. Generated meshes are never interactable and never orange.
6. Tidy: `Documentation/Design/captures/ride_v3/` is the current set of ten edit-mode views; dock-time hint arrows show in them (they are hidden at runtime).

---

## 5. Traps found this session (read before changing things)

1. **Stale mesh in the Editor.** Updating a loaded mesh asset in place (`EditorUtility.CopySerialized`) left the Editor drawing the old GPU data, so the finale orb and haze never appeared (unloading the asset fixed it). `SaveMesh` now deletes and recreates. Keep it that way.
2. **Transparent sorting.** Transparent objects sort by the distance of their centres, so a wide plate dims text and icons off its centre line. Plates and glows use `_QueueOffset` (URP Unlit takes its queue from it, **not** from `renderQueue`): plate −10, glows −20, core shell −5.
3. **`Start()` order.** The director starts the intro in its own `Start()`; any view that clears state in its `Start()` can wipe the first line. Clear in `OnEnable()`.
4. **`\\n` in a bash heredoc turns into a real newline** inside a generated C# string and breaks the compile. Write the patch script to a file with the Write tool, or edit with the Edit tool.
5. **The Editor can look fine while compilation is failing** (it keeps the last good assemblies and the console may be empty). Read `recompile_status` for the real errors.
6. **Unity's test runner fails any test during which an error is logged.** The dev-only MCP bridge logs a WebSocket error every few seconds when no MCP server runs. Set `EditorPrefs.SetBool("Convergence.McpAutoConnect.Disabled", true)` and call the bridge's `StopAsync` (reflection on `MCPForUnity.Editor.Services.MCPServiceLocator.Bridge`) before running PlayMode tests without MCP, then delete the key.
7. **`capture_game_view` saves under `Assets/` when given a path** (it then creates `.meta` files); use the inline base64 result or `RideLiveShot`. `UnityStats.batches` does not exist; use `drawCalls`, `setPassCalls`, `triangles`.
8. **The `RenderTexture` for screenshots must be plain ARGB32** in this Gamma-colour-space project (an sRGB format came out far too dark).
9. **The active render pipeline asset is a desktop sample** (`PackageSamplesURPAsset`: depth and opaque textures, HDR, 2x MSAA, main-light shadows on). The ride camera overrides depth/opaque/HDR/post-processing per camera. Quest MSAA is 2x; 4x would need a mobile asset (an ADR and a decision for the human).
10. **Orange means grabbable.** Nothing else is orange; generated assets never are.
11. A generated `Assets/Temp/` folder appears if a capture command is given a path; delete it with its `.meta`.

---

## 6. Deviations from the plan, and why (do not "fix" these back)

- Shaders are hand-written HLSL, not Shader Graph (authorable as text; ADR-013).
- Text floor is 1.5° (the dash subtitle at arm's length is 1.6°; everything else is at least 1.8°). The plan said 2°.
- The pod's overhead is a procedural low tub (two meshes), not four posts and a rim.
- The network's activations are a real forward pass **baked at build time**, not live.
- No `RideMixer.mixer` (no scripting API).
- Briefing 1 is 31 s (the approved text is long). The whole narration is about 3 minutes; the test caps it at 4.
- The equation panel is a transient reveal that may sit in front of the core for a few seconds; the check treats it as a target but not a blocker.
- Up to 3 Gate B mesh retries per attempt, two attempts maximum, is how the plan's two statements are reconciled.

---

## 7. Audit of the earlier (subagent) work

| Piece | Verdict |
|---|---|
| WP1 music change and builder assertion (Haiku) | Correct as written; verified (0.14 / 0.05 in the scene, build assertion passes). |
| `PodSeat.cs` eye-height lift (Haiku) | Reviewed against the builder: the seat is on the pod floor so the maths is right; compiles; **never run on a device or the Simulator**. |
| `RideSightlines.cs` v1 (Haiku) | **Replaced.** It tested world boxes of every group at every stop and reported 223 to 233 false problems, so it could not be switched on. The rewrite is triangle-accurate, live-group-aware, adds cone and text checks, and reports **0** on the real build; a mutation test proves it still catches a real blocker. |
| `RideSeatCaptures.cs` (Haiku) | Sound, but it left the chapter card in every frame and could not show the runtime view. Fixed; `RideLiveShot` added for Play Mode. |
| ADR-012, `STORY_OF_AI.md` (Haiku) | Faithful and well hedged ("to confirm"). Fixed stale lines (the bake-off folder existed; the narration description), added the sidecar-match rule, and softened the 1969 / AI-winter wording. |
| `ArtSource/voice_bakeoff/` (Haiku) | Honest about what it did not verify (the Qwen and Chatterbox commands are marked unconfirmed). Good enough; run it. |
| The v2 ride's look (earlier sessions) | Flat unlit parts, 190 renderers for the track, text 0.9° to 1.3°, poles over the UI, an empty finale. All addressed in this work. |

---

## 8. Driving Unity without the MCP bridge

`unity status --format json` shows the Editor (`state: ready`). Useful commands: `recompile` / `recompile_status`, `menu`, `console`, `clear_console`, `eval` (Roslyn; qualify `UnityEngine.Object`), `editor_play` / `editor_stop`, `set_autotick` (keeps the Editor ticking when unfocused; already on), `run_tests` / `test_status`, `capture_game_view`. Put a command's own parameters **after `--`**. After `editor_play` the first `eval` may return nothing while the domain reloads: retry. `Time.timeScale` set through `eval` is the way to fast-forward travel (the narration timers use `Time.deltaTime`, so they scale too).

A walk-through script exists only in the scratchpad of the session that wrote this; to recreate it: play, `SkipNarration()`, set the GO lever with `Controls.Get(RideControlIds.Action).SetFromUser(1f)`, wait for `AtStop`, then set `Trigger` 0.5 (stop 1); `Rock` 1, `Ice` 1, `Trigger` 0.5 (stop 2); GO again for LEARN (stop 3); shoot with `RideLiveShot.Shoot(...)` at each step.

---

## 9. File map (all uncommitted)

Modified: `Gameplay/Ride/{RideDirector,RideTypes}.cs`; `Presentation/Ride/{DashScreenView,NetworkDiagramView,RideMusic,RideTheme}.cs`; `XR/Ride/{DesktopSeat,PodSeat}.cs`; `Tools/Editor/{NeuralRideBuilder,McpAutoConnect}.cs`; `Tests/EditMode/Convergence.Tests.EditMode.asmdef`; `Tests/PlayMode/Gameplay/RideDirectorTests.cs`; generated: `Assets/Scenes/NeuralRide.unity`, `Assets/Prefabs/NeuralRide/*`, `Assets/Materials/NeuralRide/*`, `Assets/ScriptableObjects/RideTheme.asset`; docs: `.agents/{DECISIONS_INDEX,PROJECT_MAP}.md`, `.agents/tasks/IN_PROGRESS.md`, the master plan, `Documentation/Design/TEAM_BRIEFING.md`.

New: `Gameplay/Ride/RideScript.cs`; `Presentation/Ride/{NarrationLibrary,ChapterCardView,NeuralCoreView}.cs`; `Assets/Shaders/{HoloLit,NeuralLinks,NeuralNodes}.shader`; `Assets/ScriptableObjects/{RideScript,NarrationLibrary}.asset`; `Assets/Prefabs/NeuralRide/Meshes/`; `Assets/Materials/NeuralRide/{LineWhite,LineAmber,NetLinks,NetNodes,SlotFrame}.mat` and `SlotFrame.png`; `Tools/Editor/{NeuralRideBuilder.Pod,NeuralRideBuilder.Narration,NeuralRideBuilder.Network,RideNarrationData,RideSightlines,RideSeatCaptures,RideLiveShot}.cs`; `Tests/EditMode/Ride/RideNarrationTests.cs`; `Documentation/Architecture/ADRs/{ADR-012,ADR-013}-*.md`; `Documentation/Design/STORY_OF_AI.md`; `Documentation/Design/captures/ride_v3/` (and `ride_v2_baseline/`); `ArtSource/voice_bakeoff/`; this file. Commit the `.meta` files with their assets.
