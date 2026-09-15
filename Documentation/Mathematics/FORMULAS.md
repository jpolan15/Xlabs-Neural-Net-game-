# Mathematical Formulas — Convergence

This is the **canonical** source of mathematical definitions for the Convergence neural simulation.

- When a Core agent changes any formula, they must update this file and add or update tests.
- Puzzle agents must implement loss functions exactly as defined here.
- Tolerances in this file are the values used in test assertions.

---

## 1. The Artificial Neuron

Pre-activation weighted sum:

$$z = \sum_{i=1}^n w_i x_i + b$$

Activated output:

$$a = f(z)$$

Where $w_i$ are weights, $x_i$ are inputs, $b$ is the bias term, and $f$ is the activation function.

---

## 2. Activation Functions

| Name | Formula | Derivative | Output range |
|---|---|---|---|
| Step | $f(z) = 1 \text{ if } z \ge 0 \text{ else } 0$ | $0$ (compatibility stub) | $\{0, 1\}$ |
| Linear | $f(z) = z$ | $f'(z) = 1$ | $(-\infty, \infty)$ |
| ReLU | $f(z) = \max(0, z)$ | $1$ if $z > 0$, else $0$ | $[0, \infty)$ |
| Sigmoid | $\sigma(z) = \frac{1}{1 + e^{-z}}$ | $\sigma(z)(1 - \sigma(z))$ | $(0, 1)$ |
| Tanh | $f(z) = \tanh(z)$ | $1 - \tanh^2(z)$ | $(-1, 1)$ |
| Softmax | $P(y = i) = \frac{e^{z_i}}{\sum_{j=1}^K e^{z_j}}$ | — | $(0,1)$, sums to 1 |

**Softmax numerical stability**: compute using shifted logits $z_i' = z_i - \max(z)$ to avoid floating-point overflow. Note: Softmax is vector-valued over a layer, not a scalar single-neuron activation.

---

## 3. Loss Functions & Evaluation Metrics

### Per-chamber assignment

| Chamber | Task type | Loss function / Metric |
|---|---|---|
| Level 01 — The Awakening Gate | Binary classification (OR gate) | Binary Accuracy (100% threshold) |
| Chamber 02 — XOR | Binary classification | BCE |
| Chamber 03 — Training | Gradient descent | MSE or BCE per sub-puzzle |
| Chamber 04 — Attention | Multi-class token prediction | CCE |

### Binary Accuracy (BA)

$$\text{BinaryAccuracy} = \frac{1}{n} \sum_{i=1}^n \mathbb{I}\left( (\hat{y}_i \ge 0.5) == y_i \right)$$

Where $y_i \in \{0, 1\}$ are binary targets. For Step activation, $\hat{y}_i \in \{0, 1\}$. Evaluator threshold is 1.0 (100% accuracy).
| Chamber 02 — XOR | Binary classification | BCE |
| Chamber 03 — Training | Gradient descent | MSE or BCE per sub-puzzle |
| Chamber 04 — Attention | Multi-class token prediction | CCE |

### Mean Squared Error (MSE)

$$\text{MSE} = \frac{1}{n} \sum_{i=1}^n (\hat{y}_i - y_i)^2$$

### Binary Cross-Entropy (BCE)

$$\text{BCE} = -\frac{1}{n} \sum_{i=1}^n \left[ y_i \log(\hat{y}_i + \epsilon) + (1 - y_i) \log(1 - \hat{y}_i + \epsilon) \right]$$

Clamp $\hat{y}$ to avoid $\log(0)$: use $\epsilon = 10^{-7}$.

### Categorical Cross-Entropy (CCE)

$$\text{CCE} = -\sum_{k=1}^K y_k \log(\hat{y}_k + \epsilon)$$

Clamp $\hat{y}$ with $\epsilon = 10^{-7}$. Target $y_k$ is one-hot encoded.

---

## 4. Gradient Descent Update Rule

$$w \leftarrow w - \eta \cdot \frac{\partial L}{\partial w}$$

Where $\eta$ is the player-configurable learning rate, bounded to a safe range.  
Gradients must be computed from real mathematics — not approximated or faked.

---

## 5. Scaled Dot-Product Attention

$$\text{Attention}(Q, K, V) = \text{Softmax}\left(\frac{Q K^T}{\sqrt{d_k}}\right) V$$

Where $Q$, $K$, $V$ are query, key, and value matrices and $d_k$ is the key dimension scaling factor.

Full Chamber 04 implementation spec: `Assets/Puzzles/Chamber04_Attention/`.

---

## 6. Numerical Tolerances

| Comparison | Tolerance ($\epsilon$) | Notes |
|---|---|---|
| Unit test floating-point equality | $10^{-5}$ | Use `Assert.AreEqual(expected, actual, delta)` |
| Loss function convergence (puzzle pass) | $10^{-4}$ | Configurable per chamber |
| UI display rounding | $10^{-3}$ | Display only; not used in evaluation |
| Softmax sum check | $10^{-6}$ | Probability distribution must sum to 1 |
| BCE/CCE log clamp | $10^{-7}$ | Prevents $\log(0)$ |

Do not use exact floating-point equality (`==`) in any test assertion.
