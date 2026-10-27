# Game Design Overview: Convergence, Level 1 ("Teach Pixel to read")

Current as of Level 1 v7 (2026-10-28). Decisions: ADR-016 to ADR-021. Live plan: `.agents/tasks/TASK_LEVEL1_V7_DEMO.md`.
Older versions (the A.U.R.A. spaceship ride, Chamber 01) are only in git history.

## Premise

A seated VR ride (Quest 2) through a real neural network. Pixel, a small robot, can't read handwriting. The rider
builds Pixel a real network and trains it, live, to read the 3 the rider draws. Everything that lights up is computed:
real MNIST digits, a real 49 → 16 → 10 network (970 numbers), real backpropagation and gradient descent.

## Pillars

1. **Real, not a cartoon.** The rider's own 3 goes through the rider's own network; nothing is scripted to look right.
2. **A game, not a lecture.** Every stop is explain (short, 3Blue1Brown style) → play (a game with a win) → stage clear.
3. **One thread.** Draw a 3, build a detector by hand, send the 3 through, find the blame, train, see it read the 3.
4. **Orange means you can press it.** Lasers in both hands (trigger or A); right hand green (+), left red (−).
5. **Comfortable.** Seated, eased moves, no roll; the table follows eye height.

## The ride (about 7 minutes)

| Stop | Idea | The game |
|---|---|---|
| Dock | What we'll build; the five stops | Pull GO |
| Pixels | A picture is 49 numbers | Draw your own 3 |
| Neuron | Weights, bias, sigmoid | Spray weights until it fires for 3s only |
| Network | Layers; the untrained guess; the loss | Shoot your 3 into the network |
| Backpropagation | Blame flows back with the chain rule | Drag the hottest wires before they cool |
| Gradient descent | Learning rate; real mini-batch training | Hold a step size to train to 85 % |
| Right | What the 16 hidden neurons learned; test accuracy | Pixel reads your 3 |
| Bonus | Even trained networks make mistakes | Stump Pixel |
| End | Tokens: the same idea for words | Level 2 teaser |

Between stops: **blast the 3s**. Real handwritten digits fly at the pod; blast every 3, spare the rest (streaks multiply
the score, mistakes cost). The rider is the classifier Pixel is learning to be.

## Where the details live

Architecture and rules: `AGENTS.md`, `.agents/PROJECT_MAP.md`, `Documentation/Architecture/ADRs/`. Math:
`Documentation/Mathematics/FORMULAS.md`. Script: `Tools/Editor/RideNarrationData.cs`. Presenting it:
`Documentation/Design/TEAM_BRIEFING.md`. Playtests: `Documentation/Design/PLAYTEST.md`.
