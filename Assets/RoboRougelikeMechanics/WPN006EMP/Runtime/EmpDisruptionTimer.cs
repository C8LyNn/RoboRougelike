using UnityEngine;

namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>Non-MonoBehaviour EMP duration; repeated hits refresh to max duration, not stack additively.</summary>
    public sealed class EmpDisruptionTimer
    {
        public float Remaining { get; private set; }
        public bool IsDisrupted => Remaining > 0f;

        public bool Apply(float duration)
        {
            if (!EmpPulseMath.IsValidFinite(duration) || duration <= 0f) return false;
            bool newlyStarted = !IsDisrupted;
            Remaining = Mathf.Max(Remaining, duration);
            return newlyStarted;
        }

        /// <returns>True if a running disruption ended in this step.</returns>
        public bool Tick(float deltaTime)
        {
            if (!EmpPulseMath.IsValidFinite(deltaTime) || deltaTime <= 0f || !IsDisrupted) return false;
            Remaining = Mathf.Max(0f, Remaining - deltaTime);
            return !IsDisrupted;
        }

        public void Clear() => Remaining = 0f;
    }
}
