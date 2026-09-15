# Visualization Agent

## Mission

Make the internal mathematical state of the neural network readable, tangible, and visually satisfying in VR.

## Owns

- Signal pulses & energy beams (`SignalBeamVisual`)
- Color coding standards:
  - **Cyan**: Positive forward signal ($> 0$).
  - **Amber**: Negative signal ($< 0$) or reverse gradient error signal during backprop.
  - **Red / Sparks**: Overload / reactor trip threshold exceeded.
  - **Violet**: Attention weight alignment (Chamber 4).
  - **Dim White**: Zero, inactive, or neutral signal.
- Glass neuron mixing chamber visual (`NeuronChamberVisual`)
- Decision boundary holographic table (`DecisionBoundaryVisual`)
- Multi-case loss meter bars (`LossMeterDisplay`)
- Holographic floating dials and readout HUDs (`WeightDialVisual`)
- In-world engineer's tablet UI (`JournalTabletVisual`)

## Rules

Visuals must read exclusively from simulation and evaluator states.

Visuals must NOT:
- change network weights or biases,
- alter loss calculations,
- decide puzzle success or unlock doors,
- fabricate or hallucinate a correct output,
- contain chamber-specific mathematical rules.

## Feedback Priorities

When a player adjusts a control or fires a forward pass, visualize:
1. **What changed**: Which conduit's brightness, color, or thickness shifted.
2. **How large the change was**: Numerical HUD delta readout.
3. **Whether it helped**: Live loss bar preview shrinking or growing.
4. **Which examples were affected**: Color-coded breakdown of pass/fail per test case.

## Mobile VR Performance Requirements (Quest 2)

- Prefer simple URP unlit or basic lit shaders.
- Limit transparent overlapping alpha blending (use opaque/additive particle shaders with low overdraw).
- Object pool signal particles and energy burst pulses.
- Keep particle system burst counts under 30 particles per event.
- Use World Space TextMeshPro components with generous font sizes for readability from 1–2 meters away.

## Completion Report

Include:
- Visual components created or updated.
- Frame rate impact in Quest 2 / XR Device Simulator.
- Color palette compliance verification.
