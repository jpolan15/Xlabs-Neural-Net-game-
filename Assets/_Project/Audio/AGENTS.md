# Audio Assets — Scope Rules

This is the canonical location for Convergence audio assets.

> **Migration status**: Migrated from `Assets/AudioAssets/` as part of Phase 6 restructure (2026-09-14).  
> The old `Assets/AudioAssets/` directory retains a redirect notice.  
> When adding new audio assets, add them here — not to the old location.

## Contents

| Type | Convention |
|---|---|
| Audio clips | `.wav` or `.ogg` for SFX; `.ogg` for music loops |
| Mixer assets | `AudioMixer` assets with exposed parameters |
| Ambience | Looping spatial audio for chambers |
| Haptic profiles | ScriptableObject-based haptic frequency descriptors |

## Quest 2 audio constraints

- Compress SFX to Vorbis at quality 70 for mobile builds.
- Maximum simultaneous audio sources: 32.
- Use 3D spatial blend for all diegetic sounds.
- Use 2D blend for UI and haptic trigger feedback.

## Rules

- Do not place audio assets in `Assets/AudioAssets/` — that directory is deprecated.
- Do not place audio C# scripts here; those belong in `Assets/Scripts/Presentation/`.
- Do not move files outside the Unity Editor — `.meta` files must be preserved.
