using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>Optional standalone bank; use R6 energy interface when one exists.</summary>
    public sealed class PointDefenseEnergyStore : MonoBehaviour, IPointDefenseEnergySource
    {
        [SerializeField, Min(0f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float current = 100f;
        [SerializeField, Min(0f)] private float rechargePerSecond = 0f;
        public float Current { get { return current; } }
        public bool TrySpend(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            if (current + 0.00001f < amount) return false;
            current = Mathf.Max(0f, current - amount);
            return true;
        }
        public void Configure(float max, float start, float recharge)
        {
            capacity = Mathf.Max(0f, max);
            current = Mathf.Clamp(start, 0f, capacity);
            rechargePerSecond = Mathf.Max(0f, recharge);
        }
        private void Update()
        {
            current = Mathf.Min(capacity, current + rechargePerSecond * Time.deltaTime);
        }
    }
}
