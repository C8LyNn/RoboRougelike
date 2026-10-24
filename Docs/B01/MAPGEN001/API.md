# MAPGEN-001 R1 / Public API

```csharp
using B01.MAPGEN001;
var builder = isolatedHost.AddComponent<MapGenBuilder>();
var result = builder.Generate(MapRecipe.CombatArena, seed, MapGenConfig.Default, MapVehicleProfile.Light);
var second = builder.Generate(MapRecipe.ObstacleCourse, seed, MapGenConfig.Default, MapVehicleProfile.Heavy);
builder.Clear();
```

The pure `MapGenPlanner.Plan(recipe, seed, config, profile)` returns `MapBlueprint` without touching the scene. `Generate()` first validates/constructs a new level, then atomically disables the old root. `Clear()` immediately disables old physics before Unity deferred object destruction. Caller controls seed, density (0..100), difficulty (0..1), course segments (3..5), lane width (10..30m), reference vehicle width, length, min turn radius.

The generated hierarchy is parented beneath the opt-in host Transform. All `Start`, `Finish`, `Spawns`, `CheckpointDescriptors` and `Bounds` are returned in **world space**; `GeometryHash` is based on canonical local geometry and therefore unaffected by host placement.

`MapResult`: `GeneratedRoot` (all colliders owned by builder), `Start`, `Finish`, `Spawns[]` (combat enemy spawn descriptors), `CheckpointDescriptors[]` (course waypoints DATA only), `Bounds`, `GeometryHash` SHA-256 of canonical rounded geometry, `Diagnostics`, `GenerationVersion`, `Recipe`, `Seed`.

`CheckConfig` rejects non-finite inputs, a vehicle wider than the actually ramp-limited free corridor (width*0.60 - 3 metres with 0.5m safety margin), or impossible turn radius with descriptive `ArgumentException`. No silent unsafe random rerolls. Seed same => same plan; different seeds are expected to vary cover/slalom/ramp placement. Generator owns no health/rewards/run progress.

**Limits:** geometric clearance is a conservative protected-lane rule, not swept-body real PhysX proof; very large/odd robots and aggressive mobility modules require downstream vehicle profile calibration and PlayMode tests. `Generate` must be called on Unity main thread.

## R2 optional route safety API
`MapGenRouteValidator.Validate(MapBlueprint, MapVehicleProfile) -> Report { Passed, Code, ExploredCells, MinimumRadius }` is used automatically by `MapGenPlanner.Plan`. A failed route throws `ArgumentException("IMPOSSIBLE_GENERATED_ROUTE: <CODE>")` before the existing live level is removed. Codes include `BFS_NO_CONNECTED_ROUTE`, `SPINE_FOOTPRINT_BLOCKED`, `TURN_RADIUS_TOO_SMALL`, `RAMP_EXCEEDS_PROFILE_SLOPE`, `INVALID_SLOPE_LIMIT`. `MapVehicleProfile.MaxSlopeDegrees` is optional (0 -> legacy 12 degree default); Light defaults 14, Heavy 10. This is a preflight, not a physical-playtest certificate.

## R3 validation notes
- `MapGenRouteValidator.Validate(plan, profile)` checks a **static aligned rectangle**, 3x3 vehicle support grid, ground-level obstacle overlap, 0.25m sampled planned spine, four-neighbour 1m BFS and conservative half-adjacent-segment fillet feasibility. Rejecting unsupported poses is expected, but a PASS is not full 3D vehicle traversal proof.
- `MapGenBuilder.Regenerate(recipe, seed, config, vehicle)` is an alias for `Generate` with the same return type and failure behavior. Planning, geometry, metadata and bounds must complete before replacing `Current`; a failed precommit operation disables and releases the candidate root.
- Enemy spawn points are validated for the given reference envelope including mutual overlap. Integration remains data-only: generated checkpoints carry **no** CHL-003 reward or progression logic.
- The demo's follow camera and on-screen physical cube are TEST aids, not the shipping garage robot controller.
