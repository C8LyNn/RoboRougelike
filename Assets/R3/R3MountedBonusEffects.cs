using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R1;

namespace RoboRougelike.R3
{
    /// <summary>
    /// Resolves only actually mounted and operational bonus modules.
    /// Losing or detaching a module removes its effect in the same run.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class R3MountedBonusEffects : MonoBehaviour
    {
        private R3VehicleAssembly assembly;
        private R1VehicleState player;
        private R1RunStats baseStats;
        private readonly List<R3MountedPart> active=new List<R3MountedPart>();
        private readonly List<R3MountedPart> nextActive=new List<R3MountedPart>();
        private readonly List<BonusSnapshot> snapshot=new List<BonusSnapshot>();
        private static readonly System.Comparison<R3MountedPart> CompareIds=
            (a,b)=>Comparer<string>.Default.Compare(a.Id,b.Id);
        private readonly struct BonusSnapshot
        {
            readonly R3MountedPart part;
            readonly string id;
            readonly int tier;
            readonly R1UpgradeEffect effect;
            readonly R1ModifierOperation operation;
            readonly R1BonusScope scope;
            readonly float amount;
            public BonusSnapshot(R3MountedPart p)
            {part=p;id=p.Id;tier=p.Tier;effect=p.UpgradeEffect;operation=p.UpgradeOperation;scope=p.UpgradeScope;amount=p.UpgradeAmount;}
            public bool Matches(R3MountedPart p)=>ReferenceEquals(part,p)&&id==p.Id&&tier==p.Tier&&
                effect==p.UpgradeEffect&&operation==p.UpgradeOperation&&scope==p.UpgradeScope&&amount.Equals(p.UpgradeAmount);
        }
        private float lastRepairPulse;
        private float overdriveIncrease;
        private float rapidFireReduction;
        private R3DriveKind lastDrive;
        public R1RunStats EffectiveStats {get;private set;}
        public float DroneDamageMultiplier {get;private set;}=1;
        public float TurretDamageMultiplier {get;private set;}=1;
        public float TrapDamageMultiplier {get;private set;}=1;
        public float BlastDamageMultiplier {get;private set;}=1;
        public float BlastTrapDamageMultiplier {get;private set;}=1;
        public float PiercingDamageMultiplier {get;private set;}=1;
        public float KineticDamageMultiplier {get;private set;}=1;
        public float AutomaticDamageMultiplier {get;private set;}=1;
        public float HeavyDamageMultiplier {get;private set;}=1;
        public float ShotgunDamageMultiplier {get;private set;}=1;
        public float RailgunDamageMultiplier {get;private set;}=1;
        public float RocketDamageMultiplier {get;private set;}=1;
        public float BeamDamageMultiplier {get;private set;}=1;
        public float SawDamageMultiplier {get;private set;}=1;
        public float AuraDamageMultiplier {get;private set;}=1;
        public float EnergyDamageMultiplier {get;private set;}=1;
        public float WeaponDamageMultiplier {get;private set;}=1;
        public float ShieldDurabilityMultiplier {get;private set;}=1;
        public float RepairEfficiencyMultiplier {get;private set;}=1;
        public float ConstructionHullMultiplier {get;private set;}=1;
        public float DriveMobilityMultiplier {get;private set;}=1;

        // Saturation is only the representable numeric limit, not a balance cap.
        public static float SafeDamageProduct(float amount,float multiplier)
        {
            if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0||
               float.IsNaN(multiplier)||float.IsInfinity(multiplier)||multiplier<=0)return 0;
            return (float)System.Math.Min(float.MaxValue,(double)amount*multiplier);
        }
        public static float SafeDamageProduct(float amount,float multiplier,float units,float seconds=1f)
        {
            if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0||
               float.IsNaN(multiplier)||float.IsInfinity(multiplier)||multiplier<=0||
               float.IsNaN(units)||float.IsInfinity(units)||units<=0||
               float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return 0;
            return (float)System.Math.Min(float.MaxValue,(double)amount*multiplier*units*seconds);
        }

        // Keep finite, non-negative cooldowns representable without a gameplay cap.
        // Preserve invalid inputs for each device's existing validation policy.
        public static float SafeIntervalProduct(float seconds,float multiplier)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||float.IsNaN(multiplier)||float.IsInfinity(multiplier))
                return seconds*multiplier;
            double product=(double)seconds*multiplier;
            return product>float.MaxValue?float.MaxValue:(float)product;
        }

        // Resolve global and matching category bonuses for owned deployables;
        // weapon-specific bonuses remain on the weapon path.
        public static float ScaleOwnedDamage(Transform owner,double amount,R1BonusScope category=R1BonusScope.Global,bool blast=false)
        {
            if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<=0)return 0;
            var effects=owner!=null?owner.GetComponentInParent<R3MountedBonusEffects>():null;
            float factor=effects==null?1f:category switch {
                R1BonusScope.Drones=>effects.DroneDamageMultiplier,R1BonusScope.Turrets=>effects.TurretDamageMultiplier,
                R1BonusScope.Traps=>blast?effects.BlastTrapDamageMultiplier:effects.TrapDamageMultiplier,
                _=>blast?effects.BlastDamageMultiplier:effects.EffectiveStats?.Damage??1f};
            if(float.IsNaN(factor)||float.IsInfinity(factor)||factor<=0)return 0;
            return (float)System.Math.Min(float.MaxValue,amount*factor);
        }

        public void SetOverdriveIncrease(float increase)
        {
            if(float.IsNaN(increase)||float.IsInfinity(increase)||increase<0)increase=0;
            if(overdriveIncrease==increase)return;
            overdriveIncrease=increase;Refresh(true);
        }
        public void SetRapidFireReduction(float reduction)
        {
            if(float.IsNaN(reduction)||float.IsInfinity(reduction)||reduction<0)reduction=0;
            if(rapidFireReduction==reduction)return;
            rapidFireReduction=reduction;Refresh(true);
        }

        public void Configure(R3VehicleAssembly owner,R1VehicleState vehicle,R1RunStats baseline)
        {
            assembly=owner;player=vehicle;baseStats=(baseline??new R1RunStats()).Copy();
            snapshot.Clear();lastRepairPulse=Time.time;
            Refresh(true);
        }
        private void Update()
        {
            if(assembly==null||player==null)return;
            Refresh(false);
            if(Time.time-lastRepairPulse>=0.25f)ApplyRepairPulse();
        }
        private void Refresh(bool force)
        {
            nextActive.Clear();
            foreach(var part in assembly.Parts)
                if(part!=null&&part.gameObject.activeInHierarchy&&part.Kind==R3PartKind.BonusModule&&part.Operational&&part.transform.IsChildOf(assembly.transform))
                    nextActive.Add(part);
            nextActive.Sort(CompareIds);
            bool unchanged=!force&&lastDrive==assembly.Model.SelectedDrive&&snapshot.Count==nextActive.Count;
            for(int i=0;unchanged&&i<nextActive.Count;i++)unchanged=snapshot[i].Matches(nextActive[i]);
            if(unchanged)return;
            lastDrive=assembly.Model.SelectedDrive;snapshot.Clear();active.Clear();
            foreach(var part in nextActive){active.Add(part);snapshot.Add(new BonusSnapshot(part));}
            var stats=baseStats.Copy();
            var damage=new R1ModifierGroup();var basicDamage=new R1ModifierGroup();var basicBlastDamage=new R1ModifierGroup();var droneDamage=new R1ModifierGroup();
            var turretDamage=new R1ModifierGroup();var trapDamage=new R1ModifierGroup();var blastDamage=new R1ModifierGroup();
            var blastTrapDamage=new R1ModifierGroup();var piercingDamage=new R1ModifierGroup();var kineticDamage=new R1ModifierGroup();var mobility=new R1ModifierGroup();var hull=new R1ModifierGroup();
            var core=new R1ModifierGroup();var reload=new R1ModifierGroup();var shieldDurability=new R1ModifierGroup();var weaponDamage=new R1ModifierGroup();var automaticDamage=new R1ModifierGroup();var energyDamage=new R1ModifierGroup();var repairEfficiency=new R1ModifierGroup();var constructionHull=new R1ModifierGroup();var driveMobility=new R1ModifierGroup();
            var heavyDamage=new R1ModifierGroup();var shotgunDamage=new R1ModifierGroup();var railgunDamage=new R1ModifierGroup();var rocketDamage=new R1ModifierGroup();
            var beamDamage=new R1ModifierGroup();var sawDamage=new R1ModifierGroup();var auraDamage=new R1ModifierGroup();
            damage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            weaponDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            automaticDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            heavyDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            shotgunDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            railgunDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            rocketDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            beamDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            sawDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            auraDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            energyDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            basicDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            basicBlastDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            droneDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            turretDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            trapDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            blastDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            blastTrapDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            piercingDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            kineticDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            reload.Add(R1ModifierOperation.Increased,rapidFireReduction,1,true);
            double hybridShare=0;
            foreach(var module in active)
                if(module.UpgradeEffect==R1UpgradeEffect.Damage)
                {
                    if(module.UpgradeScope==R1BonusScope.Global)
                        damage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons)
                        weaponDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.BasicCannon||module.UpgradeScope==R1BonusScope.KineticDamage)
                        basicDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.BasicCannon||module.UpgradeScope==R1BonusScope.BlastDamage)
                        basicBlastDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Drones||module.UpgradeScope==R1BonusScope.KineticDamage)
                        droneDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Turrets||module.UpgradeScope==R1BonusScope.KineticDamage)
                        turretDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Traps||module.UpgradeScope==R1BonusScope.KineticDamage)
                        trapDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.BlastDamage)
                        blastDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Traps||module.UpgradeScope==R1BonusScope.BlastDamage)
                        blastTrapDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.PiercingDamage)
                        piercingDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.KineticDamage)
                        kineticDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.KineticDamage||module.UpgradeScope==R1BonusScope.AutomaticWeapon)
                        automaticDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.HeavyWeapon,R1BonusScope.KineticDamage))
                        heavyDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.ShotgunWeapon,R1BonusScope.KineticDamage))
                        shotgunDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.RailgunWeapon,R1BonusScope.PiercingDamage))
                        railgunDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.RocketWeapon,R1BonusScope.BlastDamage))
                        rocketDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.BeamWeapon,R1BonusScope.EnergyDamage))
                        beamDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.SawWeapon,R1BonusScope.KineticDamage))
                        sawDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(AppliesToWeapon(module.UpgradeScope,R1BonusScope.AuraWeapon,R1BonusScope.EnergyDamage))
                        auraDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Weapons||module.UpgradeScope==R1BonusScope.EnergyDamage)
                        energyDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                }
                else if(module.UpgradeEffect==R1UpgradeEffect.Mobility)
                {
                    if(module.UpgradeScope==R1BonusScope.Global)mobility.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||MatchesDrive(module.UpgradeScope,lastDrive))
                        driveMobility.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                }
                else if(module.UpgradeEffect==R1UpgradeEffect.Hull)
                {
                    if(module.UpgradeScope==R1BonusScope.Global)hull.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Construction)
                        constructionHull.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                }
                else if(module.UpgradeEffect==R1UpgradeEffect.Core)
                    core.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Reload)
                    reload.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier),true);
                else if(module.UpgradeEffect==R1UpgradeEffect.ShieldDurability)
                    shieldDurability.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.RepairEfficiency)
                    repairEfficiency.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.HybridBlastRounds&&!float.IsNaN(module.UpgradeAmount)&&!float.IsInfinity(module.UpgradeAmount)&&module.UpgradeAmount>0)
                    hybridShare+=(double)module.UpgradeAmount*R3VehicleModel.TierEffectUnits(module.Tier);
            stats.ApplyBonusGroups(hull,damage,reload,mobility,core);
            EffectiveStats=stats;
            DroneDamageMultiplier=droneDamage.Apply(baseStats.Damage);
            TurretDamageMultiplier=turretDamage.Apply(baseStats.Damage);
            TrapDamageMultiplier=trapDamage.Apply(baseStats.Damage);
            BlastDamageMultiplier=blastDamage.Apply(baseStats.Damage);
            BlastTrapDamageMultiplier=blastTrapDamage.Apply(baseStats.Damage);
            PiercingDamageMultiplier=piercingDamage.Apply(baseStats.Damage);
            KineticDamageMultiplier=kineticDamage.Apply(baseStats.Damage);
            AutomaticDamageMultiplier=automaticDamage.Apply(baseStats.Damage);
            HeavyDamageMultiplier=heavyDamage.Apply(baseStats.Damage);
            ShotgunDamageMultiplier=shotgunDamage.Apply(baseStats.Damage);
            RailgunDamageMultiplier=railgunDamage.Apply(baseStats.Damage);
            RocketDamageMultiplier=rocketDamage.Apply(baseStats.Damage);
            BeamDamageMultiplier=beamDamage.Apply(baseStats.Damage);
            SawDamageMultiplier=sawDamage.Apply(baseStats.Damage);
            AuraDamageMultiplier=auraDamage.Apply(baseStats.Damage);
            EnergyDamageMultiplier=energyDamage.Apply(baseStats.Damage);
            WeaponDamageMultiplier=weaponDamage.Apply(baseStats.Damage);
            ShieldDurabilityMultiplier=shieldDurability.Apply(1);
            RepairEfficiencyMultiplier=repairEfficiency.Apply(1);
            ConstructionHullMultiplier=constructionHull.Apply(baseStats.Hull);
            DriveMobilityMultiplier=driveMobility.Apply(baseStats.Mobility);
            player.Apply(stats,ConstructionHullMultiplier,DriveMobilityMultiplier);
            player.Controller.SetBasicDamageProfile(basicDamage.Apply(baseStats.Damage),basicBlastDamage.Apply(baseStats.Damage),(float)System.Math.Min(1d,hybridShare));
        }
        private static bool AppliesToWeapon(R1BonusScope scope,R1BonusScope weapon,R1BonusScope damageType)=>
            scope==R1BonusScope.Global||scope==R1BonusScope.Weapons||scope==weapon||scope==damageType;
        private static bool MatchesDrive(R1BonusScope scope,R3DriveKind drive)=>
            (scope==R1BonusScope.WheelDrive&&drive==R3DriveKind.Wheels)||
            (scope==R1BonusScope.TrackDrive&&drive==R3DriveKind.Tracks)||
            (scope==R1BonusScope.OmniDrive&&drive==R3DriveKind.Omni)||
            (scope==R1BonusScope.HoverDrive&&drive==R3DriveKind.Hover);
        private void ApplyRepairPulse()
        {
            float elapsed=Mathf.Clamp(Time.time-lastRepairPulse,0,0.5f);
            lastRepairPulse=Time.time;
            // The legacy 'field repair' bonus had no actual stat implementation.
            // As a physical module it regenerates ALIVE connected parts while operational.
            int repairModules=active.Where(p=>p.UpgradeEffect==R1UpgradeEffect.Repair).Sum(p=>R3VehicleModel.TierEffectUnits(p.Tier));
            if(repairModules==0||elapsed<=0)return;
            var state=R1RunController.Instance?.Progress?.State;
            if(state!=R1RunState.Combat&&state!=R1RunState.TestDrive)return;
            float increase=(float)System.Math.Min(1d,repairModules*.025d*elapsed*RepairEfficiencyMultiplier);
            foreach(var piece in player.GetComponentsInChildren<P12PipelineSafeDamageable>())
            {
                if(piece==null||piece.IsDead||piece.MaxHp<=0)continue;
                if(piece.GetComponentInParent<R3MountedPart>()?.Kind==R3PartKind.BonusModule&&
                   piece.GetComponent<R3MountedPart>()==null)continue; // virtual footprint sockets
                float ratio=piece.CurrentHp/piece.MaxHp;
                if(ratio<1f)piece.RestoreHealthRatio(Mathf.Min(1,ratio+increase));
            }
        }
    }
}
