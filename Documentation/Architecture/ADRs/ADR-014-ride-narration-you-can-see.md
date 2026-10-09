# ADR-014: Narration You Can See, Never Cut Off, and the Ride's Sound

| Field | Value |
|---|---|
| ID | ADR-014 |
| Date | 2026-10-09 |
| Status | Accepted (built, tested in Play Mode and by EditMode/PlayMode tests; not yet heard or seen on a headset) |
| Deciders | Project owner (requests of 2026-10-09); Ride v3 work |

## Context

With real voices bound (ADR-012), the owner raised three things about the narrated ride:

1. "Make sure the audios give a full explanation before going off." The pod left a solved stop after a fixed 3 s, and the next briefing (a scripted line) cut off the stop's punchline, which is 6 to 8 s long ("That's it. Signal times weight, compared to a trigger. That's all a neuron is: math.").
2. "Users can see what the explainer is talking about as he explains, so they don't have to sit there bored." The intro is 74 s and the briefings 22 to 37 s of listening, with only a chapter card to look at.
3. "Making it interactable may not even be a bad idea."

The ride also had no sound effects at all: only voice and music.

## Decision

### 1. A stop waits for the line said at its solve

`RideScript` now carries every line's seconds (`lines`, written by the builder from the clip lengths). The director records when the newest station line will end (`_speechEndsAt`) and, after a solve, waits for the later of the fixed `advanceDelay` and that line's end plus `speechPadSeconds` (0.6 s), capped at `maxSpeechHoldSeconds` (15 s) so a live demo can never stall. Gameplay still paces from numbers only and never touches audio. Test: `RideDirectorTests.Stop_LetsTheLineSaidAtTheSolveFinish_BeforeMovingOn`.

### 2. Cues in the script; views act them out

- A clause in `Tools/Editor/RideNarrationData.cs` may open with `{cue}` (or `{a,b}`), for example `{neurons}Every glowing point is a neuron.` The tag is never spoken or shown; it becomes `NarrationLibrary.Segment.cues`. Only pause marks were added to the approved wording.
- `DashScreenView` is the one component that knows which clause the voice is on (it already advanced the subtitles by the measured clause timings). It raises `SegmentShown(line, index, segment)` and `LineCleared`.
- Views that know a cue react; everything else ignores it:
  - `NeuralCoreView`: `neurons` (neurons flare, connections step back), `links` (the reverse, pulses race), `net_wake`, `net_broken` (smooth flicker), `heart`, `billions`, `liftoff` (flashes).
  - `HintArrowView`: `orange`, `go` (the GO arrow while it is named; a gameplay hint always wins).
  - `ExplainerView`: a panel 4.2 m out, 8.5° down, directly under the chapter card, with one diagram per cue (offline/online, rock or friend, the plan's three tiles, signals adding to a total that crosses the trigger and fires, rocks vs drones, the volume knob, two sensors, the backwards wire, the learning rule, the machine tuning itself, guess vs answer, the error hill with a rolling ball, repeat, today's AI, the recap, a billion weights, XOR, hidden layers). Diagrams are built by `Tools/Editor/NeuralRideBuilder.Explainer.cs` from flat bars, the ride's icons, halos and text; motions are `ExplainerMotion`.
  - `ChapterCardView`: the outro card waits for `xor` (the 1969 line) instead of showing over the recap.
- The build fails if the script names a cue nothing shows, or if explainer text is under the 1.5° floor or runs off the panel.

### 3. One hands-on moment: the dock's weight

While the rider decides to launch, the director unlocks the ROCK lever (−2 to +2). `WeightDemoView` shows one connection up close (input neuron, a wire whose thickness and colour are the weight, the neuron it feeds, which glows as much as it hears), and at the dock the lever *is* that weight ("TRY IT: MOVE THE ROCK LEVER"). The lever locks again when GO is pulled. During the intro the same demo holds +1 on "every connection has a number called a weight" and sweeps through negative on "change the weights, and you change what the AI thinks".

### 4. Reading screens are solid

The network's additive shaders render in the plain Transparent queue, after the transparent panel plates, so its lines crossed the text. The chapter card, the explainer and the credits use an opaque `PlateSolid` (depth-writing) plate. The finale panel stays see-through on purpose (the orb glows behind it).

### 5. Sound effects, levelled by measurement

`RideSfxView` (Presentation, observes only) plays: a cockpit hum (whole ride), an engine loop that follows the pod's speed (both duck under speech), launch thrust and rumble, docking, a stop going live, a sensor tick, the zap at the zapped object's position, a shatter or a buzzer (a zapped drone), lever detent ticks (pitch follows the lever), solve chimes, falling learning ticks (pitch follows the error), the chapter card whoosh, the finale swell, and a skip click. Clips are the CC0 Kenney packs plus seamless loops and a buzzer made by `ArtSource/sfx/make_sfx.py`. Every level is computed from the clip's measured loudness and a target (voice −16 LUFS; effects −21 to −34; loops −31 and −38); the build fails if an effect is louder than −20 LUFS or a loop above −30. Eight pooled sources, no allocation during play; voice and effects import as Vorbis 70.

### 6. Credits

`RideNarrationData.Credits` (every voice, music and sound source with its license) appears under the finale.

## Consequences

- The ride holds a little longer after each solve (stop 1 about 8 s instead of 3), and never cuts the lesson short.
- Changing the script now means: edit `RideNarrationData.cs` (words, pauses, cues), re-render that line (`ArtSource/voice/render_voices.py`), verify, install, rebuild. The builder checks the cues and the sidecar text.
- The ROCK lever is live at the dock. The PlayMode tests are unchanged and pass; `ConfigureLevers()` relocks it at launch.
- Explainer and effects cost nothing while hidden or silent; the explainer is skipped by the sightline and budget checks like the chapter card (hidden until its moment).
- Not verified: anything on a Quest 2 (frame time with the panel open, the effects at 50 % volume, whether the diagrams read at headset resolution).
