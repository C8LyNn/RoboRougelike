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
            var itemCollider=item.GetComponent<Collider>();if(itemCollider==null)return false;
            // Spawned chassis and device colliders have just been scaled and
            // positioned; queries must see those transforms in this same frame.
            Physics.SyncTransforms();
            var blockers=owner.GetComponentsInChildren<P12PipelineSafeDamageable>();
            var supports=chassis.GetComponentsInChildren<P12PipelineSafeDamageable>()
                .Where(p=>!p.IsDead&&p!=enemy.Core&&p.GetComponent<RRLDeviceBinding>()==null&&
                    !RoboRougelike.R6.R6VehicleDrive.IsWheel(p)&&p.GetComponent<Collider>()!=null&&p.GetComponent<Collider>().enabled&&
                    (p.GetComponent<R3BlockMountPoints>()==null||
                     (p.GetComponent<R3BlockMountPoints>().WorldMask&R3MountingGrid.FaceBit(Vector3Int.up))!=0))
                .OrderBy(p=>(p.transform.position-item.transform.position).sqrMagnitude);
            var rotation=owner.rotation*R3MountingGrid.Rotation(state.RotationQuarterTurns);
            foreach(var support in supports)
            {
                var bounds=support.GetComponent<Collider>().bounds;
                var center=new Vector3(bounds.center.x,bounds.max.y+.26f,bounds.center.z);
                bool blocked=false;
                foreach(var part in blockers)
                {
                    if(part==null||part==support||part.IsDead)continue;
                    var collider=part.GetComponent<Collider>();
                    if(collider==null||!collider.enabled||collider.isTrigger)continue;
                    if(Physics.ComputePenetration(itemCollider,center,rotation,collider,collider.transform.position,
                        collider.transform.rotation,out _,out float depth)&&depth>.002f)
                    {blocked=true;break;}
                }
                if(blocked)continue;
                item.transform.SetPositionAndRotation(center,rotation);
                item.transform.SetParent(support.transform,true);
                return true;
            }
            return false;
        }
    }
}
