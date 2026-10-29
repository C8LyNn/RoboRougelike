# B01-50 / BROADPHASE-001 R1 — continuous XZ projectile/part broadphase

**Status:** executable standalone **research/profiling algorithm**, NOT integrated into RoboRougelike. Producer B01-50. No edits to canonical `Assets/**`, CHL, PARTSIZE, Codex or Unity packages. This is **not** a new Unity gameplay implementation package.

## Purpose

Prototype a deterministic candidate filter for many projectile line segments against many circular part proxies. Baseline tests every projectile–part pair; grid index inserts each target disk into all overlapped XZ cells, queries projectile swept bounding boxes, and uses **the same exact continuous-sweep quadratic narrowphase** for both. First hit is selected by earliest normalized `t`, with stable ID tie-break. Moving target sets must **rebuild the index**; this is a static-per-tick proof, NOT full real-time moving-world/rigidbody detection.

No claims about the performance of the actual game or its P12/R4 collision code. All geometries and timings here are **SYNTHETIC**. A real hotspot must be established in licensed Unity/Windows per existing CV-12 before Codex considers any optimization or native port. `2/6/12-gun` are experiment labels, NOT live equipped guns or source-derived fire rates.

## Reproduce (Node.js 18+, no dependencies or downloads)

```sh
cd Tools/B01/B01-50/BROADPHASE001
node --test tests/broadphase.test.mjs
node benchmark.mjs --seed 31 --runs 3 --out benchmark-output.json
```

The CLI rejects overwriting its output. To repeat, use a fresh output path. Verify JSON `classification=SYNTHETIC_2D_REFERENCE_BENCHMARK_NOT_GAME_PROFILING` and each `hitMatches==shots`, not the varying wall-clock time as a Unity claim. `benchmark.mjs` prevalidates targets before timing the baseline, times grid build separately from query, and includes both in `optimized` work. Timings are subject to Node JIT, GC, CPU load, warmup and browser/runtime differences; numeric speedups are *not* an acceptance criterion.

## Tested original scenarios

- Light: 24 synthetic parts per enemy ×8 enemy clusters; 64 moving disk projectiles.
- Mid: 72 parts ×16; 256 projectiles.
- Heavy: 144 parts ×32; 1024 projectiles.
- 13 local Node tests PASS (11 original + 2 R2 first-contact precision regressions): continuous first-contact parity, tangencies, overlap, degenerate shots, negative/grid-boundary coordinates, duplicate/invalid inputs, large-target deduplication, 36 seeded configurations ×120 shots, 9 moving target snapshots, initial validation parity, checked-pair counter.
- Original `evidence/js_benchmark_seed31_r1.json` contains **9 executed JS measurements** (3 configs ×3 repetitions) and exact logical pair counts. No actual game frames, Unity Profiler samples, FPS, GPU, GC or PhysX solver data exist in this package.

## Input/ownership and handoff

**Input:** current Codex gameplay owner [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) observed while authoring; exact code was **not copied or modified**. Existing standalone performance intake [PERFSCALING-001 #328](https://github.com/C8LyNn/RoboRougelike/pull/328) measures different proxy PhysX cost; [PERFVIEW-001 #343](https://github.com/C8LyNn/RoboRougelike/pull/343) displays true-format trace files; this package instead supplies a *collision candidate selection algorithm plus reproducible CPU work counts*.

**Install for Codex:** pull/download this Tools-only directory and run two Node commands above; do not copy files into canonical Unity source without separate product integration approval. If a measured real collision-query hotspot is identified, Codex may implement its own C# equivalent and run real Unity tests to verify identical impact ID/time, interactions with 3D colliders, armor, moving Rigidbody bodies, layers, explosions, friendly fire, and pool lifecycle. The 2D disk reference is not a plug-in replacement for game PhysX.

**Rollback:** remove the isolated `Tools/B01/B01-50/BROADPHASE001` directory or close its Draft PR. Zero persistent game, save, scenes or build changes. Independent receiver: B01-40 scope/manifest intake → B01-60 source/test replay → B01-00 handoff ordering → Codex explicit ACK if accepted. Game native `NOT_RUN`, Codex integration `NOT_STARTED`, HQ G1/G2 `HOLD`.

## R2 / PASS17 WAVE03 — verified original-source correctness fix

R1 immutable commit `3104370c99dce19253085a75589858975ca5f79d` selected the **wrong first hit** when two geometrically different impact times were separated by less than `1e-10`: target ID 99 at `t=0.4` was superseded by later ID 1 at `t≈0.40000000005`. This was a genuine algorithm correctness defect, not a benchmark variance. R2 chooses strictly earlier `t` regardless of how close it is to the alternative; ID ordering only breaks **exact** equal-time ties. `validateSameHits` now rejects tiny time mismatches instead of falsely accepting them.

**Reproduced in original R1 code with the two added tests:** 11 PASS / 2 FAIL; source-fix + same tests **13 PASS / 0 FAIL**. See `evidence/W03_R2_red_green_receipt.json`; rerunning the 9 controlled Node benchmarks preserved all 9 hit and candidate-count results. This R2 has no native Unity/Player acceptance and no Codex integration authorization.

**QA intake:** verify original R2 `broadphase.mjs` and `tests/broadphase.test.mjs` Git blob SHAs, execute `node --test tests/broadphase.test.mjs` plus `node benchmark.mjs --seed 31 --runs 3 --out <new-file>`, compare stable integer counters and hits to the prior R1 receipt (do not require matching wall-clock timings), verify manifest, and submit an independent result to B01-00. Do not treat R1 or R2 as `CODEX_CLAIMED` or native 3D physics.
