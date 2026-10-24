using UnityEngine;

namespace RoboRougelike.VFX001
{
    public sealed class ModuleLossFeedback : MonoBehaviour
    {
        [SerializeField] private VfxPoolManager pool;
        [SerializeField, Range(0f, 1f)] private float defaultIntensity = .65f;
        public VfxBurstActor PlayLostModuleFeedback(Vector3 position, Vector3 outwardNormal,
            ImpactMaterialType materialType = ImpactMaterialType.Metal, float? intensity = null)
        {
            if (pool == null) pool = GetComponentInParent<VfxPoolManager>();
            return pool == null ? null : pool.Emit(new VfxEvent(VfxEventKind.Detached,
                position, outwardNormal, materialType, intensity ?? defaultIntensity));
        }
    }
}
