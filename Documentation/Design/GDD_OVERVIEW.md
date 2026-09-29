# Game Design Document: Project Convergence

## Premise

An asteroid strike knocks the ship out of hyperspace. The navigation AI, A.U.R.A., has lost its trained weights. The player is alone with it. Each chamber teaches one real machine-learning concept and changes the ship. The decision record is ADR-008.

## Chamber Roadmap

1. **Chamber 01, sensor array**
   - Setting: the ship's sensor bay, power out, shutters closed.
   - Concept: one neuron, two inputs, a bias, and a step activation.
   - Core discovery: the array wakes if a radio beacon OR a light signature is present. Quiet sensors stay dark.
   - Canonical margin: \(w_1 = 1\), \(w_2 = 1\), \(b = -0.5\), Step. Any weights that score every row pass. The displays do not print those numbers as the answer.
   - Payoff: shutters open, lights steady, the door aft of the bay unlocks. The ship drifts toward the asteroids. Nothing kills the player.
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

Sentry defense, the neural gun, the motherboard room, and Synapse-GPT. Those tasks are dropped, not completed.
