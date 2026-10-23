using UnityEngine;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    public interface IRRLHitReceiver
    {
        bool ReceiveHit(float damage,Vector3 point,Vector3 direction,Transform source);
    }
    // One real collider is resolved to one physical owner. Arena/objective adapters
    // may own their original durability; vehicle HP always remains P12.
    public static class RRLHitRouter
    {
        public static int Team(Transform root)
        {
            if(root==null)return 0;
            if(root.GetComponentInParent<P12PipelineSafeEnemyTurret>()!=null)return 2;
            if(root.GetComponentInParent<R1VehicleState>()!=null)return 1;
            return 0;
        }
        public static bool CanHit(Collider collider,Transform source)
        {
            if(collider==null||!collider.enabled||!collider.gameObject.activeInHierarchy)return false;
            if(source!=null&&(collider.transform==source||collider.transform.IsChildOf(source)))return false;
            if(collider.GetComponentInParent<RoboRougelike.R3.R3Debris>()!=null)return false;
            var hp=collider.GetComponentInParent<P12PipelineSafeDamageable>();
            if(hp!=null&&hp.IsDead)return false;
            int a=Team(source),b=Team(collider.transform);
            return a==0||b==0||a!=b;
        }
        public static bool Apply(Collider collider,float damage,Vector3 point,Vector3 direction,Transform source)
        {
            if(!CanHit(collider,source)||damage<=0||float.IsNaN(damage)||float.IsInfinity(damage))return false;
            var receiver=collider.GetComponentInParent<IRRLHitReceiver>();
            if(receiver!=null)return receiver.ReceiveHit(damage,point,direction,source);
            var hp=collider.GetComponentInParent<P12PipelineSafeDamageable>();
            if(hp==null)return false;
            hp.ApplyDamage(damage,point,direction);return true;
        }
    }
}
