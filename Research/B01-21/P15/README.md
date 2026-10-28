# B01-21 / PASS15 — LD02_ARENA_SEALED_CORNERS (independent original-format map)

**Product:** `LD02_ARENA_SEALED_CORNERS.json` is a genuine `MapDocument v2` level-data alternative, **not** a new Unity package or an edit to B01-25-owned source. Current state: `DATA_READY / ORIGINAL_CSHARP_COMPATIBILITY_TESTED / UNITY_PHYSX_NOT_RUN / NOT_INTEGRATED`.

## Input identity and why the correction exists
- B01-25 original arena `Research/B01-25/P14/LD02_ARENA.json`, source commit `eecdc5ab9f20cad2f47dd7a139c9cb96981d0c3e`, Git blob `6de0d04900a69606ba873f7c29cef75ed6f182cd`, 168 objects.
- Codex editor source: #310 `231005c6d0351945a8ee6684f3856af118f0e94f`, `MapDraft.cs` Git blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb` and `MapWorld.cs` `8928908db123b35dcab4b324fe9d3a7a8fceee4e`.
- **Source-derived physical geometry gap:** Wall `MapWorld.CreateItem` uses local Cube scale `(4m, 4m, 0.5m)`. At each corner B01-25 has a 90° Wall `(0,0),(0,9),(11,0),(11,9)` adjacent to a 0° horizontal Wall `(1,0),(1,9),(10,0),(10,9)`. Their projected solid AABBs have a **1.75m gap at every corner** even though 40 logical cells form a ring. The original wall geometry consequently forms **four separated physical-source components** (one per side), not a continuous ring.
- **Correction:** only the FOUR old corner Wall entries change to `MapItemKind.Platform=6` with `rotation=0,scaleX=1,scaleY=4,scaleZ=1`. Codex `MapWorld` actually constructs Platform as a Cube with local `position=(0,.5,0),scale=(4,1,4)`; with parent `scaleY=4` this becomes a solid **4×4×4m** corner block. Adjacent 4m-long Walls meet these block faces with no 2D AABB gap at original nominal coordinates. Original logical footprint of each Platform remains one cell and original `MapDraft` load accepts it.
- This deliberately uses the available solid Cube primitive; the `Platform` editor icon/appearance may not communicate a defensive corner block. Codex or B01-25 may later choose a dedicated wall corner-piece design **only under appropriate owner/scope authorization**. Physical collision seams, chassis clearance, toppling and appearance still require real Unity/PhysX.

## Exact change invariants

| Metric | Original | Corrected |
| --- | ---: | ---: |
| Map items | 168 | 168 |
| Local Ground / 12 × 10 cells | 120 | 120 |
| Wall pieces | 40 | 36 |
| Solid 4×4×4m corner Platform | 0 | 4 |
| Pillar, LowBarrier, Spawn, Finish | 4,2,1,1 | 4,2,1,1 |
| Start / Finish cells | (2,5) / (9,5) | unchanged |
| Source projected corner gaps | 4 × 1.75m | 0 |
| Physical-source wall-perimeter connected components | 4 | 1 |

Other 164 entries are unchanged in identity and order. This is a *source-data design correction*; the QA geometry oracle is explicitly source-derived and not a native physics trace.

## Reproduction / Codex CV-05 acceptance

1. Verify original SHA `#310` and file Git blob before running. Use **a disposable empty sandbox** `Application.persistentDataPath/Maps`. Copy the exact JSON to a previously absent unique file `B01_21_LD02_SEALED_P15.json`. Do not overwrite user saves; Codex D02 save collision remains open. In the editor set `EditorName=B01_21_LD02_SEALED_P15`, press **Wczytaj**.
2. **PASS import:** original engine `MapDraft.TryLoad=true`, `TestError=null`; `globalFloor=false`; exactly the expected 168 items and 4 tall Platform corner cubes; Spawn (2,5), Finish (9,5).
3. **PASS source geometry:** all 40 perimeter solid colliders are non-trigger, bottom at floor and top at approximately 4m, with continuous end-to-end corner contacts. Sample four physical corner raycasts including **diagonal escape paths**, not only cardinal side-wall hits. Record actual `Collider.bounds`, source/fixture hashes and before/after screenshots.
4. **PASS actual physical containment:** separately drive a physically assembled Light and Heavy rig toward all four corners and seams without teleporting, measure position/contacts and whether chassis can cross. If any escapes or wall-intersection jank occurs, **FAIL** and send raw evidence to Codex; do not call geometric contact proof a real drive PASS.
5. UI and VFX model of Platform-as-buttress needs a visual owner acceptance; no new architecture/weapon goals, victory/reward or route-certification policy implied. Current editor Finish trigger is only sandbox touch.
6. Evidence: genuine Unity version/activation, exact game/map SHA, Editor.log, NUnit XML / GUI video / Player if available, real timestamps and Physics trace, per-corner result. If unavailable `NATIVE_NOT_RUN / CODEX_ACK_PENDING`.

**Original C# QA runner:** [B01-21 source shim harness](https://github.com/C8LyNn/RoboRougelike/tree/qa/b01-21-p13-mapdraft-original-csharp/Research/B01-21/P13) loads both true immutable JSON files with **unmodified actual Codex `MapDraft.TryLoad/TestError`** and source-shape-pinned AABBs. Its `P15_LD02_ORIGINAL_CSHARP_LOAD_AND_PHYSICAL_SOURCE_CORNER_SEALS` test rejects old 4 disconnected source components/1.75m gaps and accepts new 1 connected source component/0 gap. GitHub Actions Windows + Ubuntu is original C# logic with Unity type/JSON shims, **not real Unity**.

**Ownership:** original B01-25 arena remains immutable/untouched. This optional alternative exists in B01-21 research branch for B01-25 review; Codex exclusively owns integrated game changes; HQ G0/G1/G2 and finish policy preserved. **Do not merge automatically.**
