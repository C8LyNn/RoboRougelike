# B01-21 / DEPENDENCY-AWARE QA: C-DQ / CHL gate current-source response

**TASK_ID:** `B01-21-C-DQ-NEWHEAD-GATE-20261027`
**OWNER:** B01-21 (read-only source support)
**INPUT_ARTIFACT_URI@SHA:** [B01-20 CHL PR #285](https://github.com/C8LyNn/RoboRougelike/pull/285) original source exact producer `74b6af45d71297c2b1fc50a19152fb6771a5bcf0`; previous historical producer `f4ca318577edbdf4d003ab3e104ac7ec71d83cfe`.
**REQUIRES:** documented prior [B01-21 W17 QA PR #295](https://github.com/C8LyNn/RoboRougelike/pull/295) source evidence `cadc35dca5e5e8cb2cd6595f7e860b0c898ef8ec` at old producer `9a947108...`, DONE/SOURCE; latest producer source original verified from Git blob SHA. Not dependent on B01-60 or Codex.
**OWNED_PATHS:** exclusively `Tools/B01/CHL003WorldQA/**` and own B01-21 source QA workflows; no CHL/Partsize/Codex source edit.
**QA_KIND:** REAL compiled ORIGINAL CHL C# production gate policy, NOT Unity PhysX, wheels or 3×2 traversal.

## Reactive actual finding: original defect FIXED by exclusive owner (not OPEN)
B01-20 read source feedback at [PR #295](https://github.com/C8LyNn/RoboRougelike/pull/295) and added [dependency C1 owner receipt](https://github.com/C8LyNn/RoboRougelike/blob/74b6af45d71297c2b1fc50a19152fb6771a5bcf0/Docs/B01/CHL003/DEPENDENCY_C1_HEAVY_YAW8.md). Owner's new original CHL demo `CHL003DemoBootstrap.cs` blob SHA `9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e` **actually widened all four checkpoint and finish trigger volumes from 3.50m to 3.70m**, physically matching the already 3.70m bridge and -12° ramp width; posts now at X±1.97m. Original producer policy class `CHL003GateCrossing.cs` is still immutable Git blob `5c736da620487e48131f6d454bcf231ae368b513`, requiring actual body full-envelope clearance0.25m per side. We are NOT asking B01-20 to fix the prior yaw8 issue again.

### Test on actual original production C# policy
The `GateGeometryReceipt.csproj` compiles *directly* the existing CHL runtime file `Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003GateCrossing.cs` (not a fake copy). The test runner independently reconstructs 2D analytical rigid original wheel/box AABB from original CHL demo geometry (Heavy chassis 2.5×3.4m and wheels diameter.82 at ±1.0/±1.15m; Light chassis1.45×2.30 wheels±.59/±.75). This tests real C# predicate for artificially supplied **ground-truth source-geometric** localMinX/localMaxX; it does **not** invoke Unity Physics, actual `Collider.bounds`, wheels, real OnTriggerEnter or G1.

| Original same CHL production method | Historical gate3.50m | Current owner gate3.70m |
|---|---|---|
| Heavy straight yaw0°, root center offset **+0.10m** | REJECT (guard per-side minimum only .24m) | **ACCEPT** (.34m) |
| Heavy centered yaw5° | REJECT | **ACCEPT** |
| Heavy centered yaw8° | REJECT (.1897m margin) | **ACCEPT** (.2897m margin) |
| Heavy centered yaw10° | REJECT (.1555m margin) | **ACCEPT** (.2555m margin) |
| Heavy centered yaw12° | REJECT | **REJECT** (same valid .25m guard still enforced) |
| Heavy yaw8° offset+0.03m | historical control not needed | **ACCEPT** (.2597m remaining) |
| Heavy yaw8° offset+0.05m | historical control not needed | **REJECT** (.2397m remaining) |

Thus the prior P0 source geometry mismatch is **SOURCE_FIXED**, not NATIVE_ACCEPTED. Gate-aligned bridge has centered Heavy max yaw for 0.25m reserve **10.3296247°**; yaw8 allows only **±0.0397m** center offset, which may be intentional technical driving accuracy. This residual is a **finite and exact new native negative** for B01-60, not a claim any real trial failed.

## Exact execution and outputs
```sh
dotnet run --project Tools/B01/CHL003WorldQA/DependencyQueue/GateGeometryReceipt.csproj -c Release -- /tmp/CHL_GATE_CSHARP_SOURCE.json
B01_21_CHL_DQ_RECEIPT=/tmp/CHL_GATE_CSHARP_SOURCE.json python3 -m unittest discover -s Tools/B01/CHL003WorldQA/DependencyQueue -p 'test_*.py' -v
```
Own CI verifies FIVE original immutable producer file blobs **at the exact current input commit** using `git rev-parse <producer_sha>:path`; source `DemoBootstrap` has new literal `new Vector3(3.70f, 4.5f, .16f)` and visible posts±1.97. The CI then genuinely compiles/calls the original C# policy (**25 source cases** with before/after controls), produces full per-case JSON and runs **15 separate** receipt-integrity tests (one actual genuine receipt, fourteen independent mutants). Runs on GitHub-hosted Ubuntu and Windows. Reporter prohibits claiming `NATIVE_PASS` from pure code. Artifacts contain original tested SHA, source input pin, exact C# log, adversarial verifier log and SHA256 JSON.

## Critical producer CI gate (independent of this source test)
B01-20 current input `74b6af...` source Actions [40488576985](https://github.com/C8LyNn/RoboRougelike/actions/runs/40488576985) actually ran original C# **24,063/24,063 PASS** and printed `CHL003_C1_B01_21_MEASURED_GATE_NEGATIVE_OLD_RED_NEW_GREEN`, **but its overall source workflow FAILED** in full package manifest verification with `RuntimeError: SHA256 manifest stale`. Thus **C1 full source archive candidate is BLOCKED_BY B01-20 exact-HEAD manifest repair** and fresh CI/ZIP; C2 B01-40 latest independent G0 cannot use historical ZIP. Only B01-20 may fix those owned manifest/docs/files. Generic Unity Edit/Play workflows failed activation prerequisite **before Unity launched**, so native Edit19/Play14+3 Light/3 Heavy complete course **NOT_RUN**, C3/B01-60 BLOCKED_BY licensed Unity runner. No OFFERED_FROZEN, no Codex ACK, no merge, no G2.

## NEXT_UNBLOCKS and finite operator action
- B01-20: update manifest on own PR #285, run genuine exact-head source CI fully GREEN and publish matching original physical ZIP then notify B01-40 independent C2. Current W22 source design fix already visible; no duplicate gate widening needed.
- B01-60, only after licensed disposable Unity6000.3.14f1: P14 true **OLD3.5 REJECT → Retry → NEW3.7 ACCEPT** and original whole physical path 3 Light+3 Heavy, contact+time/actual collider bounds XML/log; additional **yaw8+0.05m** offset negative and **yaw8+0.03m** positive; no direct API checkpoint injection or fabricated physics.
- B01-00: C4 G2 strictly BLOCKED until C2 current physical ZIP G0 and C3 Unity native G1 genuine evidence. This read-only QA receipt itself only unblocks **owner source-rule signoff / finite native test specifications**. If producer changes the exact source, mark our inputs `STALE_INPUT` and recompute.

Rollback: delete only the B01-21 `DependencyQueue` subdirectory and own single CI. No Unity/runtime modifications have been made.
