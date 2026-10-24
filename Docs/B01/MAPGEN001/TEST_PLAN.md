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
