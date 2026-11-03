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
