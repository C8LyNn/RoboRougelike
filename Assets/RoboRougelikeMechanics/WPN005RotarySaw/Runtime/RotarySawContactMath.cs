using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    public static class RotarySawContactMath
    {
        // Blade lies in a plane perpendicular to axleAxis; the hub itself cannot cut.
        public static bool IsOnCuttingDisc(Vector3 offsetFromCenter, Vector3 axleAxis,
            float radius, float halfThickness, float hubDeadZone)
        {
            if (radius <= 0f || halfThickness <= 0f || axleAxis.sqrMagnitude < 0.00001f) return false;
            Vector3 normal = axleAxis.normalized;
            float axial = Vector3.Dot(offsetFromCenter, normal);
            if (Mathf.Abs(axial) > halfThickness + 0.0001f) return false;
            Vector3 planar = offsetFromCenter - normal * axial;
            float planarSqr = planar.sqrMagnitude;
            return planarSqr <= radius * radius + 0.0001f &&
                planarSqr >= hubDeadZone * hubDeadZone - 0.0001f;
        }

        public static float Damage(float fullSpeedDps, float rpmRatio, float seconds, float resistance)
        {
            if (float.IsNaN(resistance) || float.IsInfinity(resistance) || resistance <= 0f ||
                float.IsNaN(fullSpeedDps) || float.IsInfinity(fullSpeedDps) ||
                float.IsNaN(rpmRatio) || float.IsInfinity(rpmRatio) ||
                float.IsNaN(seconds) || float.IsInfinity(seconds)) return 0f;
            return Mathf.Max(0f, fullSpeedDps) * Mathf.Clamp01(rpmRatio) *
                   Mathf.Max(0f, seconds) / Mathf.Max(0.25f, resistance);
        }
    }
}
