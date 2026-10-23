using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>Optional isolated demo energy store. Prefer R6 energy adapter in production.</summary>
    public sealed class EmpEnergyReservoir : MonoBehaviour, IEmpEnergyBank
    {
        [SerializeField, Min(0f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float current = 100f;
        [SerializeField, Min(0f)] private float rechargePerSecond = 4f;
        public float Current => current;
        public float Capacity => capacity;

        public void Configure(float maxEnergy, float initialEnergy, float regeneration)
        {
            capacity = EmpPulseMath.IsValidFinite(maxEnergy) ? Mathf.Max(0f, maxEnergy) : 0f;
            current = EmpPulseMath.IsValidFinite(initialEnergy) ? Mathf.Clamp(initialEnergy, 0f, capacity) : 0f;
            rechargePerSecond = EmpPulseMath.IsValidFinite(regeneration) ? Mathf.Max(0f, regeneration) : 0f;
        }

        public bool TrySpend(float amount)
        {
            if (!EmpPulseMath.IsValidFinite(amount) || amount < 0f || current < amount) return false;
            current -= amount;
            return true;
        }

        public void Refund(float amount)
        {
            if (EmpPulseMath.IsValidFinite(amount) && amount > 0f)
                current = Mathf.Min(capacity, current + amount);
        }

        private void Update()
        {
            current = Mathf.Min(capacity, current + rechargePerSecond * Time.deltaTime);
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(0f, capacity);
            current = Mathf.Clamp(current, 0f, capacity);
            rechargePerSecond = Mathf.Max(0f, rechargePerSecond);
        }
    }
}
