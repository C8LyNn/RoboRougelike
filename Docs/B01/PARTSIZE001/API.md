# PARTSIZE-001 R1 — API contract and receiver boundary

**Package owner:** B01-23. **State:** IN_PROGRESS / NOT OFFERED / no Codex claim / G2 HOLD. Unity 6000.3.14f1. Sources: `Assets/PARTSIZE001/**`. No dependency on canonical R3, garage, R6, CAMERA, BUILD001 or reward services. This document does **not** authorize modifying Codex-owned integration source.

## Pure C# contract (no Unity reference)

`B01.PARTSIZE001.PartSizeDefinition` describes one logical part ID, positive integer `Dimensions`, optional measured local rectangular collider `LocalBoxCenter` / `LocalBoxSize` in metres and `Provenance`. Both geometry fields missing means **UNMEASURED**, not a zero-size collider; only six source-measured wide kinds and canonical 0.52m cubes have source-backed box definitions in `PartSizeBaselineCatalog`. Visual/functional clearance and canonical host mass/HP are **not** supplied by this package.

`PartSizeGrid.GridMeters=0.55`, `LegacyBlockMeters=0.52`, `MaximumWorldOvershootMeters=0.005` (review tolerance, not a request to alter R3). `Cell3` is an immutable integer XYZ cell. `PartSizeGrid.Rotate` uses Unity-compatible **Z→X→Y** 90-degree quarter turns (including negatives). `Occupied(def, anchor, turns)` returns actual rotated logical cells; `LogicalBounds` returns the union of cell-aligned bounding boxes; `PhysicalBounds` rotates all eight corners of the measured local physical box. `DefinitionError` yields a rejection reason before any physical installation.

## Unity native physical gate (isolated, fail-closed)

`PartSizeNativePhysics.Validate(spec, anchor, turns, owner, ownedColliders, surroundingColliders, completeBroadphase)` returns **null only for verified valid placement**, otherwise a rejection string. Caller must supply the real owner transform and full owned child-collider inventory. The external inventory and `completeBroadphase=true` are **not trusted**: the native validator independently calls `Physics.SyncTransforms` and `Physics.OverlapBox` around each registered physical collider, including all Unity layers, and rejects any unreported nearby active non-trigger Collider with `INCOMPLETE_EXTERNAL_BROADPHASE`. It then calls genuine `Physics.ComputePenetration` on disclosed neighbors; an undiscoverable owner Collider returns `PHYSICS_QUERY_UNVERIFIED`. The query is limited to the active default Unity physics scene; other custom PhysicsScenes are not certified. No dummy colliders or speculative collider shapes. An exposed logical face from `PartSizeGrid.IsExposedFace` is **not a physical mounting socket**. `PartSizeNativePhysics.ValidateMountSocketAuthority` always rejects missing typed socket provenance (`TYPED_SOCKET_UNVERIFIED` for exposed faces). PARTSIZE exposes **no trusted socket registry**, so the Codex/BUILD owner must independently authorize actual typed socket face, normal, identity and contact area. A null from pure grid API is **not** installation authority.

Do **not** use a one-cell logical footprint to shrink wheel/tread/barrel/exhaust rendering or physics. `PartSizeBaselineCatalog` exposes measured logical dimensions and explicit UNMEASURED physical status for unverified canonical kinds. R3 saves must continue to deserialize unchanged; integration is opt-in and requires a separate Codex target SHA/ownership ACK and migration approval.

## Openable Unity demo and test contract

`Assets/PARTSIZE001/Demo/PARTSIZE001_Demo.unity` is a standalone serialized Unity scene, **not an instruction to edit existing main scenes**. It directly references the owned `PartSizeDemoEntry` component by GUID. Open it in an isolated Unity editor and press Play: `Start()` spawns its own camera if absent, three primitive physical BoxColliders (0.52 cube, rotated 2×1×2 wide 1.07×.52×1.07, 1.2m oversize rejection), applies green/red URP/unlit previews and displays observed verdicts plus a deterministic Rebuild action. Scene creates no active canonical gameplay objects, no R3 inventory, no saved data. `Demo/PARTSIZE001_Demo.prefab` remains available for manual prefab insertion in a disposable empty scene.

**Important limits:** rendering, real Collider/PhysX, successful `.unity` scene opening and 720p/1080p screenshots are **NATIVE_NOT_RUN by producer** until independently confirmed in Unity 6000.3.14f1. Authored **17 EditMode + 11 PlayMode** tests likewise **NOT_RUN**. Portable .NET source tests and archive CI do not certify the physical gate.

## Codex adapter interface (proposal, not implemented)

A future Codex-owned adapter should map *one actual canonical physical part ID* → one PARTSIZE logical owner and its occupied cell list, preserve R3 one-owner HP/inventory and current mounting semantics, provide actual collider identities (including all children), broadphase completeness and typed mount socket authority, and call physical validation only at transaction commit. An R3 rejection must remain unchanged unless separately approved by HQ. No host `Assets/R3/**`, `Assets/R6/**`, Garage or shader/settings changes belong in this producer PR.

**Rollback:** in the authorized disposable target, remove only imported `Assets/PARTSIZE001/**` plus `Assets/PARTSIZE001.meta` after preserving any preexisting target copy. Do not remove shared `Assets/B01Packages.meta` or modify Packages/ProjectSettings. Revert to the original exact target HEAD and confirm bytes/asset GUIDs before any live Codex install (G2 HOLD).

**Gates before OFFERED:** independent B01-40 source/GUID/archive verification and import+rollback; B01-60 actual native scene launch + 17 Edit +11 Play NUnit XML/log/exits + real collider 0/90/180/270 and negative oversize tests; owner-visible R3 compatibility/actual measured asset evidence; B01-00 G2 and Codex explicit ACK.
