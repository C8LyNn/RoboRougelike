using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    // Prototype adapter only; R6 must implement IRotarySawDamageSink via actual part IDs.
    public sealed class RotarySawDemoDamageSink : MonoBehaviour, IRotarySawDamageSink
    {
        public bool TryResolve(Collider collider, out SawTarget target)
        {
            target = default(SawTarget);
            if (collider == null) return false;
            RotarySawDemoTarget demo = collider.GetComponentInParent<RotarySawDemoTarget>();
            if (demo == null || demo.HitPoints <= 0f) return false;
            target = new SawTarget(demo.GetInstanceID(), demo.Resistance);
            return true;
        }

        public void ApplyDamage(SawDamageEvent damage)
        {
            if (damage.contactCollider == null) return;
            RotarySawDemoTarget demo = damage.contactCollider.GetComponentInParent<RotarySawDemoTarget>();
            if (demo != null && demo.GetInstanceID() == damage.partId)
                demo.Receive(damage.damage);
        }
    }
}
