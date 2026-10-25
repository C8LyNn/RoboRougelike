# CAMERA-001 R1.1 — native Unity and functional QA

2026-10-25. **FAIL / integration HOLD.** Original compilation/Edit/Play now pass. Actual camera pose still violates the own-reticle requirement on a pitch transition, and the corridor fallback exhibits large jumps/poor wall clearance. Producer repair is required before a green independent acceptance candidate. Evidence is ready for B01-60 reproduction; no independent G1/G2 approval is granted.

## Inputs, scope and preservation

| Item | Exact version / SHA |
|---|---|
| Full CAMERA R1.1, PR134 + PR214 | `d48108f56d3fe0790ad715683e77b2de9219e4b1` |
| Target OBJ210, includes PHY125 | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` |
| Included PHY125 | `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` |
| Original native compile/Edit/Play snapshot | `494840deba798574fa950800de8764fd2ad9d2d9` |
| Functional build code snapshot | `6fd76d717240068a4713cc8db14387509cc858c1` |
| Saved functional scene and exact Player launch snapshot | `244d6af11a53f86507438dc4874c51aae38259ae` |

Read requirements #66 and camera P0 portion of #41. Used clean, inactive `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-W9-Validation` on a **new** `qa/rrl-camera-r11-native` branch from prior capsule `4a4f3e3ddc04ac7001f3df1b2e28d868e0b9a4db`. Target PR210 was checked and remains4c9236a. Existing branches/worktree HEADs, MAPGEN results and active game were preserved. No extra agents, producer code fixes, Unity/package upgrades or repeated original suites. No AGENTS reservation file found; no Editor/Player was active at start.

Complete source tree and original metas were imported, not only PR214's two-asmdef patch. [Integrity record](source-integrity.json): **37 complete files byte-identical**,36 declared Git blobSHA1/byte entries match; independent SHA256 receipts computed. Producer manifest SHA256 fields are null, so no producer SHA256-verification claim. **0 GUID collisions /0 invalid metas**. Shared parent GUID preserved. Dependencies: unchanged Unity6000.3.14f1, URP and existing Test Framework. No MAPGEN/BUILD/RAC/LEGENDARY or active R3/R6/weapon/camera/scene/settings file was modified. New QA scene is opt-in only and has a single camera owner.

## Native results and evidence

| Stage | Actual result | Evidence |
|---|---|---|
| Project compilation, Unity6000.3.14f1 | **PASS**, native0 | [Editor.log](Compile/Editor.log), [process/SHA](Compile/process.json) |
| Original EditMode | **PASS5/5**,0 skipped, native0 | [NUnit XML](OriginalUnity/EditMode.xml), [log](OriginalUnity/EditMode.log), [process](OriginalUnity/EditMode-process.json) |
| Original PlayMode | **PASS3/3**,0 skipped, native0 | [NUnit XML](OriginalUnity/PlayMode.xml), [log](OriginalUnity/PlayMode.log), [process](OriginalUnity/PlayMode-process.json) |
| Windows x64 QA build | **PASS**, native0;0 errors/4 warnings retained | [Editor.log](Build/Editor.log), [process/SHA](Build/process.json) |
| Actual functional Player | **FAIL_OBSERVED**, native2;0 logged errors/exceptions | [JSON](Functional/functional-results.json), [diagnostic XML](Functional/functional-diagnostic.xml), [CSV](Functional/camera-trace.csv), [Player.log](Functional/Player.log), [exit](Functional/process.json) |
| Render captures | **34/34**, true1280×720 or1920×1080 | [PNG dimensions/hashes, execution metadata](native-summary.json) |

The840 geometry proposals are a loop inside one of the five actual EditMode cases, not840 NUnit cases. No JS geometry probe rerun. Three original PlayMode cases verify own-collider handling, a wall shortening the arm and reset after a Heavy→Light resize; they do not test the transient actual-pose defect below. Functional XML is a conversion of real standalone diagnostics, **not extra NUnit/PlayMode test cases**.

## Functional cases

[Eight-row measured table](FUNCTIONAL_TABLE.md). All eight requested rig/resolution combinations actually completed. Real `R3VehicleAssembly.Build`/`R3VehicleModel`/`R3VehicleCatalog`; canonical palette, parts, colliders, HP, weapon and drive APIs. Static framing/orbit fixtures are kinematic to isolate the camera; the Light drive/fire phase uses the original live Rigidbody/R6 force drive. Tall/Wide are deliberately extreme armour additions through actual mounted-part construction, not certified garage blueprints. No fake shots, vehicle teleport traversal or replacement camera solver.

| Requested scenario | Result / precise coverage |
|---|---|
| Small Light | Static framingPASS; transition own-rayFAIL, corridorFAIL at both resolutions.20parts, hull2.980×1.564×2.720m |
| Large Heavy | Static framingPASS; transition own-rayFAIL, corridorFAIL at both resolutions.139parts,5.180×2.114×4.920m |
| Very tall | Static framingPASS, targetvisible; corridorFAIL at both resolutions.34parts,9.160m high |
| Very wide | Static framingPASS; transition own-rayFAIL, corridorFAIL at both resolutions.149parts,10.420m wide |
| Wall / tight passage | Wall clipping exercised; blocked-pivot corridor fallbackFAIL (details below). Instant wall-insertion jumps are stress evidence, not normal-drive smoothing measurements |
|360degree orbit | Full95deg/s sweep executed for each row. Initial pitch transitionFAIL for Light/Heavy/Wide; subsequent fixed18deg-pitch yaw sweep has no own-ray hits |
| Aim/fire while moving | **PASS for limited API/muzzle observation** on Light twice:17.559m moved per5s run;11 actual P17PhysicalShell bodies per run; maximum muzzle→reticle target error0.0198deg. Full ballistic trace/mouse-input integrationNOT_RUN |
| Enemy behind tall own modules | Stationary real R3 target at(0,1,30), tinted red, remains in viewport with0 own-hull enemy-line occlusions during facing-target phases, including Tall. Enemy AI/target acquisitionNOT_RUN |
| Damage / parts | Existing HP damage applied in all8; operative mounted count drops1, camera follows changed hull,0 own-ray intersections in damage phase; largest sampled step0.0961m. Raw `damageDetached` accepts destroyed OR detached state, so separate fragment-trajectory/detachment-flag certification isNOT_RUN |
|720p /1080p | All4 rigs rendered in each;34 actual PNGs. Dimensions verified; selected Light/Tall/corridor images visually inspected |

Framing pitch is calculated to face the actual stationary R3 target. The orbit phase intentionally switches to pitch18 and then sweeps yaw. Camera001Rig.LateUpdate is disabled; its original public StepCamera receives actual frame delta once per sampled update. No P12 camera runs alongside it. No LMB/RMB human play, garage/poligon/menu regression or exhaustive1–180part matrix is claimed.

## Producer findings — route via B01-00 to B01-20

### CAM-R11-01 — high: actual reticle crosses own vehicle after pitch transition

Reproduction: Light R3 model20parts, flat floor, target R3 Light at(0,1,30), cameraFOV62, default package bounds/zoom/collision parameters. Configure source rig, frame target with derived near-horizontal pitch, then `SetOrbit(yaw,18,8)` and advance yaw at95deg/s with StepCamera(frameDelta). No world wall exists during this phase.

Expected: camera's actual centre ray stays outside own colliders throughout the supported orbit/pitch input, and a safe-pose flag must not be mistaken for actual-pose safety.

Actual: **26 sampled intersections total** (Light5,Heavy4,Wide4 per resolution; Tall0). These occur only during the initial pitch transition, before the remaining constant-pitch yaw sweep. First Light720p sample: Time0.6780, camera(-0.0039,3.0547,-4.8652), positionstep0.0667m, rotationstep15.0347deg, `LastSolution.OwnHullIntersectsCrosshair=false`, actual centre-ray own-collider intersection=true, WorldClipped=false. [Exact CSV](Functional/camera-trace.csv).

The rig smooths position at source Camera001Rig.cs:135 but immediately assigns the new rotation at:161. This is a plausible mechanism for the measured transient, not an applied correction. LastSolution describes the desired proposal, not collision/smoothing-adjusted actual pose. B01-20 should add an actual-pose transition regression and resolve the camera pose without merely changing a flag or weakening an assertion.

No screenshot was taken at the exact transient intersection frame. The framing PNG documents its fixture/context; CSV provides the frame-level defect evidence. No claim of steady fixed-pitch yaw failure or manual-camera reproduction.

### CAM-R11-02 — high: corridor blocked-pivot fallback jumps and loses wall clearance

Walls are static during the sweep: for each measured hull, x=±(hull.extents.x+0.7), thickness0.4m, zextent32m, height=hullheight+5. Sweep0→180deg across70frames at pitch18. All four rigs, both resolutions, exercise the source blocked-pivot path.

Actual: **92 WorldOverlapAtAim samples**, **52 camera-centre clearance flags** across eight rows. The QA clearance predicate is camera centre within0.1m of a wall AABB (axis-aligned primitive walls), not a full near-plane/swept-camera penetration proof. Tall720p peak: time26.4793, position(1.7566,10.1600,3.2091), **9.1732m position change** with2.6086deg rotation change, WorldClipped=true/WorldOverlapAtAim=true. It occurs at zero-based corridor sample11/70 (first sample step0.2654m), after the walls were installed. This is the documented unsupported dense-geometry fallback becoming a player-visible jump, not a hidden success.

Expected: preserve a safe camera placement and readable view when the desired pivot overlaps a corridor wall; avoid abrupt fallback relocation. Source line123 chooses the blocked aim point as desired. B01-20 should provide a safe fallback and native corridor regression. Review clearance against actual geometry rather than relying on the QA proxy alone.

Instant wall insertion behind the camera is also a separate stress phase (peak8.3261m); that number is **not** evidence of a normal moving-camera approach to a preexisting wall. Corridor CSV and screenshots provide the separate continuous-turn reproduction.

## Captures and limits

- [Light720p framing](Functional/Light-1280-framing.png)
- [Tall1080p framing](Functional/Tall-1920-framing.png)
- [Wide1080p corridor](Functional/Wide-1920-corridor.png)
- [Light1080p drive/fire](Functional/Light-1920-driveFire.png)
- [Tall1080p damage](Functional/Tall-1920-damage.png)

R3 target is stationary with controller disabled; no enemy AI claim. Camera faces away from the enemy during the yaw sweep, so those frames are intentionally excluded from enemy visibility scoring. Zero muzzle error fields on non-Light rows meanNOT_RUN, not perfect weapon tracking. Observed fast bodies carry P17PhysicalShell/R4ProjectileOrigin components; no projectile-direction history was saved, so muzzle alignment does not certify ballistic impacts or input-provider integration. Separate physical fragment flight was not tracked. Exact transient reticle failure pixels and actual near-plane clearance remain follow-up evidence gaps.

Full original logs under ignored Artifacts/Local; public copies omit machine/connection identifiers and trailing whitespace. No UI automation was attached; native exit2 is the diagnostic failure status, not a shutdown crash investigation. Local executable `Builds/CAMERA-R11-QA/CameraNativeQA.exe` is a finite QA harness, not a game release.

## Safe future integration / rollback instructions for B01-40

**Preparation only; not executed.** Require a revised frozen producer SHA/manifest, independent B01-60 review, B01-40 scope/GUID check and explicit owner G2/integration authorization. Reconfirm current approved game base rather than assuming this target is still latest. Verify unchanged source/meta; import full package on an isolated integration branch, preserve existing shared parent GUID and other package ownership.

Choose **one** Transform owner for the actual game camera: disable the original P12 camera controller before enabling Camera001Rig, or adapt the geometry solution into that controller. Never enable both. Bind current R3 root after vehicle spawn/rebuild and call Configure/ResetSmoothing only at intentional context changes. Preserve existing camera/input settings for rollback. Feed weapons the actual final camera ray and existing hit resolver; do not use the solver's desired AimWorld/pose as a substitute for the actual reticle ray. Preserve combat RMB weapon-group input and garage orbit semantics. Validate real mouse aim/fire, hull destruction/debris, corridor transitions, garage/poligon/OBJ flow and both resolutions before release.

Rollback: revert only the integration adapter commits on that isolated branch, restore captured original P12 camera owner/input/scene references, remove only newly introduced CAMERA-owned objects/assets if safe, preserve `Assets/B01Packages.meta` and other packages. Re-run original camera/weapon/garage/OBJ smoke on the restored approved baseline. No main/develop merge or active-build install was performed by this QA task.

## Reproduce and decision

Original native suites: checkout494840de (runner included), Unity6000.3.14f1, run `powershell -NoProfile -File Tools/B01Validation/Run-PackageTests.ps1 -Filter B01.CAMERA001.Tests -Mode Both -Label CAMERA-R11-repro`. Expected5/5 and3/3, native0/0.

Functional: checkout244d6af, run Unity `-batchmode -quit -projectPath <checkout> -executeMethod CameraR11QABuild.Build -logFile <Editor.log>` and wait for actual Editor exit. Launch `Builds/CAMERA-R11-QA/CameraNativeQA.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -camera-qa-output <directory> -logFile <Player.log>`. It runs eight rows, writes PNG/JSON/CSV and exits2 for observed camera failures. Launch metadata includes exact assembly digest and SHA.

**Decision: requires producer repair; not ready as a green B01-60 acceptance candidate or future game integration.** Native assembly-reference fix is confirmed; framing, actual API driving/firing and HP observations have useful positive evidence. B01-00 should route the two camera findings and evidence gaps to B01-20 for the next iteration. B01-60 can independently reproduce this negative QA packet; G2 remains HOLD. No producer source or assertion was altered, no further package/task started.
