# Task: Chamber 01 master plan (game, then ship)

Status: in-progress
Owner: Presentation/Tools
Created: 2026-10-06
Updated: 2026-10-06

## Description

Chamber 01 reads as a pile of controls and grey boxes. The master plan (source: the Chamber 01 visual-kit plan, not edited by this task) first proves the OR-gate loop is clear, then adds later puzzles, then makes the room look like a worn research ship, inside the Quest 2 budget. This file is the running report. WP0 is read-only recon. Later work packages are not started.

WP0 did not change game code, scenes, materials, ProjectSettings, packages, or ThirdParty. The Editor was already in Play Mode when the scene was measured. This recon did not enter Play Mode, did not save the scene, and did not modify objects.

## Allowed paths

WP0 (this work package) is read-only. The only files WP0 may write are this task file and `.agents/tasks/IN_PROGRESS.md`.

Allowed paths for the overall plan, after WP0, and only inside the work package that names them:

- `Tools/Editor/Level01SceneBuilder.cs`
- `Tools/Editor/Level01DefenseSetup.cs`
- `Tools/Editor/` helpers a later WP card names (`CaptureChamberViews` and related editor scripts do not exist yet)
- `Tools/Verify-Chamber01.ps1` (does not exist yet; WP1 creates it)
- `Tools/Build-Level01Scene.ps1` (invoke only; do not point it at a different Unity version)
- `Assets/Scripts/Presentation/**`
- `Assets/Materials/**` (first-party materials only, and only through the builder)
- `Documentation/Design/**`

User order on 2026-10-06 to finish the whole plan also allows these, because the cards cannot be done inside Presentation alone:

- `Assets/Scripts/Gameplay/**` for live evaluation, prompts, curriculum flow, and saved circuits. Presentation still must not call `PuzzleEvaluator.Evaluate`.
- `Assets/Scripts/Core/**` only after the Core Change Proposal in this file is marked approved (WP6).
- `Tests/EditMode/**` for the reference-solution tests and the XOR single-neuron sweep (WP6).
- `Assets/Scripts/XR/**` only for detent haptics on the existing slider and dial interactors (WP13).
- `Documentation/Mathematics/FORMULAS.md` only if a Core formula changes. WP6 reuses `Forward` and `Evaluate` and does not change a formula.

`Assets/Scenes/Level01_AwakeningGate.unity` is generated. Later WPs change the builder and rebuild. They do not hand-edit the scene.

## Forbidden paths

- `Assets/ThirdParty/**`
- `Assets/Samples/**`
- `Assets/Scripts/Core/**` except inside an approved Core Change Proposal (WP6)
- `ProjectSettings/**`
- `Packages/**`
- `Tools/chamber01-thresholds.json` once it exists (human-owned; agents do not edit it)
- Any URP, Quality, or Player setting

## Acceptance criteria

- [x] WP0 RECON below covers all 14 items, with contradictions and a stop decision.
- [ ] Later work packages are not done. Their checks are not started.
- [ ] Tests cover positive, negative, zero, and boundary values when a later WP changes Core.
- [ ] No forbidden dependencies introduced.
- [ ] Handoff report filed when a WP crosses a subsystem boundary.

## Blocked on

None for WP0. Later gates need a human for captures, the headset text strip, threshold calibration, playtests, and any Core change approval.

## Handoff notes

WP0 is done. Do not STOP. The pipeline is URP, and the Chamber 01 truth table matches plan section 3.5 (fire at comet, rock, and both; hold at the drone). The next agent starts at WP1 and must read this RECON instead of the plan's stale line numbers.

Measured from Unity `6000.6.0f1`, scene `Assets/Scenes/Level01_AwakeningGate.unity`, MCP instance already pinned. Branch at recon time: `main`. Worktree: `C:/Users/Panda/Downloads/xlabs-neural-net-game-`.

## RECON

**Do not STOP.** `GraphicsSettings.defaultRenderPipeline` and the active quality pipeline are both `UniversalRenderPipelineAsset` named `PackageSamplesURPAsset`. The live Chamber 01 table is fire at the icy comet, the rocky asteroid, and the rock-and-ice chunk, and hold at the repair drone.

Measurement caveat: the Editor was already playing. Authored poses below come from the saved scene and the XR Origin prefab. Live component values (some light intensities, HUD strings, particle counts, UnityStats) are from that play session. The play camera had already been walked off the start pose. No screenshot was judged.

### 1. Truth table and what "missed" means

`PuzzleDefinition.CreateORGatePuzzle` is the puzzle `ChamberController` starts with. Required activation is Step. Accuracy threshold is 100%. ROCK is input `x1` and weight socket 0. ICE is input `x2` and weight socket 1. The world HUD formula is `FIRE = step(Weight1·ROCK + Weight2·ICE + Bias)`.

| Case | ROCK (`x1`) | ICE (`x2`) | Expected | Title | Role | HUD label |
|---|---:|---:|---:|---|---|---|
| 0 | 0 | 0 | 0 (HOLD) | Repair Drone | LET IT DOCK | Drone |
| 1 | 0 | 1 | 1 (FIRE) | Icy Comet | FIRE | Comet |
| 2 | 1 | 0 | 1 (FIRE) | Rocky Asteroid | FIRE | Rock |
| 3 | 1 | 1 | 1 (FIRE) | Rock-and-Ice Chunk | FIRE | Both |

This is plan section 3.5's Chamber 01 row.

`WorldSpaceHud.BuildResultLine` writes one word per case from `ChamberController.LastEvaluation`:

- **missed** when that case is not correct. Correct means `CaseDiagnostic.IsCorrect` and `PuzzleEvaluation.ActivationMatches`. A wrong FIRE and a wrong HOLD both say missed. Linear or ReLU also makes every case missed, even if the raw number would have matched, because Step is required.
- **burned** when the case is correct and the expected output is at least 0.5 (the three FIRE cases).
- **safe** when the case is correct and the expected output is below 0.5 (the drone).

Before any evaluation, the result line is the static sentence "Drone slips past. Ice, rock, and both should burn." During the play session already running, the HUD showed `Drone missed   Comet missed   Rock missed   Both missed` with activation `LINEAR` and weights `0.0`, `0.0`, bias `-1.0`. That matches the opening preset, not a different truth table.

`FailureHintDirector` uses the same case order: 0 drone, 1 icy comet, 2 rocky asteroid, 3 rock-and-ice chunk.

### 2. Controls, ranges, steps, how Presentation reads them

| Control | Component | Writes | Range | Step |
|---|---|---|---|---|
| ROCK slider | `KineticWeightSliderInteractor` on `SliderTrack_W1`, `socketIndex` 0 | `NeuralState.SetWeight(0, value)` (`Weight1`) | -2 to 2 | 0.5 |
| ICE slider | `KineticWeightSliderInteractor` on `SliderTrack_W2`, `socketIndex` 1 | `NeuralState.SetWeight(1, value)` (`Weight2`) | -2 to 2 | 0.5 |
| Bias dial | `BiasDialInteractor` | `NeuralState.SetBias` (`Bias`) | -2 to 2 | 0.5 |
| ROCK cable | `CableInteractable` `cableIndex` 0 | `NeuralState.SetCableConnected(0, …)` | connected or not | n/a |
| ICE cable | `CableInteractable` `cableIndex` 1 | `NeuralState.SetCableConnected(1, …)` | connected or not | n/a |

The builder also adds a `WeightRegulatorInteractor` on each slider track with the same socket index. Both components write the same `NeuralState` weight. The builder does not override step or range; those are the field defaults.

Visible labels are `W1`, `W2`, `TRIGGER BIAS`, `ROCK SENSOR`, `ICE SENSOR`, `FIRE`, `SELF-TEST`. The slider `sensorName` strings are still "Earth Continental Sensor" and "Earth Atmosphere Sensor". Those names are not the visible labels.

`ChamberController.GetEffectiveNetwork` returns the live network when both cables are connected. Otherwise it deep-copies the network and sets the disconnected weight to 0. `NeuralState.FilterInputsByCables` also exists and zeros the input; the evaluation path uses the weight-zeroing copy.

Presentation does not write these controls. It reads `NeuralState` properties (`Weight1`, `Weight2`, `Bias`, `Activation`, `Cable1Connected`, `Cable2Connected`) and `PuzzleEvaluation` events (`OnEvaluationComplete`, `LastEvaluation`). Scripts that do this include `WorldSpaceHud`, `NeuralNetwork3DVisualizer`, `DecisionBoundaryHologram`, `DiagnosticHologramVisual`, `NeuronPedagogyHologramVisual`, `ChamberSignboardVisual`, `EngineerFieldManualVisual`, `NeuronTopologyVisualizer`, `NeuronMachineVisual`, `PlanetaryAICoreHologram`, `VoiceLinePlayer`, and `AudioHookManager`. No file under `Assets/Scripts/Presentation` calls `PuzzleEvaluator.Evaluate`. Several of those scripts recompute `w1*x1 + w2*x2 + b` locally for a drawing. That is display math. It is not the evaluator, and it does not run on a hypothetical copy.

Opening preset (`BrokenConfig_Opening`, the level start): weights 0 and 0, bias -1, activation Linear, both cables disconnected.

### 3. Evaluation API, hypothetical weights, live vs SELF TEST

The only evaluator entry used by gameplay is `PuzzleEvaluator.Evaluate(NetworkModel network, PuzzleDefinition puzzle)`. There is no overload that takes hypothetical weights. `NetworkModel.DeepCopy` exists, and so do `LayerModel.DeepCopy` and `NeuronModel.DeepCopy`. A caller can copy the network, change the copy, and pass that copy to `Evaluate`. That is enough for a hint search. It is not a dedicated hypothetical API. Presentation is forbidden from calling `Evaluate`. A later live-card or hint feature has to go through Gameplay, or through an approved Core change.

Evaluation does **not** run on every control change. `NeuralState.OnStateMutated` only raises the event.

These call `TriggerForwardPass`, which evaluates every case:

- `ClockPulseLeverInteractor` (the physical lever labeled SELF-TEST)
- `UiForwardPassRelay` (the world-HUD button labeled SELF-TEST)
- `DesktopWalk`
- `NeuralPulseToolInteractor` (also has a single-case path)

These call `TriggerSingleCasePass`:

- `NeuralPulseToolInteractor`
- `XRInteractableBridge` when the clicked object is a `DataTargetReceptor`
- `AsteroidDefenseDirector`

`TriggerForwardPass` and `TriggerSingleCasePass` both call `GetEffectiveNetwork` and then `PuzzleEvaluator.Evaluate`. `ReplaySingleCase` evaluates a caller-supplied `PuzzleDefinition` and does not change solve state.

### 4. Player start pose, console position, window rectangle

Authored start, from the saved scene and the XRI starter-rig prefab, not from the walked play camera:

- `XR Origin (XR Rig)` at `(0, 0, -0.9)`, rotation identity, floor tracking.
- `Camera Offset` local position `(0, 1.36144, 0)`.
- `Main Camera` local position zero, local rotation identity, field of view 60, near 0.03, far 500 (the prefab near/far are overwritten by the scene).
- Authored eye: `(0, 1.36144, -0.9)`, looking along +Z.

The play session already running had walked the rig to about `(0.477, 0, -2.088)`, yaw 54.9 degrees. That is not the start pose.

Console root `TactileEngineeringWorkstation` is at `(0, 0, -0.22)`, scale `(1, 1, 1)`.

Flight desk `ConsoleDesk_FlightModel` is a child at local scale `(1.30, 1, 1)`, world position `(0, 0, -0.22)`, yaw 180. Renderer bounds center `(0, 0.20, -0.22)`, size `(1.82, 0.40, 0.90)`. Mesh `table-large`. Material `Mat_Mainframe_DarkPlating`.

Window glass `PanoramicViewportGlass` bounds center `(0, 2.00, 3.55)`, size `(6.80, 2.45, 0.03)`. The pane is the rectangle `x` from -3.40 to 3.40, `y` from 0.775 to 3.225, at `z = 3.55`. The frame root is at `(0, 0, 3.50)`.

### 5. Light inventory, bloom, ambient

33 lights. Every light's shadows are `None`. Only `DirectionalLight_StellarKey` is Realtime. Every other light is Baked. Intensities were read during the play session already running, so script-driven point lights may differ from the saved scene. `Lightmapping.lightingDataAsset` is assigned and named `LightingData`. Whether that bake matches this hierarchy was not checked.

| Type | Object | Intensity | Range | Spot angle | Shadows | Mode |
|---|---|---:|---:|---:|---|---|
| Directional | DirectionalLight_StellarKey | 1.050 | 10 | 30 | None | Realtime |
| Directional | DirectionalLight_DeckFill | 0.450 | 10 | 30 | None | Baked |
| Spot | NeuralNetwork_Spotlight | 2.600 | 5.00 | 75 | None | Baked |
| Point | NeuralNetwork_Aura | 1.900 | 4.50 | 30 | None | Baked |
| Point | RibDownlight (three ribs) | 1.200 | 8.00 | 30 | None | Baked |
| Point | CeilingDaylight_1 through _4 | 1.350 | 7.50 | 30 | None | Baked |
| Point | Lamp_ROCK | 2.400 | 0.80 | 30 | None | Baked |
| Point | Lamp_ICE | 2.400 | 0.80 | 30 | None | Baked |
| Point | Lamp_FIRE | 0.120 | 0.80 | 30 | None | Baked |
| Point | SliderTrack_W1, SliderTrack_W2 | 0.080 | 0.90 | 30 | None | Baked |
| Point | ThresholdSquelchValve, ClockPulseLever_Base | 0.080 | 0.90 / 1.00 | 30 | None | Baked |
| Point | NeuralCable_1, NeuralCable_2 | 1.215 | 0.90 | 30 | None | Baked |
| Point | SocketedCrystalRoot | 1.593 | 1.00 | 30 | None | Baked |
| Point | StasisPod_AwakeningLight | 0.500 | 4.00 | 30 | None | Baked |
| Point | AftBeacon | 0.080 | 4.50 | 30 | None | Baked |
| Point | DataTarget case auras | 2.400, 2.400, 5.280, 5.280 | 4.50 | 30 | None | Baked |
| Point | PlanetaryAICore_Hologram | 0.559 | 4.00 | 30 | None | Baked |
| Point | AlarmLight_Port, AlarmLight_Starboard | 0.764 | 6.00 | 30 | None | Baked |
| Point | GateAuraLight | 0.350 | 6.00 | 30 | None | Baked |
| Point | Cinematic ship Engine_L, Engine_R | 0.350 | 3.20 | 30 | None | Baked |

Ambient mode is Flat. Color is `(0.10, 0.13, 0.20)`. Intensity is 0.75. Skybox is `Sky_Starfield` (`Skybox/Cubemap`). Reflection intensity is 0.6. Fog is off.

Bloom: one global volume, `GlobalVolume`, weight 1, profile `Volume_Bloom`. The profile's component list is a null entry (`{fileID: 0}`). At runtime `components.Count` is 0. There is no Bloom override. The builder method `CreateBloomVolume` would set intensity 0.35 and threshold 1.15 if it ran again and found no Bloom component. That is source intent, not the live profile. The main camera's components are Transform, Camera, AudioListener, and TrackedPoseDriver. It has no `UniversalAdditionalCameraData`, so post-processing is not enabled on that camera.

### 6. Particle inventory

| System | Max particles | Rate over time | Lifetime | Start size | Start speed | Shape | Live count |
|---|---:|---:|---:|---:|---:|---|---:|
| EmergencySparkParticles | 25 | 4 | 0.8 | 0.035 | 2.5 | Sphere, radius 0.15 | 3, playing |
| DataStreamParticles | 40 | 6 | 4 | 0.02 | 0.12 | Box `(5, 2.5, 5)` at `(0, 1.8, 0.5)` | 24, playing |
| DefenseExplosion | 1000 | 0 | 0.45 | 0.25 | 3.5 | Cone | 0, not playing |

`DefenseExplosion` is in the saved scene. The data-stream box covers the room, not only the console. No other `ParticleSystem` was in the loaded scene.

### 7. Materials visible from the authored seat

Frustum used for this grouping: eye `(0, 1.36144, -0.9)`, looking +Z, vertical fov 60, aspect 16:9 (assumed), near 0.03, far 500. 601 enabled renderers in the scene, 339 inside that frustum, 41 unique material rows. Play Mode had already instanced some materials, so a few emission colors are runtime copies.

Required surfaces:

| Surface | Material | Shader | Smoothness | Metallic | Emission |
|---|---|---|---:|---:|---|
| Flight desk and avionics terminal | `Mat_Mainframe_DarkPlating` | URP Lit | 0.65 | 0.60 | off |
| Hull, viewport header, viewport sill, command-bridge deck | `Mat_Spaceship_Hull` | URP Lit | 0.85 | 0.05 | off |
| Deck tiles, corridor floors, both bridge chairs | `Mat_Kenney_SpaceStation` | URP Lit | 0.75 | 0.15 | off |
| Panoramic glass and four side window panes | `Mat_Spaceship_ViewportGlass` | URP Lit | 0.98 | 0.00 | about `(0.002, 0.005, 0.008)` |
| Window shutters | `colormap` | URP Lit | 0.00 | 0.00 | off |

`Mat_Kenney_SpaceStation._BaseMap` is `variation-a.png`. The flight desk and the hull the player sees do not use that material. They use `Mat_Mainframe_DarkPlating` and `Mat_Spaceship_Hull`.

Other groups inside the same frustum, by renderer count: `Mat_Space_StarGlow` 125 (URP Lit, smoothness 0.95, metallic 0, emission about `(3.33, 3.43, 3.50)`); `Mat_Glow_Amber` 44 and `Mat_Glow_Cyan` 44 (smoothness 0.90, metallic 0.20, emission on); `Font Material` 15 (`GUI/Text Shader`); bus-trace glow 7 plus 2 instances; asteroid material 6 plus a few instances; viewport-adjacent glows (coral, emerald, violet), hazard yellow, warning amber, ceiling-light emission about `(2.88, 2.94, 3.00)`, and `Shader Graphs/SkyPlanet` on the gas giant (no `_Smoothness` or `_Metallic`). Unlit materials in view: `Mat_Axon_Positive` and `Mat_Mainframe_CorePlasma`.

### 8. Text inventory

23 `TextMesh` objects and 4 TMP objects. `fontSize` is the TextMesh font size. `characterSize` is the world scale. The facing dot is `Vector3.Dot(forward, authoredEye - position)` with the authored eye from item 4. The plan's readable test (`dot < 0`) assumes the glyphs face -Z. Unity `TextMesh` faces +Z and world TMP often faces the other way, so this dot is recorded and not treated as a visual verdict. No image was judged. Callout dots are also mixed with the walked play camera, because those labels billboard at runtime.

| Object | Size | Facing dot | Kind | Content |
|---|---|---:|---|---|
| W1_Label, W2_Label | font 17, char 0.011 | -0.59 | static TextMesh | W1, W2 |
| Bias_Label | font 17, char 0.011 | -0.71 | static | TRIGGER BIAS |
| Cable1_Label, Cable2_Label | font 15, char 0.010 | -0.71 | static | ROCK SENSOR, ICE SENSOR |
| Socket_Label | font 18, char 0.012 | -0.58 | static | FIRE |
| Lever_Label | font 18, char 0.012 | -0.62 | static | SELF-TEST |
| ROCKTag, ICETag, FIRETag | font 0, char 0.045 | -0.88 | static | ROCK, ICE, FIRE |
| Sign_ChamberNumber | font 22, char 0.022 | -0.40 | static | 01 |
| Sign_Subtitle | font 16, char 0.018 | -0.40 | static | BRIDGE DECK 01 // EMERGENCY WARP RECOVERY |
| ScreenText_Port, ScreenText_Starboard | font 20, char 0.015 | +2.95 | static | empty |
| DepartureCredits | font 28, char 0.018 | +2.45 | static | empty |
| VR_Holographic_Subtitle_Banner | font 24, char 0.028 | -3.25 | runtime `AuraSubtitles` | empty during this sample |
| Callout on cases 1–4 | font 0, char 0.550 | +6 to +29 | runtime `DataTargetVisual` | DRONE, ICE, ROCK, BOTH |
| Planetary header, bar, details | font 20 / 18 / 15 | -0.28 | runtime `PlanetaryAICoreHologram` | warp-navigation copy, including a 30% integrity bar |
| WorldSpaceHud Objective | TMP 34 | -1.27 | runtime `WorldSpaceHud` (yaw only) | Grab the ROCK cable and click it in. Then do the same for ICE. |
| WorldSpaceHud Readout | TMP 28 | -1.27 | runtime yaw billboard | FIRE = step(0.0·ROCK + 0.0·ICE + -1.0) LINEAR |
| WorldSpaceHud Result | TMP 26 | -1.27 | runtime yaw billboard | four "missed" words, then "Change one dial." |
| WorldSpaceHud button label | TMP 30 | -1.27 | child of the HUD | SELF-TEST |

`DataTargetVisual` sets the callout rotation with `LookRotation(camera - callout)`. `WorldSpaceHud` uses `LookRotation(-(camera - hud))` with pitch locked. `AuraSubtitles` forces `LookRotation(Vector3.forward)`. Whether any of these look mirrored in a headset was not judged.

### 9. Window objects

Frame `Viewport_StructuralCanopyFrame` at `(0, 0, 3.50)`:

| Object | World position | Renderer size | Made of |
|---|---|---|---|
| ViewportHeader_Upper | `(0, 3.45, 3.50)` | `(7.20, 0.45, 0.40)` | Cube, `Mat_Spaceship_Hull` |
| ViewportSill_Lower | `(0, 0.55, 3.50)` | `(7.20, 0.55, 0.40)` | Cube, hull material |
| CanopyMullion_1 / _2 | `x = ±3.40`, `y = 2`, `z = 3.50` | `(0.22, 2.55, 0.35)` | Cubes, hull material |
| PanoramicViewportGlass | `(0, 2.00, 3.55)` | `(6.80, 2.45, 0.03)` | Cube, viewport glass |
| Viewport_BackingCollider | `(0, 2.00, 3.75)` | `(7.20, 3.50, 0.20)` | Cube, renderer off |

Beyond the glass, under `OuterSpace_CelestialEntities`:

- `Starfield_Constellation`: 200 sphere children. Builder places them from `x` -8..8, `y` 0.5..6, `z` 8..28, and assigns `Mat_Space_StarGlow`, `Mat_Glow_Cyan`, or `Mat_Glow_Amber`.
- `Celestial_GasGiantPlanet` at `(2.40, 2.15, 9.50)`, scale 3.4, mesh Sphere, shader `Shader Graphs/SkyPlanet`.
- `PlanetaryRings` on the planet, mesh `pTorus1`, `Mat_Space_PlanetRings`.
- `FloatingAsteroidField`: six children, mesh `pPyramid1`, `Mat_Space_Asteroid`. Positions were moving in Play Mode (about `x` -5.4..4.4, `y` 1.4..4.7, `z` 9.3..17.7). Authored positions in the builder are `(-3, 2.6, 11)`, `(3.2, 1.8, 10)`, `(-4.2, 3.5, 15)`, `(4.5, 3.2, 16)`, `(-5.5, 1.5, 9.5)`, `(2, 4.2, 18)`.
- `Celestial_FaintEarth` at `(8, 3.2, 40)`, scale 0.35, sphere, `Mat_Earth_Water`.
- `PhotoBody_Earth` `(-2.2, 2.4, 14)` scale 0.8 water; `PhotoBody_IceGiant` `(0, 3.1, 16)` scale 1.1 cyan glow; `PhotoBody_Rust` `(2.4, 2.2, 13)` scale 0.7 coral glow.
- `CanopyShutters`: `ShutterLeft` `(-2.85, 1.70, 3.35)` and `ShutterRight` `(2.85, 1.70, 3.35)`, mesh `wall-window-shutters`, non-uniform scale `(0.35, 2.30, 0.20)`, material `colormap`.
- `ShipNose` at `(0, 0.85, 5.40)`, scale `(1.4, 0.6, 2.2)`, Kenney `structure`.
- `CinematicShip` at `(0, 1.45, 8.60)` is inactive. Children Hull, Nose, Wing_L, Wing_R use `Mat_Spaceship_Hull`; Canopy uses cyan glow; Engine_L and Engine_R use amber glow.

### 10. Render pipeline, URP, quality, refresh

Confirmed URP.

| Fact | Value |
|---|---|
| Unity | `6000.6.0f1` |
| Default and quality pipeline | `UniversalRenderPipelineAsset` `PackageSamplesURPAsset` |
| Asset path | `Assets/Samples/Universal Render Pipeline/17.6.0/URP Package Samples/SharedAssets/Settings/PackageSamplesURPAsset.asset` |
| HDR | on |
| HDR color buffer precision | `_32Bits` (URP's 32-bit buffer, which is R11G11B10) |
| MSAA | 2 |
| Render scale | 1 |
| Additional lights per object | 4 |
| Additional light mode | Per Pixel |
| Main light shadows supported | yes |
| Shadow distance on the asset | 20 |
| Quality | index 5, Ultra |
| vSync | 1 |
| `Application.targetFrameRate` | -1 |
| Color space | Gamma (`m_ActiveColorSpace: 0`, and `QualitySettings.activeColorSpace` Gamma) |
| Quest refresh rate | NOT MEASURED |

`ProjectSettings/AGENTS.md` requires Linear color space and an R11G11B10 HDR buffer. Color space is Gamma and was not changed. The live HDR precision `_32Bits` is that R11G11B10 buffer, so the buffer format is not a separate mismatch. No OpenXR or player setting in the repo states 72, 90, or 120 Hz. 72 was not assumed. At an unknown refresh, the 13.9 ms frame budget is not confirmed.

### 11. Kenney mesh bounds, UV2, packs

Pack present under `Assets/ThirdParty/Kenney/`: `SpaceStation` only. Texture file present: `Textures/variation-a.png` only. No `variation-b` or `variation-c`. That texture's importer max size is 2048, sRGB on, mipmaps on, filter bilinear. There is no Android platform override, so ASTC is not set on the importer. Kenney interface and sci-fi audio clips are under `Assets/_Project/Audio/Kenney/`, which is first-party audio, not `ThirdParty`.

Every mesh below has UV0 and does not have UV1 (no lightmap UV set). Bounds are imported local bounds of the named mesh. `structure`, `wall-detail`, `table-display`, and `table-display-planet` also contain extra child meshes; those child meshes also have UV0 and no UV2.

| Module the builder instantiates | Bounds size (x, y, z) | Tris | Verts |
|---|---|---:|---:|
| table-large | 1.400, 0.400, 0.900 | 104 | 112 |
| computer-wide | 0.800, 0.497, 0.533 | 174 | 230 |
| chair-armrest-headrest | 0.500, 0.700, 0.350 | 142 | 172 |
| floor-panel | 1.000, 0.300, 1.000 | 92 | 104 |
| wall-window-frame | 0.600, 0.400, 0.100 | 36 | 56 |
| wall | 1.000, 1.000, 0.300 | 44 | 68 |
| wall-detail | 0.400, 0.700, 0.173 | 80 | 96 |
| wall-pillar | 1.000, 1.000, 0.500 | 88 | 124 |
| wall-door-wide | 1.000, 1.000, 0.300 | 98 | 152 |
| table-display | 1.241, 0.411, 0.816 | 176 | 238 |
| wall-window-shutters | 0.600, 0.400, 0.100 | 44 | 72 |
| balcony-rail | 1.000, 0.400, 0.700 | 214 | 230 |
| balcony-rail-corner | 0.700, 0.400, 0.700 | 124 | 141 |
| floor | 1.000, 0.300, 1.000 | 12 | 24 |
| computer-system | 0.900, 0.600, 0.695 | 176 | 222 |
| structure | several pieces; largest plate 1.000, 0.100, 1.000 | 8–32 each | |
| table-display-planet | table 0.500, 0.100, 0.500; planet 0.190, 0.200, 0.190; ring 0.386, 0.104, 0.400 | 44 / 64 / 32 | |

FBX files exist, and the builder names them, but `CreateModelInstance` is not called for: `wall-corner-round`, `wall-window`, `floor-detail`, `computer-screen`, `structure-panel`, `pipe`, `pipe-ring-colored`, `pipe-bend`. Other files in the same folder (`stairs`, `balcony-floor`, `floor-panel-straight`, `structure-barrier`) are not referenced by the builder.

No meteor or spacecraft pack was found under `Assets/ThirdParty`. Window rocks are a pyramid sample mesh. The planet and stars are Unity spheres.

### 12. Baseline stats

The Editor was already in Play Mode, and static batching had replaced 228 `MeshFilter`s with combined meshes. A naive sum of every filter's index count counts those shared combined meshes once per filter and is not the scene budget. That inflated sum was 1,323,448 triangles (1,307,686 on enabled renderers). It is discarded as a budget number.

| Measure | Value | Label |
|---|---:|---|
| Unique shared meshes, each mesh once | 23,220 tris, 47 meshes | library size, not the scene total |
| MeshFilters whose mesh name is not a combined mesh | 197,690 tris, 367 filters | editor mesh triangle sum, not Stats window. Misses geometry that Play Mode had already moved into the 228 combined meshes |
| UnityStats.triangles | 139,755 | Game view during the existing play session |
| UnityStats.vertices | 97,539 | same |
| UnityStats.drawCalls | 318 | same |
| UnityStats.setPassCalls | 58 | same |
| UnityStats.dynamicBatches | 3 | same |
| UnityStats.staticBatches | 9 | same |
| UnityStats.instancedBatches | 0 | same |
| Screen | 1101×506 | not a headset, not the authored seat |

`UnityEditor.UnityStats` has no single `batches` field. The camera for these stats was the walked play camera, not the authored seat. On-device frame time, draw calls, and triangles: **NOT MEASURED**. A seated edit-mode Stats capture: **NOT MEASURED**.

The 200 star spheres are the bulk of the geometry. One shared `Sphere` mesh is 768 triangles, and 227 sphere filters were in the scene. That alone is above the 100,000 triangle chamber budget in `Assets/AGENTS.md`. This is not a pass.

### 13. Builder map

`Tools/Editor/Level01SceneBuilder.cs` is a partial class. `BuildLevel01` is the public entry. `Tools/Editor/Level01DefenseSetup.cs` adds the rest of the partial class. The scene is generated. Running the builder replaces it.

`BuildLevel01` assigns the URP asset, creates directories, broken presets, and materials, then makes an empty scene and calls the builders below. It saves `Assets/Scenes/Level01_AwakeningGate.unity`, registers that scene in Build Settings, and calls `CaptureBridgeView`.

| Function | What it builds |
|---|---|
| `EnsureDirectories` | Folders the builder writes into |
| `GenerateBrokenPresets` / `CreateOrUpdatePreset` | Opening preset plus presets A–F |
| `GenerateMaterials` / `CreateOrUpdateMaterial` / `CreateOrUpdateUnlitMaterial` | URP materials, including Kenney, hull, glass, glows |
| `PlaceShip` | Builds the hull, saves `PF_Ship_Hull` prefab, instances it, marks renderers static |
| `CreateSpaceshipBridgeHull` | Deck tiles, bulkheads, pillars, aft door, side window glass |
| `CreateBulkheadRib` | Ceiling rib and its downlight |
| `CreateCeilingConduitSystem` | Ceiling conduits |
| `CreateAuxiliaryShipConsoles` | Port and starboard aux stations and their screens |
| `CreateAftShip` | Aft corridor, observatory door, telescope, engine door, jump drive |
| `PlaceModule` / `MakeDoor` | Module placement and named doors |
| `AssignStarfield` | Writes `Assets/Materials/Sky_Starfield.cubemap` and `Sky_Starfield.mat`, assigns the skybox |
| `CreateBloomVolume` | Global volume and, when the profile has no Bloom component, a Bloom override |
| `CreateForwardObservationViewportAndSpaceVista` | Canopy frame, glass, stars, planet, rings, asteroids, photo bodies, shutters, cinematic ship |
| `CreateCinematicShip` / `CreateWing` / `CreateEngine` | Inactive ship outside the window |
| `CreatePhotoBodies` / `MakeBody` | Earth, ice giant, and rust spheres |
| `CreateTactileCommandBridgeRail` | Balcony rails and corners |
| `CreateHazardStripeSegment` | Hazard stripes |
| `CreateDeckRunwayLights` | Floor LED strips |
| `CreateBusTraces` | Deck traces |
| `CreatePerimeterHardware` | Side computers and heat sinks |
| `CreateDataParticles` / `CreateSparkParticles` | Room data motes and the emergency sparks |
| `CreatePlatformTrim` | Trim under the floor ring |
| `CreatePlanetaryAICoreHologram` | Planetary hologram pedestal and its texts |
| `CreateOrbitRing` | Line-renderer rings around the neuron |
| `InstantiateXrRig` | XRI starter rig at the authored start pose, plus `DesktopWalk` |
| `CreateXrInteractionServices` | Interaction manager and event system if missing |
| `AttachXriBridges` / `BridgeAll` | `XRInteractableBridge` on sliders, dial, lever, socket, cables, receptors, voyage buttons |
| `EnsureComfortCollider` | Extra colliders |
| `CreateWorldSpaceHud` / `CreateHudText` / `StretchRect` | World HUD, SELF-TEST button, `UiForwardPassRelay` |
| `BakeNonKeyLights` | Stellar key stays Realtime; every other light is marked Baked. It does not start a lightmap bake |
| `RegisterSceneInBuildSettings` | Enables the Level 01 scene in the build list |
| `CaptureBridgeView` | Editor capture after the build |
| `WirePointDefense` | `AsteroidDefenseDirector`, opening teleprompter, voice player, gate posts |
| `MakeGatePost` | A gate post |
| `ReadVoice` | Reads a voice transcript file |
| `CaptureOpeningPreview` and the opening-capture helpers | Editor preview capture. Not part of the room build |
| `CreateModelInstance` / `GetSampleMesh` / `SetMeshOrKeepPrimitive` / `CreateProceduralTorusMesh` | Mesh loading and the torus fallback |
| `ApplyMaterialRecursively` / `GetSampleAudio` / `GetSampleTexture` | Material and sample lookup |

Inside `BuildLevel01` itself, before those calls return, the method also creates the gameplay object (`NeuralState`, `ChamberController`, `VoyageDirector`, `FailureHintDirector`, `AsteroidDrift`, `SkyPhotoCapture`, `FinaleSequence`), the neuron machine, the flight desk at scale `(1.30, 1, 1)`, both weight sliders, the bias dial, cables, the SELF-TEST lever, and the activation socket.

### 14. Tooling

`Tools/Build-Level01Scene.ps1` starts `C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe` with `-batchmode -nographics -projectPath <repo> -executeMethod Convergence.EditorTools.Level01SceneBuilder.BuildLevel01 -quit -logFile <repo>\Temp\unity_build.log`. It waits and prints the exit code and the last 50 log lines. This recon did not run it. The Editor already has the project open.

`Tools/Validation/Validate-RepositoryLayout.ps1` and `Tools/Validation/Validate-CoreBoundaries.ps1` are PowerShell. They do not launch Unity. Both declare `[switch]$WhatIf`. Neither script body reads `$WhatIf`, so the switch is not a dry run. Layout validation checks directories, instruction files, and asmdef references. Core-boundary validation scans `Assets/Scripts/Core` for forbidden usings. CI notes in the scripts say `pwsh Tools/Validation/Validate-RepositoryLayout.ps1` and `pwsh Tools/Validation/Validate-CoreBoundaries.ps1`.

On this machine `pwsh` is not on PATH. `powershell -NoProfile -File Tools/Validation/Validate-RepositoryLayout.ps1` exited 0 (46 checks passed, 0 violations, 0 warnings). The core-boundary script was not run. WP0 did not change Core.

`Packages/manifest.json` contains `com.unity.test-framework` version `1.4.5`. The Unity Test Framework is installed. `com.unity.render-pipelines.universal` is `17.6.0`.

### Later chambers in the live Level 01 flow

`VoyageDirector` is on the generated gameplay object. After the OR puzzle passes it calls `BeginPuzzle(PuzzleDefinition.CreateXorPuzzle())` (Spectrum Filter: `(1,1) -> 0`). After that passes, the leg is Earth photos (`CreateEarthPhotoPuzzle`, three inputs, Sigmoid). After that grades pass, the leg is nav-log attention (`NavLogAttention.Predict`), then the jump. `InstallHiddenLayer` builds two Step hidden neurons and one Step output. That is in this scene's code. It is not plan section 3.5 chamber 02 (fire only when both inputs are on) and it is not chamber 03 (fire at rock only). Those two hypothesized tables were not found. Separate scenes `Chamber02_XORWall`, `Chamber03_TrainingBay`, and `Chamber04_AttentionCore` are named in `Assets/Scenes/AGENTS.md` and were not the loaded scene.

## Per-WP log

- WP0: Read-only recon. No game files changed. Evidence is this RECON, measured from the open Editor and from source. No captures. The Editor was already in Play Mode, so live numbers are labeled as such. Authored poses come from the saved scene and the XR rig prefab.
- WP1: Tooling only, reviewed and kept. Files: `Tools/Editor/CaptureChamberViews.cs`, `Tools/Editor/Level01SceneBuilder.cs` (capture markers and `DevHeadsetTextStrip` only), `Tools/Verify-Chamber01.ps1`, `Tools/chamber01-thresholds.json` (`calibrated` false), `Tools/Editor/WP1-ASSUMPTIONS.md`. `CaptureChamberViews.CountMaterials` calls `GetEntityId()` because `GetInstanceID()` is CS0619 in Unity 6000.6.0f1. CLI rebuild via `Tools/Build-Level01Scene.ps1` (`Unity.exe` `-batchmode -nographics -projectPath` this repo `-executeMethod Convergence.EditorTools.Level01SceneBuilder.BuildLevel01 -quit -logFile Temp/unity_build.log`). Exit code 0. Log `Temp/unity_build.log` contains `[Level01SceneBuilder] Level 1 generated and saved successfully`. Saved scene `Assets/Scenes/Level01_AwakeningGate.unity` contains `ConsoleBounds`, `WindowBounds`, `ValueProbe_Ceiling`, `ValueProbe_Floor`, `ValueProbe_Wall`, and `DevHeadsetTextStrip`. After that Unity process exited, `Tools/Verify-Chamber01.ps1 -Wp B0` wrote `Documentation/Design/captures/verify_B0.log`: V-01 PASS, V-02 PASS, V-05 SKIP (no captures), V-06 SKIP (no captures), V-10 FAIL on the pre-existing `BlitToRTHandleRenderer.asset` under `Assets/Samples/` (this package did not touch it). Verify exit code 1. RESULT: FAIL (1 fail, 0 warn, 2 skip). Captures NOT MEASURED. `CaptureChamberViews` has `DumpHierarchy` and the Play Mode menu `Tools/Chamber 01/Capture Views` only; no batch capture entry was added. No PNGs, no `metrics.json`, no `editor_numbers.json`. Headset text strip NOT MEASURED. GATE A is not ready.
- WP2: Readability. Files: `Tools/Editor/Level01SceneBuilder.cs`, `Tools/Editor/Chamber01TextRules.cs`, `Tools/Verify-Chamber01.ps1` (V-03), `Documentation/Design/ART_BIBLE_CHAMBER01.md`, `Documentation/Design/captures/text_audit.json`, Presentation text and palette scripts, `ChamberOnboardingController` prompts, generated scene, hull prefab, and first-party materials including `Mat_Chamber01_Ceiling`. Command: `powershell -NoProfile -File Tools\Verify-Chamber01.ps1 -Wp WP2`. Exact log `Documentation/Design/captures/verify_WP2.log`: V-01 PASS (2 builds, 0 diffs, 772 lines), V-02 PASS (powershell.exe; core-boundary passed), V-03 PASS (11 texts, 0 violations), V-05 SKIP (editor numbers file missing), V-06 SKIP (metrics file missing), V-10 FAIL on the pre-existing `BlitToRTHandleRenderer.asset` under `Assets/Samples/` (not touched). RESULT FAIL (1 fail, 0 warn, 2 skip). Glare bisect pixels NOT MEASURED, so the subtractive set was applied together: bloom override removed and not re-added, non-key lights disabled, hull/floor smoothness 0.25, metallic at or below 0.3, console glow emission cut from a 1.4 multiplier to 0.7. No light was added back. Headset text strip NOT MEASURED. Thresholds stay `calibrated: false`. A stale empty `Temp/UnityLockfile` had no Editor process; it was deleted so the CLI could run. No process was killed. Deviations: static audit skips `DevHeadsetTextStrip`; B0 window words were runtime billboards and are not spawned; star emission was lowered here and the spheres stay until WP10; value-probe luminance NOT MEASURED. GATE A and GATE B are not ready.
- WP3: Visible concept. Files: `Tools/Editor/Chamber01Concept.cs`, `Assets/Scripts/Presentation/NeuronCauseVisual.cs`, `Assets/Materials/Chamber01NeuronBoundary.shader`, generated icons and materials, `Level01SceneBuilder.cs` calls `Chamber01Concept.Install` before the text pass. The diagram parents to `NeuronDisplayAnchor` under the console. Wires, cards, and the boundary read `NeuralState` and `LastEvaluation`. Presentation does not call `Evaluate`. Command: `powershell -NoProfile -File Tools\Verify-Chamber01.ps1 -Wp WP3`. Log: V-01 PASS (2 builds, 0 diffs, 809 lines), V-02 PASS, V-03 PASS (12 texts, 0 violations), V-05 SKIP, V-06 SKIP, V-10 FAIL on the pre-existing Samples blit asset. RESULT FAIL (1 fail, 0 warn, 2 skip). Play Mode frame timing of the cause update is NOT MEASURED. The refresh runs from `OnStateMutated`. Card correctness marks stay hidden until an evaluation exists; live re-evaluation is WP4. No GATE READY.
- WP4: Feedback. `LiveEvaluationRelay` calls `TriggerForwardPass` on `OnStateMutated` and searches one-step hints with `DeepCopy` plus `PuzzleEvaluator.Evaluate`. `ChamberFeedbackVisual` runs the amber/cyan timeline, term lines, synthesized tones, the cable glow, and the engage label. `Chamber01StateTest` writes `state_test.json`. Command: `powershell -NoProfile -File Tools\Verify-Chamber01.ps1 -Wp WP4`. Log: V-01 PASS (810 lines), V-02 PASS, V-03 PASS (12 texts), V-07 PASS (flashHz 0.5, damage elements 0), V-05 SKIP, V-06 SKIP, V-10 FAIL on the pre-existing Samples blit asset. RESULT FAIL (1 fail, 0 warn, 2 skip). Headset comfort of the payoff is NOT MEASURED. Damage props are not in yet, so the count is 0. No GATE READY.
- WP5: Playtest kit. `Documentation/Design/PLAYTEST.md` has the script, the sheet, and a blank results table. `PlaytestEventLog` appends lines in the editor and development builds only. Human sessions are NOT MEASURED. Gate B was not passed by players. Later packages still continue because the user ordered the full plan on 2026-10-06. Command: `powershell -NoProfile -File Tools\Verify-Chamber01.ps1 -Wp WP5`. Log: V-01 PASS (810 lines), V-02 PASS, V-03 PASS, V-07 PASS (flashHz 0.5, damage 0), V-05 SKIP, V-06 SKIP, V-10 FAIL on the pre-existing Samples blit asset. RESULT FAIL (1 fail, 0 warn, 2 skip).
## Core Change Proposal (WP6)

Puzzle data lives in `CurriculumPuzzle`: id, title, input names, output name, the existing `PuzzleDefinition` cases, hidden-unit count, prompts, term reveals, unlock rule, available controls, and a reference weight vector. Evaluation stays `PuzzleEvaluator.Evaluate` on a `NetworkModel`. Hypothetical weights are a copied single neuron or an explicit hidden-layer network passed to that same method. Chamber 01 uses the OR cases already in `CreateORGatePuzzle` (fire comet, rock, and both; hold the drone) with reference weights 1, 1, -0.5 and Step. Chambers 02 and 03 are more rows in the same catalog and reuse this room. Chamber 04 is XOR: a grid search from -2 to 2 step 0.5 finds no single-neuron solution, and the reference solution is the OR cartridge, the AND cartridge, and an output that fires on the first hidden unit and not the second. Risks: a voyage that still jumps straight to the old XOR leg until WP7, and Presentation strings that must read names from the catalog. Tests: each single-neuron reference passes, the XOR grid fails, and the hidden XOR reference passes. Positive, negative, zero, and the step boundary are covered.

CCP APPROVED by user order to complete the full plan on 2026-10-06

- WP6: Data-driven puzzles after the proposal above. `CurriculumCatalog` holds chambers 01 to 04. `RunReferenceChecks` passes OR, AND, and rock-not-ice, reports XOR unsolvable on the -2..2 step 0.5 grid, and passes the hidden XOR network. NUnit coverage is `Tests/EditMode/Core/CurriculumPuzzleTests.cs`. `Validate-CoreBoundaries.ps1` exited 0 (14 files, 0 violations). Command: `powershell -NoProfile -File Tools\Verify-Chamber01.ps1 -Wp WP6`. Log: V-01 PASS (810 lines), V-02 PASS, V-03 PASS, V-07 PASS, V-08 PASS, V-05 SKIP, V-06 SKIP, V-10 FAIL on the Samples blit asset and the still-uncommitted `CurriculumPuzzle.cs`. RESULT FAIL (1 fail, 0 warn, 2 skip). The Core file is this WP and is committed with it. The Samples file is not. No GATE READY.
- WP7: Chambers 02 and 03. After chamber 01 the voyage loads chamber 02, then 03, then the XOR row, and resets the neuron to the crisis preset. Solved networks are stored on `SavedCircuitLibrary`. Term lines come from the catalog. Earth photos still follow a passed XOR. Command: `powershell -NoProfile -File Tools\Verify-Chamber01.ps1 -Wp WP7`. Log: V-01 PASS (810 lines), V-02 PASS, V-03 PASS, V-07 PASS, V-08 PASS, V-05 SKIP, V-06 SKIP, V-10 FAIL only on the pre-existing Samples blit asset. RESULT FAIL (1 fail, 0 warn, 2 skip). Play Mode of the three chambers is NOT MEASURED. No GATE READY.
- WP8:
- WP9:
- WP10:
- WP11:
- WP12:
- WP13:

## Metrics tables

Glare bisect pixels are NOT MEASURED. WP2 applied the subtractive set without a golden frame.

| Measure | Value | Notes |
|---|---|---|
| Authored-seat frustum renderers | 339 of 601 | 16:9 assumed, fov 60, authored eye |
| Unique materials in that frustum | 41 | includes play-mode instances |
| Editor mesh triangle sum, not Stats window | 197,690 | excludes 228 play-mode combined meshes, so it is incomplete |
| UnityStats triangles / setPass / drawCalls | 139,755 / 58 / 318 | existing play session, walked camera, 1101×506 |
| On-device | NOT MEASURED | |

## Not measured

- Quest refresh rate, and therefore the frame time at that rate. Not assumed to be 72 Hz.
- On-device frame time, batches, triangles, and thermal behaviour.
- A seated edit-mode Stats window. The Game view stats above are from a play session whose camera had already moved.
- Whether `LightingData` is a complete, current bake.
- Visual judgment of mirrored text. No capture was taken, and the callouts were billboarding toward a walked camera.
- Headset text-strip angles. The strip is in the scene. A person has not read it.
- WP2 glare-bisect pixels, golden V1, and value-probe luminance. `calibrated` is still false.
- Whether the walked play session had already changed asteroid positions away from the builder's authored points. Authored points are listed from the builder; live points were moving.

## Contradictions found

**Do not STOP.** The pipeline is URP, and the Chamber 01 truth table matches plan section 3.5.

1. Color space is Gamma. `ProjectSettings/AGENTS.md` requires Linear. Not changed.
2. Withdrawn. URP's `_32Bits` HDR precision is the R11G11B10 buffer `ProjectSettings/AGENTS.md` asks for. Not a contradiction.
3. `Assets/Scenes/AGENTS.md` lists `Chamber01_SignalFoundry.unity`. The live scene is `Assets/Scenes/Level01_AwakeningGate.unity`. The same file also lists separate chamber scenes that are not the loaded Level 01 flow.
4. The saved bloom profile has no Bloom component, and the main camera has no URP camera-data component. The plan's glare note assumes bloom is on. The builder knows how to add Bloom, but the asset it would edit is empty today.
5. Every Kenney mesh the builder instantiates has no second UV set. Plan rule 10 treats missing lightmap UVs as a stop. WP0's stop rule for this recon is only the pipeline and the Chamber 01 table, so work continues, and WP11 cannot bake lightmaps by editing `ThirdParty`.
6. Plan section 3.5 chambers 02 and 03 (fire only at both; fire at rock and not ice) are not in the live voyage. The live voyage after OR is XOR, then Earth-photo training, then nav attention, in this same scene.
7. `variation-a.png` imports at 2048 with no Android ASTC override. `Assets/AGENTS.md` wants hero textures at 1024, secondary at 512, ASTC. The file was not modified.
8. The flight desk is scaled `(1.30, 1, 1)` and uses `Mat_Mainframe_DarkPlating`, not the Kenney atlas material. Hull smoothness is 0.85 and metallic is 0.05, against the plan's later guess of 0.25 and 0.3. The atlas texture is assigned on `Mat_Kenney_SpaceStation`, and chairs and deck tiles use that material. The "atlas never assigned" note in the plan's diagnosis is only true of the hull and the desk.
9. Both validator scripts declare `-WhatIf` and never read it.
10. `Assets/Scripts/AGENTS.md` still says the Presentation `AGENTS.md` does not exist. The file exists.
11. Slider `sensorName` values still say "Earth Continental Sensor" and "Earth Atmosphere Sensor". Visible copy and the truth table say ROCK and ICE.
12. Scene triangle weight is above the 100,000 budget largely because stars are Unity spheres. See the stats section. This is not marked as a pass.

## Proposals

Out of scope for WP0. Not implemented.

- Live cards and the hint search need a Gameplay method that deep-copies `NetworkModel` and calls the existing `Evaluate`. Presentation must not call `Evaluate`. A new Core hypothetical API is only the WP6 proposal, and only after a human writes `CCP APPROVED`.
- WP11 should default to no lightmaps. The Kenney meshes have no UV2, and `ThirdParty` stays untouched unless a human approves an import-setting exception.
- `variation-a.png` is already 2048. Do not upscale it. An ASTC / 1024 import change touches a ThirdParty texture and needs a scoped exception.
- Window archetypes cannot come from a Kenney meteor pack. That pack is not in the repo. WP10's procedural fallback is the path that matches the files on disk.
- The room data-stream particles and the 200 star spheres are the things to remove or move before adding any new particles.

## Open questions

1. Design intent: the code's Chamber 01 table matches section 3.5. A human still needs to say if that intent has changed.
2. Is a Quest 2 and a build path available for the on-device gate? Not known from this repo.
3. Is the target refresh 72, 90, or 120 Hz? Not stored in the settings that were searched. Do not pick 72 by default.
4. Does "amber" in `Assets/Materials/AGENTS.md` mean a negative outcome, or a negative number? The plan's section 4.2 reading was not confirmed by a human.
5. After Gate B, is the order depth first (WP6–WP8) or art first (WP9–WP11)?
6. Chambers 02–04 in the plan are new puzzle data. The live game already continues in this room through XOR, Earth photos, and attention. Should those voyage legs stay, or does the plan replace them?
