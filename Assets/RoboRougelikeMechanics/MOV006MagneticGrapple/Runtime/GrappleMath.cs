using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    public static class GrappleMath
    {
        public static float ReelLength(float length, float minLength, float reelSpeed, float dt)
        {
            float floor = Mathf.Min(Mathf.Max(0.1f, minLength), Mathf.Max(0.1f, length));
            return Mathf.Max(floor, Mathf.Max(0f, length) - Mathf.Max(0f, reelSpeed) * Mathf.Max(0f, dt));
        }

        // Positive separationVelocity = anchor moving away from vehicle along rope direction.
        // A slack rope never pushes an object toward the other end.
        public static float Tension(
            float distance, float restLength, float separationVelocity,
            float stiffness, float damping, float maximumForce,
            float vehicleMass, float maximumVehicleAcceleration,
            float targetMass, float maximumTargetAcceleration,
            bool dynamicTarget)
        {
            float stretch = distance - restLength;
            if (stretch <= 0f || maximumForce <= 0f || vehicleMass <= 0f)
                return 0f;

            float requested = Mathf.Max(0f,
                stretch * Mathf.Max(0f, stiffness) +
                separationVelocity * Mathf.Max(0f, damping));
            float limited = Mathf.Min(requested, maximumForce,
                vehicleMass * Mathf.Max(0f, maximumVehicleAcceleration));
            if (dynamicTarget)
            {
                if (targetMass <= 0f) return 0f;
                limited = Mathf.Min(limited,
                    targetMass * Mathf.Max(0f, maximumTargetAcceleration));
            }
            return Mathf.Max(0f, limited);
        }

        public static bool HasDirection(Vector3 direction)
        {
            return direction.sqrMagnitude > 0.000001f &&
                !float.IsNaN(direction.x) && !float.IsNaN(direction.y) &&
                !float.IsNaN(direction.z) &&
                !float.IsInfinity(direction.x) && !float.IsInfinity(direction.y) &&
                !float.IsInfinity(direction.z);
        }
    }
}
