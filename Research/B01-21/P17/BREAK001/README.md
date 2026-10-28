# B01-21 · PASS 17 / WAVE 02 — MAP-BREAK-001

**A new, source-derived, playable-in-browser prototype of reactive cover destruction** for three **real** original MAPGEN001 CombatArena blueprints. The user can choose the arena, destroy any existing source Cover, compare real before/after source visibility, and restore all cover. The prototype is standalone and never changes the canonical Unity game.

This is **not** the existing MAP-COVER-001 (#362) static original-cover/LOS view. This package adds actual reversible **world state transitions** and materialized source geometry difference fixtures, automatically recomputes accessible positions, visibility and safe source-level Start→Finish routes after each destroyed Cover, and invalidates original shelter anchors that depended on the destroyed object.

## Actual production code & assets

- `cover_break.py` — deterministic Python 3.11 standard-library source-world state engine, 33 single-cover destruction events (6 + 11 + 16 original Cover objects). Immutably reads each original `MapBlueprint` through existing original-hash-pinned `map_routes.load_original` and `COVER001.cover_field`. Every state is indexed by original Cover ordinal and original full `MapElement` index. Computes 1-cover-removed source geometry and a **separate state SHA-256**, leaving the original MAPGEN `geometryHash` untouched.
- `test_cover_break.py` — executable independent positive and destructive-negative invariants: unchanged original input, one-cover-only mutation, monotonic LOS when removing opaque boxes, changed visibility evidence, loss of anchored shelter, original Light+Heavy conservative source-grid Start→Finish continuity, source metadata and deterministic replay, mutation/drift rejection.
- `generated/AR16_01_OPEN_FLANK.break.json`, `generated/AR16_02_SPLIT_DECISION.break.json`, `generated/AR16_03_DENSE_PRESSURE.break.json`: **all 33 actual source geometry damage cases**; each stores which original box is removed, change in visible enemy-source descriptor count for every affected baseline 4m-grid point, newly accessible static sample cells, original shelters invalidated, retained shelter IDs, and both original historical reference profile route lengths. Data is non-authoritative to Unity.
- `generated/cover_break_viewer.html`: actual **interactive self-contained browser prototype**, with embedded **exact real source-case JSON** (not demo values), 3 arena switcher, dropdown/click-to-destroy cover, reset/rebuild action and two separately rendered source canvases. Computes DOM stats from actual 33 pre-generated damage fixtures and redraws removed source cover. Self-test `?selftest=1` actually performs **33 destroy + 33 restore events**, verifies state IDs and counts, and must be checked in Chromium.
- `generated/manifest.json`: verified source SHA pin, exact states/case counts and source testing/native readiness classification. Materialized output committed to GitHub; not only workflow artifacts.
- `.github/workflows/b01-21-p17-map-break.yml`: real Ubuntu + Windows source tests, pinned 5 original Git blobs, committed deterministic 5/5 source output verifier before/after regeneration; real Ubuntu Chromium canvas UI/event smoke and 720p/1080p screenshots.

## Input identity and stacked PR boundary

Parent is existing B01-21 Draft [#362 MAP-COVER-001](https://github.com/C8LyNn/RoboRougelike/pull/362) at **`454cd190639ba6ce3600ceed5f4fbd97fff298c2`**. That in turn requires previously frozen MAP-ROUTES-001 P17 + MAPGEN original C# source blueprints. The original game source snapshot is Codex #310 `589b60a912096d1e9917cb0bbac73a53e7543b4f`; this package's **pinned actual original geometry JSON blobs** are `fce7cdce22fa9d9f512c8cd1d112cf1d83ad205e`, `022b5c0cd1e13ca64460a3d6a0ab0122c4c2c790`, and `1fe7bf8b124d999de75c0fd3cacc85ee62319429`. The code and CI explicitly require frozen previous tools `map_routes.py` blob `5888a7e96a58cabcc32da72cb8f74715b4d6ceb5` and `cover_field.py` blob `9ebc860f48f4681316b47c46ee0deba86330033b`. Any change fails the source gate. **No `Assets/**`, no original generator, no Codex scripts, no other agents' files are changed.**

## Reproduce and test

From the root of a disposable checkout of the proposed P17 branch:

```bash
python -m unittest discover -s Research/B01-21/P17/BREAK001 -p 'test_cover_break.py' -v
python Research/B01-21/P17/BREAK001/cover_break.py --check
python Research/B01-21/P17/BREAK001/cover_break.py
python Research/B01-21/P17/BREAK001/cover_break.py --check
```

Open `Research/B01-21/P17/BREAK001/generated/cover_break_viewer.html` in a modern browser; it is self-contained, with no network or service. Try each original Cover and reset. The standalone prototype provides **one destroyed Cover at a time**, not multi-damage or a saved run. The generated cases have a source map geometry ID and a per-state SHA-256; a consumer must not confuse the latter with original `MapBlueprint.GeometryHash`.

## Evidence classification and limitations

- **SOURCE_SIMULATION:** original MAPGEN static local XZ collider-like boxes at eye height Y=1.25m, fixed 4m Heavy-reference exposure grid, static original 1m BFS/A* clearance, default Light/Heavy historical reference envelopes, original COVER001 LOS source oracle. The original 4m sample grid and calculated newly exposed/collision-free cells are not actual dynamic camera sensor visibility or PhysX wheel clearance.
- **Reversibility** in this proof-of-concept means selecting pristine original immutable source again and recomputing identical original-state hash; it does **not** write Unity prefabs, create a runtime undo stack or serialize a save/load state.
- **Not implemented in game:** no damage-to-cover health, actual destruction animation, collider disable, dynamic AI reroute, actual projectiles and repair pickup, Unity scene, R6 vehicle/chassis simulation, asynchronous world events or multiplayer.
- No independent B01-60 acceptance yet unless separately reported. No native Unity 6000.3.14f1 or Windows Player run is reported from this research CI. Current rig hull must be newly measured before any physical acceptance. `NATIVE_UNITY=NOT_RUN`, `PHYSX=NOT_RUN`, `INTEGRATED=NO`, `G1/G2=HOLD`.

## Codex receiver, installation and rollback

See [INSTALL_FOR_CODEX.md](INSTALL_FOR_CODEX.md). B01-40 owns source manifest gate, B01-60 owns independent QA, B01-00 directs any approved handoff to #79, and **only Codex** owns integration with runtime map generation, destruction/colliders/AI and rollback testing. Do not merge into main/develop.
