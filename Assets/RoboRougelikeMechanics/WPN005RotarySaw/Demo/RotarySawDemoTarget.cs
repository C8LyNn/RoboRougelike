using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    // DEMO ONLY: DO NOT use as the game's HP/structural-damage authority.
    public sealed class RotarySawDemoTarget : MonoBehaviour
    {
        [SerializeField] private float hitPoints = 120f;
        [SerializeField] [Min(0.01f)] private float resistance = 1f;
        public float HitPoints { get { return hitPoints; } }
        public float Resistance { get { return resistance; } }
        public int HitCount { get; private set; }
        public float TotalDamage { get; private set; }
        public void SetResistance(float value) { resistance = Mathf.Max(0.01f, value); }
        public void SetHitPoints(float value) { hitPoints = Mathf.Max(0f, value); }
        public void Receive(float amount)
        {
            if (hitPoints <= 0f || amount <= 0f) return;
            HitCount++;
            float applied = Mathf.Min(hitPoints, amount);
            hitPoints -= applied;
            TotalDamage += applied;
        }
    }
}
