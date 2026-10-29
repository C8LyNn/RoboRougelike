using System;

namespace RoboRougelike.B0132P17.SalvageUnity
{
    // Fully deterministic COSMETIC clock. No Unity, loot, save, pickup or combat dependency.
    public sealed class SalvageBeaconClock
    {
        public const int FrameCount = 8;
        public const float SecondsPerFrame = 0.12f;
        private float remainder;
        public int Frame { get; private set; }

        public void Reset() { Frame = 0; remainder = 0f; }

        public int Advance(float elapsedSeconds, bool vfxEnabled)
        {
            if (!vfxEnabled) { Reset(); return Frame; }
            if (float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) ||
                elapsedSeconds < 0f) return Frame;
            // Prevent unbounded catchup loops following a suspended window.
            double delta = Math.Min(elapsedSeconds, 1.92f);
            double total = remainder + delta;
            int whole = (int)(total / SecondsPerFrame);
            if (whole > 0)
            {
                Frame = (Frame + whole) % FrameCount;
                remainder = (float)(total - whole * SecondsPerFrame);
            }
            else remainder = (float)total;
            return Frame;
        }
    }
}
