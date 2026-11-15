using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Pure logic; explicit tick avoids relying on wall time in tests.
    public sealed class StructuralScanClock
    {
        // Keep elapsed time separately: repeatedly subtracting a frame delta
        // from a large float can round to the same value forever.
        double cooldownDuration, cooldownElapsed, visibleDuration, visibleElapsed;
        public float CooldownLeft => (float)System.Math.Max(0d,cooldownDuration-cooldownElapsed);
        public float VisibleLeft => (float)System.Math.Max(0d,visibleDuration-visibleElapsed);
        public bool Ready => cooldownElapsed>=cooldownDuration;
        public bool Visible => visibleElapsed<visibleDuration;

        public void Start(float cooldown, float visibility)
        {
            cooldownDuration=Mathf.Max(0f,cooldown);cooldownElapsed=0d;
            visibleDuration=Mathf.Max(0f,visibility);visibleElapsed=0d;
        }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
            cooldownElapsed=System.Math.Min(cooldownDuration,cooldownElapsed+deltaTime);
            visibleElapsed=System.Math.Min(visibleDuration,visibleElapsed+deltaTime);
        }

        public void ClearVisibility() { visibleElapsed=visibleDuration; }
        public void Reset() { cooldownDuration=cooldownElapsed=visibleDuration=visibleElapsed=0d; }
    }
}
