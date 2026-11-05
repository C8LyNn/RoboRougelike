using System;

namespace RoboRougelike.R3
{
    /// <summary>Shared, bounded energy accounting for any vehicle.</summary>
    public sealed class VehicleEnergyState
    {
        public const float CoreCapacity = 500f;
        public const float BatteryCapacity = 100f;
        public const float PassiveReservation = 25f;
        public const float RegenerationPerSecond = 5f;

        public float Maximum { get; private set; } = CoreCapacity;
        public float Current { get; private set; } = CoreCapacity;

        public void Refresh(int batteries, int passiveModules, bool coreAlive)
        {
            var available = coreAlive
                ? CoreCapacity + Math.Max(0, (double)batteries) * BatteryCapacity
                    - Math.Max(0, (double)passiveModules) * PassiveReservation
                : 0d;
            Maximum = (float)Math.Min(float.MaxValue, Math.Max(0d, available));
            Current = Math.Min(Current, Maximum);
        }

        public bool TrySpend(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f || amount > Current)
                return false;
            Current -= amount;
            return true;
        }

        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;
            Recharge(RegenerationPerSecond * seconds);
        }

        public void Recharge(float amount)
        {
            if (float.IsNaN(amount) || amount <= 0f) return;
            Current = (float)Math.Min(Maximum, (double)Current + amount);
        }

        public void Fill() => Current = Maximum;

        public void Restore(float amount)
        {
            Current = float.IsNaN(amount) || float.IsInfinity(amount)
                ? 0f : Math.Max(0f, Math.Min(Maximum, amount));
        }
    }

    public static class VehicleEnergyCosts
    {
        public const float BasicShot=5f, Shotgun=10f, Railgun=18f, Rocket=15f, Emp=25f;
        public const float ContinuousWeaponPerSecond=12f, BoosterActivation=15f;
        public const float Jump=25f, Blink=30f, Grapple=12f;
    }
}
