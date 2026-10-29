# B01-12-SNAPGRID-001 / R2 — source-backed export freshness repair

**Owner:** B01-12 Interactive UX Prototypes. **Dispatch:** B01 #367 PASS17 Wave02. **Delivery:** one standalone HTML research instrument, not a game implementation. **Scope:** exactly `Research/B01-12/P17W02/SnapGrid001/`, no canonical `Assets/**`, game economy/save, CHL, PARTSIZE or new model jobs.

## Runnable product

Open `snapgrid.html` in a desktop/mobile Chromium-compatible browser without a web server or network. Click or keyboard-control the **11×7 simulated 2D mounting grid**. Select Armor (2×1), Gun (1×2), Wheel (1×1), Hover (2×1) or Remove, rotate the footprint, preview validity, and place at the clicked cell. Core is fixed at (5,3) in zero-based grid coordinates. A placement must fit inside grid, not overlap an existing part, and share an **orthogonal** side with the connected structure. Removing a part is refused if it would disconnect any remaining part from core. Undo and redo affect placement. Demo fixture: core, bridge Armor, Gun attached to bridge, left/right Wheels; removing the bridge is refused until dependent modules are removed. Visual alerts show presence of a gun and wheels either side of core (simple 2D heuristic). Export provides readable, noncanonical JSON for UX review only; **there is no import into the game**.

This is *not* real Robocraft 3D attachment, 1×2×2 PARTSIZE geometry, corner/vertical socket physics, Build/Load/Save run storage, or actual combat/mobility readiness. The `2D` heuristic is deliberately labeled in the UI. Never consume the JSON in game without a Codex-owned adapter and source contract.

## Tests and source integrity

Requires Python >=3.10, `pip install playwright`, and installed Linux Chromium at `/usr/bin/chromium` (or locally adapt test runner's browser path in a **new test revision**, not frozen original).

```
cd Research/B01-12/P17W02/SnapGrid001
python3 test_snapgrid.py
```

The script checks the exact HTML SHA256 against `manifest.json` **before launching the browser**. On genuine original source: **152/152 browser predicates PASS** — original 100 regression checks plus 52 additional export-freshness checks across **320×720, 768×1024, 1280×720, 1920×1080**; **0 JavaScript exceptions**, screenshots under `evidence/` locally. Tests execute real DOM actions and keyboard events: isolated placement, overlap, right boundary, rotation, bridge/protected removal, core immutability, Undo/Redo, real keyboard R/arrows/Enter/Delete, 11×7 cells, alerts, export and overflow. Source HTML SHA256 `be8b4908663cd7c887d8feb6c9b096ef24f9b1c0bde2326b4705f205c8e681ef`.

**Negative control:** change a byte in `snapgrid.html` then run `test_snapgrid.py`: it must exit 2 with **FAIL CLOSED source mismatch**; restores unchanged source before positive execution. Browser tests are **NOT Unity EditMode/PlayMode/PhysX/Windows Player**, and there are **0 human subjects**.

## Intake, owner restrictions, rollback

- **B01-40:** verify branch/PR against `main`: only `Research/B01-12/P17W02/SnapGrid001/{snapgrid.html,test_snapgrid.py,manifest.json,README.md}` new. Verify Git blobs, manifest, dependency and original test launch in a disposable workspace.
- **B01-60:** independent real browser playback on exact new HTML source, especially demo bridge removal, keyboard navigation, 320/720/1080 responsiveness and false-positive connectivity cases. Report **new exact SHA**; producer own Chromium green is not independent QA.
- **B01-00:** sequence after QA into existing Codex inbox #79. **Codex** alone decides how/if 2D affordances can inform original Unity garage (3D sockets, orientation, float error handling, HP, economy, PARTSIZE all require separately owned implementation and native Unity proof).
- **Install:** no installation in Unity; download/open file as research prototype. **Rollback:** close Draft PR / remove standalone research directory. No migrations, default assets, game state or CI changed.

**Status:** producer browser G0 PASS; independent receiver PENDING, CODEX_ACK_PENDING, Unity G1 NOT_RUN, GAME_INTEGRATED=NO, G2 HOLD. Do not merge to `main` or `develop` without owner authorization.

## WAVE03 R2 — REAL original-browser RED → GREEN

Old immutable R1 Git blob `39113ac4f90e57584877339fae7c5eafcd10150e`: export JSON containing **1 core**, then mount Armor next to the core; UI shows **2 parts**, but textarea still exports **1 part**. This was reproduced in real Chromium across 320, 768, 1280, 1920 CSS widths (4/4 stale exports). Export must never misrepresent the current review layout.

R2 `snapgrid.html` Git blob `1ee408a1532d07c3a83437780e2d87174a6b2656`: adds accessible `#exportStatus`; after successful mount, removal, reset, demo, Undo and Redo, `invalidateExport()` clears stale readonly JSON until a new explicit Export. A rejected placement/demolition preserves already-current JSON. Original mount/connectivity/keyboard logic unchanged. Four actual viewport RED→GREEN comparisons: 8/8 expected outcomes. Reconstructed exact published new `test_snapgrid.py` Git blob `d86cb982c91f7842ffc31d48ccc9a159887b43e1` and locally executed byte-for-byte **152/152 PASS, 0 JavaScript exceptions** in Linux Chromium 144. The runnable script is source-SHA256 gated before browser launch and writes real `results.json` and two local screenshots. Old R1 remains available by immutable commit for comparison, not overwritten.

**R2 acceptance:** B01-40 validate four source hashes and Research-only changed-file scope; B01-60 independent original-Chromium retest of the changed exact blob and fail-closed tamper negative; B01-00 determines Codex #79 priority only after receiver. Canonical Unity/PARTSIZE sockets/PhysX/Player and human tests **NOT_RUN**, Codex ACK **PENDING**, no merge or game integration.
