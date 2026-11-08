# B01-60 / PASS17 WAVE28 — real Unity gameplay weapon physics regressions

**CO ZAWIERA PACZKA:** FOUR new original `[UnityTest]` cases on ACTUAL EXISTING R3 vehicle/game source, physical Unity Rigidbody/BoxCollider and real Codex W27 weapons from [#824](https://github.com/C8LyNn/RoboRougelike/pull/824), exact **base `e03d36f2b86f8c73a067e574b67237a8921f622a`**. Separate B01-60 QA ownership, six ADDED-only QA-owned paths, NO runtime/source changes, no active Codex worktree, no main/develop merge. Existing W27 Shotgun/Railgun/Rocket/Beam tests **not duplicated**.

**W28 gap coverage / genuine expected product outcomes:**

1. Dynamic owned **kinematic Rigidbody** blocks shotgun, then moves away using actual Unity `Rigidbody.MovePosition`, `WaitForFixedUpdate`, `Physics.SyncTransforms`; blocked group no ammo, `DeviceReadyAt`, or `NextFireTime` spent; after movement actual shotgun can fire.
2. Actual **Rotary Saw** R3-binder: physical owned BoxCollider in rotor contact radius, barrel/muzzle rotates with running motor (non-vacuous `Motor.Rpm>0`), `RRLSawBinding.TryResolve` refuses self collision, no owner Core HP damage, motor request cleared on release. **NOT a projectile path rule.**
3. Actual **EMP radial** R3-binder: its own electronic receiver remains unaffected, physical external wall on layer9 prevents enemy disruption, real successful pulse spends cooldown ONCE despite occlusion (area weapon, not projectile), repeated cooldown-blocked pulse spends nothing; wall removed and cooldown explicitly advanced/test reset -> real receiver disrupted. Owner/self occlusion semantics follow existing `EmpEmitter` implementation, not invented one-rule bullet logic.
4. **Two weapon groups** on the real R3 0.55m vehicle with distinct part IDs: LPM Shotgun path physically blocked and does not debit resources; RPM Basic group clear and still fires, without altering Shotgun ammo.

Native-only genuine PlayMode tests use existing `Assets/Tests/Editor/` test runner assembly, `EditorSceneManager.NewScene`, `EnterPlayMode` and `ExitPlayMode`, actual MonoBehaviours and Unity PhysX. This fixture does NOT introduce fake `UnityEngine` shims. **C# authored but NOT Unity-compiled or run by B01-60:** no installed/activated Unity Editor and no access to owner Windows Player. Source CI proves only exact Codex game source identity, test registration, unchanged binaries and unique GUID.

## Actual test execution — only on isolated consented Unity 6000.3.14f1 checkout

After independent **B01-40/B01-61 exact G0 for the new QA branch** (not source CI alone) and a legitimate locally activated Unity Hub 6000.3.14f1 editor, copy the approved exact branch to a DISPOSABLE isolated checkout, never the active Codex working tree, with private `Application.persistentDataPath`/unity project name. One possible explicit invocation on Windows:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Path\To\Independent\RoboRougelike-W28-QA' -runTests -testPlatform EditMode -testFilter 'B0160W28RealWeaponPhysicsAcceptanceTests' -testResults 'C:\Path\To\Evidence\w28-weapons-nunit.xml' -logFile 'C:\Path\To\Evidence\w28-weapons-editor.log' -quit
```

Actual Editor.log must identify exact original package checkout, test fixture compiled in Unity, four unique original NUnit test identities, 0 failures, process exit 0. Independently verify NUnit XML test counts/status, timeouts, editor version, Unity Console warnings, new game source SHA and **ZIP/Player are separate**; a source test PASS is NOT PhysX/Unity PASS. Test failures are genuine missing gameplay regressions/fixture issues until triaged; do not silently skip/disable tests. No native Editor license, no human playtest, no build evidence here.

## Source-only fast verification and CI

```powershell
python Tools/B01/B01-60/P17W28WeaponPhysicsAcceptance/verify_source.py
```

This compares real Git blob IDs of original Codex #824 `R3VehicleAssembly`, `RRLSawBinding`, `RRLEmpBinding`, `RRLHitRouter`, actual EMP and Saw original gameplay modules and already-reported W27 NUnit sources, rejects dirty authored test files, unexpected protected/owner path modifications, missing original 4 NUnit scenarios or missing real Unity physical assertions, and rejects collision of the NEW real Unity `.meta` GUID. Two-source-OS GitHub Actions check is SOURCE ONLY. **Rollback:** do not use this isolated QA branch or discard only its six additions. No merge, no original producer/game file touched.

## Broader W28 gates — DO NOT PROMOTE

**Confirmed inherited #824 scope:** real W27 mainline already has shotgun/railgun/rocket projectile group clearance and beam own-barrel guard; original owner reported selected native EditMode results 6/6 + separate shotgun-spread 1/1, not full suite. This W28 package tests additional scenarios but did not implement missing saw/EMP gameplay behavior or garage 3D visual zones, real owner loaner softlock resolution, new R41 turret import, Save V7, integrated optional W27 packages or Windows Player.

**Known economy P0 [#812](https://github.com/C8LyNn/RoboRougelike/pull/812)**: original V7 direct-source witness authentic Core alive/Drive dead/0 Scrap/loaner spent after Win1/free rebuild delayed to Win2 leaves no route to Win2. Source test PASS is **anti-softlock FAIL** until B01-00 approves a narrowly scoped rule or compliant game-defined terminal run and Codex implements/retests actual loop. Do not change V7 or one-shot loaner from B01-60 QA.

**Known Save:** #797 .NET API compile mismatch in Unity (CS0117/CS1501 for SHA256.HashData / Convert.ToHexString / File.Move overwrite), only B01-62 owns ABI-compatible versioned successor; never add competing writer. **Native Save=BLOCKED_BY_PRODUCER unless new accepted version exists.** G1 source/native separation required.

**Receiver:** B01-40 and B01-61 source G0, B01-60 independent native physics actual evidence, B01-00 control ACK/next queue, Codex only if specifically ACK + original G0. No overwrite of Codex branch, no release, no unauthorized merge. **Status at creation:** IMPLEMENTED=YES_NEW_NATIVE_TEST_SOURCE; TESTED=SOURCE_CI_PENDING; QA_ACCEPTED=PENDING; NATIVE_G1=NOT_RUN; WINDOWS_PLAYER=NOT_RUN; GAME_ACCEPTED=NO; READY_FOR_CODEX=NO; INTEGRATED=NO; RELEASED=NO.
