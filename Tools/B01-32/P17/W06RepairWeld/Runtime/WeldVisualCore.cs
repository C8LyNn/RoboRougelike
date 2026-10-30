using System;

namespace RoboRougelike.B0132P17.RepairWeld
{
    // Presentation-only C#9 deterministic weld and cooldown lifecycle.
    // No UnityEngine, inventory, currency, HP, repair transactions, or network state.
    public enum WeldPhase { Idle, Welding, Cooling }

    public struct WeldSpark
    {
        public float X0, Y0, X1, Y1, Intensity;
    }

    public sealed class WeldFrame
    {
        public bool Visible;
        public float Progress, Heat, ArcX, ArcY;
        public readonly WeldSpark[] Sparks = new WeldSpark[8];
    }

    public sealed class WeldVisualCore
    {
        public const float CoolSeconds = 0.65f;
        public WeldPhase Phase { get; private set; }
        public float Progress { get; private set; }
        public float TimeSeconds { get; private set; }
        private uint seed;

        public void Begin(uint visualSeed)
        {
            Phase = WeldPhase.Welding;
            Progress = 0f;
            TimeSeconds = 0f;
            seed = visualSeed;
        }

        public void SetProgress(float progress)
        {
            if (Phase != WeldPhase.Welding || !Finite(progress)) return;
            float clamped = Math.Max(0f, Math.Min(1f, progress));
            Progress = Math.Max(Progress, clamped); // never show visual rollback as successful work
        }

        public void Finish()
        {
            if (Phase != WeldPhase.Welding) return;
            Progress = 1f;
            TimeSeconds = 0f;
            Phase = WeldPhase.Cooling;
        }

        public void Cancel()
        {
            Phase = WeldPhase.Idle;
            Progress = 0f;
            TimeSeconds = 0f;
        }

        public void Tick(float deltaSeconds)
        {
            if (Phase == WeldPhase.Idle || !Finite(deltaSeconds) || deltaSeconds < 0f) return;
            TimeSeconds += Math.Min(deltaSeconds, 1.0f);
            if (Phase == WeldPhase.Cooling && TimeSeconds >= CoolSeconds) Cancel();
            else if (TimeSeconds >= 3600f) TimeSeconds = 0f;
        }

        // Samples into one reused frame buffer: no heap allocation per Unity Update.
        public void Sample(WeldFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            frame.Visible = Phase != WeldPhase.Idle;
            frame.Progress = Progress;
            frame.Heat = Phase == WeldPhase.Idle ? 0f :
                Phase == WeldPhase.Welding ? 1f : Math.Max(0f, 1f - TimeSeconds / CoolSeconds);
            frame.ArcX = -0.52f + 1.04f * Progress;
            frame.ArcY = 0.05f + 0.13f * (float)Math.Sin(TimeSeconds * 19f);
            for (int i = 0; i < frame.Sparks.Length; i++)
            {
                float noise = HashUnit(seed, i);
                float life = (TimeSeconds * (2.4f + noise) + i * 0.173f + noise) % 1f;
                float angle = (i + noise) * 2.39996f + TimeSeconds * 5.3f;
                float reach = 0.08f + 0.29f * life;
                float dx = (float)Math.Cos(angle) * reach;
                float dy = (float)Math.Sin(angle) * reach;
                float fade = (1f - life) * frame.Heat;
                frame.Sparks[i] = new WeldSpark {
                    X0 = frame.ArcX, Y0 = frame.ArcY,
                    X1 = frame.ArcX + dx, Y1 = frame.ArcY + dy,
                    Intensity = frame.Visible ? fade : 0f
                };
            }
        }

        public static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float HashUnit(uint initial, int index)
        {
            unchecked
            {
                uint h = initial ^ ((uint)index + 1u) * 0x9E3779B9u;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return (h & 0xFFFFu) / 65536f;
            }
        }
    }
}
