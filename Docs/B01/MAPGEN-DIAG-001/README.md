# MAPGEN-DIAG-001 — B01-21 (diagnostic-only)
**Original generator remains owned by B01-20.** Git base R3.2 frozen `003bced54480d509ffc0d49d26daf87160a8d609` (full stacked PR #99+#103+#211). Issue #223, parent #93.

Run from repository root with .NET 8:
```bash
dotnet run --project Tools/B01/MAPGEN-DIAG-001/MapGen.Diag.csproj --configuration Release -- "$PWD" "$PWD/mapgen-diag-001-evidence"
```

Compiles the **real unchanged** MapGenData.cs, MapGenPlanner.cs and MapGenRouteValidator.cs through the **already existing** Tools/B01/MAPGEN001/Headless/UnitySurrogate.cs. It tests new orthogonal properties, **not a rerun** of the 1013-case harness: exact native failure fixture, a new supported-endpoint centre-hole negative, independent 12-source scenarios, 7 Heavy suspects, 5 Light narrow-course regressions, physical floor seam/first-goal geometry, and PlayMode harness early-abort detection. Each row writes its own JSON, plus results.json/cases.md. Expected native/R6 status remains NOT_RUN.

CI: .github/workflows/b01-mapgen-diag-001.yml, original MAPGEN source Git blobs asserted, upload artifacts with `if: always()`. A headless source PASS certifies no PhysX, steering, wheel grip, seam traversal, 3D geometry collisions or manual drive. The 12 R6_PENDING rows are **planner geometry checks only** and are still physically NOT_RUN.

B01-60 must independently replay revised Unity tests on real PR125+OBJ210 wheels before G1. No source change/merge/integration authorized by this diagnostics PR.
