using UnityEngine;

namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>Self-contained demonstration only. Replace with the game's economy adapter.</summary>
    public sealed class OmniPrototypeEnergyStore : MonoBehaviour, IOmniEnergySource
    {
        [SerializeField, Min(0f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float energyOnStart = 100f;
        [SerializeField, Min(0f)] private float regenerationPerSecond = 8f;
        public float CurrentEnergy { get; private set; }
        private void Awake() { CurrentEnergy = Mathf.Clamp(energyOnStart, 0f, capacity); }
        private void Update()
        {
            CurrentEnergy = Mathf.Min(capacity, CurrentEnergy + regenerationPerSecond * Time.deltaTime);
        }
        public void Configure(float maximum, float initial, float regen)
        {
            capacity = OmniDriveMath.NonNegative(maximum);
            energyOnStart = Mathf.Clamp(OmniDriveMath.NonNegative(initial), 0f, capacity);
            regenerationPerSecond = OmniDriveMath.NonNegative(regen);
            CurrentEnergy = energyOnStart;
        }
        public bool TrySpend(float amount)
        {
            if (!OmniDriveMath.Valid(amount) || amount < 0f) return false;
            if (CurrentEnergy + 0.00001f < amount) return false;
            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
            return true;
        }
    }
}
