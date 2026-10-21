using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.Booster
{
    /// <summary>OPTIONAL self-contained demo energy store; not required by the booster.</summary>
    [DisallowMultipleComponent]
    public sealed class BoosterEnergyReservoir : MonoBehaviour, IBoosterEnergySource
    {
        [SerializeField, Min(0f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float startingEnergy = 100f;
        [SerializeField, Min(0f)] private float rechargePerSecond = 8f;

        private float currentEnergy;
        private bool initialized;

        public float CurrentEnergy { get { EnsureInitialized(); return currentEnergy; } }
        public float Capacity => capacity;

        private void Awake() => EnsureInitialized();

        private void FixedUpdate() => Regenerate(Time.fixedDeltaTime);

        public bool TryConsume(float amount)
        {
            EnsureInitialized();
            if (!BoosterCycle.IsFinite(amount) || amount < 0f) return false;
            if (currentEnergy + 0.00001f < amount) return false;
            currentEnergy = Mathf.Max(0f, currentEnergy - amount);
            return true;
        }

        public void Regenerate(float deltaSeconds)
        {
            EnsureInitialized();
            if (!BoosterCycle.IsFinite(deltaSeconds) || deltaSeconds <= 0f) return;
            currentEnergy = Mathf.Min(capacity, currentEnergy + rechargePerSecond * deltaSeconds);
        }

        public void Refill()
        {
            EnsureInitialized();
            currentEnergy = capacity;
        }

        /// <summary>Convenient for demo configuration and deterministic tests.</summary>
        public void Configure(float newCapacity, float initialEnergy, float newRechargePerSecond)
        {
            if (!BoosterCycle.IsFinite(newCapacity) || newCapacity < 0f ||
                !BoosterCycle.IsFinite(initialEnergy) || initialEnergy < 0f ||
                !BoosterCycle.IsFinite(newRechargePerSecond) || newRechargePerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), "Energy settings must be finite and non-negative.");

            capacity = newCapacity;
            startingEnergy = initialEnergy;
            rechargePerSecond = newRechargePerSecond;
            currentEnergy = Mathf.Clamp(initialEnergy, 0f, capacity);
            initialized = true;
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            capacity = Mathf.Max(0f, capacity);
            startingEnergy = Mathf.Max(0f, startingEnergy);
            rechargePerSecond = Mathf.Max(0f, rechargePerSecond);
            currentEnergy = Mathf.Clamp(startingEnergy, 0f, capacity);
            initialized = true;
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(0f, capacity);
            startingEnergy = Mathf.Max(0f, startingEnergy);
            rechargePerSecond = Mathf.Max(0f, rechargePerSecond);
        }
    }
}
