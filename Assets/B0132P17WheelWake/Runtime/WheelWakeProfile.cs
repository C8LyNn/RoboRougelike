using System;

namespace RoboRougelike.B0132P17
{
    // Pure deterministic COSMETIC policy: authored vehicle inputs, no gameplay state mutation.
    // Separate from Unity to support compiling and actually running this exact source in .NET CI.
    public enum WheelWakeMode { Off = 0, Cruise = 1, Drift = 2, Brake = 3 }

    public readonly struct WheelWakeSample
    {
        public readonly float RatePerSecond;
        public readonly float ParticleLifetime;
        public readonly float ParticleSize;
        public readonly float ParticleSpeed;
        public readonly float Alpha;
        public readonly float Red;
        public readonly float Green;
        public readonly float Blue;

        public bool Visible => RatePerSecond > 0f && Alpha > 0f;

        public WheelWakeSample(float rate, float lifetime, float size, float speed,
            float alpha, float red, float green, float blue)
        {
            RatePerSecond = rate;
            ParticleLifetime = lifetime;
            ParticleSize = size;
            ParticleSpeed = speed;
            Alpha = alpha;
            Red = red;
            Green = green;
            Blue = blue;
        }
    }

    public static class WheelWakeProfile
    {
        private const float ReferenceSpeedMetersPerSecond = 14f;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        // Input mode is authored by the receiver. These profiles MUST NOT choose
        // when actual tires slip, brake, make contact, or affect real traction/HP.
        public static WheelWakeSample Evaluate(WheelWakeMode mode,
            float speedMetersPerSecond, float slip01, bool grounded, bool vfxEnabled)
        {
            if (!grounded || !vfxEnabled ||
                !Finite(speedMetersPerSecond) || speedMetersPerSecond < 0f ||
                !Finite(slip01) || slip01 < 0f || slip01 > 1f ||
                mode < WheelWakeMode.Off || mode > WheelWakeMode.Brake)
                return default;

            float speed = Clamp01(speedMetersPerSecond / ReferenceSpeedMetersPerSecond);
            if (speed <= 0.025f || mode == WheelWakeMode.Off)
                return default;

            switch (mode)
            {
                case WheelWakeMode.Cruise:
                    return new WheelWakeSample(
                        3f + 14f * speed, .31f + .21f * speed, .12f + .11f * speed,
                        .38f + .68f * speed, .12f + .16f * speed,
                        .61f, .67f, .71f);
                case WheelWakeMode.Drift:
                    return new WheelWakeSample(
                        (7f + 37f * speed) * (.30f + .70f * slip01),
                        .43f + .25f * speed, .19f + .18f * speed,
                        .68f + .86f * speed, .19f + .27f * slip01,
                        .76f, .66f, .44f);
                case WheelWakeMode.Brake:
                    return new WheelWakeSample(
                        (5f + 31f * speed) * (.35f + .65f * slip01),
                        .18f + .18f * speed, .07f + .09f * speed,
                        .65f + 1.07f * speed, .24f + .34f * slip01,
                        .97f, .56f, .27f);
                default:
                    return default;
            }
        }
    }
}
