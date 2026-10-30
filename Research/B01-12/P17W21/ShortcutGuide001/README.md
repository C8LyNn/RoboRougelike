# B01-12 SHORTCUT-GUIDE-001 / R1 — interactive context-specific command discovery

Owner: B01-12 Interactive UX Prototypes. Dispatch: PASS17 W21 #687. Governance: #276.

## Implemented

- Standalone pure deterministic UMD engine for **three contexts** (garage, combat, map) plus global actions, with 20 original illustrative command records.
- Validated source command schema, duplicate ID and keyboard-chord conflicts (same-context and global), cross-context chord reuse accepted, search by key/name/description, deterministic category/name ordering, bounded strict state/reducer, context-filtered selection, safe atomic JSON restore.
- Real keyboard/pointer offline UI; search, context tabs, contextual commands and accessible detail panel; `?` toggles the overlay, Escape closes; all user-originated text enters DOM through `textContent`, no `innerHTML`.
- This is **shortcut discovery**, distinct from the previous B01-12 `CONTROLS-001` *binding remapping* #428, `DEVICE-HINTS-001` *device hysteresis* #583, `FOCUSNAV-001` *within-panel focus* #449, `MENUSTACK-001` *screen history* #602 and game native Unity Input System.
- `guide_engine.js` is a library/demo **not gameplay code**. Illustrative bindings do NOT claim to match canonical input action IDs. There is no live input polling or gameplay side effect.

## QA and security

`manifest.json` records exactly pinned SHA-256 and byte size for executable sources (`guide_engine.js`, `ui.js`, `demo.html`, `test_engine.cjs`, `test_browser.py`). Node and Python runners **fail closed, exit 2 before any test**, on altered bytes. Local Node tests cover keyboard conflicts, distinct contexts, state/events, invalid/foreign selections and JSON. Real Linux Chromium/Playwright exercises the original offline DOM in four browser viewports, pointer, keyboard Enter, Escape and `?`, search, absence of markup injection and horizontal overflow.

From folder: `node test_engine.cjs` and `CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py`. Python needs Playwright; the tests use local preinstalled Chromium. CI on Ubuntu 24.04 installs playwright and Chromium, tests original source and deliberate separate-copy byte tampering. Original source scopes are new-only under `Research/B01-12/P17W21/ShortcutGuide001/**` and `.github/workflows/b01-12-shortcut-guide.yml`.

## Ownership and rollout

- NO `Assets/**`, Codex gameplay, `#510/#556/#596/#647`, canonical API `#666`, native QA scripts, previous B01-12 sources, or protected branches changed. No owner-unauthorized integration/merge.
- **G0 receiver B01-40:** verify exact SHA and all Git blob IDs, 5 pinned executable SHA-256/lengths, only own paths, workflow least privileges, safe discard of exact own paths preserving foreign files, empty directories and symlinks.
- **Independent QA B01-60:** independently rerun exact Git SHA on Node22/real Chromium and source corruption test, publish original logs; this producer execution is not independent QA.
- **B01-00:** decide Codex priority only after independent formal acceptance. Codex integrates native UI only on explicit authorized scope, never automatically.
- **Rollback:** do not run recursive cleaners on shared checkout; drop only the isolated own research paths and one own workflow from disposable staging, close draft PR. No migrations or live data writes.
- Native Unity 6000.3.14f1 EditMode, PlayMode, PhysX, Windows player and human playtest are NOT_RUN; G1 NOT_RUN/G2 HOLD.
