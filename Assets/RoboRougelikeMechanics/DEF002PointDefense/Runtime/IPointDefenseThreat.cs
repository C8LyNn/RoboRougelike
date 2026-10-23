using UnityEngine;

namespace RoboRougelike.Mechanics.PointDefense
{
    /// <summary>Implement on a Rocket/Projectile R6 adapter; do not replace the existing projectile's HP.</summary>
    public interface IPointDefenseThreat
    {
        int FactionId { get; }
        bool IsActiveThreat { get; }
        Transform AimTransform { get; }
        Vector3 Velocity { get; }
        /// <summary>Return true only if this damage destroyed/intercepted the threat.</summary>
        bool TryIntercept(float damage);
    }

    public interface IPointDefenseEnergySource
    {
        bool TrySpend(float amount);
    }
}
