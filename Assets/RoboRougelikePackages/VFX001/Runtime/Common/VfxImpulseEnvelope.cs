using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Pure, deterministic envelope for non-gameplay jolts and camera impulses.</summary>
    public static class VfxImpulseEnvelope
    {
        public static float Attenuation(float distance, float fullStrengthDistance, float maximumDistance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance)) return 0f;
            float near = Mathf.Max(0f, fullStrengthDistance);
            float far = Mathf.Max(near + 0.0001f, maximumDistance);
            float t = Mathf.Clamp01((Mathf.Max(0f, distance) - near) / (far - near));
            return 1f - t * t * (3f - 2f * t);
        }

        public static float Decay(float age, float duration)
        {
            float normalized = Mathf.Clamp01(Mathf.Max(0f, age) / Mathf.Max(0.0001f, duration));
            float tail = 1f - normalized;
            return tail * tail;
        }

        public static Vector3 Bounded(Vector3 input, float maximumMagnitude)
        {
            if (!Finite(input)) return Vector3.zero;
            return Vector3.ClampMagnitude(input, Mathf.Max(0f, maximumMagnitude));
        }

        public static bool Finite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
}
