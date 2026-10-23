using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    // Pure deterministic calculations, with no physics or Unity random state.
    public static class RailgunRules
    {
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static float ClampFinite(float v, float min, float max, float fallback)
            => Finite(v) ? Mathf.Clamp(v, min, max) : fallback;
        public static float AfterTravel(float energy, float travelledMeters, float lossPerMeter)
        {
            if (!Finite(energy) || !Finite(travelledMeters) || !Finite(lossPerMeter)) return 0f;
            if (energy <= 0f) return 0f;
            return Mathf.Max(0f, energy - Mathf.Max(0f, travelledMeters) * Mathf.Max(0f, lossPerMeter));
        }
        public static RailgunImpact Evaluate(float incomingEnergy, float resistance,
            float damagePerEnergy, float maxDamage, int partsBeforeImpact,
            int maximumParts, float minRemainingEnergy, bool penetrable)
        {
            if (!Finite(incomingEnergy) || incomingEnergy <= 0f)
                return new RailgunImpact(0f, 0f, false);
            float damage = Mathf.Min(Mathf.Max(0f, maxDamage),
                incomingEnergy * Mathf.Max(0f, damagePerEnergy));
            // At least a small positive cost per *distinct* part, never zero-cost penetration.
            float cost = Finite(resistance) ? Mathf.Max(0.01f, resistance) : incomingEnergy;
            float after = Mathf.Max(0f, incomingEnergy - cost);
            bool go = penetrable && partsBeforeImpact + 1 < Mathf.Max(1, maximumParts) &&
                after > Mathf.Max(0f, minRemainingEnergy);
            return new RailgunImpact(damage, after, go);
        }
    }

    public readonly struct RailgunImpact
    {
        public readonly float Damage, RemainingEnergy;
        public readonly bool Continues;
        public RailgunImpact(float damage, float energy, bool continues)
        { Damage = damage; RemainingEnergy = energy; Continues = continues; }
    }
}
