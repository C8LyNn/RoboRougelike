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
