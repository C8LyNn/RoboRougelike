using System.Collections.Generic;
using System.Linq;
using RoboRougelike.R1;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R3
{
    // A separate active 1x1x1 device. Numbers are provisional tuning values.
    // The existing passive field-repair bonus remains independent.
    public sealed class R3RepairPulse : MonoBehaviour
    {
        public const float EnergyCost=15f, Duration=5f, Cooldown=15f, RepairRatioPerSecond=.05f;
        sealed class Pulse
        {
            public readonly List<R3MountedPart> Devices=new List<R3MountedPart>();
            public readonly List<R3MountedPart> Candidates=new List<R3MountedPart>();
            public R3MountedPart Target,ProgressTarget;
            public float Remaining,ObservedHp,ObservedMax;
            public double PreciseRatio;
        }
        readonly List<Pulse> active=new List<Pulse>();
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        public int ActiveCount=>active.Count;
        public string Status
        {
            get
            {
                if(assembly==null||energy==null)return "niedostępny";
                foreach(var pulse in active)foreach(var device in pulse.Devices)if(ReadyHardware(device))return "naprawia";
                float totalCost=0;bool available=false;
                foreach(var device in assembly.Parts)
                {
                    if(!ReadyHardware(device))continue;
                    var state=FindState(device.Id);
                    if(state!=null){available=true;totalCost+=EnergyCost*R3VehicleModel.TierActivationCost(state.Tier);}
                }
                if(!available)return "nieaktywny";
                float remaining=SharedCooldownRemaining();
                if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
                bool damaged=false;
                foreach(var part in assembly.Parts)if(Damaged(part)){damaged=true;break;}
                if(!damaged)return "brak uszkodzeń";
                return energy.CanSpend(totalCost)?"gotowy":"brak energii";
            }
        }
        public void Configure(R3VehicleAssembly owner){assembly=owner;energy=GetComponent<VehicleEnergyBank>();}
        bool Live(R3MountedPart part)=>part!=null&&part.gameObject.activeInHierarchy&&part.Operational&&part.transform.IsChildOf(transform);
        bool ReadyHardware(R3MountedPart part)=>Live(part)&&part.Kind==R3PartKind.RepairPulse&&
            !RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject);
        R3PartState FindState(string id)
        {
            foreach(var state in assembly.Model.Parts)if(state.Id==id)return state;
            return null;
        }
        float SharedCooldownRemaining()
        {
            float remaining=Mathf.Max(0,assembly.Model.RepairPulseReadyAt-Time.time);
            foreach(var state in assembly.Model.Parts)
                if(state.Kind==R3PartKind.RepairPulse)remaining=Mathf.Max(remaining,state.DeviceReadyAt-Time.time);
            return remaining;
        }
        bool Damaged(R3MountedPart part)=>Live(part)&&part.Damageable.MaxHp>0&&
            part.Damageable.CurrentHp<part.Damageable.MaxHp;
        R3MountedPart RandomTarget()
        {
            var candidates=assembly.Parts.Where(Damaged).ToArray();
            return candidates.Length==0?null:candidates[Random.Range(0,candidates.Length)];
        }
        R3MountedPart NextTarget(Pulse pulse)
        {
            while(true)
            {
                if(pulse.Candidates.Count==0)
                {
                    // Re-scan when exhausted so callbacks that damaged a new
                    // part during this step do not make that repair disappear.
                    foreach(var part in assembly.Parts)if(Damaged(part))pulse.Candidates.Add(part);
                    if(pulse.Candidates.Count==0)return null;
                }
                int index=Random.Range(0,pulse.Candidates.Count),last=pulse.Candidates.Count-1;
                var selected=pulse.Candidates[index];
                pulse.Candidates[index]=pulse.Candidates[last];pulse.Candidates.RemoveAt(last);
                if(Damaged(selected))return selected;
            }
        }
        public int TryActivate()
        {
            var run=R1RunController.Instance;
            if(assembly==null||energy==null||run==null||run.Player==null||run.Player.gameObject!=gameObject||!run.CanUseMovement)return 0;
            if(active.Count>0||SharedCooldownRemaining()>0)return 0;
            var combined=new Pulse{Target=RandomTarget(),Remaining=Duration};
            if(combined.Target==null)return 0;
            float totalCost=0;
            foreach(var module in assembly.Parts)
            {
                if(!ReadyHardware(module))continue;
                var state=FindState(module.Id);if(state==null)continue;
                combined.Devices.Add(module);totalCost+=EnergyCost*R3VehicleModel.TierActivationCost(state.Tier);
            }
            if(combined.Devices.Count==0||!energy.TrySpend(totalCost))return 0;
            assembly.Model.RepairPulseReadyAt=Time.time+Cooldown;
            foreach(var state in assembly.Model.Parts)
                if(state.Kind==R3PartKind.RepairPulse)state.DeviceReadyAt=assembly.Model.RepairPulseReadyAt;
            active.Add(combined);return combined.Devices.Count;
        }
        void Update()
        {
            var run=R1RunController.Instance;
            if(run==null||run.Player==null||run.Player.gameObject!=gameObject||!run.CanUseMovement)return;
            Tick(Time.deltaTime);
        }
        void Tick(float delta)
        {
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0||assembly==null)return;
            for(int i=active.Count-1;i>=0;i--)
            {
                var pulse=active[i];
                int units=0;foreach(var device in pulse.Devices)if(ReadyHardware(device))units+=R3VehicleModel.TierEffectUnits(device.Tier);
                if(units==0){active.RemoveAt(i);continue;}
                double frame=System.Math.Min(delta,pulse.Remaining);pulse.Remaining-=(float)frame;
                pulse.Candidates.Clear();
                while(frame>0d)
                {
                    if(!Damaged(pulse.Target))pulse.Target=NextTarget(pulse);
                    if(pulse.Target==null)break;
                    var hp=pulse.Target.Damageable;
                    float bonus=GetComponent<R3MountedBonusEffects>()?.RepairEfficiencyMultiplier??1f;
                    double rate=(double)RepairRatioPerSecond*units*bonus;
                    if(rate<=0d||double.IsNaN(rate)||double.IsInfinity(rate))break;
                    double actual=hp.CurrentHp/(double)hp.MaxHp;
                    if(pulse.ProgressTarget!=pulse.Target)
                    {pulse.ProgressTarget=pulse.Target;pulse.PreciseRatio=actual;}
                    else if(pulse.ObservedHp!=hp.CurrentHp||pulse.ObservedMax!=hp.MaxHp)
                    {
                        double remainder=pulse.ObservedMax>0?pulse.PreciseRatio-pulse.ObservedHp/(double)pulse.ObservedMax:0d;
                        pulse.PreciseRatio=System.Math.Max(0d,System.Math.Min(1d,actual+remainder));
                    }
                    double ratio=pulse.PreciseRatio;
                    double needed=(1d-ratio)/rate;
                    bool complete=needed<=frame;
                    float before=hp.CurrentHp;
                    pulse.PreciseRatio=complete?1d:System.Math.Min(1d,ratio+frame*rate);
                    hp.RestoreHealthRatio((float)pulse.PreciseRatio);
                    pulse.ObservedHp=hp.CurrentHp;pulse.ObservedMax=hp.MaxHp;
                    if(!complete)break; // Retain sub-float progress for the next step.
                    if(hp.CurrentHp<=before)break;
                    frame-=needed;pulse.Target=null;pulse.ProgressTarget=null;
                }
                if(pulse.Remaining<=0)active.RemoveAt(i);
            }
        }
    }
}
