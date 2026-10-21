using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Optional DEMO store: use existing R6 energy system if present.
    public sealed class StructuralScanEnergyStore : MonoBehaviour, IStructuralScanEnergy
    {
        [Min(0f)] [SerializeField] private float capacity = 100f;
        [SerializeField] private float charge = 100f;
        public float Charge { get { return charge; } }
        public bool TrySpend(float units)
        {
            if (float.IsNaN(units) || float.IsInfinity(units) || units < 0f || charge < units) return false;
            charge -= units;
            return true;
        }
        public void Configure(float newCapacity, float newCharge)
        {
            capacity = Mathf.Max(0f, newCapacity);
            charge = Mathf.Clamp(newCharge, 0f, capacity);
        }
        private void OnValidate() { capacity = Mathf.Max(0f, capacity); charge = Mathf.Clamp(charge, 0f, capacity); }
    }
}
