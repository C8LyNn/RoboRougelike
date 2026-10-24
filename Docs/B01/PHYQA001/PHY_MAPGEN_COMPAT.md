# PHY_MAPGEN_COMPAT — B01-00 / P0 producer-source interface review

**Scope:** compare existing Codex PR #125 (head `916654634d1bfdc2bbc14e64b9eeac856ede1fa8`, product runtime `523046f5bf6ddab0caf62253615c962e808c3053`) against MAPGEN #93 / R3.1 PR #103 `ac7d957799588452f831a645162a0ab7c05246e0`. This is a source compatibility review, NOT a native Unity import or independent PhysX PASS. Date: 2026-10-25.

## Examined actual boundaries (read-only)
- `Assets/R6/R6WheelContacts.cs` at PR#125: mounted eligible wheel collider casts `Physics.RaycastNonAlloc`, requires `Terrain(hit.collider)` (non-trigger, enabled, active and `MOVSeriesVehicleAdapter.IsTerrain`), normal up dot >=.5, normal with down dot >=.5; contact uses collider shape, gravity-down probe `radius+skin`, short skin .015-.08m. No contact = zero wheel authority. Other MOV solvers remain their own responsibility.
- `Assets/R6/R6VehicleDrive.cs`: `GroundedWheels`, `GroundNormal`, contact-centred force and torque; gravity/unconstrained body. `R6CoreRecovery.cs`: terrain support and finite lift/torque for tipped Core, not warp/restore.
- `Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs`: builds `GameObject.CreatePrimitive(PrimitiveType.Cube)` static collider geometry on a newly created map root; no wheel traction adapter or R6 dependencies; returns world metadata and clears old colliders.
- `MapGenData.cs`: Light rectangle 2.1×3.2m, minimum turn radius3m, declared max ramp 14°; Heavy rectangle4.5×5.8m, minimum turn radius6m, declared max ramp10°. These are **geometry/profile assumptions**, not the footprint, wheelbase, suspension or drive authority measured for a real PR#125 robot.
- `MapGenRouteValidator.cs`: grid/BFS + 3×3 flat footprint check and centreline; no suspension, contact manifold, wheel positions, friction, sweep with yaw or real collision callbacks. `MapGenPhysicalTraversalTests` uses a free Rigidbody cube with forces and torques, not `R6VehicleDrive`.
- Producer physical PR#125 evidence describes Light/Heavy driving on real **10-degree** slopes; this does not certify MAPGEN ramps (BoxCollider incl intersections/seams), 14° Light design default, 12-vehicle MAPGEN crossings or harder topology.
- The two Draft PRs have **no direct shared changed-path overlap** in the indicated source namespaces. This does not prove a conflict-free simultaneous import into a recipient's unpublished worktree. W8 target/scene conflicts need external ACK.

## Integration-risk matrix
| Risk | Why B01 expects it | Reproduction that can decide |
| --- | --- | --- |
| P0 / PHY-MAP-01 wheel contact on generated floor | BoxColliders may be legal terrain or excluded by target terrain predicates/layers | On frozen **separate disposable Unity checkout** containing both *for testing only*, spawn true R3/R6 mounted wheels and read grounded count 0→4 and motor distance; no package integration PR |
| P0 / PHY-MAP-02 slope seam/ramp entry | 17m floor cubes overlap every 16m; ramps intersect floor and carry sloped BoxCollider normal | Drive real vehicle over seed 7/31/83 flat→ramp→seam; record wheel normals, contact count, yaw, vertical impulses/stalls, collision contacts |
| P0 / PHY-MAP-03 Heavy geometry mismatch | Rectangle Light/Heavy may not match actual occupied colliders, axle layout, mass/COM | Compare world colliders/profiles on real target Heavy (published 1312kg in native fixture); test course seeds 2/7/31 normal and 0/2/7/19 hard |
| P1 / PHY-MAP-04 full support | MOV009 FullSupport raised physically unloads wheels by design | R3 Heavy on generated floor, raise support, check no phantom drive; lower and restore; no false impassability classification |
| P1 / PHY-MAP-05 roll/recovery | Arena boundaries/cover and ramps can impede a tipped Core | Controlled roof position on flat and near obstacle; cap attempts, torque/force and effects, no warping |
| P1 / PHY-MAP-06 spawn origin/Y & transformed map root | Geometric `MapResult.Start/Finish` returned as world metadata but not actual tire-ground contact; arbitrary nonuniform host scales change clearance | Separate offset/rotated/uniform/nonuniform host tests; collider-world bounds and wheel contact at start |
| P1 / PHY-MAP-07 stop/regenerate | Destroy in PlayMode is delayed; old collider deactivation is immediate | Regenerate 3× with live true wheel physics, assert no stale support/duplicated colliders; compare exact hash and transformed spawns |
| P1 / PHY-MAP-08 checkpoint logic ownership | MAPGEN emits descriptors, CHL-003 owns progression | Feed data into independent test adapter only; verify no duplicate win/score components or invalid route after spawn |

## Test ownership and methods
- **B01-60 PHY #131:** independently verify physics contract on Codex PR125 without patching runtime, real Unity Edit/Play/test logs, screenshots and actual controls. No source producer PASS counts inherited as its own.
- **B01-60 MAPGEN #93:** test MAPGEN R3.1 standalone *and* in separate throwaway compatibility harness with a real R6-wheeled vehicle on generated floor/ramp, if legally licensed Unity is accessible. Record 100 seeds/recipe and 3 manual/physical seeds x Light/Heavy, failure seeds, wheel contacts and slopes. The throwaway harness is QA-only; no main/develop, Codex or GPT OSS worktree modification.
- **B01-20:** own the only active source-production slot on isolated MAPGEN branch; target a narrow R3.2 after an actual fail; no alternative grounded drive/Core code.
- **B01-40:** confirm project asmdef/native assembly dependencies, terrain predicate for generated colliders, recipient SHA and package source GUIDs before G2; evaluate whether a tiny opt-in adapter is needed, not a new system.
- **B01-10:** correlate scale/mass/COM/height, wheel count, contact loss, traversal time and player recoverability.
- **B01-30:** visibility markers and true collider/seam visualization only; PR #109 remains optional and separately audited.

## Exact gate
**G0:** source integrity MAPGEN R3.1 42/42 SHA256 static PASS, target-specific compatibility PENDING. **PHY source producer:** evidence receipt passed 11/11 JSON checks (not Unity tests). **G1:** independent B01-60 Unity/PhysX NOT_RUN. **G2:** HOLD. Codex owns PR#125 integration; GPT OSS receives MAPGEN only after B01-00 evidence-backed approval. No merge or automatic handoff.
