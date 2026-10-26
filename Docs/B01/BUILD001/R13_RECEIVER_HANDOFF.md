# BUILD-001 R1.3 — B01-23 isolated source / Codex integration HOLD

Dispatch: `HQ-B01-20261025-W12-B01-23-BUILD-OWNERSHIP-TRANSFER`
Base (immutable BUILD R1.2): `489676414a3eb45fe70124098e3778a81d0aaff2`, stacked complete #149 + #215.
Source: branch `b01-23/w12-build001-r13`, Draft PR R1.3; use **full tree** not just R1.3 changed-file patch.
Reference native RED (not fixed proof): QA #239 — 34/38 NUnit passed, four BUILD W9 failures, ten magenta demo screenshots.
Contract #240 — 30 BUILD fixtures; this pack is owned only by B01-23.

## Actual implementation changes

1. HIT-02 / W9-01: `BuildSocketRegistry` binds **exact Collider instance** to `PartInstanceId` plus `SocketIndex`; requires trusted `BuildMountSurface` marker on actual collider. Visual children may remain in the hierarchy but are NOT sockets. The R1.2 legacy root-only signature is retained **only for isolated fixtures** and now refuses child colliders. New receiver implementation MUST use strict overload.
2. ROOT-02 / W9-02: multiple surviving Core parts invalidate `Check` and yield empty `Reachable`; the strict installer exposes `CoreDisconnected` when an orphan island otherwise touches candidate. Never destructively repair a loaded save: preserve bytes and quarantine until migration.
3. FP-02/03 / W9-03: `BuildCellBond(A,B,PhysicalJoin)` requires a connected orthogonal bond graph across every multi-cell part. The known legal 2x2 fixture declares three physical joins. Future destructible multi-cell prefab authors must supply truthful bonds and **native** verify real joints; metadata is NOT a substitute for actual PhysX connectivity.
4. VOL-03 / W9-04: `BuildPlacementRules.CheckForInstallation` is the **only entry point for commit**. It requires complete `BuildPhysicsEvidence`: active colliders for candidate and neighbors, `CompleteBroadphase=true` attestation. `BuildVolumeValidator` executes genuine `Physics.ComputePenetration` and rejects depth >0.005m; missing/unsupported evidence becomes `PhysicsUnverified`. `Check()` is purely a *preview* — its Valid NEVER allows commit.
5. DEMO: an explicit `Resources/BUILD001_PreviewURP.shader` is built for URP (non-strippable Resources asset). Demo assigns green/red/neutral retained shader-backed Materials, no global renderer settings, start cycle choice fixed to 0. Native Windows material appearance is NOT verified by this source packet.

## Strict receiver order — ONLY after B01-00 G2 authorization

- B01-40 G0: pin entire R1.3 Git head, run `python Tools/B01/BUILD001/test_package_r13.py`, then `python Tools/B01/BUILD001/package_r13.py`. Verify source ZIP + embedded manifest SHA256 / lengths / GUID/meta; stage + rollback in disposable target and check shared GUID roots. Do **not** hand the zip to Codex as approved on source CI alone.
- B01-60/independent licensed operator G1: Unity 6000.3.14f1 fresh isolated checkout, compile, original Edit 11 + new Edit 7, original Play 2 + new Play 4, actual Scene `BUILD001_Demo`; test W9-01..04 and 5 states x 720p/1080p with real PNG and absence of Hidden/InternalErrorShader.
- Codex owner must disclose live target branch, SHA, path locks and reserve allowed installation slot. Then Codex alone integrates strict `BuildMountSurface` authoring / registry lifecycle (spawn/despawn/load/damage), core graph, reliable broadphase over real vehicle/garage colliders (including parts outside nearest-owner), validated physical joins, and maps reasons to existing R3GarageView UI without modifying inventory/economy or physics authority. Ensure compatibility with LEGENDARY typed `WeaponBase`; `MobilityHub` never becomes wildcard Structural.
- Only Codex tests actual R3/R6 canonical ground contacts, mass/COM, detachment, saves/load, garage undo/redo, combat, 720/1080 visuals. Report exact source SHA, target SHA, Editor.log/process exit/NUnit XML and Windows Player screenshots.
- Rollback: remove only R1.3 package import + Codex adapter commits on disposable branch; restore destination `.meta` (never delete if shared). Preserve raw save and all existing asset identities; do not alter main/develop.

## Status

B01-23 source produced; native Unity tests on this producer host: **NOT_RUN** (no licensed Unity editor); native functional W9 fixes: **UNVERIFIED until independent G1**. No merge, live game edits, automatic installation, G2/release or claimed READY_FOR_CODEX.
