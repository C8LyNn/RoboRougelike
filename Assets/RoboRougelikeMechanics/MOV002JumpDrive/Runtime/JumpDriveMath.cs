using UnityEngine;

namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    /// <summary>Deterministic impulse calculations. No Unity scene or GameObject required.</summary>
    public static class JumpDriveMath
    {
        /// <summary>
        /// Computes an instantaneous upward velocity increment and its equivalent impulse (N*s).
        /// Below supportedMassKg the unit delivers a capped delta-v. Above it, available impulse is capped.
        /// The final upward velocity never exceeds maxUpwardSpeedMps.
        /// </summary>
        public static bool TryCalculate(
            float gravityMps2, float targetHeightMeters, float supportedMassKg,
            float vehicleMassKg, float currentUpwardSpeedMps, float maxUpwardSpeedMps,
            out float deltaVelocityMps, out float impulseNewtonSeconds)
        {
            deltaVelocityMps = 0f;
            impulseNewtonSeconds = 0f;
            if (!Valid(gravityMps2) || gravityMps2 <= 0f ||
                !Valid(targetHeightMeters) || targetHeightMeters <= 0f ||
                !Valid(supportedMassKg) || supportedMassKg <= 0f ||
                !Valid(vehicleMassKg) || vehicleMassKg <= 0f ||
                !Valid(currentUpwardSpeedMps) ||
                !Valid(maxUpwardSpeedMps) || maxUpwardSpeedMps <= 0f)
                return false;

            float nominalDeltaV = Mathf.Sqrt(2f * gravityMps2 * targetHeightMeters);
            float massFactor = Mathf.Min(1f, supportedMassKg / vehicleMassKg);
            float availableSpeed = maxUpwardSpeedMps - currentUpwardSpeedMps;
            if (!Valid(nominalDeltaV) || !Valid(massFactor) ||
                !Valid(availableSpeed) || availableSpeed <= 0.0001f)
                return false;

            deltaVelocityMps = Mathf.Min(nominalDeltaV * massFactor, availableSpeed);
            impulseNewtonSeconds = deltaVelocityMps * vehicleMassKg;
            if (!Valid(deltaVelocityMps) || !Valid(impulseNewtonSeconds) || deltaVelocityMps <= 0.0001f)
            {
                deltaVelocityMps = 0f;
                impulseNewtonSeconds = 0f;
                return false;
            }
            return true;
        }

        public static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
