using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.BeamCutter;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLBeamBinding : MonoBehaviour,IBeamDamageSink
    {
        public RRLDeviceBinding Device {get;private set;}
        public R3PartState State=>Device.State;
        public BeamCutterModule Module {get;private set;}
        P12PipelineSafeTankController tank;
        float damageScale=1;
        bool held;
        public void Bind(R3PartState state,Transform owner,Transform muzzle,float damageMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            tank=owner.GetComponent<P12PipelineSafeTankController>();damageScale=damageMultiplier;
            Module=gameObject.AddComponent<BeamCutterModule>();Module.SetAutomaticSimulation(false);
            Module.Bind(Device.Body,muzzle,owner,this);Module.RestoreThermalState(state.ThermalHeat,state.ThermalLocked);
            Module.SetOwnColliderFilter(c=>!RRLHitRouter.CanHit(c,owner));
            var visual=new GameObject("WPN003 beam visual");visual.transform.SetParent(transform,false);
            var line=visual.AddComponent<LineRenderer>();line.sharedMaterial=GetComponent<Renderer>().sharedMaterial;
            line.startWidth=.065f;line.endWidth=.04f;Module.SetLineRenderer(line);
            Device.Damageable.Died+=Stop;Module.SetOperational(Device.Available);
        }
        public void SetTriggerHeld(bool value){held=value;if(!value&&Module!=null)Module.SetTriggerHeld(false);}
        void FixedUpdate()
        {
            bool available=Device.Available;Module.SetOperational(available);Module.SetAttached(available);
            Module.SetTriggerHeld(held&&available);Module.ManualFixedStep(Time.fixedDeltaTime);CaptureState();
        }
        public void CaptureState(){if(Module==null||Device?.State==null)return;State.ThermalHeat=Module.Heat;State.ThermalLocked=Module.IsOverheated;}
        public void ApplyBeamHit(in BeamHitRequest hit)
        {
            if(!Device.Available||hit.ShooterBody!=Device.Body)return;
            RRLHitRouter.Apply(hit.Collider,hit.Damage*(tank!=null?tank.PackageDamageScale:damageScale),hit.Point,hit.Direction,Device.Owner);
        }
        void Stop(){held=false;if(Module!=null){Module.SetTriggerHeld(false);Module.SetOperational(false);}CaptureState();}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
