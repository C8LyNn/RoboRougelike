# B01-21 / PASS 16 — MAPGEN-ARENA-P16

**Deliverable:** three new deterministic `CombatArena` layout **requests** plus generated original-MAPGEN001 blueprint JSON and matching orthographic SVG previews. This is a new **B01-21 research/level-data package** using the already existing MAPGEN producer and MAPGENWORLD contract; it is **not** a new game generator, Unity Scene, prefab, canonical game package, or implemented combat encounter.

## Contract / immutable producer source
- Original Codex integrated source snapshot [#310 at `589b60a912096d1e9917cb0bbac73a53e7543b4f`](https://github.com/C8LyNn/RoboRougelike/tree/589b60a912096d1e9917cb0bbac73a53e7543b4f/Assets/B01Packages).
- Actual source `B01.MAPGEN001.MapGenPlanner.Plan(MapRecipe.CombatArena, seed, MapGenConfig, MapVehicleProfile)`, `MapGenRouteValidator.Validate`, `B01.MAPGENWORLD001.MapWorldPolicy.TryPlan`, and `MapWorldReplayCodec.Encode/TryDecode`, source identity checked using Git SHA1 blobs in [own workflow](../../../../.github/workflows/b01-21-p16-three-arenas.yml). **No `Assets/**` modified.**
- `arena_requests.json` is an explicit **test/authoring manifest** for these C# types; the shipped producer **does not have a JSON importer for this file**. A Codex adopter must construct `MapWorldRequest` using the present contract or add an approved data ingestion path on the Codex-owned integration side.
- Earlier B01 W12 measured **historical reference** profiles Light width 2.980m and Heavy width 5.180m with the original `MapVehicleProfile` reference lengths/radii; `ClearancePerSideMeters=0.25`. This QA reuses **historical reference envelopes**; it did not remeasure any current player/robot. In production `EnvelopeMeasured=true` must come only from actual current assembled rig measurements. This research manifest is not allowed to silently authorize unknown vehicle geometry.

## Three different reproducible variants

| ID | Original recipe | Seed | Density | Difficulty | Design intent |
| --- | --- | ---: | ---: | ---: | --- |
| `AR16_01_OPEN_FLANK` | CombatArena | 160101 | 40% | .20 | Open approach with some side cover |
| `AR16_02_SPLIT_DECISION` | CombatArena | 160202 | 55% | .55 | Alternating side cover, protected centre |
| `AR16_03_DENSE_PRESSURE` | CombatArena | 160303 | 100% | .85 | All eligible side cover slots, protected escape avenue |

Existing original recipe fixes the arena's 64×64m floor, 4 boundaries, 12m central avenue, 3 *descriptors* for enemy spawn and Start=(0,1.25,-25), Finish=(0,1.25,25). **No real enemy AI instance, kill reward, branching mission goal, or Light/Heavy physics guaranteed.** Density influences authored cover probability, not a fixed number of covers.

## Reproducibility and actual test execution

```powershell
dotnet run --project Research/B01-21/P16/MapgenArenas.csproj -c Release
```

This compiles and executes **unchanged original C# methods**, linked directly from `Assets/B01Packages/MAPGEN001` and `MAPGENWORLD001`. The deliberately labeled `UnityMathShim.cs` provides only managed Vec2/Vec3/Quaternion/Bounds/Mathf for a host lacking the original engine. Three variants run against both historically referenced Light and Heavy profiles: original source policy acceptance, real source route validator with 3×3 floor support and 1m BFS, repeated identical geometry hashes, disjoint variant hashes, replay encode→decode, corruption fail-closed, envelope-unmeasured negative. A failing mandatory check exits nonzero.

Each successful original planner run writes **seven inspectable files** into `generated/`: for each of the three variants, a blueprint JSON containing the exact generated `MapElement` geometry + source `GeometryHash` + actual original replay receipts, and a top-down geometry SVG generated from the same C# `MapBlueprint`; plus `generated_manifest.json`. On subsequent runs files must regenerate **byte-identically**, or the runner fails. The original Git blobs and CI logs define provenance.

**Strict evidence classification:** `ACTUAL_ORIGINAL_MAPGEN_CSHARP_WITH_UNITY_MATH_SHIM` is source-level preflight. It does NOT mean Unity loaded the scene, applied BoxCollider/MeshCollider, visually rendered URP, ran PhysX wheel clearance, spawned actual enemy AI or produced G1/G2 certification. The route validator itself explicitly cautions that the checked 4-neighbour grid and spline cannot prove swept-wheel traversal.

## Actual PASS16 production receipt

Three genuinely generated **different** originals are committed under `generated/`; no hand-drawn placeholder:
- `AR16_01_OPEN_FLANK` / seed 160101 / config density 40%, difficulty 0.20 / **6** source `Cover` elements / geometry `30fb147b8ea3b4eaa9a7f300fcfa093f55ab0d340ad301c5a659c7e14c7b878c`.
- `AR16_02_SPLIT_DECISION` / seed 160202 / density 55%, difficulty 0.55 / **11** `Cover` / geometry `5cfc45a3308cf5d15b48ab9fd01eb5ae2c3b292bc44b97d5a158a9dcc870048d`.
- `AR16_03_DENSE_PRESSURE` / seed 160303 / density 100%, difficulty 0.85 / **16** `Cover` / geometry `a797f6fc20a6be9da0e0eb7cc316d1bf3f67ca8ca42362a9bb2f990c635d1b98`.

**Producer-run correction:** the first sparse attempt at 20% happened to produce 0 cover pieces and 85% was only 12. Instead of counting that as successful differentiation, B01-21 changed **only owned request parameters** (40%/100%) and regenerated every source-derived output. The final 6 / 11 / 16 steps are real original MAPGEN geometry, not nominal density values. Also corrected a real local exporter Windows CRLF/Ubuntu LF byte drift by normalizing generated text; these exact seven final committed files were proven **identical across both OS outputs** and **byte-identical after re-running against the committed files**.

**Inspect locally without Unity:** [arena_comparison.html](arena_comparison.html) is a standalone working keyboard-accessible browser preview which loads exactly the three generated source SVG files and their blueprint JSON links. Buttons select variants; arrow keys switch selection; actual seed/config/geometry hash and cover counts update. A real automated headless Chromium DOM/image/keyboard self-check executes when opened with `?selftest=1`; the repo's [GitHub Actions workflow](../../../../.github/workflows/b01-21-p16-three-arenas.yml) records 720p and 1080p screenshots as artifacts. Browser acceptance is **not** Unity/PhysX acceptance.

## Codex native receiver (join existing CV-05 / MAPGEN queue, no duplicate task)

1. On an HQ-approved host running actual licensed Unity 6000.3.14f1, exact game/build SHA documented, take a disposable isolated scene; use the current legitimate `MapGenBuilder` and `MapGenWorldAdapter.TryGenerate` with these seed/config pairs and a **fresh measured current rig envelope**, not the historical widths by assumption. Verify original `MapGenPlanner.Plan` geometry hash against committed blueprint **only if the envelope-independent same generator version has not changed**. If changed, STOP/REBASE; do not claim old geometry result.
2. For each request test generation twice and `TryReplay` the actual receipt, stable original `GeometryHash` and correct root/bounds/three spawn descriptors. Test invalid checksum and impossible vehicle on an active previous map: **must not destroy previous map**, record exact failure code. Don't infer playability from hash.
3. Drive actual assembled Light and Heavy with wheels: front bumper/body swept footprint on central corridor, at least two side-cover maneuvers, wall and cover collision, physical start-to-finish continuity and accessible target descriptions. Report if any cannot navigate; **no teleports**. Separately evaluate actual enemy AI/combat and finish/mission semantics; these are not included in MAPGEN source data.
4. **PASS** only for scenarios actually run and evidenced with true Unity project source hash, version/license, `Editor.log`, NUnit Edit+Play XML and exit codes, timecoded Player drive capture/rig dimensions/collision contact trace. **FAIL** if original generated geometry is inconsistent, replay differs, actual vehicle clips/cannot pass, or original scene loses existing live map on rejected request. Otherwise `NOT_RUN`.
5. B01-40 staging/compatibility and independent B01-60 G1 review where authorized, **Codex only** integrates canonical game. No automatic merge/release, no CHL/PART producer edits. Existing #79 queue and #323 CX-08/CX-09 route future Codex action; merely posting package does not ACK it.

**Package status:** `RESEARCH_CONTENT_SOURCE_READY` until CI and generated files exist; `NATIVE_UNITY_NOT_RUN`; independent QA is separate from author success.
