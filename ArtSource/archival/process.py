#!/usr/bin/env python3
"""Reproduce the archival ride clips from their public-domain sources (see SOURCES.md).

  vo_ride_16.wav             JFK, Rice University, 12 Sep 1962 - the "We choose to go to the Moon ..." sentence only
  vo_ride_16_alt_leadin.wav  optional variant: the two spoken lead-ins + shortened applause + the sentence
  vo_ride_24.wav             Apollo 11 launch, 16 Jul 1969 - countdown "three ... zero", "all engine running", liftoff call

Chain per clip (float64 throughout until the final 16-bit write):
  sequential decode -> mono (channel mean) -> cut region + 0.75 s guard -> resample to 48 kHz (polyphase 160/147)
  -> 2nd-order Butterworth high-pass @ 80 Hz -> exact trim -> (optional equal-power crossfade join)
  -> 15 ms fade-in / 120 ms fade-out (raised cosine) -> gain to -16 LUFS integrated (ITU-R BS.1770-4, pyloudnorm)
  -> look-ahead peak limiter ONLY if the sample peak would exceed -1 dBFS (not needed for these clips)
  -> 16-bit PCM WAV, mono, 48 kHz, rounded (no dither).
Nothing else touches the voice: no EQ, de-noise, compression, or time/pitch change.

Usage:
  python process.py --jfk JFKWHA-127-002-AU_WR.mp3 --apollo 590320main_ringtone_apollo11_countdown.mp3 [--out DIR] [--verify]
Needs numpy, scipy, soundfile, pyloudnorm. --verify also needs faster-whisper (prints small.en / base.en transcripts).
Cut points are seconds in the decoded source timeline (libsndfile and ffmpeg decode these MP3s sample-aligned).
Environment used: Python 3.13 venv at C:\\Users\\Panda\\.cache\\archival\\venv (numpy 2.5.3, scipy 1.18.1, soundfile 0.14.0,
pyloudnorm 0.2.0, faster-whisper 1.2.1). On Windows keep venv paths short; deep paths break pip (onnxruntime).
"""
import argparse
import json
import math
import os

import numpy as np
import pyloudnorm as pyln
import soundfile as sf
from scipy.ndimage import minimum_filter1d
from scipy.signal import butter, resample_poly, sosfilt

SR = 48000
TARGET_LUFS = -16.0
PEAK_CEIL_DB = -1.0
GUARD_S = 0.75  # extra audio decoded/filtered either side of a cut so the filters are settled

CLIPS = [
    dict(name="vo_ride_16", src="jfk", fade_out_ms=120,
         segments=[(535.43, 543.12)]),                      # "We choose ... because they are hard."
    dict(name="vo_ride_16_alt_leadin", src="jfk", fade_out_ms=120, xfade_ms=250,
         segments=[(524.84, 530.30), (535.00, 543.12)]),    # lead-in x2 + 1.2 s of applause, then the sentence
    dict(name="vo_ride_24", src="apollo", fade_out_ms=120,
         segments=[(5.03, 13.84)]),                         # "Three, two, one, zero. All engine running. Liftoff. We have a liftoff."
]


def load_mono(path):
    x, sr = sf.read(path, dtype="float32", always_2d=True)
    return x.mean(axis=1), sr


def cut_segment(x, sr, t0, t1):
    """48 kHz, high-passed audio for source seconds [t0, t1], sample-exact on the 48 kHz grid."""
    a = max(0, int(round((t0 - GUARD_S) * sr)))
    b = min(len(x), int(round((t1 + GUARD_S) * sr)))
    g = math.gcd(SR, sr)
    y = resample_poly(x[a:b].astype(np.float64), SR // g, sr // g)
    y = sosfilt(butter(2, 80.0, "highpass", fs=SR, output="sos"), y)
    start = a / sr  # source time of y[0]
    return y[int(round((t0 - start) * SR)):int(round((t1 - start) * SR))]


def join(parts, xfade_ms):
    out = parts[0]
    n = int(SR * xfade_ms / 1000)
    for p in parts[1:]:
        u = np.linspace(0.0, 1.0, n)
        mix = out[-n:] * np.cos(0.5 * np.pi * u) + p[:n] * np.sin(0.5 * np.pi * u)  # equal power
        out = np.concatenate([out[:-n], mix, p[n:]])
    return out


def fades(y, in_ms, out_ms):
    y = y.copy()
    ni, no = int(SR * in_ms / 1000), int(SR * out_ms / 1000)
    y[:ni] *= 0.5 * (1 - np.cos(np.pi * np.arange(ni) / ni))
    y[-no:] *= 0.5 * (1 + np.cos(np.pi * np.arange(no) / (no - 1)))
    return y


def peak_limit(x, ceil_lin, look_ms=5.0):
    """Offline look-ahead limiter: smoothed running-minimum of the required gain, so |out| <= ceil everywhere."""
    la = int(SR * look_ms / 1000)
    need = np.minimum(1.0, ceil_lin / np.maximum(np.abs(x), 1e-12))
    gmin = minimum_filter1d(need, size=2 * la + 1, mode="nearest")
    w = np.hanning(2 * la + 1)
    g = np.convolve(np.pad(gmin, la, mode="edge"), w / w.sum(), mode="valid")
    return x * g, float(20 * np.log10(g.min()))


def db(v):
    return float(20 * np.log10(max(v, 1e-12)))


def normalise(y):
    meter = pyln.Meter(SR)
    ceil = 10 ** (PEAK_CEIL_DB / 20)
    gain_db = TARGET_LUFS - meter.integrated_loudness(y)
    for _ in range(12):
        z, limited_db = y * 10 ** (gain_db / 20), 0.0
        if np.max(np.abs(z)) > ceil:  # only touch the signal if the -1 dBFS ceiling would be exceeded
            z, limited_db = peak_limit(z, ceil)
        err = TARGET_LUFS - meter.integrated_loudness(z)
        if abs(err) < 0.05 or limited_db < -3.0:  # on target, or the limiter is already working too hard
            break
        gain_db += err
    return z, gain_db, limited_db


def measure(path):
    x, sr = sf.read(path, dtype="float64")
    assert sr == SR and x.ndim == 1
    return dict(duration_s=round(len(x) / sr, 2), lufs=round(pyln.Meter(sr).integrated_loudness(x), 2),
                sample_peak_dbfs=round(db(np.max(np.abs(x))), 2),
                true_peak_dbfs=round(db(np.max(np.abs(resample_poly(x, 4, 1)))), 2))


def transcripts(path):
    from faster_whisper import WhisperModel
    x, _ = sf.read(path, dtype="float32")
    x16 = resample_poly(x, 1, 3).astype(np.float32)
    out = {}
    for name in ("small.en", "base.en"):
        segs, _ = WhisperModel(name, device="cpu", compute_type="int8").transcribe(
            x16, language="en", word_timestamps=True, beam_size=5, condition_on_previous_text=False)
        out[name] = " ".join(s.text.strip() for s in segs)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--jfk", required=True, help="JFKWHA-127-002-AU_WR.mp3 (JFK Library)")
    ap.add_argument("--apollo", required=True, help="590320main_ringtone_apollo11_countdown.mp3 (NASA)")
    ap.add_argument("--out", default=os.path.dirname(os.path.abspath(__file__)))
    ap.add_argument("--verify", action="store_true")
    args = ap.parse_args()
    sources = {"jfk": load_mono(args.jfk), "apollo": load_mono(args.apollo)}
    for c in CLIPS:
        x, sr = sources[c["src"]]
        y = join([cut_segment(x, sr, a, b) for a, b in c["segments"]], c.get("xfade_ms", 0))
        y, gain_db, limited_db = normalise(fades(y, 15, c["fade_out_ms"]))
        path = os.path.join(args.out, c["name"] + ".wav")
        sf.write(path, y, SR, subtype="PCM_16")
        r = measure(path)
        r.update(gain_db=round(gain_db, 2), max_limiter_reduction_db=round(limited_db, 2), source_cuts_s=c["segments"])
        if args.verify:
            r["transcripts"] = transcripts(path)
        print(c["name"], json.dumps(r))


if __name__ == "__main__":
    main()
