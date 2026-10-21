using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.COV001
{
    public enum CoverDamageStage { Intact, Damaged, Critical, Broken }

    /// <summary>
    /// Opt-in arena obstacle only. Never modifies vehicle modules, weapons or damage routing.
    /// The caller owns hit detection and invokes ApplyDamage explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DestructibleArenaCover : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxDurability = 50f;
        [SerializeField, Range(0f, 1f)] private float damagedAtOrBelow = 0.66f;
        [SerializeField, Range(0f, 1f)] private float criticalAtOrBelow = 0.33f;
        [Tooltip("Optional explicit blocking colliders; if empty, non-trigger child colliders are used.")]
        [SerializeField] private Collider[] explicitBlockers;
        [Header("Optional stage renderers (visuals only, no physics changes)")]
        [SerializeField] private Renderer[] intactVisuals;
        [SerializeField] private Renderer[] damagedVisuals;
        [SerializeField] private Renderer[] criticalVisuals;
        [SerializeField] private Renderer[] brokenVisuals;
        [SerializeField] private UnityEvent onDamaged = new UnityEvent();
        [SerializeField] private UnityEvent onBroken = new UnityEvent();

        private Collider[] blockingColliders;
        private bool[] initialColliderStates;
        private bool initialized;

        public float MaxDurability =>
            float.IsNaN(maxDurability) || float.IsInfinity(maxDurability)
                ? 50f : Mathf.Max(1f, maxDurability);
        public float Durability { get; private set; }
        public bool Broken => Stage == CoverDamageStage.Broken;
        public CoverDamageStage Stage { get; private set; }
        public event Action<float> DamageAccepted;
        public event Action<CoverDamageStage> StageChanged;
        public event Action BrokenOnce;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (initialized) return;
            var source = explicitBlockers != null && explicitBlockers.Length > 0
                ? explicitBlockers : GetComponentsInChildren<Collider>(true);
            var selected = new System.Collections.Generic.List<Collider>(source.Length);
            foreach (var candidate in source)
            {
                if (candidate == null || candidate.isTrigger || selected.Contains(candidate)) continue;
                // Never disable a collider belonging to a separate nested cover.
                var owner = candidate.GetComponentInParent<DestructibleArenaCover>();
                if (owner != this) continue;
                selected.Add(candidate);
            }
            blockingColliders = selected.ToArray();
            initialColliderStates = new bool[blockingColliders.Length];
            for (int i = 0; i < blockingColliders.Length; i++)
                initialColliderStates[i] = blockingColliders[i].enabled;
            Durability = MaxDurability;
            Stage = CoverDamageStage.Intact;
            initialized = true;
            UpdateVisuals();
        }

        /// <summary>Returns false for invalid damage or an already broken cover.</summary>
        public bool ApplyDamage(float amount)
        {
            EnsureInitialized();
            if (Broken || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
                return false;

            float applied = Mathf.Min(amount, Durability);
            Durability = Mathf.Max(0f, Durability - applied);
            var next = ResolveStage();
            bool changed = next != Stage;
            Stage = next;
            if (Broken)
                for (int i = 0; i < blockingColliders.Length; i++)
                    if (blockingColliders[i] != null) blockingColliders[i].enabled = false;

            if (changed) UpdateVisuals();
            DamageAccepted?.Invoke(applied);
            onDamaged.Invoke();
            if (changed) StageChanged?.Invoke(Stage);
            if (Broken)
            {
                BrokenOnce?.Invoke();
                onBroken.Invoke();
            }
            return true;
        }

        public void ResetCover()
        {
            EnsureInitialized();
            bool changed = Stage != CoverDamageStage.Intact;
            Durability = MaxDurability;
            Stage = CoverDamageStage.Intact;
            for (int i = 0; i < blockingColliders.Length; i++)
                if (blockingColliders[i] != null)
                    blockingColliders[i].enabled = initialColliderStates[i];
            UpdateVisuals();
            if (changed) StageChanged?.Invoke(Stage);
        }

        private CoverDamageStage ResolveStage()
        {
            if (Durability <= 0f) return CoverDamageStage.Broken;
            float ratio = Durability / MaxDurability;
            float critical = Mathf.Clamp01(criticalAtOrBelow);
            float damaged = Mathf.Max(critical, Mathf.Clamp01(damagedAtOrBelow));
            if (ratio <= critical) return CoverDamageStage.Critical;
            if (ratio <= damaged) return CoverDamageStage.Damaged;
            return CoverDamageStage.Intact;
        }

        private void UpdateVisuals()
        {
            SetRenderers(intactVisuals, Stage == CoverDamageStage.Intact);
            SetRenderers(damagedVisuals, Stage == CoverDamageStage.Damaged);
            SetRenderers(criticalVisuals, Stage == CoverDamageStage.Critical);
            SetRenderers(brokenVisuals, Stage == CoverDamageStage.Broken);
        }

        private static void SetRenderers(Renderer[] renderers, bool visible)
        {
            if (renderers == null) return;
            foreach (var renderer in renderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}
