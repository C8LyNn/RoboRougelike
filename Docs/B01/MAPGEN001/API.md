# MAPGEN-001 R1 / Public API

```csharp
using B01.MAPGEN001;
var builder = isolatedHost.AddComponent<MapGenBuilder>();
var result = builder.Generate(MapRecipe.CombatArena, seed, MapGenConfig.Default, MapVehicleProfile.Light);
var second = builder.Generate(MapRecipe.ObstacleCourse, seed, MapGenConfig.Default, MapVehicleProfile.Heavy);
builder.Clear();
```

The pure `MapGenPlanner.Plan(recipe, seed, config, profile)` returns `MapBlueprint` without touching the scene. `Generate()` first validates/constructs a new level, then atomically disables the old root. `Clear()` immediately disables old physics before Unity deferred object destruction. Caller controls seed, density (0..100), difficulty (0..1), course segments (3..5), lane width (10..30m), reference vehicle width, length, min turn radius.

`MapResult`: `GeneratedRoot` (all colliders owned by builder), `Start`, `Finish`, `Spawns[]` (combat enemy spawn descriptors), `CheckpointDescriptors[]` (course waypoints DATA only), `Bounds`, `GeometryHash` SHA-256 of canonical rounded geometry, `Diagnostics`, `GenerationVersion`, `Recipe`, `Seed`.

`CheckConfig` rejects non-finite inputs, profile too wide for guaranteed corridor or impossible turn radius with descriptive `ArgumentException`. No silent unsafe random rerolls. Seed same => same plan; different seeds are expected to vary cover/slalom/ramp placement. Generator owns no health/rewards/run progress.

**Limits:** geometric clearance is a conservative protected-lane rule, not swept-body real PhysX proof; very large/odd robots and aggressive mobility modules require downstream vehicle profile calibration and PlayMode tests. `Generate` must be called on Unity main thread.
