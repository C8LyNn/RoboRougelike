using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    // Optional prototype adapter; replace with the project's real energy provider on integration.
    public sealed class BlinkEnergyBank : MonoBehaviour, IBlinkEnergySource
    {
        [Min(0f)] [SerializeField] private float capacity = 100f;
        [SerializeField] private bool startFull = true;
        private BlinkEnergyPool pool;
        public float Current => pool == null ? 0f : pool.Current;
        private void Awake() { pool = new BlinkEnergyPool(capacity, startFull ? capacity : 0f); }
        public bool CanSpend(float amount) => pool != null && pool.CanSpend(amount);
        public bool TrySpend(float amount) => pool != null && pool.TrySpend(amount);
        public void Recharge(float amount) { if (pool != null) pool.Recharge(amount); }
    }
}
