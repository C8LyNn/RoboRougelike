using UnityEngine;
using RoboRougelike.R3;

namespace RoboRougelike.VisualIntegration
{
    /// <summary>Wheel appearance only. No forces, colliders or contact modification.</summary>
    [DefaultExecutionOrder(710)]
    [DisallowMultipleComponent]
    public sealed class PartVisualWheelSpin : MonoBehaviour
    {
        Transform visual;
        R3MountedPart part;
        Rigidbody chassis;
        Quaternion initialRotation;
        float degrees;
        float radiusMeters = 0.39f;

        public void Configure(Transform modelRoot, R3MountedPart mountedPart, float wheelRadiusMeters)
        {
            visual = modelRoot;
            part = mountedPart;
            radiusMeters = Mathf.Max(0.05f, wheelRadiusMeters);
            initialRotation = visual != null ? visual.localRotation : Quaternion.identity;
            chassis = GetComponentInParent<Rigidbody>();
        }

        public static float SpinDegrees(float forwardSpeedMetersPerSecond,
                                        float radius, float deltaTime)
        {
            if (radius <= 0f || deltaTime <= 0f) return 0f;
            return forwardSpeedMetersPerSecond / radius * Mathf.Rad2Deg * deltaTime;
        }

        void LateUpdate()
        {
            if (visual == null) return;
            if (chassis == null) chassis = GetComponentInParent<Rigidbody>();
            if (chassis == null) return; // Garage ghost: no spin
            if (part == null || !part.Operational ||
                part.GetComponentInParent<Rigidbody>() != chassis)
                return; // No spin after detachment or destruction

            float speed = Vector3.Dot(chassis.linearVelocity, chassis.transform.forward);
            degrees = Mathf.Repeat(degrees + SpinDegrees(speed, radiusMeters, Time.deltaTime), 360f);
            // GLB W01 axle is local +X. Rotating the child does NOT fight the follower.
            visual.localRotation = initialRotation * Quaternion.AngleAxis(degrees, Vector3.right);
        }
    }
}
