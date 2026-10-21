using UnityEngine;

namespace RoboRougelike.VFX001
{
    public sealed class ExplosionFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private VfxPoolManager pool;
        private VfxPoolManager ResolvePool()
        {
            if (pool == null) pool = GetComponentInParent<VfxPoolManager>();
            return pool;
        }
        public VfxBurstActor PlayExplosion(Vector3 position, Vector3 normal,
            ImpactMaterialType materialType, float intensity01, bool isCore = false)
        {
            var manager = ResolvePool();
            if (manager == null) { Debug.LogWarning("VFX-001: explosion requires a VfxPoolManager.", this); return null; }
            return manager.Emit(new VfxEvent(isCore ? VfxEventKind.CoreDestroyed : VfxEventKind.Explosion,
                position, normal, materialType, intensity01));
        }
        public VfxBurstActor PlayLandingDust(Vector3 position, Vector3 surfaceNormal, float intensity01)
        {
            var manager = ResolvePool();
            return manager == null ? null : manager.Emit(new VfxEvent(VfxEventKind.LandingDust,
                position, surfaceNormal, ImpactMaterialType.Default, intensity01));
        }
    }
}
