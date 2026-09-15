# ADR-006: Align Package Versions with Unity 6000.6.0f1 Built-in Distributions

| Field | Value |
|---|---|
| ID | ADR-006 |
| Date | 2026-09-15 |
| Status | Accepted |
| Deciders | Master Agent, Architecture |

## Context

Unity 6000.6.0f1 ships with built-in versions of Core packages including `com.unity.render-pipelines.universal` at version 17.6.0 and `com.unity.shadergraph` at version 17.6.0.

In `Packages/manifest.json`, `com.unity.render-pipelines.universal` was pinned to an older version `17.0.3`. When importing assets or building scenes in batchmode, this version discrepancy caused asset import crashes in `ShaderGraphImporter` during deserialization of node UI views. Aligning package versions with the exact Unity 6000.6.0f1 distribution resolves importer stability and ensures compatibility across URP and ShaderGraph.

## Decision

- Update `com.unity.render-pipelines.universal` in `Packages/manifest.json` to `17.6.0` to match Unity 6000.6.0f1's built-in package distribution.
- Ensure all Core and Editor dependencies resolve against the native Unity 6000.6.0f1 package suite.

## Consequences

- Eliminates version mismatches between URP and ShaderGraph in Unity 6000.6.0f1.
- Restores clean asset importation in batchmode and interactive editor sessions.

## Supersedes

Nothing.
