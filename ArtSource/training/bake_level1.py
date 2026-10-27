"""Bake Level 1's tiny handwritten-digit network from real MNIST.

Everything that glows in Level 1 comes from this file's output: a 49 -> 16 -> 10 network (7x7 pixels in,
16 sigmoid hidden neurons, 10 softmax outputs) trained offline with plain full-batch gradient descent on the
60,000 MNIST training digits. The JSON holds the untrained and trained weights, the hero "3", the neuron puzzle's
digits, the real loss landscape the Valley is built from, the finale's digits with their real predictions, and a
gallery of twenty real test threes (28x28) for the dock's opening.

Run (NumPy only, deterministic, fails loudly on any broken check):
    uv run --with numpy python -I ArtSource/training/bake_level1.py --out Assets/_Project/Data/Level1/level1_digits.json

MNIST is downloaded once into ArtSource/training/mnist/ (git-ignored) and checked against its known MD5s.
MNIST: Yann LeCun and Corinna Cortes, CC BY-SA 3.0.

The math matches Convergence.Core.Neural exactly: z = b + sum(w_i x_i); sigmoid clamps at |z| > 45 like
ActivationFunctions.Sigmoid; softmax is layer-wide with the max shift; weights are stored row-major per neuron
(w1[j*49+i] is hidden neuron j's weight on pixel i). Every value written is rounded first, and every derived number
(golden activations, guesses, accuracy, predictions) is recomputed from the rounded values, so Core reproduces it.
"""

import argparse
import base64
import datetime
import gzip
import hashlib
import json
import os
import struct
import sys
import urllib.request

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
CACHE = os.path.join(HERE, "mnist")
MIRROR = "https://storage.googleapis.com/cvdf-datasets/mnist/"
FILES = {
    "train_images": ("train-images-idx3-ubyte.gz", "f68b3c2dcbeaaa9fbdd348bbdeb94873"),
    "train_labels": ("train-labels-idx1-ubyte.gz", "d53e105ee54ea40749a09fcbcd1e9432"),
    "test_images": ("t10k-images-idx3-ubyte.gz", "9fb629c4189551a2d022fa330f9573f3"),
    "test_labels": ("t10k-labels-idx1-ubyte.gz", "ec29112dd5afa0611ce80d1b7f02629c"),
}

N_IN, N_HID, N_OUT = 49, 16, 10
SEED_INIT = 7            # weight init
LR = 2.0                 # normal learning rate (mean cross-entropy gradient)
LR_FIRST = 10.0          # step 1 on purpose too big: the overshoot the Valley shows
STEPS = 3000             # real gradient-descent steps after the first
HOP_STEPS = [1, 3, 8, 20, 50, 120, 250, 500, 900, 1500, 2200, 3001]   # trajectory index after each hop
SAMPLES_PER_HOP = 16     # ribbon rows per hop
WALL_SAMPLES = 13        # ribbon columns across
WALL_HALF_WIDTH = 30.0  # how far sideways the walls look, in weight-space units
SEED_WALL = 1
PUZZLE_THRESHOLD = 5.0   # owner, 2026-10-27: at 2.5 four green cells won ("it doesn't look like a 3"); at 5 it takes 8+ in a 3 shape
PUZZLE_OTHERS = [0, 1, 7, 4]   # the neuron puzzle: threes fire, one each of these stays dark
PUZZLE_CUT = 0.1         # ranking pattern: + where threes have this much more ink on average than the others, - where less
PUZZLE_SHOWN_PLUS_CUT = 0.03   # the reference the tests and E2 use: a fuller green 3 (same red), so it clears the line of 5
FINALE_COUNT = 60
GALLERY_COUNT = 20       # the dock's cloud of real threes (E0), 28x28 each
GALLERY_DEC = 2          # decimals kept for gallery pixels (display only)
SEED_FINALE = 2026
TARGET_THREES = 12       # the travel game (blast the 3s): real threes...
TARGET_OTHERS = [8, 5, 2, 9, 7, 0]   # ...and look-alikes to spare
TARGET_PER_OTHER = 2

W_DEC = 9                # decimals kept for weights
PX_DEC = 4               # decimals kept for pixels

# Live training (v6, ADR-020): the rider trains the network in the headset with mini-batch SGD from the same initial
# weights, on a balanced subset of the real training digits, and is scored on a balanced subset of the test digits.
LIVE_TRAIN_PER_CLASS = 300
LIVE_TEST_PER_CLASS = 50
LIVE_SEED = 11            # which digits go into the subsets
LIVE_BATCH = 32
LIVE_RATES = [0.15, 4.0, 40.0]   # the three step-size buttons: tiny, good, huge
LIVE_TARGET = 0.85        # the training game is won at this test accuracy
LIVE_SAMPLER_SEED = 2463534242  # xorshift32 seed of the batch sampler (LiveTrainer uses the same one)
LIVE_MAX_STEPS = 900


class BakeError(Exception):
    pass


def check(condition, message):
    if not condition:
        raise BakeError(message)


# ---------------------------------------------------------------- data

def fetch():
    os.makedirs(CACHE, exist_ok=True)
    paths = {}
    for key, (name, md5) in FILES.items():
        path = os.path.join(CACHE, name)
        if not os.path.exists(path):
            print(f"downloading {name}")
            urllib.request.urlretrieve(MIRROR + name, path)
        digest = hashlib.md5(open(path, "rb").read()).hexdigest()
        check(digest == md5, f"{name}: md5 {digest} != {md5}; delete it and re-run")
        paths[key] = path
    return paths


def read_images(path):
    with gzip.open(path, "rb") as f:
        magic, n, rows, cols = struct.unpack(">IIII", f.read(16))
        check(magic == 2051 and rows == 28 and cols == 28, f"{path}: bad image header")
        return np.frombuffer(f.read(), dtype=np.uint8).reshape(n, 28, 28)


def read_labels(path):
    with gzip.open(path, "rb") as f:
        magic, n = struct.unpack(">II", f.read(8))
        check(magic == 2049, f"{path}: bad label header")
        return np.frombuffer(f.read(), dtype=np.uint8).astype(np.int64)


def pool7(images):
    """28x28 uint8 -> 7x7 average of each 4x4 block, 0 = paper, 1 = ink, rounded to PX_DEC."""
    x = images.reshape(-1, 7, 4, 7, 4).astype(np.float64).mean(axis=(2, 4)) / 255.0
    return np.round(x.reshape(-1, 49), PX_DEC)


# ---------------------------------------------------------------- network (Core's math)

def sigmoid(z):
    out = 1.0 / (1.0 + np.exp(-np.clip(z, -45.0, 45.0)))
    out = np.where(z < -45.0, 0.0, out)
    return np.where(z > 45.0, 1.0, out)


def softmax(z):
    e = np.exp(z - z.max(axis=-1, keepdims=True))
    return e / e.sum(axis=-1, keepdims=True)


def unpack(theta):
    i = 0
    w1 = theta[i:i + N_HID * N_IN].reshape(N_HID, N_IN); i += N_HID * N_IN
    b1 = theta[i:i + N_HID]; i += N_HID
    w2 = theta[i:i + N_OUT * N_HID].reshape(N_OUT, N_HID); i += N_OUT * N_HID
    b2 = theta[i:i + N_OUT]
    return w1, b1, w2, b2


N_PARAMS = N_HID * N_IN + N_HID + N_OUT * N_HID + N_OUT


def forward(theta, x):
    w1, b1, w2, b2 = unpack(theta)
    h = sigmoid(x @ w1.T + b1)
    p = softmax(h @ w2.T + b2)
    return h, p


def loss_of(theta, x, y):
    _, p = forward(theta, x)
    return float(-np.mean(np.log(np.maximum(p[np.arange(len(y)), y], 1e-300))))


def gradient(theta, x, y):
    w1, b1, w2, b2 = unpack(theta)
    h, p = forward(theta, x)
    n = len(y)
    dz2 = p.copy()
    dz2[np.arange(n), y] -= 1.0
    dz2 /= n
    dw2 = dz2.T @ h
    db2 = dz2.sum(axis=0)
    dz1 = (dz2 @ w2) * h * (1.0 - h)
    dw1 = dz1.T @ x
    db1 = dz1.sum(axis=0)
    loss = float(-np.mean(np.log(np.maximum(p[np.arange(n), y], 1e-300))))
    return np.concatenate([dw1.ravel(), db1, dw2.ravel(), db2]), loss


def init_theta(seed):
    rng = np.random.default_rng(seed)
    a1 = np.sqrt(6.0 / (N_IN + N_HID))
    a2 = np.sqrt(6.0 / (N_HID + N_OUT))
    w1 = rng.uniform(-a1, a1, size=(N_HID, N_IN))
    w2 = rng.uniform(-a2, a2, size=(N_OUT, N_HID))
    return np.concatenate([w1.ravel(), np.zeros(N_HID), w2.ravel(), np.zeros(N_OUT)])


class XorShift32:
    """The batch sampler LiveTrainer uses (Convergence.Core.Training), so the bake can replay the game's run."""

    def __init__(self, seed):
        self.state = seed & 0xFFFFFFFF

    def next(self):
        x = self.state
        x ^= (x << 13) & 0xFFFFFFFF
        x ^= x >> 17
        x ^= (x << 5) & 0xFFFFFFFF
        self.state = x
        return x


def live_subset(x, y, per_class, rng):
    """A balanced, shuffled subset; pixels quantized to bytes exactly as the game stores them."""
    idx = np.concatenate([np.sort(rng.choice(np.nonzero(y == d)[0], per_class, replace=False)) for d in range(10)])
    idx = idx[rng.permutation(len(idx))]
    q = np.round(x[idx] * 255.0).astype(np.uint8)
    return q, y[idx].astype(np.int64)


def live_run(theta0, tx, ty, vx, vy, lr, steps):
    """Replays the training game: xorshift batches with replacement, plain SGD. Returns (losses, accuracies per 10 steps)."""
    sampler = XorShift32(LIVE_SAMPLER_SEED)
    theta = theta0.copy()
    batch_losses, accuracies = [], []
    for step in range(1, steps + 1):
        i = np.array([sampler.next() % len(ty) for _ in range(LIVE_BATCH)])
        g, loss = gradient(theta, tx[i], ty[i])
        batch_losses.append(loss)
        theta = theta - lr * g
        if step % 10 == 0:
            _, p = forward(theta, vx)
            accuracies.append(float((p.argmax(axis=1) == vy).mean()))
    return np.array(batch_losses), np.array(accuracies)


def weights_json(theta):
    w1, b1, w2, b2 = unpack(theta)
    return {"w1": w1.ravel().tolist(), "b1": b1.tolist(), "w2": w2.ravel().tolist(), "b2": b2.tolist()}


# ---------------------------------------------------------------- the neuron puzzle (mirrors Core.Puzzles.WeightPaintPuzzle)

def puzzle_eval(weights, digits, should_fire, threshold):
    sums = digits @ weights
    fired = sums >= threshold
    correct = fired == should_fire
    margins = np.where(should_fire, sums - threshold, threshold - sums)
    return int(correct.sum()), float(margins.sum()), sums


def puzzle_hint(weights, digits, should_fire, threshold):
    """The one cell change that most improves the number correct; tie-break: biggest total margin, then lowest
    cell index, then +1 before -1 before 0. None when solved."""
    correct, margin, _ = puzzle_eval(weights, digits, should_fire, threshold)
    if correct == len(digits):
        return None
    best = None
    for cell in range(len(weights)):
        for value in (1, -1, 0):
            if weights[cell] == value:
                continue
            trial = weights.copy()
            trial[cell] = value
            c, m, _ = puzzle_eval(trial, digits, should_fire, threshold)
            key = (c, m)
            if best is None or key[0] > best[0][0] or (key[0] == best[0][0] and key[1] > best[0][1] + 1e-12):
                best = (key, cell, value)
    return best[1], best[2]


def solve_by_hints(digits, should_fire, threshold, limit=49):
    weights = np.zeros(49)
    for move in range(limit + 1):
        hint = puzzle_hint(weights, digits, should_fire, threshold)
        if hint is None:
            return move, weights
        weights[hint[0]] = hint[1]
    return None, weights


def reference_solution(train_x, train_y, plus_cut=PUZZLE_CUT):
    """+ where the average training 3 has clearly more ink than the average of the other puzzle digits, - where it has
    clearly less, 0 elsewhere: the 3's top bar, middle bump and bottom curve green, its open left side red."""
    diff = train_x[train_y == 3].mean(axis=0) - np.mean([train_x[train_y == d].mean(axis=0) for d in PUZZLE_OTHERS], axis=0)
    return np.where(diff > plus_cut, 1.0, np.where(diff < -PUZZLE_CUT, -1.0, 0.0))


# ---------------------------------------------------------------- bake

def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True)
    args = parser.parse_args()

    paths = fetch()
    train_raw = read_images(paths["train_images"])
    train_y = read_labels(paths["train_labels"])
    test_raw = read_images(paths["test_images"])
    test_y = read_labels(paths["test_labels"])
    check(len(train_y) == 60000 and len(test_y) == 10000, "unexpected MNIST sizes")
    train_x = pool7(train_raw)
    test_x = pool7(test_raw)

    # ---- training: one too-big step, then plain full-batch gradient descent
    theta0 = np.round(init_theta(SEED_INIT), W_DEC)
    trajectory = [theta0.copy()]
    losses = []
    theta = theta0.copy()
    g, loss = gradient(theta, train_x, train_y)
    losses.append(loss)
    theta = theta - LR_FIRST * g
    trajectory.append(theta.copy())
    for _ in range(STEPS):
        g, loss = gradient(theta, train_x, train_y)
        losses.append(loss)
        theta = theta - LR * g
        trajectory.append(theta.copy())
    losses.append(loss_of(theta, train_x, train_y))
    losses = np.array(losses)
    trajectory = np.array(trajectory)
    theta_trained = np.round(theta, W_DEC)
    trajectory[-1] = theta_trained

    check(losses[1] > losses[0], f"step 1 did not overshoot: loss {losses[0]:.4f} -> {losses[1]:.4f}")
    rises = np.nonzero(np.diff(losses[1:]) > 1e-12)[0]
    check(len(rises) == 0, f"loss rose after step 1 at steps {rises[:5] + 1}; lower LR")

    _, p_test = forward(theta_trained, test_x)
    test_pred = p_test.argmax(axis=1)
    test_accuracy = float((test_pred == test_y).mean())
    _, p_train = forward(theta_trained, train_x)
    train_accuracy = float((p_train.argmax(axis=1) == train_y).mean())
    check(0.80 <= test_accuracy <= 0.98, f"test accuracy {test_accuracy:.4f} out of the expected range")

    # ---- hero 3: trained net sure it's a 3 (> 0.8), untrained net says something else
    _, p_untrained_test = forward(theta0, test_x)
    threes = np.nonzero(test_y == 3)[0]
    ok = [i for i in threes if p_test[i, 3] > 0.8 and p_untrained_test[i].argmax() != 3]
    check(len(ok) > 0, "no test 3 the trained net gets right and the untrained net gets wrong")
    ink = test_x[ok].sum(axis=1)
    typical = np.median(test_x[threes].sum(axis=1))
    # the most confident clear 3 with a typical amount of ink (not a blob, not a sliver)
    scored = sorted(ok, key=lambda i: (-int(p_test[i, 3] > 0.97),abs(test_x[i].sum() - typical), i))
    hero = int(scored[0])
    hero_x = test_x[hero]
    h_untrained, p_untrained = forward(theta0, hero_x[None, :])
    h_trained, p_trained = forward(theta_trained, hero_x[None, :])
    untrained_guess = int(p_untrained[0].argmax())
    check(untrained_guess != 3 and int(p_trained[0].argmax()) == 3, "hero checks failed")

    # ---- the dock's gallery: real test threes, all different (thin, thick, slanted), never the hero.
    # Ranked by ink and taken at evenly spaced ranks from the 5th to the 95th percentile, so it is deterministic and
    # shows the spread of how people write a 3.
    others = [int(i) for i in threes if int(i) != hero]
    by_ink = sorted(others, key=lambda i: (float(test_raw[i].sum()), i))
    ranks = np.linspace(0.05, 0.95, GALLERY_COUNT) * (len(by_ink) - 1)
    gallery_idx = [by_ink[int(round(r))] for r in ranks]
    check(len(set(gallery_idx)) == GALLERY_COUNT, "gallery threes repeat")
    gallery = [{"testIndex": i, "label": 3,
                "pixels28": np.round(test_raw[i].astype(np.float64).ravel() / 255.0, GALLERY_DEC).tolist()}
               for i in gallery_idx]

    # ---- the travel game's targets (blast the 3s, v7): clear real test digits, threes and look-alikes, 28x28.
    # Clear = the trained network reads it with p > 0.9 (a stand-in for "a person reads it at a glance"); within each
    # label, evenly spaced ink ranks, so thin and thick ones both fly. Never the hero or a gallery three.
    used = set(gallery_idx) | {hero}
    targets = []
    for label, count in [(3, TARGET_THREES)] + [(d, TARGET_PER_OTHER) for d in TARGET_OTHERS]:
        pool = [int(i) for i in np.nonzero(test_y == label)[0]
                if int(i) not in used and p_test[i, label] > 0.9 and int(p_test[i].argmax()) == label]
        pool.sort(key=lambda i: (float(test_raw[i].sum()), i))
        picks = [pool[int(round(r))] for r in np.linspace(0.15, 0.85, count) * (len(pool) - 1)]
        check(len(set(picks)) == count, f"target {label}s repeat")
        targets += [{"testIndex": i, "label": label,
                     "pixels28": np.round(test_raw[i].astype(np.float64).ravel() / 255.0, GALLERY_DEC).tolist()}
                    for i in picks]

    # ---- neuron puzzle: 4 clear threes, one clear 0, 1, 7 and 4, a line the reference clears with margin
    ref = reference_solution(train_x, train_y)
    threes_all = np.nonzero(test_y == 3)[0]
    three_scores = test_x[threes_all] @ ref
    # the clearest examples, but not the extremes: take ranks spread through the clear half (never the hero)
    threes_sorted = [int(i) for i in threes_all[np.argsort(-three_scores)] if int(i) != hero]
    pick_threes = [threes_sorted[k] for k in (40, 120, 200, 280)]
    pick_others = []
    for d in PUZZLE_OTHERS:
        idx = np.nonzero(test_y == d)[0]
        pick_others.append(int(idx[np.argsort(test_x[idx] @ ref)][120]))
    puzzle_idx = pick_threes + pick_others
    should_fire = np.array([True] * 4 + [False] * 4)
    puzzle_x = test_x[puzzle_idx]
    T = PUZZLE_THRESHOLD
    c_empty, _, _ = puzzle_eval(np.zeros(49), puzzle_x, should_fire, T)
    c_plus, _, _ = puzzle_eval(np.ones(49), puzzle_x, should_fire, T)
    # The digits are picked with the ranking pattern above (unchanged, so the same eight digits); the reference that must
    # solve the puzzle is the fuller one.
    ref = reference_solution(train_x, train_y, PUZZLE_SHOWN_PLUS_CUT)
    c_ref, _, ref_sums = puzzle_eval(ref, puzzle_x, should_fire, T)
    check(c_empty < 8, "the empty grid already solves the puzzle")
    check(c_plus < 8, "painting everything + already solves the puzzle")
    check(c_ref == 8, f"the reference solution fails: sums {np.round(ref_sums, 2)} vs line {T}")
    ref_margin = float(np.min(np.where(should_fire, ref_sums - T, T - ref_sums)))
    check(ref_margin >= 0.3, f"reference margin {ref_margin:.2f} too thin")
    moves, _ = solve_by_hints(puzzle_x, should_fire, T)
    check(moves is not None and moves <= 49, "following hints from the empty grid does not solve it in 49 moves")

    # ---- the Valley: a ribbon of real loss that follows the real training path.
    # A flat 2-D slice (the PCA plane) misplaces this run by up to ~2 loss units (the path curves through 970
    # dimensions), so the track would not sit at the real loss. Instead row r of the ribbon is a real point on the
    # path (straight along the one big step for hop 1, then the recorded steps), and column c moves sideways from it
    # along one fixed random direction. The middle column is exactly the training loss; the walls are real losses
    # beside the path.
    hop_index = [0] + HOP_STEPS
    check(hop_index[-1] == len(trajectory) - 1, "last hop must be the trained weights")
    rng_wall = np.random.default_rng(SEED_WALL)
    first = trajectory[1] - trajectory[0]
    longway = trajectory[0] - trajectory[-1]
    side = rng_wall.normal(size=N_PARAMS)
    for d in (longway, first):
        d = d / np.linalg.norm(d)
        side -= (side @ d) * d
    side /= np.linalg.norm(side)
    rows_theta, rows_step, rows_hop = [], [], []
    for k in range(1, len(hop_index)):
        a, b_ = hop_index[k - 1], hop_index[k]
        if b_ - a == 1:
            # one real step: the straight line between its two weight vectors, sampled finely
            for u in np.linspace(0.0, 1.0, SAMPLES_PER_HOP + 1)[:-1]:
                rows_theta.append(trajectory[a] + u * (trajectory[b_] - trajectory[a]))
                rows_step.append(a + u)
                rows_hop.append(k - 1 + u)
        else:
            # recorded steps, placed by distance travelled in weight space inside the hop
            steps = np.arange(a, b_ + 1)
            seglen = np.linalg.norm(np.diff(trajectory[a:b_ + 1], axis=0), axis=1)
            cum = np.concatenate([[0.0], np.cumsum(seglen)]) / max(seglen.sum(), 1e-12)
            for u in np.linspace(0.0, 1.0, SAMPLES_PER_HOP + 1)[:-1]:
                i = int(np.searchsorted(cum, u, side="right") - 1)
                i = min(i, len(steps) - 2)
                w = (u - cum[i]) / max(cum[i + 1] - cum[i], 1e-12)
                rows_theta.append(trajectory[steps[i]] + w * (trajectory[steps[i + 1]] - trajectory[steps[i]]))
                rows_step.append(steps[i] + w)
                rows_hop.append(k - 1 + u)
    rows_theta.append(trajectory[-1])
    rows_step.append(float(hop_index[-1]))
    rows_hop.append(float(len(hop_index) - 1))
    offsets = np.linspace(-WALL_HALF_WIDTH, WALL_HALF_WIDTH, WALL_SAMPLES)
    surface = np.zeros((len(rows_theta), WALL_SAMPLES))
    for r, th in enumerate(rows_theta):
        for c, o in enumerate(offsets):
            surface[r, c] = loss_of(th + o * side, train_x, train_y)
    mid = WALL_SAMPLES // 2
    hops = []
    for k, t in enumerate(hop_index):
        row = k * SAMPLES_PER_HOP
        check(abs(surface[row, mid] - losses[t]) < 1e-9, f"ribbon row {row} is not the real loss of step {t}")
        hops.append({"step": int(t), "realSteps": int(t - hop_index[k - 1]) if k > 0 else 0, "row": int(row),
                     "loss": round(float(losses[t]), 6)})
    seg = surface[0:SAMPLES_PER_HOP + 1, mid]
    dip = float(seg.min())
    check(dip < min(seg[0], seg[-1]) - 0.05,
          f"the big first step does not cross lower ground ({seg[0]:.3f} -> min {dip:.3f} -> {seg[-1]:.3f})")
    centre = surface[SAMPLES_PER_HOP:, mid]
    check(np.all(np.diff(centre) < 1e-9), "the ribbon centre rises after the overshoot")
    walls = np.minimum(surface[:, 0], surface[:, -1]) - surface[:, mid]
    wall_rise = (float(walls.min()), float(np.median(walls)), float(walls.max()))

    # ---- finale: 60 test digits through the trained net
    rng = np.random.default_rng(SEED_FINALE)
    finale_idx = sorted(rng.choice(10000, size=FINALE_COUNT, replace=False).tolist())
    finale = []
    for i in finale_idx:
        finale.append({"testIndex": int(i), "label": int(test_y[i]), "prediction": int(test_pred[i]),
                       "confidence": round(float(p_test[i].max()), 6), "pixels7": test_x[i].tolist()})
    finale_right = sum(1 for d in finale if d["label"] == d["prediction"])

    # ---- live training (v6): the subsets the rider trains on and is tested on, and a replay of each button
    rng_live = np.random.default_rng(LIVE_SEED)
    live_tq, live_ty = live_subset(train_x, train_y, LIVE_TRAIN_PER_CLASS, rng_live)
    live_vq, live_vy = live_subset(test_x, test_y, LIVE_TEST_PER_CLASS, rng_live)
    live_tx, live_vx = live_tq.astype(np.float64) / 255.0, live_vq.astype(np.float64) / 255.0
    start_loss_live = loss_of(theta0, live_tx, live_ty)
    tiny_l, tiny_a = live_run(theta0, live_tx, live_ty, live_vx, live_vy, LIVE_RATES[0], 300)
    good_l, good_a = live_run(theta0, live_tx, live_ty, live_vx, live_vy, LIVE_RATES[1], LIVE_MAX_STEPS)
    huge_l, huge_a = live_run(theta0, live_tx, live_ty, live_vx, live_vy, LIVE_RATES[2], 60)
    reached = np.nonzero(good_a >= LIVE_TARGET)[0]
    check(len(reached) > 0, f"the good step size never reaches {LIVE_TARGET:.0%} in {LIVE_MAX_STEPS} steps")
    steps_to_target = int((reached[0] + 1) * 10)
    check(steps_to_target <= 600, f"the good step size takes {steps_to_target} steps to {LIVE_TARGET:.0%}")
    check(tiny_a[-1] < 0.5, f"the tiny step size is not slow: {tiny_a[-1]:.0%} after 300 steps")
    huge_peak = float(huge_l[:20].max())
    check(huge_peak > 2.5 * start_loss_live, f"the huge step size does not blow up (peak loss {huge_peak:.2f})")
    check(huge_a[-1] < 0.3, f"the huge step size still learns: {huge_a[-1]:.0%} after 60 steps")

    out = {
        "meta": {
            "version": 1,
            "source": "MNIST (60,000 training + 10,000 test handwritten digits), " + MIRROR,
            "licence": "MNIST by Yann LeCun and Corinna Cortes, CC BY-SA 3.0",
            "generator": "ArtSource/training/bake_level1.py",
            "numpyVersion": np.__version__,
            "date": datetime.date.today().isoformat(),
            "seedInit": SEED_INIT, "seedFinale": SEED_FINALE, "seedWall": SEED_WALL,
            "notes": "Input: 28x28 averaged in 4x4 blocks to 7x7, 0 = paper, 1 = ink. Hidden: sigmoid. Output: "
                     "softmax, cross-entropy loss. Training: full-batch gradient descent on all 60,000 training "
                     "digits; step 1 uses firstStepLearningRate (too big on purpose, the loss rises), then "
                     "learningRate. Valley: loss over the plane of the top-2 principal directions of the weight "
                     "trajectory is replaced by a ribbon: each row is a real point on the training path (rowStep), each "
                     "column steps sideways along one fixed random direction (seed seedWall), loss on all 60,000 "
                     "training digits; the middle column is exactly the training loss.",
        },
        "inputSize": N_IN, "hiddenSize": N_HID, "outputSize": N_OUT,
        "trainCount": int(len(train_y)), "testCount": int(len(test_y)),
        "testAccuracy": round(test_accuracy, 6), "trainAccuracy": round(train_accuracy, 6),
        "learningRate": LR, "firstStepLearningRate": LR_FIRST, "totalSteps": int(len(trajectory) - 1),
        "initial": weights_json(theta0),
        "trained": weights_json(theta_trained),
        "hero": {
            "testIndex": hero, "label": 3,
            "pixels28": np.round(test_raw[hero].astype(np.float64).ravel() / 255.0, PX_DEC).tolist(),
            "pixels7": hero_x.tolist(),
            "untrainedGuess": untrained_guess,
            "untrainedHidden": h_untrained[0].tolist(), "untrainedOutputs": p_untrained[0].tolist(),
            "trainedHidden": h_trained[0].tolist(), "trainedOutputs": p_trained[0].tolist(),
        },
        "puzzle": {
            "threshold": T,
            "digits": [{"testIndex": int(i), "label": int(test_y[i]), "shouldFire": bool(f),
                        "pixels7": test_x[i].tolist()} for i, f in zip(puzzle_idx, should_fire)],
            "referenceSolution": [int(v) for v in ref],
            "hintMovesFromEmpty": int(moves),
        },
        "valley": {
            "rows": len(rows_theta), "columns": WALL_SAMPLES, "samplesPerHop": SAMPLES_PER_HOP,
            "wallHalfWidth": WALL_HALF_WIDTH,
            "loss": np.round(surface.ravel(), 6).tolist(),
            "rowStep": [round(float(s_), 4) for s_ in rows_step],
            "hops": hops,
        },
        "finale": {"digits": finale, "correct": finale_right},
        "threes": gallery,
        "targets": targets,
        "live": {
            "batch": LIVE_BATCH, "rates": LIVE_RATES, "targetAccuracy": LIVE_TARGET,
            "samplerSeed": LIVE_SAMPLER_SEED, "dataSeed": LIVE_SEED,
            "trainPixels": base64.b64encode(live_tq.tobytes()).decode("ascii"),
            "trainLabels": live_ty.tolist(),
            "testPixels": base64.b64encode(live_vq.tobytes()).decode("ascii"),
            "testLabels": live_vy.tolist(),
            "startLoss": round(start_loss_live, 6),
            "stepsToTarget": steps_to_target,
            "goodAccuracyAt300": round(float(good_a[29]), 6),
            "tinyAccuracyAt300": round(float(tiny_a[-1]), 6),
            "hugePeakLoss": round(huge_peak, 6),
        },
    }
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, separators=(",", ":"))

    probs = " ".join(f"{v:.2f}" for v in p_untrained[0])
    print(f"wrote {args.out} ({os.path.getsize(args.out) / 1024:.0f} KB)")
    print("| check | value |")
    print("|---|---|")
    print(f"| test accuracy (10,000 unseen digits) | {test_accuracy * 100:.2f} % |")
    print(f"| train accuracy | {train_accuracy * 100:.2f} % |")
    print(f"| training | {len(trajectory) - 1} full-batch steps on {len(train_y)} digits, lr {LR_FIRST} then {LR} |")
    print(f"| loss: start / after step 1 / end | {losses[0]:.4f} / {losses[1]:.4f} (overshoot +{losses[1] - losses[0]:.4f}) / {losses[-1]:.4f} |")
    print(f"| hero 3 | test #{hero}; untrained guess {untrained_guess} ({p_untrained[0].max():.2f}); trained p(3) {p_trained[0, 3]:.4f} |")
    print(f"| untrained outputs | {probs} |")
    print(f"| puzzle | line {T}; empty {c_empty}/8, all-plus {c_plus}/8, reference 8/8 (margin {ref_margin:.2f}); hints solve from empty in {moves} moves |")
    print(f"| valley ribbon | {len(rows_theta)} rows x {WALL_SAMPLES} columns of real loss; centre = training loss exactly |")
    print(f"| valley walls (side min - centre) | min {wall_rise[0]:+.3f}, median {wall_rise[1]:+.3f}, max {wall_rise[2]:+.3f} |")
    print(f"| big first step | {seg[0]:.3f} -> dips to {dip:.3f} -> climbs to {seg[-1]:.3f} |")
    print(f"| finale | {finale_right}/{FINALE_COUNT} right |")
    print(f"| live | {len(live_ty)} train / {len(live_vy)} test digits; start loss {start_loss_live:.3f}; good lr {LIVE_RATES[1]} hits "
          f"{LIVE_TARGET:.0%} at step {steps_to_target} ({good_a[29]:.0%} at 300); tiny {tiny_a[-1]:.0%} at 300; huge peak loss {huge_peak:.1f}, {huge_a[-1]:.0%} at 60 |")
    print(f"| gallery | {GALLERY_COUNT} real test threes (28x28): " + ", ".join(f"#{g['testIndex']}" for g in gallery) + " |")
    print("| hops (step:real steps:loss) | " +
          "; ".join(f"{h['step']}:{h['realSteps']}:{h['loss']:.3f}" for h in hops) + " |")


if __name__ == "__main__":
    try:
        main()
    except BakeError as error:
        print(f"BAKE FAILED: {error}", file=sys.stderr)
        sys.exit(1)
