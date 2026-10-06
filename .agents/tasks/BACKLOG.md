# Agent Task Backlog

Reconciled 2026-09-29 with the code and with ADR-008.

## Superseded by ADR-011 (2026-10-06)

The Chamber 01 console tasks (bridge room, cables, activation crystal, SELF-TEST lever, planet hologram, Earth-photo leg, attention leg, hull and death loop) are out of the live game. The Neural Ride is the build's first scene.

Open for the ride: ship intro and outro scenes with the 3-question quiz and credits, tunneling vignette and comfort settings, Quest Link run and frame numbers, stretch XOR stop (hidden layer), playtest with 3 to 5 classmates using `Documentation/Design/PLAYTEST.md`.

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
