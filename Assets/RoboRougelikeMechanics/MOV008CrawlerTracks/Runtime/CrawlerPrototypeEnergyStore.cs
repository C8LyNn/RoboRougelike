using UnityEngine;

namespace RoboRougelike.Mechanics.MOV008
{
    /// <summary>Standalone demonstration resource. Replace with the game's power adapter.</summary>
    public sealed class CrawlerPrototypeEnergyStore : MonoBehaviour, ICrawlerEnergySource
    {
        [Min(0f)] [SerializeField] private float capacity = 100f;
        [Min(0f)] [SerializeField] private float rechargePerSecond = 12f;
        [SerializeField] private float currentEnergy = 100f;

        public float CurrentEnergy => currentEnergy;
        public float Capacity => capacity;

        private void OnValidate()
        {
            capacity = Mathf.Max(0f, CrawlerTrackMath.Finite(capacity));
            rechargePerSecond = Mathf.Max(0f, CrawlerTrackMath.Finite(rechargePerSecond));
            currentEnergy = Mathf.Clamp(CrawlerTrackMath.Finite(currentEnergy), 0f, capacity);
        }

        public void Configure(float newCapacity, float newCharge, float initialEnergy)
        {
            capacity = Mathf.Max(0f, CrawlerTrackMath.Finite(newCapacity));
            rechargePerSecond = Mathf.Max(0f, CrawlerTrackMath.Finite(newCharge));
            currentEnergy = Mathf.Clamp(CrawlerTrackMath.Finite(initialEnergy), 0f, capacity);
        }

        public bool TryConsume(float amount)
        {
            if (!enabled || !gameObject.activeInHierarchy) return false;
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            if (amount > currentEnergy + 0.00001f) return false;
            currentEnergy = Mathf.Max(0f, currentEnergy - amount);
            return true;
        }

        private void Update()
        {
            currentEnergy = Mathf.Min(capacity, currentEnergy + rechargePerSecond * Time.deltaTime);
        }
    }
}
