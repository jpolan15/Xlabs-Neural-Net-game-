# Archival audio sources (public domain) - Neural Ride archival voice clips

Two short archival clips cut from US-government recordings, plus one optional variant. Everything was fetched on
2026-10-09. Raw sources are NOT stored in the repo (the JFK file is 42.7 MB); the URLs and SHA-256 hashes below let anyone
re-fetch and verify them. `process.py` (this folder) rebuilds the three WAVs bit-for-bit from the sources.

| File | Content | Duration | Integrated loudness | Sample peak | True peak |
|---|---|---|---|---|---|
| `vo_ride_16.wav` | JFK, Rice University, 12 Sep 1962 - the Moon sentence only | 7.69 s | -16.0 LUFS | -3.74 dBFS | -3.74 dBFS |
| `vo_ride_24.wav` | Apollo 11 launch, 16 Jul 1969 - countdown end + liftoff call | 8.81 s | -16.0 LUFS | -2.34 dBFS | -2.33 dBFS |
| `vo_ride_16_alt_leadin.wav` (optional, not requested) | JFK with the two spoken lead-ins, applause shortened | 13.33 s | -16.0 LUFS | -3.16 dBFS | -3.16 dBFS |

All three: mono, 48 kHz, 16-bit PCM WAV. Loudness measured on the written files with pyloudnorm (ITU-R BS.1770-4) and
cross-checked with ffmpeg `ebur128` (both -16.0 LUFS; true peaks -3.7 / -2.3 / -3.2 dBFS). No limiter was needed.

Suggested credits (on-screen or in the credits roll):
- "President John F. Kennedy, address at Rice University, 12 September 1962. John F. Kennedy Presidential Library and Museum, White House Audio Collection (public domain)."
- "Apollo 11 launch commentary, 16 July 1969. NASA."

---

## vo_ride_16.wav - JFK, "We choose to go to the Moon"

| Field | Value |
|---|---|
| Title | "Address at Rice University in Houston, Texas on the Nation's Space Effort, 12 September 1962" (digital identifier JFKWHA-127-002; White House Audio Collection, JFK Presidential Library and Museum) |
| Speaker | President John F. Kennedy |
| Date / place | 12 September 1962, Rice Stadium, Houston, Texas |
| Page URL | https://www.jfklibrary.org/asset-viewer/archives/jfkwha-127-002 |
| File URL | https://static.jfklibrary.org/2311tr5g0r14552308p8qs05546cv6c1.mp3?filename=JFKWHA-127-002-AU_WR.mp3&odc=20260305215117-0500 (the page's "Download" link on 2026-10-09) |
| Source file | `JFKWHA-127-002-AU_WR.mp3`, 42,741,646 bytes, MP3 ~320 kbps, mono, 44.1 kHz, 1068.49 s. SHA-256 `74f8433ae3953b650ed1c1f1de073ebcbe43ef0f750d2b98744128a7a80fdd8f` |
| License / terms as shown on the page | "Copyright Status: Public Domain". The page's copyright notice says documents in the collection prepared by US officials as part of their official duties are in the public domain, and also warns that some other items in the collection may carry restrictions; this item's own status line is Public Domain. Archival creator on the page: Department of Defense / Defense Communications Agency / White House Communications Agency. |
| Trim (source timeline) | start **535.43 s**, end **543.12 s** (8:55.43 to 9:03.12). Voice onset of "We" is at about 535.50 s; the last voiced sound of "hard" ends at about 543.00 s, where the 120 ms fade-out begins. |
| Words included | "We choose to go to the Moon in this decade and do the other things, not because they are easy, but because they are hard." Nothing before, nothing after (the next word in the speech, "because that goal...", starts at 543.80 s and is not in the clip). No lead-in. |
| Final | 7.69 s, -16.0 LUFS integrated, sample peak -3.74 dBFS, true peak -3.74 dBFS (gain +7.17 dB, limiter not engaged) |
| Verified transcript of the final file | faster-whisper small.en: "We choose to go to the moon in this decade and do the other things. Not because they are easy, but because they are hard." - exact match. (base.en on the same file writes "...to the moon and dislocate and do the other things, not because they are easy, but because they are hard." - a base-model mishearing of "in this decade" on narrow-band audio; small.en, and base.en on the Commons copy, all give "in this decade".) |
| SHA-256 of the WAV | `e37d54e944ad960d4ddf132a324f2b9460f1faa421fa0529e5f01f171832e146` |

Notes
- In the real speech JFK says "We choose to go to the Moon." twice (source 524.9 to 526.4 s and 527.6 to 529.1 s), then the
  crowd applauds for about 6.4 s (529.1 to 535.4 s), and only then delivers the sentence that begins "We choose to go to the
  Moon in this decade...". The lead-ins are therefore NOT adjacent to the sentence, so the primary clip leaves them out to meet
  "exact words, nothing before or after".
- `vo_ride_16_alt_leadin.wav` is the optional alternative: the first lead-in, the crowd murmur, the second lead-in, 1.2 s of the
  applause, a 250 ms equal-power crossfade that skips the rest of the applause (source 530.30 to 535.00 s is not in the clip), and the
  sentence. Source cuts: 524.84 to 530.30 s and 535.00 to 543.12 s. It is an edit (about 4.7 s of applause removed), not a
  continuous excerpt. Verified transcript (small.en): "We choose to go to the moon, we choose to go to the moon, we choose to go
  to the moon in this decade and do the other things, not because they are easy, but because they are hard." SHA-256
  `cc42bb7fb709b83d826e88367de6ed10a49b5f2eea2e81851a23039186547e3c`.
- The recording is narrow-band (speech energy stops at about 4.5 kHz) and has stadium atmosphere; a faint residue of the applause
  sits under the first second of the sentence. Both are inherent to the source and were left alone.
- Cross-checks (same speech, not used for the final): Wikimedia Commons `File:Jfk_rice_university_we_choose_to_go_to_the_moon.ogg`
  (https://commons.wikimedia.org/wiki/File:Jfk_rice_university_we_choose_to_go_to_the_moon.ogg, taken from the JFK Library
  recording, tagged US-government public domain; cross-correlates with the Library MP3 at a +7.03 s offset) and NASA's own
  "JFK: We Choose the Moon" clip (https://www.nasa.gov/wp-content/uploads/2015/01/586447main_JFKwechoosemoonspeech.mp3, listed on
  https://www.nasa.gov/historical-sounds/). Both carry the identical sentence.

---

## vo_ride_24.wav - Apollo 11 launch countdown and liftoff call

| Field | Value |
|---|---|
| Title | "Apollo 11: We Have a Lift-Off" (NASA sound/ringtone clip on NASA's "Historical Sounds" page, section "Apollo and Mercury") |
| Speaker | NASA Public Affairs launch commentator Jack King, Kennedy Space Center launch control. The sound page does not name the speaker; NASA's biography page (https://www.nasa.gov/people/the-chroniclers-jack-king/) describes King as the commentator who counted down the first lunar-landing mission. |
| Date / place | 16 July 1969, Apollo 11 liftoff at 9:32 a.m. EDT (13:32 UTC) from Launch Complex 39A, Kennedy Space Center ("32 minutes past the hour" is spoken in the source) |
| Page URL | https://www.nasa.gov/historical-sounds/ ("Historical Sounds - NASA") |
| File URL | https://www.nasa.gov/wp-content/uploads/2015/01/590320main_ringtone_apollo11_countdown.mp3 |
| Source file | 400,398 bytes, MP3 ~128 kbps, dual-mono stereo, 44.1 kHz, 24.8 s (ID3 date 2011-09-17, made with Adobe Soundbooth). SHA-256 `9f14d326e95356e096b181c807136dd460f5a30afae02d55135e84d5e3c0a6f0` |
| License / terms as shown | The page says: "For sound file use policy, please see Media Usage Guidelines" (https://www.nasa.gov/multimedia/guidelines/index.html, which resolves to https://www.nasa.gov/nasa-brand-center/images-and-media/). That page says NASA content, audio included, "generally are not subject to copyright in the United States", allows educational or informational use, and asks that "NASA should be acknowledged as the source". It also says the NASA insignia/logotype are not public domain and use must not imply endorsement - use the audio only; do not add NASA logos or imply NASA endorsement. Credit: NASA. |
| Trim (source timeline) | start **5.03 s**, end **13.84 s**. Start is inside the ~150 ms quiet gap after "four" (ends about 4.95 s) and before the "th" of "three" (about 5.12 s). "liftoff" ends at about 13.72 s, where the 120 ms fade-out begins; the next word ("thirty-two") only starts at about 13.87 s. |
| Words included | As spoken: "Three, two, one, zero. All engine running. Liftoff. We have a liftoff." Note the singular "engine": the recording says "all engine running", not "engines" (both ASR models hear it that way, and it matches how the call is usually transcribed). Nothing after "We have a liftoff" ("32 minutes past the hour" is not in the clip). |
| Final | 8.81 s (within the 11 s budget; 6-10 s target), -16.0 LUFS integrated, sample peak -2.34 dBFS, true peak -2.33 dBFS (gain +1.18 dB, limiter not engaged) |
| Verified transcript of the final file | faster-whisper small.en: "Three, two, one, zero. All engine running. Lift off. We have a lift off."; base.en: "3, 2, 1, 0, all engine running, liftoff, we have a liftoff." - both match the target words. |
| SHA-256 of the WAV | `0a2e2e3f7e387274d965e99edb829de37d0f03a9f0163278f28f3de927d7776a` |

Notes
- The source is narrow-band (nothing above about 5.6 kHz) and contains a faint steady tone near 2.08 kHz between words, plus
  background noise that rises after ignition. All of it is in the NASA file and was left untouched ("do not otherwise alter the voice").
- If a shorter or longer Apollo cut is ever needed, the same NASA file continues with "32 minutes past the hour. Liftoff on Apollo 11."
  (source 13.87 to 17.4 s); change the `segments` entry in `process.py`.

---

## Processing chain (identical for every clip; see `process.py`)

1. Decode the whole MP3 sequentially (libsndfile 1.2.2 via soundfile 0.14.0; ffmpeg 7.1 decodes these files sample-aligned, lag 0), average channels to mono.
2. Cut the region plus 0.75 s guard each side, resample to 48 kHz (scipy polyphase 160/147 from 44.1 kHz).
3. Light high-pass: 2nd-order Butterworth, 80 Hz, single pass.
4. Trim to the exact cut points above (sample-exact on the 48 kHz grid).
5. Fades: 15 ms raised-cosine fade-in, 120 ms raised-cosine fade-out (the first and last samples are 0).
6. Gain to -16.0 LUFS integrated (pyloudnorm 0.2.0, BS.1770-4 gating). A look-ahead peak limiter would engage only above -1 dBFS sample peak; it never did.
7. Write 16-bit PCM WAV, mono, 48 kHz, plain rounding (no dither). No EQ, de-noise, compression, or time/pitch change.

Reproduce:
`python process.py --jfk JFKWHA-127-002-AU_WR.mp3 --apollo 590320main_ringtone_apollo11_countdown.mp3 --out <dir> [--verify]`
(needs numpy, scipy, soundfile, pyloudnorm; `--verify` also needs faster-whisper). The same SHA-256 hashes came out of three
separate virtual environments. Environment used: Python 3.13.14 venv at `C:\Users\Panda\.cache\archival\venv` (keep Windows venv
paths short), numpy 2.5.3, scipy 1.18.1, soundfile 0.14.0, pyloudnorm 0.2.0, faster-whisper 1.2.1 / ctranslate2 4.8.2
(models Systran/faster-whisper-small.en and base.en, CPU, int8, beam 5, word timestamps, fed as 16 kHz numpy arrays).

## Provenance checklist

- JFK: official JFK Library file, page states Public Domain. Apollo: NASA-hosted file, NASA media policy applies. No third-party
  or copyrighted recording (for example the Westinghouse Group W radio copy of the speech) was used.
- Sources were downloaded into empty, separate scratch folders and only ever decoded (never executed).
- No repo file outside `ArtSource/archival/` was changed. (Outside the repo: scratch downloads live in the session scratchpad, the
  Python venv is at `C:\Users\Panda\.cache\archival\venv`, and the Whisper small.en/base.en weights, about 0.6 GB, are in the
  standard Hugging Face cache `C:\Users\Panda\.cache\huggingface`; all can be deleted.)
