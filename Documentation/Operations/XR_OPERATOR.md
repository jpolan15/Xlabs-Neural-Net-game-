# Meta XR Operator — Development Tool Reference

Meta XR Operator is **experimental development tooling**. It is not a shipped game dependency.

Meta XR Operator must not be required for:
- Core tests
- Gameplay tests
- Building the game
- Running the game
- Opening or editing normal Unity scenes

---

## What it is

Meta XR Operator is an OpenXR-layer tool that lets an MCP-compatible agent inspect and control a running Unity application. It can operate with:
- Unity Editor (Play Mode)
- Meta XR Simulator (desktop OpenXR runtime)
- A physical Quest 2 headset (via Link or standalone build)

It is intended for agent-driven smoke tests, automated UI validation, and development iteration — not for production use.

---

## Compatibility

Last verified: 2026-09-14

| Component | Required version |
|---|---|
| Unity | 6000.0.x or later |
| Meta XR Core SDK | v205 or later |
| OpenXR Plugin | 1.17.0 or later |
| Runtime | OpenXR (required) |
| Agent connection | MCP-compatible agent |

> [!CAUTION]
> Meta currently classifies XR Operator as experimental. Do not add production dependencies on it. Verify compatibility against Meta's release notes before upgrading any component.

---

## Setup (desktop — Simulator)

1. Install the Meta XR Core SDK (v205+) via Unity Package Manager.
2. Install OpenXR Plugin (1.17.0+).
3. Activate Meta XR Simulator as the OpenXR runtime in the Meta XR Simulator settings.
4. Start the MCP server: see `Tools/AgentBridge/README.md`.
5. Open the Unity project and enter Play Mode.
6. Connect your MCP-compatible agent to the local MCP endpoint.

---

## Setup (headset)

1. Complete all desktop steps above.
2. Build and deploy the game to Quest 2 with developer mode enabled.
3. The Operator connects via the same MCP server over the network.
4. Confirm OpenXR Plugin 1.17.0+ is active in the Android build settings.

---

## Agent smoke tests

Smoke tests using XR Operator are located in `Tests/AgentScenarios/`.  
These are run after PlayMode tests pass, as the final pre-headset step.

---

## Limitations

- Simulated imagery; spatial entities come from synthetic environment, not room scan.
- Cannot reproduce Quest 2 thermal, battery, or frame-timing behavior.
- Operator availability is not guaranteed across SDK updates.
- Do not use Operator results as a substitute for headset validation.

---

## Reference

For desktop test workflow: `Documentation/Operations/DESKTOP_WORKFLOW.md`  
For headset-required tests: `Documentation/Operations/HEADSET_WORKFLOW.md`  
For AgentBridge setup: `Tools/AgentBridge/README.md`
