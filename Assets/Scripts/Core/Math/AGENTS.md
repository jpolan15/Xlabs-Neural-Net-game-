# Math Agent

## Mission

Implement and maintain pure C# neural-network mathematics.

## Owns

- Activation functions (Linear, ReLU, Sigmoid, Softmax)
- Weighted sums: $\sum_{i=1}^n w_i x_i$
- Bias calculations: $z = \sum (w_i x_i) + b$
- Loss functions (MSE, Binary Cross-Entropy, Categorical Cross-Entropy)
- Gradient calculations: $\frac{\partial L}{\partial w}, \frac{\partial L}{\partial b}$
- Softmax & Scaled Dot-Product Attention calculations: $\text{Softmax}\left(\frac{Q K^T}{\sqrt{d_k}}\right) V$
- Numerical stability helpers (clamp, log-sum-exp, epsilon guards)

## Allowed dependencies

- `System`
- `System.Collections.Generic`
- `Unity.Mathematics`, only if strictly necessary

## Forbidden dependencies

Do not reference:
- `UnityEngine`
- `XR Interaction Toolkit`
- `MonoBehaviour`
- `GameObjects`
- `Prefabs`
- `Audio`
- `Scenes`
- `Materials`

This folder must remain 100% testable without entering Unity Play Mode or requiring the Unity Editor.

## Required behavior

Functions must:
- be deterministic,
- avoid hidden global state,
- handle edge cases (e.g. division by zero, $\log(0)$, explosive exponentials),
- use clear parameter names,
- expose formulas through comments or documentation where useful.

## Testing requirements

Every new mathematical feature requires unit tests for:
- normal input,
- zero input,
- negative input,
- boundary values,
- invalid or extreme values where applicable.

## Completion report

Report:
- files changed,
- formulas added or changed,
- tests added,
- test results,
- numerical limitations.
