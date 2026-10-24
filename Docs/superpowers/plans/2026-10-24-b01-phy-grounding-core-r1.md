# Grounded Drive and Core Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Ground wheel traction in actual terrain contact and give a surviving Core a bounded emergency physical recovery attempt.

**Architecture:** Keep R6 as the force authority; sample each available physical wheel against actual terrain, then use those contacts for existing thrust/grip/yaw and AI StepWorld. A separate Core capability adds bounded impulses/torque to the same body, never sets pose, HP or parts. Existing MOV controllers keep their own forces.

**Tech Stack:** Unity6000.3.14f1/URP/C#, existing local licensed Unity runner and Windows x64.

**Spec:** Docs/Physics/B01-CODEX-PHY-R1-SPEC.md. The human explicitly approved implementation in this chat after its PROPOSAL gate was raised.

## Global Constraints

- BASE feature/rrl-mov011-integration at865932fd78ef936493bdc51ea9ca612fe9ec31eb; work codex/b01-phy-grounding-core-r1 in the existing isolated checkout.
- No main/develop/integration branch merge; preserve W7/W8 ZIPs and existing GUIDs, scenes, prefabs, serializable fields and packages.
- No CAMERA-001, MAP, economic, weapons or model changes; no second force solver or velocity/pose reset in air/recovery.
- Real Rigidbody/collider tests; report API/native-input/human methods separately; no unrun PASS or C0000005 fix claim.

## Review Focus

- Thin edges/walls, own compound colliders, triggers, detached or EMP wheels must not manufacture support; Task1/2 contact tests.
- Nonuniform wheel scale and XYZ mounting, reversed robot, sloped normal and asymmetric surviving supports; Task1/2 physical tests.
- Airborne tumbles, garage/range pause, lost/dead/EMP Core and rebuilt state must not permit free flight or revive parts; Task3 tests.
- Existing support and drive controllers must keep one body and distinct forces; Task4 regression.
- Stuck under a ceiling/wall or a heavy offset payload must receive only a finite bounded attempt and retain a real collider path; Task3 tests/native fixture.

### Task1: Characterize current behavior and reproduce missing contact gating

**Files:** add Assets/Tests/Editor/B01GroundedDriveTests.cs; maintain Docs/Physics/B01-CODEX-PHY-R1-REPORT.md.
**Interfaces:** consumes R3VehicleAssembly.Build, R6VehicleDrive.Step/StepWorld and existing P12 damage/EMP; produces actual RED contact evidence.
- [ ] Add real Light collider/floor tests PHY-GROUND01..06 plus trigger/self/rotation/edge filters, no mocked contact state.
- [ ] Run targeted EditMode via the local runner with filter B01GroundedDriveTests. Expected: airborne/roof/AI wheel forces violate zero traction; retain actual XML/log/exit.
- [ ] Commit tests and source audit only, before production edits.

### Task2: Existing R6 wheel forces consume physical contacts

**Files:** add Assets/R6/R6WheelContacts.cs; modify Assets/R6/R6VehicleDrive.cs only at mass/step boundaries; tests fromTask1.
**Interfaces:** R6WheelContacts.Refresh(Rigidbody,P12PipelineSafeDamageable[]) exposes GroundedCount, weighted traction/left/right/track and support normal, independent of installed-wheel count. Step and StepWorld consume the same snapshot each call.
- [ ] Derive a bounded tread probe from the actual wheel collider/transform; short skin tolerance, terrain-normal and mount-facing checks, ignore own/trigger/debris/detached bodies. No contact means no Wheel thrust/grip/yaw or air drag.
- [ ] Preserve damage/EMP/attachment tests per sample, contact-weighted asymmetry, cap sparse-contact yaw; leave Tracks/Omni/Hover delegation ahead of Wheel force.
- [ ] Characterize default planar versus full body mode; ordinary Wheels must obey gravity and be capable of physical overturning. Reuse existing R6 physical-mode switch, never add stabilization/hover to fake grounding. Document any intentional obsolete planar-test assertions.
- [ ] Run contact suite + R6Driving/R6Physics/MOV009/Steering targeted regression; expected all final contact cases PASS with native0; fix causes, do not weaken contact assertions.
- [ ] Commit verified grounding.

### Task3: Bounded Core emergency recovery

**Files:** add Assets/R6/R6CoreRecovery.cs, Assets/Tests/Editor/B01CoreRecoveryTests.cs; wire only existing vehicle initialization.
**Interfaces:** sole Rigidbody force API, actual R1/P12 Core availability/EMP and R6 contact snapshot. Serialized threshold/confirmation/lift/torque/duration/cooldown settings and read-only attempt telemetry; no key binding.
- [ ] Write physical RED PHY-CORE01..05 for sustained inversion, normal slope/air rejection, live/dead/EMP Core, finite lift/torque budget/cooldown, obstruction, surviving part state. Observe missing ability before implementing.
- [ ] Auto-confirm inversion only while grounded on actual terrain, slowly moving and playable. Apply one mass-bounded lift impulse and short capped roll torque; stop on upright/death/pause/EMP, preserve attempt cooldown and require landing/reconfirmation before retry.
- [ ] Use Core authority and existing body, never write position/rotation/velocity or restore HP/parts; coexist with MOV004 gyro.
- [ ] Run new Core + contact tests, inspect real roll outcome and limits. Commit GREEN code/evidence.

### Task4: Integration, full regression, native QA and Draft PR

**Files:** targeted existing tests if fixtures assumed ghost traction/planar locked height; QA-only native fixture if needed; Docs/Physics/B01-CODEX-PHY-R1-REPORT.md and evidence.
**Interfaces:** existing Run-Unity.ps1 EditMode/PlayMode/Build, actual ZIP/EXE hashes, native visible game.
- [ ] Run PHY-REG01 (MOV009/MOV011/all MOV modes) and PHY-REG02 (actual loss/garage/next encounter). Preserve geometry/HP/resources and original mode solvers.
- [ ] One fresh whole-branch review, rule on findings, one RED/GREEN fix pass; no per-task reviewer or second review.
- [ ] Full EditMode and PlayMode after targeted GREEN, Windows build if licensed support remains available. Record all failures/skip/native exits and immutable source.
- [ ] Windows native actual Light/Heavy flat/slope/roof/recovery/loss/camera/garage/one encounter transition; distinguish controlled physics QA, native UI/pilot and human smoke. Alt+F4 separately classed; no unproven fix.
- [ ] Complete source/report/evidence, CRC/extraction and actual launch, SHA256 ZIP/EXE. Push own branch, create/attach Draft PR, report DONE/PARTIAL/BLOCKED/NOT_RUN per requirement, stop at this iteration.
