using UnityEngine;

namespace RoboRougelike.VFX001
{
    public struct VfxLayerSpec
    {
        public int Count;
        public float Speed;
        public float Lifetime;
        public float Size;
        public Color Color;

        public VfxLayerSpec(int count, float speed, float lifetime, float size, Color color)
        {
            Count = Mathf.Max(0, count);
            Speed = Mathf.Max(0f, speed);
            Lifetime = Mathf.Max(0.02f, lifetime);
            Size = Mathf.Max(0.001f, size);
            Color = color;
        }
    }

    public struct VfxPreset
    {
        public VfxLayerSpec Flash;
        public VfxLayerSpec Sparks;
        public VfxLayerSpec Smoke;
        public VfxLayerSpec Fragments;
        public float Duration;
    }

    public static class VfxPresetResolver
    {
        public static VfxPreset Resolve(VfxEvent ev)
        {
            float i = Mathf.Clamp01(ev.Intensity);
            Color spark = new Color(1f, .76f, .28f);
            Color smoke = new Color(.27f, .32f, .37f, .74f);
            Color fragment = new Color(.57f, .64f, .72f);
            switch (ev.Material)
            {
                case ImpactMaterialType.Rubber: spark = new Color(.26f, .28f, .3f); fragment = new Color(.14f, .15f, .16f); break;
                case ImpactMaterialType.Energy: spark = new Color(.25f, .76f, 1f); smoke = new Color(.19f, .35f, .54f, .52f); break;
                case ImpactMaterialType.Engine: spark = new Color(1f, .46f, .12f); smoke = new Color(.22f, .25f, .27f, .8f); break;
                case ImpactMaterialType.Core: spark = new Color(1f, .3f, .3f); smoke = new Color(.35f, .24f, .28f, .7f); break;
            }
            int scale = ev.Kind == VfxEventKind.CoreDestroyed ? 4 : ev.Kind == VfxEventKind.Explosion ? 3 : 1;
            bool isHit = ev.Kind == VfxEventKind.Hit;
            bool dust = ev.Kind == VfxEventKind.LandingDust;
            bool rubber = ev.Material == ImpactMaterialType.Rubber;
            return new VfxPreset
            {
                Flash = new VfxLayerSpec(dust || rubber ? 0 : Mathf.RoundToInt((1f + i) * (isHit ? 1 : 3)), .05f, .09f, (isHit ? .12f : .37f) * (1f + i), spark),
                Sparks = new VfxLayerSpec(dust ? 0 : Mathf.RoundToInt((4f + 10f * i) * scale * (rubber ? .45f : 1f)), 2f + 7f * i, .2f + .3f * i, .045f + .055f * i, spark),
                Smoke = new VfxLayerSpec(isHit ? (i > .65f ? 1 : 0) : Mathf.RoundToInt((dust ? 6f : 3f) * scale * (.6f + i)), dust ? .9f : 1.5f + i * 2f, dust ? .6f : 1f + i * 1.1f, dust ? .35f : .46f + i * .32f, dust ? new Color(.57f, .51f, .39f, .55f) : smoke),
                Fragments = new VfxLayerSpec(isHit || dust ? 0 : Mathf.RoundToInt((2f + i * 5f) * scale), 1.6f + i * 4f, .45f + i * .55f, .07f + i * .06f, fragment),
                Duration = isHit ? .85f : dust ? 1.1f : 2.8f
            };
        }
    }
}
