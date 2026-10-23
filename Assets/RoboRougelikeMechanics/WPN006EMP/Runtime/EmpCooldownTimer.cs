using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    public sealed class EmpCooldownTimer
    {
        public float Remaining { get; private set; }
        public bool Ready => Remaining <= 0f;

        public void Commit(float seconds)
        {
            Remaining = EmpPulseMath.IsValidFinite(seconds) ? Mathf.Max(0f, seconds) : 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!EmpPulseMath.IsValidFinite(deltaTime) || deltaTime <= 0f) return;
            Remaining = Mathf.Max(0f, Remaining - deltaTime);
        }
    }
}
