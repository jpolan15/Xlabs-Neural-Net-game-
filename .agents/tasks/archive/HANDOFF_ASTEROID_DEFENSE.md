# Handoff: Asteroid point-defense redesign (Level 1)

Written: 2026-10-03
Status: **Implemented 2026-10-03.** Scene rebuilt. PlayMode 13/13 and EditMode 55/55 passed. See `HANDOFF_ASTEROID_DEFENSE_REPORT.md`. The Quest Link playtest in `BACKLOG.md` is still open.

## START HERE (new agent)

This redesign is already built. Read `HANDOFF_ASTEROID_DEFENSE_REPORT.md`. Do not rebuild it. The only open follow-up is the Quest Link playtest in `BACKLOG.md`.

## 1. What has actually been done

### Before this agent (from the user's handoff)
- All 16 voice clips are converted and normalized in `Assets/_Project/Audio/Voice/` (`vo_*.ogg`), each with a `.txt` transcript beside it. Every `.txt` has a `Speech: a-b s` line and the confirmed wording.
- The plan file exists: `C:\Users\Panda\.cursor\plans\asteroid_defense_redesign_cbc98d3f.plan.md`.
- Nothing else: no C#, scene, ADR, docs, or task-file changes.

### By this agent
- Read: `AGENTS.md`, `.agents/PROJECT_MAP.md`, `WORKFLOW.md`, `DECISIONS_INDEX.md`, the plan, `TASK_OPENING_RECORDING.md`, `IN_PROGRESS.md`.
- Read the directory `AGENTS.md` for `Gameplay`, `Presentation`, `_Project/Audio`, `Tools`, `Tests`, `XR`, and `Core/Puzzles`.
- Read these sources: `ChamberController`, `DataTargetReceptor`, `ChamberOnboardingController`, `NeuralState`, `BrokenConfigurationSO`, `GatewayController`, `LevelResetter`, `FailureHintDirector`, `ChamberPhase`, `InteractionAudioBus`, `VoyageDirector` (first 120 lines), `AuraSubtitles`, `AudioHookManager`, `DataTargetVisual`, `ShipDamageVisual`, `FloatingAsteroidField`, `FacilityAIVoiceAnnouncer`, `WorldSpaceHud`, `CableInteractable`, and the voice `.txt` files.
- Read most of `Tools/Editor/Level01SceneBuilder.cs`: lines 1–1420, the pods, presets and core hologram (about 2640–3075), the viewport (2021–2194), and the spark particles and aux consoles.
- **Files created: only this one.** Nothing else was edited, and no commands that change files were run.

### Environment, verified
- The Unity Editor is **closed**. There is no Unity process, no `Temp/UnityLockfile`, and the `unityMCP` tools return `no_unity_session`. Use **batchmode**, and say so in the final report.
- `mcp-for-unity` and Unity Hub processes are running. These are not the Editor.
- Unity is `C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe`.

## 2. Repo rules that constrain the work

- The agent's allowed paths come from task files. Create those first (see section 4, step 1).
- The test assembly `Convergence.Tests.PlayMode` references **Gameplay, XR, and Core. It does not reference Presentation.** Tests can only cover Gameplay.
- `Convergence.Presentation` does not reference XRI. Do not add that reference. Haptics use `UnityEngine.XR.InputDevices.GetDeviceAtXRNode(...).SendHapticImpulse(0u, amp, dur)`, the same way `Assets/Scripts/XR/CableInteractable.cs` does. The engine module needs no asmdef change.
- Presentation must not call `PuzzleEvaluator`, decide correctness, or drive state machines. It also must not allocate per frame in `Update`.
- Gameplay must not reimplement the math. It must call `PuzzleEvaluator` or the chamber methods that do.
- No package changes. There is no Timeline and no Cinemachine, so script the opening in code.
- Never shake the camera.
- Core is untouched, so `Validate-CoreBoundaries.ps1` should pass trivially. Still run it.

## 3. Findings that change the plan (verified in code)

These were not in the user's handoff. Each one would cause a bug if ignored.

1. **`VoyageDirector` replaces the puzzle on solve.**
   - `HandlePuzzleSolved` calls `chamber.BeginPuzzle(XOR)`. That sets `_puzzle` to XOR, sets `_hasSolved=false`, and sets the phase to `NeuralRepair`.
   - The victory swarm would then evaluate XOR, which is dishonest and wrong.
   - **Fix:**
     - The director captures `chamber.Puzzle` (the OR puzzle) in `Start`.
     - Add `ChamberController.ReplaySingleCase(int caseIndex, PuzzleDefinition puzzle)`. It evaluates honestly against the given puzzle and notifies the receptor. It fires `OnSingleCaseEvaluated`. It never sets `_hasSolved`, `LastEvaluation`, the phase, or `OnEvaluationComplete`.
     - Use `TriggerSingleCasePass` before solve and `ReplaySingleCase` after.
     - The director tracks its own `_solved` flag from `OnPuzzleSolved`, because `_hasSolved` gets reset.
   - `VoyageDirector.HandlePuzzleSolved` also calls `Unlock("SensorBay")` and `Unlock("Observatory")` immediately, before the swarm ends. That is outside this task. Note it in the report as a known limitation.

2. **`GatewayController` opens the door immediately on `OnPuzzleSolved`.**
   - The design wants the door to open after the swarm.
   - Add `[SerializeField] bool deferOpenToDirector` (default false). The builder sets it to true. The director calls `gateway.Open()` after the lesson delay.
   - The existing test `Level01_FullPlayableLoop_VerifiesCanonicalSequence` expects the gateway to open on solve. With the default false it keeps passing.

3. **`ChamberController.ApplyShieldDamage` ends the game at hull 0.**
   - It calls `TriggerEmergencyPurge`, which sets `_isPurged` and disables everything.
   - Add a serialized `enableSoftReroute` and `rerouteRestoreFraction = 0.6`, plus an event `OnHullRerouted`. When the hull reaches 0 with soft reroute on, restore it to 60%, fire `OnShieldUpdated` and `OnHullRerouted`, and do not purge. Keep the old behavior when the flag is off.
   - `ApplyShieldDamage` also returns early when `enableSandboxMode` or `_hasSolved` is true. The builder turns sandbox off. The director skips damage after its own `_solved`.

4. **The chamber's own wave loop and `EvaluateTacticalOutcomes` fight the director.**
   - Add `externalWaveDirector` with `SetExternalWaveDirector(bool)`.
   - When it is true, `Update` returns early, and `TriggerForwardPass` skips `EvaluateTacticalOutcomes`.
   - The lever and the "RUN TEST" HUD button still call `TriggerForwardPass`. Keep them as a "self-test". It evaluates honestly and can legitimately solve the puzzle.

5. **`TriggerSingleCasePass` fires `OnEvaluationComplete` on every scan.** That causes four side effects:
   - `AudioHookManager` plays a success chord or failure tone on every rock. Add `[SerializeField] bool playEvaluationSounds = true`, and set it to false in the builder.
   - `ChamberOnboardingController.HandleEvaluationComplete` calls `TransitionToStep(Completed)` on every passing evaluation. Guard it with `currentStep != Completed`.
   - `FailureHintDirector` counts every failed scan. It would hint at 2, 4, and 6 scans. Rewrite its strings in rock/ice/fire language.
   - `PerformanceTracker.RecordPulseFired` is also called on every scan.

6. **Two components both write the banner `TextMesh`.**
   - `AuraSubtitles` and `FacilityAIVoiceAnnouncer` both write `subText` (the "VR_Holographic_Subtitle_Banner"). The announcer also has an `OnGUI` that does not render in VR.
   - Plan: stop adding `FacilityAIVoiceAnnouncer` in the builder. `AuraSubtitles` already covers the banner.
   - `AuraSubtitles` plays `confirmation_001.wav` on every line. Remove that, as the opening task says.
   - `VoyageDirector.Start` fires `OnAuraLine("Sensor array dark...")`. That would overwrite the opening banner. `AuraSubtitles` must hold hints while the teleprompter or voice player is busy, keep the latest pending line, and show it afterward.

7. **`DataTargetVisual` problems.**
   - `OnGUI` draws text cards that do not render in VR. Delete it.
   - After a vaporize, the body reappears when `_impactTimer` expires. It should stay hidden until the next launch.
   - Pods are visible from the start. Add `IsInFlight`, `Launch()`, and `Retire()` on `DataTargetReceptor` (default true for back-compat; the director retires all on `Start`). The visual hides the body and aura light when not in flight.
   - `ResetReceptor` must not touch `isInFlight`.

8. **Geometry.**
   - The player is near the origin and looks toward +z.
   - The canopy glass is at z=3.5, spanning x ±3.4 and y about 1.0–3.0. `ShipNose` is at z=6.2.
   - Existing pods sit indoors at z 2.6→1.6 and are about 0.25 m, so they are invisible as asteroids.
   - New corridor:
     - Spawn at z≈34, spread across x ±6, y 2–4.
     - Scan line at about 65% progress (z≈15).
     - Perimeter at z≈4.8, just outside the glass, near x ±1.8 and y 1.8–2.6.
     - Scale the pod bodies up to about 1.2–2.0 m.
   - Remove the `CorridorRail` line renderers and the `PerimeterDock` pedestals.
   - Build a scan-line gate marker at z≈15 from emissive cubes. `PointDefenseVisual` flashes it on each scan.
   - Put the laser muzzle near the nose, for example (±0.9, 1.2, 5.0).

9. **Case index to object.** The OR puzzle's case order is `(0,0), (0,1), (1,0), (1,1)`.
   - Index 0 is the drone.
   - Index 1 has x2=1 and index 2 has x1=1.
   - The existing builder labels them "Radio" (idx 1) and "Light" (idx 2).
   - The new design: ROCK is x1 and ICE is x2. So idx 1 is the **icy comet** and idx 2 is the **rocky asteroid**. Cable 1 / W1 is ROCK. Cable 2 / W2 is ICE.
   - Use `CaseDiagnostic.ExpectedOutput` from the puzzle to classify right and wrong. Do not hardcode which objects are threats.

10. **Presets.**
    - `BrokenConfig_D` (w=0.5/0.5, b=0, Linear, both cables out) already has both cables disconnected. Add a new dedicated preset instead: "BrokenConfig_Opening" with W1=0, W2=0, bias=-1, Linear, **both cables disconnected**. Make it `presets[0]`, which `LevelResetter` and `ChamberController` use as the start.
    - `HandleNeuralStateMutated` in onboarding calls `AutoConfigureForLevel1()` when both cables connect. That sets Step activation and reconnects the cables. It does not change weights. Keep it.

## 4. Remaining work, in order

Mark steps done in this file as you finish them. Every step needs the task file's allowed paths to exist first.

### Step 1. Process files
- [x] DONE: ADR-010 written, index row added, GDD, LEVEL_DESIGN_L01 and README premise updated, `TASK_ASTEROID_DEFENSE.md` and `TASK_DEFENSE_FEEDBACK.md` created.
- [ ] TODO: create `TASK_OPENING_IMPACT.md` (Presentation: new `ShipImpactSequence.cs`, plus edits to `ShipDamageVisual`, `FloatingAsteroidField`, `NeuronMachineVisual`, `RetroAudioSynthesizer` if needed) and `TASK_BRIDGE_DECLUTTER.md` (Tools: `Tools/Editor/Level01SceneBuilder.cs`, the generated `Assets/Puzzles/Chamber01/BrokenConfig_Opening.asset`, and `Assets/Scenes/Level01_AwakeningGate.unity` as a build output only). Then widen `TASK_OPENING_RECORDING.md`, update `IN_PROGRESS.md` (add all 4 tasks), and run `pwsh Tools/Validation/Validate-RepositoryLayout.ps1`.
- [x] (original item) `Documentation/Architecture/ADRs/ADR-010-automated-point-defense.md`. Explain why it differs from ADR-008:
  - The player never holds a gun.
  - The neuron is the targeting brain.
  - The OR table and Core math are unchanged.
  - Correctness always comes from `PuzzleEvaluator`.
  - Use the ADR template used by the existing ADRs.
- [ ] Add the ADR-010 row to `.agents/DECISIONS_INDEX.md`.
- [ ] Update `Documentation/Design/GDD_OVERVIEW.md`, `Documentation/Design/LEVEL_DESIGN_L01.md`, and the README premise line.
- [ ] Create `.agents/tasks/TASK_OPENING_IMPACT.md`, `TASK_ASTEROID_DEFENSE.md`, `TASK_DEFENSE_FEEDBACK.md`, and `TASK_BRIDGE_DECLUTTER.md` with allowed and forbidden paths.
- [ ] Widen `TASK_OPENING_RECORDING.md` allowed paths to cover the onboarding rewrite, `ChamberController`, `GatewayController`, the new Presentation scripts, and `Tests/PlayMode/Gameplay/**`.
- [ ] Update `.agents/tasks/IN_PROGRESS.md` and `.agents/PROJECT_MAP.md` if directories change.
- [ ] Do **not** mark the Quest Link playtest in `BACKLOG.md` as done.

### Step 2. Gameplay changes
- [ ] `ChamberController`:
  - `externalWaveDirector` and `SetExternalWaveDirector`.
  - Soft reroute and `OnHullRerouted`.
  - `ReplaySingleCase`.
  - Skip `EvaluateTacticalOutcomes` when external.
- [ ] `DataTargetReceptor`:
  - New default titles: Repair Drone, Icy Comet, Rocky Asteroid, Rock-and-Ice Chunk.
  - `IsInFlight`, `Launch`, `Retire`.
- [ ] `GatewayController`: `deferOpenToDirector`.
- [ ] `ChamberOnboardingController`:
  - Add `OnOpeningBriefing`, fired once from `RoutineAwakeningSequence`. Replace the old "Asteroid impact detected" line.
  - Rewrite the step prompts and hints in rock/ice/fire language.
  - Guard the Completed transition.
  - Stasis doors still open at about 3 s. The player is never frozen.
- [ ] `FailureHintDirector`: rewrite its strings.
- [ ] New `AsteroidDefenseDirector.cs` in `Assets/Scripts/Gameplay/`:
  - Tick-driven. `Update` calls a public `Tick(float dt)`, so tests can step deterministically.
  - Shuffled waves of the four receptors, about 8 s travel, about 3 s apart, about 4 s between waves.
  - Starts after `OnOpeningBriefing` plus about 18.24 s (serialized).
  - At about 65% progress it scans. It calls `TriggerSingleCasePass(i)` before solve and `ReplaySingleCase(i, _defensePuzzle)` after.
  - Decision: `fired = diag.ActualOutput >= 0.5`. Classify with `diag.ExpectedOutput`.

    | Fired | Expected | Result |
    |---|---|---|
    | yes | fire | vaporize |
    | yes | don't fire | friendly fire: vaporize the drone, small damage, `OnFriendlyCasualty` |
    | no | don't fire | drone docks at the perimeter, `RestoreShield` a few points |
    | no | fire | breach at the perimeter: `ApplyShieldDamage` (about 12) |

  - No damage after the director's own `_solved` flag.
  - On `OnHullRerouted`: clear the in-flight objects and pause about 4 s.
  - On `OnPuzzleSolved`: cancel unlaunched objects and let in-flight ones resolve. Wait about 4.5 s for `vo_solved`. Run 3 victory waves (about 3.5 s travel, about 0.8 s spacing) through `ReplaySingleCase`. Then raise `OnVictoryComplete`. After about 9.5 s (the lesson length, serialized), raise `OnRestored` and call `gateway.Open()`.
  - Subscribe to `OnChamberReset` to restart.
  - Events for Presentation: `OnWaveStarted(index, isVictory)`, `OnObjectLaunched`, `OnScan(receptor, diag, fired)`, `OnOutcome(receptor, outcome)`, `OnVictoryStarted`, `OnVictoryComplete`, `OnRestored`.

### Step 3. Presentation scripts (`Assets/Scripts/Presentation/`)
- [ ] `OpeningTeleprompter`:
  - Plays `vo_opening_neural_recording` once from a spatial `AudioSource` on the console.
  - Banner lines from `TASK_OPENING_RECORDING.md`: 0.00–2.70, 3.06–4.62, 5.16–6.72, 7.24–11.72, 12.48–16.72, clear at 16.72.
  - Quiet `computerNoise_003.ogg` tick at start and a low loop of `spaceEngineLow_001.ogg` on a 2D source. The voice stays louder.
  - Expose `IsPlaying` and `OnFinished`.
  - Use the hand-checked transcript, with four "warning"s.
- [ ] `VoiceLinePlayer`:
  - One voice at a time. Treat the teleprompter as busy.
  - `vo_solved`, `vo_reroute`, `vo_restored` interrupt and clear the queue.
  - Hit lines (`vo_hit_a/b/c`) have a 6 s cooldown and are dropped if busy. The first hit always plays `vo_first_hit`.
  - Other lines queue (FIFO, small cap, about 12 s expiry).
  - The banner shows each line for its speech span.
  - Parse the `.txt` files in the builder (a regex on `Speech: a-b s`, with the text taken from the first paragraph after the blank line) and serialize the text and span into the component. That keeps the `.txt` the source of truth.
  - Triggers:

    | Line | Trigger |
    |---|---|
    | `vo_wave1_incoming` | first wave launch |
    | `vo_first_hit` | first hull hit |
    | `vo_cables_in` | both cables connected |
    | `vo_first_kill` | first correct vaporize |
    | `vo_shot_drone` | friendly fire |
    | `vo_hit_a/b/c` | later hits, random |
    | `vo_hull_half` | hull under 50% |
    | `vo_reroute` | `OnHullRerouted` |
    | `vo_stuck` | 45 s with no positive outcome, not solved, repeat at most every 60 s |
    | `vo_solved` | `OnPuzzleSolved` |
    | `vo_swarm` | victory start |
    | `vo_lesson` | victory complete |
    | `vo_restored` | gateway opened / `OnRestored` |

- [ ] `ShipImpactSequence`, driven by `OnOpeningBriefing`:
  - Impact boom at 0 s, red lights, sparks.
  - Axons go dark at about 5–7 s. Cables spark and pop out at about 5–7 s.
  - Asteroid field slides into view at about 10–12 s.
  - Alarm pulses at 12.48, 13.72, 14.92, and 16.20 s, using `RetroAudioSynthesizer`.
  - No camera shake.
  - Reuse `ShipDamageVisual`, `FloatingAsteroidField`, and `NeuronMachineVisual`.
  - "Lights go blue" on `OnRestored`.
  - Cables start disconnected because of the preset, not because of this visual.
- [ ] `PointDefenseVisual`:
  - Laser `LineRenderer` from the muzzle to the target on vaporize, plus an explosion particle and the scan-gate flash.
  - Hull-hit sparks, red flash lights, and haptics (see section 2) on `OnShieldDamaged`.
  - Console lamps ROCK, ICE, and FIRE light live from `OnScan` (`diag.Inputs` and `ActualOutput`).
  - Kenney sounds: `laserLarge_*`, `explosionCrunch_*`, `impactMetal_*`, `forceField_*`.
- [ ] `DataTargetVisual`: delete `OnGUI`, hide the body after a vaporize until the next launch, and hide it when not in flight.
- [ ] `AuraSubtitles`: remove the confirmation clip, and hold hints while the teleprompter or voice player is busy.
- [ ] `AudioHookManager`: add `playEvaluationSounds`.
- [ ] `WorldSpaceHud`: this becomes the single formula panel. Show `FIRE = step(w1·ROCK + w2·ICE + b)` with live values. Update `caseLabels` to Drone / Comet / Rock / Both (note the idx 1 = ICE, idx 2 = ROCK ordering).

### Step 4. `Tools/Editor/Level01SceneBuilder.cs`
- [ ] Add the "BrokenConfig_Opening" preset (both cables disconnected) as `presets[0]`.
- [ ] Remove:
  - The pedagogy board (about lines 844–911) and the diagnostic matrix (about 933–1004).
  - The engineer tablet (about 913–931).
  - The pod header and bottom screens, rails, and pedestals (in `CreateDataTargetPods`).
  - The core hologram text (about 2784–2822 in `CreatePlanetaryAICoreHologram`). Keep the hologram sphere if wanted.
  - The aux console `TextMesh` text.
  - The sign subtitle. Change "USS CONVERGENCE" to "NEURAL" (about line 1135).
  - Add no `FacilityAIVoiceAnnouncer`.
- [ ] Rename the console labels to ROCK SENSOR, ICE SENSOR, TRIGGER BIAS, and FIRE. Keep the sign "NEURAL". Shorten the lever label (for example "SELF-TEST").
- [ ] Rebuild the pods: new corridor, scaled bodies, rock / ice / chunk / drone looks, scan-gate marker, and muzzle.
- [ ] Wire everything with `SerializedObject`:
  - Director, `PointDefenseVisual`, `ShipImpactSequence`, `OpeningTeleprompter`, `VoiceLinePlayer`.
  - `externalWaveDirector=true`, `enableSandboxMode=false`, `enableSoftReroute=true`, `deferOpenToDirector=true`, `playEvaluationSounds=false`.
  - Voice `.ogg` clips and parsed text/spans.
- [ ] `AudioHookManager` clip slots:
  - Cable: `impactMetal_000.ogg`.
  - Lever: `doorClose_000.ogg`.
  - Step: `click_001.wav`.
  - Success: `confirmation_001.wav`.
  - Failure: `lowFrequency_explosion_001.ogg` at low volume.
  - Ship bed: looping `spaceEngineLow_001.ogg` on a 2D source.
  - The Kenney clips are in `Assets/_Project/Audio/Kenney/`.
- [ ] Rebuild the scene in batchmode:

  ```powershell
  & "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\Panda\Downloads\neural game" -executeMethod Convergence.EditorTools.Level01SceneBuilder.BuildLevel01 -quit -logFile -
  ```

  - Note `BuildLevel01` ends by calling `CaptureBridgeView()`. That may fail under `-nographics`. Check the log, and drop `-nographics` if needed.
  - The generated `Assets/Scenes/Level01_AwakeningGate.unity` is a build output. Do not hand-edit it.

### Step 5. Tests (`Tests/PlayMode/Gameplay/AsteroidDefenseTests.cs`)
Use the same reflection `SetField` pattern as `Level01IntegrationTests.cs`. Build `ChamberController`, `NeuralState`, `PerformanceTracker`, and four `DataTargetReceptor` objects on a test root. Drive the director with `Tick(dt)`, not real time. Set `externalWaveDirector` and soft reroute through the public setters or reflection.
- [ ] Broken network: threats breach and the hull drops.
- [ ] Correct network (w1=w2=1, b=-0.5, Step, both cables connected): rocks vaporize, the drone docks, and `OnPuzzleSolved` fires **exactly once**.
- [ ] Hull 0 reroutes (about 60%, settings kept, no purge) instead of failing.
- [ ] Solved only when `PuzzleEvaluator` passes. A 3/4-correct network never solves.
- [ ] Victory waves keep evaluating OR even after the puzzle is swapped to XOR. Simulate this by calling `chamber.BeginPuzzle(PuzzleDefinition.CreateXorPuzzle())` in the test, then check the swarm outcomes are still against OR.
- [ ] Gateway opens only after the victory sequence when `deferOpenToDirector` is true.
- [ ] Existing `Level01IntegrationTests` still pass.

### Step 6. Validate and report
Run these, and report each command and its exact output:
- [ ] Compile with no errors. Batchmode log, or `-quit` and read the log.
- [ ] PlayMode tests:

  ```powershell
  & "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\Panda\Downloads\neural game" -runTests -testPlatform PlayMode -testResults "Tests\Results\playmode.xml" -logFile -
  ```

  Running tests with `-nographics` can fail. Omit it. Do not commit `Tests/Results/`.
- [ ] EditMode tests, since the Core tests exist (`-testPlatform EditMode`).
- [ ] `pwsh Tools/Validation/Validate-RepositoryLayout.ps1`.
- [ ] `pwsh Tools/Validation/Validate-CoreBoundaries.ps1`.
- [ ] Update `.agents/tasks/IN_PROGRESS.md`, and write the final report with `.agents/templates/HANDOFF_TEMPLATE.md`.

## 5. Things to say they could not be verified
Unless you actually did them:
- Anything on Quest 2 hardware, Quest Link, or Meta XR Simulator. This includes haptics, comfort, performance, and timing feel. The Quest Link playtest stays open in `BACKLOG.md`.
- Visual quality in a headset. Batchmode cannot show it.
- Audio mix levels (voice versus the engine bed).
- `VoyageDirector` still unlocks `SensorBay` and `Observatory` on solve, before the swarm ends.

## 6. Pitfalls
- Do not commit `Library/`, `Logs/`, `Temp/`, or `UserSettings/`. They show up as untracked in git status. Only stage files this work touches.
- `Assets/Materials/Mat_*.mat` show as modified in git from before this work. Do not include them without checking.
- Do not edit `.meta` files or generated scene files by hand.
- Do not delete failing tests.
- Do not claim a test passed unless you ran it.
