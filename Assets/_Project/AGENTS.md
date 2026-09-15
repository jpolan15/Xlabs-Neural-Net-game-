# Assets/_Project — First-Party Assets

This directory holds all first-party project assets, organized by type.  
It distinguishes Convergence content from third-party SDK samples and imports.

## Directory layout

| Path | Purpose |
|---|---|
| `_Project/Audio/` | Audio clips, mixers, and audio-related assets |
| `_Project/Materials/` | URP materials and textures |
| `_Project/Prefabs/` | First-party prefabs |
| `_Project/Scenes/` | Unity scene files |
| `_Project/ScriptableObjects/` | Data assets for puzzle configs, facility settings, etc. |

## Rules

- Do not place third-party or SDK-generated assets here.
- Do not move files outside the Unity Editor — `.meta` files must be preserved.
- Each subdirectory's `AGENTS.md` defines its own asset conventions.
- Canonical audio assets location: `Assets/_Project/Audio/`
