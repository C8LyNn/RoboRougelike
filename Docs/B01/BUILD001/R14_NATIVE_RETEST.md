# BUILD001 R1.4 / W13 — native RC2 contact-area blocker repair

**Source:** stacked BUILD R1.4 on **frozen R1.3** `be54b4a33360cbd1923ae5dacbc92dff8eaf586b` / Draft #256.
**Native RED before repair:** Codex RC2 Draft [#258](https://github.com/C8LyNn/RoboRougelike/pull/258), snapshot `702cb3b2eeba1661f8ca16f6ff75de76ba4ea5e4` and [original receiver fixture](https://github.com/C8LyNn/RoboRougelike/blob/5ef18ff27fc3ae4228acfad1e0f132db7c66c0d9/Tools/B01RC2/Blocked/BuildRC2SafetyTests.cs). Last R1.3 Unity native compile PASS, original 18 EditMode+6 PlayMode PASS, new native safety tests 2 FAIL /1 positive PASS; native G2 HOLD.

## Actual defect and precise correction

R1.3 `BuildSocketGeometry.Touches()` used `aw+bw-|center separation|`, which is **not** the interval intersection for contained rectangles. It elevated valid but physically tiny nested sockets above the 0.01 grid-cells-squared acceptance minimum, and overstated accepted contact area.

R1.4 intersects finite projected intervals independently along both local tangent axes:

```
overlapU = max(0, min(aHalfU, bCentreU+bHalfU)
                  - max(-aHalfU, bCentreU-bHalfU))
overlapV = max(0, min(aHalfV, bCentreV+bHalfV)
                  - max(-aHalfV, bCentreV-bHalfV))
areaM2 = overlapU * overlapV * 0.55^2
```

This fixes nested, unequal, offset, rotated and partial overlap while preserving existing socket type, normal, grid footprint and `MinContactAreaCellsSquared` threshold.

## Acceptance requested of independent native Windows Unity operator

1. In a **disposable Unity6000.3.14f1 checkout**, import entire frozen R1.4 source ZIP, preserve all .meta GUIDs and original connected package dependencies. Require native Editor compilation exit 0 and no new shader error.
2. Run every old W13 R1.3 Unity test: **18 EditMode +6 PlayMode** (same tests as native receiver PASS on source R1.3). Also run **new `Build001R14ContactAreaTests`**. Tests include nested tiny .02 rejected, .2 accepted and .0484m² exact, barely-under/above-threshold .049/.051, unequal partial area, edge-only negative, displaced nested positive, five XYZ quarter-turn fixtures. Record original NUnit XML, Unity Editor.log, exact HEAD SHA, process exit and all case names.
3. Re-run **unchanged receiver native test** `BuildRC2SafetyTests.cs` from #258 (two negative/positive TestCases and explicit area case). Do not edit receiver assertions or relax 0.01 threshold. Fail/stop if any regression.
4. Optional native player demo 5 choices ×720p/1080p and real R3/R6 physical attachment/save/volume acceptance; W13 source-only math is **not** full gameplay admission.
5. Preserve original RED evidence; publish QA-only Draft with exact pinned SHA, raw logs and report. On any native failure **G1 FAIL, G2 HOLD**. No main/develop merge, no live Codex worktree or host installation.

## Source/provenance gate

R1.4 full source ZIP is built via `python3 Tools/B01/BUILD001/package_r14.py` and verified with `python3 Tools/B01/BUILD001/test_package_r13.py` plus `dotnet run --project Tools/B01/BUILD001/Portable/B01.Build001.Portable.csproj -c Release` (surrogate Unity API, **NOT real PhysX/Unity**). Every tracked BUILD source and .meta has SHA256 and byte length in the embedded `PACKAGE_MANIFEST_R13.json` (schema value R14, legacy filename retained for full backward-compatible packager verifier). Rebuild twice deterministically and compare SHA256. Unity native G1 is separate and must not be inferred from these source checks.

**Receiver scope:** B01-60 independent QA, B01-40 package/target compatibility, Codex gameplay only after B01-00 explicit G2. Do not claim APPROVED before those gates.
