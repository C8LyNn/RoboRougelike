# Actual R3/R6 traversal table

19 independent physical attempts;17 distinct recipe/rig/seed/difficulty configurations. Historical normal7/31 repeat two base controls. API force driver, not human play.

| Group | Recipe | Rig | Seed | Difficulty | Result | Distance m | Seconds | Contacts min/max | Zero queries | Seams | Max tilt deg | Parts before/after | Finish |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASE | CombatArena | Light | 7 | 0.5 | PASS | 48.16 | 6.74 | 4/4 | 0/274 | 0 | 0.00 | 20/20 | True |
| BASE | CombatArena | Heavy | 7 | 0.5 | PASS | 48.14 | 10.80 | 12/12 | 0/479 | 0 | 0.00 | 139/139 | True |
| BASE | CombatArena | Light | 31 | 0.5 | PASS | 48.16 | 6.70 | 4/4 | 0/274 | 0 | 0.00 | 20/20 | True |
| BASE | CombatArena | Heavy | 31 | 0.5 | PASS | 48.14 | 10.80 | 12/12 | 0/479 | 0 | 0.00 | 139/139 | True |
| BASE | CombatArena | Light | 83 | 0.5 | PASS | 48.16 | 6.70 | 4/4 | 0/274 | 0 | 0.00 | 20/20 | True |
| BASE | CombatArena | Heavy | 83 | 0.5 | PASS | 48.14 | 10.81 | 12/12 | 0/479 | 0 | 0.00 | 139/139 | True |
| BASE | ObstacleCourse | Light | 7 | 0.5 | PASS | 54.64 | 7.96 | 0/4 | 1/338 | 3 | 10.55 | 20/20 | True |
| BASE | ObstacleCourse | Heavy | 7 | 0.5 | PASS | 54.44 | 12.44 | 10/12 | 0/561 | 3 | 1.73 | 139/139 | True |
| BASE | ObstacleCourse | Light | 31 | 0.5 | PASS | 54.38 | 7.93 | 0/4 | 1/336 | 3 | 10.48 | 20/20 | True |
| BASE | ObstacleCourse | Heavy | 31 | 0.5 | PASS | 54.29 | 12.40 | 12/12 | 0/559 | 3 | 0.60 | 139/139 | True |
| BASE | ObstacleCourse | Light | 83 | 0.5 | PASS | 54.48 | 7.96 | 0/4 | 1/337 | 3 | 10.56 | 20/20 | True |
| BASE | ObstacleCourse | Heavy | 83 | 0.5 | PASS | 54.40 | 12.42 | 12/12 | 0/560 | 3 | 0.73 | 139/139 | True |
| HISTORICAL_NORMAL | ObstacleCourse | Heavy | 2 | 0.5 | PASS | 54.42 | 12.42 | 12/12 | 0/560 | 3 | 0.61 | 139/139 | True |
| HISTORICAL_NORMAL | ObstacleCourse | Heavy | 7 | 0.5 | PASS | 54.43 | 12.42 | 12/12 | 0/560 | 3 | 0.71 | 139/139 | True |
| HISTORICAL_NORMAL | ObstacleCourse | Heavy | 31 | 0.5 | PASS | 54.32 | 12.40 | 12/12 | 0/559 | 3 | 0.62 | 139/139 | True |
| HISTORICAL_HARD | ObstacleCourse | Heavy | 0 | 1.0 | PASS | 54.38 | 12.44 | 12/12 | 0/561 | 3 | 0.78 | 139/139 | True |
| HISTORICAL_HARD | ObstacleCourse | Heavy | 2 | 1.0 | PASS | 54.55 | 12.43 | 12/12 | 0/561 | 3 | 0.78 | 139/139 | True |
| HISTORICAL_HARD | ObstacleCourse | Heavy | 7 | 1.0 | PASS | 54.68 | 12.48 | 12/12 | 0/563 | 3 | 0.50 | 139/139 | True |
| HISTORICAL_HARD | ObstacleCourse | Heavy | 19 | 1.0 | PASS | 54.62 | 12.48 | 12/12 | 0/563 | 3 | 0.52 | 139/139 | True |

Contacts are actual R6WheelContacts queries at configured0.02s fixed cadence, after warmup. Three Light course rows have one zero query each. Raw longestNoContact=0 undercounts a single-query episode; do not read it as continuous contact. Use zero-query counts above; substep contact duration was not measured.

A PASS means the real chassis reached all generated waypoints and finished within2.4m horizontally, without loss of parts, fall, flip or unresolved stall under the65s limit. Optional side ramps were avoided in this protected-spine matrix; see separate RampProbes data.
