# B01-12-TACTICAL-PING-001/R1 — tactical marker UI prototype

**Producer:** B01-12 Interactive UX Prototypes · **Dispatch:** [B01 WAVE 18 #632](https://github.com/C8LyNn/RoboRougelike/issues/632) · **Governance:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Actually implemented
- Pure deterministic `ping_engine.js` validates bounded schema v1, map grid integer coordinates 0–999, marker types attack/defend/repair/move, priorities 1–3 and per-type TTL (6/8/10/5 seconds), at most 6 markers, unique stable active IDs, explicit logical-clock tick/expiry, priority order, select/remove/reset and strict snapshot import/export. Invalid/duplicate/overcapacity/corrupt operations fail closed without partial state mutations.
- Offline responsive `demo.html` + `ui.js`: actual interactive tactical grid, pointer click and keyboard Enter to place; visible, selectable markers with priority, timed expiry, removable current marker, dynamic counts, ARIA status and reproducible JSON state. Four viewport sizes. No browser storage, network, game-world writes or telemetry.
- **Distinct** from B01-21 MAPGEN physical routes #342, B01-24 squad pressure/alerts, B01-11 in-fight HUD warnings and B01-12 HUDSAFE #530; this is player-side tactical **ping UI prototyping**, not pathfinding, AI command or canonical Unity UI integration.

## Reproduce actual tests
From this directory with Node.js 22 and Python 3.11 Playwright 1.55 + Chromium:

```bash
node test_engine.cjs
CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py
```

Both unchanged runners first verify SHA-256+exact byte sizes for `demo.html`, `ping_engine.js`, `ui.js`, `test_engine.cjs`, `test_browser.py` against `manifest.json`. On any mismatch they exit **2 FAIL_CLOSED** before executing source or browser. To reproduce negative without touching original: copy this complete directory, append one byte to `ui.js` in copy, run original copied runners and expect exit 2 from both. Original evidence is recorded in `node-results.json`, `browser-results.json`, `evidence-1280x720.png` (the screenshot is producer-local, not committed to GitHub source). Tests run the actual bytes in headless Chromium via Playwright `set_content` with the exact pinned JS inlined to avoid the host's blocked `file://` protocol.

**Producer** locally tested Node 84/84 PASS and real Chromium 84/84 PASS, 0 JS page exceptions, 4 viewports. Dedicated path-scoped workflow reruns tests and negative source tamper in GitHub Actions. These are not independent B01-40/B01-60 receipts, and **native Unity EditMode/PlayMode/PhysX/Windows/human: NOT_RUN**.

## Install, authority, rollback
Standalone offline source research only: unpack directory and open `demo.html` in a local browser. No Unity `.meta`/asmdef and no GameObjects. **Not gameplay or integration ready.** A native game version would require explicit Codex scope/ACK, stable host world coordinate contract, gameplay command ownership, map collision proof and Unity G1.

Owned paths: `Research/B01-12/P17W18/TacticalPing001/**` and `.github/workflows/b01-12-tactical-ping.yml` exclusively. Zero edits to `Assets/**`, any Codex-owned file, frozen #510/#556/#596, alt #604, old B01-12 source, shared CI, main/develop. Rollback: close isolated Draft and discard only these new owned files; no folder-recursive deletion of foreign paths; no live migration.

**Receivers:** B01-40 independent exact HEAD + Git blobs + 5 manifest pins + rollback/CI scope G0. B01-60 independently rerun exact SHA actual Node/Chromium/negative with original evidence. B01-00 may queue for Codex only after independent receipt and prioritization; no phantom ACK or merge.

States: `IMPLEMENTED=YES / TESTED=PRODUCER_PASS / QA_ACCEPTED=PENDING / READY_FOR_INTEGRATOR=NO / INTEGRATED=NO / RELEASED=NO`, `SOURCE_G0_INDEPENDENT_PENDING / NATIVE_G1_NOT_RUN / G2_HOLD / CODEX_ACK_NONE`.
