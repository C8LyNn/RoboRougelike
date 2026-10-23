using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>Deterministic, Unity-independent-of-physics numerical rules.</summary>
    public static class EmpPulseMath
    {
        public static bool IsValidFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static float CalculateDuration(float baseDuration, float distance, float radius,
            float resistance, float edgeFraction)
        {
            if (!IsValidFinite(baseDuration) || !IsValidFinite(distance) || !IsValidFinite(radius)
                || !IsValidFinite(resistance) || !IsValidFinite(edgeFraction)
                || baseDuration <= 0f || radius <= 0f || distance < 0f || distance > radius)
                return 0f;

            float distanceFactor = Mathf.Lerp(1f, Mathf.Clamp01(edgeFraction), distance / radius);
            return baseDuration * distanceFactor * (1f - Mathf.Clamp01(resistance));
        }

        public static bool IsWithinRadius(float distance, float radius) =>
            IsValidFinite(distance) && IsValidFinite(radius) && distance >= 0f
            && radius > 0f && distance <= radius;
    }
}
