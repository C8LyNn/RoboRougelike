# B01-11 / PASS 17 / COMBAT-CUES-001 — ImpactLens

A **real interactive combat-readability prototype**, not a game-system patch. Open `SOURCE/index.html` in a modern browser (no dependencies): three replayable combat traces, five independently damaged parts, single-weapon availability badge, core status, source-of-damage disclosure, event history, compact mode, keyboard N/R and reduced-motion preference.

## Play
1. Open the HTML locally; choose Pojedynek / Friendly fire / Awaria rdzenia.
2. Select Next event, Play all, Reset, or compact mode.
3. Test destruction of weapon A, destruction of a wheel, ally hit/blocked hit and core loss.
4. Results are **synthetic UX trace data**: no real gameplay, physics or authoritative damage.

## Owner & source
Owner B01-11 only; new paths `Research/B01-11/P17/COMBAT-CUES-001/**`. Baseline `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Exclusive source `SOURCE/index.html` + browser driver `SOURCE/test_browser.py`; no `Assets/**`, CHL/PARTSIZE, B01-12 HUD3, Codex #310 or existing owner folders modified. Upstream references: #330, #276, #322, #79. Standalone reproduction follows `TEST_PLAN.md`.

## Actual verification
80/80 **headless Chromium tests executed locally** on 320x720, 720x960, 1280x800 and 1920x1080. Includes 6 original JS reducer positive/negative cases per viewport. HTML Git blob `9d6d5cb726827cbc782c1c754fe6243a83493ab9`, sha256 `307cd083d40348f73b4721cf89d27a12a70c6945c40f717f296e2db3687737c6`. Original test driver Git blob `6dfb6096e525b27ce453263768fd13580ed3c81b`, sha256 `dd66130ee312b807891e189ba1f1fd4ac119203149224bbe4e9cc7e9f99017ae`.

**NATIVE UNITY / CODEX INTEGRATION / HUMAN UX TEST: NOT_RUN.** See limitations. No G2, CODEX_ACK, merge, gameplay integration or MODEL_JOB claimed.
