# Task: Opening recording and teleprompter

Status: complete
Owner: Presentation
Created: 2026-10-03
Updated: 2026-10-03

## Description

Play the player's own voice at the start of Level 1 and show the words on the existing holographic banner in time with the recording. Add a quiet ship bed and real interaction sounds from the Kenney clips already in the project. Do not generate a voice. Do not use text-to-speech.

The leftover handoff cleanup is already done. Do not delete more task files.

## Source recording

`C:\Users\Panda\OneDrive\Documents\Sound Recordings\neural recording 1.m4a`

Length: 18.24 seconds.

Convert it with ffmpeg to Vorbis ogg and place the result at `Assets/_Project/Audio/Voice/vo_opening_neural_recording.ogg`. Leave the original m4a where it is. Copy this transcript into `Assets/_Project/Audio/Voice/vo_opening_neural_recording.txt` beside the clip.

## Transcript

Warning, passengers of Neural. This is not a drill. Our ship has lost connection. It is your job to reconnect it, or we will be stuck in space forever. Warning, warning, warning, warning.

The first "Warning" and the word "Neural" were the only unclear words. Keep this wording.

Show each line from its start until the next line starts. Clear the banner at 16.72 seconds.

- 0.00–2.70s: Warning, passengers of Neural.
- 3.06–4.62s: This is not a drill.
- 5.16–6.72s: Our ship has lost connection.
- 7.24–11.72s: It is your job to reconnect it, or we will be stuck in space forever.
- 12.48–16.72s: Warning, warning, warning, warning.

Word timings, in seconds:

- 0.00–1.24 Warning,
- 1.60–1.98 passengers
- 1.98–2.38 of
- 2.38–2.70 Neural.
- 3.06–3.50 This
- 3.50–3.80 is
- 3.80–4.18 not
- 4.18–4.38 a
- 4.38–4.62 drill.
- 5.16–5.50 Our
- 5.50–5.80 ship
- 5.80–6.02 has
- 6.02–6.28 lost
- 6.28–6.72 connection.
- 7.24–7.50 It
- 7.50–7.66 is
- 7.66–7.98 your
- 7.98–8.46 job
- 8.46–8.64 to
- 8.64–8.96 reconnect
- 8.96–9.30 it,
- 9.30–9.96 or
- 9.96–10.04 we
- 10.04–10.16 will
- 10.16–10.32 be
- 10.32–10.68 stuck
- 10.68–10.92 in
- 10.92–11.22 space
- 11.22–11.72 forever.
- 12.48–12.94 Warning,
- 13.72–14.22 warning,
- 14.92–15.48 warning,
- 16.20–16.72 warning.

## What to build

Gameplay owns the moment. Presentation only plays and displays.

- Add `OnOpeningBriefing` on `Assets/Scripts/Gameplay/ChamberOnboardingController.cs`. Fire it once from `RoutineAwakeningSequence`. Leave later hints on `OnAnnouncerVoicePrompt`.
- Add `OpeningTeleprompter` in `Assets/Scripts/Presentation/`. Play the ogg once from a spatial `AudioSource` on the console. Reveal each line above on the existing world-space `TextMesh`. Do not decide puzzle state.
- Under the voice, quiet only: one `computerNoise_003.ogg` tick at the start, and a low loop of `spaceEngineLow_001.ogg`. The recording stays louder than the bed.
- Stasis doors still open during the line. Do not freeze the player. Hold the next hint until 18.24 seconds so a click does not cover the take.
- Stop `Assets/Scripts/Presentation/AuraSubtitles.cs` from playing `confirmation_001.wav` on every line. Hints stay as text.
- Wire the component in `Tools/Editor/Level01SceneBuilder.cs`, and add the same objects to the already built `Assets/Scenes/Level01_AwakeningGate.unity` through the Unity Editor. Do not rebuild the whole scene by hand-editing the scene file.

Kenney clips are already under `Assets/_Project/Audio/Kenney/`. Assign them on `AudioHookManager` from the scene builder, and mirror those assignments in the live scene:

- Cable snap: `impactMetal_000.ogg`
- Lever: `doorClose_000.ogg`
- Dial and slider step: `click_001.wav`
- Success: `confirmation_001.wav`
- Failure: `lowFrequency_explosion_001.ogg` at low volume
- Ship bed: looping `spaceEngineLow_001.ogg` on a 2D source, started with the opening and kept under the voice

XR interactors that already call `PlayOneShot` stay as they are. Fill the empty presentation slots. Do not add a second audio system.

## Allowed paths

- `Assets/Scripts/Gameplay/ChamberOnboardingController.cs`
- `Assets/Scripts/Gameplay/ChamberController.cs`
- `Assets/Scripts/Gameplay/GatewayController.cs`
- `Assets/Scripts/Presentation/**`
- `Assets/_Project/Audio/Voice/**`
- `Tools/Editor/Level01SceneBuilder.cs`
- `Tools/Editor/Level01DefenseSetup.cs`
- `Tests/PlayMode/Gameplay/**`
- `Assets/Scenes/Level01_AwakeningGate.unity` through the Unity Editor or the scene builder only

## Forbidden paths

- `Assets/Scripts/Core/**`
- `Assets/Scripts/XR/**` except leaving existing `PlayOneShot` calls alone
- `ProjectSettings/**`
- `Packages/**`
- Do not delete task files, handoff templates, or the redirect folders `Assets/AudioAssets/`, `Assets/Scripts/Audio/`, and `Assets/Scripts/Visualization/`

## Acceptance criteria

- [ ] Press Play and hear `neural recording 1` once, with no generated voice.
- [ ] The five lines appear on the world-space banner at the times in this file and clear at 16.72 seconds.
- [ ] The next hint does not start until the 18.24 second recording has finished.
- [ ] Engine bed and computer tick sit under the voice.
- [ ] One cable or lever interaction plays its Kenney clip.
- [ ] PlayMode gameplay tests pass after the onboarding event change.
- [ ] Unity console shows no compile errors.

## Blocked on

None.

## Handoff notes

Cleanup of the old handoff reports and `Documentation/AgentTasks/` is already complete. `.agents/tasks/IN_PROGRESS.md` still tracks the Chamber 01 Quest Link playtest in `BACKLOG.md`. That playtest is a separate task. Do not mark it done from this work.
