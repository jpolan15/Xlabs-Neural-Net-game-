# ADR-015: A Hands-On Opening, and the Network as the Star

| Field | Value |
|---|---|
| ID | ADR-015 |
| Date | 2026-10-09 |
| Status | Accepted (built; EditMode and PlayMode tests pass; looked at in Play Mode captures; not yet seen, heard or felt on a headset) |
| Deciders | Project owner (requests of 2026-10-09, handoff part 2 sections 1 to 3); Ride v3 work |

## Context

The owner asked for two things before the Ignite demo:

1. "Make the opening scene awesome and interactive. It needs to be amazing to really HOOK a player." The intro was 74 s of narration at the dock. The rider's first input came at about 75 s, so a first-timer stood and listened for over a minute.
2. "The lines of the neural net are kinda hard to distinguish", "make sure the neural net is the most important thing in this whole ride", and "make sure the net stands out among the background, it blends in with the ride". Every connection was drawn 1.6 to 4 px wide, and brightness varied only about 3× with the weight. The unlit network glowed at 62 %. The rails, the gates every 7 m and 600 cyan dust specks used the network's own colours and shader.

## Decision

### 1. Hands-on beats in the intro (Gameplay, numbers only)

- `RideScript.Beat` gains `waitFor` (a control id), `waitFrom`, `waitAtLeast` and `waitTimeout`. A beat with `waitFor` is a hands-on beat. Its line plays, and the director waits until the rider sets that control to at least `waitAtLeast`, or until `waitTimeout` seconds after the line ends.
- On a timeout the director sets the value itself (`Drive`, not `SetFromUser`), so the moment still plays out and nobody gets stuck. GO is the exception, because a driven GO would read as a skip. Either way the line finishes, and the reaction gets `taskReactSeconds` (1.8 s) before the next line.
- New events: `RiderTaskStarted(controlId, atLeast)` and `RiderTaskDone(controlId, byRider)`. The hint arrow points at the lever through the existing `HintChanged`.
- The data lives in `RideNarrationData.IntroTasks`. The builder writes it into the intro sequence and fails if a task names a line that is not in the intro or has no timeout.
- **Skip vs. wake:** during the intro, holding GO for 2 s skips (the operator's handle). The pull that wakes AURA is not a skip, even if the rider keeps holding: the hold timer only counts again after GO has been seen released. Space (`SkipNarration`) always works.

### 2. The new opening (≈ 68 s of talk, hands on a lever at ≈ 13 s)

| Beat | What happens |
|---|---|
| JFK (archival) | The network starts fully dark. Neurons and connections appear one by one with his words (`{jfk_build}`). On "because they are hard" everything flashes once (`{jfk_hard}`, with a shimmer sting), then the dark comes back with a few embers. |
| AURA: "Targeting… offline" | The embers flicker. |
| **Guide (new, 30):** "Anything orange, you can grab. Pull the orange lever, and wake her up." | **Hands-on beat 1:** GO ≥ 0.5, 8 s timeout. On the pull, light races out from the pod to the far end (`_LitUpTo` sweep, 2.4 s), with a rumble and shimmer, the hum swelling, and a haptic pulse in the hand that pulled. |
| Guide (19, reworded): "There she is: AURA… a neural network… But something in it has gone wrong." | The neurons, then the connections, are named and acted out. On "gone wrong" the light falls back to the first layers. |
| **Guide (new, 31):** "Here's one connection, up close. Push the rock lever up, until this neuron fires." | **Hands-on beat 2:** ROCK from 0 to ≥ 1.5, 12 s timeout. Below the line the panel's neuron stays small. At the line it fires (pops, "IT FIRED!", the solve chime, a haptic pulse), and the big network answers with a flash. |
| Guide (20, reworded): "That lever is a weight… Change the weights, and you change what the AI thinks." | The words land on what the rider just did. |
| Plan, then "pull the orange GO lever" | As before. `GuideOrange` (22) and `GuideHardProblem` (17) are no longer in the intro. Line 30 teaches the orange rule at the moment it is used. |

Tests:
- EditMode `RideNarrationTests.Intro_PutsTheRidersHandsOnALever_EarlyAndOften` checks the first hands-on beat within 15 s, at most 30 s of talk between hands-on moments, real control ids, and a timeout on every beat.
- PlayMode: `Intro_PullingGoToWakeAura_WakesHer_AndHoldingOnDoesNotSkip`, `Intro_MakeItFire_WaitsForTheWeightToReachTheLine` and `Intro_WithNobodyTouchingAnything_TimesOutAndStillReachesTheDock`.

### 3. The network is the only bright, living thing

- **Connections** (`NeuralLinks.shader`):
  - Width `_Width × (0.25 + 0.75 r)` on 2.2 to 9 px (was 1.6 to 4).
  - Brightness `0.15 + 0.85 r²` (strong weights bright, weak ones faint).
  - A crisper core (`pow(across, 6) × 1.1`) with a softer glow (× 0.35).
- **Unlit network** at 30 % (`networkDim` 0.62 → 0.3). The part lit by solving stops is what stands out, so the network visibly lights chapter by chapter.
- **At a stop** the network steps back less (`stopFocus` 0.4 → 0.55).
- **Scaffolding is not network.** The rails and the gates (now every 14 m) moved to their own mesh, `Structure`, with its own material `NetStructure`: thin, dim slate, always lit, never driven. That is one extra draw call: the network is now 3.
- **Background recedes.** Dust is desaturated to cool grey at 45 % brightness, and haze strength drops from 0.28 to 0.16. Neurons are a little bigger, and the three dock neurons are 30 % brighter.
- **`_Reveal`** (0 to 1, default 1) on both network shaders shows connections and neurons one by one by their baked phase. It drives the JFK cold open. Haze ignores it.

### Layering

- Gameplay paces from numbers and never touches visuals or audio.
- Presentation (`NeuralCoreView`, `WeightDemoView`, `RideSfxView`) only reacts to `RiderTask*`, cues and states. The fire line's value comes from the director.
- XR (`PodLeverInteractable`) only pulses the hand that is holding the lever named in `RiderTaskDone`.
- `DashScreenView.CurrentSegmentSeconds` lets views time a reaction to a clause's words, which is how the flash lands on "hard".

## Consequences

- The first input moves from about 75 s to about 13 s (the wake prompt starts at 13.1 s), and the rider acts twice before the dock.
- The talk is 68 s, not the 60 s target: line 19 rendered at 14.5 s. Trim its first sentence if the headset play-through feels long.
- Talk between hands-on moments: 21.5 s from the wake to the fire beat, and 28 s from the fire beat to the dock (where the weight and GO are live).
- The editor's budget estimate rises by one draw call per stop (111 / 115 / 99), from the structure mesh. Part 1 measured 72 / 103 / 77 in Play Mode.
- Wider lines add additive overdraw near the rider (9 px cap). Check frame time on the Quest during the wake wave.
- **Not verified:** any of this on a headset, by ear, or in the hand (haptics). The voice lines were checked by transcription (0 % word error) and loudness (−16 LUFS), not by listening.
