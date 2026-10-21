using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009
{
    /// <summary>Pure spring calculations; no global state and no Unity physics queries.</summary>
    public static class SuspensionMath
    {
        public static float Finite(float value, float fallback = 0f)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }

        /// <summary>
        /// A one-sided, compression-only support spring. Distances are measured from
        /// the suspension mount down to the ground in metres. Positive vertical point
        /// velocity means the mount is moving upwards. Result is force in newtons.
        /// </summary>
        public static float LiftForceNewtons(
            float targetDistance, float measuredDistance, float upwardPointVelocity,
            float stiffnessNPerM, float dampingNsPerM, float forceLimitN)
        {
            targetDistance = Mathf.Max(0f, Finite(targetDistance));
            measuredDistance = Finite(measuredDistance, float.PositiveInfinity);
            stiffnessNPerM = Mathf.Max(0f, Finite(stiffnessNPerM));
            dampingNsPerM = Mathf.Max(0f, Finite(dampingNsPerM));
            forceLimitN = Mathf.Max(0f, Finite(forceLimitN));
            upwardPointVelocity = Finite(upwardPointVelocity);
            if (measuredDistance < 0f || measuredDistance >= targetDistance || forceLimitN <= 0f)
                return 0f;

            float compression = targetDistance - measuredDistance;
            float force = stiffnessNPerM * compression - dampingNsPerM * upwardPointVelocity;
            if (float.IsNaN(force)) return 0f;
            return Mathf.Clamp(force, 0f, forceLimitN);
        }

        public static float DesiredHeight(float baseHeight, float offset, float minimum, float maximum)
        {
            minimum = Mathf.Max(0.01f, Finite(minimum, 0.1f));
            maximum = Mathf.Max(minimum, Finite(maximum, minimum));
            return Mathf.Clamp(Finite(baseHeight, minimum) + Finite(offset), minimum, maximum);
        }

        public static float SafeForceScale(float totalRequestedN, float maxAllowedN)
        {
            totalRequestedN = Mathf.Max(0f, Finite(totalRequestedN));
            maxAllowedN = Mathf.Max(0f, Finite(maxAllowedN));
            if (totalRequestedN <= 0f || maxAllowedN <= 0f) return 0f;
            return Mathf.Min(1f, maxAllowedN / totalRequestedN);
        }
    }
}
