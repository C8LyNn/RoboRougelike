# B01-12-GARAGE-FILTER-001 / R1 — executable offline module catalogue query UX

Producer **B01-12** | Dispatch [PASS17 / WAVE17 #608](https://github.com/C8LyNn/RoboRougelike/issues/608) | Policy [#276](https://github.com/C8LyNn/RoboRougelike/issues/276)

## Scope and functioning

An actual **stand-alone search implementation**, not a static illustration:

- `catalog.js`: twelve authored, immutable illustrative module records with names, part type, mass, cost, energy, slots, ownership, fit (light/heavy/both) and tags. These records are **not** actual Unity inventory, production economics, or representative real balance values.
- `filter_engine.js`: deterministic data-only `parse`, `search`, `exportSelection`, with Unicode-aware Polish text normalization, quoted phrases and Boolean AND, controlled predicates `type:`, `tag:`, `fit:`, `owned:`, `mass<=N`, `cost>=N`, etc., deterministic sorting `sort:mass`, `sort:-cost`. Strict rejection of unrecognised operators, invalid values, duplicate constraints, malformed input, and corrupt/duplicate catalogue IDs. Invalid queries return **no partial results**. The source catalogue is not mutated.
- `demo.html`, `ui.js`: working interactive browser UI with query input, Enter, presets, reset, keyboard focus, `aria-live` status, responsive result cards, count/facets, portable **ID-only export**. User strings enter the DOM via `textContent`, not `innerHTML`. **No** `localStorage`, network access, live Unity behaviour or save-file writes.
- No changes to `Assets/**`, Codex-owned gameplay, frozen Secure Reattach #510, selected Save contract #556, competing adapters #596/#604, or earlier B01-12 packages.

## Run the original package

Open `demo.html` with sibling `catalog.js`, `filter_engine.js`, `ui.js` in a local browser. From the directory:

```sh
node test_engine.cjs
python3 -m pip install playwright==1.55.0
python3 -m playwright install chromium
python3 test_browser.py
```

Source test runners verify **all six original source/test files** against the manifest SHA256 and byte count **before executing the engine or Chromium**; a mismatched file exits 2 with `FAIL_CLOSED <name>`. Browser tester runs actual Chromium DOM; because local harness may block `file://`, it inserts the exact already-hash-verified original JS into the exact verified HTML **in memory only**, not a stub or mock browser. On a host with `/usr/bin/chromium`, optionally `CHROMIUM_PATH=/usr/bin/chromium`.

## Source evidence, limitations and gates

- Producer local Node suite: **63/63 PASS**, actual Node 22, exit 0; parser, boundaries, id validation, negative queries, sorting, no mutations, export contract.
- Producer real Chromium Playwright: **96/96 PASS**, 0 page JS exceptions, actual responsive DOM at **320×720, 768×1024, 1280×720, 1920×1080**, actual filters, results, facets, errors, keyboard Enter, export and copy status; original receipts `node-results.json`, `browser-results.json`, rendered image `evidence-1280x720.png` (local physical evidence and CI artifact).
- Negative: in a disposable copy append one byte to `ui.js`: unmodified Node and Python runners **both exit 2** before execution, expected `FAIL_CLOSED`.
- Six original files `manifest.json` are SHA256+UTF-8-byte pinned, plus Git blob SHA1 for independent source reconstruction. `build_manifest.py` is a **producer-only** convenience script and should **never** be run to repair a receiver mismatch.
- Native Unity EditMode, PlayMode, PhysX, Windows Player, real users, actual inventory integration: **NOT_RUN**. G1_NATIVE=NOT_RUN, LIVE_GAME=NO, G2_HOLD. No claim of native functionality.

## Exact-source receiver and rollback

B01-40: verify exact PR head, eleven own paths (ten new Research files + one dedicated workflow), six pinned original sha256/bytes, Git blob SHA1, permissions, clean `main...HEAD` scope, and discard-only rollback; issue formal `G0_PASS/HOLD`. B01-60: independently rerun **exact HEAD** Node+real Chromium+copy corruption checks, preserving own exit logs and case evidence. B01-00: may prioritize optional future intake only after independent acceptance; this research does **not** constitute Codex OFFER/ACK.

Rollback = close this **Draft** and discard only `Research/B01-12/P17W17/GarageFilter001/**` + `.github/workflows/b01-12-garage-filter.yml` on its isolated branch; **do not delete shared empty directories or run unsafe rollback scripts**. No migrations, scene edits, database or persistent writes. Never merge without authorization.

**Source producer PASS ≠ independent source G0 ≠ Unity G1 ≠ Codex ACK ≠ integrated game.**
