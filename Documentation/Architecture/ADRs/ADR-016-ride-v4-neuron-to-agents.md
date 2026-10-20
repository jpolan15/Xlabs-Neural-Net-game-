# ADR-016: Ride v4, "From One Neuron to Agents"

| Field | Value |
|---|---|
| ID | ADR-016 |
| Date | 2026-10-20 |
| Status | Accepted (plan agreed; built phase by phase, see `.agents/tasks/TASK_RIDE_V4_P*.md`) |
| Deciders | Project owner (Neural Ride v3/v4 planning, 2026-10-20) |

## Context

The ride teaches one neuron, learning and an XOR teaser. The owner wants a total beginner to leave with a working picture of modern AI: "AI is magic" becomes "AI = weights guessing the next word, in a loop".

## Decision

### 1. Six stops, one shape

| Stop | Idea | Hands-on | Core evaluator |
|---|---|---|---|
| 1 One neuron (old 1 + 2) | a neuron is a vote | ROCK / ICE / TRIGGER levers | `PuzzleEvaluator` (OR) |
| 2 Learning | learning = rolling downhill | ball by hand, then LEARN | `PuzzleEvaluator` + `ManualDescentScore` |
| 3 Layers | stacking draws any shape | add a helper neuron | `XorPuzzle` (2-2-1) |
| 4 Tokens / Word Space | meaning = geometry | sort orbs, carry the arrow | `AnalogyPuzzle` (`VectorMath.Nearest` on `WordSpace`) |
| 5 Attention + next word | look back, guess next | aim the spotlight, WILD lever | `AttentionPuzzle`, `NextTokenModel` |
| 6 Agent loop (finale) | agent = guesser in a loop | GOAL levers, STOP lever | `AgentStepEvaluator` |

Every stop runs **Clip → Handoff → Minigame → Payoff**:

- **Clip:** a ~20 s Manim video on a curved screen. Gameplay gets a new `StationPhase.Clip` and waits on a number of seconds from `RideScript`, never on the `VideoPlayer`. Space or a 2 s GO hold skips.
- **Handoff:** in the last 1.5 s, 1 to 4 flat sprites fly from the screen to the 3D pieces. Presentation only.
- **Minigame:** only Core decides win or lose.
- **Payoff:** a one-line banner, and one more layer of the big network lights.

### 2. Palette: minus/drain yellow is `#FFEB00`

`#FFD000` sits about 29° from orange `#FF5500` (hue 49° vs 20°), inside the 35° rule. `#FFEB00` sits at hue 55°, 35° away. `RideTheme.amber` and the `NeuralLinks` negative colour move to `(1, 0.922, 0)`. Orange stays for grabbables only, in the game and in the clips.

### 3. Video playback needs `com.unity.modules.video`

The built-in video module is not in `Packages/manifest.json` today. Phase P2 adds `"com.unity.modules.video": "1.0.0"` (a built-in Unity module, no third-party code) for `VideoPlayer`. This ADR records the reason, as `AGENTS.md` requires for package changes.

### 4. Clip sources live outside `Assets/`

Manim sources go in `ArtSource/clips/` (the existing raw-art folder from ADR-012, not imported or shipped). Rendered H.264 files (≤ 2 MB each) go in `Assets/_Project/Video/`. P2 creates both folders and adds them to `ArtSource/AGENTS.md` and `.agents/PROJECT_MAP.md`.

### 5. Level 01 leaves the build list (P9)

`Level01_AwakeningGate` is removed from the build list because the ride is the whole game. Its files and tests stay.

## Consequences

- Five new Core types and their EditMode tests (P1). No UnityEngine in Core.
- Draw calls are already over budget (111 / 115 / 99, ADR-015). Each phase must leave `RideBudget` lower than it found it. The clip screen is hidden outside `Clip`, so it costs 0 calls in the minigame.
- Talk shrinks: ≤ 2 sentences per beat, and the first hands-on stays ≤ 15 s.
- Target length is about 15 minutes (≤ 16 min gate in P8).
- **Not verified by this ADR:** anything on a headset, by ear, or in the hand.
