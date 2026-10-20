# Task: Bridge declutter and point-defense scene wiring

Status: complete
Owner: Tools
Created: 2026-10-03
Updated: 2026-10-03

## Description

Rebuild Level 1 from `Tools/Editor/Level01SceneBuilder.cs`. Add the opening preset, remove the extra text boards, rebuild the asteroid corridor, and wire the director, teleprompter, voice player, impact sequence, and point-defense visual. The scene file is a build output. Do not hand-edit it.

## Allowed paths

- `Tools/Editor/Level01SceneBuilder.cs`
- `Tools/Editor/Level01DefenseSetup.cs`
- `Assets/Puzzles/Chamber01/BrokenConfig_Opening.asset`
- `Assets/Scenes/Level01_AwakeningGate.unity` as a build output only

## Forbidden paths

- `Assets/Scripts/Core/**`
- `ProjectSettings/**`
- `Packages/**`
- Hand-editing `.meta` files or the generated scene

## Acceptance criteria

- [ ] `BrokenConfig_Opening` is `presets[0]`, both cables disconnected.
- [ ] Pedagogy board, diagnostic matrix, engineer tablet, pod screens, rails, and pedestals are gone.
- [ ] Console labels read ROCK SENSOR, ICE SENSOR, TRIGGER BIAS, and FIRE. The sign reads NEURAL.
- [ ] `externalWaveDirector`, soft reroute, and deferred gateway are on. Sandbox mode and evaluation sounds are off.
- [ ] The scene rebuilds from the builder in batchmode.

## Blocked on

None.

## Handoff notes

Full spec is `HANDOFF_ASTEROID_DEFENSE.md` Step 4.
