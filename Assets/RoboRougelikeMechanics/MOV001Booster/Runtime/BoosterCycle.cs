using System;

namespace RoboRougelike.Mechanics.Booster
{
    /// <summary>
    /// Deterministic boost/cooldown state machine with no MonoBehaviour dependencies.
    /// Advances in seconds; returns the fraction of each timestep spent boosting.
    /// </summary>
    public sealed class BoosterCycle
    {
        public enum CyclePhase { Ready, Boosting, CoolingDown }

        private readonly float duration;
        private readonly float cooldown;

        public CyclePhase Phase { get; private set; }
        public float BoostSecondsRemaining { get; private set; }
        public float CooldownSecondsRemaining { get; private set; }
        public bool IsBoosting => Phase == CyclePhase.Boosting;
        public bool IsReady => Phase == CyclePhase.Ready;

        public BoosterCycle(float durationSeconds, float cooldownSeconds)
        {
            if (!IsFinite(durationSeconds) || durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (!IsFinite(cooldownSeconds) || cooldownSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));

            duration = durationSeconds;
            cooldown = cooldownSeconds;
            Reset();
        }

        public bool TryActivate()
        {
            if (!IsReady) return false;
            Phase = CyclePhase.Boosting;
            BoostSecondsRemaining = duration;
            CooldownSecondsRemaining = 0f;
            return true;
        }

        /// <summary>Returns boost-active seconds in this step, in [0, deltaSeconds].</summary>
        public float Advance(float deltaSeconds)
        {
            if (!IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            if (deltaSeconds == 0f) return 0f;

            if (Phase == CyclePhase.Boosting)
            {
                float activeSeconds = Math.Min(deltaSeconds, BoostSecondsRemaining);
                BoostSecondsRemaining = Math.Max(0f, BoostSecondsRemaining - activeSeconds);

                if (BoostSecondsRemaining <= 0f)
                {
                    BeginCooldown();
                    // In a frame crossing the end of the boost, the remaining time
                    // counts towards cooldown, so results do not depend on tick size.
                    AdvanceCooldown(deltaSeconds - activeSeconds);
                }
                return activeSeconds;
            }

            if (Phase == CyclePhase.CoolingDown)
                AdvanceCooldown(deltaSeconds);

            return 0f;
        }

        public void Cancel()
        {
            if (Phase == CyclePhase.Boosting)
                BeginCooldown();
        }

        public void Reset()
        {
            Phase = CyclePhase.Ready;
            BoostSecondsRemaining = 0f;
            CooldownSecondsRemaining = 0f;
        }

        private void BeginCooldown()
        {
            BoostSecondsRemaining = 0f;
            CooldownSecondsRemaining = cooldown;
            Phase = cooldown > 0f ? CyclePhase.CoolingDown : CyclePhase.Ready;
        }

        private void AdvanceCooldown(float deltaSeconds)
        {
            if (Phase != CyclePhase.CoolingDown || deltaSeconds <= 0f) return;
            CooldownSecondsRemaining = Math.Max(0f, CooldownSecondsRemaining - deltaSeconds);
            if (CooldownSecondsRemaining <= 0f)
            {
                CooldownSecondsRemaining = 0f;
                Phase = CyclePhase.Ready;
            }
        }

        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
