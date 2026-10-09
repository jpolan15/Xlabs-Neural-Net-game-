# ArtSource — Source Art (not imported by Unity)

Raw and work-in-progress art for the Asset Forge pipeline (`.agents/skills/asset-forge/SKILL.md`).
It lives outside `Assets/` on purpose: Unity imports everything under `Assets/`, so 50 MB raw meshes and concept PNGs here would slow every import and build.

## Layout

| Path | Purpose |
|---|---|
| `ArtSource/_TEMPLATE/brief.md` | Copy this to start a new asset |
| `ArtSource/<asset_id>/` | One folder per asset (snake_case): `brief.md`, `concept/`, `views/`, `raw/`, `clean/`, `verify/` |
| `ArtSource/voice_bakeoff/` | Voice candidates (anonymized A/B/C/D) and the locked `VOICE_SPEC.md` (master plan WP4) |
| `ArtSource/voice/` | The narration pipeline: `render_voices.py` (script → `vo_ride_NN.wav` + sidecars, transcription check, install), its README, and engine backends. Model weights and render candidates are git-ignored |
| `ArtSource/archival/` | The JFK and Apollo 11 recordings: sources, licenses, trim points and the processing script (`SOURCES.md`) |
| `ArtSource/sfx/` | `make_sfx.py`: derives the ride's loops and buzzer from the CC0 Kenney packs and prints the loudness table the builder's effect levels come from |

## Rules

- Only a mesh that passed Gate B moves to `Assets/_Project/Models/<AssetId>/`.
- Every generated file keeps its prompt / settings / tool + license next to it.
- Nothing here ships in the game binary.
