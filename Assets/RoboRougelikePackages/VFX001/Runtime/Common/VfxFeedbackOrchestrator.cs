using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Optional read-only visual adapter. Game code calls Emit AFTER it resolves damage,
    /// detachment and contacts. Never subscribes to or changes gameplay physics itself.
    /// Individual optional receivers may be left unassigned.
    /// </summary>
    public sealed class VfxFeedbackOrchestrator : MonoBehaviour
    {
        [SerializeField] private VfxPoolManager pool;
        [SerializeField] private VfxCameraImpulseReceiver cameraImpulses;
        [SerializeField] private VfxMechanicalJolt mechanicalJolt;
        [SerializeField] private VfxHeatImpactController heatOverlay;
        [SerializeField] private Transform localMechanismSpace;
        public void Bind(VfxPoolManager effects, VfxCameraImpulseReceiver camera = null,
            VfxMechanicalJolt mechanics = null, VfxHeatImpactController heat = null)
        {
            pool = effects;
            cameraImpulses = camera;
            mechanicalJolt = mechanics;
            heatOverlay = heat;
        }

        public bool Emit(VfxEvent evt)
        {
            // Keep incidental camera shake and heat from accumulating if the VFX budget rejects the burst.
            var actor = pool != null ? pool.Emit(evt) : null;
            if (actor == null) return false;
            if (cameraImpulses != null) cameraImpulses.Accept(evt);
            if (evt.Kind == VfxEventKind.Hit)
            {
                if (mechanicalJolt != null)
                {
                    var frame = localMechanismSpace != null ? localMechanismSpace : mechanicalJolt.transform;
                    var local = frame.InverseTransformDirection(-evt.Normal);
                    mechanicalJolt.React(evt.Intensity, local);
                }
                if (heatOverlay != null)
                    heatOverlay.PlayHeat(evt.Position, evt.Normal, evt.Intensity, evt.Material);
            }
            return true;
        }
    }
}
