# Independently executed original cube/flat tests

| Native NUnit case | Actual result |
|---|---|
| Arena_Seed31_Heavy_CubePhysics | Passed |
| Arena_Seed31_Light_CubePhysics | Passed |
| Arena_Seed7_Heavy_CubePhysics | Passed |
| Arena_Seed7_Light_CubePhysics | Passed |
| Arena_Seed83_Heavy_CubePhysics | Passed |
| Arena_Seed83_Light_CubePhysics | Passed |
| Course_Seed31_Heavy_CubePhysics | Passed |
| Course_Seed31_Light_CubePhysics | Passed |
| Course_Seed7_Heavy_CubePhysics | Passed |
| Course_Seed7_Light_CubePhysics | Passed |
| Course_Seed83_Heavy_CubePhysics | Passed |
| Course_Seed83_Light_CubePhysics | Passed |
| CubeForceActuatorMovesOnFlatControlSurface | Passed |

Original assertions and limits unchanged. Detailed positions/velocities/support-ray counts are emitted by producer code on failure; successful cases do not emit those numeric values. Native PASS proves their actual unchanged assertions, not fabricated per-case telemetry or real-wheel drive.
