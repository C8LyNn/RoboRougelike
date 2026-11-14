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
            float cd = ShotgunSpec.NonNegativeCooldown(cooldownSeconds,.01f);
            // Even zero cooldown may fire only once for one clock value.
            double tick=System.Math.Max(double.Epsilon,System.Math.Abs((double)seconds)*2.2204460492503131e-16);
            nextReadyExact=(double)seconds+System.Math.Max((double)cd,tick);
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
