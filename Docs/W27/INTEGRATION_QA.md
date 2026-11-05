# W27 integration — partial native QA

Status: **IN PROGRESS / not a release**. The W26 integration base is `e7a85944465d908f175a6f5e2ae07f4aedcc593f`; the tested gameplay and Windows Player source is `e03d36f2b86f8c73a067e574b67237a8921f622a`. The branch contains subsequent documentation-only changes. No merge to main, develop, or release was performed.

## Delivered so far

- Shotgun, railgun and rocket group fire respect the existing owned-vehicle clearance guard; their direct bindings also check their real firing direction. Shotgun checks individual pellet directions and inherited velocity. Blocked attempts do not commit resources in the tested cases.
- Beam binding gates operation on its actual forward path and cools while obstructed. Original gun, damage and aim systems remain in use. Tested beam scenarios are native Editor tests, not a Windows Player beam test.
- `W27WeaponPlayerQA` is a batch-only acceptance harness. It is dormant unless explicitly enabled by command-line argument; it is not interactive gameplay.

## Native verification of e03d36f2

Unity 6000.3.14f1, Windows x64, hidden process. Evidence folder: `Artifacts/Local/FM270-W27-FinalCandidate-20261102T220406Z/`, with Editor logs, NUnit XML and `*-process.json` exit codes. Local artifacts are not committed.

| Check | Result | Evidence |
|---|---|---|
| Unity compile | PASS, exit 0 | `Compile.log`, `Compile-process.json` |
| Windows x64 build | PASS, exit 0 | `Build.log`, `Build-process.json` |
| Full EditMode | FAIL, exit 2: 1328 passed, 5 failed, 1 skipped of 1334 | `EditMode.xml`, `EditMode.log` |
| Full PlayMode | PASS, exit 0: 474/474 | `PlayMode.xml`, `PlayMode.log` |
| Targeted retest of five failing EditMode cases | 4 passed, 1 failed, exit 2 | `Artifacts/Local/FM270-W27-RegressionRetest-20261103T080919Z/` |
| Extracted Windows Player weapon smoke | PASS, exit 0; 4 blocked and 4 unblocked cases: Basic, Shotgun, Railgun, Rocket | `Artifacts/Local/W27/ExtractedBeamWeaponSmoke/` |
| Extracted Windows Player garage, two encounters and two rewards | PASS, exit 0, controlled via API; includes real enemy projectile and persistent part IDs | `Artifacts/Local/W27/ExtractedBeamFlowSmoke/` |
| Human gameplay, hand controls, subjective quality | NOT_RUN | No desktop/mouse takeover |
| Beam in Windows Player, all rotations, many weapons, moving obstacles and performance | NOT_RUN | Further scenarios required |

The original five EditMode failures were `Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle`, `MOVSeriesGarageTests.ActualKeyboardActivatesAbilitiesAndExistingMouseStillFires`, `R1PlayIntegrationTests.SixEncountersCanBeWonWithActualPlayerGunAndPhysicalHits`, `R41GarageTests.PointedRepairRemovalAndInventoryMountUseActualPartAndSurviveNextFight`, and `R4CombatTests.AllWeaponsHaveIndependentCadenceUpgradesAndPhysicalDamage`. All except the MOVSeries input scenario passed the single targeted retest. This does not convert the full suite to PASS. The skipped B0124 opaque-cover policy case requires its upstream approval. The remaining MOVSeries timeout has not been diagnosed as a game defect or a test defect.

Additional targeted tests were run during development: `Artifacts/Local/W27/direct-green.xml` 6/6, `spread-diagnostic3.xml` 1/1, and `beam-final.xml` 5/5, all with Unity process exit 0. Earlier red tests and intermediate fixture failures remain in the same evidence tree and are not represented as passing. The early Player QA bootstrap failed once and was fixed in the test harness before the passing run.

## Partial build

`Builds/RoboRougelike_W27_BeamProjectile_PARTIAL_Windows_x64.zip` — 38,638,404 bytes, SHA-256 `f3e5baf6fba1a37d0e7347416dd8e18ac69ce2ec0c2b699d29aa98972b54cd31`. It was extracted and the extracted Player was tested as above. This is a partial test build, not W27 acceptance or a release. Unzip the entire archive, keep the data folders next to the EXE and start `RoboRougelike.exe` on Windows x64.

## Outstanding

- Rotary saw and radial EMP need mechanism-specific owned-part/sector protection and tests. Additional dynamic blockage, multi-gun, orientation and performance checks remain. Railgun penetration behind an external collider needs dedicated investigation.
- The requested five-color 3D garage zones and their unified preview/mount validation are not implemented. No claim of W27 completion is made.
- Producer Save #797 `2b7983f0f9e09dd1d56906b379a3f7af7b02f21a` is still the previously incompatible Unity API source; #822 `e66968408b583404cd50250f53e655ea75a085fc` is a source test, not the repaired implementation. Await producer repair and independent G0; no competing save was imported.
- B01 #804, #810, #813 and #814 remain separate pending independent acceptance and compatibility review. W27 must not import unapproved packages.
- `R41_Turret_v3` is not installed. Previous native import found mismatched muzzle axes; await corrected source or validated rig adapter. Existing production prefab remains in use.

## MOV input regression diagnosis

A diagnostic run with temporary logging (`Artifacts/Local/W27/MOV-input-diagnostic3/`) reached the training range and reproduced the timeout. The original test mounted Blink at `(0,1,1)`: both its block and physical payload intersected the gun barrel and overlapped the muzzle. The guard returned `clear=false`; firing through that part would contradict the new safety requirement. The diagnostic logging was removed afterwards.

The same input test now mounts Blink at `(1,1,1)`, explicitly verifies a clear path, and retains its real keyboard actions, mouse press and projectile assertion. All six `MOVSeriesGarageTests` passed in the native Editor on the modified test tree: `Artifacts/Local/W27/MOV-clear-fixture/`, exit 0. This fixture correction is not a retroactive PASS for the full EditMode run.

Latest #810 source `05db0ddf0a48da592d924e68b7658eea6bba764e` has since received an independent **source transport / scope-only G0** in #829; native G1, generated prefab/scene and a B01-00 #79 integration offer are still pending. It has not been imported. Newer #827 and #843 supersede #804 and #813 respectively and are not implicitly approved for installation.

## First garage overlay increment

The garage preview now draws non-colliding red wireframes for the exact `R3VehicleModel.OccupiedCells` set used by placement, including destroyed plans and rotations of multi-cell parts. This is one color only: blue, purple, orange and the complete green mounting-zone display are still outstanding; rendering, accessibility and performance for large designs are not yet verified in a human Windows session.

The native red test `Artifacts/Local/W27/GarageOccupied-red/` failed as expected with 0 visible occupied cells. `GarageOccupied-green/` passed 2/2. `GarageOccupied-regression/` passed the overlay test including a purchased, X-rotated 4-cell shield, but one pre-existing firing assertion failed. Diagnostic `R41-input-diagnostic/` showed the repaired armor at `(2,2,2)` physically intersecting the gun's current shot path. The test retains its garage repair, inventory, next-combat and physical projectile assertions, now aims into a checked clear path, and `GarageOccupied-regression2/` passes the combined garage, overlay, special projectile and beam suite: 14/14, process exit 0. These are native Editor tests on the working tree immediately before this commit, not a full final-HEAD suite.

## Garage moving/protruding geometry increment

The placement ghost keeps its existing green/red validity feedback and displays the selected part's actual child payload geometry in blue (including wheel/turret geometry). Ghost colliders stay disabled. This is a visualization of geometry only, not proof that every moving-part swept volume is clear. `GaragePayload-red/` reproduced the missing blue marking. `GaragePayload-green/` passed the two overlay tests plus the full R41 garage test group, 12/12, Unity exit 0. Human visual usability, performance, purple firing/safety zones, orange swept spaces and complete green attachment affordances remain NOT_RUN / incomplete.

The later partial ZIP containing the red occupied-cell increment was built at `2632299d3a96796c7e915c4b299d69ad78ea7bdb`, size 38,638,874 bytes, SHA-256 `fa7bd0193be1c7a886cf1dff4373502bfda56fd49b2920bea0a910d9af1b56d9`, local file `Builds/RoboRougelike_W27_OccupiedGrid_PARTIAL_Windows_x64.zip`. The extracted executable passed the actual API-driven Player weapon and garage/two fights/two rewards smokes in `Artifacts/Local/W27/ExtractedOccupiedWeaponSmoke/` and `ExtractedOccupiedFlowSmoke/`, exit 0; neither is manual play or a rendering verification. This ZIP precedes the blue increment and must not be described as containing it.

## Latest partial Windows build

Compiled and built Unity Windows x64 from `af33caed0d5b5412c450dd65ef0f1692f08bc695`, compile and build exit 0: `Artifacts/Local/FM270-W27-BlueCandidate-20261103T082823Z/`. `Builds/RoboRougelike_W27_GarageZones_PARTIAL_Windows_x64.zip` contains the red occupancy and blue ghost-payload increments. Size 38,638,966 bytes; SHA-256 `58c4f3187ce95d27301f4e9cf6187f2b5a49c13bf4f33b0aaeebb390dadc3b4c`. It was unpacked and executed: real Player API-controlled Basic/Shotgun/Railgun/Rocket blocked and free 4+4 PASS (`ExtractedGarageZonesWeaponSmoke/`); API-controlled garage, 2 fights and 2 rewards PASS, part IDs preserved (`ExtractedGarageZonesFlowSmoke/`); both exit 0. Human rendering/controls NOT_RUN. The most recent full EditMode/PlayMode results above refer to the earlier `e03d36f2` source; a full run on `af33caed` is still required. This remains a partial W27 test build, not a final release.

## New producer revisions observed during W27

- The repaired sole-writer candidate is now #826 `99fb08619c78b3834bb4c0450499babf991f218a`, not the old #797. #847 reports independent netstandard ABI/source-only acceptance, but the required composite #826/#787/#792/#780/#750 G0 remains HOLD and the real gameplay binder and native Unity G1 remain missing. No Save package was imported.
- #828 `887fd18bf0a518ee24776c660f5efa6e3a0491d8` proposes saw own-disc protection; #839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384` proposes EMP direct activation authority. Both remain separate drafts without independent full G0 or native Unity results; neither is integrated or claimed complete. Avoid competing source implementation while producer work is active.
- #836 `3587d233a3eee30adb9cdfca3d71edfaf0000610` supplies three independently authored moving-obstacle/rotation/beam Unity test cases with source-only QA. They have not yet run natively and were not installed into the gameplay worktree.
- A full EditMode/PlayMode run on source `710b7ce01bd0a7bc9a00008cecd56c69ce67e052` was launched under `Artifacts/Local/FM270-W27-GarageZones-Full-20261103T083024Z/`; status pending at this report. A separate lower-priority Unity diagnostic ran concurrently in a different worktree; consider timing effects when interpreting failures.

## Independent W28 native fixture and saw candidate checks

Original #836 source `3587d233a3eee30adb9cdfca3d71edfaf0000610`, unmodified test + `.meta` on isolated QA checkout `0fadf6a727e62fa9b646eb1112aa01f5037f4590`, was actually executed in Unity 6000.3.14f1 EditMode: **3/3 PASS, exit 0**, local XML/log `RoboRougelike-W27-Saw/Artifacts/Local/W27/B0161-836-DynamicEdit/`. This covers moving own colliders for Shotgun/Rail/Rocket direct + group fire, rotated gun vs external cover, and continuous Beam. It is not Windows Player or human gameplay and is not integrated into this branch. Product weapon source is byte-identical to original #824@e03d36f2.

Isolated producer candidate #828 `887fd18bf0a518ee24776c660f5efa6e3a0491d8` native Unity: EditMode 2/4 PASS (corrected own blocker/resume fixture PASS; existing player and enemy saw scenarios FAIL), PlayMode 19/20 PASS (overflow diagnostic flag FAIL). See [#828 native report](https://github.com/C8LyNn/RoboRougelike/pull/828#issuecomment-6599153868) and QA-only #848. Earlier hypothesis that resuming was broken was explicitly corrected: the first test accidentally left its supporting core in the disc. No G0, G2, or integration claimed.

## Isolated EMP and remaining source readiness

#839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384` ran unchanged in a separate Unity 6000.3.14f1 QA checkout: existing real vehicle/device `RRLW6EmpPhysicalTests` EditMode **12/12 PASS, exit 0**; original WPN006 `EmpEmitterPhysicsTests` PlayMode **22/22 PASS, exit 0**. XML/logs/exits: `RoboRougelike-W27-Saw/Artifacts/Local/W27/Emp839-Native/`. The integration full suite ran concurrently in a separate worktree; no performance conclusions. This does not establish complete W27 EMP coverage, Player behavior, formal G0, G2, or installation; #839 remains separate.

On latest check #820 `45a5c54d6b6985b54e6fba22a9095ecbf04eba60` and #806 `9cb0a93d55030e3bf66baec57b4437830dcf655d` had no independent acceptance comments. #816 `cc82b984090dfd81278c386719387fad3a8bc1a8` remains browser research without independent source G0 or Unity runtime; do not import as a game package. Source states can change later and must be rechecked before any intake.

### EMP candidate Windows Player

Additional isolated Draft QA [#849](https://github.com/C8LyNn/RoboRougelike/pull/849) `16d338f13779ee71ac74b8549e0168dce92bfda3` adds an explicitly opted-in batch QA harness on #839 without editing producer code. Exact product `fea000fe0a706df2b52b6f295bdb3135ba1f9384`, Player harness build commit `0eb1bf164f8d201ed8b34e05e10a2663446b4818`: Unity compile/build exit 0, actual Windows Player exit 0. Real R3 EMP denied direct activation while detached, preserving energy and cooldown, and after reattachment emitted a pulse that disrupted a real receiver on a simple external collider. API-controlled; not a full enemy, manual gameplay, performance, or integration test. Local JSON and Player.log: `RoboRougelike-W27-Saw/Artifacts/Local/W27/Emp839-PlayerSmoke/`. Full regression was running concurrently; no G0/G2 or merge claimed.

The latest #79 coordinator note confirms #829 scoped **source-transport-only** G0 for original #804/#810/#814, not the newer #827 revision or complete native gameplay acceptance. Prefab/scene and G1/offer remain outstanding; no candidate has been imported.

## Turret v5 native import

New local `R41_Turret_v5.fbx` SHA-256 `fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533` and `.glb` SHA-256 `e898a9df602fee9e461702060848f4e00b798d4e73f2992da2ffcdb6c253055d` were independently inspected; producer grid metadata says 0.55 m mount, left/right at `(-/+0.06,0.135,0.70)` and central `(0,0.135,0.70)`, fire direction +Z. Actual isolated Unity 6000.3.14f1 FBX import completed exit 0: default import yields left/right mirrored/inverted in Z and forward +Y; `bakeAxisConversion=true` restores left/right positions but both forwards point -Y, and central muzzle remains `(0,0,0.565)` rather than metadata. Applying local Y yaw to imported yaw moves the muzzle in the wrong plane. Materials import as URP/Lit. Independent GLB header and triangle screening passed; this is not Unity GLB or gameplay validation. Full measurements and producer request: [#141](https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6599393182), local `RoboRougelike-W27-Saw/Artifacts/Local/W27/TurretV5/`. Existing production prefab remains unchanged; v5 is BLOCKED for shooting until corrected rig/export or validated adapter.

Turret v5 follow-up: an isolated Unity `Object.Instantiate` prototype with baked import, fresh world-preserving yaw/pitch wrapper hierarchy at the authored pivot and +Z muzzle markers passed 12 measured yaw/pitch combinations (left/right marker position errors <0.0001 m; direction errors <0.02°). An earlier `PrefabUtility.InstantiatePrefab` approach failed because Unity rejected reparenting prefab-instance child nodes; both results were reported in [#141](https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6599452191). This proves transform feasibility only: the central marker is inferred, and production prefab, actual vehicle mounting, collisions/swept zones, firing, animation and Windows Player are NOT_RUN. The FBX is not installed and v5 remains BLOCKED for product integration.

## Full regression on 710b7ce0

Exact checked-out source `710b7ce01bd0a7bc9a00008cecd56c69ce67e052` (gameplay `af33caed0d5b5412c450dd65ef0f1692f08bc695`), Unity 6000.3.14f1: full EditMode **FAIL**: 1334 passed, 1 failed, 1 skipped of 1336, process exit 2; full PlayMode **PASS 474/474**, exit 0. Evidence XML/logs/process JSON: `Artifacts/Local/FM270-W27-GarageZones-Full-20261103T083024Z/`. Skip is `B0124EnemyAiNegativeEditModeTests.SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy` pending upstream policy approval. Failure `R4CombatTests.AllWeaponsHaveIndependentCadenceUpgradesAndPhysicalDamage`: expected 4 automatic projectiles but got 3.

Separate diagnostic `R4Cadence-Diagnostic/`, same product source, added logging to the test only. All three original projectiles still existed at the second check; one of two automatic guns was ready but `ClearProjectilePath=false`, while the input aim had turned it toward its own construction during the 0.17-second wait. This identifies a fixture/aim interaction rather than a vanished projectile; a corrected clear-path fixture is being verified. Results of a targeted correction do not retroactively make this full run pass. Several independent low-priority Unity QA processes ran concurrently in separate worktrees; performance and timing conclusions require care.

## Readable W27 partial Windows build and targeted integration verification

Gameplay source `ed8cf4bcc131688b9a93968a89683d2496586683`, Unity 6000.3.14f1 Windows x64: compilation and build each **PASS**, exit 0 (`Artifacts/Local/FM270-W27-ReadableCandidate-20261103T093043Z/`). The merged garage, weapon clearance, beam and R4 cadence targeted EditMode suite passed **22/22**, exit 0 (`Artifacts/Local/FM270-W27-ReadableTargeted-20261103T093123Z/`). This includes the corrected cadence fixture; it does not retroactively change the full `710b7ce0` result above.

`Builds/RoboRougelike_W27_ReadableGarage_PARTIAL_Windows_x64.zip`, 39,630,831 bytes, SHA-256 `051eaa949dbe2084307161b499ec8101b4c1d93df71bc5b6ff03a8f84e951e3f`, includes the updated red outlines, blue ghost and Polish legend. The complete ZIP was extracted and run in Windows Player: real R3 Basic/Shotgun/Railgun/Rocket blocked and unblocked firing **4+4 PASS**, exit 0 (`Artifacts/Local/W27/ExtractedReadableWeaponSmoke/`); API-controlled garage, two fights, two wins, two rewards, enemy shot and persistent part IDs **PASS**, exit 0 (`Artifacts/Local/W27/ExtractedReadableFlowSmoke/`). These are controlled tests, not human gameplay; normal GUI, manual visual quality, large builds, full exact-SHA EditMode/PlayMode and performance remain unverified. This ZIP is a partial test candidate, not W27 completion.
