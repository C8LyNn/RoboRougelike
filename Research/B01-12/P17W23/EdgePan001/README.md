# B01-12 EDGE-PAN-001/R1.0.1 — viewport auto-pan during drag

PASS17/WAVE23 · HQ #731 · supersedes R1 ALT_HOLD UX RED · owner B01-12 Interactive UX Prototypes · policy #276.

## Real functionality
- `pan_engine.js`: original deterministic pure viewport/gesture reducer, quadratic edge-zone acceleration, 760px/s cap, 50ms max step, pointer identity guard, 1440x940 canvas, no-scroll-unless-drag, boundary clamp, revision/schema validation and fail-closed invalid event.
- `demo.html` + `ui.js`: responsive, offline interactive 8-part mock garage, actual pointer-captured drag, auto-pan near all four edges, visible diagnostic readings, canceled-on-blur/Escape, disable checkbox, reset and pan presets. No external runtime deps, storage, networking or game modification.
- Distinct from previous BOXSELECT (bulk selection and collision-safe moves), HUDSAFE (HUD panel packing), SnapGrid (mount placement), DEVICE-HINTS (input glyph switch).

## Tests
Run `node test_engine.cjs` (Node22) and `python3 test_browser.py` (Python Playwright Chromium available at `/usr/bin/chromium`). Both validate original SHA256 and byte sizes from `manifest.json` before running. To test fail-closed, copy the package to a disposable folder, append one byte to `ui.js`, expect exit code 2 from both unchanged runners. Never mutate original source for a negative test.

## Scope & rollback
New-only `Research/B01-12/P17W23/EdgePan001/**` and dedicated `.github/workflows/b01-12-edge-pan.yml`; no `Assets/**`, protected sources, previous package paths, `main/develop`, canonical Save/Load or Codex modifications. Safe rollback is to close Draft and discard only own files in a disposable checkout; preserve foreign files/empty dirs/symlinks.

**SOURCE ONLY:** no native Unity, actual robot physics, canonical Garage, game Save/Load or real human tests. `G0_INDEPENDENT_PENDING`, `G1_NATIVE_NOT_RUN`, `INTEGRATED_NO`. B01-40 independent source G0; B01-60 independent original-byte Node+Chromium rerun; B01-00 decides optional future Codex intake after acceptance.

## R1 → R1.0.1 verified regression repair
Original R1 #746@bce875a7263a818360d966f5206f7030b352d85b had 80 browser PASS but failed eight newly added real-browser checks: 2 axes × 4 viewport sizes. During an ongoing drag near a canvas edge, the viewport scrolled but the part stayed at its previous world coordinate and drifted away from the stationary pointer. R1 is not feature-complete; it is ALT_HOLD and must not be installed.

R1.0.1 updates the dragged module's world-space position on each render/TICK by combining the current active pointer coordinate with the new viewport scroll offset, clamped to world bounds. The eight new actual Chromium assertions keep the part anchored beneath the stationary pointer at four sizes. This separate candidate does not rewrite #746. Producer source regression PASS is not independent G0.
