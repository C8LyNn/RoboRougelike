using UnityEngine;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer
{
    /// <summary>Standalone data model for optional prototype energy; not a game-wide energy system.</summary>
    public sealed class GyroEnergyBudget
    {
        public float Capacity { get; private set; }
        public float Current { get; private set; }
        public float RechargePerSecond { get; private set; }

        public GyroEnergyBudget(float capacity, float rechargePerSecond, bool startFull = true)
        {
            Configure(capacity, rechargePerSecond);
            Current = startFull ? Capacity : 0f;
        }

        public void Configure(float capacity, float rechargePerSecond)
        {
            Capacity = SafeNonNegative(capacity);
            RechargePerSecond = SafeNonNegative(rechargePerSecond);
            Current = Mathf.Clamp(Current, 0f, Capacity);
        }

        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(SafeNonNegative(value), 0f, Capacity);
        }

        public bool TrySpend(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            if (amount > Current) return false;
            Current = Mathf.Max(0f, Current - amount);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
            Current = Mathf.Min(Capacity, Current + RechargePerSecond * deltaTime);
        }

        private static float SafeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }
}
