using UnityEngine;
using RoboRougelike.Mechanics.Rockets;

namespace RoboRougelike.WeaponIntegration
{
    // One receiver per actual P12 part. Original blast resolver deduplicates this identity.
    [DisallowMultipleComponent]
    public sealed class RRLRocketDamageReceiver : MonoBehaviour,IRocketDamageReceiver
    {
        Collider target;
        void Awake()=>target=GetComponent<Collider>();
        public void ReceiveRocketDamage(float damage,Vector3 point,Vector3 impulse,GameObject attacker)
        {
            if(target==null)return;
            var binding=attacker!=null?attacker.GetComponent<RRLRocketBinding>():null;
            Transform source=binding!=null?binding.Device.Owner:attacker!=null?attacker.transform:null;
            Vector3 direction=impulse.sqrMagnitude>.000001f?impulse.normalized:source!=null?(point-source.position).normalized:Vector3.up;
            var body=target.attachedRigidbody;
            if(RRLHitRouter.Apply(target,damage,point,direction,source)&&body!=null&&!body.isKinematic)
                body.AddForceAtPosition(impulse,point,ForceMode.Impulse);
        }
    }
}
