using UnityEngine;

namespace RoboRougelike.Mechanics.MOV007
{
    /// <summary>Standalone demo-only energy source. Replace with game's energy system.</summary>
    public sealed class HoverPrototypeEnergyTank : MonoBehaviour, IHoverEnergySource
    {
        [Min(0f)] public float capacity = 150f;
        [Min(0f)] public float initialEnergy = 150f;
        [Min(0f)] public float regenPerSecond = 4f;
        public float CurrentEnergy { get; private set; }

        private void Awake() { CurrentEnergy = Mathf.Min(capacity, Mathf.Max(0f, initialEnergy)); }
        private void FixedUpdate()
        {
            CurrentEnergy = Mathf.Min(capacity, CurrentEnergy + Mathf.Max(0f, regenPerSecond) * Time.fixedDeltaTime);
        }
        public bool CanSpend(float amount) => amount >= 0f && CurrentEnergy + 0.00001f >= amount;
        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;
            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
            return true;
        }
        public void SetEnergyForTest(float amount) { CurrentEnergy = Mathf.Clamp(amount, 0f, capacity); }
    }
}
