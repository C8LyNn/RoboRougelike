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
        private string lastSignature;
        private float lastRepairPulse;
        public R1RunStats EffectiveStats {get;private set;}

        public void Configure(R3VehicleAssembly owner,R1VehicleState vehicle,R1RunStats baseline)
        {
            assembly=owner;player=vehicle;baseStats=(baseline??new R1RunStats()).Copy();
            lastSignature=null;lastRepairPulse=Time.time;
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
            var online=assembly.Parts.Where(p=>p!=null&&p.Kind==R3PartKind.BonusModule&&
                p.Operational&&p.transform.IsChildOf(assembly.transform)).OrderBy(p=>p.Id).ToArray();
            string signature=string.Join("|",online.Select(p=>p.Id));
            if(!force&&signature==lastSignature)return;
            lastSignature=signature;active.Clear();active.AddRange(online);
            var stats=baseStats.Copy();
            foreach(var module in active)stats.Apply(module.UpgradeEffect,module.UpgradeAmount);
            EffectiveStats=stats;
            player.Apply(stats);
        }
        private void ApplyRepairPulse()
        {
            float elapsed=Mathf.Clamp(Time.time-lastRepairPulse,0,0.5f);
            lastRepairPulse=Time.time;
            // The legacy 'field repair' bonus had no actual stat implementation.
            // As a physical module it regenerates ALIVE connected parts while operational.
            int repairModules=active.Count(p=>p.UpgradeEffect==R1UpgradeEffect.Repair);
            if(repairModules==0||elapsed<=0)return;
            var state=R1RunController.Instance?.Progress?.State;
            if(state!=R1RunState.Combat&&state!=R1RunState.TestDrive)return;
            float increase=Mathf.Min(0.08f,repairModules*0.025f)*elapsed;
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
