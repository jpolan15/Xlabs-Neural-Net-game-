# Chamber 04 Attention Core Agent

## Mission

Teach the core mechanisms of attention and contextual prediction in modern Transformer architectures without oversimplifying or misleading players.

## Learning Objective

The player discovers that **attention determines which context tokens influence the next prediction** by calculating similarity between Query ($Q$) and Key ($K$) vectors, using Softmax to generate an attention distribution, and computing a weighted sum of Value ($V$) vectors:
$$\text{Attention}(Q, K, V) = \text{Softmax}\left(\frac{Q K^T}{\sqrt{d_k}}\right) V$$

## The Puzzle Scenario: The Corrupted Context Window

A.U.R.A.'s nav log is mis-predicting the jump because a corrupted token is taking the attention weight. Masking that token is what lets the circuit predict JUMP_HOME. This is a small attention circuit, not a language model.

### Context Sequences Under Investigation:
1. `[EARTH_LOCK] [FUEL_OK]` $\longrightarrow$ Expected: `JUMP_HOME`
2. `[EARTH_LOCK] [FUEL_OK] [NOISE]` $\longrightarrow$ Failing because the corrupted token takes the attention weight
3. The same window with `NOISE` masked $\longrightarrow$ Expected: `JUMP_HOME`

## Player Actions & Mechanics

1. **Inspect Attention Matrix**:
   - Holographic 2D grid shows query-key dot products and resulting attention weights.
   - Violet particle beams connect tokens to show where attention mass is focused.
2. **Diagnose Misleading Attention**:
   - The player discovers the model is attending strongly to `[CORRUPTED_NOISE]` due to mismatched projection vectors or lack of positional context.
3. **Configure Attention Heads**:
   - Player adjusts projection weights or inserts a Context Mask cartridge to block irrelevant / corrupted tokens.
4. **Compare Attention States**:
   - Mode 1: No attention (uniform bag-of-words) $\to$ unable to distinguish context.
   - Mode 2: Distracted attention $\to$ high probability assigned to the wrong command.
   - Mode 3: Focused attention $\to$ Softmax correctly concentrates attention mass on relevant tokens (`OVERHEAT` + `CONTAINMENT`), driving the output probability distribution over actions to peak at `SEAL_DOOR`.

## Educational Rigor & Truth in Teaching

- Do NOT claim the player is building a full LLM or ChatGPT.
- Do NOT use a hardcoded magic number (e.g. "reach 98%").
- The output is an honest probability distribution over candidate facility command tokens.
- Language to use: *"This simplified attention circuit demonstrates how context determines next-token predictions."*

## Acceptance Criteria

- Player demonstrates how masking or tuning query-key alignment shifts the probability distribution.
- The model correctly predicts actions across all 3 context test sequences.
