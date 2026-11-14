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
            var damage=new R1ModifierGroup();var basicDamage=new R1ModifierGroup();var mobility=new R1ModifierGroup();var hull=new R1ModifierGroup();
            var core=new R1ModifierGroup();var reload=new R1ModifierGroup();
            damage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            basicDamage.Add(R1ModifierOperation.Increased,overdriveIncrease,1);
            reload.Add(R1ModifierOperation.Increased,rapidFireReduction,1,true);
            foreach(var module in active)
                if(module.UpgradeEffect==R1UpgradeEffect.Damage)
                {
                    if(module.UpgradeScope==R1BonusScope.Global)
                        damage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                    if(module.UpgradeScope==R1BonusScope.Global||module.UpgradeScope==R1BonusScope.BasicCannon)
                        basicDamage.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                }
                else if(module.UpgradeEffect==R1UpgradeEffect.Mobility)
                    mobility.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Hull)
                    hull.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Core)
                    core.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier));
                else if(module.UpgradeEffect==R1UpgradeEffect.Reload)
                    reload.Add(module.UpgradeOperation,module.UpgradeAmount,R3VehicleModel.TierEffectUnits(module.Tier),true);
            stats.ApplyBonusGroups(hull,damage,reload,mobility,core);
            EffectiveStats=stats;
            player.Apply(stats);
            player.Controller.SetBasicDamageModifier(basicDamage.Apply(baseStats.Damage));
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
