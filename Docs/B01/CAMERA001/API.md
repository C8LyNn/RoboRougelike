# CAMERA001 isolated API
Assembly B01.CAMERA001.Runtime; namespace B01.CAMERA001; no dependencies on R1/R3/R6/P12.
Camera001Framing.Solve(Request) -> Result is deterministic / physics-free. Inputs: world-space HullBounds, YawDegrees, PitchDegrees, RequestedDistance, MinimumDistance, MaximumDistance, VerticalFovDegrees, Aspect, SafetyHeadroom, AimLead. Outputs: Rotation, AimWorld, DesiredCameraWorld, Distance, FitDistance, FitSaturated, OwnHullIntersectsCrosshair.
Camera001Rig.Configure(vehicleRoot, attachedCamera); SetOrbit(yaw,pitch,distance); OrbitBy(deltas); ResetSmoothing(); StepCamera(deltaTime); static GetHullBounds(root). Exposes LastSolution, WorldClipped, WorldOverlapAtAim. No gameplay input is consumed by the runtime.
Camera001Demo.cs is ONLY a standalone input/display harness. Never copy it into gameplay control. No object collider is disabled by this package.
