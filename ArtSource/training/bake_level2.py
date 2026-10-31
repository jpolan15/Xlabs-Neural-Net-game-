"""Bake Level 2's language-model data from a real GPT-2 (124M).

Everything the Word Machine shows as a number comes from this file's output: how GPT-2 chops sentences into tokens,
where its own token embeddings put words (squashed from 768 numbers to 3), which words it expects next and how sure it
is, and the branching road of the story game (every fork's real probabilities). Level 2 never invents a probability.

Run (CPU is enough; the first run downloads GPT-2, about 550 MB, into the Hugging Face cache):
    uv run --with torch --with transformers --with numpy python -I ArtSource/training/bake_level2.py explore
    uv run --with torch --with transformers --with numpy python -I ArtSource/training/bake_level2.py bake --out Assets/_Project/Data/Level2/level2_words.json

`explore` prints what the model says for every candidate in CONFIG so the content can be picked by eye; `bake` writes
the JSON the game reads (Convergence.Gameplay.Ride.Level2.Level2Data). GPT-2: Radford et al. 2019, MIT license.
The text of the prompts is ours. Every text gets <|endoftext|> in front, the way GPT-2 starts a document.
"""

import argparse
import datetime
import json
import math
import os
import sys

import numpy as np
import torch
from transformers import GPT2LMHeadModel, GPT2TokenizerFast

HERE = os.path.dirname(os.path.abspath(__file__))

# ---------------------------------------------------------------------------------------------------- the content
# Words the slicer game can serve (mid-sentence spelling, so each starts with a space inside the model).
SLICER_WORDS = [
    "cat", "the", "pizza", "robot", "happy", "moon", "dog", "tree",                        # likely whole
    "unbelievable", "tokenization", "strawberry", "astronaut", "rollercoaster", "microscope", "jellyfish",
    "hamburger", "skateboard", "dinosaur", "refrigerator", "unhappiness", "watermelon", "spaceship", "chatbot",
    "supercalifragilisticexpialidocious", "Pixel", "neural", "backpropagation", "marshmallow", "toothbrush",
]

# The sentence the whole level rides on.
THREAD = "Because the cat was hungry, it chased the mouse."

CATEGORIES = {
    "animals": ["cat", "dog", "horse", "lion", "rabbit", "tiger", "bird", "fish", "bear", "mouse"],
    "food": ["pizza", "bread", "cheese", "apple", "banana", "soup", "rice", "cake", "pasta", "cookie"],
    "vehicles": ["car", "bus", "train", "plane", "bike", "truck", "boat", "ship", "rocket", "taxi"],
    "feelings": ["happy", "sad", "angry", "scared", "proud", "tired", "calm", "excited", "worried", "lonely"],
    "places": ["city", "forest", "beach", "school", "castle", "river", "mountain", "village", "desert", "island"],
}
WORDS_PER_CATEGORY = 8

ANALOGIES = [("king", "man", "woman"), ("walking", "walk", "swim")]

PROMPTS = [
    "Once upon a", "Peanut butter and", "Twinkle, twinkle, little", "The capital of France is", "My favorite color is",
    "The cat chased the mouse because it was", "I opened the fridge and grabbed a glass of", "Happy birthday to",
    "Roses are red, violets are", "Today I want to eat a big", "The astronaut looked out the window and saw the",
    "In the morning I drink a cup of", "A dog says woof and a cat says", "The sky is", "Once upon a time there was a little",
    "She put on her shoes and went to the", "Knock, knock. Who's", "One, two, three, four,", "I love to play video",
    "The opposite of hot is", "To be, or not to", "The quick brown fox jumps over the lazy",
]

PREDICT_ROUNDS = [
    ("Once upon a", " time"),
    ("Peanut butter and", " jelly"),
    ("In the morning I drink a cup of", " tea"),
    ("Roses are red, violets are", " blue"),
    ("I opened the fridge and grabbed a glass of", " milk"),
]
EXPLAIN_PROMPT = ("The cat chased the mouse because it was", " hungry")

ATTENTION_ROUNDS = [
    # (sentence, query word, the words that matter)
    ("The tiny green dragon ate the pizza", "dragon", ["tiny", "green"]),
    ("The dog chased the cat, then it climbed a tree", "it", ["cat"]),
    ("She poured milk into the glass and drank it", "it", ["milk"]),
    ("The fluffy blue creature walked the quiet forest", "creature", ["fluffy", "blue"]),
]

STORY_STARTS = [
    "My robot loves to", "Pixel the robot wants to", "The robot's favorite food is", "My dog loves to", "Once upon a time, a little",
    "When the robot got home, it", "The little dragon wanted to", "The astronaut opened the door and", "On Saturday, my best friend and I",
    "The cat looked at the robot and",
]
# Never offered on a lane: the road is played by kids. Checked again by eye on every word of the baked tree.
BLOCKED = {
    "cum", "sex", "sexy", "porn", "fuck", "fucking", "shit", "damn", "hell", "ass", "bitch", "nude", "naked", "rape", "raped", "kill",
    "killed", "killing", "die", "died", "dead", "death", "blood", "gun", "guns", "drug", "drugs", "drunk", "beer", "wine", "vodka",
    "suicide", "slave", "slaves", "nazi", "hitler", "terror", "terrorist", "bomb", "murder", "cock", "dick", "pussy", "penis",
    "vagina", "boobs", "breast", "breasts", "stupid", "idiot", "hate", "hated", "gay", "lesbian", "jesus", "god", "satan", "devil",
    "cocaine", "weed", "marijuana", "stripper", "orgasm", "fetish", "gang", "shot", "shoot", "shooting", "weapon", "weapons",
    "violence", "violent", "abuse", "abused", "hurt", "pain", "cancer", "sick", "crap", "bastard", "slut", "whore", "fat", "ugly",
}
STORY_START = "My robot loves to"
STORY_STOP = {" the", " a", " an", " of", " to", " and", " in", " is", " was", " that", " it", " for", " on", " with", " as",
              " at", " by", " from", " his", " her", " their", " its", " this", " these", " be", " are", " were", " had", " has"}
STORY_LANE_RANKS = [0, 1, 3, 9]       # among the word candidates: the safe pick, the next, a spicy one, a wild one
STORY_FORKS = 3
TEMPERATURES = [0.2, 0.5, 1.0, 1.5, 2.5]


def load():
    torch.set_grad_enabled(False)
    tok = GPT2TokenizerFast.from_pretrained("gpt2")
    model = GPT2LMHeadModel.from_pretrained("gpt2", attn_implementation="eager")
    model.eval()
    return tok, model


def encode(tok, text):
    ids = [tok.eos_token_id] + tok.encode(text)
    return torch.tensor([ids])


def pieces(tok, text):
    ids = tok.encode(text)
    return [tok.decode([i]) for i in ids], ids


def next_probs(tok, model, text, temperature=1.0):
    logits = model(encode(tok, text)).logits[0, -1].double()
    return torch.softmax(logits / temperature, dim=-1)


def top(tok, probs, k):
    values, ids = torch.topk(probs, k)
    return [(tok.decode([int(i)]), float(v), int(i)) for v, i in zip(values, ids)]


def is_word(piece):
    body = piece[1:]
    return piece.startswith(" ") and len(body) >= 2 and body.isalpha() and body.islower()


# ---------------------------------------------------------------------------------------------------- explore
def explore(tok, model):
    print("== tokens of the slicer words")
    for w in SLICER_WORDS:
        p, ids = pieces(tok, " " + w)
        print("  %-36s %d  %s" % (w, len(p), p))
    p, ids = pieces(tok, THREAD)
    print("== thread sentence:", p, ids)

    print("== next-word prompts")
    for prompt in PROMPTS:
        probs = next_probs(tok, model, prompt)
        entropy = float(-(probs * torch.log2(probs.clamp_min(1e-12))).sum())
        print("  %-50s H=%.2f bits  %s" % (prompt, entropy, "  ".join("%s %.1f%%" % (t, 100 * v) for t, v, _ in top(tok, probs, 8))))

    print("== embeddings")
    embeds, words, cats = embedding_table(tok, model)
    xyz, explained = pca3(embeds)
    quality(embeds, xyz, cats)
    print("  PCA explained variance of 3 axes:", explained)
    for a, b, c in ANALOGIES:
        print("  %s - %s + %s ->" % (a, b, c), analogy(tok, model, a, b, c))

    print("== attention heads")
    head_search(tok, model)

    print("== story starts")
    for start in STORY_STARTS:
        story_walk(tok, model, start, 0, "", show=True, limit=1)

    print("== story tree, first levels")
    story_walk(tok, model, STORY_START, 0, "")


# ---------------------------------------------------------------------------------------------------- embeddings
def embedding_table(tok, model):
    wte = model.transformer.wte.weight.detach().double().numpy()
    words, cats, vecs = [], [], []
    for cat, ws in CATEGORIES.items():
        kept = 0
        for w in ws:
            ids = tok.encode(" " + w)
            if len(ids) != 1:
                print("  (skipping %s: %d tokens)" % (w, len(ids)))
                continue
            if kept == WORDS_PER_CATEGORY:
                break
            kept += 1
            words.append(w)
            cats.append(cat)
            vecs.append(wte[ids[0]])
    return np.array(vecs), words, cats


def pca3(x):
    centered = x - x.mean(axis=0)
    u, s, vt = np.linalg.svd(centered, full_matrices=False)
    xyz = centered @ vt[:3].T
    explained = (s[:3] ** 2) / (s ** 2).sum()
    return xyz, [float(e) for e in explained]


def quality(full, xyz, cats):
    names = sorted(set(cats))
    for label, data in (("768-d", full), ("3-d  ", xyz)):
        centroids = {n: data[[i for i, c in enumerate(cats) if c == n]].mean(axis=0) for n in names}
        right = 0
        for i, c in enumerate(cats):
            best = min(names, key=lambda n: np.linalg.norm(data[i] - centroids[n]))
            right += best == c
        print("  nearest-centroid accuracy %s: %d/%d" % (label, right, len(cats)))


def analogy(tok, model, a, b, c, k=5):
    wte = model.transformer.wte.weight.detach().double()
    ids = {w: tok.encode(" " + w) for w in (a, b, c)}
    if any(len(v) != 1 for v in ids.values()):
        return "multi-token word"
    target = wte[ids[a][0]] - wte[ids[b][0]] + wte[ids[c][0]]
    sims = torch.nn.functional.cosine_similarity(wte, target[None, :], dim=-1)
    for w in (a, b, c):
        sims[ids[w][0]] = -1
    values, idx = torch.topk(sims, 40)
    out = []
    for v, i in zip(values, idx):
        piece = tok.decode([int(i)])
        if is_word(piece):
            out.append((piece.strip(), round(float(v), 3)))
        if len(out) == k:
            break
    return out


# ---------------------------------------------------------------------------------------------------- attention
def attention_for(tok, model, sentence):
    ids = encode(tok, sentence)
    out = model(ids, output_attentions=True)
    att = torch.stack([a[0] for a in out.attentions]).double()   # layers, heads, query, key
    toks = [tok.decode([int(i)]) for i in ids[0]]
    return att, toks


def word_index(toks, word):
    for i, t in enumerate(toks):
        if t.strip().lower() == word.lower():
            return i
    raise KeyError(word + " not found in " + str(toks))


def head_search(tok, model, show=True):
    layers, heads = model.config.n_layer, model.config.n_head
    score = np.zeros((layers, heads))
    detail = {}
    for sentence, query, answers in ATTENTION_ROUNDS:
        att, toks = attention_for(tok, model, sentence)
        q = word_index(toks, query)
        keys = [i for i in range(1, q)]                       # not the document-start token, not itself
        right = [word_index(toks, a) for a in answers]
        for l in range(layers):
            for h in range(heads):
                row = att[l, h, q, keys]
                row = row / row.sum()
                mass = float(sum(row[keys.index(r)] for r in right if r in keys))
                score[l, h] += mass / len(ATTENTION_ROUNDS)
                detail[(l, h, sentence)] = mass
    order = np.dstack(np.unravel_index(np.argsort(-score.ravel()), score.shape))[0][:6]
    if show:
        for l, h in order:
            print("  layer %2d head %2d  mean mass on the right words %.2f   %s" % (
                l, h, score[l, h], "  ".join("%.2f" % detail[(l, h, s)] for s, _, _ in ATTENTION_ROUNDS)))
    return [(int(l), int(h)) for l, h in order], score


# ---------------------------------------------------------------------------------------------------- the story road
def is_story_word(piece):
    body = piece[1:]
    return piece.startswith(" ") and len(body) >= 1 and body.isalpha() and body.lower() not in BLOCKED and (body.islower() or body.istitle())


def lane_candidates(tok, probs):
    words = []
    for piece, p, token_id in top(tok, probs, 600):
        if is_story_word(piece) and (len(piece) > 2 or piece in (" I", " a")):
            words.append((piece, p, token_id))
    lanes = []
    for rank in STORY_LANE_RANKS:
        if rank < len(words):
            lanes.append(words[rank])
    return lanes


def story_walk(tok, model, text, depth, path, nodes=None, show=True, limit=STORY_FORKS - 1):
    probs = next_probs(tok, model, text)
    lanes = lane_candidates(tok, probs)
    if nodes is not None:
        nodes.append({
            "path": path,
            "text": text,
            "lanes": [{"word": w.strip(), "p": round(p, 5), "id": i} for w, p, i in lanes],
        })
    if show and depth <= max(1, limit):
        print("  %s%-44s %s" % ("  " * depth, text, "  ".join("%s %.1f%%" % (w.strip(), 100 * p) for w, p, _ in lanes)))
    if depth + 1 >= STORY_FORKS or depth >= limit:
        return
    for k, (w, _, _) in enumerate(lanes):
        story_walk(tok, model, text + w, depth + 1, path + str(k), nodes, show, limit)


# ---------------------------------------------------------------------------------------------------- bake
def bake(tok, model, out):
    data = {"meta": {
        "model": "GPT-2 small (124M), Radford et al. 2019, MIT license",
        "vocab": int(model.config.vocab_size), "layers": int(model.config.n_layer), "heads": int(model.config.n_head),
        "dims": int(model.config.n_embd), "parameters": int(sum(p.numel() for p in model.parameters())),
        "date": datetime.date.today().isoformat(), "source": "ArtSource/training/bake_level2.py",
    }}

    p, ids = pieces(tok, THREAD)
    data["thread"] = {"text": THREAD, "tokens": [{"text": t, "id": i} for t, i in zip(p, ids)]}

    words = []
    for w in SLICER_WORDS:
        ps, wids = pieces(tok, " " + w)
        words.append({"word": w, "tokens": ps, "ids": wids})
    data["words"] = words

    embeds, names, cats = embedding_table(tok, model)
    xyz, explained = pca3(embeds)
    xyz = xyz / np.abs(xyz).max()
    kinds = sorted(set(cats))
    centroids = {k: xyz[[i for i, c in enumerate(cats) if c == k]].mean(axis=0) for k in kinds}
    keep = [i for i, c in enumerate(cats) if min(kinds, key=lambda k: np.linalg.norm(xyz[i] - centroids[k])) == c]
    print("galaxy keeps %d of %d words (the rest sit nearer another category in 3-d)" % (len(keep), len(cats)))
    centroids = {k: xyz[[i for i in keep if cats[i] == k]].mean(axis=0) for k in kinds}
    data["galaxy"] = {
        "explained": [round(e, 4) for e in explained],
        "centroids": [{"category": k, "x": round(float(v[0]), 4), "y": round(float(v[1]), 4), "z": round(float(v[2]), 4)} for k, v in centroids.items()],
        "words": [{"word": names[i], "category": cats[i], "x": round(float(xyz[i][0]), 4), "y": round(float(xyz[i][1]), 4), "z": round(float(xyz[i][2]), 4)}
                  for i in keep],
    }
    data["analogies"] = [{"a": a, "b": b, "c": c, "answers": [{"word": w, "similarity": s} for w, s in analogy(tok, model, a, b, c, 5)]} for a, b, c in ANALOGIES]

    wte = model.transformer.wte.weight.detach().double()
    neighbours = []
    for w in ("cat", "king"):
        i = tok.encode(" " + w)[0]
        sims = torch.nn.functional.cosine_similarity(wte, wte[i][None, :], dim=-1)
        sims[i] = -1
        values, idx = torch.topk(sims, 60)
        found = [{"word": tok.decode([int(j)]).strip(), "similarity": round(float(v), 3)} for v, j in zip(values, idx) if is_word(tok.decode([int(j)]))][:6]
        neighbours.append({"word": w, "nearest": found})
    data["neighbours"] = neighbours

    rounds = []
    heads, _ = head_search(tok, model, show=False)
    layer, head = heads[0]
    for sentence, query, answers in ATTENTION_ROUNDS:
        att, toks = attention_for(tok, model, sentence)
        q = word_index(toks, query)
        keys = list(range(1, q))
        row = att[layer, head, q, keys]
        row = (row / row.sum()).tolist()
        rounds.append({
            "sentence": sentence, "query": word_index(toks, query) - 1, "answers": [word_index(toks, a) - 1 for a in answers],
            "words": [t.strip() for t in toks[1:]], "weights": [round(x, 4) for x in row], "layer": layer, "head": head,
        })
    data["attention"] = rounds

    predict = []
    for prompt, truth in PREDICT_ROUNDS + [EXPLAIN_PROMPT]:
        probs = next_probs(tok, model, prompt)
        entropy = float(-(probs * torch.log2(probs.clamp_min(1e-12))).sum())
        truth_id = tok.encode(truth)[0]
        truth_p = float(probs[truth_id])
        rank = int((probs > probs[truth_id]).sum()) + 1
        predict.append({
            "prompt": prompt, "truth": truth, "truthP": round(truth_p, 5), "truthRank": rank, "entropy": round(entropy, 3),
            "top": [{"word": w, "p": round(v, 5), "id": i} for w, v, i in top(tok, probs, 12)],
        })
    data["predict"] = predict

    temps = []
    for t in TEMPERATURES:
        probs = next_probs(tok, model, STORY_START, t)
        pool = [(w, v, i) for w, v, i in top(tok, probs, 200) if is_story_word(w)][:6]
        temps.append({"temperature": t, "top": [{"word": w, "p": round(v, 5), "id": i} for w, v, i in pool]})
    data["temperature"] = {"prompt": STORY_START, "settings": temps}

    nodes = []
    story_walk(tok, model, STORY_START, 0, "", nodes, show=False)
    data["story"] = {"start": STORY_START, "forks": STORY_FORKS, "nodes": nodes}

    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, indent=1)
    print("wrote", out, os.path.getsize(out), "bytes;", len(nodes), "story nodes")


def main():
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("explore")
    sub.add_parser("stories")
    b = sub.add_parser("bake")
    b.add_argument("--out", required=True)
    args = parser.parse_args()
    tok, model = load()
    if args.command == "explore":
        explore(tok, model)
    elif args.command == "stories":
        for start in STORY_STARTS:
            story_walk(tok, model, start, 0, "", show=True, limit=2)
    else:
        bake(tok, model, args.out)


if __name__ == "__main__":
    main()
