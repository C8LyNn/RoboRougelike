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
        [Min(0.01f)] public float damagePerEnergy = 0.65f;
        [Min(1f)] public float maximumDamagePerPart = 75f;
        [Range(1, 24)] public int maximumDistinctParts = 6;
        [Min(0f)] public float minimumEnergyToContinue = 2f;
        [Min(0.01f)] public float cooldownSeconds = 2.4f;
        [Min(0f)] public float recoilImpulse = 48f;
        public bool recoilAtMuzzle = true;
        [Range(0f, 2f)] public float inheritedVelocityFactor = 1f;

        public RailgunSpec Snapshot() => new RailgunSpec(
            RailgunRules.ClampFinite(projectileSpeed, 1f, 600f, 85f),
            RailgunRules.ClampFinite(maximumRange, 1f, 1500f, 120f),
            RailgunRules.ClampFinite(projectileRadius, 0.005f, 1f, 0.075f),
            RailgunRules.ClampFinite(initialPenetrationEnergy, 1f, 10000f, 100f),
            RailgunRules.ClampFinite(energyLossPerMeter, 0f, 100f, 0.4f),
            RailgunRules.ClampFinite(damagePerEnergy, 0.001f, 100f, 0.65f),
            RailgunRules.ClampFinite(maximumDamagePerPart, 0.01f, 5000f, 75f),
            Mathf.Clamp(maximumDistinctParts, 1, 24),
            RailgunRules.ClampFinite(minimumEnergyToContinue, 0f, 9999f, 2f),
            RailgunRules.ClampFinite(cooldownSeconds, 0.01f, 120f, 2.4f),
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
