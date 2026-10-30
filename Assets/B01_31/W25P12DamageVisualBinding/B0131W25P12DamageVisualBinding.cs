#nullable disable
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.B0131.Art
{
    /// <summary>
    /// B01-31 W25: real read-only visual bridge from the existing P12 gameplay
    /// Damageable's actual CurrentHp/MaxHp/IsDead/Died source to W24 geometry.
    /// This never owns or changes game HP, unique component IDs, colliders,
    /// Rigidbody, Save/Load, deployment, wallet, rebuild vouchers, or rewards.
    /// No implicit FindObjectOfType: source must be explicitly supplied/bound.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(B0131W24ArmorDamageVisual))]
    public sealed class B0131W25P12DamageVisualBinding : MonoBehaviour
    {
        [SerializeField] private P12PipelineSafeDamageable source;
        [SerializeField] private B0131W24ArmorDamageVisual visual;
        [SerializeField, Range(0.05f, 0.95f)] private float damagedAtOrBelowHpRatio = 0.50f;

        private bool subscribed;
        private bool deathLatched;

        public bool IsBound => source != null && visual != null;
        public float DamageThreshold => damagedAtOrBelowHpRatio;

        public bool Bind(P12PipelineSafeDamageable newSource, B0131W24ArmorDamageVisual newVisual)
        {
            if (newSource == null || newVisual == null || !ValidThreshold(damagedAtOrBelowHpRatio))
                return false;

            if (source != newSource)
            {
                Unsubscribe();
                source = newSource;
                deathLatched = false;
            }
            visual = newVisual;
            if (isActiveAndEnabled) Subscribe();
            return RefreshVisual();
        }

        public bool SetDamageThreshold(float ratio)
        {
            if (!ValidThreshold(ratio)) return false;
            damagedAtOrBelowHpRatio = ratio;
            return RefreshVisual();
        }

        public bool RefreshVisual()
        {
            if (!IsBound || !ValidThreshold(damagedAtOrBelowHpRatio)) return false;
            if (deathLatched || source.IsDead)
            {
                deathLatched = true;
                return visual.SetVisualPhase(ArmorVisualPhase.Destroyed);
            }

            float hp = source.CurrentHp;
            float max = source.MaxHp;
            // Malformed or contradictory authority must NOT appear as a healthy part.
            if (float.IsNaN(hp) || float.IsInfinity(hp) ||
                float.IsNaN(max) || float.IsInfinity(max) ||
                max <= 0f || hp < 0f || hp > max)
                return false;

            ArmorVisualPhase phase = hp <= 0f
                ? ArmorVisualPhase.Destroyed
                : hp / max <= damagedAtOrBelowHpRatio
                    ? ArmorVisualPhase.Damaged : ArmorVisualPhase.Ok;
            return visual.SetVisualPhase(phase);
        }

        private static bool ValidThreshold(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value)
                && value >= 0.05f && value <= 0.95f;
        }

        private void Subscribe()
        {
            if (source == null || subscribed) return;
            source.Died += OnSourceDied;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (source != null && subscribed) source.Died -= OnSourceDied;
            subscribed = false;
        }

        private void OnSourceDied()
        {
            // Original P12 emits Died BEFORE detachment/destruction.
            deathLatched = true;
            RefreshVisual();
        }

        private void OnEnable()
        {
            if (source == null || visual == null)
                return;
            Subscribe();
            RefreshVisual();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            // Read actual P12 health changes/repair; no P12 damage/repair call.
            RefreshVisual();
        }
    }
}
