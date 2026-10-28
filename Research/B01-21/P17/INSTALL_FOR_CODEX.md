# B01-21 MAP-ROUTES-001 v1 / CODEX intake, validation and rollback

## Immutable package shape

- Producer: B01-21; owned delta exclusively `Research/B01-21/P17/**` and `.github/workflows/b01-21-p17-map-routes.yml`.
- Dependency: P16 original immutable B01-21 map blueprint sources `a71eda99a260f807ae8cfea71348e75ff4f6c65a`, three pinned Git blobs in `map_routes.py`. No Codex MAPGEN source edits.
- Runtime requirement: **none until Codex authorizes and implements a receiver**; Python tool consumes actual source-generated map blueprints and exports precomputed read-only waypoints. JSON is **NOT MapDocument v2** and cannot simply be imported by the map editor.
- No changes to `Assets/`, `ProjectSettings/`, `Packages/`, `main`, `develop`, CHL or PARTSIZE.

## Intake (operator; never touch Codex active worktree without consent)

1. Have B01-40 validate Draft PR HEAD and diff against base P16 branch: only P17+own CI; verify historical P16 files/sha exist and Git blob identities match.
2. In separate disposable checkout of P17 branch run `python -m unittest discover -s Research/B01-21/P17 -p 'test_map_routes.py' -v` and `python Research/B01-21/P17/map_routes.py --check`. Re-run `python Research/B01-21/P17/map_routes.py` and recheck. All seven outputs must be exact bytes; a mismatch fails closed.
3. Inspect each `generated/*.routes.svg` (three overlays) and `generated/*.routes.json`, confirm 6 routes per map and that each `sourceMapGenGeometryHash` equals actual `MapGenPlanner.Plan` from **the target game**. If generator source changes, STOP and request a new owner version; never silently accept stale paths.
4. If a separate, approved native Unity 6000.3.14f1 test environment is available, regenerate each seed using an **actually measured current R6 vehicle**; transform local XZ to `MapGenBuilder` host world coordinates correctly. Drive every selected path with real Rigidbody/wheels and actual colldiers, in particular 90° lateral turns and outside/inside cover; record exact Unity player/edit/play XML/log/exit and owner SHA. Retest dynamic world regeneration/restart, inactive objects, different maps and enemy navigation independently. Without these real observations, mark `NOT_RUN` and decline gameplay safety claims.
5. Only if Codex chooses to integrate: implement a canonical consumer reading strict `schema`, immutable geometry hash, profile fit and waypoint list. Do NOT replace navigation/AI control with unverified grid paths, change rewards, or write save data. G2/merge requires separate HQ approval; no automatic install.

## Falsifiable native receiver

**PASS**: geometry hashes match target original MAPGEN results, correct host transform, route selection, original R6 light/heavy physical start→finish with no clip/out-of-floor/softlock and original logs/rig measurement; static source coordinates become correct world-space points and the existing main route remains accessible after spawn/despawn. **FAIL**: hash mismatch allowed, blocked/colliding physical route, wrong world transform, missing original source, duplicate path absorption, unsafe serialized map mutation. **NOT_RUN** unless actually executed on licensed authorized Unity/Windows host. Source-only CI does not clear G1.

## Rollback

If not integrated: discard the isolated P17 research branch/diff (or refrain from fetching it) — canonical game/source unchanged. If Codex later installs a receiver under separate authorization: revert **only Codex's explicit integration commit** and remove any new P17-only generated non-authoritative files; do not delete or overwrite existing saves or P16 maps, do not modify upstream MAPGEN or CHL/PART. Retain test evidence and hash pins for forensic traceability.

**Next owners:** B01-40 source/manifest gate → B01-60 independent QA → Codex decides whether to claim and integrate; HQ owns release/G2. ACK_PENDING, NOT_INTEGRATED.
