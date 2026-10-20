# HANDOFF — Ride v3 "Ignite", part 2 (state at 2026-10-09, end of the audio + explainer session)

For the next agent. Read this first, then `AGENTS.md`, `.agents/PROJECT_MAP.md`, `.agents/WORKFLOW.md`, the master plan
(`TASK_IGNITE_RIDE_V3_MASTER_PLAN.md`, sections 0, 4 and 8), the part-1 handoff (`HANDOFF_IGNITE_RIDE_V3.md`, section 5
"Traps" especially) and ADR-012 / ADR-013 / ADR-014.

**Evidence rule.** *Verified* = observed in Unity this session (tests, console, or a rider-eye screenshot that was looked at).
*Not verified* = nobody has seen or heard it. Nothing below claims more than that. **Nobody has listened to any audio**
(the agent cannot hear); voices were chosen and checked by measurement.

> **Update, session 3 (2026-10-09): priorities 1 and 2 are done; see section 10 at the end.** Sections 0 to 9 describe
> the state before that session; section 10 says what changed and what is still open.

---

## 0. State in twelve lines

- Everything is still **uncommitted** on `main` (part 1's work plus this session's). The owner decides how to commit.
- **Verified:** the project compiles; `Convergence > Build Neural Ride` passes every build check; **EditMode 86/86**,
  **PlayMode ride tests 8/8** (all on the final build); the whole ride played end to end twice in Play Mode with a
  screenshot at every narration cue (curated set: `Documentation/Design/captures/ride_v3_explainer/`).
- **Voices are in:** 28 of 28 lines bound. Guide = Kokoro `bm_fable`, AURA = Kokoro `af_heart` (Apache-2.0, local),
  picked by measurement at the owner's request. Real archival JFK (Rice 1962) and NASA Apollo 11 liftoff clips.
- **Sound effects are in:** hum, engine, launch, dock, zaps, lever ticks, solve chimes, learning ticks, finale (Kenney CC0).
- **The ride shows what it says:** every narration clause can carry a `{cue}`; an explainer panel under the chapter
  card shows a diagram for it, the big network acts out "neurons"/"connections", the GO arrow points on "orange".
- **No explanation is cut off any more:** a solved stop waits for its punchline line to finish.
- **One hands-on moment:** at the dock the ROCK lever is a weight the rider can play with before launching.
- Reading screens (chapter card, explainer, credits) are solid, so the network's lines no longer cross the text.
- Credits panel under the finale lists every voice, music and sound source.
- **Not verified:** anything on a headset; any sound by ear; levels at 50 % Quest volume; frame time with the panel up.
- The owner's newest asks (section 1) are **not started**: the opening hook, and the network as the star.
- UnityMCP works now (server name `UnityMCP`, one instance `xlabs-neural-net-game-@1eb301227e6d3d15`).

---

## 1. Your mission, in the owner's words (in this order)

1. **"Make the opening scene awesome and interactive. It needs to be amazing to really HOOK a player."**
2. **"The lines of the neural net are kinda hard to distinguish."** and **"Make sure the neural net is the most important
   thing in this whole entire ride."**
3. **"Did we set up the rule for using subagents to look at assets from different angles?"** Now yes: `AGENTS.md`
   "Required validation" item 6, and `.claude/agents/vr-qa.md`. Use it for 1 and 2.
4. Human items still open (cannot be done by an agent): a headset play-through, one listen to the voices (approve or
   swap, see `ArtSource/voice/README.md`), the `telstar.wav` license, the teacher's OK on the script wording.

Sections 2 and 3 are concrete plans for 1 and 2. They are proposals: improve on them, but keep the rules in section 6.

---

## 2. Plan: make the opening hook (priority 1)

### What the opening is today (verified, see the `01`–`05` captures)
Intro, 74 s of narration at the dock, no input except "hold GO 2 s to skip": JFK (8.5 s) → "every great journey…" →
AURA "Targeting… offline" (explainer: TARGETING OFFLINE) → the Guide explains AURA's brain (the network wakes, then
flickers) → "Look ahead" (neurons flare, then connections, then the weight demo +1, then the weight sweeps) → the plan
(three tiles light one by one) → "anything orange you can grab" (GO arrow) → "pull the orange GO lever". Then the dock:
the ROCK lever is live as a weight toy until GO.

The problem: the first input is at about 75 s. A first-timer at Ignite stands still and listens for over a minute.

### Proposed shape (the rule: the rider's hands do something within 15 seconds, and every 15 to 20 s after that)
1. **Cold open in the dark (0 to 10 s).** The network starts fully dark. JFK speaks; neurons ignite one by one in time
   with his words (his word timestamps are in `ArtSource/archival/SOURCES.md` and the sidecar `Segments:`), and on
   "because they are hard" the whole network flashes once and goes dark again. Music sting on the flash.
   (New network cue, for example `jfk_build`, driven per clause or per word; `NeuralCoreView` already has `_cueFlash`.)
2. **The rider wakes the ship (10 to 20 s).** "Ours is inside this ship." AURA: "Targeting… offline." Then a new short
   Guide line: "Grab the orange lever and pull it. Wake her up." The GO lever (the only live lever) becomes a **power
   switch** for this beat: pulling it ignites the network layer by layer from the pod outward (a wave along
   `_LitUpTo`), with a rising engine sound and the hum swelling. **It must time out** (auto-wake after ~8 s) so no one
   gets stuck, and the operator skip must still work (hold GO 2 s = skip the whole intro: keep that distinct from
   a single pull, see `RideDirector.Update`).
3. **First "aha" with their own hands (25 to 45 s).** Move the existing weight demo from the dock into the intro as a
   mini goal: "Make this neuron fire." The rider turns the ROCK weight until the output neuron glows past a line;
   success chime, and the matching part of the big network lights. Then the Guide's weights line lands on something
   they just did. (Reuse `WeightDemoView`; add a "fired" threshold and an event; the director waits for it or times out.)
4. **The plan and launch (45 to 60 s).** Keep "three stops" with the tiles, then GO with Apollo.
5. Cut narration that the interaction now shows. Target: intro ≤ 60 s of narration, first input ≤ 15 s.

Implementation notes:
- Gameplay owns the flow (new intro beats that wait for an input or a timeout). Today `RunBeats` only waits on seconds.
  Add a beat kind "wait for this control (or N seconds)" to `RideScript` + `RideDirector` (numbers and ids only).
- New lines need ids appended to `RideLine` (next free: **30**; 23 is reserved, never reuse), text in
  `RideNarrationData.cs`, a render with `ArtSource/voice/render_voices.py render --lines 30,31`, `verify`, `install`, rebuild.
  Keep the words short, plain and voice-tested (section 7 lists words the voice garbled).
- Update the narration tests' budgets if the intro shape changes (`RideNarrationTests.Sequences_FitTheDemoTimeBudget`:
  intro 55 to 110 s today) and add PlayMode tests: the wake beat times out; the "make it fire" beat completes when the
  weight is right and times out when it is not; GO hold still skips.
- The wake moment is the best place for haptics (XR layer, `Assets/Scripts/XR/Ride/`): a pulse on the pulling hand.

---

## 3. Plan: the network is the star, and its lines are easy to tell apart (priority 2)

### Why the lines blur today (read the shader: `Assets/Shaders/NeuralLinks.shader`)
- Every connection is drawn 1.6 to 4 px wide (`_MinPixels`, `_MaxPixels`), so near and far, strong and weak look alike.
- Brightness only varies about 3× with the weight (`weight = 0.35 + 0.65 r`), and the not-yet-lit part of the network
  glows at 62 % (`theme.networkDim = 0.62`, set in `NeuralRideBuilder.LoadTheme`), so hundreds of additive lines
  overlap into haze.
- The "gates" every 7 m and the two rails (`NeuralRideBuilder.Network.cs`, `links.Strip(gate, 0.12f, ...)`) use the same
  shader and colours as the real connections and compete with them (the big concentric rings in every capture).

### Proposed changes (then look, from several angles, and iterate)
- Shader: width `_Width * (0.25 + 0.75 r)`; set `_MinPixels` about 2.2 and `_MaxPixels` about 9 on the material from
  the builder; brightness hierarchy `0.15 + 0.85 r²` (strong weights bright, weak ones faint); a crisper core
  (`pow(across, 6)` at about 1.1) with a softer outer glow (about 0.35). Mind Quest fill rate (additive overdraw).
- `theme.networkDim` 0.62 → about 0.3: the part the rider has lit by solving stops is what stands out, and the network
  visibly "lights up chapter by chapter" (this also feeds the opening hook).
- Gates and rails: dimmer, thinner, every 14 m, or their own subtler colour, so only the network uses bright cyan/amber.
- Neurons: a little bigger (`radius = 0.75 + 0.6 a` in the network builder) and the three dock neurons brighter.
- At stops the network steps back to `theme.stopFocus` (0.4 in `RideTheme.asset`): raise to about 0.55 so it stays
  present; then the stations' transparent plates may need `PlateSolid` too (the network shows through them). Re-run the
  build: `RideSightlines` must stay clean.
- Keep the network unhidden: the explainer panel opens only on cues; consider making briefing diagrams smaller or
  lower if captures show they cover the network's heart.
- Verify with the new rule: rider-eye shots at the dock, mid-travel, each stop and the finale, from 1.15 / 1.30 / 1.45 m
  and yaw −30° / 0° / +30°; have `vr-qa` review them; compare with `ride_v3_explainer/`.

---

## 4. What this session built (all verified unless marked)

### Audio
- **Voices:** `ArtSource/voice/render_voices.py` reads the script from `RideNarrationData.cs`, renders one sentence per
  TTS call (Kokoro-82M via kokoro-onnx), inserts the `[pX]` pauses, applies AURA's glitch filter (fading by line:
  1.0, 0.8, 0.6, 0.4, 0.25, then clean), normalizes to −16 LUFS / −1 dBTP true peak, writes `vo_ride_NN.wav` + sidecar
  (`Source:`, `Segments:` measured clause seconds, exact text), verifies by Whisper transcription, installs.
  Choice evidence (15 voices × WER × UTMOS naturalness × pitch range): `ArtSource/voice/README.md`. Locked spec:
  `ArtSource/voice_bakeoff/VOICE_SPEC.md`. Result: worst word error 2.3 %, mean naturalness 4.34, all clips
  −16.0 to −16.3 LUFS, true peak ≤ −1.0 dBTP.
- **Archival:** `ArtSource/archival/` (`SOURCES.md`, `process.py`): JFK 7.7 s (public domain, JFK Library
  JFKWHA-127-002), Apollo 11 8.8 s (NASA, Jack King). Both re-transcribed after trimming.
- **Builder:** voice import Vorbis 70 compressed-in-memory; the `Segments:` timings become caption weights
  (`NeuralRideBuilder.Narration.ApplyMeasuredTiming`), so subtitles change on the clause actually being spoken.
- **Effects:** `RideSfxView` + `NeuralRideBuilder.Audio.cs` (cue table with measured clip loudness and target loudness;
  the build fails above −20 LUFS for an effect, −30 for a loop). Derived loops/buzzer: `ArtSource/sfx/make_sfx.py` →
  `Assets/_Project/Audio/Sfx/` (CC0, see its LICENSE.txt). *Not verified by ear.*

### Narration you can see (ADR-014)
- `{cue}` tags in `RideNarrationData.cs`; `DashScreenView.SegmentShown` / `LineCleared`; reactions in
  `NeuralCoreView` (network cues), `HintArrowView` (`orange`, `go`), `ExplainerView` (about 20 diagrams built in
  `NeuralRideBuilder.Explainer.cs`, motions in `ExplainerMotion`), `ChapterCardView` (outro card waits for `xor`).
- The build fails if a cue has nothing to show or explainer text is under 1.5° or off the panel.
- `WeightDemoView`: one connection up close (intro: hold +1, then sweep; dock: the ROCK lever drives it).

### Flow
- `RideScript.lines` (every line's seconds); `RideDirector` waits after a solve for the solve line to finish
  (`speechPadSeconds` 0.6, cap `maxSpeechHoldSeconds` 15). Test: `Stop_LetsTheLineSaidAtTheSolveFinish_BeforeMovingOn`.
- Dock: the ROCK lever is live (−2 to +2) until GO; `ConfigureLevers()` relocks it at launch.

### Look
- `PlateSolid` (opaque) for the chapter card, explainer and credits; the finale stays see-through on purpose (orb).

### Tests and docs
- New: `RideNarrationTests.EveryRideLine_HasAVoiceClipMatchingTheScript`, the PlayMode stop-hold test.
- ADR-012 updated (voices chosen, archival details, credits done); **ADR-014** new; `DECISIONS_INDEX`, `PROJECT_MAP`,
  `ArtSource/AGENTS.md`, `AGENTS.md` (validation item 6), `.claude/agents/vr-qa.md` updated.

---

## 5. Numbers (verified)

- Build log: `Narration: intro 74 s, briefings 37 / 28 / 22 s, outro 27 s. Voice clips bound: 28 of 28.`
  (Budgets: intro 55–110, briefing ≤ 40, outro ≤ 40, total ≤ 240 s. Briefing 1 is the tight one: Apollo 8.8 s + line 25.)
- `RideSightlines`: clear at 1.15 / 1.30 / 1.45 m at every stop.
- `RideBudget` (editor estimate, counts every renderer as a draw call): stops 110 / 114 / 98. Unchanged by this session
  (the explainer, finale and card are skipped as hidden-until-their-moment). Part 1 measured 72 / 103 / 77 in Play Mode.
- Audio sources at runtime: 12 (Quest limit 32).

---

## 6. Rules that still hold (do not break these)

- The scene is generated: change `Tools/Editor/NeuralRideBuilder*.cs`, then `Convergence > Build Neural Ride`.
- Layers: Gameplay paces from numbers and never touches AudioClips or materials; Presentation observes and decides
  nothing; Core stays Unity-free; `Tools/` is dev-only.
- Orange means grabbable. Nothing else is orange (the explainer's knob icon is a cyan copy for that reason).
- `RideLine` ids are never renumbered; 23 is reserved; append from 30.
- A voice clip only binds if its sidecar text matches the script; never edit a sidecar by hand to pass the check.
- The agent cannot hear: judge audio by measurement and ask the human to listen.
- Report every command and its output; never claim an unrun test passed (`AGENTS.md`).

---

## 7. Traps found this session (plus part 1, section 5)

1. **The network draws over transparent plates.** Its shaders are in the plain Transparent queue; panel plates sit at
   −10 so their text draws after them. Anything that must be read in front of the network needs `PlateSolid`.
2. **Windows path length.** `pip install torch` into a venv under the long scratchpad path fails (WinError 206). Put
   venvs under `C:\Users\Panda\.cache\` (existing: `utmos` = CPU torch + librosa for UTMOS/prosody; `archival\venv`).
   The Kokoro venv and model files used this session were in the session scratchpad and may be gone: recreate with
   `python -m venv C:\Users\Panda\.cache\tts` then `pip install kokoro-onnx soundfile pyloudnorm scipy numpy faster-whisper`,
   and put `kokoro-v1.0.onnx` + `voices-v1.0.bin` (kokoro-onnx GitHub release `model-files-v1.0`) in `ArtSource/voice/models/`.
3. **faster-whisper + PyAV mismatch:** pass a 16 kHz numpy array, not a file path (`render_voices.py verify` does).
4. **Words the voice garbled** (fixed with spoken spellings or per-line speed in `render_voices.py`): "GO lever"
   (heard "GoLaver"; spoken "go-lever"), a lone "Lower." (heard "slower" below speed 1.0), "Too big a step"
   ("two biggest step" above 0.9), "Whoa, you zapped a drone" (needs its own beat), AURA's stutter on a first word.
5. **Executing a menu item right after editing scripts** can trigger a domain reload that silently cancels the build.
   Refresh/compile first, check the console, then run the menu item.
6. **MCP `execute_code` blocks `Directory.Delete`** (safety filter); write to a new folder instead.
7. **Exiting Play Mode via MCP can hang in "playmode_transition"**; send `stop` again and re-read `editor/state`.
8. **Capturing at `Time.timeScale = 2`** makes subtitles linger (audio plays at real time): judge pacing at 1×.
9. Station lines `LowerTrigger` (3), `TurnPipes` (7) and `Converged` (13) are rendered but **never said**: nothing in
   `StationController` triggers them. Candidates for hints if wanted.

---

## 8. How to drive Unity (MCP is live)

- Compile: `refresh_unity` (compile=request, mode=force), wait ~25 s, `read_console` (types error), and confirm types
  with `execute_code` (the editor can look fine while compilation failed).
- Build: `execute_menu_item` "Convergence/Build Neural Ride", then `read_console` filtered on "Ride".
- Tests: `run_tests` EditMode (all, about 1 s), PlayMode `group_names: ["RideDirectorTests"]`, init_timeout 120000
  (about 63 s); poll with `get_test_job` wait_timeout 120 to 180.
- Look: `manage_editor` play, then `execute_code` with `Convergence.EditorTools.RideLiveShot.Shoot(path, eye, fov, yaw, pitch)`.
  To shoot at narration cues, subscribe to `DashScreenView.SegmentShown` inside `execute_code` and shoot from an
  `EditorApplication.update` callback (the script is in the Appendix; it drives the levers like the PlayMode test).
  The Unity CLI from part 1 (`unity command ...`) still works as a fallback.

---

## 9. Files touched this session (all uncommitted)

New: `ArtSource/voice/{render_voices.py,README.md,.gitignore}`, `ArtSource/archival/{SOURCES.md,process.py,vo_ride_16.*,vo_ride_24.*,vo_ride_16_alt_leadin.wav}`,
`ArtSource/sfx/make_sfx.py`, `Assets/_Project/Audio/Sfx/{ride_hum_loop,ride_engine_loop,ride_wrong}.wav + LICENSE.txt`,
`Assets/Scripts/Presentation/Ride/{RideSfxView,ExplainerView,ExplainerMotion,WeightDemoView}.cs`,
`Tools/Editor/NeuralRideBuilder.{Audio,Explainer}.cs`, `Documentation/Architecture/ADRs/ADR-014-ride-narration-you-can-see.md`,
`Documentation/Design/captures/ride_v3_explainer/`, this file.

Changed: `Assets/_Project/Audio/Voice/vo_ride_01..29` (+ sidecars; the old robotic 01–15 were replaced),
`Assets/Scripts/Gameplay/Ride/{RideDirector,RideScript}.cs`, `Assets/Scripts/Presentation/Ride/{DashScreenView,NarrationLibrary,NeuralCoreView,HintArrowView,ChapterCardView,FinaleView,DataStreamView}.cs`,
`Tools/Editor/{NeuralRideBuilder,NeuralRideBuilder.Narration,RideNarrationData,RideSightlines}.cs`,
`Tests/EditMode/Ride/RideNarrationTests.cs`, `Tests/PlayMode/Gameplay/RideDirectorTests.cs`, generated scene/prefabs/
materials/ScriptableObjects, `ADR-012`, `DECISIONS_INDEX.md`, `PROJECT_MAP.md`, `ArtSource/AGENTS.md`,
`ArtSource/voice_bakeoff/{VOICE_SPEC.md,key.txt}`, `AGENTS.md`, `.claude/agents/vr-qa.md`. Commit `.meta` files with their assets.

Optional, parked: a Qwen3-TTS comparison voice (a helper agent downloaded its models, probably under
`C:\Users\Panda\.cache\`, before a rate limit stopped it; nothing was rendered). Only worth resuming if the human's
listen rejects Kokoro.

---

## Appendix: the cue-capture script (paste into MCP `execute_code` right after `manage_editor play`)

It shoots the rider's view 0.55 s after each listed cue starts, plays with the dock weight, solves each stop like the
PlayMode test, and writes `done.txt` when the finale has shown. Change `dir`, the cue list, and the `Shoot` arguments
(eye, fov, yaw, pitch) to cover more angles. Runs at 2x speed (judge pacing at 1x).

```csharp
var dash = UnityEngine.Object.FindFirstObjectByType<Convergence.Presentation.Ride.DashScreenView>();
var director = UnityEngine.Object.FindFirstObjectByType<Convergence.Gameplay.Ride.RideDirector>();
if (dash == null || director == null) return "not running yet";
string dir = @"C:\path\to\new\folder\";   // a NEW folder each run (Directory.Delete is blocked)
System.IO.Directory.CreateDirectory(dir);
var want = new System.Collections.Generic.HashSet<string>(new[] { "offline", "neurons", "links", "weights", "plan2", "sum", "fire", "knob", "negative", "error", "downhill", "recap", "xor" });
var due = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float, string>>();
int n = 0;
dash.SegmentShown += (line, index, seg) => {
    if (seg.cues == null) return;
    foreach (string c in seg.cues) if (want.Contains(c)) due.Add(new System.Collections.Generic.KeyValuePair<float, string>(Time.realtimeSinceStartup + 0.55f, c));
};
float dockAt = -1f; int dockStep = 0; float doneAt = -1f; bool[] solving = new bool[3];
EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!Application.isPlaying) { EditorApplication.update -= tick; return; }
    float now = Time.realtimeSinceStartup;
    for (int i = due.Count - 1; i >= 0; i--) {
        if (now < due[i].Key) continue;
        n++; Convergence.EditorTools.RideLiveShot.Shoot(dir + n.ToString("00") + "_" + due[i].Value + ".png", 1.3f, 75f, 0f, 0f);
        due.RemoveAt(i);
    }
    var controls = director.Controls; var state = director.State;
    if (state == Convergence.Gameplay.Ride.RideState.Dock) {
        if (dockAt < 0f) dockAt = now; float t = now - dockAt;
        if (dockStep == 0 && t > 0.4f) { controls.Get(Convergence.Gameplay.Ride.RideControlIds.Rock).SetFromUser(2f); dockStep = 1; }
        if (dockStep == 1 && t > 1.6f) { Convergence.EditorTools.RideLiveShot.Shoot(dir + "dock.png", 1.3f, 75f, 0f, 0f); dockStep = 2; }
        if (dockStep == 2 && t > 1.8f) { controls.Get(Convergence.Gameplay.Ride.RideControlIds.Action).SetFromUser(1f); dockStep = 3; }
    }
    if (state == Convergence.Gameplay.Ride.RideState.AtStop) {
        int s = director.StopIndex;
        if (!solving[s]) {
            solving[s] = true;
            if (s == 0) controls.Get(Convergence.Gameplay.Ride.RideControlIds.Trigger).SetFromUser(0.5f);
            if (s == 1) { controls.Get(Convergence.Gameplay.Ride.RideControlIds.Rock).SetFromUser(1f); controls.Get(Convergence.Gameplay.Ride.RideControlIds.Ice).SetFromUser(1f); controls.Get(Convergence.Gameplay.Ride.RideControlIds.Trigger).SetFromUser(0.5f); }
            if (s == 2) controls.Get(Convergence.Gameplay.Ride.RideControlIds.Action).SetFromUser(1f);
        }
    }
    if (state == Convergence.Gameplay.Ride.RideState.Complete) {
        if (doneAt < 0f) doneAt = now;
        if (now - doneAt > 7f) {
            Convergence.EditorTools.RideLiveShot.Shoot(dir + "finale.png", 1.3f, 75f, 0f, 0f);
            Time.timeScale = 1f; EditorApplication.update -= tick;
            System.IO.File.WriteAllText(dir + "done.txt", "done " + n);
        }
    }
};
EditorApplication.update += tick;
Time.timeScale = 2f;
return "armed at state " + director.State;
```


---

## 10. Session 3 result: priorities 1 and 2 (2026-10-09)

Decision record: **ADR-015** (`Documentation/Architecture/ADRs/ADR-015-ride-interactive-opening.md`). Curated captures:
`Documentation/Design/captures/ride_v3_hook/`. Everything is still uncommitted.

**Verified in Unity:**
- Compile is clean (only the old deprecation warnings).
- `Convergence > Build Neural Ride` passes:
  - `Narration: intro 68 s, briefings 37 / 28 / 22 s, outro 27 s. Voice clips bound: 30 of 30.`
  - Network: 3 draw calls.
  - `RideSightlines` clear at all three eye heights.
- EditMode tests: 87/87.
- PlayMode `RideDirectorTests`: 11/11.
- Both validators pass.
- A full scripted ride was captured from 5 angles at the dock, briefing 1, each stop and the finale, plus 15 intro moments. The captures were reviewed by `vr-qa` (findings below).

**What changed:**
- **Hands-on intro beats.** `RideScript.Beat.waitFor/waitFrom/waitAtLeast/waitTimeout`, `RideDirector.RiderBeat` and the `RiderTaskStarted` / `RiderTaskDone` events. The data is `RideNarrationData.IntroTasks`.
  - Wake: pull GO, or it auto-wakes 8 s after the line.
  - Fire: ROCK from 0 to ≥ 1.5, or the ride does it after 12 s.
  - The wake pull never counts as the operator's 2 s skip until GO has been released.
- **New intro order:** JFK → AURA offline → **30** "Anything orange, you can grab. Pull the orange lever, and wake her up." → **19** (reworded) → **31** "Here's one connection, up close. Push the rock lever up, until this neuron fires." → **20** (reworded) → plan → launch line. Lines 17 and 22 stay rendered but are not in the intro.
- **Voices:** 19, 20, 30 and 31 were rendered with Kokoro and passed transcription at 0 % word error and −16 LUFS. One render-side spelling was added: "orange lever" → "orange-lever" (heard as "laver"). **Nobody has listened to them.**
- **Network:**
  - New shader weight hierarchy (width and brightness).
  - `networkDim` 0.3 and `stopFocus` 0.55.
  - Rails and gates moved to a dim slate `Structure` mesh, with gates every 14 m.
  - Dust and haze toned down; bigger neurons; brighter dock neurons.
  - `_Reveal` drives the JFK cold open.
- **Views:**
  - `NeuralCoreView`: cold open, flash on "hard", wake wave, fall-back on "gone wrong".
  - `WeightDemoView`: fire goal; the neuron's core now grows with its glow.
  - `RideSfxView`: wake rumble and shimmer, hum swell, fire chime, JFK sting.
  - `PodLeverInteractable`: a haptic pulse at both beats.

**Still open (human):**
- A headset run: feel the haptics, check frame time during the wake wave (wider additive lines), see the cold open in a real HMD.
- One listen to lines 19, 20, 30 and 31.
- The intro talk is 68 s against a 60 s target; line 19 is 14.5 s. Trim it after the headset run if it drags.

**`vr-qa` review (45 captures) and what was done about it:**
- Fixed:
  - The fire goal now names its number ("MAKE IT FIRE: ROCK LEVER TO +1.5").
  - "IT FIRED!" turns bold cyan.
  - The panel's neuron core grows with its glow: a dim dot one detent short, a big bright glow at the line.
  - The big network flashes harder on the fire.
- Confirmed by vr-qa: the gate rings are gone, the stop readouts are no longer crossed by bright streaks, and the network is the star in the intro and at the dock.
- Not a bug: captions lag the wake in the captures because the scripted rider pulled mid-line, and the line is meant to finish.
- Open, and already the same in the `ride_v3_explainer` baseline:
  1. At eye 1.45 m and pitch 0 the lever knobs and the bottom caption line sit below the frame. Riders look down, and the hint arrow points there, but consider a "look down" glow on GO at the wake beat.
  2. The explainer panel covers the network's heart during the intro diagrams. It could sit lower or smaller; re-run `RideSightlines` if it moves.
  3. At stops, the orange knob overlaps the end of a 3-line caption (s1), and "weight" and "×1.0" labels collide with their arcs (s2, s3).
  4. In the finale, the orb's white core and two streaks cross "TO BE CONTINUED…", and the trained network isn't shown.
  5. The dash's neon top stripe is brighter than any network line.
- vr-qa estimated some labels at about 1.2°. That is a screenshot estimate; the build's geometric check enforces ≥ 1.5° and passes.

**Traps found:**
- Arm a Play Mode capture from `SceneManager.sceneLoaded` after `LoadScene`. Otherwise the first narration cues fire before `execute_code` can subscribe.
- The `RideLiveShot` names use `(int)(h*100)`, so 1.15 m is written as `h114`.

---

## 11. Session 3b: live-playtest check and the layout fixes (2026-10-09)

**Live playtest (Play Mode through UnityMCP, scripted "stranger" rider).** The rider hesitates, keeps holding GO after the wake, misses the fire line by one detent, sets the stop 1 trigger too low, leaves the ice weight backwards, and picks learning rate 2 first. Logs and numbers are in `Documentation/Design/captures/ride_v3_hook/playtest_*.txt`.
- **Pacing:** the whole ride takes 266.8 s (4 min 27 s) with those mistakes. The first hands-on moment is at 12.4 s.
- **Recovery:** every mistake triggers its reaction line and a hint, and every fix solves the stop. Holding GO after the wake did not skip the intro.
- **Comfort:** yaw at most 3.0 °/s and acceleration at most 1.75 m/s² (the final roll), sampled once per game frame. An earlier 6.45 m/s² reading was editor-loop sampling noise.
- **Audio:**
  - Voice lines never overlap (at most 1).
  - The loudest real-time moment was −0.4 dBFS: zap + "wrong" buzzer + voice at the stop 1 mistake. An earlier run touched full scale at the stop 2 solve.
  - Fix: the zap target drops from −22 to −24 LUFS and the solve chime from −21 to −22 LUFS (`NeuralRideBuilder.Audio.cs`). It has not been re-measured since.
- **Photosensitivity:**
  - The "damaged network" flicker produced up to **6 flashes in one second** (mean 3.7/s) over a large part of the view, above the WCAG 2.3.1 limit of 3.
  - Fixed: `NeuralCoreView.flickerRate` is now 2, measured at a worst second of 2 flashes and a mean of 0.9/s.
  - Other animations are small icons, not big brightness changes.
- **Editor frame cost:**
  - Mean 2.3 ms. The ~250 ms worst frames are the capture frames.
  - At most 100 draw calls live at stop 2. That is at the Quest budget; check it with the OVR Metrics Tool on the headset.

**Layout fixes (the vr-qa list; in the headset every eye is set to 1.30 m by `PodSeat`, so 1.30 is the design case):**
- **Dash:**
  - Levers sit 2.2 cm lower and the subtitle 1.5 cm higher, so a lever at the top never covers the 3rd subtitle line.
  - The panel is now 1.2 × 0.52 m, so the learning-rate labels stay on it.
  - The trims and the tub rim are dim cyan; they had been the brightest lines in view.
- **Stops:** the weight labels are one line ("×1.0") above the rock wire and below the ice wire. The pipe labels sit above their arcs. The first try enlarged the diagram plate, and `RideSightlines` failed the build because the plate then blocked the equation panel. That was reverted.
- **Explainer:** it moved from 8.5° to 12.5° down, so the network's hub stays visible above it.
- **Finale:**
  - The text stacks above AURA's core, so the orb glows in the open.
  - The credits are 4 lines, 2.05 m away and 12° down, at 1.7° cap height (was 1.15°, under the 1.5° floor).
  - The dash's top edge is really about 19.6° down, not the 26° an old comment claimed.
- **Second vr-qa review:** fixes 1, 3 and 5 confirmed. Fix 2 is mostly confirmed: the reviewer still saw one streak line through stop 1's "×1.0" label and the stop 1 numbers washing out on the orb halo, and those are open. The finale core is clear of the text, but the orb's radial beams still cross it. The credits and "CRAZY" label were fixed after that review; I checked them myself in `14_finale.png` (no reviewer has).
- **Left alone:** the stop 3 error landscape is bright yellow because high error is amber by design; it is the lesson object.

**Quest settings audit (read-only; changing these needs the owner's OK under `AGENTS.md`):**
- **OK:** the XR loaders on Android and PC, the Quest 2 OpenXR feature, Touch controller profiles, NeuralRide first in the build, and stereo macros in the custom shaders.
- **Risks:**
  1. The Android scripting backend is not set; it should be IL2CPP. Make a test APK early.
  2. URP MSAA is 2× (4× is better for thin lines).
  3. A `DecalRendererFeature` is active with no decals, which forces a depth texture.
  4. The URP asset is a Samples asset (`Assets/Samples/...`).
  5. Standalone (Link) renders multi-pass, while the APK uses single-pass instanced.
  6. The product name is still "My project (2)".

---

## 12. Session 4: closing the vr-qa leftovers (2026-10-09)

**Changed** (all in `Tools/Editor/NeuralRideBuilder.cs`, then `Convergence > Build Neural Ride`):
- New helper `LabelBacking(label, widest, pad)`: a small `PlateSolid` tag parented to a label, sized for its widest text.
  It follows the label when a view moves it and writes depth, so additive network lines and halos stop at its edge.
- Stop core: the cyan `sum` label gets a tag (it washed out on the core's cyan halo). The white `fire at` label is left
  bare on purpose (readable, and each renderer costs a draw call at the stops).
- Network diagram plate (stops 1 to 3): `Plate` → `PlateSolid`, so network lines no longer cross its `×` weight labels.
- Finale: tags behind "YOU TAUGHT THE AI!" and "TO BE CONTINUED…". The big plate stays see-through, so the network still
  shows around the words and the orb glows in the open below.
- Tried and reverted: a tag on the pipe's `×1.0` label. `RideSightlines` failed the build because the tag hid "fire at"
  at eye 1.15 and 1.30 m. The validator did its job.

**Verified in Unity this session:**
- Build passes. Narration intro 68 s, voice clips 30 of 30, network 3 draw calls.
  `RideSightlines`: clear at 1.15 / 1.30 / 1.45 m. `RideBudget` editor estimate: 112 / 116 / 99 (it counts every renderer).
- EditMode 87/87.
- Scripted Play Mode run (`Documentation/Design/captures/ride_v3_finish/`, `run_log.txt`). Each stop was shot at
  3 heights × yaw −30/0/+30; the finale at yaw −30/0/+30 plus 1.15 and 1.45 m.
  - **Live draw calls at the stops: max 100 (stop 2).** That is the same as session 3b, still at the Quest budget.
  - **Audio re-measured after the zap/chime cut:** the worst output peak at the stop 1 mistake (zap + buzzer + voice)
    is **−1.6 dBFS** (it was −0.4). No clipping.
- Looked at myself: `s1_h130_y0.png` (the sum label is crisp; the diagram has no lines through it) and `finale_y0.png` (beams stop at
  the text). **No `vr-qa` review of this set yet**: the session ran out of time. Next agent: run `vr-qa` on `ride_v3_finish/`.

**Still open:**
- The pipe `×` labels moved from x ±0.95 to ±1.2 to clear the core halo. The build and `RideSightlines` pass, but nobody has
  looked at a capture since the move.
- The finale still does not show the "trained network" (vr-qa item 4, second half).
- Everything in section 10 "Still open (human)" and the section 11 Quest settings audit (owner's OK needed).
- Asset Forge pilot (WP7) not started.
- Nothing is committed. The owner decides how.
