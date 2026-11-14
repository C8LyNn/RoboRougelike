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
        public R1RunStats EffectiveStats {get;private set;}
        public float DroneDamageMultiplier {get;private set;}=1;
        public float TurretDamageMultiplier {get;private set;}=1;
        public float TrapDamageMultiplier {get;private set;}=1;
        public float BlastDamageMultiplier {get;private set;}=1;
        public float BlastTrapDamageMultiplier {get;private set;}=1;
        public float PiercingDamageMultiplier {get;private set;}=1;
        public float KineticDamageMultiplier {get;private set;}=1;
        public float ShieldDurabilityMultiplier {get;private set;}=1;

        // Resolve global and matching category bonuses for owned deployables;
        // weapon-specific bonuses remain on the weapon path.
        public static float ScaleOwnedDamage(Transform owner,float amount,R1BonusScope category=R1BonusScope.Global,bool blast=false)
        {
            if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0)return 0;
            var effects=owner!=null?owner.GetComponentInParent<R3MountedBonusEffects>():null;
            float factor=effects==null?1f:category switch {
                R1BonusScope.Drones=>effects.DroneDamageMultiplier,R1BonusScope.Turrets=>effects.TurretDamageMultiplier,
                R1BonusScope.Traps=>blast?effects.BlastTrapDamageMultiplier:effects.TrapDamageMultiplier,
                _=>blast?effects.BlastDamageMultiplier:effects.EffectiveStats?.Damage??1f};
            return (float)System.Math.Min(float.MaxValue,(double)amount*factor);
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
            bool unchanged=!force&&snapshot.Count==nextActive.Count;
            for(int i=0;unchanged&&i<nextActive.Count;i++)unchanged=snapshot[i].Matches(nextActive[i]);
            if(unchanged)return;
            snapshot.Clear();active.Clear();
            foreach(var part in nextActive){active.Add(part);snapshot.Add(new BonusSnapshot(part));}
            var stats=baseStats.Copy();
            var damage=new R1ModifierGroup();var basicDamage=new R1ModifierGroup();var basicBlastDamage=new R1ModifierGroup();var droneDamage=new R1ModifierGroup();
            var turretDamage=new R1ModifierGroup();var trapDamage=new R1ModifierGroup();var blastDamage=new R1ModifierGroup();
            var blastTrapDamage=new R1ModifierGroup();var piercingDamage=new R1ModifierGroup();var kineticDamage=new R1ModifierGroup();var mobility=new R1ModifierGroup();var hull=new R1ModifierGroup();
            var core=new R1ModifierGroup();var reload=new R1ModifierGroup();var shieldDurability=new R1ModifierGroup();
            damage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
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
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.BasicCannon||module.UpgradeScope==R1BonusScope.KineticDamage)
                        basicDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.BasicCannon||module.UpgradeScope==R1BonusScope.BlastDamage)
                        basicBlastDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Drones||module.UpgradeScope==R1BonusScope.KineticDamage)
                        droneDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Turrets||module.UpgradeScope==R1BonusScope.KineticDamage)
                        turretDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Traps||module.UpgradeScope==R1BonusScope.KineticDamage)
                        trapDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.BlastDamage)
                        blastDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.Traps||module.UpgradeScope==R1BonusScope.BlastDamage)
                        blastTrapDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.PiercingDamage)
                        piercingDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.KineticDamage)
                        kineticDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                }
                else if(module.UpgradeEffect==R1UpgradeEffect.Mobility)
                    mobility.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Hull)
                    hull.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Core)
                    core.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Reload)
                    reload.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier),true);
                else if(module.UpgradeEffect==R1UpgradeEffect.ShieldDurability)
                    shieldDurability.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
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
            ShieldDurabilityMultiplier=shieldDurability.Apply(1);
            player.Apply(stats);
            player.Controller.SetBasicDamageProfile(basicDamage.Apply(baseStats.Damage),basicBlastDamage.Apply(baseStats.Damage),(float)System.Math.Min(1d,hybridShare));
        }
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
            float increase=repairModules*0.025f*elapsed;
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
