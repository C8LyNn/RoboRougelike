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
        float nextMassRefresh, traction, left, right, track, powerUnits;
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
        // Motor output rises with installed working devices; the cap represents
        // finite tire traction rather than a limit on how many blocks can be fitted.
        public static float MotorMultiplier(float units)=>Mathf.Min(2f,1f+.2f*Mathf.Max(0,units));
        float BaseTractionForce=>traction*R6Tuning.Current.WheelForce;
        public float AvailableForce => BaseTractionForce*MotorMultiplier(powerUnits);
        public float ActivePowerUnits=>powerUnits;
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
            if(part.TryGetComponent<VehicleEnergyBattery>(out var battery))return R4WeaponDefinition.PartMass(R3PartKind.Battery)*R3VehicleModel.TierMass(battery.Tier);
            if(part.TryGetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>(out var device)&&device.State!=null)return R4WeaponDefinition.PartMass(device.State);
            if(part.TryGetComponent<R3MountedPart>(out var mounted))return R4WeaponDefinition.PartMass(mounted.Kind,mounted.Weapon)*R3VehicleModel.TierMass(mounted.Tier)*
                (mounted.Kind==R3PartKind.Wheel?R3VehicleModel.WheelMass(mounted.WheelStyle):1);
            part.TryGetComponent<P12PipelineSafeFunctionalModule>(out var module);
            return module!=null&&module.ModuleType==P12FunctionalModuleType.Weapon?32:
                (module!=null&&module.ModuleType==P12FunctionalModuleType.Wheel||part.name.StartsWith("Koło"))?12:8;
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
            if(part==null||!part.gameObject.activeInHierarchy||part.IsDead||!part.transform.IsChildOf(transform)||part.GetComponentInParent<Rigidbody>()!=body)return false;
            var mounted=part.GetComponent<R3MountedPart>();return mounted==null||mounted.Operational;
        }
        public void RefreshMass()
        {
            if(parts==null||!gameObject.activeInHierarchy)return;
            float mass=0,yaw=0;Vector3 center=Vector3.zero;
            traction=left=right=track=powerUnits=0;Wheels=0;
            foreach(var part in parts)
            {
                if(!BelongsToBody(part))continue;
                float m=PartMass(part);var local=transform.InverseTransformPoint(part.transform.position);
                mass+=m;center+=local*m;
                var mounted=part.GetComponent<R3MountedPart>();
                if(mounted!=null&&mounted.Kind==R3PartKind.DrivePower&&
                    !RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                    powerUnits+=Mathf.Clamp01(part.CurrentHp/Mathf.Max(1,part.MaxHp))*R3VehicleModel.TierEffectUnits(mounted.Tier);
                if(!IsWheel(part)||RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))continue;
                float condition=Mathf.Lerp(R6Tuning.Current.DamageTractionFloor,1,part.CurrentHp/part.MaxHp);
                condition*=R3VehicleModel.TierWheelTraction(mounted!=null?mounted.Tier:
                    part.GetComponent<RoboRougelike.WeaponIntegration.RRLDeviceBinding>()?.State?.Tier??1);
                if(mounted!=null)condition*=R3VehicleModel.WheelTraction(mounted.WheelStyle);
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
            var assembly=GetComponent<R3VehicleAssembly>();
            if(assembly!=null&&assembly.Model!=null)assembly.TuneChassis();
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
            else force=-Mathf.Sign(speed)*Mathf.Min(Mathf.Abs(speed)*body.mass/Time.fixedDeltaTime,BaseTractionForce*tune.CoastFraction+body.mass*.08f);
            body.AddForceAtPosition(forward*force,contacts.CenterOfTraction,ForceMode.Force);
            float lateral=Vector3.Dot(body.linearVelocity,rightDirection);
            float grip=Mathf.Min(Mathf.Abs(lateral)*body.mass*5,BaseTractionForce*tune.LateralGrip+body.mass*.1f);
            body.AddForce(-rightDirection*Mathf.Sign(lateral)*grip,ForceMode.Force);
            float reverse=speed<-.2f?-1:1;
            float coverage=Wheels==0?0:Mathf.Clamp01(Mathf.Min(left,right)/Mathf.Max(1,Mathf.Max(left,right)));
            float targetYaw=steered?Mathf.Clamp(ForwardSpeed*solution.curvatureInverseMeters,-turnLimit*Mathf.Deg2Rad,turnLimit*Mathf.Deg2Rad):
                steering*reverse*turnLimit*Mathf.Lerp(.2f,1,coverage)*Mathf.Deg2Rad;
            float contactFraction=Mathf.Clamp01((float)GroundedWheels/Mathf.Max(4,Wheels));
            targetYaw=Mathf.Clamp(targetYaw,-turnLimit*Mathf.Deg2Rad*contactFraction,turnLimit*Mathf.Deg2Rad*contactFraction);
            float requested=(targetYaw-Vector3.Dot(body.angularVelocity,contacts.Normal))*body.inertiaTensor.y*tune.TurnResponse;
            // A fully supported steering controller can compensate the known motor yaw
            // moment within this same traction-limited servo. Partial/damaged support keeps
            // its physical asymmetry; roll/pitch and external impulses are not cancelled.
            if(steered&&GroundedWheels==Wheels&&coverage>.99f)
                requested-=Vector3.Dot(Vector3.Cross(contacts.CenterOfTraction-body.worldCenterOfMass,forward*force),contacts.Normal);
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

        // Wheeled AI steers its chassis before driving instead of applying a
        // sideways world-space motor force. Omni/hover retain their own drives.
        public void StepToward(Vector3 wish,float speed,float turnLimit)
        {
            if(body.isKinematic)return;
            Prepare();
            var movement=GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
            if(movement!=null&&movement.TryDriveWorld(wish,speed))return;
            if(GroundedWheels==0)return;
            var input=WorldSteering(transform.forward,wish,contacts.Normal,ForwardSpeed);
            Step(input.x,input.y,Mathf.Max(0,speed),Mathf.Max(0,speed),turnLimit);
        }

        // x = throttle, y = steering. Reverse when the target is behind so a
        // wheeled robot can escape corners without driving sideways.
        public static Vector2 WorldSteering(Vector3 forward,Vector3 wish,Vector3 normal,float forwardSpeed=0)
        {
            if(normal.sqrMagnitude<.0001f)normal=Vector3.up;
            normal.Normalize();
            forward=Vector3.ProjectOnPlane(forward,normal);
            wish=Vector3.ProjectOnPlane(wish,normal);
            if(forward.sqrMagnitude<.0001f||wish.sqrMagnitude<.0001f)return Vector2.zero;
            float angle=Vector3.SignedAngle(forward,wish,normal);
            float direction=1;
            if(Mathf.Abs(angle)>100f)
            {
                angle=Mathf.DeltaAngle(0,angle+(angle>0?-180f:180f));
                direction=-1;
            }
            float throttle=direction*Mathf.Clamp01(Mathf.Cos(angle*Mathf.Deg2Rad));
            float steering=(forwardSpeed<-.2f?-1:1)*Mathf.Clamp(angle/45f,-1,1);
            return new Vector2(throttle,steering);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
