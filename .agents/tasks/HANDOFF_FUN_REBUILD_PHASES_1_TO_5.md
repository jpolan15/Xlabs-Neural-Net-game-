# Handoff: Convergence Fun Rebuild, Phases 1 to 5

Status: code for phases 1 to 5 is in. The Level 1 scene file still needs a builder run. See `HANDOFF_PHASES_1_TO_5_REPORT.md`.
Owner of the remaining work: next agent
Updated: 2026-09-29

The owner asked for this file so the next agent can finish the rebuild without rediscovering the project. Read this, then `.agents/WORKFLOW.md`, then the task file you create for the phase you are on. Do not scan the whole repo.

The original plan is the owner's message in the chat that started this rebuild ("Convergence Fun Rebuild"). This file is the current state plus the work still to do. Where they differ, this file wins.

---

## What already shipped (Phase 0)

Playable scene: `Assets/Scenes/Level01_AwakeningGate.unity`, rebuilt by menu `Convergence/Build Level 1 — The Awakening Gate` in `Tools/Editor/Level01SceneBuilder.cs`. Never hand-edit the scene. Change the builder and run the menu through Unity MCP.

- The XRI 3.6 Starter Assets prefab `Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab` is instantiated with `PrefabUtility.InstantiatePrefab`. Tracking origin is Floor. The scene has one `XRInteractionManager`, an `EventSystem`, and `XRUIInputModule`.
- Head tracking is `TrackedPoseDriver` on `Main Camera`. It must stay enabled while a headset is connected.
- The 11 puzzle controls are still the old Gameplay-facing components. `Assets/Scripts/XR/XRInteractableBridge.cs` (`XRSimpleInteractable`) sits on each one. Dials and sliders turn by wrist twist (18 degrees per detent) or a quick tap. Cables, the crystal, the lever, terminals, and case receptors act on select. Public methods: `Activate()` and `Step(int)`.
- HUD is world-space TMP: `Assets/Scripts/Presentation/WorldSpaceHud.cs`. The RUN TEST button uses `Assets/Scripts/XR/UiForwardPassRelay.cs`. No `OnGUI`.
- `VRControllerPointerInteractor`, `DesktopInputFallback`, and `SciFiEngineerHUD` are deleted. Do not restore them.
- Desktop play without a headset is `Assets/Scripts/XR/DesktopWalk.cs` on the rig. WASD walks through the rig `CharacterController`. Mouse looks. Left click calls `XRInteractableBridge.Activate`. Scroll calls `Step`. Space calls `ChamberController.TriggerForwardPass`. Escape unlocks the cursor. It waits 2 seconds before touching anything, checks for a headset every frame, and the moment a device or a running `XRDisplaySubsystem` exists it stops and re-enables `TrackedPoseDriver`. It must never write the camera while a headset is active.
- Android OpenXR loader, `MetaQuestFeature`, and `OculusTouchControllerProfile` are enabled. Decision: `Documentation/Architecture/ADRs/ADR-007-xri-starter-rig.md`. Tooling: `ADR-009-unity-mcp-tooling.md`.
- XR Device Simulator sample is imported. How to use it: `Documentation/Operations/DESKTOP_WORKFLOW.md`.

### Neural net, fixed 2026-09-29

`ClassicNeuralNetwork3DVisualizer` is `[ExecuteAlways]`. Its `Awake` used to grab the pivot before the builder assigned it, cache the root position, then write that into the pivot every frame. The net was doubled away from the origin and spun through the ceiling and the front window.

The rest pose is now captured on the first `Update`, after the serialized pivot exists. The builder places the root at `(0, 1.55, 0.55)` with pivot scale `0.85`. In play mode the nodes stayed around y 1.9 and z 0.1 to 1.0. The ceiling is about y 3.5 and the viewport wall is about z 3.3. Do not scale the pivot back up or move the root toward z 1.45 or it will clip again. The dark backdrop is at z 1.85.

The visualizer still draws a 3-4-2 network. The simulation is one neuron with 2 inputs. Phase 1 replaces that picture. Do not "fix" the clip by hiding the net.

### How to drive Unity

`unityMCP` is HTTP at `http://localhost:8080/mcp`. If the tools are down, start the server from the project root:

```
uvx --from mcpforunityserver==9.7.3 mcp-for-unity --transport http --http-host 127.0.0.1 --http-port 8080
```

`uv` is at `%USERPROFILE%\.local\bin\uv.exe`. `Tools/Editor/McpAutoConnect.cs` reconnects the Editor side after a domain reload. The Editor must be open on this project. Do not use batchmode while it is open. Rebuild with `execute_menu_item` on `Convergence/Build Level 1 — The Awakening Gate`, and only when the Editor is not in play mode.

### Tests last run (2026-09-29, before the pivot fix)

- EditMode: 41 passed, 0 failed.
- PlayMode: 5 passed, 0 failed, including `Tests/PlayMode/XR/XRInteractableBridgeTests.cs`.
- `Validate-RepositoryLayout.ps1`: 47 checks, 0 violations. `pwsh` is not installed. Run it with Windows PowerShell: `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validation/Validate-RepositoryLayout.ps1`.

Re-run PlayMode after any scene or XR change. The Link playtest (look, walk into a wall, snap turn, teleport, ray, grab, press the HUD) has not been done by a person.

### Deviations the next agent must keep

- Dials are wrist-twist on `XRInteractableBridge`, not `RotationAxisLockGrabTransformer`.
- Cables, crystal, and lever are select-to-act, not grab and socket. Phase 3 is where real grab and socket should land, if the gameplay events stay.
- `DesktopWalk` exists on purpose. The owner plays the level in the Editor with no headset.
- The defense sentry is no longer placed by the builder. `CreateDefenseSentryTurret` is still in the file and unused. Phase 1 deletes that path and the sentry, gun, and motherboard visuals.
- `SetUnifiedWeight` in `Assets/Scripts/Gameplay/NeuralState.cs` still auto-sets bias to `-weight/2`. Nothing calls it right now. Phase 1 removes the auto-bias. `SetWeight` and `SetBias` are the real controls.
- `ChamberController` always loads `CreateORGatePuzzle()` in `Awake`. The `_puzzle ??= CreateEarthLocationPuzzle()` lines in the forward-pass paths are dead. Phase 1 removes those fallbacks. Do not wire the Earth puzzle up until Phase 5. `PuzzleEvaluator` only scores `outputs[0]` as a binary value, so it cannot grade Earth yet. That is Phase 4.

---

## Premise (locked for every later phase)

An asteroid strike knocks the ship out of hyperspace. The navigation AI, A.U.R.A., has lost its trained weights. The player is alone with it. Each chamber teaches one real ML concept and changes the ship:

1. Chamber 01, sensor array: perceptron OR. Power and shutters.
2. Chamber 02, spectrum filter: XOR and a hidden layer. Telescope comes online.
3. Telescope photo mode, then Chamber 03: label photos EARTH or NOT EARTH, train, fail honestly on a bad set.
4. Chamber 04, nav computer: attention over log tokens. Then the jump, and Earth fills the window.

Cut from the live game, not from history: sentry defense, the neural gun, the motherboard room, Synapse-GPT, and any HUD copy that says "calibrate sentry" while the voice talks about Earth.

Write this as `Documentation/Architecture/ADRs/ADR-008-premise-lost-in-space.md` and add a row to `.agents/DECISIONS_INDEX.md` before rewriting design docs.

---

## Phase 1 — One premise

Create `.agents/tasks/TASK_P1_PREMISE_LOCK.md` with allowed paths before editing.

Do:

- ADR-008, then rewrite `Documentation/Design/GDD_OVERVIEW.md` and `Documentation/Design/LEVEL_DESIGN_L01.md` around the arc above.
- Update `Assets/Puzzles/Chamber01/AGENTS.md`, `Chamber02_XOR/AGENTS.md`, `Chamber03_Training/AGENTS.md`, and `Chamber04_Attention/AGENTS.md` so the story matches. Do not change the math requirements in those files.
- Stop placing, and then delete the unused types: `DefenseSentryVisual`, `NeuralGunViewmodel`, `MotherboardEnvironmentVisual`, and the Synapse-GPT / sentry copy. The gun is still parented in the builder around the old "NeuralPulseGunViewmodel" block. It was removed from the rig path. Confirm nothing in the built scene still spawns it.
- Replace `ClassicNeuralNetwork3DVisualizer` with a visualizer that reads the real `NetworkModel` topology (one neuron, two inputs, bias, one output). Keep it inside the room. Same placement rule as above.
- Remove the auto-bias in `NeuralState.SetUnifiedWeight`. W1, W2, and bias stay independent. The dials already call `SetWeight` and `SetBias`.
- Remove the dead `CreateEarthLocationPuzzle` fallbacks in `ChamberController`.
- Reconcile `.agents/tasks/BACKLOG.md` and `COMPLETED.md` with what the code actually does. Several older task files describe sentry waves and a gun. Do not mark those done. Mark them dropped by ADR-008.

Done when the scene shows the real neuron, the three controls change three different numbers, the copy is only the lost-in-space premise, and layout validation passes.

---

## Phase 2 — The ship

Create `.agents/tasks/TASK_P2_SHIP.md` first.

Do:

- Stop building the hull from `CreatePrimitive` in the builder. Compose Kenney modules as prefabs under `Assets/_Project/Prefabs/Ship/`. The scene only places prefabs. See `Assets/Scenes` rules and `Assets/_Project`.
- Layout: bridge with a forward canopy, a corridor, and three rooms behind doors (sensor bay, observatory with the telescope, engine room with the jump drive). The player can walk the whole ship. Walls keep colliders.
- Keep the Kenney texture (`variation-a.png`). Darker hull, emissive accents in the existing color standard.
- Damage that goes away as repairs complete: sparks, flickering lights, a hull-breach patch.
- A real starfield skybox. Record the CC0 cubemap license in `Assets/ThirdParty/AGENTS.md`. Distant planet meshes, a faint far Earth, the ship's nose through the canopy, drifting asteroids via the existing `FloatingAsteroidField`.
- Bake lightmaps, one realtime key light, a URP Volume with bloom only.
- CC0 Kenney Sci-Fi and Interface sounds in `Assets/_Project/Audio/`. A.U.R.A. lines with world-space subtitles.
- Budget from any standing position: 100 draw calls or fewer, 100k tris or fewer.

`RenderSettings.skybox` is null today. `Assets/_Project/Audio` is empty. About half the Kenney Space Station models are unused, and the ones in use are re-skinned with flat white `Mat_Spaceship_Hull`.

---

## Phase 3 — Chamber 01 is the first fun slice

Create `.agents/tasks/TASK_P3_CHAMBER01.md` first. Do not start Phase 4 until the owner has played this over Link.

Do:

- Wrist objective plus short A.U.R.A. lines. Example: "Sensor array dark. Either beacon should wake it."
- OR stays honest: alert if a radio beacon OR a light signature is present. The four truth-table cases are sensor pings the player can see hit the neuron.
- Grab-and-twist weight dials with haptic detents, plug-in cables, a socketed activation crystal, a pull lever to fire a test pulse. Each case lights green or red from the existing `DiagnosticReport`.
- Three hints that explain the cause, triggered by repeated failures. They must not reveal the weights.
- Payoff: canopy shutters open onto the starfield, lights return, sensor bay door unlocks.
- The ship drifts toward the asteroid field with rising alarms and no death. This replaces the purge timer. `enableSandboxMode` is currently true and the timer is off.
- Update `Tests/PlayMode/Gameplay/Level01IntegrationTests.cs`. Add XR event tests under `Tests/PlayMode/XR/`. The bridge tests already cover cable select and a one-detent slider tap.

---

## Phase 4 — Core, only after Phase 3's playtest

Create `.agents/tasks/TASK_P4_CORE.md` first. Allowed paths are Core, `Documentation/Mathematics/FORMULAS.md`, and `Tests/EditMode`. No UnityEngine in Core.

Do:

- `PuzzleEvaluator` scores multi-output and continuous targets with a tolerance per output. `NetworkModel` gets gradient hooks.
- Replace empty `TrainingStub.cs` with deterministic backpropagation and SGD. A telemetry event fires on every step.
- Scaled dot-product attention in Core Math.
- `FORMULAS.md` updated in the same change. An EditMode test per formula.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validation/Validate-CoreBoundaries.ps1`.

---

## Phase 5 — Chambers 02 to 04 and the ending

Create `.agents/tasks/TASK_P5_CHAMBERS.md` first. One chamber per task if a change would cross assemblies. Stop and write a handoff instead of editing another subsystem.

- Chamber 02, XOR: one neuron cannot separate it. A hidden layer with a non-linear activation can. Solving it turns the telescope on.
- Chamber 03, Earth recognizer: at the telescope, aim and pull the trigger to photograph skybox bodies. A photo card prints. The player files each card in an EARTH or NOT EARTH tray. Features come from the capture (blue-ocean ratio, white-cloud ratio, brightness). Training is the Phase 4 SGD. A held-out set grades it. A bad set fails in a way the player can read. Example: only blue planets were labeled, so a blue ice giant is called Earth.
- Chamber 04, attention: nav-log tokens such as `[EARTH_LOCK] [FUEL_OK] [NOISE]` predict `JUMP_HOME` once the corrupted token is masked.
- Finale: the jump plays, Earth fills the canopy, then a short credits and a journal of what the player taught the AI.

---

## Rules that keep getting broken

- One task file with allowed paths before any edit. `AGENTS.md` and `.agents/WORKFLOW.md`.
- XRI is the only interaction system. `DesktopWalk` is a desktop substitute that turns off when a headset is present. It is not a second interaction stack.
- Core assemblies do not reference Unity, XR, or the MCP package.
- Do not modify `Assets/Samples/` or package versions. XRI 3.6 and the Starter Assets are already installed.
- Do not hand-edit `.meta` files or the scene YAML.
- `ProjectSettings/` needs the owner's approval. Android XR was approved in chat on 2026-09-29 and is done.
- Run the narrowest tests. Do not claim a pass that was not run.
- After directory or instruction-file changes, run `Validate-RepositoryLayout.ps1` with Windows PowerShell.
- File a handoff with `.agents/templates/HANDOFF_TEMPLATE.md` when a phase ends or a change leaves its allowed paths.
