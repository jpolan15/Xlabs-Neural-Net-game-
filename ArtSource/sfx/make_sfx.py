"""
Make the Neural Ride's derived sound effects (master plan WP1/WP4 audio; ADR-012).

    python make_sfx.py            writes Assets/_Project/Audio/Sfx/*.wav and prints the measured loudness of every cue

Sources: Kenney Sci-fi Sounds and Interface Sounds (CC0 1.0, already in Assets/_Project/Audio/Kenney/), plus one
tone synthesized here (CC0, made for this project). Outputs:
  ride_hum_loop.wav     cockpit hum, seamless loop  (spaceEngineLow_002, low-passed, 1 s equal-power crossfade)
  ride_engine_loop.wav  travel engine, seamless loop (engineCircular_000, 1 s equal-power crossfade)
  ride_wrong.wav        two-tone "nope" buzzer for zapping a friendly drone (synthesized)

The loudness table it prints is what the builder's per-cue volumes are computed from (cue target LUFS minus clip
LUFS), because nobody on the build side can listen: levels are set by numbers, then checked in the headset.
"""
from pathlib import Path

import numpy as np
import pyloudnorm as pyln
import soundfile as sf
from scipy.signal import butter, sosfilt

REPO = Path(__file__).resolve().parents[2]
KENNEY = REPO / "Assets" / "_Project" / "Audio" / "Kenney"
OUT = REPO / "Assets" / "_Project" / "Audio" / "Sfx"


def mono(path):
    x, sr = sf.read(path, dtype="float64")
    return (x.mean(axis=1) if x.ndim > 1 else x), sr


def seamless_loop(x, sr, fade_s=1.0):
    """Equal-power crossfade of the tail into the head: the result loops with no click or level dip."""
    n = int(fade_s * sr)
    body, tail = x[:-n].copy(), x[-n:]
    t = np.linspace(0, np.pi / 2, n)
    body[:n] = body[:n] * np.sin(t) + tail * np.cos(t)
    return body


def lowpass(x, sr, hz):
    return sosfilt(butter(4, hz, "lowpass", fs=sr, output="sos"), x)


def buzzer(sr=48000):
    """Two short descending square-ish tones, soft-edged and band-limited so it is clear but not harsh."""
    out = []
    for f, dur in ((330.0, 0.11), (247.0, 0.16)):
        t = np.arange(int(dur * sr)) / sr
        tone = np.tanh(3.0 * np.sin(2 * np.pi * f * t)) * 0.6 + 0.25 * np.sin(2 * np.pi * 2 * f * t)
        env = np.minimum(1, t / 0.008) * np.minimum(1, (dur - t) / 0.03)
        out += [tone * env, np.zeros(int(0.035 * sr))]
    x = lowpass(np.concatenate(out), sr, 3500)
    return x / np.abs(x).max() * 10 ** (-1.5 / 20), sr


def seam_jump(x):
    """Size of the step when the loop wraps, relative to the clip's RMS (lower is smoother)."""
    return abs(x[-1] - x[0]) / (np.sqrt(np.mean(x ** 2)) + 1e-12)


def lufs(x, sr):
    return pyln.Meter(sr, block_size=min(0.4, len(x) / sr * 0.9)).integrated_loudness(x)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    made = {}

    x, sr = mono(KENNEY / "sci-fi" / "Audio" / "spaceEngineLow_002.ogg")
    made["ride_hum_loop.wav"] = (seamless_loop(lowpass(x, sr, 1800), sr), sr)

    x, sr = mono(KENNEY / "sci-fi" / "Audio" / "engineCircular_000.ogg")
    made["ride_engine_loop.wav"] = (seamless_loop(x, sr), sr)

    made["ride_wrong.wav"] = buzzer()

    for name, (x, sr) in made.items():
        x = x * min(1.0, 10 ** (-1 / 20) / np.abs(x).max())  # peak at most -1 dBFS (the low-pass overshoots)
        sf.write(OUT / name, x.astype(np.float32), sr, subtype="PCM_16")
        extra = f"  seam {seam_jump(x):.3f}" if "loop" in name else ""
        print(f"{name:24s} {len(x) / sr:5.2f} s  {lufs(x, sr):6.1f} LUFS  peak {20 * np.log10(np.abs(x).max()):5.1f} dBFS{extra}")


if __name__ == "__main__":
    main()
