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
        public static bool Attach(GameObject item,Transform owner,R3PartState state)
        {
            if(item==null||owner==null||state==null)return false;
            var chassis=owner.Find("Base");if(chassis==null)return false;
            var enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            if(enemy==null)return false;
            var support=chassis.GetComponentsInChildren<P12PipelineSafeDamageable>()
                .Where(p=>!p.IsDead&&p!=enemy.Core&&p.GetComponent<RRLDeviceBinding>()==null&&
                    !RoboRougelike.R6.R6VehicleDrive.IsWheel(p)&&p.GetComponent<Collider>()!=null&&
                    (p.GetComponent<R3BlockMountPoints>()==null||
                     (p.GetComponent<R3BlockMountPoints>().WorldMask&R3MountingGrid.FaceBit(Vector3Int.up))!=0))
                .OrderBy(p=>(p.transform.position-item.transform.position).sqrMagnitude).FirstOrDefault();
            if(support==null)return false;var bounds=support.GetComponent<Collider>().bounds;
            var rotation=owner.rotation*R3MountingGrid.Rotation(state.RotationQuarterTurns);
            var center=new Vector3(bounds.center.x,bounds.max.y+.26f,bounds.center.z);
            item.transform.SetPositionAndRotation(center,rotation);
            item.transform.SetParent(support.transform,true);
            return true;
        }
    }
}
