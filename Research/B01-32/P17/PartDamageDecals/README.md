# B01-32 / PASS 17 — PART DAMAGE DECAL ATLAS
**PACKAGE_ID:** `B01-32-DAMAGE-DECAL-001`, version 1.0.0, art producer B01-32.
**Destination:** isolated visual research source only. No modifications to Codex canonical scripts, VFX001, #285 CHL or #286 PARTSIZE.

## NEW GAME-READY ART CONTENT
12 separate transparent 256×256 SVG animation frames for four visual states: clean (deliberately 100% transparent), abraded (short physical scratch silhouettes), fractured (bright branching fissure), critical (fracture + segmented outer pulse). Three phase frames per visual state; no runtime coupling to health, physics, rewards, damage, or player input. `manifest.json` gives atlas coordinates (3 columns ×4 rows) and phase duration (160ms).
`viewer.html` is a standalone 720/1080 browser preview on a synthetic plate with actual stage/frame buttons, playback and VFX OFF. The plate background is only viewer decoration; original art exports are transparent.

## EXECUTABLE OUTPUT (NOT MOCK)
`python Tools/B01-32/P17/render_art.py build/b01-32-p17` uses real CairoSVG/Pillow to rasterize exact repository SVG into **12 transparent PNG files plus one 768×1024 RGBA texture atlas** and SHA-256 digest manifest; CLI rejects corrupt/incomplete source and validates transparency, atlas tile parity, stage visual difference and animation pulse. Actual GitHub Actions CI runs this on Ubuntu 24.04 and archives downloadable original PNG output. Playwright Chromium validates browser controls and screenshots 720px/1080px.

## CODEX IMPORT / ROLLBACK
1. Pin producer PR commit SHA + original GitHub Actions artifact SHA/evidence. Independent B01-40 verifies file hashes.
2. Extract only `part-damage-atlas.png` or individual `frames/*.png` from the exact green Actions run. Do **not** import `viewer.html` as game code.
3. For Unity 6000.3.14f1 URP, import PNG as 2D Sprite (Multiple), slice on a **256×256** grid: width 768, height 1024, three columns, four rows; Unity sprite import coordinates are bottom-origin, while manifest/PNG source rows are top-down, so **convert row** (`unityRow=3-sourceStage`).
4. Configure SpriteRenderer/material overlays on **existing** physical parts with Codex-owned adapter. Map states from **actual canonical part condition system**, not guessed HP thresholds. Use the 3-frame pulse only if desired. Stop/clear overlay on part detach or object unload; OFF policy requires HQ decision. This asset cannot itself change health or combat.
5. If rejected, remove imported atlas/overlays and any Codex-owned adapter; leave source art PR unmerged. No game save/schema changes.

## LIMITATIONS AND ACCEPTANCE
No native Unity Editor import/test/GPU screenshot or runtime claim. No native G1/G2 before licensed Unity testing and independent B01-60 assessment. This is **not** a new gameplay production source stream. Candidate only; Codex/HQ decides actual overlay routing, contrast and cost. Failure cases: alpha bleed on Unity sampling, wrong atlas orientation, overlay z-fighting on real mesh, unreadable color/geometry at 720p, pool-off semantics.
Producer must not merge. File-scope limited to Research/B01-32/P17, Tools/B01-32/P17 and standalone scoped CI.
