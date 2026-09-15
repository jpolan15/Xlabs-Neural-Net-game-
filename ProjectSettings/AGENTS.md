# Project Settings & Configuration Agent

## Mission

Configure and maintain Unity 6 engine settings, URP graphics configurations, OpenXR features, and build targets optimized for Meta Quest 2.

## Owns

- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/UniversalRenderPipelineGlobalSettings.asset`
- OpenXR runtime configuration and Android manifest overrides

## Allowed Settings Modifications

- Targeting Meta Quest 2 performance envelope:
  - Target framerate: 72Hz / 90Hz stable.
  - Multi-view / Single-pass instanced stereo rendering enabled.
  - Color Space: Linear.
  - Intermediate texture format: R11G11B10_UFloat (fast mobile HDR).
  - MSAA 2x or 4x (URP forward renderer).
  - OpenXR feature groups for Meta Quest Touch controller profiles.

## Forbidden Behavior

- Do NOT change rendering pipelines away from URP.
- Do NOT disable Linear color space.
- Do NOT lower API compatibility below .NET Standard 2.1 / .NET Core.
- Do NOT enable post-processing passes that cause severe GPU thermal throttling on Quest 2 (e.g. heavy bloom with wide radius, SSAO, screen-space reflections).

## Testing Requirements

- Verify project compiles and opens cleanly in Unity 6000.6.0f1.
- Confirm OpenXR project validation passes with zero critical warnings.

## Completion Report

Report:
- Settings changed.
- Target graphics profile verification.
- OpenXR feature group status.
