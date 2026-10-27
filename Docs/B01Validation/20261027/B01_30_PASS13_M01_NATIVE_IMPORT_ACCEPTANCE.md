# B01-30 / PASS13 — one conditional M01-024 native import acceptance (NOT_RUN)

**Only one proposed case:** M01_024_SourcePinned_FBX_GLB_VisualAxesAndColliderSeparation_4Yaw2Pose.

**Scope:** B01-60 independent QA in an HQ-approved separate licensed disposable Windows Unity 6000.3.14f1 environment. NOT a new game package, not a Unity run, no permission to touch Codex worktree, no PARTSIZE/G2/CODEX_ACK. No third-party import packages may be added without explicit HQ authorization.

## Immutable files and new original test

M01 owner #227 exact source commit: b27d210bb598e738c6dd979686706b5fc29b73ed. Actual original Git blobs:
- OBJ f1c3cc2274833cc522d422e563306f3f343d518c (223621 bytes)
- FBX 29344b9acb5a3fb4896178a06ec756a22f4ad633 (210668 bytes)
- GLB 2d98f05eb05ac8e5c4c475114a3767a1b7981f8b (299068 bytes)

[New GitHub Actions original 51-mesh byte test](https://github.com/C8LyNn/RoboRougelike/actions/runs/40593875289) actually decompressed all 51 original FBX geometry arrays (3322 vertices). **Raw FBX mesh basis equals authored OBJ, tolerance approximately 0.00000005246 m; original GLB geometry basis equals (OBJ.x,OBJ.z,-OBJ.y), same error scale.** FBX MOUNT_FRONT_SOCKET root X rotation -90.0000025 degrees, local scale (100,100,100), while GLB root has no equivalent explicit rotation. This is proven differing export representation, NOT a demonstrated Unity failure.

## Preconditions and stop conditions

1. HQ explicitly authorizes separate licensed Unity operator/host, source-pinned disposable worktrees and outside-root evidence. M01 owner explicitly confirms target authored forward (+Z), up (+Y), 1m/unit, importer compensation policy and whether the socket root is a deliberate virtual offset.
2. Confirm the isolated project has **both** genuine FBX and GLB import support. Unity built-in FBX importing does not prove GLB importer availability. If GLB is unavailable, report GLB_IMPORTER_UNAVAILABLE / NATIVE_TWO_FORMAT_PARITY_NOT_RUN, without implicitly installing packages.
3. Record ModelImporter scale/axis flags, imported root and tip transform local/world matrices, Unity version, original source bytes and hashes. Apply conversions exactly once, not twice.

## One test: four yaw rotations × two real animation poses

Instantiate both original exported models in a temporary **disposable** Unity scene. For each format, sample its **real imported animation clip** (not an invented transform) at rest frame 1 and full-stroke frame 19 or 43; rotate around authored world +Y at yaw 0,90,180,270 degrees. Capture union **Renderer.bounds** across all original visible meshes, tip displacement, root matrices and screenshots (front, side, top). The single test has 8 pose/yaw observations **per format**; this is a receiver plan, not 16 executed native tests.

Source authored reference visual AABB in metres, relative to root after logged approved import orientation:

| Yaw | Rest min XYZ | Rest max XYZ | Extended significant axis endpoint |
|---|---|---|---|
| 0 | (-.3323,.2900,.0050) | (.5770,.8513,1.7750) | max Z=2.0150 |
| 90 | (.0050,.2900,-.5770) | (1.7750,.8513,.3323) | max X=2.0150 |
| 180 | (-.5770,.2900,-1.7750) | (.3323,.8513,-.0050) | min Z=-2.0150 |
| 270 | (-1.7750,.2900,-.3323) | (-.0050,.8513,.5770) | min X=-2.0150 |

**PROPOSED, not approved tolerance:** each renderer AABB endpoint compared with authored source and the other correctly normalized import format within ±0.005 m. Tip advance 0.240 ±0.005 m along chosen actual vehicle-forward, unwanted upward displacement at most 0.005 m. If root/scale cannot be reconciled under documented import settings, return INCONCLUSIVE / SOURCE_IMPORT_CONTRACT_DISCREPANCY with matrices for M01 owner, not automatically UNITY_ENGINE_BUG.

## Mandatory separation: visual, socket, physics

- **Visible render:** full original rendered hull X width ~0.9093 m, Z extent 1.7700 m rest and 2.0150 m forward endpoint extended; this is not an approved 0.55m PARTSIZE footprint, so do not declare garage defect.
- **Mount pivot:** authored MOUNT_FRONT_SOCKET origin is ~0.290043 m (AABB distance) from nearest Mount_Front_Key mesh surface. Require M01 owner to answer whether virtual offset is intentional; name does not authenticate a physical typed socket.
- **Collider / PhysX:** Renderer.bounds and visual mesh AABB are NOT Collider.bounds or a registered collision hull. No existing M01 approved Collider is proven. If the imported prefab has components, record collider type, enabled/trigger, bounds and rigidbody separately; any real PhysX contact requires distinct native test/authority. Never claim PHYSICS_PASS from this visual measurement.

**Evidence to return:** Unity/editor version, source hashes and asset import settings, raw actual per-pose union bounds, true imported clip time/displacement, collider inventory, screenshots, Editor.log/NUnit XML/exit as applicable, and disposable rollback. Never report G1 PASS without genuine licensed native receiver; only M01 owns source fixes and B01-60 native independent acceptance.
