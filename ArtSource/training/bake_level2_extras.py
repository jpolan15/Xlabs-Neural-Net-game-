"""Add two v3 sections to Level 2's bake, read straight from GPT-2 small's own files (no PyTorch needed).

    uv run --with numpy --with safetensors python -I ArtSource/training/bake_level2_extras.py Assets/_Project/Data/Level2/level2_words.json

* "merges": how GPT-2's tokenizer decides where a word is chopped. Before training, its byte-pair encoder read a lot of text and wrote
  an ordered glue list (merges.txt, 50,000 rules): the pair seen side by side most often first. Chopping a word replays that list: glue
  the pair that comes earliest in the list, again and again, until no pair of pieces has a rule. Each step is baked with its rule number
  (rank + 1), for the words the tokens stop shows. Words are spelled as at the start of a sentence (no leading space), so the pieces are
  the rider's letters; the ones picked chop the same way with a space in front (checked below).
* "analogyNumbers": the king - man + woman sum done on GPT-2's real token embeddings (wte, 768 numbers a token), shown on eight of the
  768 columns: the eight where woman and man differ most, so the change the sum makes is visible. Also queen's own numbers there.

Run it after bake_level2.py (which writes the rest of the file); it rewrites only these two keys. GPT-2: Radford et al. 2019, MIT.
The model files come from the Hugging Face cache bake_level2.py fills (models--gpt2).
"""

import glob
import json
import os
import sys

import numpy as np
from safetensors.numpy import load_file

MERGE_WORDS = ["the", "chatbot"]
ANALOGY = ("king", "man", "woman", "queen")
COLUMNS = 8


def snapshot():
    roots = [os.path.expanduser("~/.cache/huggingface/hub/models--gpt2/snapshots/*/")]
    for pattern in roots:
        found = sorted(glob.glob(pattern))
        if found:
            return found[-1]
    sys.exit("GPT-2 is not in the Hugging Face cache. Run bake_level2.py once (it downloads it).")


def glue_steps(word, rank):
    parts = list(word)
    steps = [{"pieces": list(parts), "rule": 0}]
    while len(parts) > 1:
        best = min(((rank.get((parts[i], parts[i + 1]), None), i) for i in range(len(parts) - 1)),
                   key=lambda t: float("inf") if t[0] is None else t[0])
        if best[0] is None:
            break
        a, b = parts[best[1]], parts[best[1] + 1]
        glued, j = [], 0
        while j < len(parts):
            if j < len(parts) - 1 and parts[j] == a and parts[j + 1] == b:
                glued.append(a + b)
                j += 2
            else:
                glued.append(parts[j])
                j += 1
        parts = glued
        steps.append({"pieces": list(parts), "rule": best[0] + 1})
    return steps


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    path = sys.argv[1]
    snap = snapshot()
    lines = open(os.path.join(snap, "merges.txt"), encoding="utf-8").read().split("\n")[1:]
    merges = [tuple(l.split()) for l in lines if l.strip()]
    rank = {m: i for i, m in enumerate(merges)}
    vocab = json.load(open(os.path.join(snap, "vocab.json"), encoding="utf-8"))
    if len(merges) != 50000:
        sys.exit("expected 50,000 glue rules, found %d" % len(merges))

    out = []
    for w in MERGE_WORDS:
        steps = glue_steps(w, rank)
        final = steps[-1]["pieces"]
        spaced = glue_steps("Ġ" + w, rank)[-1]["pieces"]
        if [p.lstrip("Ġ") for p in spaced] != final:
            sys.exit("%s chops differently with a space in front: %s vs %s" % (w, spaced, final))
        for p in final:
            if p not in vocab:
                sys.exit("%s: piece %r is not a GPT-2 token" % (w, p))
        out.append({"word": w, "steps": steps, "ids": [vocab[p] for p in final]})
        print("  %-8s %s" % (w, " -> ".join("|".join(s["pieces"]) + ("" if s["rule"] == 0 else " (#%d)" % s["rule"]) for s in steps)))

    wte = load_file(os.path.join(snap, "model.safetensors"))
    wte = wte.get("wte.weight", wte.get("transformer.wte.weight")).astype(np.float64)
    ids = {w: vocab["Ġ" + w] for w in ANALOGY}
    v = {w: wte[i] for w, i in ids.items()}
    result = v["king"] - v["man"] + v["woman"]
    cols = [int(c) for c in np.argsort(-np.abs(v["woman"] - v["man"]))[:COLUMNS]]
    cols.sort()
    unit = wte / np.linalg.norm(wte, axis=1, keepdims=True)
    sims = unit @ (result / np.linalg.norm(result))
    for w in ANALOGY[:3]:
        sims[ids[w]] = -1
    best = int(np.argmax(sims))
    if best != ids["queen"]:
        sys.exit("the nearest word is not queen")
    row = lambda x: [round(float(x[c]), 3) for c in cols]
    numbers = {
        "columns": cols,
        "king": row(v["king"]), "man": row(v["man"]), "woman": row(v["woman"]),
        "result": row(result), "queen": row(v["queen"]),
        "queenSimilarity": round(float(sims[ids["queen"]]), 3),
    }
    print("  columns", cols)
    for k in ("king", "man", "woman", "result", "queen"):
        print("  %-7s %s" % (k, numbers[k]))

    data = json.load(open(path, encoding="utf-8"))
    data["merges"] = out
    data["analogyNumbers"] = numbers
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, indent=1)
        f.write("\n")
    print("wrote", path)


if __name__ == "__main__":
    main()
