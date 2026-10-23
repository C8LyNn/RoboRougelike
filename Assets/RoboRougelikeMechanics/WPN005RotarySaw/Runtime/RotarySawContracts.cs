using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    // Targets must be resolved by stable PART identity, NOT by Collider.GetInstanceID().
    public struct SawTarget
    {
        public readonly int partId;
        public readonly float materialResistance;

        public SawTarget(int partId, float materialResistance)
        {
            this.partId = partId;
            this.materialResistance = materialResistance;
        }
    }

    public struct SawDamageEvent
    {
        public readonly int partId;
        public readonly float damage;
        public readonly Vector3 worldPoint;
        public readonly Vector3 worldBladeAxis;
        public readonly Collider contactCollider;
        public readonly RotarySawModule source;

        public SawDamageEvent(int partId, float damage, Vector3 worldPoint,
            Vector3 worldBladeAxis, Collider contactCollider, RotarySawModule source)
        {
            this.partId = partId;
            this.damage = damage;
            this.worldPoint = worldPoint;
            this.worldBladeAxis = worldBladeAxis;
            this.contactCollider = contactCollider;
            this.source = source;
        }
    }

    public interface IRotarySawDamageSink
    {
        bool TryResolve(Collider collider, out SawTarget target);
        void ApplyDamage(SawDamageEvent damage);
    }

    public interface IRotarySawEnergySource
    {
        // Must be atomic: consume all requested energy or consume none.
        bool TryConsume(float amount);
    }
}
