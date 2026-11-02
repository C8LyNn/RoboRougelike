using System;
using UnityEngine;

namespace B01.BuildImpactTelemetry001
{
    /// <summary>Optional, read-only runtime impact diagnostics on a physical vehicle part.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BuildImpactTelemetry : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minimumImpulseNewtonSeconds = 1f;
        [SerializeField, Min(1)] private int capacity = 32;

        public event Action<ImpactSample> ImpactRecorded;
        public int TotalRecorded { get; private set; }
        public ImpactSample LastSample { get; private set; }

        // No damage, Save, detach, joint changes or vehicle state mutation.
        private void OnCollisionEnter(Collision collision) => Record(collision);
        private void OnCollisionStay(Collision collision) => Record(collision);

        private void Record(Collision collision)
        {
            if (collision == null || collision.contactCount == 0) return;
            float impulse = collision.impulse.magnitude;
            if (float.IsNaN(impulse) || float.IsInfinity(impulse) ||
                impulse < Mathf.Max(0f, minimumImpulseNewtonSeconds)) return;

            var contact = collision.GetContact(0);
            Vector3 point = contact.point;
            Vector3 normal = contact.normal;
            if (!Finite(point) || !Finite(normal)) return;
            LastSample = new ImpactSample(Time.fixedTime, impulse,
                collision.relativeVelocity.magnitude, point, normal,
                collision.collider != null ? collision.collider.GetInstanceID() : 0);
            TotalRecorded = TotalRecorded == int.MaxValue ? int.MaxValue : TotalRecorded + 1;
            ImpactRecorded?.Invoke(LastSample);
        }

        public void Configure(float impulseThreshold)
        {
            if (float.IsNaN(impulseThreshold) || float.IsInfinity(impulseThreshold)
                || impulseThreshold < 0f) throw new ArgumentOutOfRangeException(nameof(impulseThreshold));
            minimumImpulseNewtonSeconds = impulseThreshold;
        }

        private static bool Finite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    public readonly struct ImpactSample
    {
        public readonly float FixedTime;
        public readonly float ImpulseNewtonSeconds;
        public readonly float RelativeSpeedMetersPerSecond;
        public readonly Vector3 WorldPoint;
        public readonly Vector3 WorldNormal;
        public readonly int OtherColliderInstanceId;

        public ImpactSample(float fixedTime, float impulse, float speed, Vector3 point, Vector3 normal, int otherId)
        {
            FixedTime = fixedTime;
            ImpulseNewtonSeconds = impulse;
            RelativeSpeedMetersPerSecond = speed;
            WorldPoint = point;
            WorldNormal = normal;
            OtherColliderInstanceId = otherId;
        }
    }
}
