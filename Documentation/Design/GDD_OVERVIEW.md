# Game Design Document: Project Convergence

## Premise
You are an engineering specialist inside an abandoned, locked-down AI research complex. The facility's central intelligence is malfunctioning and locking down sectors. You must physically inspect, wire, calibrate, and diagnose neural circuits to unlock doors and stabilize the core.

## Chamber Roadmap
1. **Level 01 / Chamber 01: The Awakening Gate**
   - Setting: Alien computation megastructure floating in deep space.
   - Concept: Artificial neuron perceptron, weighted inputs, bias threshold offset, step activation function.
   - Core Discovery: $y = \text{step}(w_1 x_1 + w_2 x_2 + b)$ must satisfy all four binary classification cases of the OR truth table simultaneously. Solution margin: $w_1 = 1.0, w_2 = 1.0, b = -0.5$.
   - Tools: Neural Pulse Tool, Arc Blade, detented weight regulators, bias dial, activation crystal socket.
   - Acceptance: 100% binary accuracy required to unlock the Awakening Gate; 3/4 solutions (75%) keep the gate sealed.
2. **Chamber 02: The XOR Wall**
   - Concept: Linear separability limits, hidden layers, non-linear activation (ReLU).
   - Core Discovery: A straight line cannot separate diagonal classes; hidden units act as specialized region feature detectors.
3. **Chamber 03: The Training Bay**
   - Concept: Loss surfaces, gradient descent, hyperparameter tuning, debugging training failures.
   - Core Discovery: Hyperparameters (learning rate, normalization, batch size) determine whether a model converges or diverges.
4. **Chamber 04: The Attention Core**
   - Concept: Transformers, Query-Key similarity, Softmax attention distributions, contextual prediction.
   - Core Discovery: Attention dynamically determines which context tokens influence the next prediction.
