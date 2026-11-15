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
        public const float RegenerationPerModule = 5f;

        float fieldRemaining;
        int regeneratorCount;
        float capacityMultiplier=1f,batteryCapacityMultiplier=1f,regenerationMultiplier=1f;
        public void SetResourceMultipliers(float capacity,float regeneration,float? batteryCapacity=null)
        {
            capacityMultiplier=float.IsNaN(capacity)||float.IsInfinity(capacity)?1f:Math.Max(0,capacity);
            float battery=batteryCapacity??capacity;
            batteryCapacityMultiplier=float.IsNaN(battery)||float.IsInfinity(battery)?1f:Math.Max(0,battery);
            regenerationMultiplier=float.IsNaN(regeneration)||float.IsInfinity(regeneration)?1f:Math.Max(0,regeneration);
        }
        float additionalPassiveReservation;
        public void SetRegeneratorCount(int count)=>regeneratorCount=Math.Max(0,count);
        public void SetAdditionalPassiveReservation(float amount)=>additionalPassiveReservation=
            float.IsNaN(amount)||float.IsInfinity(amount)?0:Math.Max(0,amount);
        readonly List<float> pickups = new List<float>();
        public float Maximum { get; private set; } = CoreCapacity;
        public float Current { get; private set; } = CoreCapacity;

        public void Refresh(int batteries, int passiveModules, bool coreAlive)
        {
            var available = coreAlive
                ? (double)CoreCapacity*capacityMultiplier + Math.Max(0, (double)batteries) * BatteryCapacity*batteryCapacityMultiplier
                    - Math.Max(0, (double)passiveModules) * PassiveReservation-additionalPassiveReservation
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

        internal System.Numerics.BigInteger SpendUpTo(float unitCost,System.Numerics.BigInteger requested)
        {
            if(requested<=0||float.IsNaN(unitCost)||float.IsInfinity(unitCost)||unitCost<0)return System.Numerics.BigInteger.Zero;
            if(unitCost==0)return requested;
            var available=DeviceFireSchedule.Ticks(Current);
            var cost=DeviceFireSchedule.Ticks(unitCost);
            var count=System.Numerics.BigInteger.Min(requested,available/cost);
            var remainder=available-count*cost;
            Current=(float)((double)remainder*1.401298464324817e-45d);
            return count;
        }

        internal System.Numerics.BigInteger[] SpendFair(float[] unitCosts,System.Numerics.BigInteger[] requested)
        {
            if(unitCosts==null||requested==null||unitCosts.Length!=requested.Length)throw new ArgumentException("Shot costs and requests must match.");
            var paid=new System.Numerics.BigInteger[requested.Length];
            var remaining=new System.Numerics.BigInteger[requested.Length];
            var costs=new System.Numerics.BigInteger[requested.Length];
            for(int i=0;i<requested.Length;i++)
            {
                float cost=unitCosts[i];
                if(requested[i]<=0||float.IsNaN(cost)||float.IsInfinity(cost)||cost<0)continue;
                if(cost==0){paid[i]=requested[i];continue;}
                costs[i]=DeviceFireSchedule.Ticks(cost);remaining[i]=requested[i];
            }
            var available=DeviceFireSchedule.Ticks(Current);
            while(available>0)
            {
                var max=System.Numerics.BigInteger.Zero;var minCost=System.Numerics.BigInteger.Zero;
                for(int i=0;i<remaining.Length;i++)
                    if(remaining[i]>0&&costs[i]<=available)
                    {max=System.Numerics.BigInteger.Max(max,remaining[i]);if(minCost.IsZero||costs[i]<minCost)minCost=costs[i];}
                if(minCost.IsZero)break;
                var low=System.Numerics.BigInteger.Zero;var high=System.Numerics.BigInteger.Min(max,available/minCost);
                while(low<high)
                {
                    var middle=(low+high+1)>>1;var total=System.Numerics.BigInteger.Zero;
                    for(int i=0;i<remaining.Length&&total<=available;i++)
                        if(remaining[i]>0&&costs[i]<=available)total+=System.Numerics.BigInteger.Min(remaining[i],middle)*costs[i];
                    if(total<=available)low=middle;else high=middle-1;
                }
                if(low>0)
                    for(int i=0;i<remaining.Length;i++)
                        if(remaining[i]>0&&costs[i]<=available)
                        {var count=System.Numerics.BigInteger.Min(remaining[i],low);remaining[i]-=count;paid[i]+=count;available-=count*costs[i];}
                for(int i=0;i<remaining.Length;i++)
                    if(remaining[i]>0&&costs[i]<=available)
                    {remaining[i]--;paid[i]++;available-=costs[i];}
            }
            Current=(float)((double)available*1.401298464324817e-45d);
            return paid;
        }

        public void Advance(float seconds) => AdvanceWithMotion(seconds, false);

        public void AdvanceWithMotion(float seconds, bool stationary)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;
            double charge=(RegenerationPerSecond+(double)regeneratorCount*RegenerationPerModule)*regenerationMultiplier*(stationary?4d:1d)*seconds;
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
        public const float BasicShot=0f, OtherConventionalShot=2.5f, Shotgun=5f, Railgun=9f, Rocket=7.5f, Emp=12.5f;
        public static float ConventionalShot(RoboRougelike.R4.R4WeaponKind weapon) =>
            weapon==RoboRougelike.R4.R4WeaponKind.Basic?BasicShot:OtherConventionalShot;
        public const float ContinuousWeaponPerSecond=6f, BoosterActivation=7.5f;
        public const float OmniDrivePerSecond=2.5f, HoverDrivePerSecond=1f;
        public const float Jump=12.5f, Blink=15f, Grapple=6f;
    }
}
