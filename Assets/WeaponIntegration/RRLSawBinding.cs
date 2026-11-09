using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Weapons.RotarySaw;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLSawBinding : MonoBehaviour,IRRLDeviceLifecycle,IRotarySawDamageSink
    {
        public void RefreshAvailability(){if(Module!=null)Module.SetOperational(Device.Available);}
        public RRLDeviceBinding Device {get;private set;}
        public RotarySawModule Module {get;private set;}
        public R3PartState State=>Device.State;
        Transform blade,rotor;bool requested,artRotor;
        P12PipelineSafeTankController tank;float damageScale=1;
        public static RotarySawConfiguration Geometry(R3PartState state)
        {
            var config=new RotarySawConfiguration();
            if(state!=null&&state.Kind==R3PartKind.Gun&&state.Weapon==RoboRougelike.R4.R4WeaponKind.RotarySaw&&state.FootprintRevision==2)
            {config.bladeRadius*=2;config.bladeHalfThickness*=2;config.hubDeadZone*=2;}
            return config;
        }
        public void Bind(R3PartState state,Transform owner,Transform center,float damageMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            blade=center;tank=owner.GetComponent<P12PipelineSafeTankController>();damageScale=damageMultiplier;
            Module=gameObject.AddComponent<RotarySawModule>();Module.SetAutomaticSimulation(false);
            Module.Bind(Device.Body,owner,center);Module.SetDamageSink(this);
            var energy=owner.GetComponent<VehicleEnergyBank>();
            var config=Geometry(state);
            if(energy!=null)config.energyPerSecond=VehicleEnergyCosts.ContinuousWeaponPerSecond*R3VehicleModel.TierActivationCost(state.Tier);
            Module.Configure(config);if(energy!=null)Module.SetEnergySource(energy);
            foreach(var collider in owner.GetComponentsInChildren<Collider>())Module.RegisterOwnerCollider(collider);
            Module.Motor.RestoreState(state.ThermalHeat,state.ThermalLocked,Mathf.Max(0,state.DeviceReadyAt-Time.time));
            // The previous visual may be inactive until Unity destroys it at
            // frame end. Bind only the active replacement, not that old model.
            var art=GetComponentInChildren<RoboRougelike.R4_1.R4WeaponArt>();
            rotor=art!=null?art.SawRotor:null;artRotor=rotor!=null;
            if(rotor==null)rotor=RoboRougelike.R4_1.R41PartGeometry.SawRotor(center,GetComponent<Renderer>().sharedMaterial);
            Device.Damageable.Died+=Stop;Module.SetOperational(Device.Available);
        }
        public void SetMotorRequested(bool value){requested=value;if(!value&&Module!=null)Module.SetMotorRequested(false);}
        void FixedUpdate(){bool active=Device.Available;Module.SetOperational(active);Module.SetMotorRequested(active&&requested);Module.Advance(Time.fixedDeltaTime);CaptureState();}
        void LateUpdate(){if(rotor!=null){if(artRotor&&blade!=null)rotor.position=blade.position;
            rotor.Rotate(artRotor?Vector3.right:Vector3.up,Module.Motor.Rpm*6*Time.deltaTime,Space.Self);}}
        public void CaptureState(){if(Module==null||Device?.State==null)return;State.ThermalHeat=Module.Motor.Heat;State.ThermalLocked=Module.Motor.Overheated;State.DeviceReadyAt=Time.time+Module.Motor.JamRemaining;}
        public bool TryResolve(Collider collider,out SawTarget target)
        {
            target=default;if(!Device.Available||!RRLHitRouter.CanHit(collider,Device.Owner))return false;
            var hp=collider.GetComponentInParent<P12PipelineSafeDamageable>();
            if(hp!=null){target=new SawTarget(hp.GetInstanceID(),Mathf.Clamp(hp.MaxHp/200f,.5f,4));return true;}
            var receiver=collider.GetComponentInParent<IRRLHitReceiver>() as Component;
            target=new SawTarget(receiver!=null?receiver.GetInstanceID():collider.GetInstanceID(),receiver!=null?2:4);return true;
        }
        public void ApplyDamage(SawDamageEvent hit)
        {
            if(!TryResolve(hit.contactCollider,out var part)||part.partId!=hit.partId)return;
            var direction=(hit.worldPoint-blade.position).normalized;
            RRLHitRouter.Apply(hit.contactCollider,hit.damage*(tank!=null?tank.PackageDamageScale:damageScale)*R3VehicleModel.TierEffectUnits(State.Tier),hit.worldPoint,direction,Device.Owner);
        }
        void Stop(){requested=false;if(Module!=null)Module.SetOperational(false);CaptureState();}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
