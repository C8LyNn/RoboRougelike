using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>Pure math: can be tested without scenes or changing global RNG.</summary>
    public static class PointDefenseMath
    {
        public static float ClampPositive(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? fallback : value;
        }

        public static bool IsThreatApproaching(Vector3 position, Vector3 velocity,
            Vector3 defendedPoint, Vector3 defendedVelocity, float lookAhead,
            float defendedRadius, out float secondsToClosest)
        {
            secondsToClosest = 0f;
            Vector3 offset = position - defendedPoint;
            Vector3 relativeVelocity = velocity - defendedVelocity;
            float square = relativeVelocity.sqrMagnitude;
            if (square < 0.0001f) return offset.sqrMagnitude <= defendedRadius * defendedRadius;
            float t = -Vector3.Dot(offset, relativeVelocity) / square;
            if (t <= 0f)
                return offset.sqrMagnitude <= defendedRadius * defendedRadius;
            if (t > lookAhead) return false;
            secondsToClosest = t;
            return (offset + relativeVelocity * t).sqrMagnitude <= defendedRadius * defendedRadius;
        }

        /// <summary>Analytical constant-velocity interception in shooter-relative frame.</summary>
        public static bool TryLead(Vector3 shooter, Vector3 shooterVelocity, Vector3 target,
            Vector3 targetVelocity, float projectileSpeed, float maxLeadTime,
            out Vector3 aimPoint, out float flightTime)
        {
            aimPoint = target;
            flightTime = 0f;
            if (projectileSpeed <= 0.001f || maxLeadTime <= 0f) return false;
            Vector3 r = target - shooter;
            Vector3 v = targetVelocity - shooterVelocity;
            float a = Vector3.Dot(v, v) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(r, v);
            float c = Vector3.Dot(r, r);
            if (c < 0.000001f) return true;
            float t;
            if (Mathf.Abs(a) < 0.00001f)
            {
                if (Mathf.Abs(b) < 0.00001f) return false;
                t = -c / b;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc < 0f) return false;
                float root = Mathf.Sqrt(disc);
                float t0 = (-b - root) / (2f * a);
                float t1 = (-b + root) / (2f * a);
                t = t0 > 0f && t1 > 0f ? Mathf.Min(t0, t1) : Mathf.Max(t0, t1);
            }
            if (float.IsNaN(t) || float.IsInfinity(t) || t <= 0f || t > maxLeadTime) return false;
            flightTime = t;
            aimPoint = target + targetVelocity * t;
            return true;
        }

        public static bool IsAligned(Vector3 facing, Vector3 desired, float toleranceDegrees)
        {
            return desired.sqrMagnitude > 0.000001f &&
                   Vector3.Angle(facing, desired) <= Mathf.Max(0f, toleranceDegrees);
        }
    }
}
