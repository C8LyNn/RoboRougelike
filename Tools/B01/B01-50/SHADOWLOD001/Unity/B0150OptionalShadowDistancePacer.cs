// Unity 6000.3.14f1 URP opt-in sandbox component, no canonical Assets edits.
// Exclusively optional visual shadows; do not use on essential telegraphs or occluders.
#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering;
using B01.Performance.ShadowBudget;

namespace B01.Performance.ShadowBudget.UnityAdapter
{
    public sealed class B0150OptionalShadowDistancePacer : MonoBehaviour
    {
        [SerializeField] private Transform observingCamera;
        [SerializeField] private Renderer[] optionalDecoration = Array.Empty<Renderer>();
        [SerializeField, Range(1,128)] private int renderersCheckedPerFrame = 16;
        [SerializeField, Min(0f)] private float enableWithinMeters = 25f;
        [SerializeField, Min(0.1f)] private float disableBeyondMeters = 35f;

        private ShadowDistanceBudget<Renderer> scheduler;
        private Renderer[] registered;
        private ShadowCastingMode[] originalModes;

        public int RegisteredDecorationCount => registered == null ? 0 : registered.Length;
        public long Evaluated => scheduler == null ? 0 : scheduler.Checks;
        public long DisabledShadows => scheduler == null ? 0 : scheduler.Disabled;
        public long ReenabledShadows => scheduler == null ? 0 : scheduler.Reenabled;

        private void OnEnable() { Rebuild(); }

        // All renderers must be user-selected, dispensable decorations and NOT controlled
        // by another game system. Preserve original TwoSided mode on near/restore.
        public void SetOptionalDecoration(Renderer[] eligibleRenderers, Transform cameraTransform)
        {
            if (!Application.isPlaying) return;
            RestoreAndClear();
            optionalDecoration = eligibleRenderers ?? Array.Empty<Renderer>();
            observingCamera = cameraTransform;
            if (isActiveAndEnabled) Rebuild();
        }

        private void Rebuild()
        {
            RestoreAndClear();
            if (observingCamera == null || optionalDecoration == null || optionalDecoration.Length == 0)
                return;
            int max = Mathf.Min(optionalDecoration.Length, 4096);
            registered = new Renderer[max];
            originalModes = new ShadowCastingMode[max];
            int n = 0;
            for (int i = 0; i < optionalDecoration.Length && n < max; i++)
            {
                Renderer renderer = optionalDecoration[i];
                if (renderer == null || !renderer.enabled) continue;
                ShadowCastingMode mode = renderer.shadowCastingMode;
                // Off was already disabled; ShadowsOnly would disappear completely.
                if (mode == ShadowCastingMode.Off || mode == ShadowCastingMode.ShadowsOnly) continue;
                bool already = false;
                for (int j = 0; j < n; j++)
                    if (registered[j] == renderer) { already = true; break; }
                if (already) continue;
                registered[n] = renderer;
                originalModes[n++] = mode;
            }
            if (n == 0) { registered = Array.Empty<Renderer>(); originalModes = Array.Empty<ShadowCastingMode>(); return; }
            Array.Resize(ref registered, n);
            Array.Resize(ref originalModes, n);
            float near = Mathf.Max(0, enableWithinMeters);
            float far = Mathf.Max(near + 0.01f, disableBeyondMeters);
            scheduler = new ShadowDistanceBudget<Renderer>(n,
                Mathf.Clamp(renderersCheckedPerFrame, 1, 128), near, far);
            foreach (Renderer renderer in registered) scheduler.Register(renderer);
        }

        private double DistanceToRenderer(Renderer renderer)
        {
            if (renderer == null || observingCamera == null) return 0d;
            return Vector3.Distance(observingCamera.position, renderer.bounds.center);
        }

        private void ApplyShadowOnly(Renderer renderer, bool enable)
        {
            if (renderer == null) return;
            if (!enable) { renderer.shadowCastingMode = ShadowCastingMode.Off; return; }
            // Retain original TwoSided/On, never force an arbitrary new shadow setting.
            for (int i = 0; i < registered.Length; i++)
                if (registered[i] == renderer)
                { renderer.shadowCastingMode = originalModes[i]; return; }
        }

        private void Update()
        {
            if (scheduler != null && observingCamera != null)
                scheduler.EvaluateFrame(DistanceToRenderer, ApplyShadowOnly);
        }

        private void RestoreAndClear()
        {
            if (registered != null && originalModes != null)
                for (int i = 0; i < registered.Length; i++)
                    if (registered[i] != null) registered[i].shadowCastingMode = originalModes[i];
            scheduler = null;
            registered = null;
            originalModes = null;
        }

        private void OnDisable() { RestoreAndClear(); }
        private void OnDestroy() { RestoreAndClear(); }
    }
}
#endif
