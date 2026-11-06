using System;
using System.Collections.Generic;

namespace RoboRougelike.R3
{
    /// <summary>Shared, bounded energy accounting for any vehicle.</summary>
    public sealed class VehicleEnergyState
    {
        public const float CoreCapacity = 500f;
        public const float BatteryCapacity = 100f;
        public const float PassiveReservation = 25f;
        public const float RegenerationPerSecond = 10f;

        float fieldRemaining;
        readonly List<float> pickups = new List<float>();
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

        public void Advance(float seconds) => AdvanceWithMotion(seconds, false);

        public void AdvanceWithMotion(float seconds, bool stationary)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;
            double charge=RegenerationPerSecond*(stationary?4d:1d)*seconds;
            charge+=Maximum*.05d*Math.Min(seconds,fieldRemaining);
            fieldRemaining=Math.Max(0f,fieldRemaining-seconds);
            for(int i=pickups.Count-1;i>=0;i--)
            {
                charge+=Maximum*.1d*Math.Min(seconds,pickups[i]);
                pickups[i]-=seconds;
                if(pickups[i]<=0)pickups.RemoveAt(i);
            }
            Recharge((float)Math.Min(float.MaxValue,charge));
        }

        public void ApplyFieldCharge()
        {
            Recharge((Maximum-Current)*.5f);
            fieldRemaining=10f;
        }
        public void ApplyEnergyPickup() => pickups.Add(5f);

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
        public const float BasicShot=2.5f, Shotgun=5f, Railgun=9f, Rocket=7.5f, Emp=12.5f;
        public const float ContinuousWeaponPerSecond=6f, BoosterActivation=7.5f;
        public const float Jump=12.5f, Blink=15f, Grapple=6f;
    }
}
