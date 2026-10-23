using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    // Pure state model, independent of MonoBehaviour, Time and InputSystem.
    public sealed class ShotgunFireGate
    {
        public float NextReadyTime { get; private set; } = float.NegativeInfinity;
        public uint SuccessfulVolleys { get; private set; }
        public bool Operational { get; private set; } = true;

        public static bool IsFiniteTime(float seconds) =>
            !float.IsNaN(seconds) && !float.IsInfinity(seconds);

        public bool CanFire(float seconds) => Operational && IsFiniteTime(seconds)
            && seconds >= NextReadyTime;

        public bool RegisterSuccessfulVolley(float seconds, float cooldownSeconds)
        {
            if (!CanFire(seconds)) return false;
            float cd = float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds)
                ? 0.01f : Mathf.Clamp(cooldownSeconds, 0.01f, 30f);
            NextReadyTime = seconds + cd;
            SuccessfulVolleys++;
            return true;
        }

        public void SetOperational(bool value) => Operational = value;

        public void Reset()
        {
            NextReadyTime = float.NegativeInfinity;
            SuccessfulVolleys = 0;
            Operational = true;
        }
    }
}
