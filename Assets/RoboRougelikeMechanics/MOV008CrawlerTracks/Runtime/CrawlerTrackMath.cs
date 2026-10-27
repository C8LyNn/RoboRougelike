using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008
{
    public enum CrawlerTrackSide { Left, Right }

    /// <summary>Pure calculations. Forces are in newtons; velocities in metres/second.</summary>
    public static class CrawlerTrackMath
    {
        public static float Finite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        public static float ClampInput(float value) => Mathf.Clamp(Finite(value), -1f, 1f);

        // Positive steer is a right-hand turn (+world Y torque for a +Z-facing chassis).
        public static float SideDemand(float forward, float steer, CrawlerTrackSide side)
        {
            forward = ClampInput(forward);
            steer = ClampInput(steer);
            return Mathf.Clamp(forward + (side == CrawlerTrackSide.Left ? steer : -steer), -1f, 1f);
        }

        public static float TargetSpeed(float demand, float maxForward, float maxReverse)
        {
            demand = ClampInput(demand);
            return demand >= 0f ? demand * Mathf.Max(0f, Finite(maxForward)) :
                                  demand * Mathf.Max(0f, Finite(maxReverse));
        }

        public static float SupportForceLimit(float massShareKg, float nominalLoadKg,
            float gravityMagnitude, float gripCoefficient)
        {
            float supportedMass = Mathf.Min(Mathf.Max(0f, Finite(massShareKg)),
                                            Mathf.Max(0f, Finite(nominalLoadKg)));
            return supportedMass * Mathf.Max(0f, Finite(gravityMagnitude)) *
                   Mathf.Max(0f, Finite(gripCoefficient));
        }

        public static float LongitudinalForce(float currentSpeed, float demand, float massShareKg,
            float maxForwardSpeed, float maxReverseSpeed, float responseSeconds,
            float coastSeconds, float brakeSeconds, float motorForceLimit,
            float brakingForceLimit, float tractionLimit, bool brake)
        {
            demand = ClampInput(demand);
            currentSpeed = Finite(currentSpeed);
            massShareKg = Mathf.Max(0f, Finite(massShareKg));
            float target = brake ? 0f : TargetSpeed(demand, maxForwardSpeed, maxReverseSpeed);
            // Holding throttle must not actively cancel speed from an independent Booster.
            if (!brake && Mathf.Abs(demand) > 0.001f && Mathf.Abs(target) > 0.001f &&
                Mathf.Sign(currentSpeed) == Mathf.Sign(target) &&
                Mathf.Abs(currentSpeed) >= Mathf.Abs(target)) return 0f;
            float seconds = brake ? brakeSeconds :
                (Mathf.Abs(demand) > 0.001f ? responseSeconds : coastSeconds);
            float desiredForce = (target - currentSpeed) * massShareKg /
                                 Mathf.Max(0.02f, Finite(seconds));
            float mechanicalLimit = brake || Mathf.Abs(demand) <= 0.001f ? brakingForceLimit : motorForceLimit;
            float limit = Mathf.Min(Mathf.Max(0f, Finite(mechanicalLimit)),
                                    Mathf.Max(0f, Finite(tractionLimit)));
            return Mathf.Clamp(desiredForce, -limit, limit);
        }

        public static float LateralForce(float lateralSpeed, float massShareKg,
            float responseSeconds, float tractionLimit)
        {
            float force = -Finite(lateralSpeed) * Mathf.Max(0f, Finite(massShareKg)) /
                          Mathf.Max(0.02f, Finite(responseSeconds));
            return Mathf.Clamp(force, -Mathf.Max(0f, Finite(tractionLimit)),
                                      Mathf.Max(0f, Finite(tractionLimit)));
        }

        public static Vector3 ForwardOnGround(Vector3 chassisForward, Vector3 groundNormal)
        {
            if (groundNormal.sqrMagnitude < 0.1f) return Vector3.zero;
            Vector3 projected = Vector3.ProjectOnPlane(chassisForward, groundNormal.normalized);
            return projected.sqrMagnitude < 0.0001f ? Vector3.zero : projected.normalized;
        }
    }
}
