// Unity 6000.3.14f1 URP optional-only ParticleSystem emission pacing.
// No canonical game modification or gameplay-critical particle suppression.
#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;
using B01.Performance.ParticleEmission;

namespace B01.Performance.ParticleEmission.UnityAdapter
{
    public sealed class B0150OptionalParticleEmissionLod : MonoBehaviour
    {
        [SerializeField] private Transform observingCamera;
        [SerializeField] private ParticleSystem[] optionalDecorativeEmitters = Array.Empty<ParticleSystem>();
        [SerializeField, Range(1,128)] private int inspectedEmittersPerFrame = 16;
        [SerializeField, Min(0.1f)] private float fullRateThroughMeters = 18f;
        [SerializeField, Min(0.2f)] private float zeroRateBeyondMeters = 38f;
        [SerializeField, Min(0f)] private float hysteresisMeters = 2f;
        [SerializeField, Range(0f,1f)] private float mediumRateFraction = 0.35f;
        [SerializeField, Range(0f,1f)] private float farRateFraction = 0f;

        private OptionalParticleEmissionBudget<ParticleSystem> scheduler;
        private ParticleSystem[] registered;
        private float[] originalMultipliers;

        public int RegisteredEmitters => registered == null ? 0 : registered.Length;
        public long Inspections => scheduler == null ? 0 : scheduler.Checks;
        public long EmissionApplications => scheduler == null ? 0 : scheduler.Applications;

        private void OnEnable() { Rebuild(); }

        // This API can only opt into manual, NONESSENTIAL emission.
        // Never use for boss warnings, aim markers, hit/physics, collision, triggers or gameplay state.
        public void SetOptionalDecorations(ParticleSystem[] approved, Transform cameraTransform)
        {
            if (!Application.isPlaying) return;
            RestoreAndClear();
            optionalDecorativeEmitters = approved ?? Array.Empty<ParticleSystem>();
            observingCamera = cameraTransform;
            if (isActiveAndEnabled) Rebuild();
        }

        private void Rebuild()
        {
            RestoreAndClear();
            if (observingCamera == null || optionalDecorativeEmitters == null) return;
            int max = Mathf.Min(4096, optionalDecorativeEmitters.Length);
            if (max < 1) return;
            registered = new ParticleSystem[max];
            originalMultipliers = new float[max];
            int count = 0;
            for (int i = 0; i < optionalDecorativeEmitters.Length && count < max; ++i)
            {
                ParticleSystem emitter = optionalDecorativeEmitters[i];
                if (emitter == null || !emitter.gameObject.activeInHierarchy) continue;
                var emission = emitter.emission;
                if (!emission.enabled) continue;
                float original = emission.rateOverTimeMultiplier;
                if (float.IsNaN(original) || float.IsInfinity(original) || original < 0f || original > 10000f)
                    continue;
                bool duplicate = false;
                for (int j = 0; j < count; j++)
                    if (registered[j] == emitter) { duplicate = true; break; }
                if (duplicate) continue;
                registered[count] = emitter;
                originalMultipliers[count] = original;
                count++;
            }
            if (count == 0) { registered = Array.Empty<ParticleSystem>(); originalMultipliers = Array.Empty<float>(); return; }
            Array.Resize(ref registered, count);
            Array.Resize(ref originalMultipliers, count);
            double near = Math.Max(0.1, fullRateThroughMeters);
            double hysteresis = Math.Max(0, Math.Min(hysteresisMeters, near * 0.49));
            double far = Math.Max(near + 2 * hysteresis + 0.02, zeroRateBeyondMeters);
            scheduler = new OptionalParticleEmissionBudget<ParticleSystem>(count,
                Mathf.Clamp(inspectedEmittersPerFrame, 1, 128), near, far, hysteresis,
                Mathf.Clamp01(mediumRateFraction),
                Mathf.Min(Mathf.Clamp01(mediumRateFraction), Mathf.Clamp01(farRateFraction)));
            for (int i = 0; i < count; i++) scheduler.Register(registered[i], originalMultipliers[i]);
        }

        private double DistanceToEmitter(ParticleSystem emitter)
        {
            if (emitter == null || observingCamera == null) return 0d;
            return Vector3.Distance(observingCamera.position, emitter.transform.position);
        }

        private static void SetMultiplier(ParticleSystem emitter, double value)
        {
            if (emitter == null) return; // destroyed source; never touch unrelated source
            var emission = emitter.emission;
            emission.rateOverTimeMultiplier = (float)value;
        }

        private void Update()
        {
            if (scheduler != null && observingCamera != null)
                scheduler.EvaluateFrame(DistanceToEmitter, SetMultiplier);
        }

        private void RestoreAndClear()
        {
            if (registered != null && originalMultipliers != null)
            {
                for (int i = 0; i < registered.Length; i++)
                {
                    ParticleSystem emitter = registered[i];
                    if (emitter == null) continue;
                    var emission = emitter.emission;
                    emission.rateOverTimeMultiplier = originalMultipliers[i];
                }
            }
            scheduler = null;
            registered = null;
            originalMultipliers = null;
        }

        private void OnDisable() { RestoreAndClear(); }
        private void OnDestroy() { RestoreAndClear(); }
    }
}
#endif
