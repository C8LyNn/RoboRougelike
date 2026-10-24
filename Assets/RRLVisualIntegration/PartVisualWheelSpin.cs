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
        public Transform RotationTarget => visual;

        public void Configure(Transform modelRoot, R3MountedPart mountedPart, float wheelRadiusMeters)
        {
            // W01 GLB nests the actual moving geometry under WheelRotationPivot.
            // Rotating the imported root may incorrectly rotate fixed attachments.
            visual = FindNamed(modelRoot, "WheelRotationPivot") ?? modelRoot;
            part = mountedPart;
            radiusMeters = Mathf.Max(0.05f, wheelRadiusMeters);
            initialRotation = visual != null ? visual.localRotation : Quaternion.identity;
            chassis = GetComponentInParent<Rigidbody>();
        }

        static Transform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
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
            // GLB W01 axle is local +X. Rotate the actual model pivot (not root).
            visual.localRotation = initialRotation * Quaternion.AngleAxis(degrees, Vector3.right);
        }
    }
}
