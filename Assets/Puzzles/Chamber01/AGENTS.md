# Level 1 — The Awakening Gate Agent

## Mission

Teach the single-neuron binary classification perceptron by repairing an alien computing core to unlock the Awakening Gate.

## Learning Objective

The player discovers that a single neuron with Step activation computes:
$$y = \text{step}(w_1 x_1 + w_2 x_2 + b)$$
where $\text{step}(z) = 1$ if $z \ge 0$, else $0$. The player must discover weights and a threshold/bias offset that simultaneously satisfies the complete **OR** truth table.

## Test Cases (OR Truth Table)

| Case | Input $x_1$ | Input $x_2$ | Target Output $y$ | Expected Margin ($w_1=1, w_2=1, b=-0.5$) |
|:---|:---:|:---:|:---:|:---:|
| Case 1 | 0.0 | 0.0 | 0.0 | $z = -0.5 \to 0$ |
| Case 2 | 0.0 | 1.0 | 1.0 | $z =  0.5 \to 1$ |
| Case 3 | 1.0 | 0.0 | 1.0 | $z =  0.5 \to 1$ |
| Case 4 | 1.0 | 1.0 | 1.0 | $z =  1.5 \to 1$ |

*Canonical solution*: $w_1 = 1.0, w_2 = 1.0, b = -0.5$, Activation = `Step`.  
Any mathematically valid configuration producing 100% binary accuracy passes.

## Constraints & Acceptance Criteria

1. **Gate Authority**: The Awakening Gate unlocks if and only if `PuzzleEvaluation.Passed == true` (100% accuracy on all 4 cases with Step activation).
2. **Negative Gate Protection**: 3 out of 4 correct cases produces `Accuracy = 0.75` and explicitly keeps the gateway locked.
3. **Activation Enforcement**: Linear or ReLU activation cannot solve the gate, even with correct weight values. The Step activation crystal must be physically socketed.
4. **Physical Conduits**: Disconnected cables drop the corresponding signal to 0.0 until reconnected.
5. **No Scripted Bypasses**: The evaluation runs through pure C# `PuzzleEvaluator.Evaluate()`.

## Solvable Broken Starting Configurations

| Preset | $w_1$ | $w_2$ | $b$ | Activation | Cable Status |
|---|:---:|:---:|:---:|:---:|:---|
| Preset A | 0.0 | 0.0 | -1.0 | Linear | Cable 1 disconnected |
| Preset B | -0.5 | 1.0 | 0.0 | Step | Cable 2 disconnected |
| Preset C | 1.0 | 1.0 | -2.0 | ReLU | All connected |
| Preset D | 0.5 | 0.5 | 0.0 | Linear | Both disconnected |
| Preset E | -1.0 | -1.0 | 1.0 | Step | Cable 1 disconnected |
| Preset F | 0.0 | 0.0 | 0.0 | Step | All connected |
