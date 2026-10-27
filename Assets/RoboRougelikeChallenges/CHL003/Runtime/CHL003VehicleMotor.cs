using UnityEngine;

namespace B01.CHL003
{
    /// <summary>
    /// Force-driven physically distinct demo rigs. Traction is available
    /// only when at least one real wheel-bottom ray contacts non-trigger,
    /// non-self terrain with a support normal. No propulsive AIR CONTROL.
    /// Existing Rigidbody momentum and gravity remain real while airborne.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CHL003VehicleMotor : MonoBehaviour
    {
        public float MaxForce = 4200f;
        public float SteeringTorque = 700f;
        public float MaxHorizontalSpeed = 12f;
        public float MaxAngularSpeed = 2f;
        private Rigidbody body;
        private CHL003CourseController course;
        private Transform[] groundProbes;
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private float throttle, steering;
        public bool IsGrounded { get; private set; }
        public int GroundedSteps { get; private set; }
        public int AirborneSteps { get; private set; }
        public int SupportRayCount { get; private set; }

        public void Configure(CHL003CourseController owner, float maxForce,
            float turnForce, Transform[] physicalWheels = null)
        {
            body = GetComponent<Rigidbody>();
            course = owner;
            MaxForce = maxForce;
            SteeringTorque = turnForce;
            groundProbes = physicalWheels == null ?
                new Transform[0] : (Transform[])physicalWheels.Clone();
            IsGrounded = false;
            GroundedSteps = AirborneSteps = SupportRayCount = 0;
        }

        public void ResetTelemetry()
        {
            GroundedSteps = AirborneSteps = SupportRayCount = 0;
            IsGrounded = false;
        }

        public void SetAxes(float throttleAxis, float steerAxis)
        {
            throttle = Mathf.Clamp(throttleAxis, -1f, 1f);
            steering = Mathf.Clamp(steerAxis, -1f, 1f);
        }

        private bool HasRealGroundContact()
        {
            SupportRayCount = 0;
            if (body == null || groundProbes == null) return false;
            for (int n = 0; n < groundProbes.Length; n++)
            {
                Transform probe = groundProbes[n];
                if (probe == null) continue;
                // Only physically present sphere-wheel colliders grant
                // traction. The old .95m probe falsely supported a wheel
                // hovering ~.35m above level ground (B01-60 W26 QA-01).
                SphereCollider wheel = probe.GetComponent<SphereCollider>();
                if (wheel == null || !wheel.enabled || wheel.isTrigger ||
                    wheel.attachedRigidbody != body) continue;
                float physicalRadius = wheel.bounds.extents.y;
                float rayReach =
                    CHL003WheelSupportWindow.MaximumCastMeters(physicalRadius);
                if (rayReach <= 0f) continue;
                Vector3 origin = probe.position +
                    Vector3.up * CHL003WheelSupportWindow.OriginLiftMeters;
                int count = Physics.RaycastNonAlloc(
                    origin, Vector3.down, groundHits, rayReach,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider c = groundHits[i].collider;
                    if (c == null || c.isTrigger || !c.enabled ||
                        c.attachedRigidbody == body ||
                        groundHits[i].normal.y < .5f ||
                        !CHL003WheelSupportWindow.WithinWheelReach(
                            groundHits[i].distance, physicalRadius)) continue;
                    SupportRayCount++;
                    break;
                }
            }
            return SupportRayCount > 0;
        }

        private void FixedUpdate()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (course == null || course.Core == null ||
                course.Core.State != CHL003CourseCore.Phase.Running ||
                course.IsPaused || body.isKinematic) return;
            IsGrounded = HasRealGroundContact();
            if (IsGrounded) GroundedSteps++;
            else AirborneSteps++;
            if (!IsGrounded) return; // momentum/gravity only while airborne
            Vector3 horizontal = Vector3.ProjectOnPlane(
                body.linearVelocity, Vector3.up);
            if (horizontal.magnitude < MaxHorizontalSpeed ||
                Vector3.Dot(horizontal, transform.forward * throttle) < 0f)
                body.AddForce(transform.forward * (throttle * MaxForce),
                    ForceMode.Force);
            if (Mathf.Abs(body.angularVelocity.y) < MaxAngularSpeed)
                body.AddTorque(Vector3.up * (steering * SteeringTorque),
                    ForceMode.Force);
        }
    }
}
