# B01-CODEX-PHY-R1-REPORT

**Status: READY FOR DRAFT REVIEW, with the explicit verification limits below.** The owner explicitly approved implementation in this chat after the supplied PROPOSAL/Queen condition was raised. No merge, deployment or next iteration is authorized by this report.

## Repository and scope ACK

- BASE_BRANCH: `feature/rrl-mov011-integration`
- BASE_SHA: `865932fd78ef936493bdc51ea9ca612fe9ec31eb` (W8 PR34 documentation head; gameplay/build source40173ee).
- WORK_BRANCH: `codex/b01-phy-grounding-core-r1`, existing isolated worktree reused.
- FINAL_SHA (gameplay, build and native QA): `523046f5bf6ddab0caf62253615c962e808c3053`. Final Editor-fixture checkpoint: `54f0192b89ffac9f6989da36e8f5d893e631d600`; its five changed files are Editor tests only. Later commits contain release tooling, evidence and documentation. Draft PR: pending publication.
- Initial worktree: clean, no unpublished tracked changes. Latest refs fetched before changes. No later PHY001/PHY002 implementation found in local/remote history or issue search at that snapshot. Newer model/pilot branches are independent proposals, not a newer verified gameplay base.
- INBOX #79 and central #71 read. INBOX had no comments or this iteration's approval; the owner supplied approval directly here. No foreign task was started or message posted on the basis of issue content.
- codex_scope_status: CONFIRMED; active_task: B01-CODEX-PHY-R1.
- reserved_paths: R6 grounded wheel/contact/Core recovery, the one R3 assembly capability hook, corresponding physics tests/QA and report. camera_fix_in_progress: NO; weapon_economy_in_progress: NO; map_editor_in_progress: NO; visual_models_in_progress: NO.
- allowed_package_scope: independently isolated camera/map/GDD/visual packages outside the listed physics paths; coordinate shared assembly edits before handoff.
- Unity6000.3.14f1/URP, package versions, old GUIDs/scenes/prefabs retained. main/develop and every existing integration branch are unchanged. W7 and W8 ZIPs remain frozen.

## Verified initial state and approach

R6 counted living mounted wheels for force, without checking terrain contact, in both `Step` and AI `StepWorld`. Its default mode disabled gravity and froze vertical position/pitch/roll. MOV009 and MOVSeries could already opt into unconstrained gravity. Tracks/Omni/Hover already had separate authoritative contact/lift/force logic. MOV004 was optional continuous gyro stabilization; it did not provide a Core-owned, contact-confirmed, bounded emergency attempt with cooldown. No existing Core Recovery was found.

Wheel force now uses actual available physical treads, short shape-scaled terrain probes, physics-pose mount axes and support normals. Own solids/triggers/released or EMP parts cannot provide terrain support. Installed-wheel and grounded-wheel counts remain distinct. Existing bounded R6 thrust is applied at the weighted contact center, so mass/contact asymmetry produces a physical moment. No contact means no Wheel thrust, grip, yaw servo or artificial air drag; momentum/gravity and independent MOV forces remain.

Ordinary R6 now uses its existing gravity/unconstrained physical mode, with the small compound-contact friction already used by MOVSeries. Old locked-height fixtures are adapted to physical settling; the explicit one-side-loss fixture removes that side, while new cases separately pin one/two-wheel loss. The existing R6 inertia model is retained.

Core Recovery uses one existing Rigidbody and the actual living, owned Core, canonical `DeviceReadyAt`, and the existing EMP bridge/clock. It adds no HP, parts, weapons, pose or velocity reset. It reuses MOV004's finite yaw-preserving tilt math during one bounded attempt; a capped gravity-lever term lets it start lifting from a broad supported roof. This is separate from ordinary gyro stabilization. The new electronic Core ability can be EMP-disrupted; Core HP and defeat remain under the existing authority.

Default inspector tuning: threshold110°, confirmation1.5s, duration2s, cooldown8s, target upward speed1.4m/s, maximum lift1800N·s, maximum torque30000N·m, angular acceleration9rad/s², angular damping6.5. Finite validation and bounded ranges prevent zero-cooldown/unbounded settings. Initiation requires real current terrain contact and slow motion in a playable phase. Airborne rotation, ordinary slopes, pause, defeat, missing/dead/EMP Core cannot start another attempt. No key is taken over.

## Actual RED / GREEN evidence so far

All paths below are under `Artifacts/Local/`; counts describe separate runs, not summed unique coverage.

| Run | Actual result | Meaning |
|---|---|---|
| PHY-contact-red-49689a49226946a0801e69b5671cbfd6 |0/8 PASS,8 FAIL,0 SKIP, native2 | Air x1→.514688, AI air speed2.166666, roof speed2.162191, jump x2→.839421; other cases expose absent grounded telemetry |
| PHY-contact-green-363b33a3862e41b8947998fdc92c9212 |8/8 PASS, native0 | Initial real floor/air/roof/loss/edge/jump/filter correction |
| PHY-contact-regression-9992a7c1a5bf459cab9aed69cb0d569b |41/45 PASS,4 FAIL, native2 | Exposed scaled/slope query pose mismatch and old planar/minimum-yaw assumptions |
| PHY-probe-diagnostic-eb607877d39a4f11b39d287a615cd69b |0/2 PASS,2 FAIL, native2 | Actual Rigidbody1.3m vs interpolated Transform.69m; physics Collider bounds vs render-pose ClosestPoint produced wrong reach |
| PHY-core-red-contact-retest-acb206e2d27c4e34b9169bed92dccc91 |13/20 PASS,7 FAIL, native2 | All11 new contact cases PASS; six absent Core cases RED; one old single-wheel yaw fixture remained |
| PHY-core-green-6e3f3df54572493ea4b74d22876a88f7 |19/20 PASS,1 FAIL, native2 | Emergency attempt fires but free-flight PD cannot overcome supported roof load; no recovery PASS claimed |
| PHY-core-load-green-806308d054e24fa29472ee84cbce634a |20/20 PASS,0 FAIL/SKIP, native0 |328kg real robot rolls roof→up(0,1,0), settles y.65, HP/parts unchanged; finite cap/cooldown/air/EMP/death gates pass |

Source checkpoints: tests/spec `14c3d04`; verified grounding `ed62cd9`; initial Core with additional pending regression tests `3505cb1`. Intermediate runs include deliberately uncommitted implementation states during TDD; do not label those runs as exact immutable release builds.

## Final targeted verification and review

| Run suffix / evidence directory | Actual result | What it establishes |
|---|---|---|
| `PHY-final-physics-green-7918e9f6e5464c8abc746114a526ab95` |35/35 PASS,0 FAIL/SKIP, native0 |11 contacts,8 Core,2 actual NPC,8 steering,6 MOV009 physics cases at the integration checkpoint |
| `PHY-emp-regression-0d406304fe3f43d4953e3fcdfe4ee9e8` |12/12 PASS,0 FAIL/SKIP, native0 |Actual W6 EMP physical suite; earlier shorthand `RRLW6EmpTests` matched no class and is not counted |
| `PHY-review-red-7d661d37641d48f88d24a641afccae7c` |1/2 PASS, native2 |Raised suspension lacked grounding notice; production-default offset-load cage already passed characterization |
| `PHY-review-green-0cddc75a5fcf4b9ca8b883e71ddc6725` |2/2 PASS,0 FAIL/SKIP, native0 |Grounding notice and actual raised→lowered driving; default Core torque in real ceiling/four-wall cage |

Further real failures were retained rather than hidden: Heavy fixture initially used a dirty newly parented Transform pose; the corrected explicit physics fixture proves a1312kg one-attempt recovery. MOV009/MOV011 Crab initially lost actual tread support, then exposed the known motor yaw couple under healthy full support. Adapter default height `.82→.77m` aligns the existing spring compression with real treads. The existing bounded steering servo compensates only that known motor-yaw couple with complete balanced support; partial-contact/damaged asymmetry remains physical. Source package algorithms remain unchanged.

One fresh read-only whole-branch review (`b01_phy_review`, GPT-6-astra/high, base865932f..ceeac6f) found0 Critical,2 Important and1 Minor. No second reviewer was dispatched. The two important findings were raised suspension compatibility and absent representative default-torque obstruction coverage. The new cage has unchanged30000Nm settings and a344kg asymmetric payload; actual peak torque6204.521Nm, one finite attempt, maximum geometry overlap.022682m, maximum fixed-step rotation2.181838°. HP/parts remain unchanged. This was a coverage gap: it passed first; no fictitious tunnelling RED is claimed and no unnecessary CCD rewrite was made.

**MOV009 behavior change:** positive FullSupport height can physically lift the rigidly mounted Wheels off the ground. Height adjustment is preserved, but this is a stationary configuration until lowered; the HUD now explicitly tells the player to lower suspension to regain contact/drive. The real `.2m`-raised test confirms0 contact and0 horizontal wheel propulsion, then actual contact and movement after lowering to0. No extended probe or hidden traction disguises unloaded wheels. Continuous mechanical wheel travel would require a separate design outside this iteration.

## Full project verification

- Full EditMode: **992/999 PASS,7 FAIL,0 SKIP, actual exit2**. Evidence `Artifacts/Local/20261024T214745Z-a1e6bfabd95a4338a13cba177118b465/`. This failed full-run XML is retained, not relabeled green.
- Follow-up of all five affected classes: **28/28 PASS,0 FAIL/SKIP, actual exit0**, `PHY-full-regression-corrections-6014d4ce57b44426b5b732f1e7e9b60e`. Every prior failure has a passing current counterpart. The correction changes Editor fixtures only; `git diff 523046f..54f0192 -- Assets ':!Assets/Tests/Editor'` is empty. `editmode-coverage-reconciliation.json` identifies all seven cases, the one renamed method, XML hashes and999 distinct current cases covered with0 unresolved failures. This is **combined coverage, not a second full green Unity run**. A fresh full rerun after fixture-only corrections is **NOT_RUN**; the cost/ruling is recorded below.
- Full PlayMode: **398/398 PASS,0 FAIL/SKIP, actual exit0**, `Artifacts/Local/20261024T223139Z-70f45a0d5ec24dd09a9d8ad188b54215/`, fixture checkpoint54f0192 and unchanged gameplay523046f.
- Final Windows x64 build: **PASS**, actual Editor exit0, source523046f. Evidence `Artifacts/Local/20261024T182803Z-7df0687345a3471dbb6a7079ceb125d2/Build.log` and `Build-process.json`. Retail103275407 build-report bytes, QA103359247; each0 errors and4 existing CS0414 prototype warnings (MOV004/MOV006/MOV009 unused input fields). Retail excludes the QA define; both variants compiled and the define was restored.
- First sandbox build failed before compilation: licensing IPC/WMI timeout, native exit−536870911. No Editor remained; only the orphan lock was removed before the successful normal-user build. This is an environment failure, not a game or successful compile result.
- The previous W8 976/398/47 are historical baseline counts, never substituted for current full results.

The seven full-run failures and their corrections:

| Case | Actual cause / retained acceptance |
|---|---|
| MOV009 without modules | Obsolete gravity=false/frozen-Y expectation; now requires actual gravity/unconstrained R6, no added suspension, real settling and4 contacts |
| R5 arena traversal | Obsolete exact NPC rootY=0; now bounds physical settling and retains the18-second industrial-obstacle progress requirement |
| R6 overlapping debris | Owner was still falling from starter clearance, so total velocity measured gravity as an ejection impulse; settle the owner before the unchanged ignore-pair/velocity checks |
| R6 low/high-speed barriers | Obsolete exactY=.7 plus a wall positioned from the main box instead of the entire compound; settle first and place the wall ahead of all solids. Original peak/spin/bounce limits remain; measured peaks.60/9.00m/s, spin0/.06rad/s |
| R6 wheel-less enemy | Physical hull friction stops it gradually: actual speed.53m/s at1.5s and0 at3s; test now allows3s while retaining<.5m/s and0 active wheels. No air drag was restored |
| Point Defense rocket intercept | Test rig was suspended4m high, previously frozen there; add actual static support and settle with gravity active. Real rocket HP/interceptor/ammo/repair assertions are unchanged and pass |
| Saw physical cutting | Same suspended rig moved away from its fixed target under gravity; add actual support and settle. Logical single damage event, hard-wall jam, heat, destruction and repair assertions remain and pass |

No weapon, hull-friction, AI decision, damage or product runtime code was changed for these seven corrections. The packaged gameplay/native tests remain exactly source523046f.

## Actual Windows QA — methods kept distinct

1. **Controlled native physics fixture:75/75 PASS,0 errors, actual Player exit0**,1280×720/D3D11, source523046f. `B01-native-Physics-20261024T182851Z`: actual garage armor purchases17/85scrap, real Light328kg/Heavy1312kg, real floor/slope driving, no airborne player/AI thrust, gravity/inertia, roof no wheel traction, one simulated recovery with intermediate roll screenshots, unchanged HP/parts, actual wheel loss, EMP/cooldown/dead Core and disposable range→garage→menu. This fixture explicitly positions and damages vehicles and uses a fixed inspection camera. It is **not manual gameplay**. The preliminary run stopped after the Light Core-death case because the QA fixture waited on paused simulation time; its owned process was terminated (exit−1). The product correctly paused the disabled range. QA now uses realtime for that wait; the complete75-check rerun passed.
2. **Controlled native W8 compatibility:47/47 PASS,0 errors, actual exit0**,1920×1080/D3D11, source523046f. `B01-native-W8-20261024T182949Z`: actual scanner/pulse distinction, wall visibility, R6 Crab, MOV009-supported Crab and restored workshop. This is API-driven native QA, not manual play.
3. **Native UI operator + ordinary combat-input pilot: PASS for the scoped smoke**,1920×1080, source523046f. `B01-native-Pilot-20261024T214450Z`: UI selection/route, two actual wins with active AI and228 physical rounds, reward selection, destroyed rifle0% in inventory, hull repairs8scrap, rifle rebuild36scrap, bonus1×2×2 mounting and rifle mounting, next route/fight, Core180→270HP and two online weapons, second reward→garage→range. Real scrap105→97→61, later225 after the second reward. No damage/reward/currency API shortcut. The ordinary Light range/garage camera was inspected in the preceding operator session; the rebuilt Heavy range camera in this session. Combat aiming uses the declared pilot camera, so combat images do not prove unassisted aim/camera behavior. A brief W pulse is not claimed a continuous human driving test. The earlier operator process expired during a session interruption (timeout/exit−1); it is not counted as a completed smoke or crash reproduction.
4. **Human fresh extracted retail Alt+F4: PASS, actual exit0.** The owner reported “Zamknęła się poprawnie” in this chat on2026-10-24. `B01-extracted-human-close/process.json` records source523046f, exact extracted EXE/DLL hashes, no Sky/UIA connection, no timeout. Editor regression was running independently. This is a human close test, **not a human full expedition playtest**.

All native records, selected real PNGs, process metadata and the human result are under `Docs/Evidence/B01-PHY-R1/`. Recovery roof/intermediate/upright images for both sizes and the combined COM/SEN/MOV009/MOV011 HUD were visually inspected and are not black/overlapping. Test XML/process records are retained in `unity-results.zip`; `index.json` records original local logs and their SHA256. Raw full logs remain at the exact `Artifacts/Local` paths on this machine. Direct final-DLL inventory confirms seven checked QA classes absent from retail, five MOVSeries-defined classes present in QA and product CoreRecovery present in retail; see `retail-qa-exclusion.json`.

## Shutdown investigation

Automation-attached Alt+F4 was reproduced on source523046f: actual exit−1073741819 (`C0000005`) after the two-win operator smoke. Game diagnostics reached `OnApplicationQuit`, with0 logged game errors. In contrast, both fresh no-Sky native fixtures terminated through `Application.Quit` with0, and the owner closed the fresh extracted retail by Alt+F4 with0. This narrows the reproducible condition to the automation-attached environment; it does **not** isolate which Unity/provider/environment component causes it. No shutdown fix is claimed. Status: **PARTIAL / known unresolved issue**.

The exact process54396 Windows minidump confirms exception`0xc0000005` in `UnityPlayer.dll`, module offset`0xb1b031`, exception parameters`[0x0,0xe0]`; `UIAutomationCore.DLL` was loaded. `shutdown-minidump-summary.json` preserves the exception/module stream inventory and dump SHA256`f717c523f9dbf0912e968eefa341d39fcbef868bdd4ea468c1e0bce1843d6f94`. The37418032-byte raw dump remains at `C:\Users\kubap\AppData\Local\CrashDumps\RoboRougelike.exe.54396.dmp`; it is not uploaded. This is module-level evidence, not a symbolically unwound stack or proof of a particular offending callback.

## Performance observations

Current1920×1080 native input-pilot smoke,60FPS target: stage1 median16.6669ms/p9516.7009ms/p9917.0441ms/max24.8317ms across735 frames; stage2 median16.6668ms/p9516.6779ms/p9916.8330ms/max17.1612ms across986 frames. Up to3 active enemies,229 damageable parts,15 simultaneous shells and11 debris were observed; sampled allocation peak163034083bytes. Maximum observed transition.070824s,0 game errors. This is a short live-machine, frame-capped observation with automation/background activity, **not an isolated benchmark** or proof for arbitrarily large assemblies/all six stages. Wheel shape arrays are cached; no extra body/force solver was added.

## Complete Windows artifact

- ZIP: `C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_B01_PHY_R1_Windows_x64.zip`,38488394bytes,203 runtime files plus README/manifest.
- ZIP SHA256: `3b57f2b5e6a2130cbeb66eed6c299788e023fc61f0ee3dfb74002436a44143dd`.
- Extracted EXE: `C:\Users\kubap\Desktop\Prototypy\Releases\B01-PHY-R1-extracted\RoboRoguelike_B01_PHY_R1_Windows_x64\RoboRougelike.exe`.
- EXE SHA256: `17ad8b27359b94eac8d2f44a0052d8789dc3ff5328f44ccb8c2740be4a375eb8`.
- Retail `Assembly-CSharp.dll` SHA256: `84bbcc43d9eca359f8fbb71ffe480811f63fff5d61fc06404bd2f386bfbc6d30`.
- Archive includes EXE, Data, UnityPlayer, crash handler, MonoBleedingEdge and required DLLs. ZIP CRC, fresh extraction and every runtime-file SHA matched; the actual extracted retail was launched and human-closed with0. EXE alone is a Unity stub: use DLL/source/manifest hashes to identify gameplay.
- W7 frozen archive SHA256 `41de715507c1493fc9ef5bbcce3fe3a772305641dd7935b6b84639650608d5d5`; W8 `2d40f42e223580b8dda84ffb5b57af6bdb53ab016b2915e01754b4624a83da6b`. Both were rechecked unchanged.

## Changed files and scope

Product: `R6VehicleDrive.cs`, new `R6WheelContacts.cs` and `R6CoreRecovery.cs`; one capability hook in `R3VehicleAssembly.cs`; physical handoff only in `P12_PipelineSafeEnemyTurret.ConfigureMovement`; `MOV009VehicleAdapter.cs` and its HUD grounding notice. QA: new `B01PhysicsRuntimeQA.cs`, existing `RRLW8RuntimeQA.cs` contract adjusted from planar to grounded. Tests: new B01 Grounded/Core/Enemy suites; existing `R6DrivingTests.cs` and `MOV009PhysicsAcceptanceTests.cs` adapt obsolete physical assumptions/add raised-wheel coverage. The full-run follow-up corrects fixtures in `MOV009IntegrationTests.cs`, `R5ArenaTests.cs`, `R6ReviewTests.cs`, `RRLW6PointDefenseTests.cs` and `RRLW6SawTests.cs`. All new Unity files have their own `.meta`; existing GUIDs, scenes, prefabs, packages and engine version are retained. Release tool adds the correct report path and B01 instructions to the README. Spec, plan, this report and durable evidence are the documentation changes.

## Review rulings and remaining limits

- Reuse the isolated checkout/one final reviewer and proceed on the owner's approval. Cost if wrong: branch review must catch implementation mistakes; no shared merge is performed.
- Enable existing physical R6 gravity mode because frozen Y/roll/pitch cannot support the requested falling/overturning. Cost if wrong: old planar assumptions become invalid; bounded fixture changes and full/native regression are required.
- Use physics-pose shape geometry, actual contact thrust and the existing inertia model. Cost if wrong: load transfer changes driving feel; actual R6/MOV regressions/native tests gate acceptance.
- Replace the old minimum yaw-from-one-wheel fixture with explicit complete-side loss; new contact tests still cover one/two wheel loss. Cost if wrong: no guarantee of an arbitrary minimum single-wheel yaw on frictional terrain.
- Use original MOV004 math with a capped support-lever term and a30000Nm Heavy-capable budget. Cost if wrong: extreme geometry can remain stuck; no universal recovery/tunnelling guarantee.
- Set MOV009 integration default.77m to retain actual tread contact; retain positive-offset physical height as stationary with a clear notice. Cost if wrong: a designer may need a later actual suspension-travel design; this iteration supplies no ghost traction.
- Use chassis physical up for terrain-facing probes because legacy NPC Cylinder up is its axle; preserve actual collider XYZ geometry. Restore R6 gravity/cache after NPC reconfiguration. Cost if wrong: unusual mounts/terrain can conservatively lose contact; AI decisions/weapons are unchanged.
- Compensate only the known motor yaw couple in the existing capped MOV011 healthy/full-contact servo. Cost if wrong: steering behavior under load differs; damaged/partial support remains uncompensated and regression tested.
- Resolve the review's pending full/native/performance/camera/human/PR judgments through actual records; human gameplay and a new six-stage native completion remain **NOT_RUN**. Cost if wrong: those gaps remain explicit; the old W8 run does not close them.
- Test default-torque obstruction with actual compound geometry; do not claim absolute collision proof. Cost if wrong: untested extreme arrangements need later investigation.
- Leave new maps, CAMERA redesign, weapons/economy, visual assets and multiplayer outside scope. Cost if wrong: their independent issues remain unresolved here.
- Correct the seven gravity-dependent Editor fixtures and rerun their entire five classes plus full PlayMode, preserving the failed full XML and runtime-source invariant. Cost if wrong: no second all-in-one full green run establishes test-order interactions after fixture changes. Current coverage reconciles999 distinct cases/0 unresolved; rerun full CI before merge if the accepting reviewer requires that stronger gate. This avoids repeating a roughly37-minute suite for Editor-only fixture changes.
- **Deferred minor:** add focused nonuniform-scale, combined XYZ and chassis-misaligned slope/edge fixtures. Current tests use uniform2×, a single mount axis and aligned slope. No demonstrated defect was found, but the conservative center-ray design has less coverage on those geometries.

## Acceptance status / next step

| Task | Status |
|---|---|
| Audit/base/isolated branch |DONE |
| PHY-001 grounded player/NPC Wheels |DONE, conservative static-terrain center probes |
| B01-PHY-002 bounded Core ability |DONE, actual Light/Heavy + offset-load cage; no universal recovery guarantee |
| Targeted R6/MOV/W8/EMP regressions |DONE; raised positive MOV009 explicitly stationary until lowered |
| Full EditMode / PlayMode |DONE: raw992/999 + corrections28/28; combined current999 covered/0 unresolved, **fresh full rerun NOT_RUN**. Play398/398 |
| Windows retail+QA,75 physics/47 W8 native |DONE |
| Native UI smoke, repairs/bonus/next fight/camera |DONE by operator/input pilot; human full gameplay NOT_RUN |
| ZIP/extraction/actual launch/human close |DONE |
| C0000005 root-cause fix |PARTIAL: reproduced with UI automation, normal human exit0, not fixed |
| Draft PR/final evidence |Evidence DONE; Draft publication pending |

Decision: **READY FOR DRAFT REVIEW** with known automation shutdown failure, explicit fixture-only follow-up method and deferred geometry coverage. Submit to Queen/B01-60 for acceptance; a stricter fresh full CI can be required before merge. No merge, deployment or next iteration is performed.
