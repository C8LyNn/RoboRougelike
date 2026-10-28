# B01-12 — GARAGE-BLUEPRINT-001 / PASS 17

**Scope:** standalone, interactive research prototype under `Research/B01-12/P17/**`. It does NOT modify `Assets/**`, canonical Codex garage, CHL, PARTSIZE or the game's whole-run save. Codex alone implements/integrates gameplay.

## New working feature

Open `garage-blueprint-lab.html` in a modern browser. Compare fixed **baseline** against editable **working build**. Includes 11 uniquely named part slots (3 weapons A/B, Wheels L/R, Tracks L/R, Omni, Hover, armor, core), 4 drive selections, part filters, individual HP state, correct dual critical warning causes, live purchase/repair/dismantle *estimated* quotes, undo/redo (50 changes), accept working as new local baseline, reset, schema-versioned JSON export to the textbox and atomic import. There is no autosave, network call, download or local-storage persistence.

**Distinct from prior P16 W78 garage navigation:** deliberate side-by-side baseline comparison, transactional working plan, reversible edit history, strict portable JSON plan transfer. This is NOT a competing Unity implementation or another HUD3 A/B treatment.

## Executed browser QA

- **157/157 PASS** original full author Playwright against EXACT HTML SHA256 `d1732bca3fbc6b42558b742ae841b5372de69183b9f24e1bd60f2c426e28100e` / Git blob `ada094f5688759e7ea945898c6f7967f9eef8702`; actual 4 viewports 320×720, 768×1024, 1280×720 and 1920×1080; browser JS exceptions 0. Local extended test+157 result JSON+4 real screenshots supplied with conversation evidence artifact, **not GitHub Actions**.
- **74/74 PASS** actual original Playwright Chromium on exactly same pinned HTML, using **this repository's executable `replay.py`** (verified local file Git blob `4345165997fae779dec753d02f9c53b71e2ec450` byte-identical to GitHub). Covers baseline isolation, damage and dual warnings, undo/redo, track costs, filter, JSON roundtrip and reject-atomic invalid syntax/schema/HP at four viewports.

From the repo root on a machine with Python 3, `playwright` and `/usr/bin/chromium`:

```bash
python3 -m pip install playwright==1.57.0
python3 Research/B01-12/P17/replay.py
```

Expected stdout: `"checks": 74, "pass": 74, "fail": [], "pageerrors": []`. Fails closed if the HTML source SHA256 differs. This script runs **real Chromium/JS, not Unity**. To use other platforms, update the locally installed Chromium executable path explicitly and report its version; do not call such an edit unchanged-source QA.

## Economy, semantics and constraints

The 11 visible part prices are demonstrative R3-style values: Gun 30, Wheels 15 each, Tracks 45 each, Omni 40, Hover 35, Core/Armor 5. Changed HP repair preview uses `ceil(positive_HP_gain/100 * purchase_price * .35)` with minimum 1 for a positive repair, no refund for removal, and full purchase price for newly added part. This **is a research-only quote** and not a current Codex `R3VehicleModel` transaction or policy; other game discounts/inventory ownership/real scrap restrictions are NOT modeled. Mounted/operational/selected-mobility counts ignore adjacency, collision, connected structure, and balancing, and thus are illustrative only. No statement that an assembled robot will physically deploy or fit.

Export/import schema `B01-12/garage-blueprint-v1` accepts EXACT three top-level keys, EXACT 11 slot IDs, known four drive names, integer HP from 0 through 100 or null. Unsupported, oversized and malformed input is rejected **without changes to working, baseline or history**. JSON is portable within this isolated tool only; **not a MapDocument or real run-save JSON**. The text area is user-visible; do not enter real personal/secrets data.

## Codex integration proposal, acceptance and rollback

1. B01-40/G0: inspect only three source paths in this PR; verify exact Git SHA + manifest hash and that no `Assets/**` or other packages changed.
2. B01-60: independently run `replay.py` against the exact new HTML, inspect a 1280×720 and 1920×1080 UI capture and validate reject-atomic import + dual critical status. Scope stays research browser.
3. Codex: decide whether baseline/working comparison and JSON schema should be adapted into the canonical garage. **DO NOT** import HTML directly as a live Unity scene or import this JSON as game save. Adapt controller/view under Codex's exclusive source ownership; implement real pricing, transactions, gameplay guards and actual persistency only if approved.
4. Before production acceptance, test real Unity garage with two weapons and selected mobility missing (tips OFF), 0HP repair, both drive families, price quote/commit/rollback, keyboard, 1280×720/1920×1080 Windows Player, relevant NUnit/logs. Existing CV-03 owns engine checks; native QA currently **NOT_RUN** for this experimental feature.
5. Rollback: discard isolated `Research/B01-12/P17/**` directory or close this Draft PR. Nothing in game runtime depends on these files; no migration or save edits.

**Gate:** package `OFFERED_FOR_INDEPENDENT_REVIEW` only. `CODEX_CLAIMED=NO`, `GAME_INTEGRATED=NO`, `NATIVE_G1=NOT_RUN`, `G2=HOLD`, `HUMAN_STUDY=0`. New source must not be mutated after immutable handoff without an owner-approved version bump.
