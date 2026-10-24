# B01-CODEX-PHY-R1-REPORT

**Status: IN PROGRESS.** The owner explicitly approved implementation in this chat after the supplied PROPOSAL/Queen condition was raised. No merge, deployment or next iteration is authorized by this report.

## Repository and scope ACK

- BASE_BRANCH: `feature/rrl-mov011-integration`
- BASE_SHA: `865932fd78ef936493bdc51ea9ca612fe9ec31eb` (W8 PR34 documentation head; gameplay/build source40173ee).
- WORK_BRANCH: `codex/b01-phy-grounding-core-r1`, existing isolated worktree reused.
- FINAL_SHA / Draft PR: pending final verification.
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

## Remaining acceptance gates

Broader R6/MOV009/MOV011/EMP/scanner regression is running. Additional Heavy physical recovery and garage cooldown/lost-wheel persistence must be verified. Full current-project EditMode/PlayMode, Windows build, native physics, native UI smoke, archive/EXE hashes, performance and final review are **NOT RUN for this iteration yet**. The old W8 976/398/47 results are baseline evidence only.

The known automation-attached Alt+F4 C0000005 is not claimed fixed. New-version no-client and automation-attached closes, and any human smoke result, must be recorded separately. A controlled physics fixture or input pilot must never be relabeled human manual gameplay.
