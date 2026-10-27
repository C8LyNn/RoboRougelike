# B01-23 / W58–W61 — 11 × 4 yaw PARTSIZE native verification sheet (B01-30 + B01-60)

**Dispatch:** [#313](https://github.com/C8LyNn/RoboRougelike/issues/313). **Producer**: [#286](https://github.com/C8LyNn/RoboRougelike/pull/286) exact `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`, catalog Git blob `6153d1758e800ec5c4210323610b061c06e8ea85`. **Original QA operator:** [#298](https://github.com/C8LyNn/RoboRougelike/pull/298) original PS1 Git blob `da575df50d51d5f76776d96e3b5251103b85d923`. **Current assembled R3 reference**: [Codex #310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`, `R3VehicleAssembly.cs` Git blob `49fab51ac993128a9965df38ce32489f493b257a`. All actual native measurements below are `NOT_RUN`.

## Recording protocol (each cell is a separate actual pose, not an inferred rotated AABB)

- Use **HQ-authorized separate licensed Unity 6000.3.14f1 Windows machine**, detached one-use producer worktree, not Codex active checkout. B01-60 executes and owns native receipts; B01-40 verifies original source/rollback. An imported R3 rig or moving part requires its **own separately approved disposable host**; PARTSIZE demo alone is not a certified Codex import.
- For each of the **44 kind × yaw** poses set physical owner at a known 0.55m-grid anchor; apply actual yaw **0°, 90°, 180°, 270°**, track all colliders including root AND solid child payloads at **actual 0.52 local part-root scale** when testing R3. PARTSIZE core unit-scale owner contract is not a direct adapter for an R3 child transform (`NONCANONICAL_OWNER_TRANSFORM` otherwise).
- Capture separately **Renderer.bounds min/max** and **Collider.bounds min/max in metres** (not the same and not derived from 0.52 cube); collider count and types; centre/pivot; enabled/trigger; `Physics.SyncTransforms`; overlaps with a true adjacent structural neighbour and `Physics.ComputePenetration` depth; moving swept state (if applicable). Report signed overflow against each **owned** cell (grid pitch .55m, source review limit .005m; not an automatic waiver of typed socket/penetration).
- Confirm authoritative **host-typed Build socket** ownership/ID, face normal, contact mask, support graph, exposed cell face vs *trusted* mount face. PARTSIZE itself **always** returns `TYPED_SOCKET_UNVERIFIED` for a merely exposed face, never approval. Re-run the intentional malicious/omitted-neighbour negative separately. Do not classify any `UNMEASURED` geometry as PASS from source-only approximations.
- For every pose retain immutable SHA, actual scene/host identifiers, original Unity log, raw XML, process exit code, 720p/1080p screenshots, fail reason, and `PASS / FAIL / UNVERIFIED` **per physical test**. **Acceptance must include geometry + collision + typed socket**; no `G1_PASS` without B01-60 real evidence.

| Part (`1×1×1` logical, physical unmeasured) | Yaw 0° | 90° | 180° | 270° | Distinct collision/render/sweep/native falsifier |
|---|---|---|---|---|---|
| Wheel | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Convex wheel MeshCollider vs tire render, axle direction, wheel rolling clearance |
| Gun | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Gun variants/pitch+yaw muzzle sweep; typed WeaponBase, collision must use actual part rather than decal |
| Suspension | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Root collision versus decorative moving piston; compress/extend as applicable; typed SuspensionBase |
| Booster | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Rear projection and exhaust: distinguish solid collider from VFX; rear-neighbour negative |
| JumpDrive | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Downward payload pivot, lowest point and landing clearance |
| Blink | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Physical sphere versus decorative effect; reject assumed radius from visual mesh |
| Gyroscope | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Decorative **nonphysical** ring vs rendered overhang; Collider bounds must NOT include decorative ring |
| OmniDrive | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Source-only corrected assembled yaw0 payload union Y[-0.442,+0.260]m; verify actual Unity root/child and adjacent below |
| MagneticGrapple | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Forward projectile/harpoon visual versus enabled physical capsule; moving tether sweep |
| HoverEngine | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Lower device, rotor/hover clearance, collision vs floor; no fake physical hover cert |
| CrawlerTrack | NOT_RUN | NOT_RUN | NOT_RUN | NOT_RUN | Source-based adjacent +Z BoxCollider apparent overlap (~0.008182304m³ reported by B01-30), actual PhysX signed penetration by yaw |

**Sample raw row to record per pose** (all 44 rows are required; one row for each kind and yaw): `kind | yaw_deg | input_sha | source_scene | root_world_scale | renderer_min/max_m | physical_collider_min/max_m | collider_types/count | overlap_neighbor_id | penetration_depth_m | moving_sweep_result | typed_socket_id/kind/face/owner | result/reason | xml/log/exit paths`.

**Offline C# negative fixture:** `test_socket_authority_original_csharp.py` compiles the **actual extracted** `ValidateMountSocketAuthority` body together with the original immutable `PartSizeContract.cs` and `PartSizeBaselineCatalog.cs` via .NET8. Its 11-kind × 4-yaw untrusted/forged provenance and 6 wide-kind internal/external face conditions are **NOT** Unity/PhysX/host socket proof; a deliberate `return null` mutant must fail. This extends the existing single exposed cube native test without changing the 17 Edit / 11 Play frozen count.

**Status:** `NATIVE_CONDITIONAL_ON_HQ_LICENSE / ALL_44_POSES_NOT_RUN / NOT_OFFERED / G2_HOLD`. **Recipient:** B01-30 (visual/render interpretation), B01-60 (independent native PhysX + true sockets), B01-40 (evidence intake), HQ (licensed host).
