using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.MOV009Integration
{
    [DisallowMultipleComponent]
    public sealed class MOV009PartBinding : MonoBehaviour
    {
        R3MountedPart part;
        ActiveSuspensionModule module;
        public void Configure(R3MountedPart mounted,ActiveSuspensionModule support)
        {
            part=mounted;module=support;part.Damageable.Died+=DisableSupport;
            module.SetAvailabilityFilter(()=>part!=null&&part.Operational&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject));
            module.SetGroundFilter(IsTerrain);
        }
        static bool IsTerrain(Collider collider) => collider!=null && collider.attachedRigidbody==null &&
            collider.GetComponentInParent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>()==null &&
            collider.GetComponentInParent<R3Debris>()==null;
        void DisableSupport(){if(module!=null)module.SetOperational(false);}
        void OnDestroy(){if(part!=null&&part.Damageable!=null)part.Damageable.Died-=DisableSupport;}
    }
}
