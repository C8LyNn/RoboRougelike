# B01-12-A11Y-PREFS-001 / R1 — Interactive Accessibility Preferences

**Role:** B01-12 Interactive UX Prototypes · **Dispatch:** [B01 PASS17/WAVE09 #466](https://github.com/C8LyNn/RoboRougelike/issues/466) · **Governance:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Working product

Open `a11y.html` in an ordinary modern browser alongside `a11y_engine.js` (no server or network required). Select high-contrast or standard theme; 100/125/150% text; normal/reduced animation; icon+text or icon+text+pattern critical alerts; captions on/off. The live sample responds immediately through real CSS custom properties, DOM states and ARIA status. Warnings always retain icon AND text; user cannot hide critical warnings by choosing captions off or reduced motion. The bundled engine is independent of the DOM and usable under Node.js via `require('./a11y_engine.js')`.

Additional **functional** state-management: bounded 24-step undo/redo, reset to defaults, strict enum/type validation, versioned portable profile JSON import/export, rejection of invalid/oversized/unknown fields **before** any state change, and automatic invalidation of previously exported JSON after a successful edit. No browser storage; no network, user data, analytics, cookies or game-state modifications.

This package is **standalone UX research**. The sample is not an actual Unity settings screen and does not adjust canonical camera, gameplay, weapon input, save files or Unity runtime. User-selected values are prototype policy proposals; **approval of production settings and migration contract remains Codex/HQ-owned**.

## Test and provenance

Run from `Research/B01-12/P17W09/A11YPrefs001`:

```bash
node test_engine.cjs
python3 test_browser.py
```

Requirements: Node.js 20+, Python 3.10+ with `playwright`, Chromium available as `/usr/bin/chromium` or Playwright's installed Chromium. In GitHub Actions, `ubuntu-24.04`, Python 3.11, Node 22 and `python -m playwright install --with-deps chromium` are selected. The browser runner uses an **actual** Chromium DOM, original SHA256-pinned `a11y.html` and SHA256-pinned `a11y_engine.js` inserted as a classic script because the local test host blocks navigation to `file://`; the user-facing originals remain separate on disk. No simulation of browser assertions.

Both runners verify SHA256 and byte size of **all four** executable sources from `manifest.json` before executing. Deliberately altering one source byte in a disposable copy must cause **exit code 2 / FAIL_CLOSED**, before Chromium starts. Inspect real per-case `node-results.json` and `browser-results.json` for test coverage. `evidence-1280x720.png` is a real test screenshot, local user artifact, not a fake Unity screen. The two results are producer receipts only; independent B01-60 acceptance still required.

## Receiver acceptance

**B01-40 G0:** read unchanged exact published PR HEAD SHA; verify isolated diff contains only `Research/B01-12/P17W09/A11YPrefs001/**` plus its **dedicated** workflow. Check source SHA256+sizes, Git blob IDs and actual path scope, no duplicate `.meta`/GUID/asmdef or protected path touches, and demonstrate safe rollback by discarding only this Draft. This adds **zero** `Assets/**` paths and no global required-check workflow edits.

**B01-60 source QA:** in disposable checkout of the exact HEAD run the two unmodified test scripts, record Node/Chromium versions, full logs and 320×720, 768×1024, 1280×720, 1920×1080 results. Negative: append one byte to `a11y.html` and run each original script; both must return exit 2. Independently confirm high contrast, >7:1 preview text ratio from defined palette, reduced motion 0ms, 150% text, error-rejected atomic import and keyboard-only select, focus and overflow.

**B01-00 / Codex:** only after independent QA, decide whether settings UI is appropriate for future native integration in `#79`. Codex exclusively owns any adaptation to Unity 6 UI Toolkit/uGUI, persistence, settings migration, controller integration and native EditMode/PlayMode/Windows Player validation. No ACK or game integration is implied by publishing this Draft.

## Source ownership and rollback

One owner: B01-12. No changes to previous B01-12 SnapGrid #404, Controls #428, FocusNav #449 or TutorialFlow #464. The source folder is isolated and the workflow is dedicated; this does not consume or override canonical Codex gameplay, CHL/PARTSIZE/MODEL_JOB or shared CI. **Rollback:** close Draft PR/delete this folder + the dedicated workflow branch, with no runtime migration or persistent player files to restore.

`QA_PRODUCER=PASS` (only after actual receipt), `B01-40_INDEPENDENT=PENDING`, `B01-60_INDEPENDENT=PENDING`, `NATIVE_UNITY_G1=NOT_RUN`, `GAME_INTEGRATED=NO`, `G2=HOLD`, `CODEX_ACK=NONE`, `MERGE=NO`.
