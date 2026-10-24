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

## R3 — B01-20 producer corrective iteration (2026-10-24)
- Replaced three-waypoint circumcircle-based turn criterion with conservative half-segment tangent-fillet bound; an abrupt long 90-degree corner can no longer falsely meet an arbitrary large turning radius. This is **not** a full 3D steering or wheel-contact certificate.
- Reduced spine sample step from 0.5m to 0.25m; 3x3 floor support probes instead of just four footprint corners.
- Validate every generated enemy spawn's conservative heavy/light footprint and reject enemy-enemy overlaps.
- Add seed-variable cover offsets at safe arena grid cells and difficulty-dependent optional ramp pitches (4–8 degrees), keeping basic protected through routes.
- Extend transactional exception handling in MapGenBuilder across metadata/bounds construction; add public Regenerate alias.
- Follow demo Rigidbody with smooth camera, constrain roll/pitch, add damping for playable manual traversals.
- New EditMode negative cases: middle-of-chassis floor pit, overly tight 90-degree turn, blocked/overlapping enemy spawns, difficulty variation, 100-seed arena challenge preflight; PlayMode 3-cycle regenerate/clear collider-retirement case.
- All edits limited to isolated MAPGEN001 sources/docs. **Native Unity compile, EditMode, PlayMode, real physical completion still NOT_RUN.** Independent B01-60 review and B01-40 G2 still required.

### R3.1 narrow-lane correction
- Tightened per-segment lateral wandering as a function of LaneWidth and moved optional ramps toward the side boundary (without overlapping the wall) to retain a larger safe corridor for 10m Light and 16m Heavy challenges at Difficulty=1. This follows a mathematical worst-case geometry review; native Unity runtime proof is still NOT_RUN.
- Added 600 further authored extreme course cases: 100 seeds x 3 segment counts x Light@10m/Heavy@16m at density=100/difficulty=1.
