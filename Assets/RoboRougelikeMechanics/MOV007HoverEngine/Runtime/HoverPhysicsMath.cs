using UnityEngine;

namespace RoboRougelike.Mechanics.MOV007
{
    /// <summary>Pure functions, shared by runtime and deterministic EditMode tests.</summary>
    public static class HoverPhysicsMath
    {
        public static float LiftForceNewtons(
            float bodyMassKg, int availablePodCount, float gravityMagnitude,
            float measuredHeight, float targetHeight, float verticalPointSpeed,
            float springFrequencyHz, float dampingRatio,
            float nominalPodLoadKg, float maxLiftMultiplier)
        {
            if (bodyMassKg <= 0f || availablePodCount <= 0 || gravityMagnitude <= 0f ||
                measuredHeight < 0f || targetHeight <= 0f || nominalPodLoadKg <= 0f ||
                maxLiftMultiplier <= 0f) return 0f;

            float sprungMass = bodyMassKg / availablePodCount;
            float omega = 2f * Mathf.PI * Mathf.Max(0.05f, springFrequencyHz);
            float k = sprungMass * omega * omega;
            float damping = 2f * Mathf.Max(0f, dampingRatio) * sprungMass * omega;
            float heightError = targetHeight - measuredHeight;
            float force = sprungMass * gravityMagnitude + k * heightError - damping * verticalPointSpeed;
            // Nominal load is an actual load-bearing limit, not a free mass-independent lift.
            float cap = nominalPodLoadKg * gravityMagnitude * maxLiftMultiplier;
            return Mathf.Clamp(force, 0f, cap);
        }

        public static Vector3 DesiredPlanarVelocity(Transform frame, Vector2 moveInput, float forwardSpeed, float strafeSpeed)
        {
            Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
            if (frame == null) return Vector3.zero;
            Vector3 fwd = Vector3.ProjectOnPlane(frame.forward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(frame.right, Vector3.up);
            if (fwd.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f) return Vector3.zero;
            fwd.Normalize();right.Normalize();
            double forward=(double)input.y*Mathf.Max(0f,forwardSpeed),strafe=(double)input.x*Mathf.Max(0f,strafeSpeed);
            double x=fwd.x*forward+right.x*strafe,z=fwd.z*forward+right.z*strafe;
            double largest=System.Math.Max(System.Math.Abs(x),System.Math.Abs(z));
            double scale=largest>float.MaxValue?float.MaxValue/largest:1d;
            return new Vector3((float)(x*scale),0,(float)(z*scale));
        }

        public static Vector3 PlanarMotorForce(Vector3 currentVelocity, Vector3 desiredVelocity,
            float massKg, float responseSeconds, float forceCapNewtons, bool brakeExternalBoost)
        {
            if (massKg <= 0f || forceCapNewtons <= 0f) return Vector3.zero;
            Vector3 current = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
            Vector3 desired = Vector3.ProjectOnPlane(desiredVelocity, Vector3.up);
            if(float.IsNaN(current.x)||float.IsNaN(current.z)||float.IsInfinity(current.x)||float.IsInfinity(current.z)||
               float.IsNaN(desired.x)||float.IsNaN(desired.z)||float.IsInfinity(desired.x)||float.IsInfinity(desired.z)||
               float.IsNaN(massKg)||float.IsInfinity(massKg)||float.IsNaN(forceCapNewtons)||float.IsInfinity(forceCapNewtons))return Vector3.zero;
            double currentSq=(double)current.x*current.x+(double)current.z*current.z;
            double desiredSq=(double)desired.x*desired.x+(double)desired.z*desired.z;
            // Let other modules (e.g. MOV-001) exceed this motor's own target speed.
            if (!brakeExternalBoost && desiredSq > 0.001d && currentSq > desiredSq)
            {
                double dot=((double)current.x*desired.x+(double)current.z*desired.z)/System.Math.Sqrt(currentSq*desiredSq);
                if (dot > 0.98d)
                    return Vector3.zero;
            }
            double dx=(double)desired.x-current.x,dz=(double)desired.z-current.z;
            double distance=System.Math.Sqrt(dx*dx+dz*dz);if(distance<=0d)return Vector3.zero;
            double scale=System.Math.Min((double)massKg/Mathf.Max(0.05f,responseSeconds),forceCapNewtons/distance);
            return new Vector3((float)(dx*scale),0,(float)(dz*scale));
        }

        public static float YawMotorTorque(float currentYawRadPerSec, float targetYawRadPerSec,
            float inertiaAroundWorldUp, float responseSeconds, float maxTorqueNewtonMeters)
        {
            if (inertiaAroundWorldUp <= 0f || maxTorqueNewtonMeters <= 0f) return 0f;
            double desiredTorque = ((double)targetYawRadPerSec-currentYawRadPerSec)*
                                   inertiaAroundWorldUp/Mathf.Max(0.05f,responseSeconds);
            return (float)System.Math.Max(-maxTorqueNewtonMeters,System.Math.Min(maxTorqueNewtonMeters,desiredTorque));
        }

        public static float ApproximateWorldUpInertia(Rigidbody body)
        {
            if (body == null) return 0f;
            Quaternion principalToWorld = body.rotation * body.inertiaTensorRotation;
            Vector3 upInPrincipal = Quaternion.Inverse(principalToWorld) * Vector3.up;
            Vector3 moment = body.inertiaTensor;
            return Mathf.Max(0f, upInPrincipal.x * upInPrincipal.x * moment.x +
                                 upInPrincipal.y * upInPrincipal.y * moment.y +
                                 upInPrincipal.z * upInPrincipal.z * moment.z);
        }
    }
}
