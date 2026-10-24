using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Cosmetic result. Never changes part HP or collision geometry.</summary>
    public struct VfxScarProfile
    {
        public Color Tint;
        public float Diameter;
        public float Opacity;
        public float CrackStrength;
    }

    public static class VfxScarResolver
    {
        public static VfxScarProfile Resolve(ImpactMaterialType material, float intensity01)
        {
            float t = Mathf.Clamp01(intensity01);
            Color c;
            float crack = Mathf.Lerp(0.1f, 0.95f, t);
            switch (material)
            {
                case ImpactMaterialType.Rubber: c = new Color(.10f, .095f, .10f, 1f); crack *= .28f; break;
                case ImpactMaterialType.Energy: c = new Color(.16f, .38f, .6f, 1f); crack *= .55f; break;
                case ImpactMaterialType.Engine: c = new Color(.29f, .12f, .055f, 1f); break;
                case ImpactMaterialType.Core: c = new Color(.28f, .08f, .12f, 1f); break;
                default: c = new Color(.08f, .075f, .08f, 1f); break;
            }
            return new VfxScarProfile
            {
                Tint = c,
                Diameter = Mathf.Lerp(.12f, .48f, t),
                Opacity = Mathf.Lerp(.30f, .9f, t),
                CrackStrength = crack
            };
        }
    }
}
