# B01-12 / TOOLTIP-ANCHOR-001 / R1 — PASS 17 / WAVE 19

**Producer** B01-12 — Interactive UX Prototypes · **HQ** [#646](https://github.com/C8LyNn/RoboRougelike/issues/646) · **Policy** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Real functionality
- `anchor_engine.js` — dependency-free original JavaScript geometry engine. Bounded placement of contextual tip rectangles around a chosen anchor (`top/bottom/right/left`) with exact preferred-side priority, safe insets, at most 8 exclusion rectangles, valid gap, independent clamping of the tangent axis, deterministic fallback. Fails `INVALID_INPUT`, `OUT_OF_BOUNDS`, or `NO_FIT` with **no partial geometry**, and never mutates input.
- Original strict `reduce` state machine for mouse hover/focus, click-to-pin, ignoring HIDE while pinned, second-click unpin, Escape/RESET, immutable return states, 8 allowlisted tooltip IDs and max 100,000 state revisions; invalid event/state rejected, no gameplay side effects.
- `demo.html` + `ui.js` — interactive offline source-browser UI with 8 original illustrative robot stats, 4 viewport presets (1280×720, 1920×1080, 768×1024, 320×720), 4 preferred sides, optionally excluded central map rectangle, focus/hover/click pin and Escape, live geometry diagnostics. DOM writes of content use `textContent`, no dynamic HTML injection or network/storage. Viewport is a simulation; this package **does not represent actual game balance or Unity UI positioning**.

**Not duplicates:** earlier B01-12 HUDSAFE #530 solves whole-HUD rectangle packing, FOCUSNAV #449 manages focus/modal navigation, MENUSTACK #602 navigates across screens. This package solves tooltip-to-anchor placement, fallback and pin lifecycle only. It is not enemy telegraphs or live warnings.

## Executed source QA (producer, NOT independent)
Run from this folder:

```bash
node test_engine.cjs
CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py
```

Both harnesses independently verify every original `manifest.json` source SHA256 and exact UTF-8 byte size **before** loading any engine/browser code. The browser harness runs actual Playwright/Chromium, inserting **exact verified original script bytes** in the same HTML for environments that block `file://`.

- Node.js 22 deterministic engine and malformed-input coverage, four-side fallback, exclusions, hover/pin/escape, revision cap and purity: see `node-results.json` (case-by-case).
- Actual Chromium/Playwright, mouse/keyboard/focus, four real browser viewports, virtual display presets, DOM content, status, no horizontal document overflow: see `browser-results.json` and `evidence-1280x720.png` (the screenshot is producer-only local evidence, not an asset to merge).
- **Corruption negative:** in a disposable **copy** append one byte to `ui.js` without updating manifest. Both unchanged harnesses must exit 2 and print `FAIL_CLOSED source mismatch: ui.js`, before test and browser execution. Do not mutate original producer files.
- Dedicated path-scoped `.github/workflows/b01-12-tooltip-anchor.yml` runs both suites and corruption negatives, least-privilege read-only contents.
- Native Unity EditMode/PlayMode/PhysX/Windows Player/human testing: **NOT_RUN**. No Unity implementation, no game integration.

## Exact ownership, integration boundary and rollback
Own exactly `Research/B01-12/P17W19/TooltipAnchor001/**` and `.github/workflows/b01-12-tooltip-anchor.yml`. No `Assets/**`, Codex-owned scripts, frozen game #510, Save schema #556, Save host #596 or alternate #604, other agents' files, previous B01-12 sources, common CI or main/develop changes.

**Install for QA:** open the offline demo with the two adjacent original JS scripts; Node harness requires Node 22, browser harness requires Python 3 + Playwright + Chromium. No Unity import/`.meta`/asmdef. **Rollback:** close Draft PR and discard *only* this isolated folder and its dedicated workflow in a disposable tree; never recursively delete foreign files or empty directories. No game migrations or persistent writes.

**Receivers:** B01-40 independently verifies exact Git commit, 10 Git blobs, five pinned SHA256/bytes, dedicated workflow, no-conflict ownership and safe discard (`SOURCE_G0`), then supplies its own immutable receipt. B01-60 independently reruns Node + genuine Chromium and modified-copy negative, attaches its own stdout/logs and exact SHA. B01-00 alone decides optional future intake to Codex #79 after independent acceptance. Codex alone owns future canonical Unity adaptation after explicit claim. A source producer PASS is **not** G0 accepted or Unity G1.

**Six lifecycle gates:** `IMPLEMENTED=YES / TESTED=PRODUCER_SOURCE_PASS / QA_ACCEPTED=PENDING / READY_FOR_INTEGRATOR=NO / INTEGRATED=NO / RELEASED=NO`; `SOURCE_G0_INDEPENDENT=PENDING / UNITY_G1_NOT_RUN / HOST_SAVE_NOT_APPLICABLE / CODEX_ACK_NONE / G2_HOLD`.
