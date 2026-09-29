# Handoff Report

## Agent / subsystem

Gameplay, Core, Presentation, XR, Tools/Editor

## Task

Phases 1 to 5 in `HANDOFF_FUN_REBUILD_PHASES_1_TO_5.md`

## Status

complete

## What was done

ADR-008 locks the lost-in-space premise. Chamber 01 is the OR sensor array. W1, W2, and bias are independent. The Earth-puzzle fallback is gone. Sentry, gun, motherboard, and the old 3-4-2 visualizer scripts are deleted. `NeuronTopologyVisualizer` draws the live `NetworkModel`.

The hull prefab `Assets/_Project/Prefabs/Ship/PF_Ship_Hull.prefab` is Kenney modules plus a corridor, three doors, the observatory controls, and the engine-room mask button. The starfield cubemap and bloom profile were written. CC0 Kenney interface and sci-fi sounds are under `Assets/_Project/Audio/Kenney/`.

Core scores every output, trains with deterministic SGD, and has scaled dot-product attention. `VoyageDirector` moves from OR to XOR, photo training, attention, and the jump. A single neuron cannot pass XOR. Masking NOISE is what predicts JUMP_HOME.

The first builder run spent several minutes in `Lightmapping.BakeAsync` and then saved `Assets/Scenes/Level01_AwakeningGate.unity`. That call is removed so the next build can finish without locking the Editor. Bake lightmaps from the Lighting window when you want them.

## Files changed

- `Documentation/Architecture/ADRs/ADR-008-premise-lost-in-space.md`
- `.agents/DECISIONS_INDEX.md`
- `Documentation/Design/GDD_OVERVIEW.md`
- `Documentation/Design/LEVEL_DESIGN_L01.md`
- `Documentation/Mathematics/FORMULAS.md`
- `Assets/Puzzles/Chamber01/AGENTS.md` and Chambers 02–04
- `Assets/Scripts/Core/Math/Attention.cs`
- `Assets/Scripts/Core/Math/LossFunctions.cs`
- `Assets/Scripts/Core/Neural/NeuronModel.cs`, `LayerModel.cs`, `NetworkModel.cs`
- `Assets/Scripts/Core/Training/StochasticGradientDescent.cs` (replaces `TrainingStub.cs`)
- `Assets/Scripts/Core/Puzzles/PuzzleEvaluator.cs`, `PuzzleDefinition.cs`, `NavLogAttention.cs`
- `Assets/Scripts/Gameplay/NeuralState.cs`, `ChamberController.cs`, `VoyageDirector.cs`, `ShipDoor.cs`, `FailureHintDirector.cs`, `AsteroidDrift.cs`, `PhotoRequest.cs`, `VoyageButton.cs`
- `Assets/Scripts/Presentation/NeuronTopologyVisualizer.cs` and the deleted sentry, gun, motherboard, and 3-4-2 visualizer
- `Assets/Scripts/XR/XRInteractableBridge.cs`
- `Tools/Editor/Level01SceneBuilder.cs`
- `Tools/Editor/Convergence.EditorTools.asmdef` (URP core reference for the volume)
- `Tests/EditMode/Core/AttentionAndTrainingTests.cs`
- `Tests/PlayMode/Gameplay/Level01IntegrationTests.cs`
- `Tests/PlayMode/XR/SensorPingTests.cs`
- `Assets/_Project/Prefabs/Ship/PF_Ship_Hull.prefab`
- `Assets/Materials/Sky_Starfield.cubemap` and `.mat`
- `Assets/Materials/Volume_Bloom.asset`
- `Assets/_Project/Audio/Kenney/`

## Tests run

| Test | Command | Result |
|---|---|---|
| EditMode | Unity Test Runner via MCP, job 95fc399f9e304a67a922d2ac68a8f2d0 | 55 passed, 0 failed |
| PlayMode | Unity Test Runner via MCP, job 5a3f316065ec477a90db54cf9bc2f020 | 7 passed, 0 failed |

## Validation run

| Validator | Command | Result |
|---|---|---|
| Core boundaries | `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validation/Validate-CoreBoundaries.ps1` | 13 files, 0 violations |
| Repository layout | `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validation/Validate-RepositoryLayout.ps1` | 47 checks, 0 violations |

## Dependencies added

None. `Convergence.EditorTools` now references the existing `Unity.RenderPipelines.Core.Runtime` assembly so the bloom volume type resolves.

## Known limitations

- The saved scene contains the ship prefab, the two-input neuron, the voyage director, the starfield, and the bloom volume. Lightmap baking was started and then interrupted by a domain reload, so the bake may be incomplete.
- A person has not played Chamber 01 over Link.
- Draw calls and triangle count were not measured.
- Lightmaps were not baked. The key light is marked realtime and the others baked when the builder runs. Bake from the Lighting window.
- Step activation has a zero derivative, so training uses Sigmoid or ReLU.
- Photo features come from a 16×16 render of the body that was hit.

## Cross-boundary requests

None. The owner asked for all five phases in one pass, so the chamber sequence was allowed to touch Core, Gameplay, Presentation, and XR under `TASK_P5_CHAMBERS.md`.

## Next task

Play the sensor bay over Link. Re-run the Level 1 builder only if you want a fresh scene after further edits.
