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
        sealed class Pulse { public R3MountedPart Device,Target; public float Remaining; }
        readonly List<Pulse> active=new List<Pulse>();
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        public int ActiveCount=>active.Count;
        public string Status
        {
            get
            {
                if(assembly==null||energy==null)return "niedostępny";
                foreach(var pulse in active)if(Live(pulse.Device))return "naprawia";
                float remaining=float.PositiveInfinity;
                foreach(var device in assembly.Parts)
                {
                    if(!ReadyHardware(device))continue;
                    var state=FindState(device.Id);
                    if(state!=null)remaining=Mathf.Min(remaining,Mathf.Max(0,state.DeviceReadyAt-Time.time));
                }
                if(float.IsPositiveInfinity(remaining))return "nieaktywny";
                if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
                bool damaged=false;
                foreach(var part in assembly.Parts)if(Damaged(part)){damaged=true;break;}
                if(!damaged)return "brak uszkodzeń";
                foreach(var device in assembly.Parts)
                {
                    if(!ReadyHardware(device))continue;
                    var state=FindState(device.Id);
                    if(state!=null&&state.DeviceReadyAt<=Time.time&&
                        energy.CanSpend(EnergyCost*R3VehicleModel.TierActivationCost(state.Tier)))return "gotowy";
                }
                return "brak energii";
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
        bool Damaged(R3MountedPart part)=>Live(part)&&part.Damageable.MaxHp>0&&
            part.Damageable.CurrentHp<part.Damageable.MaxHp-.001f;
        R3MountedPart RandomTarget()
        {
            var candidates=assembly.Parts.Where(Damaged).ToArray();
            return candidates.Length==0?null:candidates[Random.Range(0,candidates.Length)];
        }
        public int TryActivate()
        {
            var run=R1RunController.Instance;
            if(assembly==null||energy==null||run==null||run.Player==null||run.Player.gameObject!=gameObject||!run.CanUseMovement)return 0;
            int count=0;
            foreach(var module in assembly.Parts.Where(p=>Live(p)&&p.Kind==R3PartKind.RepairPulse))
            {
                var state=assembly.Model.Parts.FirstOrDefault(p=>p.Id==module.Id);
                if(state==null||state.DeviceReadyAt>Time.time||active.Any(p=>p.Device==module)||
                    RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(module.gameObject))continue;
                var target=RandomTarget();
                if(target==null||!energy.TrySpend(EnergyCost*R3VehicleModel.TierActivationCost(state.Tier)))continue;
                state.DeviceReadyAt=Time.time+Cooldown;
                active.Add(new Pulse{Device=module,Target=target,Remaining=Duration});count++;
            }
            return count;
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
                if(!Live(pulse.Device)||RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(pulse.Device.gameObject))
                {active.RemoveAt(i);continue;}
                float frame=Mathf.Min(delta,pulse.Remaining);pulse.Remaining-=frame;
                while(frame>.00001f)
                {
                    if(!Damaged(pulse.Target))pulse.Target=RandomTarget();
                    if(pulse.Target==null)break;
                    var hp=pulse.Target.Damageable;
                    float ratio=hp.CurrentHp/hp.MaxHp;
                    float rate=RepairRatioPerSecond*R3VehicleModel.TierEffectUnits(pulse.Device.Tier);
                    float needed=(1f-ratio)/rate;
                    float used=Mathf.Min(frame,needed);
                    hp.RestoreHealthRatio(Mathf.Min(1f,ratio+used*rate));
                    frame-=used;
                    if(used<=.000001f){pulse.Target=null;break;}
                }
                if(pulse.Remaining<=0)active.RemoveAt(i);
            }
        }
    }
}
