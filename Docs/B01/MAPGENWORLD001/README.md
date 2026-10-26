# MAPGEN-WORLD-ADAPTER-001 — B01-21 / W12
**Version 1.0.1, Draft QA-ready source-only; G2 HOLD.** Standalone Unity 6.0 package under `Assets/B01Packages/MAPGENWORLD001/**`, independent of original MAPGEN runtime owner B01-20. No R3/R4/R6/Pack dependency or edits.

## Source dependency / immutable pin
- Required complete producer MAPGEN001 **R3.4.1**, [PR #237](https://github.com/C8LyNn/RoboRougelike/pull/237), exact `5be31039411f89f17654b1c332f8692883fa03cc` (full stacked #99/#103/#211/#225/#235/#237).
- Verified public signatures: `MapGenPlanner.Plan(MapRecipe,int,MapGenConfig,MapVehicleProfile)`, `MapGenBuilder.Generate(...)`, `MapGenBuilder.Clear()`, `MapResult` world-coordinates `Start`, `Finish`, `CheckpointDescriptors`, `Spawns`, `GeometryHash`, `GeneratedRoot`.
- B01-10 [W12 contract PR #240](https://github.com/C8LyNn/RoboRougelike/pull/240) sets measured Light 2.980m, Heavy 5.180m, ≥0.25m lateral margin per side on **main route**; optional side ramp is Light-first and not automatically Heavy-certified.

## Runtime API
- `MapWorldRequest`: Recipe, Seed, Config, **actual measured** `MapVehicleProfile` and `EnvelopeMeasured=true`, `ClearancePerSideMeters>=.25`.
- `MapWorldPolicy.TryPlan(request, out MapBlueprint plan, out MapWorldFailure failure)`: pure deterministic generator preflight, source XZ bounds and protected corridor; never physically drives wheels or mutates scene.
- `MapGenWorldAdapter.TryGenerate(request, out MapWorldSnapshot snapshot, out MapWorldFailure failure)` and `TryRegenerate`: sync preflight **before** invoking unchanged producer `MapGenBuilder.Generate`; on invalid request old scene/snapshot remain; on valid commit revision increments.
- `MapGenWorldAdapter.ClearWorld()`: calls producer Clear, then sends one clear callback; repeated Clear is idempotent.
- `IMapWorldActivityListener` callbacks / `WorldReady` and `WorldCleared` events: optional activity plug-ins (race/survival/boss) consume immutable sample snapshot; throwing consumer is isolated, not a cause to roll back the already-published producer scene. Register/Unregister explicitly.
- `MapWorldSnapshot` contains Start/Finish and copied checkpoint/spawn arrays in **WORLD coordinates**. It does not spawn robots or own inventory, damage or physics.

## Reproducible source QA / deliverables
```bash
dotnet run --project Tools/B01/MAPGENWORLD001/MapWorld.SourceTests.csproj -c Release -- /tmp/mapworld-source-cases.json
python3 -m unittest discover -s Tools/B01/MAPGENWORLD001 -p 'test_*.py' -v
python3 Tools/B01/MAPGENWORLD001/package_world.py build /tmp/mapworld-package
python3 Tools/B01/MAPGENWORLD001/package_world.py verify /tmp/mapworld-package/MAPGENWORLD001_SOURCE_FULL.zip
```
CI runs **actual C# MapWorldPolicy + pinned MAPGEN planner/validator** using existing explicit surrogate for Unity vectors (not native Unity). Tests of `MapGenWorldAdapter` MonoBehaviour in Edit/Play require Unity6000.3.14f1 installed and remain **NOT_RUN** unless accompanied by native NUnit/XML/Editor/Player log+exit.

## Source QA cannot certify gameplay physics
- Protected main corridor is checked with provided measured width +0.25m per side, source route validator and endpoints; full swept length/turn radius, transform/colliders and R6 wheels require actual Unity/PhysX.
- No assertion that optional ramps accommodate Heavy; fail to enter ramp ≠ fail primary map.
- Full source ZIP includes **this package** (runtime, EditMode/PlayMode tests, metas, docs, repeatable tools, embedded SHA256 manifest), **not** MAPGEN001 producer files or shared `Assets/B01Packages.meta`. Import complete R3.4.1 producer separately first and preserve target shared-parent GUID.
- `MapWorldDemo`: add to a new disposable URP scene empty GameObject, hit Play; default Arena seed7 Light2.980, ContextMenu regenerate/clear. It is a sample, not active game wiring.
