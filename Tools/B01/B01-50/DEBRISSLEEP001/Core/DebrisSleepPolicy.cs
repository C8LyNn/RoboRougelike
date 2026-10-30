using System;

namespace B01.Performance.DebrisSleep001
{
    /// <summary>
    /// Conservative one-attempt quiet-window policy for explicitly disposable non-gameplay
    /// physics debris. Pure C#; Step never changes Rigidbody or any Unity object.
    /// </summary>
    public sealed class DebrisSleepPolicy
    {
        public const int MinQuietSteps = 8;
        public const int MaxQuietSteps = 600;
        public const float MaxLinearThreshold = 10f;
        public const float MaxAngularThreshold = 20f;
        private readonly int requiredSteps;
        private readonly float maxLinearSquared;
        private readonly float maxAngularSquared;
        private int quietSteps;
        private bool attempted;

        public DebrisSleepPolicy(int requiredQuietSteps, float maxLinearSpeed, float maxAngularSpeed)
        {
            if (requiredQuietSteps < MinQuietSteps || requiredQuietSteps > MaxQuietSteps)
                throw new ArgumentOutOfRangeException(nameof(requiredQuietSteps));
            if (!Finite(maxLinearSpeed) || maxLinearSpeed <= 0f || maxLinearSpeed > MaxLinearThreshold)
                throw new ArgumentOutOfRangeException(nameof(maxLinearSpeed));
            if (!Finite(maxAngularSpeed) || maxAngularSpeed <= 0f || maxAngularSpeed > MaxAngularThreshold)
                throw new ArgumentOutOfRangeException(nameof(maxAngularSpeed));
            requiredSteps = requiredQuietSteps;
            maxLinearSquared = maxLinearSpeed * maxLinearSpeed;
            maxAngularSquared = maxAngularSpeed * maxAngularSpeed;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public int QuietSteps { get { return quietSteps; } }
        public bool Attempted { get { return attempted; } }
        public int RequiredSteps { get { return requiredSteps; } }

        public void Reset()
        {
            quietSteps = 0;
            attempted = false;
        }

        /// <summary>
        /// Returns one Sleep() request after N consecutive stable, awake fixed steps.
        /// Invalid input, speed above the threshold, or explicit impact resets the lease.
        /// If Sleep() does not stick, never hammer it every fixed tick: only re-arm after
        /// new motion/contact, successful sleeping, or an explicit opt-in reset.
        /// </summary>
        public bool Step(float linearSpeedSquared, float angularSpeedSquared, bool isSleeping)
        {
            if (!Finite(linearSpeedSquared) || !Finite(angularSpeedSquared) ||
                linearSpeedSquared < 0f || angularSpeedSquared < 0f ||
                linearSpeedSquared > maxLinearSquared || angularSpeedSquared > maxAngularSquared)
            {
                Reset();
                return false;
            }

            if (isSleeping)
            {
                // If native physics really slept, a later wake may acquire a NEW quiet window.
                Reset();
                return false;
            }

            if (attempted) return false;
            if (quietSteps < requiredSteps) ++quietSteps;
            if (quietSteps < requiredSteps) return false;
            attempted = true;
            return true;
        }
    }
}
