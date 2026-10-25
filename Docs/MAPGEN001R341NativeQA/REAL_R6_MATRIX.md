# Actual R3.4.1 real R3/R6 matrix

4first seed7 attempts +8new seed31/83 attempts;12distinct configurations, no duplicate seed7 retest. API-steered canonical force/contact solver, not manual player input.

| Recipe | Rig | Seed | Status | Distance m | Seconds | Contacts min/max | Zero queries | Query episode estimate s | Seams | Parts | Final XYZ | Finish |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CombatArena | Light | 7 | PASS | 48.160 | 6.760 | 4/4 | 0/274 | 0.000 | 0 | 20→20 | -0.106/0.650/22.739 | True |
| CombatArena | Heavy | 7 | PASS | 48.138 | 10.802 | 12/12 | 0/479 | 0.000 | 0 | 139→139 | 0.080/0.650/22.630 | True |
| ObstacleCourse | Light | 7 | PASS | 54.474 | 7.959 | 0/4 | 1/337 | 0.020 | 3 | 20→20 | -2.870/0.650/58.868 | True |
| ObstacleCourse | Heavy | 7 | PASS | 54.454 | 12.418 | 12/12 | 0/560 | 0.000 | 3 | 139→139 | -2.578/0.650/58.791 | True |
| CombatArena | Light | 31 | PASS | 48.160 | 6.743 | 4/4 | 0/274 | 0.000 | 0 | 20→20 | -0.106/0.650/22.739 | True |
| CombatArena | Heavy | 31 | PASS | 48.138 | 10.791 | 12/12 | 0/479 | 0.000 | 0 | 139→139 | 0.080/0.650/22.630 | True |
| ObstacleCourse | Light | 31 | PASS | 54.389 | 7.924 | 2/4 | 0/336 | 0.000 | 3 | 20→20 | 1.333/0.650/58.855 | True |
| ObstacleCourse | Heavy | 31 | PASS | 54.278 | 12.393 | 12/12 | 0/559 | 0.000 | 3 | 139→139 | 1.485/0.650/58.697 | True |
| CombatArena | Light | 83 | PASS | 48.160 | 6.686 | 4/4 | 0/274 | 0.000 | 0 | 20→20 | 0.096/0.650/22.739 | True |
| CombatArena | Heavy | 83 | PASS | 48.138 | 10.792 | 12/12 | 0/479 | 0.000 | 0 | 139→139 | -0.068/0.650/22.631 | True |
| ObstacleCourse | Light | 83 | PASS | 54.544 | 7.946 | 0/4 | 1/337 | 0.020 | 3 | 20→20 | -3.962/0.650/58.915 | True |
| ObstacleCourse | Heavy | 83 | PASS | 54.401 | 12.421 | 12/12 | 0/560 | 0.000 | 3 | 139→139 | -3.832/0.650/58.715 | True |

Distance is accumulated3D body movement including initial settling; it is not a pure horizontal odometer. Contacts are original R6WheelContacts fixed-step queries. The corrected own QA episode counter accumulates0.02s per unsupported query; that estimate is not continuous airborne duration. Finite65s case cap and2.4m horizontal waypoint/finish tolerance are unchanged from the prior verified R6 harness. Optional side ramps were not traversed in this protected-route matrix.
