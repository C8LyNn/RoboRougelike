using System;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    [Serializable]
    public sealed class ShotgunConfig
    {
        [Min(1)] public int pelletCount = 8;
        [Range(0f, 90f)] public float fullConeAngleDegrees = 14f;
        [Min(0.1f)] public float projectileSpeed = 58f;
        [Min(0f)] public float damagePerPellet = 9f;
        [Min(0.1f)] public float maxTravelDistance = 40f;
        [Min(0.01f)] public float projectileLifetime = 2f;
        [Min(0f)] public float cooldownSeconds = 0.85f;
        [Range(0f, 1f)] public float inheritedVelocityFactor = 1f;
        [Min(0f)] public float recoilImpulse = 0f;
        public bool recoilAtMuzzle = false;

        // Snapshot clamps malformed serialized data without mutating the inspector values.
        // Limits prevent accidental thousands-of-projectiles bursts and extreme recoil.
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
            FullConeAngleDegrees = ClampFinite(fullConeAngleDegrees, 0f, 90f, 14f);
            ProjectileSpeed = ClampFinite(projectileSpeed, 0.1f, 250f, 58f);
            DamagePerPellet = ClampFinite(damagePerPellet, 0f, 1000f, 9f);
            MaxTravelDistance = ClampFinite(maxTravelDistance, 0.1f, 500f, 40f);
            ProjectileLifetime = ClampFinite(projectileLifetime, 0.01f, 15f, 2f);
            CooldownSeconds = ClampFinite(cooldownSeconds, 0.01f, 30f, 0.85f);
            InheritedVelocityFactor = ClampFinite(inheritedVelocityFactor, 0f, 1f, 1f);
            RecoilImpulse = ClampFinite(recoilImpulse, 0f, 10f, 0f);
            RecoilAtMuzzle = recoilAtMuzzle;
        }

        private static float ClampFinite(float value, float min, float max, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback : Mathf.Clamp(value, min, max);
        }
    }
}
