---
name: asset-forge
description: Use when creating a new 3D model for Convergence from AI concept art (GPT Image or similar) — brief, multi-view turnaround, image-to-3D, Blender cleanup, Unity import — with mandatory verification gates that compare the in-Unity render against the concept. Not for levers or anything interactable (those stay builder-made).
---

# Asset Forge — concept art → verified Quest 2 model

The rule behind every step: **an asset ships only when it is proven to match its concept and to fit Quest 2.**
Generators often produce slop that looks fine as a thumbnail and melts up close. The gates exist to catch it.

## Roles (subagents in `.claude/agents/`)

| Step | Subagent | Writes to |
|---|---|---|
| 1 Brief + concept | `concept-artist` | `ArtSource/<id>/brief.md`, `concept/` |
| 2 Gate A, Gate B | `asset-verifier` (read-only judge) | `ArtSource/<id>/verify/` |
| 3 3D + cleanup | `mesh-smith` | `ArtSource/<id>/raw/`, `clean/` |
| 4 Import + renders | `unity-integrator` | `Assets/_Project/Models/<Id>/`, `Tools/Editor/NeuralRideBuilder.cs` |
| 5 Gate C | `vr-qa` | `ArtSource/<id>/verify/`, `Documentation/Design/captures/` |

Run them in order. A failed gate sends the work back one step. **Max 3 loops per gate, then stop and report to the human.**

## Folder contract

```
ArtSource/<asset_id>/            # snake_case, e.g. drone_repair
  brief.md                       # copy of ArtSource/_TEMPLATE/brief.md, filled in
  concept/  hero.png  sheet.png  prompt_hero.txt  prompt_sheet.txt
  views/    front.png left.png back.png right.png      # split from sheet, background removed
  raw/      <tool>_<n>.glb  settings.json             # untouched generator output
  clean/    <asset_id>.fbx                             # decimated, UV'd, scaled in meters, pivot at base
  verify/   gateA.md  turntable_<view>.png  side_by_side.png  gateB.md  seat_view.png  gateC.md
```
Only `clean/` output that passed Gate B is copied into `Assets/_Project/Models/<AssetId>/`.

## Step 1 — Brief and concept

- Fill in `brief.md`: purpose in the ride, real size in meters, distance it's seen from, triangle budget, palette from `RideTheme`, and the orange rule (never orange, because it isn't interactable).
- Prompt the **hero image** first: one 3/4 view, plain mid-grey background, soft even light, no text.
- Then the **turnaround sheet**: the same object in **front, left, back, right**, orthographic, same scale and lighting, on a plain background, nothing cropped. Put the hero image in as a reference.
- Generator:
  - With `OPENAI_API_KEY` set, use the OpenAI Images API (current `gpt-image-*` model). Write the exact model snapshot into `brief.md`.
  - Without a key, write `prompt_*.txt` and ask the human to generate in ChatGPT or Google AI Studio and save the files into `concept/`. Do not invent images.

## Gate A — concept consistency (asset-verifier)

PASS only if **all** of these hold:
1. The 4 views show the same object: proportions, part count, and details line up across views (check silhouettes side by side).
2. The views are orthographic-ish, centered, uncropped, on a plain background, with no text or watermark.
3. It matches the brief's palette and the orange rule, and the silhouette reads at the viewing distance.

Write `verify/gateA.md` with a per-item PASS/FAIL and the reason.

## Step 3 — 3D and cleanup (mesh-smith)

1. Split the sheet into `views/`. Remove backgrounds: `uv run --with rembg,pillow python Tools/ArtPipeline/split_views.py` (create the script on first use).
2. Image-to-3D:
   - **Hunyuan3D-2mv** shape locally (front/left/back, ~6 GB VRAM on the RTX 3060).
   - For texture, use a hosted service: the Hunyuan 3D web studio, or Tripo's free tier and its MCP.
   - Save to `raw/` with `settings.json` (tool, version, seed, license).
3. Blender headless: `blender -b -P Tools/ArtPipeline/blender_cleanup.py -- --in raw/x.glb --out clean/<id>.fbx --tris <budget>`.
   - Merge, decimate to budget, recalculate normals, fill holes, smart UV.
   - Scale to meters, pivot at base center, ≤ 2 materials, textures ≤ 1024².
   - Grade any AI texture to the palette. Never ship raw AI colors.

## Gate B — model vs concept (asset-verifier)

`unity-integrator` imports to `Assets/_Project/Models/<Id>/` and renders the model in Unity **from the same 4 angles as the concept**. Use an orthographic camera, plain background, plus an alpha mask per view, saved to `verify/turntable_<view>.png`. Then build `side_by_side.png` (concept left, render right).

PASS only if:
1. **Silhouette IoU ≥ 0.80** in every view (`Tools/ArtPipeline/silhouette_iou.py`: concept mask vs render mask, both normalized to bounding box).
2. Proportions and key details match (rubric in `gateB.md`).
3. Budgets hold: tris, materials, texture size. No holes, flipped normals, or floating bits.
4. Real-world scale matches the brief within ±10%.

## Gate C — in the game (vr-qa)

- The builder places the asset (never a hand edit). Re-run `Convergence > Build Neural Ride`.
- Capture the seat views (master plan section 8). The asset must look right at play distance and must not break sightlines.
- Scene totals stay ≤ 100 draw calls and ≤ 100k tris.
- **The human approves hero assets** by looking at `side_by_side.png` and `seat_view.png`.

## Never

- Make a generated mesh interactable, or orange.
- Put raw or concept files under `Assets/`.
- Skip a gate, loosen a threshold to pass, or "fix" a failure by changing the concept to match a bad mesh.
- Clone a real person's likeness into an asset or voice.
