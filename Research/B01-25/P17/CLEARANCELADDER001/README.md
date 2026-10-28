# B01-25-CLEARANCELADDER-001 / R1 — progressive-precision combat arena

**Owner:** B01-25 LEVEL & CHALLENGE DESIGN. **Scope:** Original importable MapDocument v2 arena + genuinely interactive, self-contained vehicle-radius proxy. Different challenge from SLALOM001: no E/W chicane, instead one straight course with increasingly smaller physical gates. Distinct from MAPGEN random presets. NO canonical Assets/** changes.

## Actual content

- `arena.json`: 415 native MapItems: 288 Ground, 105 Wall, 4 full-height Platform perimeter corners, 8 Pillar, 8 LowBarrier, 1 Spawn (9,2), 1 Finish (9,13). Floor is local, globalFloor=false. Game-ready geometry: 72×64m grid.
- `manifest.json`: original Git blob pin, precise original source identities, original gate spans, 11-step straight centerline.
- `preview.svg`: real topdown image derived from original JSON.
- `clearance-preview.html`: completely offline interactive map preview, accessible radius slider 0.5..8m, visual proxy circle, per-stage PASS/BLOCK margin and global proxy verdict, reset. Original JSON and manifest are embedded; no dependencies. It is NOT a Rigidbody, physical Player or robot simulation.
- `clearance-core.mjs`: reusable source-derived original MapWorld geometry checker and clearance proxy.
- `test.mjs`: self-contained Node 18+ regression, original Git file SHA1 check, 45 core source predicates, nine negative mutated sources, actual HTML JavaScript run in VM with synthetic DOM/Canvas. Originally authored before an actual native Node invocation. **Now EXECUTED by GitHub Actions on both Windows and Ubuntu**, original standalone `node test.mjs` with real Node 22 on exact source Git blobs; see run 40758466800.
- `evidence.json`: precise source file Git blobs, actually executed V8 checks and NOT_RUN labels.

## Tactical geometry and design

| Gate | Row z | Open columns | Actual XZ wall gap | Center clearance |
|---|---:|---:|---:|---:|
| L1 | 3 | 6–12 | **28m** | 14m |
| L2 | 6 | 7–11 | **20m** | 10m |
| L3 | 9 | 8–11 | **16m** | 6m |
| L4 | 12 | 8–10 | **12m** | 6m |

Four rows of original Wall geometry narrow monotonically. Centerline x=9 stays open through every gate. The source AABB sampled at 0.25m reports **6m minimum static obstacle clearance**, so a test *circle* of radius 5.5m has 0.5m min positive margin; radius 6.5m blocks L3/L4. These are 2D original MapWorld primitive calculations only; do not mistake for full vehicle steering, wheelbase/yaw/PhysX collision viability. Spawn at z=2, Finish at z=13 preserves staging distance from both perimeter walls.

MapDocument can load Ground, Wall, Platform, Pillar, LowBarrier, Spawn and Finish. It cannot express enemy configuration, timed zones, four gate checkpoint state, rewards or win logic. Do not claim the prototype implements these as runtime mechanics. Codex owns all real game integration and battle loop.

## Test / receiver

From a clone of the PR head with Node 18+:

```sh
node Research/B01-25/P17/CLEARANCELADDER001/test.mjs
```

Expected: original exact `arena.json` Git blob **53eb696950121465befea2855eda4d7268750016**; 45/45 source-space data checks, nine deliberately invalid variants rejected, standalone original HTML inline-script VM test (2.5 and 5.5 radius pass; 6.5 radius fails at L3 and L4, reset/slider functional). Producer originally executed the exact original validator function and inline HTML JavaScript with DOM/Canvas stubs in V8 (45/45, 9/9, UI PASS), and **later the original standalone `node test.mjs` EXECUTED on BOTH Windows and Ubuntu under actual Node 22** in [GitHub Actions #40758466800](https://github.com/C8LyNn/RoboRougelike/actions/runs/40758466800), tested commit `57b722cc6d76a7625eebe3f332bfaa9af33c4516`, PASS both OS; two original stdout log artifacts. Real Chromium / Unity still NOT_RUN. Genuine Unity / PhysX / Player NOT_RUN.

**Conditional editor import:** after B01-40 / B01-60 independent QA and B01-00 Codex scheduling, use a *disposable* Unity Application.persistentDataPath/Maps profile. Place exact arena.json bytes as `ARENA17_CLEARANCELADDER001.json` ONLY if not already present (never overwrite player maps), set EditorName to ARENA17_CLEARANCELADDER001, Wczytaj. Native PASS demands MapDraft.TryLoad=true, TestError=null, 415 items/kinds above, 8/8 true physical perimeter-wall+corner Platform raycasts, genuine Light/Heavy driving from (9,2) to (9,13) through all four gates, no unexpected escape, collision, softlock. Record actual contact/Collider bounds, turn, physical clearance, Camera trace, XML, logs, exit, exact runtime source SHA and map blob. Any vehicle unable to finish = **physical FAIL**, regardless of proxy result.

**Rollback:** exit test scene, delete only `ARENA17_CLEARANCELADDER001.json` inside disposable profile and discard test profile. No canonical game source files changed, no integration patch to revert here. Future Codex integration revert is exclusively his responsibility.

**Source pins when authored:** Codex #310 be2c4b6ac63e04eff5b046e50641ec8d1da4837f; MapDraft.cs Git blob 0331a54a1545ecd1ea18b616ef13fed113474b8c; MapWorld.cs Git blob 8928908db123b35dcab4b324fe9d3a7a8fceee4e. Codex current SHA may change; do not test on assumed head.

**Gates:** SOURCE_TESTED / **NODE_CLI_WINDOWS+UBUNTU_PASS (run 40758466800)** / CHROMIUM_NOT_RUN / UNITY_NOT_RUN / INDEPENDENT_QA_PENDING / CODEX_ACK_PENDING / NO_MERGE / NOT_IN_GAME. Parallel CI-02/CI-03 Unity workflows on this branch failed at **Require valid Unity activation credentials**, before native tests, not at our Node/map assertions.
