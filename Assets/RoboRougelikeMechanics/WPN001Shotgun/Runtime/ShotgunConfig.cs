using System;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    [Serializable]
    public sealed class ShotgunConfig
    {
        [Min(1)] public int pelletCount = 8;
        [Range(0f, 360f)] public float fullConeAngleDegrees = 14f;
        [Min(0.1f)] public float projectileSpeed = 58f;
        [Min(0f)] public float damagePerPellet = 9f;
        [Min(0.1f)] public float maxTravelDistance = 40f;
        [Min(0.01f)] public float projectileLifetime = 2f;
        [Min(0f)] public float cooldownSeconds = 0.85f;
        [Range(0f, 1f)] public float inheritedVelocityFactor = 1f;
        [Min(0f)] public float recoilImpulse = 0f;
        public bool recoilAtMuzzle = false;

        // Snapshot validates serialized data without mutating inspector values.
        // Some other fields retain package safety limits; positive cooldowns do not.
        public ShotgunSpec Snapshot() => new ShotgunSpec(
            pelletCount, fullConeAngleDegrees, projectileSpeed, damagePerPellet,
            maxTravelDistance, projectileLifetime, cooldownSeconds,
            inheritedVelocityFactor, recoilImpulse, recoilAtMuzzle);
    }

    public readonly struct ShotgunSpec
    {
        public readonly int PelletCount;
        public readonly float FullConeAngleDegrees;
        public readonly float ProjectileSpeed;
        public readonly float DamagePerPellet;
        public readonly float MaxTravelDistance;
        public readonly float ProjectileLifetime;
        public readonly float CooldownSeconds;
        public readonly float InheritedVelocityFactor;
        public readonly float RecoilImpulse;
        public readonly bool RecoilAtMuzzle;

        public ShotgunSpec(int pelletCount, float fullConeAngleDegrees,
            float projectileSpeed, float damagePerPellet, float maxTravelDistance,
            float projectileLifetime, float cooldownSeconds, float inheritedVelocityFactor,
            float recoilImpulse, bool recoilAtMuzzle)
        {
            PelletCount = Mathf.Clamp(pelletCount, 1, 64);
            FullConeAngleDegrees = ClampFinite(fullConeAngleDegrees, 0f, 360f, 14f);
            ProjectileSpeed = FiniteMinimum(projectileSpeed, 0.1f, 58f);
            DamagePerPellet = FiniteMinimum(damagePerPellet, 0f, 9f);
            MaxTravelDistance = FiniteMinimum(maxTravelDistance, 0.1f, 40f);
            ProjectileLifetime = FiniteMinimum(projectileLifetime, 0.01f, 2f);
            CooldownSeconds = NonNegativeCooldown(cooldownSeconds, 0.85f);
            InheritedVelocityFactor = ClampFinite(inheritedVelocityFactor, 0f, 1f, 1f);
            RecoilImpulse = ClampFinite(recoilImpulse, 0f, 10f, 0f);
            RecoilAtMuzzle = recoilAtMuzzle;
        }

        private static float ClampFinite(float value, float min, float max, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback : Mathf.Clamp(value, min, max);
        }
        private static float FiniteMinimum(float value,float min,float fallback)
            =>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Max(min,value);
        internal static float NonNegativeCooldown(float value,float fallback)
        {
            if(float.IsNaN(value)||float.IsInfinity(value))return fallback;
            return Mathf.Max(0,value);
        }
    }
}
