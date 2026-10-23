using UnityEngine;

namespace RoboRougelike.Defenses.DirectionalShield
{
    /// <summary>Model pojemności i regeneracji. Bez GameObject, łatwy do testowania EditMode.</summary>
    public sealed class ShieldState
    {
        public float Maximum { get; private set; }
        public float Charge { get; private set; }
        public float SecondsSinceAbsorption { get; private set; }

        public ShieldState(float maximum)
        {
            Maximum = ShieldMath.Finite(maximum) ? Mathf.Max(0f, maximum) : 0f;
            Charge = Maximum;
            SecondsSinceAbsorption = float.PositiveInfinity; // nowy generator może regenerować natychmiast
        }

        public void ResetFull()
        {
            Charge = Maximum;
            SecondsSinceAbsorption = float.PositiveInfinity;
        }

        public void SetCharge(float charge)
        {
            if (!ShieldMath.Finite(charge)) return;
            Charge = Mathf.Clamp(charge, 0f, Maximum);
        }
        public void RestoreState(float charge,float secondsSinceAbsorption)
        {
            SetCharge(charge);
            SecondsSinceAbsorption=float.IsPositiveInfinity(secondsSinceAbsorption)?float.PositiveInfinity:
                ShieldMath.Finite(secondsSinceAbsorption)?Mathf.Max(0,secondsSinceAbsorption):0;
        }

        /// <summary>Zużycie osłony i energii jest transakcyjne. Brak energii = brak absorpcji, brak zużycia osłony.</summary>
        public float Absorb(float damage, float maxPerHit, float energyPerAbsorbed,
            IShieldEnergySource energySource = null)
        {
            if (!ShieldMath.Finite(damage) || damage <= 0f ||
                !ShieldMath.Finite(maxPerHit) || maxPerHit <= 0f ||
                !ShieldMath.Finite(energyPerAbsorbed) || energyPerAbsorbed < 0f)
                return 0f;

            float absorbed = Mathf.Min(damage, Charge, maxPerHit);
            if (absorbed <= 0f) return 0f;

            if (energyPerAbsorbed > 0f)
            {
                float energyRequired = absorbed * energyPerAbsorbed;
                if (!ShieldMath.Finite(energyRequired) || energySource == null ||
                    !energySource.TryConsume(energyRequired)) return 0f;
            }

            Charge = Mathf.Max(0f, Charge - absorbed);
            SecondsSinceAbsorption = 0f;
            return absorbed;
        }

        /// <summary>Regeneracja po opóźnieniu. W kroku przekraczającym delay regeneruje tylko pozostały czas.</summary>
        public float Advance(float deltaSeconds, float regenerationPerSecond, float delaySeconds,
            float energyPerRegenerated = 0f, IShieldEnergySource energySource = null)
        {
            if (!ShieldMath.Finite(deltaSeconds) || deltaSeconds <= 0f ||
                !ShieldMath.Finite(regenerationPerSecond) || regenerationPerSecond < 0f ||
                !ShieldMath.Finite(delaySeconds) || delaySeconds < 0f ||
                !ShieldMath.Finite(energyPerRegenerated) || energyPerRegenerated < 0f)
                return 0f;

            float oldTimer = SecondsSinceAbsorption;
            SecondsSinceAbsorption += deltaSeconds;
            float eligibleSeconds = float.IsPositiveInfinity(oldTimer)
                ? deltaSeconds
                : Mathf.Max(0f, deltaSeconds - Mathf.Max(0f, delaySeconds - oldTimer));
            float amount = Mathf.Min(Maximum - Charge, regenerationPerSecond * eligibleSeconds);
            if (!ShieldMath.Finite(amount) || amount <= 0f) return 0f;

            if (energyPerRegenerated > 0f)
            {
                float cost = amount * energyPerRegenerated;
                if (!ShieldMath.Finite(cost) || energySource == null || !energySource.TryConsume(cost))
                    return 0f;
            }
            Charge = Mathf.Min(Maximum, Charge + amount);
            return amount;
        }
    }
}
