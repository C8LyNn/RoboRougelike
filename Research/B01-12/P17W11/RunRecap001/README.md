# B01-12-RUNRECAP-001 / R1 — After-action module damage timeline (PASS17/WAVE11)

**Producer** B01-12 — Interactive UX Prototypes. **Dispatch** [#500](https://github.com/C8LyNn/RoboRougelike/issues/500); **ownership** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). **Status**: isolated UX research and executable prototype; producer QA completed. **NOT integrated with Unity**; Codex ACK **none**; independent receiver G0/G1 **pending/not run**. The actual SOURCE_SHA is the HEAD of the draft PR, not a value prefilled in this file.

## Actual function

An original, completely offline read-only after-action **module loss investigator**. `recap_engine.js` validates and replays a versioned JSON event stream (hit/detach/repair), applies cumulative module HP, caps overkill and over-repair, rejects illegal module transitions, connects every destroyed-module event with its last recorded hit, and produces deterministic run summary and per-event explanations. `demo.html` + `ui.js` provide keyboard-accessible selectable timeline, type/source/search filters, live causal details, final module HP bars, and atomic, validated JSON import/export. Four original modules and ten events form the authentic **demo data** only, NOT a recording from an actual game session.

**Distinct scopes:** B01-11 UXFEED #438 shows active combat messages; B01-10 RUNSCORE #401 generates numeric performance scores. This project instead investigates **post-fight causality and component HP loss**, not real-time notices, scoring, balancing, equipment, or rewards. It does NOT modify Codex gameplay, collisions, save files, Garage or Input System; no Unity `.meta` files because this is not a Unity importable package. No network, cookies or storage.

## Run locally

Open `demo.html` in a browser from the directory alongside `recap_engine.js` and `ui.js` (or serve this directory locally). Pick events to inspect causal explanation; apply filters; export JSON, edit it or import another matching dataset. A failed import leaves the previous report untouched.

## Deterministic data contract

`schema = "b01-12.runrecap.v1"`; root keys exactly `schema/runId/durationMs/outcome/modules/events`; `durationMs` within 1–3600000; 1–24 modules with unique ASCII IDs, safe display names and max HP; at most 300 events sorted by timestamp and uniquely identified; hit/detach/repair event keys and sources are strict; repairs can't exceed HP maximum or target detached modules; `destroyed` can only detach zero-HP modules; no hit after detach, no repeated detachment. JSON <=100000 characters. The validation is atomic, no partial state mutation. Damage counted as actual applied HP, not excess overkill. `impact` and `joint_break` can disconnect modules with remaining HP. No physics causality is inferred: destroyed attribution uses last hit, while structural detachment uses its explicit reason. Any game integration must independently supply authoritative events and handle tick ordering.

**Safety:** IDs, schema and keys validated. Text passed to DOM via `textContent`, never via imported `innerHTML`. No generated HTML markup from JSON. This is a UX projection; not a trusted gameplay validator, anti-cheat subsystem, official save schema or battle telemetry source.

## Tests — original producer execution

From this directory with Node22, Python3 and Playwright Chromium installed at `/usr/bin/chromium`:

```bash
node test_engine.cjs
python3 test_browser.py
```

- Original Node suite: **66/66 PASS**, exit 0, includes replay/order/HP limits, fail-closed invalid state, duplicate IDs, malformed JSON, detach-before-zero, corruption guards, immutable projections, filters and deterministic repeat.
- Original real Chromium + DOM suite: **136/136 PASS**, exit 0, zero JS page exceptions, in **320×720, 768×1024, 1280×720, 1920×1080**. Browser tests cover typed/source intersections, causal detail, keyboard/selectable buttons, search/reset, atomic import, markup injection rejection, responsive no-horizontal-overflow. Headless Chromium runs exact original content in an in-memory document because some CI hosts restrict `file://`; this still executes an actual browser DOM, not an emulated browser.
- Integrity: both runners SHA256+byte length check the same FIVE original execution files against `manifest.json` *before* any checks. Intentionally changing one byte in a disposable copy of `demo.html` makes both executables return **exit 2 FAIL_CLOSED** (expected negative, no browser start).
- Run evidence `node-results.json`, `browser-results.json`; full-page capture `evidence-1280x720.png` generated on the test host/Actions artifact, not committed binary.

**Not performed:** Unity6 native Editor/EditMode/PlayMode/Windows Player; actual robot telemetry integration; human usability/playtest; B01-40 independent G0; B01-60 independent source/browser replay; Codex ACK. `G1_NATIVE=NOT_RUN`, `G2=HOLD`, `GAME_INTEGRATED=NO`.

## Scope, receival, rollback

Exactly files beneath `Research/B01-12/P17W11/RunRecap001/**` and one own `.github/workflows/b01-12-runrecap.yml`. No existing paths changed; main/develop not merged.

**B01-40** independently compare exact PR HEAD SHA and Git blobs, manifest source hash and byte length, 10 owner-scope paths and dedicated workflow permissions; verify disposable rollback. **B01-60** rerun both exact-original tests and deliberate tamper in independent clean checkout, provide original exit statuses/log/artifact SHA. **B01-00** may schedule Codex after both source receipts; no automatic `OFFERED_FROZEN` or Codex handoff is implied. **Codex**, if authorized, would implement native read-only telemetry adapter and UI using the strict event contract, with Unity G1 tests and its own source ownership.

**Install**: no install; research HTML+JS runs standalone. **Rollback**: close the draft PR or drop only this new research directory and dedicated own workflow; no player state / migrations / existing game code to revert. Do not force-push/change this version after handoff. For requested corrections create a new isolated version per #276.
