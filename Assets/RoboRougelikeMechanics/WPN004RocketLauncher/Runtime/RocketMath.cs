using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    public static class RocketMath
    {
        public static float Positive(float value, float fallback) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f ? value : fallback;

        public static float NonNegative(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) ? Mathf.Max(0f, value) : 0f;

        public static float SpeedAfter(float current, float acceleration, float maximum, float dt) =>
            Mathf.Min(Positive(maximum, 1f), NonNegative(current) + NonNegative(acceleration) * NonNegative(dt));

        public static float BlastDamage(float distance, float radius, float centerDamage, float edgeFraction)
        {
            if (radius <= 0f || centerDamage <= 0f || distance > radius || distance < 0f)
                return 0f;
            float ratio = Mathf.Clamp01(distance / radius);
            return centerDamage * Mathf.Lerp(1f, Mathf.Clamp01(edgeFraction), ratio);
        }

        public static Vector3 Steer(Vector3 forward, Vector3 desired, float turnDegreesPerSecond, float dt)
        {
            if (forward.sqrMagnitude < 0.000001f) return Vector3.forward;
            if (desired.sqrMagnitude < 0.000001f) return forward.normalized;
            if (turnDegreesPerSecond <= 0f || dt <= 0f) return forward.normalized;
            return Vector3.RotateTowards(forward.normalized, desired.normalized,
                turnDegreesPerSecond * Mathf.Deg2Rad * dt, 0f).normalized;
        }
    }
}
