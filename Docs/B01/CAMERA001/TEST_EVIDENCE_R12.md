# W12 / CAMERA-001 R1.2 — SOURCE PRODUCER + NATIVE RETEST CONTRACT

## Proven native RED (independent Codex, previous source R1.1 ONLY)
- Source R1.1 frozen Draft #214 `d48108f56d3fe0790ad715683e77b2de9219e4b1`; native Codex QA [#234](https://github.com/C8LyNn/RoboRougelike/pull/234) QA HEAD `17a2399f74b7562be19c2b429dc0b1312957c66a`, actual Unity 6000.3.14f1 Windows.
- Compilation PASS, EditMode 5/5 PASS, PlayMode 3/3 PASS, Player functional FAIL (8 distinct rig-resolution rows executed).
- CAM-R11-01: 26 transient ACTUAL centre-ray intersections with real R3 Light, Heavy and Wide colliders at pitch transition (both resolutions); desired-solution-only OwnHullIntersectsCrosshair false. Source applied smoothed position then *instant desired rotation*.
- CAM-R11-02: continuous Tall720p corridor sweep jump **9.1732m** at sample11/70; original blocked pivot selected `desired=aim`, even when pivot was inside world geometry. 92 aim-pivot overlap samples and 52 centre-clearance flags across rows. Old QA clearance was approximate centre-AABB distance, not actual near plane penetration.
- Measured Light muzzle-reticle error≤0.0198deg and 11 real P17PhysicalShells in two drives are limited pre-fix positives, NOT player LMB/RMB regression certification. Never convert QA's static screenshot/CSV into new R1.2 PASS.

## Source fixes — see frozen R1.2 producer Draft (new HEAD in #66)
- CAM-01: `Camera001Rig.StepCamera` now obtains the physically constrained final camera `candidate` first and calculates **real final rotation** via `Quaternion.LookRotation(aim-candidate)`, not `LastSolution.Rotation`; measures `ActualOwnHullIntersectsCrosshair` using actual final ray (conservative hull Bounds plus actual Collider.Raycast). Explicit `LastAimErrorDegrees` derives from final transform. Earlier solution's `OwnHullIntersectsCrosshair` still describes desired proposal only, not a source of fake PASS.
- CAM-02: when desired pivot overlaps world, **retain previous safe position** (no `desired=aim` snap). Otherwise spherecast aim to desired, SmoothDamp, **cap movement to 15m/s × deltaTime**, sweep the ACTUAL previous→candidate camera sphere through world geometry and fail-closed on buffers. Independently check final camera position with near-plane-enclosing sphere and world overlap. On new intersection, freeze on previously safe position rather than teleporting. ResetSmoothing or explicit `StepCamera(0)` remains an intentional rebuild/respawn snap, distinct from ordinary continuous frames. Unresolvable initial overlaps remain **reported RED**, never silently accepted.
- No geometry generator, R3/R6 gameplay, input mappings, zoom range 3–18m, weapon LMB/RMB or production camera transform modified. Only opt-in component. One LateUpdate owner, receiver must never run it simultaneously with P12.
- Telemetry: `WorldClipped`, `WorldOverlapAtAim`, `WorldOverlapAtCamera`, `ActualOwnHullIntersectsCrosshair`, `LastCameraStepMeters`, `LastAimErrorDegrees`.
- Demo `CAMERA001_Demo.unity` uses existing scene/meta: TAB cycles Light/Heavy/Tall/Wide; C places/removes simulated corridor, X disables detachable top part, A/D yaw, W/S pitch, Q/E zoom; displays actual collision telemetry. All demo inputs are optional QA-only; no game input change.

## Regression matrix — source defined; licensed Unity runs NOT_RUN by producer
- Existing original 5 EditMode NUnit tests unchanged (840 desired proposals inside one original NUnit test). Existing original 3 PlayMode NUnit tests unchanged.
- New 8 named CAM-01..08 `[UnityTest]` methods — Light/Heavy/Tall/Wide ×1280×720/1920×1080. Each tests 242 frames (pitch transition from −8° to 18°, 360° azimuth), actual Collider.Raycast, actual conservative own-ray safety, aim ≤0.05°, ≤0.2501m/frame at60Hz and world camera sphere clearance.
- CAM-09 71-step Tall corridor with static paired walls and pivot obstruction: movement max≤0.2501m/60Hz, zero end-camera world sphere overlap, no self-ray intersections.
- CAM-10 detachment disables extra collider and forces re-framing against new hull with final aim and speed bounds. Existing 3 cases remain.
- 11? No: **10 new named tests**, original 3 PlayMode = **13** PlayMode tests; original 5 EditMode = **18** expected NUnit methods total (pending native).
- Offline `geometry_probe.js` executes 840 pure JS analytic samples, `check_r12_contract.py` checks source structure, test count, negative original-rotation mutation and 9.1732m→0.25m analytic speed bound. They **do not execute Unity, PhysX, R3, actual rendered pixels or human gameplay**.

## Native retest after source freeze and independent gates
1. Confirm exact SOURCE SHA and full stacked PR#134+#214+#R1.2, per-file SHA256/bytes, shader dependencies, original stable .meta, shared parent GUID *without overwriting recipient GUID*, target/R3/R6/Pack reserved status.
2. Disposable Unity6000.3.14f1 URP environment: actual project compile exit/Editor.log; EditMode 5/5, PlayMode >=13 (3 original+10 new) capture raw NUnit XML, exit codes and all failures. Do not fake passed method counts.
3. Actual R3 vehicle rigs Light/Hvy/Tall/Wide 720p/1080p native Windows Player, full 360° with pitch transitions, wall/occlusion and corridor 60s; use actual collider-ray and camera near-plane/volume penetration telemetry. Reproduce original 26/9.1732 failures then prove improvements on new frozen source. Preserve 34 original PNG/CSV separately from new screenshots/logs; capture exact-failure frames.
4. Verify target/muzzle/reticle angular ≤0.05°, LMB/RMB two families, own weapon fire while driving, 3–18m zoom, part detach and actual damage, finite 15m/s continuous travel except explicitly reset, and one LateUpdate writer. Native R3 functional/cursor/garage integration belongs to Codex *after* HQ G2, not producer.
5. B01-60 independently confirms G1, B01-40 source/target G0 SHA/GUID/rollback, B01-00/HQ G2 HOLD until gates. If new native FAIL, return exact frame/source SHA, fix in new producer iteration.

## Honest current tests
R1.2 *CI source* results must cite specific successful run; manual real Unity compile/EditMode/PlayMode, 8 native rig×resolution Windows functional runs, actual R3 muzzle/HP/weapon regression = **NOT_RUN BY B01-20**. Existing original R1.1 native PASS applies only to R1.1. No main/develop merge, Codex game integration, or BUILD edits.
