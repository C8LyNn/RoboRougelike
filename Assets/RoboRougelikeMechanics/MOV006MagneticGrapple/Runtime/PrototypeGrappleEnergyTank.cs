using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    // Demonstration only. Codex must connect IGrappleEnergySource to the actual game economy.
    public sealed class PrototypeGrappleEnergyTank : MonoBehaviour, IGrappleEnergySource
    {
        [Min(0f)] public float capacity = 100f;
        [Min(0f)] public float regenPerSecond = 8f;
        [SerializeField] private float currentEnergy = 100f;
        public float CurrentEnergy => currentEnergy;

        private void Awake() => Clamp();
        private void OnValidate() => Clamp();
        private void FixedUpdate()
        {
            currentEnergy = Mathf.Min(capacity, currentEnergy +
                Mathf.Max(0f, regenPerSecond) * Time.fixedDeltaTime);
        }
        public void SetEnergy(float amount)
        {
            currentEnergy = amount;
            Clamp();
        }
        public bool TrySpend(float amount)
        {
            if (amount < 0f) return false;
            if (currentEnergy + 0.00001f < amount) return false;
            currentEnergy = Mathf.Max(0f, currentEnergy - amount);
            return true;
        }
        private void Clamp()
        {
            capacity = Mathf.Max(0f, capacity);
            regenPerSecond = Mathf.Max(0f, regenPerSecond);
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, capacity);
        }
    }
}
