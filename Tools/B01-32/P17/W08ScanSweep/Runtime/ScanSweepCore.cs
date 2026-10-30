using System;

namespace RoboRougelike.B0132P17.ScanSweep
{
    // Cosmetic-only; no perception, enemy detection, physics, or gameplay decisions.
    public enum ScanState { Off, Sweeping, Fading }
    public struct ScanVertex
    {
        public float X, Y, Z, Alpha;
    }
    public struct ScanPose
    {
        public bool Visible;
        public ScanState State;
        public float AngleRadians;
        public float Opacity;
    }
    public sealed class ScanSweepCore
    {
        public const int SegmentCount = 32;
        public const int VertexCount = SegmentCount * 4;
        public const int IndexCount = SegmentCount * 6;
        public const float InnerRadius = 0.35f;
        public const float OuterRadius = 2.25f;
        public const float OpeningRadians = 1.0471976f; // 60 degrees
        public const float SweepSeconds = 1.40f;
        public const float FadeSeconds = 0.42f;
        public const float TotalSeconds = SweepSeconds + FadeSeconds;
        private const float Tau = 6.2831853f;
        private float elapsed;
        private float heading;
        private bool visualsEnabled = true;
        public ScanState State { get; private set; } = ScanState.Off;

        public bool Start(float headingRadians)
        {
            if (!visualsEnabled || !Finite(headingRadians)) return false;
            heading = headingRadians % Tau;
            elapsed = 0f;
            State = ScanState.Sweeping;
            return true;
        }
        public void SetVisualsEnabled(bool value)
        {
            visualsEnabled = value;
            if (!value) Cancel();
        }
        public void Cancel()
        {
            elapsed = 0f;
            State = ScanState.Off;
        }
        public void Advance(float dt)
        {
            if (State == ScanState.Off || !Finite(dt) || dt <= 0f) return;
            elapsed = Math.Min(TotalSeconds, elapsed + Math.Min(dt, TotalSeconds));
            if (elapsed >= TotalSeconds)
            {
                Cancel();
            }
            else if (elapsed >= SweepSeconds)
            {
                State = ScanState.Fading;
            }
        }
        public ScanPose Sample()
        {
            float alpha = State == ScanState.Off ? 0f :
                State == ScanState.Sweeping ? 1f :
                Math.Max(0f, 1f - (elapsed - SweepSeconds) / FadeSeconds);
            return new ScanPose {
                Visible = State != ScanState.Off,
                State = State,
                AngleRadians = State == ScanState.Off ? 0f :
                    (heading + Tau * Math.Min(elapsed, SweepSeconds) / SweepSeconds) % Tau,
                Opacity = alpha
            };
        }
        public static void BuildSector(ScanVertex[] vertices, int[] indices)
        {
            if (vertices == null || vertices.Length != VertexCount)
                throw new ArgumentException("Expected exact original 128 scan sector vertices.", nameof(vertices));
            if (indices == null || indices.Length != IndexCount)
                throw new ArgumentException("Expected exact original 192 triangle indices.", nameof(indices));
            for (int i = 0; i < SegmentCount; i++)
            {
                float a = -OpeningRadians * 0.5f + OpeningRadians * i / SegmentCount;
                float b = -OpeningRadians * 0.5f + OpeningRadians * (i + 1) / SegmentCount;
                int j = i * 4;
                vertices[j] = Point(InnerRadius, a, 0.21f);
                vertices[j + 1] = Point(OuterRadius, a, 0.02f);
                vertices[j + 2] = Point(OuterRadius, b, 0.02f);
                vertices[j + 3] = Point(InnerRadius, b, 0.21f);
                int k = i * 6;
                indices[k] = j;
                indices[k + 1] = j + 1;
                indices[k + 2] = j + 2;
                indices[k + 3] = j;
                indices[k + 4] = j + 2;
                indices[k + 5] = j + 3;
            }
        }
        private static ScanVertex Point(float radius, float angle, float alpha) =>
            new ScanVertex {
                X = radius * (float)Math.Sin(angle),
                Y = 0.015f,
                Z = radius * (float)Math.Cos(angle),
                Alpha = alpha
            };
        public static bool Finite(float value) =>
            !float.IsInfinity(value) && !float.IsNaN(value);
    }
}
