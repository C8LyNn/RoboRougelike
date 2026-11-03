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
        Transform blade,rotor;bool requested;
        P12PipelineSafeTankController tank;float damageScale=1;
        public void Bind(R3PartState state,Transform owner,Transform center,float damageMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            blade=center;tank=owner.GetComponent<P12PipelineSafeTankController>();damageScale=damageMultiplier;
            // Enemy-only geometric clearance. Preserve the actual player's garage placement;
            // an illegally mounted player saw must remain physically blocked.
            if(owner.GetComponent<P12PipelineSafeEnemyTurret>()!=null)
                PlaceEnemySawBeyondHull(owner,center);
            Module=gameObject.AddComponent<RotarySawModule>();Module.SetAutomaticSimulation(false);
            Module.Bind(Device.Body,owner,center);Module.SetDamageSink(this);
            foreach(var collider in owner.GetComponentsInChildren<Collider>())Module.RegisterOwnerCollider(collider);
            Module.Motor.RestoreState(state.ThermalHeat,state.ThermalLocked,Mathf.Max(0,state.DeviceReadyAt-Time.time));
            rotor=RoboRougelike.R4_1.R41PartGeometry.SawRotor(center,GetComponent<Renderer>().sharedMaterial);
            Device.Damageable.Died+=Stop;Module.SetOperational(Device.Available);
        }
        // Runtime enemy prefabs previously mount the saw in the middle of their own armored hull.
        // Keep the disc outside ALL existing chassis colliders across yaw changes, rather than
        // exempting friendly armor from the module's real collision guard. No health/ID edits.
        private static void PlaceEnemySawBeyondHull(Transform owner,Transform center)
        {
            if(center==null || center.parent==null)return;
            Transform yaw=center.parent.parent;
            if(yaw==null || yaw.name!="Enemy_Yaw")return;
            Vector3 forward=Vector3.ProjectOnPlane(yaw.forward,Vector3.up);
            if(forward.sqrMagnitude<.00001f)return;
            forward.Normalize();
            float furthestRadius=0f;
            foreach(var collider in owner.GetComponentsInChildren<Collider>(true))
            {
                if(collider==null||!collider.enabled||!collider.gameObject.activeInHierarchy||
                   collider.transform.IsChildOf(center))continue;
                Bounds bounds=collider.bounds;
                Vector3 delta=bounds.center-yaw.position;
                float radial=new Vector2(delta.x,delta.z).magnitude+
                             new Vector2(bounds.extents.x,bounds.extents.z).magnitude;
                furthestRadius=Mathf.Max(furthestRadius,radial);
            }
            float current=Vector3.ProjectOnPlane(center.position-yaw.position,Vector3.up).magnitude;
            float radius=new RotarySawConfiguration().bladeRadius;
            float extra=Mathf.Max(0f,furthestRadius+radius+.06f-current);
            if(extra>0f)center.position+=forward*extra;
        }
                public void SetMotorRequested(bool value){requested=value;if(!value&&Module!=null)Module.SetMotorRequested(false);}
        void FixedUpdate(){bool active=Device.Available;Module.SetOperational(active);Module.SetMotorRequested(active&&requested);Module.Advance(Time.fixedDeltaTime);CaptureState();}
        void LateUpdate(){if(rotor!=null)rotor.Rotate(Vector3.up,Module.Motor.Rpm*6*Time.deltaTime,Space.Self);}
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
            RRLHitRouter.Apply(hit.contactCollider,hit.damage*(tank!=null?tank.PackageDamageScale:damageScale),hit.worldPoint,direction,Device.Owner);
        }
        void Stop(){requested=false;if(Module!=null)Module.SetOperational(false);CaptureState();}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
