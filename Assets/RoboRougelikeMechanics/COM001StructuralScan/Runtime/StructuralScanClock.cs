using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Pure logic; explicit tick avoids relying on wall time in tests.
    public sealed class StructuralScanClock
    {
        public float CooldownLeft { get; private set; }
        public float VisibleLeft { get; private set; }
        public bool Ready { get { return CooldownLeft <= 0f; } }
        public bool Visible { get { return VisibleLeft > 0f; } }

        public void Start(float cooldown, float visibility)
        {
            CooldownLeft = Mathf.Max(0f, cooldown);
            VisibleLeft = Mathf.Max(0f, visibility);
        }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
            CooldownLeft = Mathf.Max(0f, CooldownLeft - deltaTime);
            VisibleLeft = Mathf.Max(0f, VisibleLeft - deltaTime);
        }

        public void ClearVisibility() { VisibleLeft = 0f; }
        public void Reset() { CooldownLeft = 0f; VisibleLeft = 0f; }
    }
}
