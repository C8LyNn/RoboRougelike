using UnityEngine;

namespace RoboRougelike.Defenses.DirectionalShield
{
    /// <summary>Bezstanowa geometria osłony. travelDirection oznacza RUCH POCISKU, nie kierunek do strzelca.</summary>
    public static class ShieldMath
    {
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);

        public static bool Covers(
            Vector3 mountPosition, Vector3 mountForward, Vector3 hitPoint,
            Vector3 travelDirection, float radius, float halfAngleDegrees,
            bool horizontalArc = true, bool requireHitPointInFront = true)
        {
            if (!Finite(mountPosition) || !Finite(mountForward) || !Finite(hitPoint) ||
                !Finite(travelDirection) || !Finite(radius) || !Finite(halfAngleDegrees) ||
                radius <= 0f || halfAngleDegrees < 0f || halfAngleDegrees > 180f)
                return false;

            Vector3 displacement = hitPoint - mountPosition;
            if (displacement.sqrMagnitude > radius * radius) return false;

            Vector3 forward = horizontalArc ? Vector3.ProjectOnPlane(mountForward, Vector3.up) : mountForward;
            Vector3 incomingFrom = horizontalArc ? Vector3.ProjectOnPlane(-travelDirection, Vector3.up) : -travelDirection;
            if (forward.sqrMagnitude < 0.000001f || incomingFrom.sqrMagnitude < 0.000001f)
                return false;

            forward.Normalize();
            incomingFrom.Normalize();
            float dot = Vector3.Dot(forward, incomingFrom);
            if (dot < Mathf.Cos(halfAngleDegrees * Mathf.Deg2Rad) - 0.00001f)
                return false;

            if (requireHitPointInFront && displacement.sqrMagnitude > 0.000001f)
            {
                Vector3 towardHit = horizontalArc ? Vector3.ProjectOnPlane(displacement, Vector3.up) : displacement;
                if (towardHit.sqrMagnitude > 0.000001f && Vector3.Dot(forward, towardHit) < -0.00001f)
                    return false;
            }
            return true;
        }
    }
}
