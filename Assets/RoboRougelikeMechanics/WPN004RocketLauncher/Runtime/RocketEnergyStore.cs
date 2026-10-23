using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    [DisallowMultipleComponent]
    public sealed class RocketEnergyStore : MonoBehaviour, IRocketEnergySource
    {
        [SerializeField, Min(0)] private float stored = 100f;
        public float Stored => stored;
        public void SetStored(float amount) => stored = RocketMath.NonNegative(amount);
        public bool TrySpendEnergy(float amount)
        {
            if (float.IsNaN(amount) || amount < 0f || amount > stored) return false;
            stored -= amount;
            return true;
        }
        public void RefundEnergy(float amount)
        {
            stored += RocketMath.NonNegative(amount);
        }
    }
}
