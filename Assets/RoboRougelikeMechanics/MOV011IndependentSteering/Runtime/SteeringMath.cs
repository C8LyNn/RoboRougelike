using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering
{
    /// <summary>Pure, deterministic kinematics; no GameObjects, Random or frame-time dependencies.</summary>
    public static class SteeringMath
    {
        public static float SafeClamp(float v, float min, float max, float fallback)
        {
            return float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max);
        }

        public static float SpeedFactor(float planarSpeedMetersPerSecond, SteeringProfile rawProfile)
        {
            var p = rawProfile.Sanitized();
            float speed = float.IsNaN(planarSpeedMetersPerSecond) || float.IsInfinity(planarSpeedMetersPerSecond)
                ? 0f : Mathf.Abs(planarSpeedMetersPerSecond);
            return Mathf.Clamp(1f / (1f + speed / p.halfAngleAtSpeedMetersPerSecond), p.minimumSpeedFactor, 1f);
        }

        public static void CenterTargets(SteeringMode mode, float input, float speed, SteeringProfile rawProfile,
                                         float frontHealthFraction, float rearHealthFraction,
                                         out float frontDegrees, out float rearDegrees)
        {
            var p = rawProfile.Sanitized();
            float normalized = SafeClamp(input, -1f, 1f, 0f);
            float angle = normalized * p.maxCenterAngleDegrees * SpeedFactor(speed, p);
            float front = Mathf.Clamp01(frontHealthFraction);
            float rear = Mathf.Clamp01(rearHealthFraction);
            switch (mode)
            {
                case SteeringMode.RearOnly:
                    frontDegrees = 0f; rearDegrees = -angle * rear; return;
                case SteeringMode.CounterPhase:
                    frontDegrees = angle * front; rearDegrees = -angle * p.rearSteerRatio * rear; return;
                case SteeringMode.Crab:
                    // In crab, front/rear wheels must aim the same way. A missing axle cannot become steering power.
                    frontDegrees = angle * front; rearDegrees = angle * rear; return;
                default:
                    frontDegrees = angle * front; rearDegrees = 0f; return;
            }
        }

        public static SteeringSolution Solve(float frontCenterDegrees, float rearCenterDegrees, SteeringGeometry rawGeometry)
        {
            var g = rawGeometry.Sanitized();
            // Ignore broken external inputs. Per-wheel physical swivel is capped at 80 degrees to avoid lock-up.
            float f = SafeClamp(frontCenterDegrees, -50f, 50f, 0f);
            float r = SafeClamp(rearCenterDegrees, -50f, 50f, 0f);
            float tf = Mathf.Tan(f * Mathf.Deg2Rad), tr = Mathf.Tan(r * Mathf.Deg2Rad);
            float curvature = (tf - tr) / g.wheelbaseMeters;
            float midpointLateral = (tf + tr) * 0.5f;
            float halfTrack = g.trackMeters * 0.5f, halfWheelbase = g.wheelbaseMeters * 0.5f;
            float fl = WheelAngle(-halfTrack, halfWheelbase, curvature, midpointLateral);
            float fr = WheelAngle(halfTrack, halfWheelbase, curvature, midpointLateral);
            float rl = WheelAngle(-halfTrack, -halfWheelbase, curvature, midpointLateral);
            float rr = WheelAngle(halfTrack, -halfWheelbase, curvature, midpointLateral);
            return new SteeringSolution(fl, fr, rl, rr, f, r, curvature, Mathf.Atan(midpointLateral) * Mathf.Rad2Deg);
        }

        private static float WheelAngle(float lateralX, float longitudinalZ, float curvature, float midpointLateral)
        {
            // Angular velocity around local Y: v_x = v_mid_x + omega*z; v_z = 1 - omega*x.
            float lateral = midpointLateral + curvature * longitudinalZ;
            float forward = 1f - curvature * lateralX;
            return Mathf.Clamp(Mathf.Atan2(lateral, forward) * Mathf.Rad2Deg, -80f, 80f);
        }

        public static float Approach(float currentDegrees, float targetDegrees, float rateDegreesPerSecond, float dt)
        {
            if (float.IsNaN(dt) || dt <= 0f || float.IsInfinity(dt)) return currentDegrees;
            float safeRate = SafeClamp(rateDegreesPerSecond, 0f, 2000f, 0f);
            return Mathf.MoveTowards(currentDegrees, targetDegrees, safeRate * dt);
        }
    }
}
