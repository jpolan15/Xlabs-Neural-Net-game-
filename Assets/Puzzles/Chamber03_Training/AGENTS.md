# Chamber 03 Training Bay Agent

## Mission

Teach loss landscapes, gradient descent, hyperparameter tuning, and diagnostic debugging of machine learning models (overfitting, learning rate instability, unnormalized data).

## Learning Objective

The player transitions from manual weight adjustment to training an automated security drone classifier by configuring hyperparameters and diagnosing why automated training fails.

## Player Experience & The Core Loop

1. **Observe**: Security scanner fails to classify unfamiliar intruder drones, despite achieving 100% accuracy on training drones.
2. **Predict**: Player hypothesizes whether the issue is:
   - learning rate too high (exploding loss),
   - dataset too small / lack of diversity (overfitting / generalization gap),
   - unnormalized sensor scales (one feature dominating weights),
   - network too deep or too shallow.
3. **Configure Controls**:
   - Learning Rate dial ($\eta \in [0.001, 1.0]$)
   - Training Batch Size lever
   - Feature Normalizer switch (Raw Sensor mV vs Scaled $[0, 1]$)
   - Dataset Expander cartridge (adds edge cases to training pool)
4. **Trigger Auto-Train**:
   - Player pulls the `COMMENCE TRAINING` lever.
   - An amber backprop pulse surges backward through network layers.
   - Dials auto-rotate incrementally as weights update.
   - A holographic 3D loss surface projects in the center of the room.
5. **Diagnose Failures**:
   - $\eta$ too high ($1.0$): Dials spin wildly back and forth; loss curve explodes to infinity; warning siren sounds ("Gradient Divergence!").
   - $\eta$ too low ($0.0001$): Dials barely budge; progress takes minutes ("Vanishing Gradient / Stalled Convergence").
   - Unscaled data: Sensor 1 (range 0–1000) dwarfs Sensor 2 (range 0–1); weights on Sensor 2 remain zero ("Feature Dominance").
   - Small dataset: Training loss reaches 0, but when a new test drone passes the scanner, it trips the alarm ("Overfitting / Validation Failure").
6. **Mastery Solution**:
   - Player scales inputs, sets moderate learning rate ($\eta \approx 0.05$), expands dataset, and verifies the trained network on unseen validation drones.

## Forbidden Behavior

- Do NOT make this a passive cutscene where the player pulls a lever and watches a pre-rendered video.
- Training must execute real gradient updates step-by-step deterministically.

## Acceptance Criteria

- Model must be evaluated on an unseen test set of drones to pass.
- Each hyperparameter error produces distinct, readable visual and auditory feedback.
