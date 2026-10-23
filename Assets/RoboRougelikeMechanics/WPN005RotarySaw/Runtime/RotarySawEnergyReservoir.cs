using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    // Optional standalone/test-only energy source. Use game's actual energy system in production.
    public sealed class RotarySawEnergyReservoir : MonoBehaviour, IRotarySawEnergySource
    {
        [Min(0f)] [SerializeField] private float capacity = 100f;
        [Min(0f)] [SerializeField] private float initialCharge = 100f;
        [Min(0f)] [SerializeField] private float rechargePerSecond = 0f;
        public float Charge { get; private set; }

        private void Awake() { Charge = Mathf.Min(capacity, initialCharge); }
        private void Update() { Charge = Mathf.Min(capacity, Charge + rechargePerSecond * Time.deltaTime); }

        public bool TryConsume(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            if (amount <= 0f) return true;
            if (Charge + 0.00001f < amount) return false;
            Charge = Mathf.Max(0f, Charge - amount);
            return true;
        }

        public void SetChargeForTesting(float value) { Charge = Mathf.Clamp(value, 0f, capacity); }
    }
}
