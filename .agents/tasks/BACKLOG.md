# Agent Task Backlog

Reconciled 2026-09-29 with the code and with ADR-008.

## Dropped by ADR-008

Do not mark these done. They are out of the live game.

- Sentry waves, friendly-fire turret, and purge death (`TASK_SENTRY_DEFENSE_AND_TACTICAL_PRESSURE_OVERHAUL.md`, `TASK_LIVE_WAVE_DEFENSE_AND_PURGE_COUNTDOWN.md`).
- Neural gun viewmodel.
- Motherboard room and Synapse-GPT copy.

## Already in the code

- Activation functions, loss functions, neuron / layer / network models, and the OR puzzle evaluator.
- XRI starter rig, world-space HUD, and desktop walk that steps aside for a headset.
- Deterministic SGD, gradient hooks, multi-output scoring, and scaled dot-product attention.
- Chamber 02 hidden layer, photo filing, and nav-log masking, wired by `VoyageDirector`.

## Still open

- [ ] A person plays Chamber 01 over Quest Link: look, walk into a wall, snap turn, teleport, ray, grab, HUD, then the OR solve.
- [ ] Confirm the built scene stays under 100 draw calls and 100k triangles from a standing position.
- [ ] Finish a blocking lightmap bake if `Lightmapping.BakeAsync` did not complete in the editor session.
