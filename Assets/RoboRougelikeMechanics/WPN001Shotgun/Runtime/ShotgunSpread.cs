using System;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    // Deterministic area-stratified pellet pattern inside a CONE OF FULL ANGLE.
    // Does not mutate UnityEngine.Random state. Not cryptographic; do not use as network authority.
    public static class ShotgunSpread
    {
        private const float GoldenAngleRadians = 2.3999632297286533f;

        public static uint VolleySeed(uint baseSeed, uint successfulShotIndex)
        {
            // Different successful salvos receive independent patterns.
            uint x = baseSeed ^ (successfulShotIndex * 0x9E3779B9u) ^ 0xA341316Cu;
            x ^= x >> 16; x *= 0x7FEB352Du;
            x ^= x >> 15; x *= 0x846CA68Bu;
            return x ^ (x >> 16);
        }

        public static int BuildDirections(Vector3 forward, int count, float fullConeDegrees,
            uint seed, Vector3[] output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (count < 1 || count > 64 || output.Length < count)
                throw new ArgumentOutOfRangeException(nameof(count), "count must be 1..64 and fit output");
            if (!IsFinite(forward.x) || !IsFinite(forward.y) || !IsFinite(forward.z)
                || forward.sqrMagnitude < 1e-10f)
                throw new ArgumentException("forward must be a finite, non-zero vector", nameof(forward));
            if (!IsFinite(fullConeDegrees)) throw new ArgumentException("cone must be finite", nameof(fullConeDegrees));
            float halfRadians = Mathf.Clamp(fullConeDegrees, 0f, 90f) * Mathf.Deg2Rad * 0.5f;
            Vector3 axis = forward.normalized;
            if (count == 1 || halfRadians <= 0f)
            {
                for (int i = 0; i < count; i++) output[i] = axis;
                return count;
            }

            Quaternion basis = Quaternion.FromToRotation(Vector3.forward, axis);
            uint x = seed == 0u ? 0x6D2B79F5u : seed;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            float offset = (x & 0x00FFFFFFu) * (Mathf.PI * 2f / 16777216f);
            float cosEdge = Mathf.Cos(halfRadians);

            for (int i = 0; i < count; i++)
            {
                // Equal-area annuli, one sample per annulus, golden-angle azimuth.
                float u = (i + 0.5f) / count;
                float cosTheta = 1f - u * (1f - cosEdge);
                float sinTheta = Mathf.Sqrt(Mathf.Max(0f, 1f - cosTheta * cosTheta));
                float azimuth = offset + GoldenAngleRadians * i;
                Vector3 local = new Vector3(Mathf.Cos(azimuth) * sinTheta,
                    Mathf.Sin(azimuth) * sinTheta, cosTheta);
                output[i] = (basis * local).normalized;
            }
            return count;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
