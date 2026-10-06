# ADR-010: Automated Point Defense Driven by the Chamber 01 Neuron

| Field | Value |
|---|---|
| ID | ADR-010 |
| Date | 2026-10-03 |
| Status | Accepted |
| Deciders | Design, Gameplay |

## Context

ADR-008 kept Chamber 01 as an OR truth table, but the four cases ("Quiet", "Radio", "Light", "Both") were abstract pings on indoor pods. Playtest notes said the player could not tell what the neuron was for or why a wrong answer mattered. The pods sat about 0.25 m wide inside the bridge, so the "asteroid" framing in the opening voice line had nothing to look at.

The player's own recording ("Our ship has lost connection. It is your job to reconnect it...") now opens the level. The level needs a visible, physical reason to reconnect the neuron within the first minute.

## Decision

The neuron becomes the targeting brain of the ship's automatic point-defense laser.

- The player never holds a gun, never aims, and never pulls a trigger. ADR-008's cut of the neural gun and sentry stands.
- Objects fly toward the canopy. A scan line reads two sensors per object: ROCK (\(x_1\)) and ICE (\(x_2\)). The neuron computes FIRE \(= \text{step}(w_1 \cdot \text{ROCK} + w_2 \cdot \text{ICE} + b)\).
- Case mapping, in the existing OR case order `(0,0), (0,1), (1,0), (1,1)`:
  - index 0: Repair Drone (no rock, no ice). It must not be shot.
  - index 1: Icy Comet (ICE only). It must be shot.
  - index 2: Rocky Asteroid (ROCK only). It must be shot.
  - index 3: Rock-and-Ice Chunk (both). It must be shot.
- The OR table, `PuzzleDefinition.CreateORGatePuzzle()`, and all Core math are unchanged. Core is not touched.
- Correctness always comes from `PuzzleEvaluator`. Each scan calls the chamber, which evaluates the live effective network. Gameplay classifies the outcome by comparing `CaseDiagnostic.ActualOutput >= 0.5` with `CaseDiagnostic.ExpectedOutput`. No layer hardcodes which object is a threat or when the puzzle is solved.
- A missed threat damages the hull. At 0 hull the ship reroutes power to 60% and keeps the player's settings. Nobody dies and the level does not purge.
- After the solve, a short victory swarm replays the OR cases against the captured Chamber 01 puzzle, even if `VoyageDirector` has already switched the live puzzle to XOR. Then the bridge door opens.
- The lever remains as a "self-test". It runs the honest full evaluation and can solve the puzzle.

## Consequences

- New Gameplay director (`AsteroidDefenseDirector`) owns waves, scans, outcomes, and the victory sequence. The chamber's built-in wave loop is disabled for Level 1 through a serialized flag, not deleted.
- `ChamberController` gains an external-director flag, a soft reroute at 0 hull, and a replay method that evaluates a given puzzle without changing solve state.
- `GatewayController` can defer opening to the director.
- Presentation gains a laser/explosion visual, an impact sequence for the opening, a voice-line player, and the opening teleprompter. Presentation still only observes events.
- The "Radio"/"Light" wording in design docs and hints is replaced with ROCK/ICE/FIRE.
- `VoyageDirector` still unlocks later rooms on solve, before the swarm ends. That is a known limitation for a later task.

## Supersedes

The Chamber 01 framing in ADR-008 ("sensor array", radio beacon or light signature). The rest of ADR-008, including the chamber roadmap and the cut of the handheld gun, sentry, and Synapse-GPT, still applies.
