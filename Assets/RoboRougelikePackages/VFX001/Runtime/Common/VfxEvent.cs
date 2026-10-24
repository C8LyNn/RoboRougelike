using UnityEngine;

namespace RoboRougelike.VFX001
{
    public enum VfxEventKind { Hit = 0, Detached = 1, Explosion = 2, CoreDestroyed = 3, LandingDust = 4 }
    public enum ImpactMaterialType { Default = 0, Metal = 1, Rubber = 2, Energy = 3, Engine = 4, Core = 5 }

    /// <summary>Purely cosmetic event. Never applies damage or changes Rigidbody state.</summary>
    public struct VfxEvent
    {
        public VfxEventKind Kind;
        public Vector3 Position;
        public Vector3 Normal;
        public ImpactMaterialType Material;
        public float Intensity;
        public int SourceId;
        public int SequenceId;

        public VfxEvent(VfxEventKind kind, Vector3 position, Vector3 normal,
            ImpactMaterialType material = ImpactMaterialType.Default, float intensity = 0.5f,
            int sourceId = 0, int sequenceId = 0)
        {
            Kind = kind;
            Position = position;
            Normal = normal.sqrMagnitude > 0.00001f ? normal.normalized : Vector3.up;
            Material = material;
            Intensity = Mathf.Clamp01(intensity);
            SourceId = sourceId;
            SequenceId = sequenceId;
        }
    }
}
