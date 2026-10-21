using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    public static class BlinkMath
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);

        public static bool TryPlan(Vector3 origin, Vector3 intendedDirection, float distance, out Vector3 destination)
        {
            destination = origin;
            if (!IsFinite(origin) || !IsFinite(intendedDirection) || !IsFinite(distance) || distance <= 0f)
                return false;

            Vector3 groundDirection = Vector3.ProjectOnPlane(intendedDirection, Vector3.up);
            if (groundDirection.sqrMagnitude <= 0.000001f)
                return false;

            destination = origin + groundDirection.normalized * distance;
            if (!IsFinite(destination))
            {
                destination = origin;
                return false;
            }
            return true;
        }
    }
}
