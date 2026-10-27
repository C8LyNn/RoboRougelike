using UnityEngine;

namespace RoboRougelike.Mechanics.MOV009
{
    /// <summary>Demonstration battery. Replace with the game's common energy system.</summary>
    [DisallowMultipleComponent]
    public sealed class ActiveSuspensionEnergyStore : MonoBehaviour, IActiveSuspensionEnergySource
    {
        [Min(0f)] [SerializeField] private float capacity = 100f;
        [Min(0f)] [SerializeField] private float regenerationPerSecond = 5f;
        [SerializeField] private float initialEnergy = 100f;
        private float energy;

        public float Energy => energy;
        public float Capacity => capacity;

        private void Awake() => energy = Mathf.Clamp(SuspensionMath.Finite(initialEnergy), 0f, capacity);
        private void FixedUpdate() => energy = Mathf.Min(capacity, energy + Mathf.Max(0f, regenerationPerSecond) * Time.fixedDeltaTime);

        public void Configure(float newCapacity, float regenPerSecond, float startingEnergy)
        {
            capacity = Mathf.Max(0f, SuspensionMath.Finite(newCapacity));
            regenerationPerSecond = Mathf.Max(0f, SuspensionMath.Finite(regenPerSecond));
            energy = Mathf.Clamp(SuspensionMath.Finite(startingEnergy), 0f, capacity);
            initialEnergy = energy;
        }

        public bool TryConsumeEnergy(float amount)
        {
            amount = SuspensionMath.Finite(amount, float.PositiveInfinity);
            if (amount < 0f || float.IsInfinity(amount)) return false;
            if (energy + 0.000001f < amount) return false;
            energy = Mathf.Max(0f, energy - amount);
            return true;
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(0f, SuspensionMath.Finite(capacity));
            regenerationPerSecond = Mathf.Max(0f, SuspensionMath.Finite(regenerationPerSecond));
            initialEnergy = Mathf.Clamp(SuspensionMath.Finite(initialEnergy), 0f, capacity);
        }
    }
}
