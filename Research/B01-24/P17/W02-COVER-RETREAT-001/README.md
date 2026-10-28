# B01-24 / PASS17 WAVE 02 — COVER-RETREAT-001 / R1

**Working independent gameplay-content prototype:** low-health *armed* enemy can propose a collision-screened route to a sheltered rally site in each of the three existing `R5ArenaKind` areas. Uses new original authored `cover_sites.json` (10 sheltered anchors, three maps) plus standalone C# `CoverRetreatPlanner.cs`, a real 8-neighbour A* pathfinder and hysteresis state machine. The original canonical enemy has **distance/strafe movement and unarmed ramming**, but no low-HP cover-retreat state; this package does not replace or edit either.

**Owner:** B01-24. **Codex canonical source:** no edits. **This is NOT yet wired into any Unity MonoBehaviour, AI or GameObject.** No actual gameplay balance rule has been approved: 35% enter / 60% exit HP in this standalone algorithm are *provisional research parameters*, NOT integrated game values.

## Run the actual code

From repository root, with .NET 8 (no third-party packages):

```sh
dotnet build Research/B01-24/P17/W02-COVER-RETREAT-001/CoverRetreatP17.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Research/B01-24/P17/W02-COVER-RETREAT-001/CoverRetreatP17.csproj
```

The source runner computes **real routes**, records every actual PASS/FAIL, exits nonzero on failure, tests two exact original Git *source blob SHA-1* inputs, and checks malformed/unsafe cover site and movement conditions. Windows + Ubuntu job: `.github/workflows/b01-24-p17-w02-cover-retreat.yml`.

### Algorithm and input contract

`RetreatPlanner.Evaluate(CoverCatalog catalog, int arenaKind, Vector2 enemyXZ, float healthFraction, bool mobilityOnline, bool weaponOnline, bool previousRetreat, float enterFraction=.35f, float exitFraction=.60f)`

- **NORMAL_COMBAT**: above source-independent low-HP threshold, return control to unchanged Codex AI (no movement applied).
- **RETREAT_TO_COVER**: health below enter (or still below exit after entry), select the nearest **reachable and actually occluded** authored cover site. Route includes exact start and exact cover coordinates and uses deterministic **8-neighbour A*** on a 2m grid, segment-checks with 2m conservative envelope against R5 walls/boxes, rejects diagonal clipping, returns exact route and length.
- **HOLD**: no drive or no reachable safe route. Does not synthesize Rigidbody motion.
- **DEFER_TO_CODEX_RAM**: no gun; preserves the already native-tested original Codex unarmed ram behavior. Does not replace firing/aiming, choose LOS policy or alter damage.

JSON data maps the **original static R5Arena.Build** wall/cover footprints for SalvageYard, Wasteland, Industrial and 10 sheltered positions behind real original geometry. `R5Arena.cs` Git blob and current original `P12_PipelineSafeEnemyTurret.cs` Git blob must match pinned immutable IDs; source-check refuses drift rather than assuming updated obstacle placement is safe.

The planner uses independent pure `System.Numerics.Vector2`. It **does not** simulate Unity PhysX, R6 vehicle mass/size, dynamic debris, traversability, enemy collision damage, vertical clearance, speed/time, door access, camera or actual bot HP. 2m expanded axis-aligned rectangles are conservative design proxies, NOT measured physical collision envelopes. A* does not solve moving hazards or guarantee combat viability. A later native receiver must independently check every waypoint against the actual complete vehicle envelope.

## Codex adapter and rollback — exclusive single-writer

After B01-40 source intake, independent B01-60 QA and **explicit Codex + HQ gameplay approval**, Codex alone may choose whether to build a C# runtime adapter into `P12PipelineSafeEnemyTurret`/original R6. At native runtime source actual HP fraction from mounted core or current design owner-approved damage component; do **not** infer from `IsDefeated`. Source mobility and online weapon state from real active original components. Call the *pure advisory* planner, transform X/Z waypoints into the R6 movement/obstacle-avoidance controller, recheck native 3D colliders at every step; never teleport enemy.

Test native Unity 6000.3.14f1 physical clearance with actual heavy/light builds, both zero-drive and no-weapon cases, cover occlusion, alternative arena access, hysteresis no jitter, and enemy defeats; publish original XML, Editor.log, process exit, source SHAs. **Rollback** by removing only Codex's optional adapter reference/selection and retaining canonical original FixedUpdate/ram strategy; no changes to CHL, PARTSIZE, MAPGEN, LD02, existing #338 enemy catalog or #355 spawn formations.

**Gates:** author source tests do not grant B01-40 `OFFERED/FROZEN`, B01-60 native G1, Codex ACK, G2 or integration. Manifest pins source blobs, exact original inputs and statuses; independent QA must distinguish original-source matching from actual Unity execution.
