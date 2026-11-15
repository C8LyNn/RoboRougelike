using UnityEngine;

namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>Pure mathematical policy; no Rigidbody, input-system or game dependencies.</summary>
    public static class OmniDriveMath
    {
        public static Vector2 ClampInput(Vector2 input)
        {
            if (!Valid(input.x) || !Valid(input.y)) return Vector2.zero;
            return Vector2.ClampMagnitude(input, 1f);
        }

        public static float ClampTurn(float turn)
        {
            return Valid(turn) ? Mathf.Clamp(turn, -1f, 1f) : 0f;
        }

        /// <summary>
        /// World XZ acceleration required to approach a vehicle-relative target speed.
        /// Never requests vertical acceleration, never writes velocity and clamps change per step.
        /// When input is zero, coast unless explicit idle braking is requested.
        /// </summary>
        public static Vector3 LinearAcceleration(Vector3 worldVelocity, Quaternion orientation,
            Vector2 localInput, float maximumSpeed, float maximumAcceleration,
            float idleBrakeAcceleration, float fixedDt)
        {
            if (!Valid(worldVelocity) || !Valid(orientation) || !Valid(fixedDt) || fixedDt <= 0f)
                return Vector3.zero;
            maximumSpeed = NonNegative(maximumSpeed);
            maximumAcceleration = NonNegative(maximumAcceleration);
            idleBrakeAcceleration = NonNegative(idleBrakeAcceleration);
            Vector2 input = ClampInput(localInput);
            bool hasInput = input.sqrMagnitude > 0.000001f;
            if (!hasInput && idleBrakeAcceleration <= 0f) return Vector3.zero;

            Vector3 forward = Vector3.ProjectOnPlane(orientation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) return Vector3.zero; // Upside-down orientation is not a drive frame.
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(worldVelocity, Vector3.up);
            float maxAccel = hasInput ? maximumAcceleration : idleBrakeAcceleration;
            if (maxAccel <= 0f) return Vector3.zero;
            // Clamp in double precision before converting back to a Unity vector.
            // A finite target can otherwise overflow in delta/dt or sqrMagnitude.
            double dx=((double)right.x*input.x+(double)forward.x*input.y)*maximumSpeed-planarVelocity.x;
            double dz=((double)right.z*input.x+(double)forward.z*input.y)*maximumSpeed-planarVelocity.z;
            double length=System.Math.Sqrt(dx*dx+dz*dz);
            if(length<=0d)return Vector3.zero;
            double scale=System.Math.Min(1d/fixedDt,maxAccel/length);
            return new Vector3((float)(dx*scale),0,(float)(dz*scale));
        }

        /// <summary>Yaw acceleration only, around world up. No roll/pitch damping.</summary>
        public static float YawAcceleration(float currentWorldYawRadiansPerSecond,
            float turnInput, float maximumYawDegreesPerSecond, float maxAngularAcceleration,
            float idleYawBrakeAcceleration, float fixedDt)
        {
            if (!Valid(currentWorldYawRadiansPerSecond) || !Valid(fixedDt) || fixedDt <= 0f)
                return 0f;
            float turn = ClampTurn(turnInput);
            float accelLimit = Mathf.Abs(turn) > 0.0001f
                ? NonNegative(maxAngularAcceleration) : NonNegative(idleYawBrakeAcceleration);
            if (accelLimit <= 0f) return 0f;
            double speed = (double)Mathf.Deg2Rad * NonNegative(maximumYawDegreesPerSecond);
            double wanted = turn * speed;
            double requested=(wanted-currentWorldYawRadiansPerSecond)/fixedDt;
            return (float)System.Math.Max(-accelLimit,System.Math.Min(accelLimit,requested));
        }

        public static float NonNegative(float n) { return Valid(n) ? Mathf.Max(0f, n) : 0f; }
        public static bool Valid(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
        public static bool Valid(Vector3 v) { return Valid(v.x) && Valid(v.y) && Valid(v.z); }
        public static bool Valid(Quaternion q) { return Valid(q.x) && Valid(q.y) && Valid(q.z) && Valid(q.w); }
    }
}
