using System.Linq;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.R6
{
    // The solver owns velocity and position. Controllers request bounded forces only.
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class R6VehicleDrive : MonoBehaviour
    {
        Rigidbody body;
        P12PipelineSafeDamageable[] parts;
        PhysicsMaterial material;
        float nextMassRefresh, traction, left, right, track;
        public int Wheels {get;private set;}
        public bool SuspensionPhysics {get;private set;}
        public void EnableSuspensionPhysics(float contactFriction)
        {
            SuspensionPhysics=true;body.useGravity=true;body.constraints=RigidbodyConstraints.None;
            // These compound wheel colliders slide; traction is supplied by bounded drive forces.
            // R6's planar mode never loaded them with vehicle weight.
            material.staticFriction=material.dynamicFriction=Mathf.Clamp(contactFriction,0,R6Tuning.Current.ContactFriction);
        }
        public float AvailableForce => traction*R6Tuning.Current.WheelForce;
        public float ForwardSpeed => Vector3.Dot(body.linearVelocity,transform.forward);
        public float WheelImbalance => (right-left)/Mathf.Max(1,right+left);

        void Awake()
        {
            body=GetComponent<Rigidbody>();body.isKinematic=false;body.useGravity=false;
            body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.maxDepenetrationVelocity=R6Tuning.Current.MaxDepenetrationSpeed;
            body.maxAngularVelocity=3;body.linearDamping=0;body.angularDamping=.1f;
            material=new PhysicsMaterial("Kontakt pojazdu R6") {bounciness=0,dynamicFriction=R6Tuning.Current.ContactFriction,
                staticFriction=R6Tuning.Current.ContactFriction,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            foreach(var col in GetComponentsInChildren<Collider>())col.sharedMaterial=material;
            parts=GetComponentsInChildren<P12PipelineSafeDamageable>();RefreshMass();
        }
        public static float PartMass(P12PipelineSafeDamageable part)
        {
            var device=part.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>();
            if(device?.State!=null)return R4WeaponDefinition.PartMass(device.State);
            var mounted=part.GetComponent<R3MountedPart>();
            if(mounted!=null)return R4WeaponDefinition.PartMass(mounted.Kind,mounted.Weapon);
            var module=part.GetComponent<P12PipelineSafeFunctionalModule>();
            return module!=null&&module.ModuleType==P12FunctionalModuleType.Weapon?32:IsWheel(part)?12:8;
        }
        static bool IsWheel(P12PipelineSafeDamageable part)
        {
            var mounted=part.GetComponent<R3MountedPart>();
            if(mounted!=null)return mounted.Kind==R3PartKind.Wheel;
            var module=part.GetComponent<P12PipelineSafeFunctionalModule>();
            return module!=null&&module.ModuleType==P12FunctionalModuleType.Wheel||part.name.StartsWith("Koło");
        }
        bool BelongsToBody(P12PipelineSafeDamageable part)
        {
            if(part==null||part.IsDead||!part.transform.IsChildOf(transform)||part.GetComponentInParent<Rigidbody>()!=body)return false;
            var mounted=part.GetComponent<R3MountedPart>();return mounted==null||mounted.Operational;
        }
        public void RefreshMass()
        {
            if(parts==null)return;
            float mass=0,yaw=0;Vector3 center=Vector3.zero;
            traction=left=right=track=0;Wheels=0;
            foreach(var part in parts)
            {
                if(!BelongsToBody(part))continue;
                float m=PartMass(part);var local=transform.InverseTransformPoint(part.transform.position);
                mass+=m;center+=local*m;
                if(!IsWheel(part))continue;
                float condition=Mathf.Lerp(R6Tuning.Current.DamageTractionFloor,1,part.CurrentHp/part.MaxHp);
                traction+=condition;Wheels++;track+=Mathf.Abs(local.x);
                if(local.x<0)left+=condition;else right+=condition;
            }
            body.mass=Mathf.Max(20,mass);center/=body.mass;body.centerOfMass=center;
            foreach(var part in parts)
            {
                if(!BelongsToBody(part))continue;
                var r=transform.InverseTransformPoint(part.transform.position)-center;
                yaw+=PartMass(part)*(r.x*r.x+r.z*r.z+.045f);
            }
            body.inertiaTensorRotation=Quaternion.identity;body.inertiaTensor=new Vector3(Mathf.Max(1,yaw*.5f),Mathf.Max(1,yaw),Mathf.Max(1,yaw*.5f));
            track=Wheels>0?Mathf.Max(.45f,track/Wheels):.45f;nextMassRefresh=Time.time+.1f;
        }
        void Prepare(){if(Time.time>=nextMassRefresh)RefreshMass();}
        public void Step(float throttle,float steering,float forwardLimit,float reverseLimit,float turnLimit)
        {
            if(body.isKinematic)return;Prepare();
            var movement=GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
            if(movement!=null&&movement.TryDrive(throttle,steering,forwardLimit,reverseLimit,turnLimit))return;
            if(Wheels==0){BareHullResistance();return;}
            var tune=R6Tuning.Current;float speed=ForwardSpeed;
            float force=0;
            if(Mathf.Abs(throttle)>.01f)
            {
                bool braking=speed*throttle<-.25f;
                if(braking)force=throttle*traction*tune.BrakeForce;
                else if(Mathf.Abs(speed)<(throttle>0?forwardLimit:reverseLimit))force=throttle*AvailableForce;
            }
            else force=-Mathf.Sign(speed)*Mathf.Min(Mathf.Abs(speed)*body.mass/Time.fixedDeltaTime,AvailableForce*tune.CoastFraction+body.mass*.08f);
            var forward=SuspensionPhysics?Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized:transform.forward;
            var rightDirection=SuspensionPhysics?Vector3.ProjectOnPlane(transform.right,Vector3.up).normalized:transform.right;
            body.AddForce(forward*force,ForceMode.Force);
            float lateral=Vector3.Dot(body.linearVelocity,rightDirection);
            float grip=Mathf.Min(Mathf.Abs(lateral)*body.mass*5,AvailableForce*tune.LateralGrip+body.mass*.1f);
            body.AddForce(-rightDirection*Mathf.Sign(lateral)*grip,ForceMode.Force);
            float reverse=speed<-.2f?-1:1;
            float coverage=Wheels==0?0:Mathf.Clamp01(Mathf.Min(left,right)/Mathf.Max(1,Mathf.Max(left,right)));
            float targetYaw=(steering*reverse*turnLimit*Mathf.Lerp(.2f,1,coverage)*Mathf.Deg2Rad)+WheelImbalance*throttle*.32f;
            float requested=(targetYaw-body.angularVelocity.y)*body.inertiaTensor.y*tune.TurnResponse;
            float maxTorque=AvailableForce*track*tune.TurnLeverage+body.mass*.08f;
            body.AddTorque(Vector3.up*Mathf.Clamp(requested,-maxTorque,maxTorque),ForceMode.Force);
        }
        public void StepWorld(Vector3 wish,float speed)
        {
            if(body.isKinematic)return;Prepare();
            var movement=GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
            if(movement!=null&&movement.TryDriveWorld(wish,speed))return;
            if(Wheels==0){BareHullResistance();return;}
            wish=Vector3.ProjectOnPlane(wish,Vector3.up);
            var delta=wish.normalized*speed-body.linearVelocity;
            if(SuspensionPhysics)delta.y=0;
            var force=Vector3.ClampMagnitude(delta*body.mass*3,AvailableForce);
            // Inactive AI still has finite mass and gradual rolling resistance.
            if(wish.sqrMagnitude<.001f)force=Vector3.ClampMagnitude(-(SuspensionPhysics?Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up):body.linearVelocity)*body.mass,AvailableForce*.15f);
            body.AddForce(force,ForceMode.Force);
        }
        void BareHullResistance()
        {
            var velocity=SuspensionPhysics?Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up):body.linearVelocity;
            body.AddForce(Vector3.ClampMagnitude(-velocity*body.mass*3,body.mass*R6Tuning.Current.BareHullDrag),ForceMode.Force);
            body.AddTorque(Vector3.up*(-body.angularVelocity.y*body.inertiaTensor.y*2),ForceMode.Force);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
