using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    public sealed class BlinkEnergyPool : IBlinkEnergySource
    {
        public float Capacity { get; private set; }
        public float Current { get; private set; }
        public BlinkEnergyPool(float capacity, float initialEnergy)
        {
            Capacity = BlinkMath.IsFinite(capacity) ? Mathf.Max(0f, capacity) : 0f;
            Current = BlinkMath.IsFinite(initialEnergy) ? Mathf.Clamp(initialEnergy, 0f, Capacity) : 0f;
        }
        public bool CanSpend(float amount) => BlinkMath.IsFinite(amount) && amount >= 0f && Current >= amount;
        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;
            Current -= amount;
            return true;
        }
        public void Recharge(float amount)
        {
            if (BlinkMath.IsFinite(amount) && amount >= 0f)
                Current = Mathf.Clamp(Current + amount, 0f, Capacity);
        }
    }
}
