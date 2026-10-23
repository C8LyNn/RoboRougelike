using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    // Attach adapter per destructible BLOCK (not just one on vehicle root).
    // Existing R6 damage/fragmentation/salvage remain authoritative.
    public interface IRocketDamageReceiver
    {
        void ReceiveRocketDamage(float damage, Vector3 hitPoint, Vector3 impulse, GameObject attacker);
    }

    public interface IRocketAmmoSource
    {
        bool TrySpendAmmo(int units);
        void RefundAmmo(int units);
    }

    public interface IRocketEnergySource
    {
        bool TrySpendEnergy(float amount);
        void RefundEnergy(float amount);
    }

    public interface IRocketInterceptable
    {
        void ApplyInterceptionDamage(float amount);
    }

    public enum RocketFireResult
    {
        Fired, Disabled, Cooldown, MissingPrefab, MissingMuzzle, MissingOwner,
        OutOfAmmo, OutOfEnergy, SpawnFailed
    }
}
