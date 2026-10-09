# Voice bake-off (master plan WP4)

Goal: the human and one classmate pick the Guide voice, blind. This folder holds the render instructions, the blind label key, and the template for the locked spec.

The agent did not render, listen to, or judge any audio. Nothing here has been generated yet.

Sources: `.agents/tasks/TASK_IGNITE_RIDE_V3_MASTER_PLAN.md` (WP4 and Appendix A). `ArtSource/AGENTS.md` applies: every generated file keeps its tool, settings and license next to it, and raw art stays out of `Assets/`.

Run every command below from `ArtSource\voice_bakeoff\`.

---

## 1. Machine check (2026-10-09)

| Tool | Status |
|---|---|
| ffmpeg | NOT on PATH. Install it yourself: `winget install Gyan.FFmpeg`, then open a new terminal and run `ffmpeg -version`. The agent installed nothing. |
| nvidia-smi | Present. NVIDIA GeForce RTX 3060, 12288 MiB VRAM, driver 595.79, CUDA 13.2. About 1.1 GB in use at check time (Unity Editor and other apps running). |
| python | 3.13.14, with `pip` on PATH. |

If a local GPU render runs out of VRAM, close Unity first.

---

## 2. Blind labels (keep this part hidden)

- The candidates are labelled A, B, C, D. The mapping is only in `key.txt`, which is operator-only.
- Before the listener hears anything, copy only `A.wav`, `B.wav`, `C.wav` and `D.wav` to a folder outside this repo. The listener must not see `README.md`, `key.txt`, `VOICE_SPEC.md` or `work\`, because they name the tools.
- Working folders use the label (`work\A\`), not the tool name, for the same reason.
- Record the pick in `key.txt` first, then copy it into `VOICE_SPEC.md`.

---

## 3. Test lines (Appendix A ids)

Three lines, all rendered in the Guide voice with the same description, so the candidates are compared on the same text. The pause markup from Appendix A is removed and replaced by explicit silence files (section 7).

| Role | Appendix A id | Speaker | Text |
|---|---|---|---|
| Calm explanation | 20 | GUIDE | Look ahead. Every glowing point is a neuron. Every line is a connection. And every connection has a number called a weight: how much one neuron listens to another. Change the weights, and you change what the AI thinks. |
| Excited reaction | 4 | AURA | Whoa, you zapped a drone! Trigger's too low. |
| Dramatic pause | 29 | GUIDE | But in nineteen sixty-nine, researchers found a problem one neuron can never solve. That's our next ride. |

Notes:
- Line 4 is AURA's line. The bake-off renders it in the Guide voice to compare delivery only. AURA's own voice and the glitch filter are a later decision and are not tested here.
- The plan does not set the Guide's gender. The description below leaves it open. Decide it before the final spec.

---

## 4. Segments (WP4 rule: one sentence per call)

Each candidate makes 9 calls, one per segment. The silence after each segment comes from Appendix A. Where Appendix A has no tag, 0.3 s is the WP4 clause default. The 1.0 s between test lines is a bake-off convention, not from the plan.

| Seg | Text sent to the model | Silence after |
|---|---|---|
| 01 | Look ahead. | 0.6 s |
| 02 | Every glowing point is a neuron. | 0.3 s |
| 03 | Every line is a connection. | 0.5 s |
| 04 | And every connection has a number called a weight: how much one neuron listens to another. | 0.8 s |
| 05 | Change the weights, and you change what the AI thinks. | 1.0 s (end of line 20) |
| 06 | Whoa, you zapped a drone! | 0.4 s |
| 07 | Trigger's too low. | 1.0 s (end of line 4) |
| 08 | But in nineteen sixty-nine, researchers found a problem one neuron can never solve. | 1.0 s |
| 09 | That's our next ride. | none |

Send the plain text only. No `[p0.x]` tags and no markdown.

---

## 5. Guide voice description (shared by every candidate)

Use this as the voice description or style prompt for every candidate. It comes from the WP4 cast: "older, warm, calm, a little gravel, unhurried, slight British lilt."

> An older, warm, calm adult narrator with a little gravel in the voice and a slight British lilt. Unhurried, measured pacing, clear diction, gentle authority, like a kind teacher. Never theatrical, never sing-song, never shouting.

Style prompt for hosted tools, placed before each segment:

> Say the following in a warm, calm, unhurried tone. An older narrator with a little gravel and a slight British lilt. Pause naturally between phrases. Keep it measured, never theatrical.

For seg 06 only, add: "Sound surprised and pleased, with a quick lift, but stay warm and controlled."

---

## 6. Render steps per candidate

The tool-to-label mapping is in `key.txt`. Use the label from that file for every folder and output name in the steps below. Do the Qwen3-TTS render first if you plan to use Chatterbox with a reference clip.

For every candidate:

1. Make the folder `work\<L>\`, where `<L>` is the label.
2. Render each of the 9 segments and save the raw output as `work\<L>\raw_NN.wav` (NN = 01 to 09).
3. Convert each raw file to the segment format:
   ```
   ffmpeg -i work\<L>\raw_NN.wav -ar 48000 -ac 1 -c:a pcm_s16le work\<L>\seg_NN.wav
   ```
4. Assemble (section 7), then loudness-normalize (section 8) into `<L>.wav` in this folder.
5. Write the tool, settings, seed and license for that label into `key.txt`.

### Candidate: Qwen3-TTS VoiceDesign (open weights)

- License: Apache-2.0 per the Simon Willison post on the January 2026 release. Confirm against the model card's LICENSE file.
- Hardware: local RTX 3060. The release post says "a few GBs of VRAM". The 1.7B model is about 4.5 GB to download.
- Install (you run this, the agent did not): `pip install -U qwen-tts`
- Load: `from qwen_tts import Qwen3TTSModel`, then `Qwen3TTSModel.from_pretrained(...)` with the VoiceDesign repo ID, `Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign`. Confirm the repo ID on the model card.
- Call: the page the agent checked only showed the CustomVoice call, not VoiceDesign. Copy the exact call from the VoiceDesign model card. Pass the Guide description from section 5 as the voice description.
- Use the sample rate returned with the audio. Do not hard-code 24 kHz.
- Fix one seed and write it into `key.txt`.
- Keep one clean 5 to 15 s clip of the designed voice as `work\reference_qwen.wav`. Chatterbox uses it as its reference.

### Candidate: Chatterbox-Turbo (MIT)

- License: MIT per the Chatterbox model page. Outputs carry Resemble AI's Perth watermark, which is imperceptible and robust. Do not try to strip it. Note it in the credits if this voice is chosen.
- Install (you run this, the agent did not): `pip install chatterbox-tts`
- The page the agent checked covered the standard English model, not Turbo. Confirm the Turbo class name and install on its model card. The standard model loads as `ChatterboxTTS.from_pretrained(device="cuda")`.
- Settings to try (names from the standard model page; confirm for Turbo):
  - `exaggeration` (emotion intensity). Default 0.5. Try 0.3, 0.5 and 0.7.
  - `cfg`. Default 0.5. Try 0.3 and 0.5.
  - Run 1: default voice, no reference.
  - Run 2: `audio_prompt_path` set to `work\reference_qwen.wav`, with the Qwen reference clip.
  - Fix one seed for each run.
- Paralinguistic tags such as `[sigh]`: the plan mentions them, but the page the agent checked lists none. The test lines need no tags, so do not add any.
- Pick one run for `C.wav` and record its settings in `key.txt`.

### Candidate: Gemini TTS (Google AI Studio, hosted free tier)

- You create or use your own Google AI Studio account. The agent does not enter any credentials.
- Open AI Studio's speech generation, choose a mature or informative prebuilt voice, and record the exact name in `key.txt`.
- Put the style prompt from section 5 before each segment's text, or in the style field if the UI has one.
- Preview rate limits apply. If you hit one, wait and retry the segment. Do not batch them into one call.
- Check the AI Studio terms for public-event use of outputs before using this in the final show.
- Download each output and convert it with the step 3 command. The download format and rate can vary; the ffmpeg command handles either.

### Candidate: ElevenLabs (free tier)

- Terms (WP4 and Appendix B): the free tier is non-commercial, and the credit "elevenlabs.io" is required. Ignite is a public event tied to the school. Confirm with the human that this counts before using this voice in the final show.
- Voice: pick a stock or library voice that fits the section 5 description. Do not use an Instant or Professional clone of any real or famous person, and do not pick a library voice that is an obvious celebrity likeness. WP4 says to stop and ask the human if anyone pushes for a celebrity clone.
- Use default settings. Record the voice name and model name shown in the app in `key.txt`.
- Download each segment (MP3 or WAV). Convert with the step 3 command; ffmpeg reads both.

---

## 7. Assemble (ffmpeg)

Make the silence files once, in `work\silence\`. They are 48 kHz mono, 16-bit PCM:

```
mkdir work\silence
ffmpeg -f lavfi -i anullsrc=r=48000:cl=mono -t 0.3 -c:a pcm_s16le work\silence\sil_0.3.wav
```

Repeat with `-t 0.4`, `-t 0.5`, `-t 0.6`, `-t 0.8` and `-t 1.0`, saving as `sil_0.4.wav`, `sil_0.5.wav`, `sil_0.6.wav`, `sil_0.8.wav` and `sil_1.0.wav`.

Write `work\<L>\list.txt` with exactly this content. Paths are relative to the list file:

```
file 'seg_01.wav'
file '../silence/sil_0.6.wav'
file 'seg_02.wav'
file '../silence/sil_0.3.wav'
file 'seg_03.wav'
file '../silence/sil_0.5.wav'
file 'seg_04.wav'
file '../silence/sil_0.8.wav'
file 'seg_05.wav'
file '../silence/sil_1.0.wav'
file 'seg_06.wav'
file '../silence/sil_0.4.wav'
file 'seg_07.wav'
file '../silence/sil_1.0.wav'
file 'seg_08.wav'
file '../silence/sil_1.0.wav'
file 'seg_09.wav'
```

Concatenate:

```
ffmpeg -f concat -safe 0 -i work\<L>\list.txt -c copy work\<L>\assembled.wav
```

If `-c copy` fails, a segment is not in the 48 kHz mono PCM format from step 3. Fix that segment and rerun.

---

## 8. Loudness: -16 LUFS, peak -1 dBFS, mono 48 kHz

Use two passes. One pass is less accurate on clips this short.

Pass 1, measure only:

```
ffmpeg -hide_banner -i work\<L>\assembled.wav -af loudnorm=I=-16:TP=-1:LRA=11:print_format=json -f null -
```

The JSON block at the end of the output gives `input_i`, `input_tp`, `input_lra`, `input_thresh` and `target_offset`.

Pass 2, apply and write the final mono 48 kHz file. Replace the bracketed values with the numbers from pass 1:

```
ffmpeg -hide_banner -i work\<L>\assembled.wav -af loudnorm=I=-16:TP=-1:LRA=11:measured_I=<input_i>:measured_TP=<input_tp>:measured_LRA=<input_lra>:measured_thresh=<input_thresh>:offset=<target_offset>:linear=true -ar 48000 -ac 1 -c:a pcm_s16le <L>.wav
```

Single-pass fallback, less accurate on short clips:

```
ffmpeg -i work\<L>\assembled.wav -af loudnorm=I=-16:TP=-1:LRA=11 -ar 48000 -ac 1 -c:a pcm_s16le <L>.wav
```

Why TP=-1: loudnorm's ceiling is true peak in dBTP. True peak is never lower than sample peak, so a true-peak ceiling of -1 dBTP keeps the sample peak at or below -1 dBFS.

Check the output: run pass 1 on the final `<L>.wav`. Expect `input_i` near -16 and `input_tp` at or below -1.

---

## 9. Do not

- Do not clone a real or famous person's voice (WP4). The Guide is an original voice.
- Do not put tags or markdown in the segment text.
- Do not give the listener the key, the README, the work folder or the spec.
- Do not treat free-tier output as cleared for commercial or public use until its terms are checked.

---

## 10. After the pick

1. The human picks the winning label, reads the tool in `key.txt`, and fills in `VOICE_SPEC.md`.
2. Lock the Guide description, seed, reference clip and settings in `VOICE_SPEC.md`.
3. For production (WP4 rules): one sentence per call; explicit silences of 0.3 s between clauses and 0.7 to 1.0 s on story beats, set by `pauseAfter`; the section 8 loudness chain on every file; mono 48 kHz; files named `vo_ride_NN.wav`; a `.txt` sidecar per clip with source, model, voice spec and exact text.
4. Credits and ADR-012 list every voice source and license. Re-check licenses at use time (Appendix B).
