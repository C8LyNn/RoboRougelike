using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.BeamCutter;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLBeamBinding : MonoBehaviour,IRRLDeviceLifecycle,IBeamDamageSink
    {
        public void RefreshAvailability(){if(Module!=null){Module.SetOperational(Device.Available);Module.SetAttached(Device.Available);}}
        public RRLDeviceBinding Device {get;private set;}
        public R3PartState State=>Device.State;
        public BeamCutterModule Module {get;private set;}
        P12PipelineSafeTankController tank;
        float damageScale=1;
        bool held;
        Transform muzzle;
        LineRenderer beamLine;
        bool intercepted;
        Vector3 interceptionPoint;
        public void Bind(R3PartState state,Transform owner,Transform muzzle,float damageMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            tank=owner.GetComponent<P12PipelineSafeTankController>();damageScale=damageMultiplier;this.muzzle=muzzle;
            Module=gameObject.AddComponent<BeamCutterModule>();Module.SetAutomaticSimulation(false);
            Module.Bind(Device.Body,muzzle,owner,this);Module.RestoreThermalState(state.ThermalHeat,state.ThermalLocked);
            var energy=owner.GetComponent<VehicleEnergyBank>();
            if(energy!=null){Module.Configure(new BeamCutterSettings{energyPerSecond=VehicleEnergyCosts.ContinuousWeaponPerSecond*R3VehicleModel.TierActivationCost(state.Tier)});Module.RestoreThermalState(state.ThermalHeat,state.ThermalLocked);Module.SetEnergySource(energy);}
            Module.SetOwnColliderFilter(c=>!RRLHitRouter.CanHit(c,owner));
            var visual=new GameObject("WPN003 beam visual");visual.transform.SetParent(transform,false);
            var line=visual.AddComponent<LineRenderer>();beamLine=line;line.sharedMaterial=GetComponent<Renderer>().sharedMaterial;
            line.startWidth=.065f;line.endWidth=.04f;Module.SetLineRenderer(line);
            Device.Damageable.Died+=Stop;Module.SetOperational(Device.Available);
        }
        public void SetTriggerHeld(bool value){held=value;if(!value&&Module!=null)Module.SetTriggerHeld(false);}
        void FixedUpdate()
        {
            bool available=Device.Available;Module.SetOperational(available);Module.SetAttached(available);
            bool canFire=held&&available;
            if(canFire)
            {
                var assembly=Device.Owner.GetComponent<R3VehicleAssembly>();
                var part=GetComponent<R3MountedPart>();
                if(assembly!=null)canFire=part!=null&&part.Muzzle!=null&&
                    assembly.ClearProjectilePath(part,part.Muzzle.forward,Module.Settings.rangeMeters,.001f);
            }
            intercepted=false;Module.SetTriggerHeld(canFire);var result=Module.ManualFixedStep(Time.fixedDeltaTime);
            if(result.Status==BeamStepStatus.Firing&&result.FirstHit==null&&muzzle!=null&&
                R3ShieldDome.TryFirstHit(muzzle.position,result.EndPoint,Device.Owner,out var shield,out float distance))
            {
                var direction=(result.EndPoint-muzzle.position).normalized;interceptionPoint=muzzle.position+direction*distance;
                shield.Absorb(Module.Settings.damagePerSecond*result.ActiveSeconds*(tank!=null?tank.PackageDamageScale:damageScale)*R3VehicleModel.TierEffectUnits(State.Tier),interceptionPoint,direction);intercepted=true;
            }
            if(intercepted&&beamLine!=null&&beamLine.positionCount>=2)beamLine.SetPosition(1,interceptionPoint);
            CaptureState();
        }
        public void CaptureState(){if(Module==null||Device?.State==null)return;State.ThermalHeat=Module.Heat;State.ThermalLocked=Module.IsOverheated;}
        public void ApplyBeamHit(in BeamHitRequest hit)
        {
            if(!Device.Available||hit.ShooterBody!=Device.Body)return;
            if(muzzle!=null&&R3ShieldDome.TryFirstHit(muzzle.position,hit.Point,Device.Owner,out var shield,out float distance))
            {
                var direction=(hit.Point-muzzle.position).normalized;interceptionPoint=muzzle.position+direction*distance;
                shield.Absorb(hit.Damage*(tank!=null?tank.PackageDamageScale:damageScale)*R3VehicleModel.TierEffectUnits(State.Tier),interceptionPoint,direction);intercepted=true;return;
            }
            RRLHitRouter.Apply(hit.Collider,hit.Damage*(tank!=null?tank.PackageDamageScale:damageScale)*R3VehicleModel.TierEffectUnits(State.Tier),hit.Point,hit.Direction,Device.Owner);
        }
        void Stop(){held=false;if(Module!=null){Module.SetTriggerHeld(false);Module.SetOperational(false);}CaptureState();}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
