using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    // Pure state model, independent of MonoBehaviour, Time and InputSystem.
    public sealed class ShotgunFireGate
    {
        public float NextReadyTime { get; private set; } = float.NegativeInfinity;
        double nextReadyExact=double.NegativeInfinity;
        public uint SuccessfulVolleys { get; private set; }
        public bool Operational { get; private set; } = true;

        public static bool IsFiniteTime(float seconds) =>
            !float.IsNaN(seconds) && !float.IsInfinity(seconds);

        public bool CanFire(float seconds) => Operational && IsFiniteTime(seconds)
            && seconds >= nextReadyExact;

        public bool RegisterSuccessfulVolley(float seconds, float cooldownSeconds)
        {
            if (!CanFire(seconds)) return false;
            float cd = ShotgunSpec.PositiveCooldown(cooldownSeconds,.01f);
            // Preserve a positive interval even when float time cannot represent it.
            nextReadyExact=(double)seconds+System.Math.Max((double)cd,System.Math.Abs((double)seconds)*2.2204460492503131e-16);
            NextReadyTime=(float)System.Math.Min(float.MaxValue,nextReadyExact);
            SuccessfulVolleys++;
            return true;
        }

        public void SetOperational(bool value) => Operational = value;

        public void Reset()
        {
            NextReadyTime = float.NegativeInfinity;
            nextReadyExact=double.NegativeInfinity;
            SuccessfulVolleys = 0;
            Operational = true;
        }
    }
}
