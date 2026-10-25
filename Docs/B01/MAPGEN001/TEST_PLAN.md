## W12 R3.4 SOURCE-CHANGE-SPECIFIC TEST EXTENSION
Read TEST_EVIDENCE_R34.md for actual prior native R3.3 FAIL, and RECEIVER_HANDOFF_R34.md.
(1) Source static: `python Tools/B01/MAPGEN001/check_w12_contract.py` checks 12 distinct trial declarations, Rigidbody AddForce/torque preserved, controlled low-friction test-only contact, flat-floor motor discriminator, package-owned Resources URP shader + paired .meta. No unity compile credited.
(2) Headless real C# planner+validator with surrogate UnityEngine vectors remains 1015 cases, geometry SHA unchanged; verify no MapGenPlanner/RouteValidator changes.
(3) Native Unity6000.3.14f1 PlayMode: verify retained package shader and URP tag, flat control >4m real AddForce, all12 cube independent reach checkpoints; trace floor-ray samples, max speed, heading, best goal distance and actual mass/force. Do not increase 900 steps or teleport.
(4) Bare Player both recipes independently without any scene shader material references / override and no change to Always Included shaders; verify render and screenshot 720/1080, old failure `MAPGEN001_URP_SHADER_NOT_AVAILABLE` absent, shader actually included, three regenerate/clear cycles and material cache cleanup.
(5) Independent 12 PR125 R6WheelContacts+OBJ210 functioning-wheel Light/Heavy physical trials; cube not a substitute. Actual source SHA, target SHA, raw Editor logs, process exit, NUnit XML, screenshots and per-case verdicts; FAIL triggers another source rework, B01-60 G1+G2 HOLD until green.

---
# R1 test and acceptance matrix

**Unity version:** 6000.3.14f1, URP. Open package through the repository's Unity project; use Unity Test Runner filters `B01.MAPGEN001.EditModeTests` and `B01.MAPGEN001.PlayModeTests`. See repository `Tools/CI/Run-Unity.ps1` for local Unity execution wrapper. Native logs, XML, command, exit code and commit SHA are mandatory. **No current author claims of passing Unity tests**.

- E01/E02: 100 distinct seeds per recipe, identical geometry on repeat, 6+ distinct geometry hashes per recipe.
- E03: combat 3 spawn descriptors, course 3–5 waypoint descriptors, no challenge-state implementation.
- E04: impossible wide vehicle, NaN difficulty, invalid segments reject without mutating existing map.
- E05: 60 seeds, continuous overlapping road floor and obstacle/ramp side clearance for heavy baseline.
- P01: generated real BoxColliders; `Regenerate` switches whole collision root and cleans old geometry.
- P02: real Rigidbody falls under Unity gravity and comes to rest on generated collision floor.
- P03: `Generate` invalid input does not destroy previous live map.
- MANUAL01: 3 known seeds (12345, 12346, 12347) in each recipe x two rigs (light/heavy). Record elapsed drive time, success/fall/stuck, wheel/physics abnormalities, collision seams and screenshot/video. Rover is only a simple physics sample; require true game vehicle tests after host integration, too.
- MANUAL02: 3 x regenerate/clear/re-enter scene, inspect stale physics, duplicated colliders and camera/audio listeners.
- NEG01: turn radius >15m and course impossible width should reject; do not auto-mark PASS when Unity unavailable.
- PERFORMANCE: record runtime generated game objects, 100-plan iteration time, scene allocation profile and GC in Unity Profiler; no invented budgets.

**Status at initial creation:** Unity compile NOT_RUN, EditMode NOT_RUN, PlayMode NOT_RUN, Windows build NOT_RUN, independent B01-60 QA NOT_RUN. Failure => smallest patch, new exact SHA, rerun affected and essential regressions.

## R2 target acceptance (actual execution required)
1. Unity 6000.3.14f1 **EditMode**: run `MapGenEditModeTests` and `MapGenRouteValidationTests` from the Test Runner, save NUnit XML, editor.log, exit code. Include 100 seeds per recipe for Light and Heavy, impossible blockers/start footprint/slope/turn cases. Log failing seeds/config, never count written tests as passed.
2. Unity **PlayMode**: run `MapGenPlayModeTests` and `MapGenPhysicalTraversalTests`; save the 12-case Rigidbody drive result (2 recipes x 3 seeds x 2 envelopes), generate/clear, actual collisions, atomic regenerate. Do not replace real motion with Transform teleport.
3. Manual in standalone `MAPGEN001_Demo.unity`: 3 seeds per recipe for 2 profiles, actual forward/back/turn, capture 720p/1080p screenshots/video including visible start/finish and collider contact. Compare easy/hard layouts on identical seeds for meaningful route/cover changes.
4. B01-60 independent rerun **exact R2 HEAD**, tag per-case PASS/FAIL/BLOCKED and raw logs (NOT_RUN if Unity/license unavailable); producer B01-20 repairs each real FAIL at a new SHA followed by retest.
5. B01-40 verifies .meta/GUID, source provenance, all SHA256, no R1/R6/W8 or Codex shared files modified, README/API/INSTALL compatibility; B01-00 alone determines G2 handoff to GPT OSS, never a release/merge.

## R3 additional acceptance, execute on exact final revision
- E06 (negative): 4 supported rectangle corners but unsupported centre must return SPINE_FOOTPRINT_BLOCKED.
- E07 (negative): 25m+25m 90-degree turn with 15m minimum turn radius must return TURN_RADIUS_TOO_SMALL, rather than passing via three-point circumradius.
- E08 (negative): overlapped or blocked arena enemy spawn points must return ENEMY_SPAWNS_OVERLAP / ENEMY_SPAWN_BLOCKED.
- E09: 100 arena seeds with 100% cover density and Difficulty=1 must remain validator-accepted for both Light and Heavy; covers should have >30 distinct rounded XZ coordinates.
- E10: higher difficulty must increase optional ramp pitch, while Heavy still passes the conservative reference route.
- P05: regenerate an active real-collider level 3 times; prior root is disabled synchronously, destroyed after frame, and no generated collider roots survive Clear().
- MANUAL R3: 3 seeds each recipe (7,31,83), each Light and Heavy = 12 manual drive records in addition to the native AddForce/AddTorque source test. Verify follow-camera, steering, chassis/ground collision, checkpoint passage, final arrival, reset and 3 regenerations. Capture logs and video; keep FAIL seeds, no silent retries. B01-60 must independently reproduce all PASS claims.
- Performance budget remains empirical: record 100-seed generation elapsed in Unity, allocation/GC and 12-vehicle physical completion times. No fabricated thresholds.
- Suggested Windows test commands (requires installed and activated Unity 6000.3.14f1):
  - `"$env:UNITY_EXE" -batchmode -nographics -projectPath "$env:PROJECT" -runTests -testPlatform EditMode -testResults "MapGen_EditMode.xml" -logFile "MapGen_EditMode.log" -quit`
  - `"$env:UNITY_EXE" -batchmode -nographics -projectPath "$env:PROJECT" -runTests -testPlatform PlayMode -testResults "MapGen_PlayMode.xml" -logFile "MapGen_PlayMode.log" -quit`
- Collect actual process exit, immutable HEAD, test XML, editor logs and screenshots; compilation alone is not physical driving.

### R3.1 extreme difficulty / minimum lane width (new EditMode source case)
- E11: for 100 seeds each, verify Segments=3/4/5, Density=100 and Difficulty=1 on Light + LaneWidth=10 and Heavy + LaneWidth=16 (600 generator calls). No attempt may silently substitute a seed; record explicit fail IDs. These are AUTHORED Unity tests and remain NOT_RUN until Unity execution.
