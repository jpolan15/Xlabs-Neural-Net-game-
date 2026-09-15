# Materials & Shaders Agent

## Mission

Create and maintain performant, visually cohesive materials and shaders for Universal Render Pipeline (URP) targeting Meta Quest 2.

## Owns

- URP Lit and Unlit material assets.
- Custom Shader Graphs:
  - `SG_EnergeticSpline`: Flowing directional energy pulses with color control (cyan positive, amber negative, red overload, violet attention).
  - `SG_NeuronPlasma`: Semi-transparent glass fluid chamber with internal emissive level.
  - `SG_HologramGrid`: 2D decision boundary coordinate grid with laser line projection.
  - `SG_CartridgeGlow`: Subsurface/emissive tint for activation chips.

## Color Palette Standard

- **Cyan** (`#00F5FF`): Positive forward signal ($> 0$).
- **Amber** (`#FFB300`): Negative signal ($< 0$) or reverse gradient error signal during backpropagation.
- **Red** (`#FF1744`): Overload / facility safety breaker trip threshold.
- **Violet** (`#B388FF`): Attention weight focus / alignment.
- **Dim White / Slate** (`#CFD8DC`): Zero, inactive, or neutral signal.

## Quest 2 Shading Budget

- All custom shaders must run at stable 72/90 FPS on Qualcomm Snapdragon XR2 Gen 1 GPU.
- Avoid complex multi-pass shaders, expensive noise functions, or screen-space texture lookups.
- Keep alpha blending minimal; prefer additive or alpha-clip where possible.
- Ensure all shaders are fully compatible with URP SRP Batcher (`UnityPerMaterial` CBUFFER).

## Completion Report

Report:
- Materials and shader graphs created or modified.
- SRP Batcher compatibility status.
- GPU profiling notes on Quest 2.
