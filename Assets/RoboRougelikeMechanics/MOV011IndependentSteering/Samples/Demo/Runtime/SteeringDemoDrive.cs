using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering.Demo
{
    /// <summary>
    /// Self-contained physical demonstration ONLY. NEVER attach alongside R6 TankController or other drives.
    /// Uses AddForceAtPosition so counterphase, crab and damage affect actual chassis dynamics.
    /// It does not claim to reproduce R6's contact patches, torque, friction, energy or drivetrain.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SteeringDemoDrive : MonoBehaviour
    {
        [SerializeField] private IndependentSteeringController controller;
        [SerializeField] private Rigidbody body;
        [SerializeField, Min(0f)] private float forceNewtonsPerWheel = 350f;
        [SerializeField, Min(0f)] private float lateralSlipGain = 4f;
        [SerializeField, Min(0f)] private float maxGripForceNewtonsPerWheel = 900f;
        [SerializeField, Min(0f)] private float poweredWheelSpeedLimit = 12f;
        [SerializeField] private bool automaticFixedUpdate=true;
        private float throttle;

        private void Awake()
        {
            if (controller == null) controller=GetComponent<IndependentSteeringController>();
            if (body == null) body=GetComponent<Rigidbody>();
        }
        public void Bind(IndependentSteeringController steering, Rigidbody vehicle)
        {
            controller=steering; body=vehicle;
        }
        public void SetThrottle(float normalized) { throttle=SteeringMath.SafeClamp(normalized,-1f,1f,0f); }
        public void SetAutomaticFixedUpdate(bool automatic) { automaticFixedUpdate=automatic; }
        public void SetDemoForces(float forcePerWheel, float slipGain, float maxGrip, float limit)
        {
            forceNewtonsPerWheel=Mathf.Max(0f,forcePerWheel);
            lateralSlipGain=Mathf.Max(0f,slipGain);
            maxGripForceNewtonsPerWheel=Mathf.Max(0f,maxGrip);
            poweredWheelSpeedLimit=Mathf.Max(0f,limit);
        }
        private void FixedUpdate() { if (automaticFixedUpdate) StepDrive(); }
        public void StepDrive()
        {
            if (body==null || controller==null || body.isKinematic) return;
            for (int i=0;i<4;i++)
            {
                var wheel=controller.GetWheel((SteeringWheelPosition)i);
                if (wheel==null || !wheel.IsSteerable) continue;
                Vector3 contact=wheel.transform.position;
                Quaternion swivel=Quaternion.AngleAxis(wheel.CurrentAngleDegrees,body.transform.up);
                Vector3 forward=swivel*body.transform.forward;
                Vector3 right=swivel*body.transform.right;
                // Project onto horizontal ground plane. This demo assumes R6-style planar driving.
                forward=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
                right=Vector3.ProjectOnPlane(right,Vector3.up).normalized;
                if (forward.sqrMagnitude<0.5f || right.sqrMagnitude<0.5f) continue;
                Vector3 pointVelocity=body.GetPointVelocity(contact);
                float lateralSpeed=Vector3.Dot(pointVelocity,right);
                float grip=Mathf.Clamp(-lateralSpeed * lateralSlipGain * body.mass, -maxGripForceNewtonsPerWheel,maxGripForceNewtonsPerWheel);
                body.AddForceAtPosition(right*grip,contact,ForceMode.Force);
                float speedAlongWheel=Vector3.Dot(pointVelocity,forward);
                // No active braking of external impulses or Booster velocity above the powered speed limit.
                if (throttle!=0f && (Mathf.Sign(throttle)*speedAlongWheel < poweredWheelSpeedLimit))
                    body.AddForceAtPosition(forward*(throttle*forceNewtonsPerWheel),contact,ForceMode.Force);
            }
        }
    }
}
