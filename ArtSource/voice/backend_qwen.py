"""
Qwen3-TTS backend for render_voices.py: "design the voice once, then clone it" (Apache-2.0 models, local GPU).

    cd ArtSource/voice
    <qwen venv python> render_voices.py render --engine-module backend_qwen.py --out out/qwen [--lines 17,20]

Why design once, then clone
  Describing a voice in words and re-designing it for every sentence lets the timbre wander from call to call,
  and this ride needs ONE narrator across ~120 sentences. The model card's own recipe for a reusable character
  voice ("Voice Design then Clone") is used instead:
    1. Qwen3-TTS-12Hz-1.7B-VoiceDesign speaks a short neutral paragraph, once per role, from a written voice
       description and a fixed seed. The clip is saved as out/qwen/reference_<role>.wav next to a .json holding the
       exact text, instruction and seed. An existing reference is reused, never redesigned.
    2. Every sentence is then rendered by Qwen3-TTS-12Hz-1.7B-Base as a voice clone of that reference (in-context
       mode: reference audio plus its transcript), one sentence per call, with a fixed seed per call.
  The two models are never in VRAM together (VoiceDesign is freed before Base loads), so the peak stays near 5 GB.

Environment knobs (all optional)
  QWEN_TTS_SEED      base seed for the cloning calls (default 1234). Combine with --lines to re-roll a failed line.
  QWEN_TTS_REF_DIR   folder holding reference_<role>.wav/.json (default out/qwen beside this file)
  QWEN_TTS_ATTN      attention implementation passed to transformers (default "sdpa"; flash-attn is not installed)
  HF_HOME            model cache (default ~/.cache/qwen/hf)

Safety net against LLM-TTS failures: every sentence gets a max_new_tokens cap proportional to its word count. A
sentence that runs into the cap (no end-of-speech token, the usual "kept talking" failure) or comes back implausibly
short is regenerated with seed + 1000 * attempt, up to MAX_RETRIES times. Every call is logged to synth_log.jsonl.

Other uses of this file:
    python backend_qwen.py design [--roles guide,aura] [--out DIR]   make/inspect the reference clips only
    python backend_qwen.py say "A sentence." [--role guide] [--out x.wav]   one-off test sentence
"""
from __future__ import annotations

import atexit
import datetime as _dt
import gc
import json
import math
import os
import sys
import time
from pathlib import Path

os.environ.setdefault("HF_HOME", str(Path.home() / ".cache" / "qwen" / "hf"))
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")
os.environ.setdefault("TOKENIZERS_PARALLELISM", "false")

import numpy as np

HERE = Path(__file__).resolve().parent
REF_DIR = Path(os.environ.get("QWEN_TTS_REF_DIR") or (HERE / "out" / "qwen"))

# ----------------------------------------------------------------------------------------------- what is used

# Exact repos and the commits that were current when this backend was written (pinned so a later upstream
# change cannot silently change the voice). Both cards say `license: apache-2.0`.
DESIGN_REPO, DESIGN_REV = "Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign", "5ecdb67327fd37bb2e042aab12ff7391903235d3"
CLONE_REPO, CLONE_REV = "Qwen/Qwen3-TTS-12Hz-1.7B-Base", "fd4b254389122332181a7c3db7f27e918eec64e3"

LANGUAGE = "English"          # explicit: "Auto" can mis-detect on one-word sentences such as "Lower."
ATTN = os.environ.get("QWEN_TTS_ATTN", "sdpa")
TOKEN_RATE = 12.5             # codec frames per second of the 12Hz tokenizer

# Fixed seeds. DESIGN_SEEDS pick the reference voices; the cloning seed is the default for every sentence.
DESIGN_SEEDS = {"guide": 1, "aura": 1}
DEFAULT_SEED = 1234
# Persistent per-sentence re-rolls, keyed by the exact sentence text handed to synth(). Filled in only where a
# sentence needed a different seed to be spoken correctly (see QWEN_NOTES.md).
SEED_OVERRIDES: dict[str, int] = {}

MAX_RETRIES = 3

VOICES = {
    "guide": {
        "instruct": ("An older, warm, calm male narrator with a little gravel in the voice and a slight British accent. "
                     "Unhurried, measured pacing, clear diction, gentle authority, like a kind teacher or a wise mentor. "
                     "Never theatrical, never sing-song."),
        "ref_text": ("The quick brown fox jumps over the lazy dog near the old stone bridge. "
                     "Sixty small boats drift across the quiet harbor while the evening light fades behind the hills."),
    },
    "aura": {
        "instruct": ("A young adult female ship computer AI voice. Clear, bright, friendly and curious, "
                     "slightly synthetic but warm, American accent, natural and expressive."),
        "ref_text": ("Five boxing wizards jump quickly over the bright, busy market. "
                     "Every morning the stalls open with fresh bread, ripe oranges, and a little music drifting down the street. "
                     "Then the sun climbs slowly over the rooftops."),
    },
}


def _base(role: str) -> str:
    return "aura" if role.startswith("aura") else "guide"


def _seed_everything(seed: int) -> None:
    import random
    import torch
    random.seed(seed)
    np.random.seed(seed % (2 ** 32))
    torch.manual_seed(seed)
    if torch.cuda.is_available():
        torch.cuda.manual_seed_all(seed)


def _ref_paths(role: str) -> tuple[Path, Path]:
    return REF_DIR / f"reference_{role}.wav", REF_DIR / f"reference_{role}.json"


def _tidy_reference(wav: np.ndarray, sr: int) -> np.ndarray:
    """Trim the designed clip's edges to a short natural lead/tail so the clone prompt carries no dead air."""
    hop = max(1, int(0.01 * sr))
    n = len(wav) // hop
    rms = np.sqrt(np.mean(wav[: n * hop].reshape(n, hop) ** 2, axis=1) + 1e-12)
    thr = max(rms.max() * 10 ** (-45 / 20), 1e-4)
    on = np.where(rms > thr)[0]
    if len(on) == 0:
        return wav
    a = max(0, on[0] * hop - int(0.12 * sr))
    b = min(len(wav), (on[-1] + 1) * hop + int(0.25 * sr))
    out = wav[a:b].copy()
    fi, fo = int(0.005 * sr), int(0.03 * sr)
    out[:fi] *= np.linspace(0, 1, fi, dtype=np.float32)
    out[-fo:] *= np.linspace(1, 0, fo, dtype=np.float32)
    return out


class QwenEngine:
    name = "qwen"

    def __init__(self):
        self._design = None
        self._clone = None
        self._clone_path = None
        self._gen_cfg: dict = {}
        self._prompts: dict = {}
        self._meta: dict = {}
        self._line_seeds: list[int] = []
        self._line_fresh = True
        self._run_id = _dt.datetime.now().strftime("%Y%m%d-%H%M%S")
        self._t_start = time.time()
        self._stats = {"calls": 0, "attempts": 0, "retries": 0, "audio_seconds": 0.0, "synth_wall_seconds": 0.0,
                       "load_wall_seconds": 0.0, "design_wall_seconds": 0.0,
                       "peak_vram_design_allocated_gb": None, "peak_vram_design_reserved_gb": None}
        atexit.register(self._write_run_log)

    # ------------------------------------------------------------------------------------------- pipeline contract

    def base_seed(self) -> int:
        env = os.environ.get("QWEN_TTS_SEED")
        return int(env) if env else DEFAULT_SEED

    def describe(self, role: str) -> str:
        base = _base(role)
        v = VOICES[base]
        meta = self._meta.get(base) or self._read_meta(base) or {}
        seeds = list(dict.fromkeys(self._line_seeds)) or [self.base_seed()]
        seed_txt = (f"seed {seeds[0]}" if len(seeds) == 1 else "seeds " + ", ".join(str(s) for s in seeds))
        ref_s = meta.get("seconds")
        self._line_fresh = True
        return (f"AI synthesized, Qwen3-TTS-12Hz-1.7B-Base (Alibaba Qwen, {CLONE_REPO} @ {CLONE_REV[:7]}, Apache-2.0) "
                f"via qwen-tts {self._pkg_version('qwen-tts')} (Apache-2.0), voice-cloned from reference_{base}.wav "
                f"({ref_s if ref_s is not None else '?'} s) that Qwen3-TTS-12Hz-1.7B-VoiceDesign ({DESIGN_REPO} @ {DESIGN_REV[:7]}, "
                f"Apache-2.0) designed once with design seed {meta.get('seed', DESIGN_SEEDS[base])} from the brief "
                f"\"{v['instruct']}\"; language {LANGUAGE}, bf16, sdpa attention, sampling "
                f"{self._sampling_text()}; clone {seed_txt}")

    def synth(self, text: str, role: str):
        base = _base(role)
        self._prepare()
        if self._line_fresh:
            self._line_seeds = []
            self._line_fresh = False

        words = max(1, len(text.split()))
        max_seconds = 0.85 * words + 2.0                       # >1.4x slower than the slowest plausible narration
        cap = int(math.ceil(TOKEN_RATE * max_seconds))
        min_seconds = 0.12 * words + 0.25
        base_seed = SEED_OVERRIDES.get(text, self.base_seed())

        tries = []
        for attempt in range(MAX_RETRIES + 1):
            seed = base_seed + 1000 * attempt
            t0 = time.time()
            wav, sr = self._clone_once(text, base, seed, cap)
            wall = time.time() - t0
            dur = len(wav) / sr
            hit_cap = dur >= (cap - 1) / TOKEN_RATE
            too_short = dur < min_seconds
            finite = bool(np.isfinite(wav).all())
            ok = finite and not hit_cap and not too_short
            self._stats["attempts"] += 1
            self._stats["synth_wall_seconds"] += wall
            self._log({"run": self._run_id, "role": base, "seed": seed, "attempt": attempt, "text": text,
                       "audio_s": round(dur, 3), "wall_s": round(wall, 2), "cap_tokens": cap, "hit_cap": hit_cap,
                       "too_short": too_short, "finite": finite, "ok": ok})
            tries.append((abs(dur - 0.45 * words) + (0 if ok else 1e6), seed, wav, sr))
            if ok:
                break
            self._stats["retries"] += 1
            print(f"    [qwen] retry {attempt + 1}/{MAX_RETRIES} for {text[:48]!r}: "
                  f"{'hit token cap' if hit_cap else 'too short' if too_short else 'non-finite audio'} ({dur:.1f} s)")
        _, seed, wav, sr = min(tries, key=lambda t: t[0])
        self._stats["calls"] += 1
        self._stats["audio_seconds"] += len(wav) / sr
        self._line_seeds.append(seed)
        return wav.astype(np.float32), int(sr)

    # ------------------------------------------------------------------------------------------- models

    @staticmethod
    def _pkg_version(name: str) -> str:
        try:
            from importlib.metadata import version
            return version(name)
        except Exception:
            return "?"

    def _sampling_text(self) -> str:
        g = self._gen_cfg or {}
        return (f"temperature {g.get('temperature', 0.9)}, top_k {g.get('top_k', 50)}, top_p {g.get('top_p', 1.0)}, "
                f"repetition_penalty {g.get('repetition_penalty', 1.05)}")

    @staticmethod
    def _snapshot(repo: str, rev: str) -> str:
        from huggingface_hub import snapshot_download
        try:
            return snapshot_download(repo_id=repo, revision=rev, local_files_only=True)
        except Exception:
            # max_workers=1: threaded downloads race on Windows' symlink probe and crash with WinError 1314.
            return snapshot_download(repo_id=repo, revision=rev, max_workers=1)

    def _load(self, repo: str, rev: str):
        import torch
        from qwen_tts import Qwen3TTSModel
        path = self._snapshot(repo, rev)
        gc_path = Path(path) / "generation_config.json"
        if gc_path.exists():
            self._gen_cfg = json.loads(gc_path.read_text(encoding="utf-8"))
        cuda = torch.cuda.is_available()
        model = Qwen3TTSModel.from_pretrained(
            path, device_map="cuda:0" if cuda else "cpu",
            dtype=torch.bfloat16 if cuda else torch.float32, attn_implementation=ATTN)
        return model, path

    @staticmethod
    def _free_vram() -> None:
        import torch
        gc.collect()
        if torch.cuda.is_available():
            torch.cuda.empty_cache()

    def _prepare(self) -> None:
        """Make sure both reference clips exist, then load the Base model and build the two clone prompts."""
        if self._clone is not None:
            return
        import torch
        missing = [r for r in VOICES if not _ref_paths(r)[0].exists()]
        if missing:
            self.design(missing)
        else:
            for r in VOICES:
                self._check_meta(r)
        t0 = time.time()
        if torch.cuda.is_available():
            torch.cuda.reset_peak_memory_stats()
        self._clone, self._clone_path = self._load(CLONE_REPO, CLONE_REV)
        import soundfile as sf
        for role, v in VOICES.items():
            wav_path, _ = _ref_paths(role)
            wav, sr = sf.read(wav_path, dtype="float32")
            self._meta[role] = self._read_meta(role)
            self._prompts[role] = self._clone.create_voice_clone_prompt(
                ref_audio=(wav, int(sr)), ref_text=v["ref_text"], x_vector_only_mode=False)
        self._stats["load_wall_seconds"] += time.time() - t0
        print(f"[qwen] Base model loaded in {time.time() - t0:.0f} s, reference clips: "
              + ", ".join(f"{r} {self._meta[r]['seconds']} s" for r in VOICES), flush=True)

    # ------------------------------------------------------------------------------------------- design

    def _read_meta(self, role: str):
        p = _ref_paths(role)[1]
        return json.loads(p.read_text(encoding="utf-8")) if p.exists() else None

    def _check_meta(self, role: str) -> None:
        meta = self._read_meta(role)
        v = VOICES[role]
        if meta is None or meta.get("text") != v["ref_text"] or meta.get("instruct") != v["instruct"]:
            sys.exit(f"{_ref_paths(role)[0]} exists but its .json does not match VOICES['{role}'] in backend_qwen.py. "
                     f"Delete reference_{role}.wav and .json to design a new reference, or restore the old text.")

    def design(self, roles=None, seeds=None, out_dir: Path | None = None, force: bool = False) -> dict:
        """Design one reference clip per role with the VoiceDesign model, then free the model. Returns role -> meta."""
        import soundfile as sf
        import torch
        roles = list(roles or VOICES)
        seeds = {**DESIGN_SEEDS, **(seeds or {})}
        out_dir = Path(out_dir) if out_dir else REF_DIR
        out_dir.mkdir(parents=True, exist_ok=True)
        todo = [r for r in roles if force or not (out_dir / f"reference_{r}.wav").exists()]
        done = {}
        if todo:
            t0 = time.time()
            if torch.cuda.is_available():
                torch.cuda.reset_peak_memory_stats()
            model, _ = self._load(DESIGN_REPO, DESIGN_REV)
            try:
                for role in todo:
                    v = VOICES[role]
                    _seed_everything(seeds[role])
                    wavs, sr = model.generate_voice_design(
                        text=v["ref_text"], language=LANGUAGE, instruct=v["instruct"],
                        max_new_tokens=int(TOKEN_RATE * 40))
                    wav = _tidy_reference(np.asarray(wavs[0], np.float32).reshape(-1), int(sr))
                    wav_path, meta_path = out_dir / f"reference_{role}.wav", out_dir / f"reference_{role}.json"
                    sf.write(wav_path, wav, int(sr), subtype="PCM_16")
                    meta = {"role": role, "text": v["ref_text"], "instruct": v["instruct"], "language": LANGUAGE,
                            "seed": seeds[role], "model": DESIGN_REPO, "revision": DESIGN_REV, "sample_rate": int(sr),
                            "seconds": round(len(wav) / sr, 2), "created": _dt.datetime.now().isoformat(timespec="seconds"),
                            "sampling": self._sampling_text(), "attn": ATTN, "dtype": "bfloat16"}
                    meta_path.write_text(json.dumps(meta, indent=2), encoding="utf-8")
                    done[role] = meta
                    print(f"[qwen] designed reference_{role}.wav: {meta['seconds']} s at {sr} Hz, seed {seeds[role]}", flush=True)
            finally:
                if torch.cuda.is_available():
                    self._stats["peak_vram_design_allocated_gb"] = round(torch.cuda.max_memory_allocated() / 1e9, 2)
                    self._stats["peak_vram_design_reserved_gb"] = round(torch.cuda.max_memory_reserved() / 1e9, 2)
                del model
                self._free_vram()
            self._stats["design_wall_seconds"] += time.time() - t0
        return done

    # ------------------------------------------------------------------------------------------- one cloning call

    def _clone_once(self, text: str, role: str, seed: int, cap: int):
        _seed_everything(seed)
        wavs, sr = self._clone.generate_voice_clone(
            text=text, language=LANGUAGE, voice_clone_prompt=self._prompts[role], max_new_tokens=cap)
        return np.ascontiguousarray(np.asarray(wavs[0], np.float32).reshape(-1)), int(sr)

    # ------------------------------------------------------------------------------------------- logs

    def _log(self, row: dict) -> None:
        REF_DIR.mkdir(parents=True, exist_ok=True)
        with (REF_DIR / "synth_log.jsonl").open("a", encoding="utf-8") as f:
            f.write(json.dumps(row, ensure_ascii=False) + "\n")

    def _write_run_log(self) -> None:
        if not self._stats["calls"] and not self._stats["design_wall_seconds"]:
            return
        row = {"run": self._run_id, "argv": sys.argv[1:], "wall_seconds": round(time.time() - self._t_start, 1),
               "python": sys.version.split()[0], "qwen_tts": self._pkg_version("qwen-tts"),
               "transformers": self._pkg_version("transformers"), "torch": self._pkg_version("torch"),
               "attn": ATTN, **{k: (round(v, 2) if isinstance(v, float) else v) for k, v in self._stats.items()}}
        try:
            import torch
            if torch.cuda.is_available():
                row["gpu"] = torch.cuda.get_device_name(0)
                row["peak_vram_clone_allocated_gb"] = round(torch.cuda.max_memory_allocated() / 1e9, 2)
                row["peak_vram_clone_reserved_gb"] = round(torch.cuda.max_memory_reserved() / 1e9, 2)
        except Exception:
            pass
        REF_DIR.mkdir(parents=True, exist_ok=True)
        with (REF_DIR / "qwen_run_log.jsonl").open("a", encoding="utf-8") as f:
            f.write(json.dumps(row) + "\n")


ENGINE = QwenEngine()


# ----------------------------------------------------------------------------------------------- command line

def _main(argv=None) -> None:
    import argparse
    p = argparse.ArgumentParser(description="Qwen3-TTS backend helpers")
    sub = p.add_subparsers(dest="cmd", required=True)
    d = sub.add_parser("design", help="make the reference clips (skips roles that already have one)")
    d.add_argument("--roles", default="guide,aura")
    d.add_argument("--seed", action="append", default=[], help="role=seed override, e.g. guide=3")
    d.add_argument("--out", help="folder to write into instead of out/qwen")
    d.add_argument("--force", action="store_true")
    s = sub.add_parser("say", help="render one sentence with the cloned voice")
    s.add_argument("text")
    s.add_argument("--role", default="guide")
    s.add_argument("--out", default="say.wav")
    a = p.parse_args(argv)
    if a.cmd == "design":
        seeds = {kv.split("=")[0]: int(kv.split("=")[1]) for kv in a.seed}
        ENGINE.design(a.roles.split(","), seeds, Path(a.out) if a.out else None, a.force)
    else:
        import soundfile as sf
        wav, sr = ENGINE.synth(a.text, a.role)
        sf.write(a.out, wav, sr, subtype="PCM_16")
        print(f"wrote {a.out}: {len(wav) / sr:.2f} s at {sr} Hz")


if __name__ == "__main__":
    _main()
