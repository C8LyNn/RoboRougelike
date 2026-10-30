# B01-12 RECOVERY-READOUT-001/R1 — PASS17 WAVE24

Owner: B01-12 Interactive UX Prototypes. HQ #731, governance #276.

## Implemented

A functional **read-only** browser UX with a pure deterministic snapshot projector, strict input validation, live DOM updates, keyboard-accessible scenario selection, screen-reader status, five fixture scenarios and JSON diagnostic export. Rejects contradictory Core, deploy, loaner eligibility, duplicate/invalid IDs, broken chronological recovery and impossible quotes. The two original test suites check strict source manifest SHA256 and byte length *before executing any code*.

Owner decisions represented as text, **never executed by this package**: one free rebuild per authentic distinct victory, newly destroyed parts wait for the next win; one weak loaner per run only for living Core and real next challenge; destroyed-part rebuild costs 65% of new part price (actual whole-Scrap rounding belongs to B01-22); fight blocked only when genuinely nonfunctional; Save crash integrity without advanced disk anti-tamper. All demo IDs, quotes and eligibility flags are illustrative and **not authenticated**. This UI never writes Save, claims victory, awards Scrap, starts combat or mints a loaner. Distinct from B01-13 #706 interactive economy decision simulator.

## Executed tests and reproduction

From this folder run `node test_engine.cjs` (Node 22) and `CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py` (Python 3.11, Playwright 1.55 and real Chromium). Test source hashes are pinned in `manifest.json`; `node-results.json` and `browser-results.json` are producer receipts, real screenshot is in the supplemental ZIP. In a disposable copy append a byte to `ui.js` and rerun unchanged tests: both must exit **2 FAIL_CLOSED**. Own dedicated workflow repeats Node, Chromium and tamper tests.

## Governance and handoff

Exclusive new-only Git paths: `Research/B01-12/P17W24/RecoveryReadout001/**`, `.github/workflows/b01-12-recovery-readout.yml`. No `Assets/**`, no existing file edits, no frozen #510/#556/#596/#647/#690/#729/#712/#669/#749 nor Codex source changed, no merge. B01-40: independent original Git/blob/source G0 and safe disposable rollback. B01-60: independent exact-SHA Node+Chromium+tamper QA; native Unity G1 NOT_RUN. B01-20/22: only authorized owner may wire real authoritative ECON/loaner data. B01-23/62: exclusive physical Save session owner. B01-00: optional Codex intake only after QA; Codex unavailable, feedback NOT_MEASURED.

Rollback: close Draft or discard **only own paths** in a clean disposable checkout, preserve all foreign files/empty directories/symlinks. Six stages: IMPLEMENTED YES, TESTED producer PASS, QA_ACCEPTED PENDING, READY_FOR_INTEGRATOR NO, INTEGRATED NO, RELEASED NO. SOURCE_G0=PENDING; NATIVE_G1=NOT_RUN; CODEX_ACK=NONE.
