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
        readonly R6WheelContacts contacts=new R6WheelContacts();
        public int GroundedWheels=>contacts.Count;
        public Vector3 GroundNormal=>contacts.Normal;
        RoboRougelike.PackageIntegration.RRLIndependentSteeringBinding independentSteering;
        public void BindSteering(RoboRougelike.PackageIntegration.RRLIndependentSteeringBinding binding)=>independentSteering=binding;
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
            body=GetComponent<Rigidbody>();body.isKinematic=false;body.useGravity=true;
            body.constraints=RigidbodyConstraints.None;SuspensionPhysics=true;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.maxDepenetrationVelocity=R6Tuning.Current.MaxDepenetrationSpeed;
            body.maxAngularVelocity=3;body.linearDamping=0;body.angularDamping=.1f;
            material=new PhysicsMaterial("Kontakt pojazdu R6") {bounciness=0,dynamicFriction=.05f,
                staticFriction=.05f,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            RefreshParts();
        }
        public void RefreshParts()
        {
            if(body==null||material==null)return;
            foreach(var col in GetComponentsInChildren<Collider>())
                if(col.attachedRigidbody==body)col.sharedMaterial=material;
            parts=GetComponentsInChildren<P12PipelineSafeDamageable>();contacts.Invalidate();RefreshMass();
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
        public static bool IsWheel(P12PipelineSafeDamageable part)
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
                if(!IsWheel(part)||RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))continue;
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
            nextMassRefresh=Time.time+.1f;RefreshGroundContact();
        }
        void RefreshGroundContact()
        {
            contacts.Refresh(body,parts);traction=contacts.Traction;left=contacts.Left;right=contacts.Right;track=contacts.Track;
        }
        void Prepare(){if(Time.time>=nextMassRefresh)RefreshMass();RefreshGroundContact();}
        public void Step(float throttle,float steering,float forwardLimit,float reverseLimit,float turnLimit)
        {
            if(body.isKinematic)return;Prepare();independentSteering?.RefreshAvailability();
            var movement=GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
            if(movement!=null&&movement.TryDrive(throttle,steering,forwardLimit,reverseLimit,turnLimit))return;
            if(GroundedWheels==0)return;
            var forward=Vector3.ProjectOnPlane(transform.forward,contacts.Normal).normalized;
            var rightDirection=Vector3.Cross(contacts.Normal,forward).normalized;
            var solution=default(RoboRougelike.Mechanics.IndependentSteering.SteeringSolution);
            bool steered=independentSteering!=null&&independentSteering.TrySolve(steering,Time.fixedDeltaTime,out solution);
            if(steered)
            {
                var heading=Quaternion.AngleAxis(solution.midpointSlipDegrees,contacts.Normal);
                forward=heading*forward;rightDirection=heading*rightDirection;
            }
            var tune=R6Tuning.Current;float speed=steered?Vector3.Dot(body.linearVelocity,forward):ForwardSpeed;
            float force=0;
            if(Mathf.Abs(throttle)>.01f)
            {
                bool braking=speed*throttle<-.25f;
                if(braking)force=throttle*traction*tune.BrakeForce;
                else if(Mathf.Abs(speed)<(throttle>0?forwardLimit:reverseLimit))force=throttle*AvailableForce;
            }
            else force=-Mathf.Sign(speed)*Mathf.Min(Mathf.Abs(speed)*body.mass/Time.fixedDeltaTime,AvailableForce*tune.CoastFraction+body.mass*.08f);
            body.AddForceAtPosition(forward*force,contacts.CenterOfTraction,ForceMode.Force);
            float lateral=Vector3.Dot(body.linearVelocity,rightDirection);
            float grip=Mathf.Min(Mathf.Abs(lateral)*body.mass*5,AvailableForce*tune.LateralGrip+body.mass*.1f);
            body.AddForce(-rightDirection*Mathf.Sign(lateral)*grip,ForceMode.Force);
            float reverse=speed<-.2f?-1:1;
            float coverage=Wheels==0?0:Mathf.Clamp01(Mathf.Min(left,right)/Mathf.Max(1,Mathf.Max(left,right)));
            float targetYaw=steered?Mathf.Clamp(ForwardSpeed*solution.curvatureInverseMeters,-turnLimit*Mathf.Deg2Rad,turnLimit*Mathf.Deg2Rad):
                steering*reverse*turnLimit*Mathf.Lerp(.2f,1,coverage)*Mathf.Deg2Rad;
            float contactFraction=Mathf.Clamp01((float)GroundedWheels/Mathf.Max(4,Wheels));
            targetYaw=Mathf.Clamp(targetYaw,-turnLimit*Mathf.Deg2Rad*contactFraction,turnLimit*Mathf.Deg2Rad*contactFraction);
            float requested=(targetYaw-Vector3.Dot(body.angularVelocity,contacts.Normal))*body.inertiaTensor.y*tune.TurnResponse;
            float maxTorque=AvailableForce*track*tune.TurnLeverage+body.mass*.08f;
            body.AddTorque(contacts.Normal*Mathf.Clamp(requested,-maxTorque,maxTorque),ForceMode.Force);
        }
        public void StepWorld(Vector3 wish,float speed)
        {
            if(body.isKinematic)return;Prepare();
            var movement=GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
            if(movement!=null&&movement.TryDriveWorld(wish,speed))return;
            if(GroundedWheels==0)return;
            wish=Vector3.ProjectOnPlane(wish,contacts.Normal);
            var delta=wish.normalized*speed-Vector3.ProjectOnPlane(body.linearVelocity,contacts.Normal);
            var force=Vector3.ClampMagnitude(delta*body.mass*3,AvailableForce);
            // Inactive AI still has finite mass and gradual rolling resistance.
            if(wish.sqrMagnitude<.001f)force=Vector3.ClampMagnitude(-Vector3.ProjectOnPlane(body.linearVelocity,contacts.Normal)*body.mass,AvailableForce*.15f);
            body.AddForceAtPosition(force,contacts.CenterOfTraction,ForceMode.Force);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
