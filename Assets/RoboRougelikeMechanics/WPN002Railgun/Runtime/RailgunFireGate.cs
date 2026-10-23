using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    public sealed class RailgunFireGate
    {
        public bool Operational { get; private set; } = true;
        public uint SuccessfulShots { get; private set; }
        public float NextReadyTime { get; private set; } = float.NegativeInfinity;
        public void SetOperational(bool active) => Operational = active;
        public bool CanFire(float now) => Operational && RailgunRules.Finite(now) && now >= NextReadyTime;
        public bool Commit(float now, float cooldownSeconds)
        {
            if (!CanFire(now)) return false;
            NextReadyTime = now + RailgunRules.ClampFinite(cooldownSeconds, 0.01f, 120f, 1f);
            SuccessfulShots++;
            return true;
        }
        public void Reset() { NextReadyTime = float.NegativeInfinity; SuccessfulShots = 0; Operational = true; }
    }
}
