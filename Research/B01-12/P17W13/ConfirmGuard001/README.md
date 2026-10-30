# B01-12-CONFIRM-GUARD-001/R1 — confirmation safety UX

PASS17/WAVE13, owner B01-12 Interactive UX Prototypes, dispatch issue #535, governance #276.

## Actual working artifact
- `confirm_engine.js` — pure deterministic reducer; one pending confirmation at a time; hold-to-confirm (>=1200ms uninterrupted start/end matching pointer) or accessible two-step acknowledgement (>=200ms after arm); 6000ms TTL, monotonic time, explicit cancel, reset, one receipt per arm serial, strict event/state schema, fail-closed for stale/invalid/unknown input.
- `demo.html` + `ui.js` — offline interactive tool; real keyboard Space/Enter hold, mouse/pointer hold, keyboard two-step, Escape cancels, live ARIA feedback, progress and receipt. All decisions are **simulation only** and NEVER call inventory, persistence or Unity gameplay.
- `test_engine.cjs` + `test_browser.py` — executable positive + adversarial negative tests under original byte pins; dedicated path-specific CI.

This is not existing B01-12 FOCUSNAV modal confirm, CONTROLS rebindings or A11Y preferences; it is an **independent action-commit guard** prototype. This is not Secure Reattach, a game save/load contract, native Input System, or part of #510. No collision with #510/#511/#494 or B01-23/62-owned paths.

## Run
```
cd Research/B01-12/P17W13/ConfirmGuard001
node test_engine.cjs
python3 test_browser.py
```
Node 22, Python 3.11+ and Playwright 1.55, Chromium; Linux chromium defaults to Playwright-installed Chromium, override `CHROMIUM_PATH=/usr/bin/chromium` to use system. Open `demo.html` locally in a normal browser to interact. In restricted environments, the test runner loads the **identical pinned HTML+JS original bytes** into Chromium in memory (no network / no mocks).

## Known limits / receiver
Time is monotonic `performance.now` in the live UI and caller-provided monotonically nondecreasing milliseconds in reducer. Browser events can be manipulated by a malicious host; this is **client-side UX research** not an authorization control or tamper-proof transaction, and **not suitable to guard real inventory/save operations** without server/game-authoritative checks. The browser demo has no source game data, persistence, real controller/device lab tests or human playtest.

B01-40: independent source G0: exact commit, files, manifest hash+size, restricted path scope and safe discard. B01-60: independent actual Node/Chromium test replay at exact original source and intentional mutation of copied source; report own logs and SHA. Unity G1 NOT_RUN; Codex-only future adaptation after HQ authorization/explicit ACK. Producer PASS is not independent QA.

**Rollback:** close Draft and discard only `Research/B01-12/P17W13/ConfirmGuard001/**` + `.github/workflows/b01-12-confirm-guard.yml`; no migration, no changes to `Assets/**`, game code, shared CI or prior frozen packages.
