using System;

namespace RoboRougelike.B0132P17.DualRecoil
{
    /// <summary>Pure C#9 deterministic dual-weapon VISUAL recoil, no physics or gameplay.</summary>
    public enum RecoilLane { Left = 0, Right = 1 }

    public struct RecoilPose
    {
        public float LeftTravel, RightTravel;
        public float LeftFlash, RightFlash;
    }

    public sealed class DualRecoilCore
    {
        public const float MaxTravel = 0.28f;
        public const float BaseImpulse = 2.8f;
        public const float SpringOmega = 17f;
        private readonly float[] displacement = new float[2];
        private readonly float[] velocity = new float[2];
        private readonly float[] flash = new float[2];
        private bool enabled = true;

        public bool VisualsEnabled => enabled;

        public void SetVisualsEnabled(bool value)
        {
            enabled = value;
            if (!enabled) Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < 2; i++)
            {
                displacement[i] = 0f;
                velocity[i] = 0f;
                flash[i] = 0f;
            }
        }

        public bool Fire(RecoilLane lane, float intensity = 1f)
        {
            int index = (int)lane;
            if (!enabled || index < 0 || index > 1 || !Finite(intensity) || intensity <= 0f)
                return false;
            float scale = Math.Min(intensity, 1f);
            velocity[index] -= BaseImpulse * scale;
            // A single channel can be retriggered without moving the other channel.
            velocity[index] = Math.Max(-BaseImpulse * 3f, velocity[index]);
            flash[index] = Math.Max(flash[index], scale);
            return true;
        }

        public void Advance(float elapsedSeconds)
        {
            if (!enabled || !Finite(elapsedSeconds) || elapsedSeconds <= 0f)
                return;
            // Closed-form critically damped recovery, stable under variable frame size.
            // Split large delays to prevent a paused view keeping a stale gun kick.
            float t = Math.Min(elapsedSeconds, 10f);
            float decay = (float)Math.Exp(-SpringOmega * t);
            for (int i = 0; i < 2; i++)
            {
                float x = displacement[i];
                float v = velocity[i];
                float coeff = v + SpringOmega * x;
                float nextX = (x + coeff * t) * decay;
                float nextV = (v - SpringOmega * coeff * t) * decay;
                displacement[i] = Math.Max(-MaxTravel, Math.Min(0f, nextX));
                velocity[i] = nextV;
                flash[i] *= (float)Math.Exp(-23f * t);
                if (Math.Abs(displacement[i]) < 0.00001f &&
                    Math.Abs(velocity[i]) < 0.0001f)
                {
                    displacement[i] = 0f;
                    velocity[i] = 0f;
                }
                if (flash[i] < 0.00001f)
                    flash[i] = 0f;
            }
        }

        public RecoilPose Sample()
        {
            return new RecoilPose {
                LeftTravel = -displacement[0],
                RightTravel = -displacement[1],
                LeftFlash = flash[0],
                RightFlash = flash[1]
            };
        }

        public static bool Finite(float v) =>
            !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
