"""
Render the Neural Ride narration to Unity-ready voice files.

    python render_voices.py render  --engine kokoro [--lines 17,20] [--out out/kokoro]
    python render_voices.py render  --engine-module backend_qwen.py --out out/qwen
    python render_voices.py verify  out/kokoro          (needs faster-whisper)
    python render_voices.py install out/kokoro          (copies into Assets/_Project/Audio/Voice/)

The script text is read from Tools/Editor/RideNarrationData.cs, the single source of truth, so a clip can never
say different words from the subtitles. Production rules (master plan WP4):
  - one sentence per TTS call; the model is never trusted to pause
  - explicit silences: the [pX] marks between clauses, SENTENCE_GAP between sentences of one clause
  - AURA's early lines get a digital-glitch filter that fades line by line; her outro lines are clean
  - -16 LUFS integrated, true peak <= -1 dBTP, mono, 48 kHz, 16-bit PCM, named vo_ride_NN.wav
  - a .txt sidecar per clip: Source (tool, model, license, settings), Segments (measured seconds of each
    subtitle clause, read by NeuralRideBuilder.Narration.cs so captions follow the voice), then the exact text

A backend is any module with `ENGINE = Engine()` where Engine has `name`, `describe(role) -> str` and
`synth(text, role) -> (float32 mono ndarray, sample_rate)`; role is "guide" or "aura".
"""
from __future__ import annotations

import argparse
import datetime as _dt
import importlib.util
import json
import random
import re
import shutil
import sys
from pathlib import Path

import numpy as np
import pyloudnorm as pyln
import soundfile as sf
from scipy.signal import butter, resample_poly, sosfilt

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
DATA_CS = REPO / "Tools" / "Editor" / "RideNarrationData.cs"
TYPES_CS = REPO / "Assets" / "Scripts" / "Gameplay" / "Ride" / "RideTypes.cs"
VOICE_DIR = REPO / "Assets" / "_Project" / "Audio" / "Voice"

SR = 48000
TARGET_LUFS = -16.0
CEILING_DBTP = -1.0
SENTENCE_GAP = {"guide": 0.34, "aura": 0.26}

# How broken AURA sounds, by line, in ride order: she is offline at the start and heals as the player teaches her.
AURA_GLITCH = {"AuraOffline": 1.0, "DroneOops": 0.8, "TwoSensors": 0.6, "LrTooHigh": 0.4, "Converged": 0.25}

# Spoken spellings only. The sidecar keeps the exact script text.
SPOKEN = [
    (r"\bAURA\b", "Aura"),
    (r"\bGO\b", "go"),
    (r"\bLEARN\b", "learn"),
    (r"\bOR gate\b", "or gate"),
    (r"^Whoa, ", "Whoa! "),  # its own beat; at one breath the transcription heard "use aptadrone"
    (r"\bgo lever\b", "go-lever"),  # run together it was heard as "GoLaver"
    (r"\borange lever\b", "orange-lever"),  # "Pull the orange lever" was heard as "orange laver" (WER 7.7 %)
]

# Where AURA's stutter lands is seeded; on these lines the default seed put it inside a word the transcription then
# lost ("zapped it to drone"), so another seed is used. Checked with `verify`.
GLITCH_SEED_OFFSET = {"DroneOops": 2000}

SPEAKERS = {"G": "guide", "A": "aura", "C": "aura_clean", "X": "archival"}


# ----------------------------------------------------------------------------------------------- script parsing

def load_script() -> list[dict]:
    cs = DATA_CS.read_text(encoding="utf-8")
    types = TYPES_CS.read_text(encoding="utf-8")
    enum_body = re.search(r"enum RideLine\s*\{(.*?)\}", types, re.S).group(1)
    ids = {name: int(num) for name, num in re.findall(r"(\w+)\s*=\s*(\d+)", enum_body)}

    line_re = re.compile(
        r'new Line\(RideLine\.(\w+),\s*([GACX]),\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"'
        r'\s*(?:,\s*([\d.]+)f)?\s*(?:,\s*([\d.]+)f)?\s*\)', re.S)
    lines = []
    for m in line_re.finditer(cs):
        name, spk, tag, text = m.group(1), m.group(2), m.group(3), m.group(4)
        lines.append({"name": name, "id": ids[name], "speaker": SPEAKERS[spk], "tag": tag, "text": text})
    if len(lines) < 20:
        sys.exit(f"Parsed only {len(lines)} lines from {DATA_CS}; the parser is out of date.")
    return lines


PAUSE_RE = re.compile(r"\[p(\d+(?:\.\d+)?)\]")
CUE_RE = re.compile(r"\{[a-z0-9_, ]+\}")  # visual cue tags; never spoken
SENTENCE_RE = re.compile(r"(?<=[^.][.!?])\s+(?=[A-Z])")


def clauses(text: str) -> list[tuple[str, float]]:
    """(clause, pause after) pairs, split at the [pX] marks exactly as RideNarrationData.Segments does."""
    out, at = [], 0
    for m in PAUSE_RE.finditer(text):
        c = CUE_RE.sub("", text[at:m.start()]).strip()
        if c:
            out.append((c, float(m.group(1))))
        at = m.end()
    tail = CUE_RE.sub("", text[at:]).strip()
    if tail:
        out.append((tail, 0.0))
    return out


def plain_text(text: str) -> str:
    return re.sub(r"\s+", " ", CUE_RE.sub(" ", PAUSE_RE.sub(" ", text))).strip()


def spoken(text: str) -> str:
    for pattern, repl in SPOKEN:
        text = re.sub(pattern, repl, text)
    return text


# ----------------------------------------------------------------------------------------------- dsp

def to_sr(x: np.ndarray, sr: int) -> np.ndarray:
    if sr == SR:
        return x.astype(np.float32)
    from math import gcd
    g = gcd(sr, SR)
    return resample_poly(x, SR // g, sr // g).astype(np.float32)


def frame_rms(x: np.ndarray, hop: int) -> np.ndarray:
    n = len(x) // hop
    if n == 0:
        return np.zeros(1, np.float32)
    return np.sqrt(np.mean(x[: n * hop].reshape(n, hop) ** 2, axis=1) + 1e-12)


def trim(x: np.ndarray, pre: float = 0.02, post: float = 0.07) -> np.ndarray:
    hop = int(0.01 * SR)
    rms = frame_rms(x, hop)
    thr = max(rms.max() * 10 ** (-38 / 20), 2e-4)
    on = np.where(rms > thr)[0]
    if len(on) == 0:
        return x
    a = max(0, on[0] * hop - int(pre * SR))
    b = min(len(x), (on[-1] + 1) * hop + int(post * SR))
    return fade(x[a:b], 0.004, 0.02)


def fade(x: np.ndarray, fin: float, fout: float) -> np.ndarray:
    x = x.copy()
    i, o = int(fin * SR), int(fout * SR)
    if i:
        x[:i] *= np.linspace(0, 1, i, dtype=np.float32)
    if o:
        x[-o:] *= np.linspace(1, 0, o, dtype=np.float32)
    return x


def highpass(x: np.ndarray, hz: float) -> np.ndarray:
    return sosfilt(butter(2, hz, "highpass", fs=SR, output="sos"), x).astype(np.float32)


def lowpass(x: np.ndarray, hz: float) -> np.ndarray:
    return sosfilt(butter(4, hz, "lowpass", fs=SR, output="sos"), x).astype(np.float32)


def glitch(x: np.ndarray, amount: float, seed: int) -> np.ndarray:
    """A light, deterministic 'damaged ship AI' filter. amount 0..1. Keeps every word intelligible (verify checks)."""
    if amount <= 0:
        return x
    rng = random.Random(seed)
    t = np.arange(len(x), dtype=np.float32) / SR

    # Stutter: repeat a 55 ms grain at a word onset, only on the most damaged lines, never on a clause's first word
    # (a stutter there turned "Targeting" into "Target editing" in the transcription check).
    hop = int(0.01 * SR)
    rms = frame_rms(x, hop)
    thr = rms.max() * 0.25
    onsets = [i * hop for i in range(1, len(rms)) if rms[i] > thr and rms[i - 1] <= thr][1:]
    count = 1 if amount >= 0.6 else 0
    stutters = sorted(rng.sample(onsets, min(len(onsets), count)))
    grain = int(0.055 * SR)
    pieces, at = [], 0
    for o in stutters:
        if o + grain >= len(x):
            continue
        g = fade(x[o:o + grain], 0.002, 0.004)
        pieces += [x[at:o], g, g * 0.7]
        at = o
    pieces.append(x[at:])
    x = np.concatenate(pieces).astype(np.float32)
    t = np.arange(len(x), dtype=np.float32) / SR

    # Digital grit: 7-bit, 16 kHz sample-and-hold layer, plus a faint ring modulator and a short metallic comb.
    hold = np.repeat(x[::3], 3)[: len(x)]
    crushed = np.round(hold * 64) / 64
    ring = x * np.sin(2 * np.pi * 110 * t)
    comb = np.concatenate([np.zeros(int(0.0045 * SR), np.float32), x])[: len(x)]
    y = x + amount * (0.30 * (crushed - x) + 0.12 * ring + 0.28 * comb)

    # Speaker band-limit grows with damage.
    y = lowpass(highpass(y, 140 + 120 * amount), 9000 - 2500 * amount)
    return y.astype(np.float32)


def limit(x: np.ndarray, ceiling_db: float) -> np.ndarray:
    """Look-ahead-free peak limiter: per-sample gain, held and smoothed over 3 ms."""
    c = 10 ** (ceiling_db / 20)
    g = np.minimum(1.0, c / np.maximum(np.abs(x), 1e-9))
    w = int(0.003 * SR)
    from scipy.ndimage import minimum_filter1d, uniform_filter1d
    g = uniform_filter1d(minimum_filter1d(g, size=2 * w + 1), size=w)
    return (x * np.minimum(g, 1.0)).astype(np.float32)


def true_peak_db(x: np.ndarray) -> float:
    up = resample_poly(x, 4, 1)
    return 20 * np.log10(max(np.max(np.abs(up)), 1e-9))


def loudness(x: np.ndarray) -> float:
    return pyln.Meter(SR).integrated_loudness(x.astype(np.float64))


def normalize(x: np.ndarray) -> tuple[np.ndarray, float, float]:
    """Gain to -16 LUFS, then limit until the 4x-oversampled true peak is under -1 dBTP. The limiter works on samples,
    and peaks between samples can sit a dB higher, so its ceiling steps down until the true peak complies; make-up
    gain and limiting alternate until both targets hold (within 0.3 LU)."""
    for _ in range(5):
        x = x * 10 ** ((TARGET_LUFS - loudness(x)) / 20)
        for step in range(10):
            if true_peak_db(x) <= CEILING_DBTP:
                break
            x = limit(x, CEILING_DBTP - 0.5 - 0.3 * step)
        if abs(loudness(x) - TARGET_LUFS) <= 0.3:
            break
    return x.astype(np.float32), loudness(x), true_peak_db(x)


# ----------------------------------------------------------------------------------------------- engines

class Kokoro:
    """Kokoro-82M v1.0 (Apache-2.0) through kokoro-onnx (MIT), CPU.

    Voices were chosen on 2026-10-09 by measurement (ArtSource/voice/README.md, "Why these voices"): 8 Guide and
    7 AURA candidates rendered on the same lines, scored on transcription error, predicted naturalness (UTMOS) and
    pitch range, because nobody on the build side can listen. A human listen is still the final word.
    """

    VOICES = {"guide": ("bm_fable", 0.90, "en-gb"), "aura": ("af_heart", 1.0, "en-us")}

    # Lines at their own speed because the transcription check heard different words otherwise: "Too big a step"
    # became "two biggest step" at 0.95 and above; a lone "Lower." became "slower" below 1.0.
    LINE_SPEED = {"LrTooHigh": 0.90, "LowerTrigger": 1.0}

    def __init__(self, models: Path, guide: str | None = None, aura: str | None = None, guide_speed: float | None = None):
        from kokoro_onnx import Kokoro as K
        import kokoro_onnx
        self.version = getattr(kokoro_onnx, "__version__", "0.6.1")
        self.k = K(str(models / "kokoro-v1.0.onnx"), str(models / "voices-v1.0.bin"))
        self.voices = dict(self.VOICES)
        if guide:
            self.voices["guide"] = (guide, guide_speed or self.voices["guide"][1], "en-gb" if guide.startswith("b") else "en-us")
        elif guide_speed:
            v = self.voices["guide"]
            self.voices["guide"] = (v[0], guide_speed, v[2])
        if aura:
            self.voices["aura"] = (aura, 1.0, "en-gb" if aura.startswith("b") else "en-us")
        self.name = "kokoro"
        self.line = None  # set by render_line, for LINE_SPEED

    def _voice(self, role: str):
        voice, speed, lang = self.voices["aura" if role.startswith("aura") else "guide"]
        return voice, self.LINE_SPEED.get(self.line, speed), lang

    def describe(self, role: str) -> str:
        voice, speed, lang = self._voice(role)
        return (f"AI synthesized, Kokoro-82M v1.0 (hexgrad, Apache-2.0) via kokoro-onnx {self.version} (MIT); "
                f"voice {voice}, speed {speed}, lang {lang}")

    def synth(self, text: str, role: str):
        voice, speed, lang = self._voice(role)
        samples, sr = self.k.create(text, voice=voice, speed=speed, lang=lang)
        return np.asarray(samples, np.float32), sr


def load_engine(args):
    if args.engine_module:
        spec = importlib.util.spec_from_file_location("tts_backend", args.engine_module)
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        return mod.ENGINE
    if args.engine == "kokoro":
        return Kokoro(Path(args.models), args.guide_voice, args.aura_voice, args.guide_speed)
    sys.exit("Unknown engine " + args.engine)


# ----------------------------------------------------------------------------------------------- render

def render_line(engine, line: dict) -> tuple[np.ndarray, list[float]]:
    role = line["speaker"]
    base = "aura" if role.startswith("aura") else "guide"
    gap = SENTENCE_GAP[base]
    amount = AURA_GLITCH.get(line["name"], 0.0) if role == "aura" else 0.0
    if hasattr(engine, "line"):
        engine.line = line["name"]

    pieces, segments = [], []
    for ci, (clause, pause) in enumerate(clauses(line["text"])):
        sentences = [s for s in SENTENCE_RE.split(clause) if s.strip()]
        audio = []
        for si, sentence in enumerate(sentences):
            raw, sr = engine.synth(spoken(sentence), role)
            audio.append(trim(to_sr(raw, sr)))
            if si < len(sentences) - 1:
                audio.append(np.zeros(int(gap * SR), np.float32))
        clip = np.concatenate(audio)
        seed = line["id"] * 31 + ci + GLITCH_SEED_OFFSET.get(line["name"], 0)
        clip = glitch(clip, amount, seed=seed) if amount else clip
        silence = np.zeros(int(pause * SR), np.float32)
        pieces += [clip, silence]
        segments.append((len(clip) + len(silence)) / SR)

    x = np.concatenate(pieces)
    x = trim(highpass(x, 70), pre=0.01, post=0.08)
    return x, segments


def write_sidecar(path: Path, source: str, segments: list[float], text: str):
    seg = " ".join(f"{s:.3f}" for s in segments)
    path.write_text(f"Source: {source}\nSegments: {seg}\n\n{plain_text(text)}\n", encoding="utf-8")


def cmd_render(args):
    engine = load_engine(args)
    out = Path(args.out or HERE / "out" / engine.name)
    out.mkdir(parents=True, exist_ok=True)
    wanted = {int(s) for s in args.lines.split(",")} if args.lines else None
    today = _dt.date.today().isoformat()
    report = {}
    for line in load_script():
        if line["speaker"] == "archival" or (wanted and line["id"] not in wanted):
            continue
        x, segments = render_line(engine, line)
        x, lufs, tp = normalize(x)
        stem = f"vo_ride_{line['id']:02d}"
        sf.write(out / f"{stem}.wav", x, SR, subtype="PCM_16")
        role_note = ""
        if line["speaker"] == "aura":
            role_note = f"; AURA glitch filter amount {AURA_GLITCH.get(line['name'], 0.0)}"
        elif line["speaker"] == "aura_clean":
            role_note = "; AURA clean (no glitch)"
        source = (f"{engine.describe(line['speaker'])}{role_note}; one sentence per call, explicit pauses, "
                  f"-16 LUFS / -1 dBTP / mono 48 kHz; ArtSource/voice/render_voices.py, {today}")
        write_sidecar(out / f"{stem}.txt", source, segments, line["text"])
        report[stem] = {"line": line["name"], "speaker": line["speaker"], "seconds": round(len(x) / SR, 3),
                        "lufs": round(float(lufs), 2), "true_peak_db": round(float(tp), 2), "segments": [round(float(s), 3) for s in segments]}
        print(f"{stem}  {line['name']:<22} {len(x) / SR:6.2f} s  {lufs:6.2f} LUFS  TP {tp:5.2f} dB")
    (out / "render_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")


# ----------------------------------------------------------------------------------------------- verify

ONES = "zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen".split()
TENS = "_ _ twenty thirty forty fifty sixty seventy eighty ninety".split()


def two_digits(n: int) -> str:
    if n < 20:
        return ONES[n]
    return TENS[n // 10] + ("" if n % 10 == 0 else " " + ONES[n % 10])


def words(text: str) -> list[str]:
    text = text.lower().replace("-", " ").replace("’", "'")
    text = re.sub(r"\b(1[0-9])(\d\d)\b", lambda m: two_digits(int(m.group(1))) + " " + two_digits(int(m.group(2))), text)
    text = re.sub(r"\b(\d{1,2})\b", lambda m: two_digits(int(m.group(1))), text)
    text = re.sub(r"\ba\.i\.?", "ai", text)
    text = re.sub(r"[^a-z0-9' ]", " ", text)
    # "trigger's" and "triggers" sound the same, so 's is joined, not expanded; the rest are expanded both sides.
    text = re.sub(r"'s\b", "s", text).replace("'re", " are").replace("'ll", " will").replace("n't", " not")
    same = {"woah": "whoa", "ok": "okay", "mcculloch": "mcculloch"}
    return [same.get(w, w) for w in text.replace("'", "").split()]


def split_compounds(ref: list[str], hyp: list[str]) -> list[str]:
    """A transcriber sometimes writes two spoken words as one ("an or gate" -> "an orgate"). That is a spelling choice,
    not a missing word, so a hyp word equal to two adjacent script words joined is split back. Nothing else is forgiven."""
    joined = {ref[i] + ref[i + 1]: (ref[i], ref[i + 1]) for i in range(len(ref) - 1)}
    out = []
    for w in hyp:
        out.extend(joined.get(w, (w,)))
    return out


def wer(ref: list[str], hyp: list[str]) -> float:
    d = list(range(len(hyp) + 1))
    for i, r in enumerate(ref, 1):
        prev, d[0] = d[0], i
        for j, h in enumerate(hyp, 1):
            prev, d[j] = d[j], min(d[j] + 1, d[j - 1] + 1, prev + (r != h))
    return d[len(hyp)] / max(1, len(ref))


def cmd_verify(args):
    from faster_whisper import WhisperModel
    out = Path(args.folder)
    model = WhisperModel(args.whisper, device="cpu", compute_type="int8")
    script = {f"vo_ride_{l['id']:02d}": l for l in load_script()}
    results, worst = {}, 0.0
    for wav in sorted(out.glob("vo_ride_*.wav")):
        line = script.get(wav.stem)
        if line is None:
            continue
        audio, sr = sf.read(wav, dtype="float32")
        audio = resample_poly(audio, 16000 // np.gcd(sr, 16000), sr // np.gcd(sr, 16000)).astype(np.float32)
        segs, _ = model.transcribe(audio, language="en", beam_size=5, condition_on_previous_text=False)
        hyp = " ".join(s.text.strip() for s in segs)
        ref_text = plain_text(line["text"])
        ref_words = words(ref_text)
        score = wer(ref_words, split_compounds(ref_words, words(hyp)))
        worst = max(worst, score)
        results[wav.stem] = {"wer": round(score, 3), "heard": hyp, "script": ref_text}
        flag = "OK " if score <= args.max_wer else "BAD"
        print(f"{flag} {wav.stem} WER {score:5.1%}  heard: {hyp}")
    (out / "verify_report.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    bad = [k for k, v in results.items() if v["wer"] > args.max_wer]
    print(f"\n{len(results)} clips checked, worst WER {worst:.1%}, over {args.max_wer:.0%}: {bad or 'none'}")
    sys.exit(1 if bad else 0)


# ----------------------------------------------------------------------------------------------- install

def cmd_install(args):
    src = Path(args.folder)
    copied = 0
    for wav in sorted(src.glob("vo_ride_*.wav")):
        for f in (wav, wav.with_suffix(".txt")):
            shutil.copyfile(f, VOICE_DIR / f.name)
        copied += 1
    print(f"Installed {copied} clips from {src} into {VOICE_DIR}")


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("render")
    r.add_argument("--engine", default="kokoro")
    r.add_argument("--engine-module")
    r.add_argument("--models", default=str(HERE / "models"))
    r.add_argument("--guide-voice")
    r.add_argument("--guide-speed", type=float)
    r.add_argument("--aura-voice")
    r.add_argument("--lines", help="comma-separated RideLine ids, default all")
    r.add_argument("--out")
    v = sub.add_parser("verify")
    v.add_argument("folder")
    v.add_argument("--whisper", default="small.en")
    v.add_argument("--max-wer", type=float, default=0.10)
    i = sub.add_parser("install")
    i.add_argument("folder")
    args = p.parse_args()
    {"render": cmd_render, "verify": cmd_verify, "install": cmd_install}[args.cmd](args)


if __name__ == "__main__":
    main()
