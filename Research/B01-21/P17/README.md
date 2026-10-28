# B01-21 / PASS 17 — MAP-ROUTES-001 v1

**Production output:** real, deterministic local-XZ navigation *data* for existing actual source-generated MAPGEN001 CombatArena layouts. Three distinct P16 map geometry hashes × two historical reference vehicle envelopes (Light 2.980×3.2m, Heavy 5.180×5.8m) × **three independent connected candidate lanes** `MAIN`, `WEST_FLANK`, `EAST_FLANK` = **18 source-level routes**.

**Not:** another seed/config preset, replacement generator, Unity scene, enemy behavior tree, navmesh, R6 steering algorithm, reward/capture mechanics, new gameplay-package source, new vehicle measurement or canonical game patch. This package adds no `Assets/**`, no MAPGEN001 or Codex modifications. Data is **NOT integrated** and only the Codex owner can do that.

## Inspect actual produced content

- `generated/AR16_01_OPEN_FLANK.routes.json` and `.routes.svg` — original seed 160101 (6 cover).
- `generated/AR16_02_SPLIT_DECISION.routes.json` and `.routes.svg` — original seed 160202 (11 cover).
- `generated/AR16_03_DENSE_PRESSURE.routes.json` and `.routes.svg` — original seed 160303 (16 cover).
- `generated/manifest.json` — three exact original geometry hashes, six route variants for each, and immutable source Git blob pins.
- `map_routes.py` — actual Python3.11 source-only deterministic grid routing, input provenance validator, negative failure modes, real source top-down SVG renderer.
- `test_map_routes.py` — executable positive and destructive-negative input tests.
- `.github/workflows/b01-21-p17-map-routes.yml` — fail-closed real Windows+Ubuntu QA and artifact publication.

**Fixed input provenance and stacked PR boundary:** new branch derives from B01-21 PASS16 frozen `research/b01-21-pass16-mapgen-three-arenas@a71eda99a260f807ae8cfea71348e75ff4f6c65a`; PR base is that research branch. Its three `MapBlueprint` JSONs came from actual unchanged Codex MAPGEN C# at `589b60a912096d1e9917cb0bbac73a53e7543b4f`; this source tool validates all **three Git blob hashes** byte-for-byte and rejects any drift. P17's own commits contain only `Research/B01-21/P17/**` and its isolated QA workflow. For Codex consumption, checkout the **P17 HEAD**, which already contains the unmodified P16 dependency, or apply the P16 dependency and P17 changes in order.

## What the new data does

For each original arena and each reference envelope, computes four-neighbour 1m-grid connected path segments from actual source Start=(0,-25) through three intermediate lateral goals to actual Finish=(0,25). Collision predicate requires original MAPGEN `Floor` support for 3×3 hull probes and respects conservative source AABB against every original solid `Boundary` or `Cover` with +0.25m margin. Source floor, blocked endpoints, unknown geometry kinds or original file/hash mismatch **reject** instead of inventing a route. Outputs every checked `gridPath`, condensed `simplifiedWaypointHints`, route length and source-map `GeometryHash` to allow Codex to avoid applying overlays to unrelated maps.

`MAIN` = continuous central route of 50 grid steps; `WEST_FLANK` and `EAST_FLANK` = laterally offset corridors of 56 grid steps each on the accepted source data. The left/right variants are source-geometrically distinct and remain inside or near the reserved main avenue. **They are NOT a guaranteed tactical cover flank, physical turning path, safe AI line-of-sight or reward branch.** Future generator layouts with actual cover in a lane may cause the algorithm to detour, or reject if source clearance cannot be established. The P17 output currently reflects the three exact original maps listed here.

## Run actual tests and rebuild (no Unity required)

From the repository root, Python 3.11+:

```bash
python -m unittest discover -s Research/B01-21/P17 -p 'test_map_routes.py' -v
python Research/B01-21/P17/map_routes.py --check
python Research/B01-21/P17/map_routes.py
python Research/B01-21/P17/map_routes.py --check
```

The **pre-generation** `--check` validates all seven committed outputs byte-for-byte (not just freshly produced files). Windows and Ubuntu CI run precisely the same suite, materialize downloadable generated assets and fail if output drift is detected. The test suite exercises three real source-map blobs, all 18 actual route variants and clearance/connectivity checks, deterministic output, left/right differentiation and fail-closed mutated blob, unexpected solid, blocked center and missing floor.

## Package constraints / truth

- Uses the P16 recorded historical Light/Heavy envelope *references*; does not measure current assembled robot or guarantee these dimensions are still valid. The player must be measured in legitimate Unity.
- Checks static planar source AABBs and 3×3 floor samples, **not** full swept vehicle rotation, 3D vertical clearance, actual collider transforms, wheel dynamics or slope. A 90° turn in a waypoint hint may be physically impossible. Do **not** feed the `gridPath` blindly into movement components and call it physical passability. A native NavMesh/physically driven route acceptance remains required.
- `routeId` is data for a future Codex-controlled consumer; no route selection UX, AI behavior, pickup, reward or mission progression is implemented.
- Source data coordinate system is local MAPGEN001 XZ. Actual generated `MapGenBuilder` can use a translated/rotated host; Codex must convert these positions to actual world transforms rather than treating them as world-space.
- No new Unity package was imported, so `UNITY_EDIT/PLAY=NOT_RUN`, `PHYSX=NOT_RUN`, `G1=NOT_RUN`, `G2=HOLD`. Unreviewed research overlay cannot be presented as shipped game content.

Full Codex receiver/rollback: [INSTALL_FOR_CODEX.md](INSTALL_FOR_CODEX.md). QA owner B01-60 and source gate B01-40 are separate from producer.

**Owner:** B01-21 | **Package ID:** `B01-21-MAP-ROUTES-001` | **Version:** `v1` | **Git source:** isolated Draft PR HEAD; do not merge without HQ / Codex.
