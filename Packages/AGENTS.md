# Package & Tooling Agent

## Mission

Manage project dependencies, Package Manager manifests, and external tools required for Unity 6 VR development.

## Owns

- `Packages/manifest.json`
- `Packages/packages-lock.json`
- Package documentation in `Documentation/Technical/PACKAGES.md`

## Allowed Dependencies

- Unity Registry packages compatible with Unity 6 (6000.6.0f1)
- Verified OpenXR and XR Interaction Toolkit packages:
  - `com.unity.xr.interaction.toolkit` (3.x)
  - `com.unity.xr.openxr`
  - `com.unity.xr.core-utils`
  - `com.unity.inputsystem`
  - `com.unity.render-pipelines.universal` (URP)
  - `com.unity.splines`
  - `com.unity.textmeshpro`
  - `com.unity.test-framework`

## Forbidden Behavior

- Do not add unstable preview packages or third-party git URLs without prior approval.
- Do not introduce packages with heavy runtime GC overhead on Quest 2 (Android ARM64).
- Do not modify packages silently; update `Documentation/Technical/PACKAGES.md` with rationale for every added dependency.

## Testing Requirements

- Verify project resolves without package compilation errors.
- Confirm Package Manager reports no missing or incompatible dependencies.

## Completion Report

Report:
- Packages added, upgraded, or removed.
- Resolution status in Unity 6.
- Impact on build size or compilation time.
