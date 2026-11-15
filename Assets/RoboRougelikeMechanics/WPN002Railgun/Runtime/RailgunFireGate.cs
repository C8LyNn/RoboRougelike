using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    public sealed class RailgunFireGate
    {
        public bool Operational { get; private set; } = true;
        public uint SuccessfulShots { get; private set; }
        public float NextReadyTime { get; private set; } = float.NegativeInfinity;
        double nextReadyExact=double.NegativeInfinity;
        public void SetOperational(bool active) => Operational = active;
        public bool CanFire(float now) => Operational && RailgunRules.Finite(now) && now >= nextReadyExact;
        public bool Commit(float now, float cooldownSeconds)
        {
            if (!CanFire(now)) return false;
            float cd=RailgunRules.NonNegativeCooldown(cooldownSeconds,1f);
            double tick=System.Math.Max(double.Epsilon,System.Math.Abs((double)now)*2.2204460492503131e-16);
            nextReadyExact=(double)now+System.Math.Max((double)cd,tick);
            NextReadyTime=(float)System.Math.Min(float.MaxValue,nextReadyExact);
            SuccessfulShots++;
            return true;
        }
        public void Reset() { NextReadyTime = float.NegativeInfinity; nextReadyExact=double.NegativeInfinity; SuccessfulShots = 0; Operational = true; }
    }
}
