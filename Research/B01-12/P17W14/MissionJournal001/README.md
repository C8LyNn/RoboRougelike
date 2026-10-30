# B01-12-MISSION-JOURNAL-001 / R1 — mission objective journal UX

**Owner:** B01-12 Interactive UX Prototypes. **Dispatch:** PASS17/WAVE14 #552. **Policy:** #276.

## Functional scope
- Isolated deterministic CommonJS/browser `journal_engine.js` with five illustrative ordered objectives and a dependency graph. Actions COMPLETE/PIN/RESET require strict event schemas and are fail-closed.
- Original browser UI `demo.html` + `ui.js`: keyboard activation, pin, filter all/available/done/locked, progress, selection, completion history, JSON import/export, ARIA status.
- Exact schema version 1: `{version,done,receipts,pin,revision}`. Validates all exact keys, dependent completion order, unique IDs/receipts, bounded length and pin; rejects malformed and overlong documents atomically. Export is deterministic. No storage, no network.
- This is **a UX prototype**, not actual native mission progression, reward, economy, save contract or Unity objective telemetry. Never import its schema as production Save/Load.

## Tests and reproduction
From this directory: `node test_engine.cjs`, `python3 test_browser.py` with Python `playwright==1.55.0` + Chromium. Manifest SHA256+byte lengths guard exact five executable source files before tests run. Intentionally change a copy of `ui.js` by one byte; both unchanged runners must exit 2 before launching tests/browser. Test verdicts for the actual published SHA are recorded by CI; native Unity/Windows Player **NOT_RUN**.

## Receiver and rollback
B01-40: independently inspect exact PR HEAD, manifest, Git blobs, own diff only, dedicated workflow and disposable rollback. B01-60: rerun exact pinned Node/real Chromium tests and both tampered-file negatives and attach genuine logs. B01-00: optional future Codex intake only if approved. **No auto Codex ACK and no Unity integration.**

Rollback: close isolated Draft PR and discard only `Research/B01-12/P17W14/MissionJournal001/**` plus `.github/workflows/b01-12-mission-journal.yml`. Zero canonical `Assets/**`, main/develop, Secure V2 #510 or existing B01-12 source changes.
