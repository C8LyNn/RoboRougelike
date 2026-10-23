using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    public static class RRLEnemyDeviceMount
    {
        // Attach to an actual armor owner. Its detachment carries the device out
        // of the vehicle hierarchy, immediately revoking shared device availability.
        public static void Attach(GameObject item,Transform owner,R3PartState state)
        {
            var chassis=owner.Find("Base");if(chassis==null)return;
            var enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            var support=chassis.GetComponentsInChildren<P12PipelineSafeDamageable>()
                .Where(p=>!p.IsDead&&p!=enemy.Core&&p.GetComponent<RRLDeviceBinding>()==null)
                .OrderBy(p=>(p.transform.position-item.transform.position).sqrMagnitude).FirstOrDefault();
            if(support==null)return;var bounds=support.GetComponent<Collider>().bounds;
            var rotation=owner.rotation*R3MountingGrid.Rotation(state.RotationQuarterTurns);
            var center=new Vector3(bounds.center.x,bounds.max.y+.26f,bounds.center.z);
            item.transform.SetPositionAndRotation(center-rotation*new Vector3(.275f,0,.275f),rotation);
            item.transform.SetParent(support.transform,true);
        }
    }
}
