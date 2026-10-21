using UnityEngine;

namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    /// <summary>Optional sample bank; replace with an adapter if the game already has an energy system.</summary>
    [DisallowMultipleComponent]
    public sealed class JumpDriveEnergyBank : MonoBehaviour, IJumpDriveEnergySource
    {
        [SerializeField, Min(0f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float regenerationPerSecond = 10f;
        [SerializeField] private bool startFull = true;
        private JumpDriveEnergyPool pool;
        public float Energy => pool == null ? 0f : pool.Current;
        public float Capacity => pool == null ? 0f : pool.Capacity;

        private void Awake() => pool = new JumpDriveEnergyPool(capacity, startFull ? capacity : 0f);
        private void FixedUpdate()
        {
            if (pool != null && JumpDriveMath.Valid(regenerationPerSecond) && regenerationPerSecond > 0f)
                pool.Regenerate(regenerationPerSecond * Time.fixedDeltaTime);
        }
        public bool TrySpend(float amount) => pool != null && pool.TrySpend(amount);
        public void Refund(float amount) => pool?.Refund(amount);
        public void ResetToFull() => pool?.ResetToFull();
    }
}
