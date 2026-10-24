using UnityEngine;

namespace RoboRougelike.VFX001
{
    public struct VfxCoreBeat
    {
        public float AtSeconds;
        public VfxEventKind Kind;
        public ImpactMaterialType Material;
        public float RelativeIntensity;

        public VfxCoreBeat(float atSeconds, VfxEventKind kind, ImpactMaterialType material, float relativeIntensity)
        {
            AtSeconds = atSeconds;
            Kind = kind;
            Material = material;
            RelativeIntensity = relativeIntensity;
        }
    }

    public static class VfxCoreDestructionTimeline
    {
        public const int BeatCount = 4;
        public const float LastBeatSeconds = .48f;

        public static VfxCoreBeat GetBeat(int index)
        {
            switch (index)
            {
                case 0: return new VfxCoreBeat(0f, VfxEventKind.Hit, ImpactMaterialType.Core, .8f);
                case 1: return new VfxCoreBeat(.13f, VfxEventKind.Explosion, ImpactMaterialType.Engine, .65f);
                case 2: return new VfxCoreBeat(.31f, VfxEventKind.CoreDestroyed, ImpactMaterialType.Core, 1f);
                case 3: return new VfxCoreBeat(.48f, VfxEventKind.Detached, ImpactMaterialType.Metal, .75f);
                default: throw new System.ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
