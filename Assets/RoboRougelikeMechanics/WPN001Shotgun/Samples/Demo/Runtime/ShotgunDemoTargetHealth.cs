using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Demo
{
    /// <summary>Demo damage receiver. Never use as an adapter for R6 block damage.</summary>
    public sealed class ShotgunDemoTargetHealth : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float health = 100f;
        public float Health => health;
        public int Hits { get; private set; }
        public void Configure(float hitPoints) { health = Mathf.Max(0f, hitPoints); Hits = 0; }
        public void ApplyDamage(float damage)
        {
            if (health <= 0f) return;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return;
            health = Mathf.Max(0f, health - damage);
            Hits++;
        }
    }
}
