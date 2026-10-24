# MAPGEN001_INDEPENDENT_QA — B01-60 / P0

**Audit date:** 2026-10-24 (Europe/Warsaw)  
**Issue:** [#93](https://github.com/C8LyNn/RoboRougelike/issues/93)  
**Producer PR:** [Draft #103](https://github.com/C8LyNn/RoboRougelike/pull/103) (stacked on Draft #99)  
**Original exact source audited:** `a2993a4cb09275864a04c6eaf8bc7b9f4dfb9946`  
**Subsequent exact source independently re-reviewed:** `e22906f9c973a632ca92815a964fadfbe1790665`  
**Production branch edited by QA:** **NO**. This report lives on an isolated QA branch.  
**Overall verdict: BLOCKED** — native Unity execution unavailable; source/static defects and high-priority reference-model failures require producer investigation. **NOT G1 PASS, NOT G2 PASS, NOT READY_FOR_GPT_OSS.**

## 1. Evidence provenance and execution status

| Activity | Actually performed? | Evidence / outcome |
|---|---|---|
| Fetch exact PR HEAD | **YES** | Initially `a2993a4...`, then HEAD advanced seven commits to `e22906f...` while QA was running |
| Inspect planner, route validator, builder, demo, test sources, docs | **YES** | Read files from both immutable Git SHAs; checked source-change scope of the seven intervening commits |
| Git tree vs content manifest (old SHA) | **YES** | 42/42 manifest entries match `a2993a4...`; source .meta pairing inspected |
| Git tree vs content manifest (new SHA) | **YES** | **6/42 mismatches** on `e22906f...` — exact files listed below |
| Source-derived *Python reference probe*: 100 seeds x 2 recipes x 2 profiles | **YES, surrogate only** | 400 reference cases at each source revision; 21 Heavy/Course clearance rejects in the independent model; NOT C# execution |
| Reference-model determinism / diversity count | **YES, surrogate only** | 100 distinct geometry representations/recipe for seeds 0..99 at default parameters in the Python model; NOT Unity PASS |
| Reference-model density=0 check | **YES, surrogate only** | 11/100 arena seeds and 4/100 course seeds unexpectedly generate a Cover/Obstacle; source-level off-by-one confirms risk |
| Unity 6000.3.14f1 C# compile | **NOT RUN / BLOCKED** | Unity Editor + required activation not available to this QA execution environment |
| Native Unity EditMode 100-seed tests | **NOT RUN / BLOCKED** | No NUnit XML, editor.log or native process exit |
| Native Unity PlayMode physics, colliders and regeneration | **NOT RUN / BLOCKED** | No Unity runtime, PhysX outcomes, native process exit or XML |
| Physical driving minimum 12 trials / manual playtesting | **NOT RUN / BLOCKED** | **0 actual vehicle runs**, no measured traversal times, stuck/fall counts or video/screenshots |
| Unity Windows player / actual game vehicle integration | **NOT RUN / OUT OF SCOPE** | Host integration belongs to GPT OSS after a separate G2 |

The Python probe is a hand-maintained, source-derived reference model of `System.Random`, planner layout and routing predicates. It is valuable for reproducible defect hypotheses; it **does not execute the producer's C# code**, and cannot certify the Unity tests. Results must be independently reproduced with the C# build before closing a defect. The local QA evidence script and machine-readable 400-case logs are supplied separately alongside this report.

## 2. Findings (FAIL -> B01-20 rework; latest source SHA `e22906f...`)

### QAMAP-001 — P0 candidate: default Heavy course can be rejected as geometrically blocked

**Evidence type:** source-derived, executable Python reference reproduction on **both** exact SHAs; **native Unity confirmation required**.  
**Config:** `ObstacleCourse`, `MapGenConfig.Default` (Segments=4, ObstacleDensity=50, Difficulty=0.5, LaneWidth=16m), `MapVehicleProfile.Heavy` (Width=4.5m, Length=5.8m, MinTurnRadius=6m, MaxSlope=10°).  
**Suspect seeds 0..99:** `2, 3, 7, 9, 12, 14, 29, 31, 36, 40, 41, 44, 53, 58, 63, 68, 80, 84, 85, 87, 90` (**21/100**).  
**Light profile:** no default seed failed the reference model. **CombatArena:** no default Light/Heavy seed failed the reference model.

**Expected:** documented reference vehicle generates a valid default map for each seed, or takes a bounded, deterministic recovery path and records rejected seed/config.  
**Reference-model observation:** `SPINE_FOOTPRINT_BLOCKED`; `MapGenPlanner.Plan` would throw `ArgumentException("IMPOSSIBLE_GENERATED_ROUTE: SPINE_FOOTPRINT_BLOCKED")` if C# produces these layouts. **No actual C# exception was observed in QA.**  
**Geometric trace for seed 7 (reference model):** at sampled route XZ approximately `(-0.3182,16.3939)` the ramp is centered at `(3.3000,23.0000)`. Vehicle-dilated overlap threshold on X = `4.0000m`, separation `3.6182m`; Z threshold approximately `6.651m`, separation `6.6061m`. Thus both overlap inequalities in `MapGenRouteValidator.IsFree` are true. Seed 31 mirrors the ramp and route laterally. The source only reserves free width around each *segment* center, not necessarily along an interpolated centreline between displaced consecutive segments.

**How to reproduce in native C#:** checkout `e22906f...`, open Unity 6000.3.14f1, call `MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 7, MapGenConfig.Default, MapVehicleProfile.Heavy)` and repeat for seed `31`; capture full NUnit assertion/exception and Unity editor.log. Execute the 100-seed reference sweep, do not silently catch/retry failures. The authored `HundredSeedsPassReferenceVehicleSweepsAndConnectivity` and `LightAndHeavyRigidbodyDriveThreeSeedsForBothRecipes` include these values but have **not** run in this QA environment.  
**Owner:** B01-20. Repair the actual lane/ramp clearance or generate and certify a continuous usable bypass; seed-specific hacks are not sufficient. New commit SHA + native rerun required.

### QAMAP-002 — P1: density=0 has nonzero obstacle probability

**Evidence type:** direct source inspection + Python reference negative sweep.  
**Source:** `MapGenPlanner.MakeArena`: `if (rng.Next(100) > config.ObstacleDensity) continue;`; `MakeCourse`: `if (rng.Next(100) <= config.ObstacleDensity)`. Both permit random draw `0` when density=0.  
**Expected:** a density parameter of zero removes all optional `Cover`/`Obstacle` primitives (mandatory course ramp may remain by design).  
**Reference-model observed seeds:** arena `37,39,44,59,64,66,73,76,85,86,87`; course `63,82,89,97`, using density=0, remaining config defaults.  
**Native reproduction:** `MapGenPlanner.Plan` with density=0, assert no optional `Cover`/`Obstacle` for 100+ seeds; run C# sweep and log failures.  
**Suggested fix:** use a consistent probability predicate `rng.Next(100) < config.ObstacleDensity` for appearance (preserve mandatory ramp semantics).

### QAMAP-003 — P1: vertical ground support not validated for arbitrary public blueprints

**Evidence type:** source inspection + source-derived reference counterexample; native test absent.  
**Source:** `MapGenRouteValidator.IsFree(x,z)` and `FloorAt(x,z)` use XZ only. Start, finish and enemy spawn `y` values are not checked against collider/floor elevation.  
**Expected:** a start 25m above the arena floor (or below it) is rejected as unsupported before returning `Passed=true`.  
**Reference-model observation:** arena blueprint from seed 7 remains `BFS_SPINE_CLEARANCE_OK` when start Y alone is changed from 1.25 to 25, since the XZ centerline remains clear.  
**Reproduce:** mutate `blueprint.Start = new Vector3(blueprint.Start.x,25f,blueprint.Start.z)`; call `Validate`. This is primarily a validator API robustness gap; stock `MakeArena` does not presently generate such a start.  
**Fix/test:** validate start/finish/spawn height against expected supporting floor + allowed suspension clearance, reject NaN/Infinity for all blueprint coordinate components, add a negative EditMode test.

### QAMAP-004 — P1: physical traversal test can hide subsequent failures and leave a rover

**Evidence type:** direct source inspection, **not executed**.  
`MapGenPhysicalTraversalTests.LightAndHeavyRigidbodyDriveThreeSeedsForBothRecipes` places all 12 trials inside **one** `[UnityTest]`. On a first failed assertion it aborts before `Object.Destroy(rover)`, while the `finally` destroys only `host`; the rover is a separate root object. No per-trial elapsed time, collision counts, stuck duration or screenshots are produced.  
**Expected:** each recipe/seed/profile records independent outcome and always destroys its rover in a per-case `finally` (including exception/failures), with physical metrics.  
**Reproduce:** force one traversal failure or use a seed known to reject in `Generate`, verify whether the remaining scenarios run and whether the rover still exists at end-of-frame.  
**Fix:** parameterize cases or aggregate failures without aborting, per-trial `try/finally` cleanup, structured telemetry; distinguish source `Rigidbody` cube from actual wheel-driven game robots.

### QAMAP-005 — P1: content identity manifest stale after R2->later SHA change

**Evidence type:** direct **Git tree PASS/FAIL comparison**, no Unity dependency.  
At original `a2993a4...`, **42/42** `manifest.json.git_blob_sha1_by_path` entries match Git tree. At `e22906f...`, **6/42** are stale:  
- `Runtime/MapGenBuilder.cs`
- `Runtime/MapGenDemo.cs`
- `Runtime/MapGenPlanner.cs`
- `Runtime/MapGenRouteValidator.cs`
- `Tests/EditMode/MapGenRouteValidationTests.cs`
- `Tests/PlayMode/MapGenPlayModeTests.cs`

All paths are relative to `Assets/B01Packages/MAPGEN001/`.  
**Expected:** the manifest represents the exact candidate tree; distinct Git blob SHA-1 identities are not physical ZIP SHA-256 digests.  
**Actual:** the manifest still records six pre-change blob IDs. `sha256_release_checksums` and `physical_zip_url` remain null.  
**Owners:** B01-20 must update provenance after source fixes; B01-40 must independently validate exact tree, unique GUIDs, physical artifact SHA-256, import/rollback and target scope ACK before handoff.

### QAMAP-006 — P1 evidence gap / P2 gameplay risk

`ObstacleCourse` generally reserves a flat central bypass; side slalom obstacles and ramps need not be driven through. Increased `Difficulty` alone does not demonstrate increased mandatory traversal demands. This is **not** a measured gameplay FAIL: quantify path length, obstacle/ramp usage, collisions and completion times for fixed seeds and both profiles in actual playtests. The standalone demo now adds a follow camera and damping at `e22906f...`, but there is still no real footage and the demo Rigidbody cube is not the final robot.

## 3. Negative-test status (distinguish source from execution)

| Negative | Authored test / source behavior | Independent native result |
|---|---|---|
| Impossible Heavy clearance/profile | `CheckConfig` rejects oversize profiles | **NOT RUN** |
| Blocked arena | Added obstacle across spine; expected `SPINE_FOOTPRINT_BLOCKED` | **NOT RUN** |
| Start over a cliff in XZ | Source test moves start Z to 1m, expected blocked | **NOT RUN** |
| Start with invalid height Y | **Not guarded** by existing XZ validator, QAMAP-003 | **NOT RUN** |
| Excessive turn radius / sharp route | Finite fillet-radius guard in latest SHA | **NOT RUN** |
| Ramp too steep | `RAMP_EXCEEDS_PROFILE_SLOPE` guard | **NOT RUN** |
| NaN slope/config | Source `ArgumentException` guard | **NOT RUN** |
| Three regeneration/clear cycles | Test source added at later SHA | **NOT RUN** |

## 4. Mandatory native evidence to unblock G1/G2

1. Pin and report a **new exact producer SHA**; all prior results are version-specific.
2. Run Unity **6000.3.14f1** project compile and complete EditMode/PlayMode suites including 100 seeds/recipe **for Light and Heavy**. Keep raw `editor.log`, NUnit XML, exit code, duration, failed seed/config, and video/screenshot evidence.
3. Execute **12+ actual** start-to-finish physical trials (3 seeds x 2 recipes x 2 reference profiles), record `PASS / STUCK / COLLISION / FALL / TIME` per case. A passing artificial Rigidbody cube suite is not sufficient evidence for the integrated wheel-driven rover.
4. Test fallback/rejection, invalid configuration, obstacle collisions, ramps, boundaries, 3+ regenerations, repeated seed reproducibility, scene cleanup and camera readability.
5. Resolve QAMAP-001 with targeted C# verification **before** attributing its source-derived prediction to Unity; fix verified failures in isolated B01-20 source and rerun all essential regressions on **new SHA**. Independently recheck refreshed manifest.
6. B01-40 performs dependency, GUID, SHA-256, ZIP and target ownership checks; B01-00 alone decides G2 and GPT OSS handoff. **NO MERGE / NO RELEASE / NO GAME INTEGRATION.**

## 5. Routing and gate recommendation

**To B01-20:** prioritize `QAMAP-001` seed 7/31 native reproduction and geometric fix; resolve density=0, validator height and traversal-harness cleanup; update manifest after all changes and provide immutable SHA, raw tests and full logs.  
**To B01-40:** independently fix/check G0 packaging and manifest mismatch; do not interpret Git blob SHA-1 as release SHA-256.  
**To B01-00:** retain **P0 / G1 BLOCKED / G2 HOLD / NO READY_FOR_GPT_OSS**. No claims of Unity PASS, playable vehicle routes or 12 manual attempts are justified by available evidence.

**Formal B01-60 verdict: `BLOCKED`** (native Unity unavailable) **with an independently demonstrated manifest FAIL and reproducible source-derived P0 risk; no release approval.**
