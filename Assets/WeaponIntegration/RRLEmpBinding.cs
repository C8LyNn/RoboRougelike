using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLEmpBinding : MonoBehaviour,IRRLDeviceLifecycle
    {
        public RRLDeviceBinding Device {get;private set;}
        public R3PartState State=>Device.State;
        public EmpEmitter Module {get;private set;}
        public EmpPulseReport LastReport {get;private set;}
        P12PipelineSafeTankController tank;float intervalScale=1;
        public void Bind(R3PartState state,Transform owner,float cadence=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            tank=owner.GetComponent<P12PipelineSafeTankController>();intervalScale=cadence;
            Module=gameObject.AddComponent<EmpEmitter>();Module.SetAutomaticSimulation(false);Module.BindVehicle(owner,owner.GetInstanceID(),RRLHitRouter.Team(owner));Module.SetOrigin(transform);
            var energy=owner.GetComponent<VehicleEnergyBank>();if(energy!=null)Module.SetEnergySource(energy);
            // Direct callers of Module.TryPulse must not bypass R3's current body/HP/team gate.
            // Check live state at pulse time, including destruction/detachment before Update().
            Module.SetActivationGate(() => Device != null && Device.Available &&
                Device.Owner != null && transform.IsChildOf(Device.Owner) &&
                !RRLEmpPartState.IsBlocked(gameObject));
            Module.RestoreCooldown(Mathf.Max(0,state.DeviceReadyAt-Time.time));Device.Damageable.Died+=Stop;RefreshAvailability();
        }
        public void RefreshAvailability(){if(Module!=null)Module.SetOperational(Device.Available);}
        public bool Fire()
        {
            RefreshAvailability();if(!Device.Available||Time.time<State.DeviceReadyAt)return false;
            RRLEmpPartState.RefreshEligibility();Module.Configure(9,3*R3VehicleModel.TierEffectUnits(State.Tier),12*(tank!=null?tank.PackageIntervalScale:intervalScale),
                Device.Owner.GetComponent<VehicleEnergyBank>()!=null?VehicleEnergyCosts.Emp*R3VehicleModel.TierActivationCost(State.Tier):0f);
            if(!Module.TryPulse(out var report)){LastReport=report;return false;}
            LastReport=report;CaptureState();RoboRougelike.R6.R6CombatFeedback.Muzzle(transform,RoboRougelike.R4.R4WeaponKind.EmpEmitter);return true;
        }
        public void CaptureState(){if(Module!=null&&Device?.State!=null)State.DeviceReadyAt=Time.time+Module.CooldownRemaining;}
        void Update(){RefreshAvailability();Module.Advance(Time.deltaTime);CaptureState();}
        void Stop(){if(Module!=null)Module.SetOperational(false);CaptureState();}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
