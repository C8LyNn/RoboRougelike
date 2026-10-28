# B01-32 · WAVE 02 — SALVAGE BEACON 001
**Unique package:** `B01-32-SALVAGE-BEACON-001 / v1.0.0`. Owned by B01-32. Cosmetic reward/salvage signal sprite effect; *not* a loot system, rarity system or main-game VFX code patch.

## Real delivered content
Eight original standalone **512×512 transparent SVG frames**, original 4 columns ×2 rows **2048×1024 RGBA PNG atlas** generated from exact source by `Tools/B01-32/P17/SalvageBeacon/render.py`, 8 individual RGBA PNG and two review screenshots, actual PNG hashes and automated pixel checks. `viewer.html` opens locally with siblings, includes Play/Pause, frame scrub, next/previous and VFX OFF, no dependencies on game engine or network.
The marker is cyan/silver/gold, geometric (not color-only), with 8 distinct rotational phases. It visually identifies a reward location for review, not a new source of game rewards or changes to permanent loot.

## Codex import (ONLY AFTER REVIEW)
1. Pin exact PR source commit and download successful GitHub Actions artifact `B01-32-P17-SALVAGE-BEACON-RENDER`; verify manifest output file hashes against original `receipt.json`.
2. Unity 6000.3.14f1 URP: import exported `salvage-beacon-atlas.png` into a **Codex-owned optional folder** as Texture Type **Sprite / Multiple**. Cut 4×2 cells, 512×512 each. **Top-down source frame mapping:** frame i = (row=floor(i/4), col=i%4). Unity sprite editor rows count from bottom; Unity row = 1 - source row. Alternatively import the 8 single PNG frame files, no atlas offset conversions.
3. Use a standard cosmetic SpriteRenderer/quad for visualization only. If a real game event or reward visual callback exists, **Codex decides** mapping and lifecycle; no inferred item rarity, no gameplay objective signal, no pickup collision or economy rule. Total 8 frames at 120ms/frame, optional looping and pause.
4. If rejected, remove newly imported PNG and any Codex-owned optional view wiring. This package adds no save changes and requires no run migration. Do not merge the research source PR into gameplay unless authorized.

## Proof and blockers
The owner will publish source-render CI actual pass/fail and browser screenshot evidence. This is NOT Unity native playback, not integration, not a test of actual legibility during R6 combat, and not a new material/model job. Independent B01-40/60 intake and explicit Codex ACK are required. No protected Codex, CHL, PARTSIZE or original sprite-bridge #356 files touched.
