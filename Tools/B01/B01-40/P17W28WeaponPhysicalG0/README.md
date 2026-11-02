# B01-40 — W28 / independent live PhysX weapon regression

**This delivers actual new original Unity NUnit PlayMode test source**, not just a test plan. The two `[UnityTest]` cases are in `Assets/Tests/Editor/B0140W28PhysicalClearanceTests.cs`, accompanied by its own new unique `.meta`. They test existing Codex W27 gameplay code by invoking the real `R3VehicleAssembly.Build`, `ClearProjectilePath`, `FireGroup`, `Rigidbody.MovePosition`, `Physics.SyncTransforms`, and actual six weapon classes, plus LPM/RPM isolation. There is **no patch to production combat**, no duplicated gun logic, and no Save, garage or producer package editing.

## Exact source identity and obligations

- Original unchanged Codex W26 PR #817 exact `e7a85944465d908f175a6f5e2ae07f4aedcc593f`.
- W27 Codex PR #824 **true exact base** `e03d36f2b86f8c73a067e574b67237a8921f622a` (beam and specialization; later comments alone do not establish a newer Git HEAD).
- New B01-40 branch `qa/b01-40-p17w28-physx-weapon-regression-r1`, **forked directly from the actual W27 Git commit** and never changes Codex active worktree or any pre-existing source.
- User's requested game features Saw, EMP, mechanic sweep and garage visual zones remain owned by Codex and are **not** reported PASS by these tests.
- Economic loaner P0 witness #812 and producer #797 Unity compilation RED remain **BLOCKED**, not bypassed by this fixture.

## Native Unity 6000.3.14f1 required acceptance (not executed here)

In a **new disposable, separately isolated local worktree**, for this exact QA commit under an authorized Unity 6000.3.14f1 Editor license, run the EditMode suite targeting the two tests above using Unity Test Framework. The suite explicitly changes from EditMode into genuine PlayMode with `EnterPlayMode` and back. It creates real dynamic Unity 3D actors with actual collision, not a fake Unity shim.

**Case 1:** Builds each of Basic/Automatic/Heavy/Shotgun/Railgun/Rocket in a real R3 Light rig, inserts owner-child physics collider, verifies suppressed group shot with unchanged part/device cooldown, ammo and absence of spawned projectile. Moves obstruction via actual kinematic `Rigidbody.MovePosition`, observes clear path, moves it back and checks suppression again. Finally validates that physically nearer foreign cover wins over a farther owner obstruction. Tests moving obstructions after a physics step rather than one static synthetic query.

**Case 2:** Builds original standard R3 two-weapon rig, first Basic in LPM and Automatic in PPM, inserts two separate owned colliders, checks blocked group cooldown, then clears PPM alone and fires while LPM remains blocked, and finally clears LPM independently.

Run **actual Unity Editor tests** and attach `Editor.log` and NUnit XML with explicit full test names, test counts, process exit code, exact source SHA. If environmental license/API compile fails, record FAIL/BLOCKED; never claim G1 PASS. Then in an **actual Windows x64 Player ZIP** measure blocked and unblocked attacks for all six weapons, including moving owned collider after ZIP extraction and log actual Player results. The separate Codex W27 Player harness covers only four weapons and is behind `B01_UNIFIED_RC2_ACCEPTANCE`, so do not silently claim six-player-class coverage. Windows ZIP SHA256, player log, platform architecture and real executable exit code are mandatory for full G1. Manual game-feel test remains separate.

## Executed source G0 and rollback

Own `verify.py` performs strict original W26→W27 immutable Git SHA, original six C# Git blob pins, no modifications to any owner/producer path, full original Unity `.meta` GUID uniqueness for new fixture, native-test API/checkpoint invariants, **actual disposable file staging**, a dirty-byte mutation fail-closed witness and untouched foreign Save and empty folder. GitHub Actions runs Windows2025 and Ubuntu24.04 and requires cross-platform byte-identical SHA256 JSON receipt. This is a **source-integrity test**, not a surrogate for Unity PhysX or a playable build.

Rollback: Close only the new Draft and delete only new own staged files verified against their Git contents, without deleting player Save, existing generated scene, Codex worktree or foreign empty directories. No merge to main/develop or release.

## Gate split

`IMPLEMENTED=B01_40_NEW_NATIVE_TEST_SOURCE`;
`SOURCE_G0=CI_DEPENDENT`;
`UNITY_G1=NOT_RUN`;
`WINDOWS_ZIP=NOT_CREATED`;
`SOURCE_GAMEPLAY_PRODUCTION_CHANGED=NO`;
`SCOPE=QA_ONLY`;
`READY_FOR_CODEX=HOLD`.

Next: B01-60 actual Unity Native G1, B01-41/61 cross-check test assumptions, Codex/B01-00 gameplay fixes Saw/EMP/garage and softlock decision, B01-40 independent rerun exact new W28 gameplay SHA once available. No new #79 offer before source and native acceptance.
