# B01-12-HUDSAFE-001 / R1 — functional safe-area HUD layout laboratory

**Owner:** B01-12 Interactive UX Prototypes. **Dispatch:** B01 PASS 17 WAVE 12, [issue #518](https://github.com/C8LyNn/RoboRougelike/issues/518). **Ownership:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Real functionality

`layout_engine.js` is a standalone DOM-free, pure JavaScript rectangle-placement solver. It validates a versioned *conceptual* interface (`width`, `height`, `insets`, `exclusions`, `widgets`), imposes maximum 12 HUD widgets and eight exclusion rectangles, chooses deterministic positions by priority, named anchor, alternate anchors then 8-pixel grid scan; it never returns partial success. It rejects invalid inputs and layouts without enough safe space. Inputs are not mutated. Every accepted widget remains in-bounds, with no widget/widget or widget/exclusion intersection. Touching edges are not a collision.

`demo.html` + `ui.js` present working controls for 320x720 / 768x1024 / 1280x720 / 1920x1080, top cutout / central forbidden combat interaction area, 12/32px safe margins, six / twelve widgets and intentionally impossible 12-large-widget case. All widgets display positions calculated by the engine. The status is announced with `aria-live`. **Demo values are illustrative**. HUDSAFE is *not* actual UI Toolkit/Unity gameplay/HUD or a claim of accessibility certification. No code in `Assets/**`, input, game HUD or any Codex/CHL/PARTSIZE scope is touched.

## Running

Offline: open `demo.html` next to both `.js` files in a browser. Tests (Node 22, Python 3 + Playwright with Chromium):

```bash
cd Research/B01-12/P17W12/HudSafe001
node test_engine.cjs
CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py
```

If Playwright has its own installed Chromium, omit `CHROMIUM_PATH`. A static HTML source blob with the two **unchanged manifest-pinned** JS files inlined by the browser runner is executed in real Chromium to support environments that disallow `file://`; this does not mock DOM/rendering. The real offline HTML uses sibling script src files normally.

Original producer evidence at published source: **55/55 Node PASS**, **224/224 Chromium PASS**, **0 JS page errors**, 4 viewport presets × 3 cutouts × 3 density scenarios with actual select-driven DOM updates, mobile no document overflow, keyboard focus, exact match to expected fail-closed impossible layout. Outputs `node-results.json`, `browser-results.json`, `evidence-1280x720.png`. Test scripts verify SHA256 and byte-size pins in `manifest.json` before executing cases or browser.

For deliberate corruption, in a disposable copy append one byte to `ui.js`, then run both original test scripts. Expected **exit code 2** before any browser/test. Never mutate the submitted/accepted producer SHA.

## Receiver acceptance and rollback

- **B01-40 / G0:** inspect exact PR HEAD, each Git blob, 5 source/test SHA256+size pins and only owner path under `Research/B01-12/P17W12/HudSafe001/` plus dedicated workflow. Confirm no changing canonical code; test discard/rollback. Original producer file check ≠ independent receipt.
- **B01-60:** independently execute original Node + Chromium scripts and one-byte corruption test on exact immutable SHA; retain original stdout/exit code and browser screenshot. Native Unity G1 **NOT_RUN** unless an authorized real Unity host runs Unity tests for a *separate actual game port*.
- **B01-00:** only after G0/QA evaluate whether to queue Codex adaptation; no automatic offer/ACK. Codex would separately implement real HUD anchors, UI Toolkit/canvas safe-area handling, input impact and native regression. Avoid silently copying this research renderer into protected code.
- **Rollback:** close isolated Draft and remove its owner research directory and own workflow from a disposable integration checkout; no player save migrations, no runtime modifications, no merge/main/develop touches.

States: `SOURCE_PRODUCER_PASS` (once original tests done) · `INDEPENDENT_G0_PENDING` · `UNITY_G1_NOT_RUN` · `G2_HOLD` · `CODEX_ACK_NONE` · `GAME_INTEGRATED_NO`.
