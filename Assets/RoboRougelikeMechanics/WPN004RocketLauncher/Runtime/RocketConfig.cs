using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    [Serializable]
    public sealed class RocketConfig
    {
        [Min(0.1f)] public float initialSpeed = 18f;
        [Min(0.1f)] public float maxSpeed = 34f;
        [Min(0f)] public float acceleration = 20f;
        [Min(0.1f)] public float maxLifetime = 5f;
        [Min(0.02f)] public float collisionRadius = 0.12f;
        [Min(0f)] public float inheritedVelocityFraction = 1f;
        [Min(0f)] public float damageAtCenter = 60f;
        [Min(0.1f)] public float explosionRadius = 3.2f;
        [Range(0f, 1f)] public float edgeDamageFraction = 0.15f;
        [Min(0f)] public float maxImpulsePerBody = 12f;
        [Min(0f)] public float cooldown = 1.7f;
        [Min(0)] public int ammoCost = 1;
        [Min(0f)] public float energyCost = 0f;
        [Min(0f)] public float recoilImpulse = 5f;
        [Min(0f)] public float guidanceDegreesPerSecond = 0f;
        [Min(0.1f)] public float rocketHitPoints = 12f;
        public bool explodeWhenIntercepted = false;
        public bool damageOwner = false;
        public LayerMask impactMask = ~0;
        public LayerMask explosionMask = ~0;
        public LayerMask occlusionMask = ~0;
        [Range(8, 4096)] public int maxBlastColliders = 256;
        [Range(8, 1024)] public int maxCastHits = 128;

        public RocketSpec Snapshot()
        {
            return new RocketSpec(
                RocketMath.Positive(initialSpeed, 18f),
                RocketMath.Positive(Mathf.Max(initialSpeed, maxSpeed), 34f),
                RocketMath.NonNegative(acceleration), RocketMath.Positive(maxLifetime, 5f),
                Mathf.Clamp(RocketMath.Positive(collisionRadius, 0.12f), 0.02f, 5f),
                Mathf.Clamp(RocketMath.NonNegative(inheritedVelocityFraction), 0f, 2f),
                RocketMath.NonNegative(damageAtCenter), RocketMath.Positive(explosionRadius, 3.2f),
                Mathf.Clamp01(RocketMath.NonNegative(edgeDamageFraction)),
                RocketMath.NonNegative(maxImpulsePerBody), RocketMath.NonNegative(cooldown),
                Mathf.Max(0, ammoCost), RocketMath.NonNegative(energyCost),
                RocketMath.NonNegative(recoilImpulse), RocketMath.NonNegative(guidanceDegreesPerSecond),
                RocketMath.Positive(rocketHitPoints, 12f), explodeWhenIntercepted, damageOwner,
                impactMask, explosionMask, occlusionMask,
                Mathf.Clamp(maxBlastColliders, 8, 4096), Mathf.Clamp(maxCastHits, 8, 1024));
        }
    }

    public readonly struct RocketSpec
    {
        public readonly float InitialSpeed, MaxSpeed, Acceleration, Lifetime, CollisionRadius;
        public readonly float InheritedVelocityFraction, CenterDamage, Radius, EdgeDamageFraction;
        public readonly float MaxImpulsePerBody, Cooldown, EnergyCost, RecoilImpulse;
        public readonly float GuidanceDegreesPerSecond, RocketHitPoints;
        public readonly int AmmoCost, ImpactMask, ExplosionMask, OcclusionMask, MaxBlastColliders, MaxCastHits;
        public readonly bool ExplodeWhenIntercepted, DamageOwner;

        public RocketSpec(float initialSpeed, float maxSpeed, float acceleration, float lifetime,
            float collisionRadius, float inheritedVelocityFraction, float centerDamage, float radius,
            float edgeDamageFraction, float maxImpulsePerBody, float cooldown, int ammoCost,
            float energyCost, float recoilImpulse, float guidanceDegreesPerSecond, float rocketHitPoints,
            bool explodeWhenIntercepted, bool damageOwner, int impactMask, int explosionMask,
            int occlusionMask, int maxBlastColliders, int maxCastHits)
        {
            InitialSpeed = initialSpeed; MaxSpeed = maxSpeed; Acceleration = acceleration;
            Lifetime = lifetime; CollisionRadius = collisionRadius;
            InheritedVelocityFraction = inheritedVelocityFraction; CenterDamage = centerDamage;
            Radius = radius; EdgeDamageFraction = edgeDamageFraction;
            MaxImpulsePerBody = maxImpulsePerBody; Cooldown = cooldown;
            AmmoCost = ammoCost; EnergyCost = energyCost; RecoilImpulse = recoilImpulse;
            GuidanceDegreesPerSecond = guidanceDegreesPerSecond; RocketHitPoints = rocketHitPoints;
            ExplodeWhenIntercepted = explodeWhenIntercepted; DamageOwner = damageOwner;
            ImpactMask = impactMask; ExplosionMask = explosionMask; OcclusionMask = occlusionMask;
            MaxBlastColliders = maxBlastColliders; MaxCastHits = maxCastHits;
        }
    }
}
