# MAPGEN-001 changelog

## R1 / initial source candidate
- New self-contained C# seed-driven plans for CombatArena and ObstacleCourse.
- Real runtime primitive/collider builder with atomic regenerate/clear semantics.
- Physical Unity scene bootstrap and two sample Rigidbody rigs.
- Unity EditMode (100 seeds/recipe) and PlayMode test source.
- Install/API/test/known-limitations/provenance/docs for GPT OSS.
- Does **not** integrate MAP-R6/W8 and does not claim passed Unity QA.

## R2 candidate — swept route preflight (2026-10-24)
- Added `MapGenRouteValidator.cs`: four-corner rectangular support/clearance, 1m BFS connectivity, half-meter centreline samples, generated course junction bend-radius check, optional maximum ramp slope per reference vehicle. This is intentionally a conservative geometry check, not a PhysX traversal guarantee.
- Fixed `ObstacleCourse` start position from Z=2 to Z=5 to keep the heavy reference envelope off the beginning cliff edge.
- Exposed optional `MapVehicleProfile.MaxSlopeDegrees` (zero inherits conservative 12 degree default), varied generated centre offsets with difficulty.
- Added EditMode sweep/negative regressions and PlayMode dynamic Rigidbody drive test for 3 seeds x 2 recipes x 2 reference profiles.
- **Status: SOURCE REVISION ONLY; Unity compile, Edit/Play tests, independent B01-60 QA and physical driving NOT RUN at publication.**
