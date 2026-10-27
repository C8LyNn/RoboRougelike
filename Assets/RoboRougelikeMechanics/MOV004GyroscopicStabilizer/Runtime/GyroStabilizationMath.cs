using UnityEngine;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer
{
    /// <summary>
    /// Pure angular-acceleration math. World-space vectors; rates in rad/s and rad/s^2.
    /// Corrects tilt relative to targetUp WITHOUT deliberately damping yaw around targetUp.
    /// Doesn't move or rotate a Transform and doesn't own vehicle physics.
    /// </summary>
    public static class GyroStabilizationMath
    {
        private const float Epsilon = 0.000001f;

        public static Vector3 Calculate(
            Quaternion bodyWorldRotation,
            Vector3 worldAngularVelocity,
            Vector3 targetUp,
            float stiffness,
            float damping,
            float maxAngularAcceleration)
        {
            if (stiffness < 0f || damping < 0f || maxAngularAcceleration <= 0f)
                return Vector3.zero;

            if (!IsFinite(bodyWorldRotation) || !IsFinite(worldAngularVelocity) || !IsFinite(targetUp)
                || !IsFinite(stiffness) || !IsFinite(damping) || !IsFinite(maxAngularAcceleration))
                return Vector3.zero;

            // Unity quaternion/transform is expected normalized; normalize defensively for caller tests.
            float qSqr = bodyWorldRotation.x * bodyWorldRotation.x +
                         bodyWorldRotation.y * bodyWorldRotation.y +
                         bodyWorldRotation.z * bodyWorldRotation.z +
                         bodyWorldRotation.w * bodyWorldRotation.w;
            if (qSqr < Epsilon) return Vector3.zero;
            float invQ = 1f / Mathf.Sqrt(qSqr);
            bodyWorldRotation = new Quaternion(bodyWorldRotation.x * invQ,
                bodyWorldRotation.y * invQ, bodyWorldRotation.z * invQ, bodyWorldRotation.w * invQ);

            Vector3 up = targetUp.sqrMagnitude > Epsilon ? targetUp.normalized : Vector3.up;
            Vector3 bodyUp = bodyWorldRotation * Vector3.up;
            Vector3 cross = Vector3.Cross(bodyUp, up);
            float sinAngle = cross.magnitude;
            float cosAngle = Mathf.Clamp(Vector3.Dot(bodyUp, up), -1f, 1f);

            Vector3 axis = Vector3.zero;
            float angleRad = 0f;
            if (sinAngle > Epsilon)
            {
                axis = cross / sinAngle;
                angleRad = Mathf.Atan2(sinAngle, cosAngle);
            }
            else if (cosAngle < 0f)
            {
                // Exactly upside down: cross product vanishes. Pick a deterministic tilt axis.
                axis = Vector3.Cross(bodyWorldRotation * Vector3.forward, up);
                if (axis.sqrMagnitude <= Epsilon)
                    axis = Vector3.Cross(bodyWorldRotation * Vector3.right, up);
                if (axis.sqrMagnitude <= Epsilon) return Vector3.zero;
                axis.Normalize();
                angleRad = Mathf.PI;
            }

            // Only damp roll/pitch. Removing the component along targetUp prevents yaw braking.
            Vector3 tiltAngularVelocity = Vector3.ProjectOnPlane(worldAngularVelocity, up);
            Vector3 acceleration = axis * (stiffness * angleRad) - damping * tiltAngularVelocity;
            acceleration = Vector3.ProjectOnPlane(acceleration, up);

            if (!IsFinite(acceleration)) return Vector3.zero;
            return Vector3.ClampMagnitude(acceleration, maxAngularAcceleration);
        }

        private static bool IsFinite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
        private static bool IsFinite(Vector3 v) { return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z); }
        private static bool IsFinite(Quaternion q) { return IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w); }
    }
}
