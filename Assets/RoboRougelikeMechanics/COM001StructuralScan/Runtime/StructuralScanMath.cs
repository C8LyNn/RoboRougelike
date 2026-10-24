using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    public static class StructuralScanMath
    {
        public static bool InCone(Vector3 origin, Vector3 forward, Vector3 point, float range, float fullConeDegrees)
        {
            Vector3 delta = point - origin;
            float distance2 = delta.sqrMagnitude;
            if (distance2 < 0.000001f || distance2 > range * range || forward.sqrMagnitude < 0.000001f)
                return false;
            float minimumCosine = Mathf.Cos(Mathf.Deg2Rad * Mathf.Clamp(fullConeDegrees, 1f, 180f) * 0.5f);
            return Vector3.Dot(forward.normalized, delta.normalized) >= minimumCosine - 0.000001f;
        }

        // Heuristic priority, NOT guaranteed graph-theoretical disconnection criticality.
        public static float Priority(StructuralPartType type, float normalizedHealth, float structuralImportance, bool functional)
        {
            float role;
            switch (type)
            {
                case StructuralPartType.Core: role = 90f; break;
                case StructuralPartType.StructuralSupport: role = 80f; break;
                case StructuralPartType.Weapon: role = 72f; break;
                case StructuralPartType.Wheel:
                case StructuralPartType.Track:
                case StructuralPartType.Propulsion: role = 64f; break;
                case StructuralPartType.Shield: role = 60f; break;
                case StructuralPartType.Utility: role = 45f; break;
                case StructuralPartType.Armor: role = 20f; break;
                default: role = 10f; break;
            }
            float health = float.IsNaN(normalizedHealth) || float.IsInfinity(normalizedHealth) ? 1f : Mathf.Clamp01(normalizedHealth);
            float importance = float.IsNaN(structuralImportance) || float.IsInfinity(structuralImportance) ? 0f : Mathf.Clamp01(structuralImportance);
            return Mathf.Clamp(role + importance * 35f + (1f - health) * 12f + (functional ? 0f : -20f), 0f, 150f);
        }
    }
}
