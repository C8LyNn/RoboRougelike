using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Counts admissions per rendered frame, independent of game time scale.</summary>
    public sealed class VfxBudgetPolicy
    {
        public int MaxActive { get; private set; }
        public int MaxPerFrame { get; private set; }
        private int _frame = -1;
        private int _accepted;
        public int AcceptedThisFrame => _accepted;

        public VfxBudgetPolicy(int maxActive, int maxPerFrame)
        {
            MaxActive = Mathf.Max(1, maxActive);
            MaxPerFrame = Mathf.Max(1, maxPerFrame);
        }

        public bool TryAdmit(int frame, int activeCount, VfxEventKind kind)
        {
            if (frame != _frame) { _frame = frame; _accepted = 0; }
            if (activeCount >= MaxActive) return false;
            // High-priority core destruction may bypass the ordinary per-frame cap.
            if (_accepted >= MaxPerFrame && kind != VfxEventKind.CoreDestroyed) return false;
            _accepted++;
            return true;
        }
    }
}
