using System;
using System.Linq;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

namespace RoboRougelike.R6
{
    [Serializable] public sealed class R6CoreRecoverySettings
    {
        public float TiltThresholdDegrees=110,ConfirmationSeconds=1.5f,AttemptSeconds=2,CooldownSeconds=8;
        public float TargetLiftSpeed=1.4f,MaximumLiftImpulse=1800,MaximumTorqueNm=30000;
        public float AngularAcceleration=9,AngularDamping=6.5f;
    }

    // Emergency Core capability only; no pose/velocity writes and no continuous upright assist.
    [DisallowMultipleComponent]
    public sealed class R6CoreRecovery : MonoBehaviour
    {
        public R6CoreRecoverySettings Tuning=new R6CoreRecoverySettings();
        Rigidbody body;R1VehicleState vehicle;P12PipelineSafeDamageable core;R3PartState state;
        Collider support,contactPart;Vector3 localContact;float confirmation,activeUntil,recoveryLever;
        readonly RaycastHit[] supportHits=new RaycastHit[64];
        public int AttemptsStarted {get;private set;}
        public bool IsRecovering {get;private set;}
        public float LastLiftImpulse {get;private set;}
        public float LastPeakTorque {get;private set;}
        public float CooldownLeft=>state==null?0:Mathf.Max(0,state.DeviceReadyAt-Time.time);

        public void Configure(R3VehicleAssembly assembly)
        {
            body=assembly.GetComponent<Rigidbody>();vehicle=assembly.GetComponent<R1VehicleState>();core=vehicle.Core;
            state=assembly.Model.Parts.FirstOrDefault(p=>p.Kind==R3PartKind.Core);
            // The new electronic capability uses the existing EMP clock and canonical Core state.
            if(core!=null&&state!=null&&core.GetComponent<RRLEmpPartState>()==null)
                core.gameObject.AddComponent<RRLEmpPartState>().Bind(transform,state);
        }
        bool Available()
        {
            if(body==null||body.isKinematic||!body.useGravity||core==null||core.IsDead||state==null||vehicle==null||
                !core.transform.IsChildOf(transform)||core.GetComponentInParent<Rigidbody>()!=body||
                !vehicle.Controller.GameplayInputEnabled||vehicle.DefeatReason!=null||RRLEmpPartState.IsBlocked(core.gameObject))return false;
            var run=R1RunController.Instance;
            return run==null||(!run.Paused&&!run.Transitioning&&!run.RangeDisabled&&run.Progress!=null&&
                (run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive));
        }
        void OnCollisionEnter(Collision collision)=>RememberSupport(collision);
        void OnCollisionStay(Collision collision)=>RememberSupport(collision);
        void RememberSupport(Collision collision)
        {
            if(body==null||!R6WheelContacts.Terrain(collision.collider))return;
            for(int i=0;i<collision.contactCount;i++)
            {
                var c=collision.GetContact(i);
                if(c.normal.y<.5f||c.thisCollider==null||c.thisCollider.attachedRigidbody!=body||c.thisCollider.isTrigger)continue;
                support=collision.collider;contactPart=c.thisCollider;
                localContact=Quaternion.Inverse(body.rotation)*(c.point-body.position);return;
            }
        }
        bool StillSupported()
        {
            if(!R6WheelContacts.Terrain(support)||contactPart==null||!contactPart.enabled||contactPart.isTrigger||contactPart.attachedRigidbody!=body)return false;
            var point=body.position+body.rotation*localContact;var bounds=contactPart.bounds;bounds.Expand(.1f);
            if(!bounds.Contains(point))return false;
            int count=Physics.RaycastNonAlloc(point+Vector3.up*.04f,Vector3.down,supportHits,.09f,~0,QueryTriggerInteraction.Ignore);
            if(count==supportHits.Length)return false;
            for(int i=0;i<count;i++)if(supportHits[i].collider==support&&supportHits[i].normal.y>.5f)return true;
            return false;
        }
        static float Limit(float value,float fallback,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
        void FixedUpdate()
        {
            if(!Available()){IsRecovering=false;confirmation=0;return;}
            if(Tuning==null)Tuning=new R6CoreRecoverySettings();
            var up=body.rotation*Vector3.up;float angle=Vector3.Angle(up,Vector3.up);
            if(IsRecovering)
            {
                if(Time.time>=activeUntil||(angle<20&&Vector3.ProjectOnPlane(body.angularVelocity,Vector3.up).magnitude<.5f))
                {IsRecovering=false;return;}
                ApplyTorque();return;
            }
            bool confirmed=CooldownLeft<=0&&angle>=Limit(Tuning.TiltThresholdDegrees,110,100,170)&&
                body.linearVelocity.magnitude<2&&body.angularVelocity.magnitude<1.2f&&StillSupported();
            confirmation=confirmed?confirmation+Time.fixedDeltaTime:0;
            if(confirmation<Limit(Tuning.ConfirmationSeconds,1.5f,.5f,10))return;
            confirmation=0;state.DeviceReadyAt=Time.time+Limit(Tuning.CooldownSeconds,8,5,60);
            activeUntil=Time.time+Limit(Tuning.AttemptSeconds,2,.1f,3);AttemptsStarted++;IsRecovering=true;LastPeakTorque=0;
            recoveryLever=SupportLever();
            LastLiftImpulse=Mathf.Min(Limit(Tuning.MaximumLiftImpulse,1800,0,3000),
                body.mass*Mathf.Max(0,Limit(Tuning.TargetLiftSpeed,1.4f,0,2)-Vector3.Dot(body.linearVelocity,Vector3.up)));
            body.AddForce(Vector3.up*LastLiftImpulse,ForceMode.Impulse);ApplyTorque();
            Debug.Log($"[PHY CORE] attempt={AttemptsStarted} lift={LastLiftImpulse:F1}Ns mass={body.mass:F1}kg cooldown={CooldownLeft:F1}s");
        }
        void ApplyTorque()
        {
            // Reuse MOV004's finite, yaw-preserving tilt math only during the bounded attempt.
            var acceleration=GyroStabilizationMath.Calculate(body.rotation,body.angularVelocity,Vector3.up,12,
                Limit(Tuning.AngularDamping,6.5f,0,12),Limit(Tuning.AngularAcceleration,9,0,15));
            var axes=body.rotation*body.inertiaTensorRotation;
            var torque=axes*Vector3.Scale(body.inertiaTensor,Quaternion.Inverse(axes)*acceleration);
            // A supported roof has a gravity moment around its edge. A free-flight PD alone
            // cannot lift a broad chassis from that support; budget the bounded Core torque for it.
            var upright=GyroStabilizationMath.Calculate(body.rotation,Vector3.zero,Vector3.up,1,0,1).normalized;
            float tilt=Vector3.Angle(body.rotation*Vector3.up,Vector3.up);
            torque+=upright*(body.mass*Physics.gravity.magnitude*recoveryLever*Mathf.InverseLerp(60,110,tilt));
            torque=Vector3.ClampMagnitude(torque,Limit(Tuning.MaximumTorqueNm,30000,0,50000));
            LastPeakTorque=Mathf.Max(LastPeakTorque,torque.magnitude);body.AddTorque(torque,ForceMode.Force);
        }
        float SupportLever()
        {
            var bounds=new Bounds(body.worldCenterOfMass,Vector3.zero);
            foreach(var c in GetComponentsInChildren<Collider>())
                if(c.enabled&&!c.isTrigger&&c.attachedRigidbody==body)bounds.Encapsulate(c.bounds);
            return Mathf.Max(bounds.extents.x,bounds.extents.z);
        }
        void OnDisable(){IsRecovering=false;confirmation=0;support=contactPart=null;}
    }
}
