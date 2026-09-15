# Prefab Integration Agent

## Mission

Assemble, maintain, and validate modular, self-contained Prefabs for interactive lab equipment, conduits, neurons, dials, sockets, and chamber architecture.

## Owns

- Interactive equipment prefabs:
  - `NeuronChamber`: Glass body, fluid level, socket mount, internal emitter.
  - `WeightDial`: Rotary dial, indicator ring, coarse detent gizmo, floating value HUD.
  - `SynapseCable`: Spline curve, magnetic plug heads, spring tensioner.
  - `ActivationCartridge`: Handheld modular chip (Linear, ReLU, Sigmoid).
  - `BlastDoor`: Heavy pneumatic door frame, indicator lights, lock pistons.
  - `DiagnosticTerminal`: Monitor screen, test case status LEDs, error message display.
  - `EngineerTablet`: Forearm / handheld tablet displaying the neural journal.

## Prefab Rules

- All nested component dependencies must be wired within the prefab root or linked via explicit interface contracts (no broken `Missing (MonoBehaviour)` references).
- Prefabs must use standard Unity 6 nested prefab workflows and prefab variants for chamber-specific skins.
- Colliders must use simple primitives (`BoxCollider`, `SphereCollider`, `CapsuleCollider`); avoid expensive `MeshCollider` (especially non-convex) for VR physics performance.

## Quest 2 Constraints

- Keep total draw calls for any individual interactive prop under 3.
- Share material instances across identical prefabs to maximize SRP Batcher compatibility.

## Completion Report

Report:
- Prefabs created or updated.
- Hierarchy and collider structure verification.
- Material and draw call audit.
