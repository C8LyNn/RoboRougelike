using UnityEngine;

namespace RoboRougelike.VFX001
{
    public sealed class ImpactFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private VfxPoolManager pool;
        [SerializeField] private Transform legacyEffectsRoot;
        public VfxBurstActor PlayImpactPooled(Vector3 position, Vector3 normal, ImpactMaterialType materialType, float impact01)
        {
            if (pool == null) pool = GetComponentInParent<VfxPoolManager>();
            return pool != null ? pool.Emit(new VfxEvent(VfxEventKind.Hit, position, normal, materialType, impact01)) : null;
        }
        public GameObject PlayImpact(Vector3 position, Vector3 normal, ImpactMaterialType materialType, float impact01)
        {
            var pooled = PlayImpactPooled(position, normal, materialType, impact01);
            if (pooled != null) return pooled.gameObject;
            if (pool != null) return null; // budget rejection must not allocate fallback
            return ProceduralImpactFactory.CreateImpact("Impact_" + materialType, position, normal,
                ImpactFeedbackResolver.Resolve(materialType, impact01), legacyEffectsRoot);
        }
    }
}
