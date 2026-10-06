# Game Design Document: Project Convergence

## Premise

An asteroid strike knocks the ship *Neural* out of hyperspace and tears the neural cables out of the targeting core. The player's own recorded voice warns the passengers that the ship has lost connection. Each chamber teaches one real machine-learning concept and changes the ship. The decision records are ADR-008 (premise) and ADR-010 (Chamber 01 point defense).

## Chamber Roadmap

1. **Chamber 01, point defense**
   - Setting: the bridge, red alarm light, an asteroid field drifting toward the canopy.
   - Concept: one neuron, two inputs, a bias, and a step activation. The neuron is the brain of an automatic laser. The player never aims or fires.
   - Core discovery: the laser should FIRE if an object is ROCK or ICE. The friendly repair drone (neither) must be let through to dock.
   - Inputs: ROCK sensor \(x_1\), ICE sensor \(x_2\). Output: FIRE \(= \text{step}(w_1 \cdot \text{ROCK} + w_2 \cdot \text{ICE} + b)\).
   - Canonical margin: \(w_1 = 1\), \(w_2 = 1\), \(b = -0.5\), Step. Any weights that score every row pass. The displays do not print those numbers as the answer.
   - Pressure: missed threats dent the hull. At 0 hull the ship reroutes power back to 60% and keeps the player's settings. Nothing kills the player.
   - Payoff: a victory swarm the laser clears on its own, lights turn blue, and the bridge door opens.
2. **Chamber 02, spectrum filter**
   - Concept: XOR is not linearly separable. A hidden layer with a non-linear activation can separate it.
   - Payoff: the telescope powers on.
3. **Telescope, then Chamber 03**
   - The player photographs sky bodies. Features are the blue ratio, white ratio, and brightness of the capture.
   - Cards go in an EARTH tray or a NOT EARTH tray. Training is stochastic gradient descent. A held-out set, including a blue ice giant, grades the result. A tray that only teaches "blue means Earth" fails in words.
4. **Chamber 04, nav computer**
   - Attention over log tokens such as `[EARTH_LOCK]`, `[FUEL_OK]`, and `[NOISE]`.
   - Masking the corrupted token is what predicts `JUMP_HOME`.
   - Payoff: the jump, Earth in the canopy, a short credits card, and a journal of what the player actually taught A.U.R.A.

## Cut from the live game

Sentry defense, the handheld neural gun, the motherboard room, and Synapse-GPT. Those tasks are dropped, not completed. The Chamber 01 laser is automatic and is driven only by the neuron (ADR-010).
