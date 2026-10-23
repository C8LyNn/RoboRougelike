using UnityEngine;

namespace RoboRougelike.Defenses.DirectionalShield
{
    /// <summary>Opcjonalny sample. W R6 podłącz wspólny system energii zamiast duplikować zasoby.</summary>
    public sealed class ShieldEnergyReservoir : MonoBehaviour, IShieldEnergySource
    {
        [SerializeField, Min(0f)] private float maximum = 150f;
        [SerializeField, Min(0f)] private float refillPerSecond = 5f;
        [SerializeField] private bool startFull = true;
        private float _current;
        public float Current => _current;

        private void Awake() => _current = startFull ? Mathf.Max(0f, maximum) : 0f;

        private void Update()
        {
            if (!isActiveAndEnabled) return;
            _current = Mathf.Min(Mathf.Max(0f, maximum), _current + Mathf.Max(0f, refillPerSecond) * Time.deltaTime);
        }

        public bool TryConsume(float amount)
        {
            if (!isActiveAndEnabled || !ShieldMath.Finite(amount) || amount < 0f) return false;
            if (amount > _current + 0.000001f) return false;
            _current = Mathf.Max(0f, _current - amount);
            return true;
        }

        public void SetForTests(float current, float max)
        {
            maximum = Mathf.Max(0f, max);
            _current = Mathf.Clamp(current, 0f, maximum);
        }
    }
}
