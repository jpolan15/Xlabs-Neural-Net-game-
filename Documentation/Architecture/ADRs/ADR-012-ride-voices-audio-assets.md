# ADR-012: Ride Voices, Archival Audio, Music Levels, and the Asset Forge Pipeline

| Field | Value |
|---|---|
| ID | ADR-012 |
| Date | 2026-10-09 |
| Status | Proposed |
| Deciders | Project owner (voice and audio choices); Design (to confirm) |

## Context

The Ride v3 "Ignite" plan (`.agents/tasks/TASK_IGNITE_RIDE_V3_MASTER_PLAN.md`) prepares the ride for a public live demo at Ignite, an event tied to the school. Before any voice, music, or generated asset ships, four things need a recorded decision:

- The narration was a Windows OneCore voice (ADR-011): 15 short lines, no intro, no briefings. Ride v3 replaces it with a 28-line script (intro, three briefings, outro) that runs on subtitles until new voices exist. The teacher asked for a good AI narrator plus a famous, recognizable voice that is free and does not sound like "AI slop".
- `telstar.wav` is mastered hot (RMS -11.4 dBFS, peak 0 dBFS). At the current music volume (0.5) it sits only about 2-3 dB under the voice. The next classroom heard it as too loud.
- Generated 3D assets must pass verification gates before they ship.
- Raw source art needs a home outside `Assets/`, so Unity does not import large files.

## Decision

Adopt the following for the Ignite ride. This ADR stays Proposed until the project owner accepts it.

1. **Voice cast:** an original Guide (main narrator) and AURA (the ship AI), plus two archival recordings: JFK (opens the intro) and NASA Apollo 11 launch audio (plays when GO is pulled).
2. **No celebrity or real-person voice clones.** Famous voices come only from archival recordings that are public domain or licensed for education.
3. **A voice file only plays when its `.txt` sidecar matches the approved script text** (`NeuralRideBuilder.Narration.cs`), so old recordings can never be heard under new words. The script lives in `Tools/Editor/RideNarrationData.cs`.
4. **Music level policy:** `RideMusic` volume 0.14, ducked volume 0.05, both set by the builder. The builder fails if music volume is above 0.2.
5. **Asset Forge** is the route for new generated 3D assets. Raw art lives in `ArtSource/`. Only a mesh that passes Gate B moves into `Assets/`. Generated meshes are never interactable and never orange.

### Voice cast

| Voice | Role | Notes |
|---|---|---|
| The Guide | Mentor and main narrator | Original voice: older, warm, calm, a little gravel, unhurried, slight British lilt. |
| AURA | Ship AI being taught | Short lines. Early lines get a light digital-glitch filter; the final line is clean, so the player hears her learn. |
| JFK | Archival, opens the intro | Rice University, 1962: "We choose to go to the Moon..." |
| NASA | Archival, Apollo 11 launch audio on GO | Specific clip not yet chosen (to confirm). |

**Voice engine: Kokoro-82M (Apache-2.0), chosen 2026-10-09 by measurement, pending one human listen.** The owner asked the agent to choose. Because an agent cannot hear, 15 Kokoro voices were rendered on the same lines and scored on transcription accuracy (Whisper), predicted naturalness (UTMOS) and pitch range; the Guide is `bm_fable` (British, the most expressive of the fifteen), AURA is `af_heart` (the most natural). Evidence, settings and the one-command swap: `ArtSource/voice/README.md`; locked spec: `ArtSource/voice_bakeoff/VOICE_SPEC.md`. All 26 synthesized lines pass the transcription check (worst word error 2.3 %) and the −16 LUFS / −1 dBTP chain, and are installed with sidecars; the builder binds 28 of 28 lines. The bake-off below was not run; it remains the way to challenge the pick. Candidates the plan named:

- **Qwen3-TTS VoiceDesign:** open, designs a voice from a text description, runs on the local RTX 3060 12 GB. License: to confirm (read its LICENSE file).
- **Chatterbox-Turbo:** MIT. Emotion-intensity control and tags such as `[sigh]`. Output is watermarked.
- **Gemini TTS in Google AI Studio:** hosted, style prompts, preview rate limits. Terms: to confirm.
- **ElevenLabs free tier:** strong quality, but the free tier is non-commercial and requires the credit "elevenlabs.io".

### No celebrity voice clones

No clone of a real person's voice, and no clone of a recognizable character's voice. Obi-Wan Kenobi and Yoda were considered and rejected.

Why:

- A voice is part of a real person's likeness.
- The Obi-Wan and Yoda characters are Disney/Lucasfilm property.
- Cloning is not free just because the model is open source.
- Mainstream TTS terms forbid cloning a voice without consent.
- Ignite is a public event tied to the school.

The Guide gives the mentor feeling the teacher asked for. JFK and NASA give a recognizable famous voice, legally and on theme. If a teammate proposes a celebrity clone, stop and ask the project owner.

### Archival sources and licenses

| Source | Used for | Terms (as checked 2026-10-09) | Link |
|---|---|---|---|
| JFK, Rice University speech, 1962 | Intro opener (`vo_ride_16.wav`, 7.7 s: "We choose to go to the Moon in this decade… because they are hard.") | Public domain per the JFK Library, ID JFKWHA-127-002 ("Copyright Status: Public Domain") | https://www.jfklibrary.org/asset-viewer/archives/jfkwha-127-002 |
| NASA Apollo 11 launch commentary (Jack King), 1969 | GO pull (`vo_ride_24.wav`, 8.8 s: "Three, two, one, zero, all engine running. Liftoff! We have a liftoff.") | NASA media are generally not subject to copyright in the US; credit NASA, no implied endorsement | https://www.nasa.gov/historical-sounds/ and https://www.nasa.gov/nasa-brand-center/images-and-media |

Trim points, processing and file hashes: `ArtSource/archival/SOURCES.md`. Both clips were checked by transcription after trimming.

- Credit NASA. The credits screen must list every voice source and license (WP4). Done: a credits panel appears under the finale (`RideNarrationData.Credits`).
- Whether a public school demo counts as "education" under NASA's terms: to confirm.
- Re-check at use time. These are summaries, not legal advice.

### Music level policy

- `RideMusic.volume` 0.5 to **0.14**; `duckedVolume` 0.2 to **0.05**. Set from `Tools/Editor/NeuralRideBuilder.cs` so a rebuild keeps them.
- Arithmetic (from the plan): `telstar.wav` is RMS -11.4 dBFS. At 0.14 that is about -28.5 dBFS, about 13 dB under the voice (RMS about -15 dBFS). Ducked at 0.05 it is about -37 dBFS, about 22 dB under.
- Guard: a `RideAudioLevels` check in the builder fails the build if music volume is above 0.2.
- Mixer (planned, not yet created): `Assets/_Project/Audio/Mixers/RideMixer.mixer`, with Music, Voice, and SFX groups and exposed volume parameters.
- Acceptance: the owner confirms in the headset, at 50% Quest volume, that the voice is clearly on top.

**telstar.wav license flag.** The file is at `Assets/_Project/Audio/Music/telstar.wav`. ADR-011 records that the user supplied it and stated it is licensed for this project. The repository does not verify that. If the file is the 1962 Tornados recording, it is commercial music. For a public Ignite demo, either confirm the license in writing or swap in a CC0 space track. Whether this file is that recording: to confirm.

### Asset Forge tooling and services

- Procedure: `.agents/skills/asset-forge/SKILL.md`. Subagents: `.claude/agents/` (concept-artist, asset-verifier, mesh-smith, unity-integrator, vr-qa).
- Pipeline: brief, then concept-artist (GPT Image hero and 4-view turnaround), then **Gate A** (asset-verifier: views agree), then mesh-smith (image-to-3D, Blender cleanup), then unity-integrator (import, builder wiring, turntable renders), then **Gate B** (silhouette IoU at least 0.80 per view, plus a rubric), then vr-qa (seat captures, budgets), then **Gate C** (in-game, plus human OK), then ship and log in `ArtSource/`.
- Pilot: the friendly repair drone (stops 1-2). Other assets wait until the pilot passes Gates A to C and the human approves.
- Budgets (Quest 2): prop at most 5k triangles; pod at most 15k; at most 2 materials; textures at most 1024² ASTC. Whole scene at most 100 draw calls and 100k triangles.
- AI textures are not used raw. They are graded to the palette, or the HoloLit materials are used.

| Tool or service | Use | License or terms (as recorded in the plan) |
|---|---|---|
| GPT Image (OpenAI API) | Concept art, when `OPENAI_API_KEY` is set | Paid, cents per image. Terms: to confirm. |
| ChatGPT or Google AI Studio (manual) | Concept art when there is no API key; the human generates from `prompt.txt` | Terms: to confirm. |
| Hunyuan3D-2mv | Local image-to-3D shape, about 6 GB VRAM | Open weights. License: to confirm. |
| Hunyuan web studio, or Tripo free tier with its MCP | Hosted option where local texturing (about 16 GB) and TRELLIS.2 (24 GB or more) do not fit | Tripo MCP is alpha. Terms: to confirm. |
| Blender (`winget install BlenderFoundation.Blender`) | Headless cleanup: decimate, UV, scale, pivot | License: to confirm. |
| FFmpeg (`winget install Gyan.FFmpeg`) | Loudness normalization of voice files (`loudnorm`) | License: to confirm. |
| Unity CLI and MCP for Unity | Import and capture | See ADR-009. |

Gate rule conflict in the plan: the diagram allows up to 3 Gate B loops back to mesh-smith, but the WP7 text says to stop after two pilot failures. Which rule applies: to confirm.

### ArtSource directory

`ArtSource/` holds raw and work-in-progress art for the Asset Forge pipeline. It sits outside `Assets/` so Unity does not import large raw meshes and concept images on every import or build.

- Exists now: `ArtSource/AGENTS.md`, `ArtSource/_TEMPLATE/brief.md`.
- Exists now: `ArtSource/voice_bakeoff/`. Planned, not yet created: `ArtSource/<asset_id>/` per asset (`brief.md`, `concept/`, `views/`, `raw/`, `clean/`, `verify/`).
- Rules: only a Gate B mesh moves to `Assets/_Project/Models/<AssetId>/` (planned, not yet created). Every generated file keeps its prompt, settings, tool, and license beside it. Nothing in `ArtSource/` ships in the game binary.
- Scripts go in `Tools/ArtPipeline/` (planned, not yet created).

## Consequences

- Music volume becomes one builder-owned value, guarded by a build check.
- The credits screen lists every voice, music and sound source (done).
- Voice files are generated, checked and installed by `ArtSource/voice/render_voices.py`; archival clips by `ArtSource/archival/process.py`. The sound effects and their measured levels are recorded in ADR-014.
- `Documentation/Architecture/DEPENDENCY_RULES.md` is not updated, because this ADR adds no assembly references.
- Not done yet: a human listen of the chosen voices, the telstar.wav license confirmation, the mixer, and the Asset Forge pilot.

## Open items (to confirm)

1. Voice engine: chosen by measurement (Kokoro-82M, see above); the human listens once and approves or swaps.
2. Qwen3-TTS license file; Gemini TTS terms (only if a bake-off against them is still wanted).
3. ElevenLabs free tier is non-commercial. Whether Ignite counts as commercial, and so whether the free tier can be used: to confirm.
4. `telstar.wav`: is it the 1962 Tornados recording? Then either a written license or a CC0 swap.
5. NASA: the clip is chosen (Jack King's liftoff call). Whether a public school demo counts as education: NASA's guidelines say its media are generally not copyrighted, with credit; re-check at use time.
6. Terms for GPT Image, Hunyuan3D-2mv, the Hunyuan web studio, Tripo MCP (alpha), Blender, and FFmpeg.
7. Gate retry rule: up to 3 Gate B loops (diagram) or stop after two pilot failures (text).
8. Deciders: confirm the list above.
9. Re-check every license at use time.
