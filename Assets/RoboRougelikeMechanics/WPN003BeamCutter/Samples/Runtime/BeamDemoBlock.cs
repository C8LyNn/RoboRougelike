using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter.Samples
{
    /// <summary>Demonstration HP ONLY. Do not use for R6 module damage.</summary>
    public sealed class BeamDemoBlock : MonoBehaviour
    {
        [SerializeField] private float startingHp = 100f;
        public float Hp { get; private set; }
        private void Awake() => Hp = startingHp;
        public void SetHp(float value) => Hp = Mathf.Max(0f, value);
        public void ApplyDamage(float amount)
        {
            Hp = Mathf.Max(0f, Hp - Mathf.Max(0f, amount));
            if (Hp <= 0f) gameObject.SetActive(false);
        }
    }
}
