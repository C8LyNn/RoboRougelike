using UnityEngine;

namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer
{
    /// <summary>
    /// OPTIONAL sample energy provider. Remove/replace when binding to RoboRougelike energy.
    /// Recharge occurs in FixedUpdate; use the same provider across all gyros for shared budget.
    /// </summary>
    public sealed class GyroPrototypeEnergyStore : MonoBehaviour, IGyroEnergySource
    {
        [SerializeField, Min(0f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float rechargePerSecond = 10f;
        [SerializeField] private bool startFull = true;
        private GyroEnergyBudget budget;

        public float CurrentEnergy { get { return budget != null ? budget.Current : 0f; } }
        public float Capacity { get { return budget != null ? budget.Capacity : Mathf.Max(0f, capacity); } }

        private void Awake()
        {
            budget = new GyroEnergyBudget(capacity, rechargePerSecond, startFull);
        }

        private void FixedUpdate()
        {
            if (budget != null) budget.Tick(Time.fixedDeltaTime);
        }

        public bool TrySpend(float amount)
        {
            return budget != null && budget.TrySpend(amount);
        }

        public void SetEnergy(float energy)
        {
            if (budget != null) budget.SetCurrent(energy);
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(0f, capacity);
            rechargePerSecond = Mathf.Max(0f, rechargePerSecond);
        }
    }
}
