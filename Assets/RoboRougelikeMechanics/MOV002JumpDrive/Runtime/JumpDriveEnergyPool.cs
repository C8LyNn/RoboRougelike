using UnityEngine;

namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    /// <summary>Independent energy model; UI/Unity timing is handled elsewhere.</summary>
    public sealed class JumpDriveEnergyPool : IJumpDriveEnergySource
    {
        public float Capacity { get; private set; }
        public float Current { get; private set; }
        public JumpDriveEnergyPool(float capacity, float startingEnergy)
        {
            if (!JumpDriveMath.Valid(capacity) || capacity < 0f)
                capacity = 0f;
            Capacity = capacity;
            Current = JumpDriveMath.Valid(startingEnergy) ? Mathf.Clamp(startingEnergy, 0f, Capacity) : 0f;
        }
        public bool TrySpend(float amount)
        {
            if (!JumpDriveMath.Valid(amount) || amount < 0f || Current < amount)
                return false;
            Current -= amount;
            return true;
        }
        public void Refund(float amount)
        {
            if (!JumpDriveMath.Valid(amount) || amount < 0f) return;
            Current = Mathf.Min(Capacity, Current + amount);
        }
        public void Regenerate(float amount) => Refund(amount);
        public void ResetToFull() => Current = Capacity;
    }
}
