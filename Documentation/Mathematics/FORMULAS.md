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

Partial derivative used by backpropagation:

$$\frac{\partial \text{MSE}}{\partial \hat{y}_i} = \frac{2}{n}(\hat{y}_i - y_i)$$

### Binary Cross-Entropy gradient

With \(\hat{y}\) clamped to \([\epsilon, 1-\epsilon]\) and \(\epsilon = 10^{-15}\) (`LossFunctions.DefaultEpsilon`):

$$\frac{\partial \text{BCE}}{\partial \hat{y}_i} = -\frac{1}{n}\left(\frac{y_i}{\hat{y}_i} - \frac{1-y_i}{1-\hat{y}_i}\right)$$

The parameter step is \(w \leftarrow w - \eta \partial L / \partial w\), with \(\partial L / \partial w_i = (\partial L / \partial a) f'(z) x_i\). Step's derivative is the compatibility stub 0, so training steps use Sigmoid or ReLU.

### Binary Cross-Entropy (BCE)

$$\text{BCE} = -\frac{1}{n} \sum_{i=1}^n \left[ y_i \log(\hat{y}_i + \epsilon) + (1 - y_i) \log(1 - \hat{y}_i + \epsilon) \right]$$

Clamp $\hat{y}$ to avoid $\log(0)$: `LossFunctions.DefaultEpsilon` is $10^{-15}$.

### Categorical Cross-Entropy (CCE)

$$\text{CCE} = -\sum_{k=1}^K y_k \log(\hat{y}_k + \epsilon)$$

Target $y_k$ is one-hot encoded, so for the true class $t$: $\text{CCE} = -\ln(\max(\hat{y}_t, \epsilon))$.
`LossFunctions.CategoricalCrossEntropy` clamps with `DefaultEpsilon` ($10^{-15}$).

Gradient with respect to the probabilities (`CategoricalCrossEntropyGradient`):
$\partial L / \partial \hat{y}_t = -1 / \max(\hat{y}_t, \epsilon)$ and $0$ for every other class. Fed through a
layer-wide softmax (`LayerModel.Backward`) this becomes $\partial L / \partial z_k = \hat{y}_k - y_k$.

### Level 1 network and weight-paint neuron

- Level 1's digit network (`NetworkModel.CreateDense`, data from `ArtSource/training/bake_level1.py`): 49 inputs
  (7×7 averages of 4×4 pixel blocks, 0 = paper, 1 = ink) → 16 sigmoid → 10 softmax, trained with full-batch gradient
  descent on CCE. Blame for a wire is the real $\partial L / \partial w$ for the hero digit (target 3).
- Weight-paint neuron (`Core.Puzzles.WeightPaintPuzzle`): weights $w_i \in \{-1, 0, +1\}$, total
  $s = \sum_i x_i w_i$, fires when $s \ge T$. A digit is right when it fires exactly when it should. The hint is the
  single weight change that maximises (digits right, then $\sum_d m_d$) with margin $m_d = s_d - T$ for digits that
  should fire and $T - s_d$ for the others. Since v5 the digits are 4 threes (fire) and one each of 0, 1, 7, 4 (stay
  dark), $T = 2.5$; the bake's reference is $+1$ where the mean training 3 has more than 0.1 more ink than the mean of
  the others, $-1$ where it has more than 0.1 less. A brush stroke sets $w_i \leftarrow b$ ($b = +1$ right hand,
  $-1$ left), or $0$ when $w_i = b$ already (`PaintValue`).

### Level 1 v5 mini-games (`Core.Puzzles`)

- **Draw the 3** (`DrawPuzzle`): outline $O = \{i : x_i \ge 0.3\}$ of the hero 3's 7×7. Passes when
  $|I \cap O| \ge \lceil 0.75\,|O| \rceil$ and $|I \setminus O| \le 3$ for the inked set $I$.
- **Plug the strings** (`PlugPuzzle`): answer scores start at the untrained network's real logits for the hero 3,
  $z_k = b^{(2)}_k + \sum_j W^{(2)}_{kj} h_j$. Plugging string $s$ (hidden neuron $j_s$, brightness $h_{j_s}$) into answer
  $k$ adds $P\,h_{j_s}$ to $z_k$, $P = 1.5$. Won when $\arg\max_k z_k = 3$. The glow is $\mathrm{softmax}(z)$.
- **Roll it down** (`DescentGame`): $L(x) = c\,(x - x^*)^2 + L_0$ with $c = 0.5$, $x^* = 2$, $L_0 = 0.3$, start
  $x_0 = -2.5$; a step is $x \leftarrow x - \eta\,L'(x)$, $L'(x) = 2c\,(x - x^*)$. Buttons $\eta \in \{0.12, 0.7, 1.85\}$:
  $\eta > 1/(2c)$ lands past the bottom, $\eta > 1/c$ climbs. Won when $|x - x^*| \le 0.15$; 3 stars within 6 steps.
  This hill is an illustration; the Valley drop after it is the real loss.

---

## 4. Gradient Descent Update Rule

$$w \leftarrow w - \eta \cdot \frac{\partial L}{\partial w}$$

Where $\eta$ is the player-configurable learning rate, bounded to a safe range.  
Gradients must be computed from real mathematics — not approximated or faked.

---

## 5. Scaled Dot-Product Attention

$$\text{Attention}(Q, K, V) = \text{Softmax}\left(\frac{Q K^T}{\sqrt{d_k}}\right) V$$

Where $Q$ is \([\text{queryCount}, d_k]\), $K$ is \([\text{keyCount}, d_k]\), and $V$ is \([\text{keyCount}, d_v]\). A false mask entry replaces that logit with \(-\infty\) before softmax. Each attention row sums to 1 within \(10^{-6}\). Implementation: `Attention.ScaledDotProduct`.


---

## 6. Numerical Tolerances

| Comparison | Tolerance ($\epsilon$) | Notes |
|---|---|---|
| Unit test floating-point equality | $10^{-5}$ | Use `Assert.AreEqual(expected, actual, delta)` |
| Loss function convergence (puzzle pass) | $10^{-4}$ | Configurable per chamber |
| UI display rounding | $10^{-3}$ | Display only; not used in evaluation |
| Softmax sum check | $10^{-6}$ | Probability distribution must sum to 1 |
| BCE/CCE log clamp | $10^{-15}$ | `LossFunctions.DefaultEpsilon`; prevents $\log(0)$ |
| Level 1 bake reproduction | $10^{-6}$ | Core forward of the baked weights vs the bake's golden activations |

Do not use exact floating-point equality (`==`) in any test assertion.
