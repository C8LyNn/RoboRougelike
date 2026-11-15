using System;
using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    [Serializable]
    public sealed class RailgunConfig
    {
        [Min(1f)] public float projectileSpeed = 85f;
        [Min(0.1f)] public float maximumRange = 120f;
        [Min(0.005f)] public float projectileRadius = 0.075f;
        [Min(0.1f)] public float initialPenetrationEnergy = 100f;
        [Min(0f)] public float energyLossPerMeter = 0.4f;
        [Min(0f)] public float damagePerEnergy = 0.65f;
        [Min(0f)] public float maximumDamagePerPart = 75f;
        [Min(1)] public int maximumDistinctParts = 6;
        [Min(0f)] public float minimumEnergyToContinue = 2f;
        [Min(0f)] public float cooldownSeconds = 2.4f;
        [Min(0f)] public float recoilImpulse = 48f;
        public bool recoilAtMuzzle = true;
        [Range(0f, 2f)] public float inheritedVelocityFactor = 1f;

        public RailgunSpec Snapshot() => new RailgunSpec(
            RailgunRules.ClampFinite(projectileSpeed, 1f, 600f, 85f),
            RailgunRules.Finite(maximumRange)?Mathf.Max(1f,maximumRange):120f,
            RailgunRules.ClampFinite(projectileRadius, 0.005f, 1f, 0.075f),
            RailgunRules.Finite(initialPenetrationEnergy)?Mathf.Max(1f,initialPenetrationEnergy):100f,
            RailgunRules.ClampFinite(energyLossPerMeter, 0f, 100f, 0.4f),
            RailgunRules.Finite(damagePerEnergy)?Mathf.Max(0f,damagePerEnergy):0.65f,
            RailgunRules.Finite(maximumDamagePerPart)?Mathf.Max(0f,maximumDamagePerPart):75f,
            Mathf.Max(1,maximumDistinctParts),
            RailgunRules.ClampFinite(minimumEnergyToContinue, 0f, 9999f, 2f),
            RailgunRules.NonNegativeCooldown(cooldownSeconds, 2.4f),
            RailgunRules.ClampFinite(recoilImpulse, 0f, 50000f, 48f),
            recoilAtMuzzle,
            RailgunRules.ClampFinite(inheritedVelocityFactor, 0f, 2f, 1f));
    }

    public readonly struct RailgunSpec
    {
        public readonly float Speed, Range, Radius, InitialEnergy, EnergyLossPerMeter;
        public readonly float DamagePerEnergy, MaxDamagePerPart, MinEnergyToContinue, Cooldown;
        public readonly float RecoilImpulse, InheritedVelocityFactor;
        public readonly int MaxDistinctParts;
        public readonly bool RecoilAtMuzzle;
        public RailgunSpec(float speed, float range, float radius, float energy,
            float loss, float damageFactor, float damageCap, int maxParts, float minEnergy,
            float cooldown, float recoil, bool recoilAtMuzzle, float inherited)
        {
            Speed = speed; Range = range; Radius = radius; InitialEnergy = energy;
            EnergyLossPerMeter = loss; DamagePerEnergy = damageFactor;
            MaxDamagePerPart = damageCap; MaxDistinctParts = maxParts;
            MinEnergyToContinue = minEnergy; Cooldown = cooldown;
            RecoilImpulse = recoil; RecoilAtMuzzle = recoilAtMuzzle;
            InheritedVelocityFactor = inherited;
        }
    }
}
