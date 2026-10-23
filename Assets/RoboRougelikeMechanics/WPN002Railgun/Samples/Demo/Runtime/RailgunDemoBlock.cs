using UnityEngine;

namespace RoboRougelike.WPN002Railgun.Demo
{
    // Demo ONLY. Does not represent actual R6 block HP/detachment/salvage.
    public sealed class RailgunDemoBlock : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float hitPoints = 65f;
        [SerializeField, Min(0.01f)] private float resistance = 30f;
        [SerializeField] private bool penetrable = true;
        public float HitPoints => hitPoints;
        public float Resistance => resistance;
        public bool Penetrable => penetrable;
        public int HitCount { get; private set; }
        public void Configure(float hp, float energyCost, bool allowPass = true)
        { hitPoints = Mathf.Max(1f, hp); resistance = Mathf.Max(0.01f, energyCost); penetrable = allowPass; }
        public void Receive(float damage)
        {
            if (hitPoints <= 0f) return;
            HitCount++;
            hitPoints = Mathf.Max(0f, hitPoints - Mathf.Max(0f, damage));
            if (hitPoints == 0f)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = Color.gray;
            }
        }
    }
}
