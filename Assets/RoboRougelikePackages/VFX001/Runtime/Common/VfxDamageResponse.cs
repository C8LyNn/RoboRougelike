using UnityEngine;

namespace RoboRougelike.VFX001
{
    public enum VfxDamageStage { Healthy = 0, Scratched = 1, Smoking = 2, Critical = 3, Detached = 4 }

    /// <summary>Read-only visual classification; the gameplay's HP system stays authoritative.</summary>
    public static class VfxDamageResponse
    {
        public static VfxDamageStage Classify(float health01, bool detached)
        {
            if (detached) return VfxDamageStage.Detached;
            float h = Mathf.Clamp01(health01);
            if (h <= .2f) return VfxDamageStage.Critical;
            if (h <= .55f) return VfxDamageStage.Smoking;
            if (h <= .85f) return VfxDamageStage.Scratched;
            return VfxDamageStage.Healthy;
        }
        public static float SmokeIntensity(float health01, bool detached)
        {
            if (detached) return 0f;
            return Mathf.Clamp01((.65f - Mathf.Clamp01(health01)) / .65f);
        }
        public static bool ShouldFlashOnDamage(VfxDamageStage previous, VfxDamageStage next)
        {
            return next > previous && next != VfxDamageStage.Detached;
        }
    }
}
