# Assets/ThirdParty — Third-Party Assets

This directory contains third-party models, textures, and assets used within Convergence.

## Contents

- `Assets/ThirdParty/Kenney/SpaceStation/`:
  - Open-source modular sci-fi space station and bridge models and textures by Kenney (https://kenney.nl/assets/space-station-kit).
  - License: CC0 1.0 Universal (Public Domain Dedication).
  - Used for the Level 1 Spaceship Bridge environment, consoles, viewports, and structural architecture.
- Starfield cubemap `Assets/Materials/Sky_Starfield.cubemap`:
  - Generated in the editor by `Level01SceneBuilder.AssignStarfield`. It is original work, not a downloaded sky texture.
  - Dedicated to CC0 1.0 Universal. No third-party sky texture is imported.
- Kenney Interface Sounds and Sci-fi Sounds, when present under `Assets/_Project/Audio/Kenney/`:
  - License: CC0 1.0 Universal. Source: https://kenney.nl/assets/interface-sounds and https://kenney.nl/assets/sci-fi-sounds.

## Rules

- All assets placed here must have clear open-source or commercial permissive licensing (e.g. CC0, MIT, Apache 2.0).
- Do not modify original vendor source assets directly unless adapting materials for URP.
- Keep polygon counts and texture resolutions compliant with Quest 2 VR performance limits (under 100k total scene tris).
