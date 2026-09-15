# ADR-004: Task-Appropriate Loss Functions Per Chamber Type

| Field | Value |
|---|---|
| ID | ADR-004 |
| Date | 2026-09-10 |
| Status | Accepted |
| Deciders | Architecture, Core/Puzzles |

## Context

Using MSE for all puzzles is mathematically misleading. MSE is appropriate for regression but not for classification or token prediction problems. Teaching players the wrong loss function for a task creates incorrect intuitions about how real neural networks work.

## Decision

Use the mathematically correct loss function for each chamber type:

| Chamber | Task type | Loss function |
|---|---|---|
| Chamber 01 — Weighted Sum | Regression / balance | MSE |
| Chamber 02 — XOR | Binary classification | Binary Cross-Entropy |
| Chamber 03 — Training | Gradient descent | MSE or BCE depending on sub-puzzle |
| Chamber 04 — Attention | Multi-class / token prediction | Categorical Cross-Entropy |

## Formulas

See `Documentation/Mathematics/FORMULAS.md` for canonical definitions.

**MSE:**
$$\text{MSE} = \frac{1}{n} \sum_{i=1}^n (\hat{y}_i - y_i)^2$$

**BCE:**
$$\text{BCE} = -\frac{1}{n} \sum \left[ y \log(\hat{y}) + (1-y) \log(1-\hat{y}) \right]$$

**CCE:**
$$\text{CCE} = -\sum y_i \log(\hat{y}_i)$$

## Consequences

- Teaches correct deep learning practice aligned with industry standards.
- Each chamber's puzzle evaluator must use the correct loss function.
- Test cases must use tolerances appropriate to the loss function's scale.
- The Core layer must implement all three loss functions independently.

## Supersedes

Nothing.
