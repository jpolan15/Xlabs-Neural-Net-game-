# Voice spec (Guide and AURA, locked 2026-10-09)

Status: LOCKED by measurement, pending one human listen. On 2026-10-09 the project owner asked the agent to make the
voice choice ("use your best judgement"). The agent cannot hear, so it chose by measurement (transcription accuracy,
predicted naturalness, pitch range) instead of the blind bake-off planned below; the evidence is in
`ArtSource/voice/README.md` ("Why these voices"). The four hosted/local bake-off candidates in `key.txt` were not
rendered. A human and one classmate should still listen to the installed set before Ignite and either approve it or
swap a voice (one command, see the README).

Governing plan: `.agents/tasks/TASK_IGNITE_RIDE_V3_MASTER_PLAN.md`, WP4.

| Field | Guide | AURA |
|---|---|---|
| Date picked | 2026-10-09 | 2026-10-09 |
| Picked by | Agent (Claude), by measurement, at the owner's request; human listen pending | same |
| Tool | Kokoro-82M v1.0 via kokoro-onnx 0.6.1, local, CPU | same |
| Model source | https://huggingface.co/hexgrad/Kokoro-82M (weights: kokoro-onnx release `model-files-v1.0`) | same |
| Model license | Apache-2.0 (model); MIT (kokoro-onnx) | same |
| Credit line | "Guide and AURA: Kokoro-82M text-to-speech, Apache 2.0" (on the credits panel) | same |
| Voice | `bm_fable` (British male), lang en-gb | `af_heart` (American female), lang en-us |
| Speed | 0.90 | 1.0 (line 12 "Too big a step!" at 0.90) |
| Post-processing | none beyond the loudness chain | glitch filter on her early lines, amount 1.0, 0.8, 0.6, 0.4, 0.25 (lines 18, 4, 6, 12, 13), then clean (14, 15) |
| Seed | Kokoro is deterministic; glitch stutter seeded per line (`render_voices.py`, line 4 offset 2000) | |
| Reference clip | none (stock voice, no cloning) | none |
| Pause rules | one sentence per call; the script's [pX] marks between clauses; 0.34 s between sentences | 0.26 s between sentences |
| Loudness chain | −16 LUFS integrated, true peak ≤ −1 dBTP (4× oversampled), mono 48 kHz 16-bit (`render_voices.py normalize`) | same |
| Output | `Assets/_Project/Audio/Voice/vo_ride_NN.wav` + `.txt` sidecar (Source, Segments, exact text) | same |
| Known limits | synthetic voice; British "lever" pronunciation; a lone "Lower." needed speed 1.0 to be heard clearly | a lone "Too big a step" needed speed 0.9 |

## Archival voices

| Line | Speaker | Source | License |
|---|---|---|---|
| 16 | President John F. Kennedy, Rice University, 1962 | JFK Library JFKWHA-127-002 | Public domain (as stated by the Library) |
| 24 | Jack King, NASA launch commentary, Apollo 11, 1969 | NASA historical sounds | NASA media, not copyrighted in the US; credit NASA |

Details, trims and hashes: `ArtSource/archival/SOURCES.md`.

## Approval

| Role | Name | Date |
|---|---|---|
| Human (listens, approves or swaps) | | |
| Teacher (script wording) | | |
