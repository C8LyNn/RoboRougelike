using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    [Serializable]
    public sealed class PointDefenseConfiguration
    {
        [Min(1f)] public float detectionRadius = 24f;
        [Min(0.05f)] public float scanInterval = 0.16f;
        [Min(0.1f)] public float threatLookAhead = 5f;
        [Min(0.1f)] public float defendedRadius = 5f;
        [Min(1f)] public float traverseDegreesPerSecond = 200f;
        [Range(0f, 25f)] public float firingToleranceDegrees = 5f;
        [Min(0.02f)] public float shotInterval = 0.24f;
        [Min(1f)] public float interceptorSpeed = 65f;
        [Min(0.1f)] public float interceptorLifetime = 2f;
        [Min(0.01f)] public float interceptorRadius = 0.15f;
        [Min(0.01f)] public float interceptorDamage = 1f;
        [Min(0f)] public float recoilImpulse = 0f;
        [Min(0f)] public float energyPerShot = 0f;
        public bool useAmmunition = false;
        [Min(0)] public int startingAmmunition = 40;
        public LayerMask threatLayerMask = ~0;
        public LayerMask blockerLayerMask = ~0;

        public PointDefenseConfiguration Copy()
        {
            return (PointDefenseConfiguration)MemberwiseClone();
        }

        public void Sanitize()
        {
            detectionRadius = PointDefenseMath.ClampPositive(detectionRadius, 24f);
            scanInterval = PointDefenseMath.ClampPositive(scanInterval, 0.16f);
            threatLookAhead = PointDefenseMath.ClampPositive(threatLookAhead, 5f);
            defendedRadius = PointDefenseMath.ClampPositive(defendedRadius, 5f);
            traverseDegreesPerSecond = PointDefenseMath.ClampPositive(traverseDegreesPerSecond, 200f);
            firingToleranceDegrees = Mathf.Clamp(float.IsNaN(firingToleranceDegrees) ? 5f : firingToleranceDegrees, 0f, 25f);
            shotInterval = PointDefenseMath.ClampPositive(shotInterval, 0.24f);
            interceptorSpeed = PointDefenseMath.ClampPositive(interceptorSpeed, 65f);
            interceptorLifetime = PointDefenseMath.ClampPositive(interceptorLifetime, 2f);
            interceptorRadius = PointDefenseMath.ClampPositive(interceptorRadius, 0.15f);
            interceptorDamage = PointDefenseMath.ClampPositive(interceptorDamage, 1f);
            recoilImpulse = float.IsNaN(recoilImpulse) || float.IsInfinity(recoilImpulse) ? 0f : Mathf.Max(0f, recoilImpulse);
            energyPerShot = float.IsNaN(energyPerShot) || float.IsInfinity(energyPerShot) ? 0f : Mathf.Max(0f, energyPerShot);
            startingAmmunition = Mathf.Max(0, startingAmmunition);
        }
    }
}
