# ArtSource — Source Art (not imported by Unity)

Raw and work-in-progress art for the Asset Forge pipeline (`.agents/skills/asset-forge/SKILL.md`).
It lives outside `Assets/` on purpose: Unity imports everything under `Assets/`, so 50 MB raw meshes and concept PNGs here would slow every import and build.

## Layout

| Path | Purpose |
|---|---|
| `ArtSource/_TEMPLATE/brief.md` | Copy this to start a new asset |
| `ArtSource/<asset_id>/` | One folder per asset (snake_case): `brief.md`, `concept/`, `views/`, `raw/`, `clean/`, `verify/` |
| `ArtSource/voice/` | The narration pipeline: `render_voices.py` (script → `vo_ride_NN.wav` + sidecars, transcription check, install), its README, and engine backends. Model weights and render candidates are git-ignored |
| `ArtSource/sfx/` | `make_sfx.py`: derives the ride's loops and buzzer from the CC0 Kenney packs and prints the loudness table the builder's effect levels come from |
| `ArtSource/training/` | `bake_level1.py`: trains Level 1's tiny 49-16-10 network on real MNIST (NumPy, deterministic, fails loudly) and writes `Assets/_Project/Data/Level1/level1_digits.json`. The MNIST download cache `mnist/` is git-ignored. MNIST: LeCun & Cortes, CC BY-SA 3.0 |

## Rules

- Only a mesh that passed Gate B moves to `Assets/_Project/Models/<AssetId>/`.
- Every generated file keeps its prompt / settings / tool + license next to it.
- Nothing here ships in the game binary.
