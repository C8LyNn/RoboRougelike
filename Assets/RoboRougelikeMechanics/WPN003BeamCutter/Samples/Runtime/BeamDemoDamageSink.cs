using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter.Samples
{
    /// <summary>For temporary demo only. R6 production must provide its own damage adapter.</summary>
    public sealed class BeamDemoDamageSink : MonoBehaviour, IBeamDamageSink
    {
        public int HitCount { get; private set; }
        public float DeliveredDamage { get; private set; }
        public void ApplyBeamHit(in BeamHitRequest hit)
        {
            if (hit.Collider == null) return;
            BeamDemoBlock block = hit.Collider.GetComponentInParent<BeamDemoBlock>();
            if (block == null) return;
            block.ApplyDamage(hit.Damage);
            HitCount++;
            DeliveredDamage += hit.Damage;
        }
    }
}
