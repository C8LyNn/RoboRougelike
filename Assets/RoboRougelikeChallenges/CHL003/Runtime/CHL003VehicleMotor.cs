using UnityEngine;

namespace B01.CHL003
{
    /// <summary>
    /// Actual Rigidbody forces/torques, not transform teleports or fake clears.
    /// UI/old input bindings supply axes; physics runs in FixedUpdate.
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
        private float throttle, steering;
        public void Configure(CHL003CourseController owner, float maxForce, float turnForce)
        {
            body = GetComponent<Rigidbody>();
            course = owner;
            MaxForce = maxForce;
            SteeringTorque = turnForce;
        }
        public void SetAxes(float throttleAxis, float steerAxis)
        {
            throttle = Mathf.Clamp(throttleAxis, -1f, 1f);
            steering = Mathf.Clamp(steerAxis, -1f, 1f);
        }
        private void FixedUpdate()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (course == null || course.Core == null ||
                course.Core.State != CHL003CourseCore.Phase.Running ||
                course.IsPaused) return;
            Vector3 horizontal = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
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
