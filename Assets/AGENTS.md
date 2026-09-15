# Assets — Scope Rules

This directory contains all Unity asset content for the shipped Convergence game.  
Development tooling, test infrastructure, and agent instructions do not belong here.

## Directory layout

| Path | Purpose |
|---|---|
| `Assets/_Project/` | First-party assets: Audio, Materials, Prefabs, Scenes, ScriptableObjects |
| `Assets/_Project/Audio/` | **Canonical** audio assets location — see `_Project/Audio/AGENTS.md` |
| `Assets/Scripts/` | All C# source code — see `Assets/Scripts/AGENTS.md` |
| `Assets/Puzzles/` | Per-chamber puzzle data: test cases, configuration, rules |
| `Assets/Scenes/` | Unity scenes |
| `Assets/Prefabs/` | Shared prefabs |
| `Assets/Materials/` | Shared URP materials and shaders |
| `Assets/ScriptableObjects/` | Shared ScriptableObject data assets |
| `Assets/AudioAssets/` | **Deprecated** — redirect to `Assets/_Project/Audio/` |
| `Assets/Samples/` | SDK sample imports — do not modify |
| `Assets/ThirdParty/` | Third-party plugin assets — do not modify |

## Mobile asset constraints (Quest 2)

- Textures: max 1024×1024 for hero props, 512×512 for secondary surfaces; ASTC compression.
- Meshes: interactive props under 3,000 tris; total chamber under 100,000 tris.
- Materials: URP Lit or Simple Lit; minimize unique materials for SRP batcher compatibility.
- Do not import uncompressed 4K textures or raw high-polygon CAD models.

## Rules

- Do not place development tooling, validators, or agent scripts in `Assets/`.
- Do not modify files in `Assets/Samples/` or `Assets/ThirdParty/`.
- Do not move Unity assets outside the Unity Editor — `.meta` files must be preserved.
- Do not dump loose assets into the root `Assets/` folder.
- Do not duplicate identical materials or textures across folders.
- Dependency direction: `Core → Gameplay → XR / Presentation`. See `Documentation/Architecture/DEPENDENCY_RULES.md`.

