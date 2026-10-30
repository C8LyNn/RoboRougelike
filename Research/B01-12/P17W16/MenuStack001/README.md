# B01-12-MENUSTACK-001/R1 — deterministic menu history and unsaved-edit guard

**Owner** B01-12 Interactive UX Prototypes. **Dispatch** [PASS 17 / WAVE 16 #589](https://github.com/C8LyNn/RoboRougelike/issues/589). **Ownership** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). **Status** standalone UX research; `PRODUCER_SOURCE_PASS`; independent G0/QA pending; `UNITY_NATIVE_G1=NOT_RUN`; Codex ACK NONE; integrated NO; merge NO.

## Functional behavior
- Pure DOM-free `route_engine.js` UMD reducer (`initial`, `valid`, `view`, `step`) with a seven-route allowlist: garage, parts, inspector, arena, pause, rewards, settings.
- Bounded 16-entry navigation history (BACK/FORWARD and forward truncation on branching), fixed ancestor breadcrumbs, revision counter with overflow guard.
- Dirty-state guard prevents NAVIGATE, BACK and FORWARD while pending unsaved changes. Pending operation can be explicitly cancelled (`CANCEL`) or accepted (`DISCARD`), which clears demo-only dirty flag. No partial route changes on refusal. Explicit RESET is immediate and intentionally resets the demo state.
- Strict event shape, schema/version/state validation, invalid routes/unknown events fail closed, no arbitrary evaluation or dynamic HTML.
- Real interactive offline `demo.html` + `ui.js`: navigation buttons, route list, status, visible history, ARIA status, keyboard Alt+Left / Alt+Right / Backspace, modal focus trap and Escape cancel. Demo changes are illustrative; no game-side effects.

**Distinct from** [FOCUSNAV #449](https://github.com/C8LyNn/RoboRougelike/pull/449) (focus *inside* a panel), [MISSION-JOURNAL #561](https://github.com/C8LyNn/RoboRougelike/pull/561) (mission DAG), [DEVICE-HINTS #583](https://github.com/C8LyNn/RoboRougelike/pull/583) (device glyphs); no Codex gameplay navigation or Unity scripts touched.

## Reproduce original executable producer QA
Run within this folder:

```bash
node test_engine.cjs
CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py
```

- Node v22: **87/87 PASS**, exit 0; deterministic negative transitions, guarding, history bounds, invalid state and events.
- Linux real Chromium/Playwright: **116/116 PASS**, exit 0, 0 page errors, responsive 320×720 / 768×1024 / 1280×720 / 1920×1080. For CI, Chromium is installed via `python -m playwright install --with-deps chromium`.
- Because this sandbox disallows `file://` and localhost browser navigation, browser tests assemble a byte-identical *original* `demo.html`, embedding the **original SHA-pinned** JS files for `page.set_content`; this executes actual Chromium DOM and UI events, not a mock.
- `manifest.json` locks SHA256 and byte lengths of all five original executable sources including both test runners; tests fail before execution if any is altered. `node-results.json` / `browser-results.json` are actual executed receipts.
- Corruption negative: append one byte to a **disposable copy** of `ui.js` while preserving original manifest: Node and Python each return **exit 2 / FAIL_CLOSED**; never mutate the original checked-in package for this test.
- Native Unity Edit/Play/PhysX, Windows Player, real input hardware and human tests: **NOT_RUN**. This is not a Unity menu implementation, nor a proof of game save behavior.

## Receiver / install / rollback
**B01-40** independently verifies exact Git commit, all path/blob IDs, original pins, dedicated workflow and **safe** rollback scope, issues formal SOURCE_G0 ACCEPT/HOLD. **B01-60** independently reruns both original suites and corruption-negative from exact SHA with own logs. **B01-00** may schedule a future Codex prototype review only after independent acceptance. **Codex** exclusively owns any authorized future Unity navigation adapter with its own native G1.

**Isolated install:** unzip this research folder and open `demo.html` in a browser. No Unity import and no migration. **Rollback:** close the Draft PR and discard only `Research/B01-12/P17W16/MenuStack001/**` plus `.github/workflows/b01-12-menustack.yml`. No changes to `Assets/**`, `main`, `develop`, Codex source, #510, #556, #585/#587, or any previous B01-12 source. Do not run any destructive rollback on shared game content.

**Limitations:** bounded in-memory history only; not browser History API, not durable player save, not real Unity navigation, no real discard semantics. UI names, routes and descriptions are illustrative. No Codex ACK or independent acceptance claimed.
