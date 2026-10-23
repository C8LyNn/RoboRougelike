using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.WPN002Railgun;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    // A mode of P17's bounded, encounter-owned physical projectile, not a demo projectile.
    public sealed class RRLRailPenetration : MonoBehaviour
    {
        readonly RaycastHit[] hits=new RaycastHit[128];
        readonly HashSet<int> visited=new HashSet<int>();
        RailgunShot shot;Rigidbody body;Vector3 position;float travelled,energy,damageScale;
        public Vector3 Velocity=>shot.InitialVelocity;
        public void Bind(RailgunShot value,float scale){shot=value;position=value.Origin;energy=value.Spec.InitialEnergy;damageScale=scale;body=GetComponent<Rigidbody>();}
        public void Step(float dt)
        {
            float move=Mathf.Min(shot.InitialVelocity.magnitude*dt,shot.Spec.Range-travelled);
            if(move<=0){Stop();return;}Vector3 direction=shot.InitialVelocity.normalized;
            int count=Physics.SphereCastNonAlloc(position,shot.Spec.Radius,direction,hits,move,~0,QueryTriggerInteraction.Ignore);
            if(count>=hits.Length){Stop();return;}
            for(int i=1;i<count;i++){var key=hits[i];int j=i-1;while(j>=0&&hits[j].distance>key.distance){hits[j+1]=hits[j];j--;}hits[j+1]=key;}
            float previous=0;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];var c=hit.collider;if(c==null||c.transform.IsChildOf(transform)||!RRLHitRouter.CanHit(c,shot.OwnerRoot.transform))continue;
                var hp=c.GetComponentInParent<P12PipelineSafeDamageable>();
                if(hp!=null&&visited.Contains(hp.GetInstanceID()))continue;
                energy=RailgunRules.AfterTravel(energy,hit.distance-previous,shot.Spec.EnergyLossPerMeter);previous=hit.distance;
                if(hp==null){RRLHitRouter.Apply(c,Mathf.Min(shot.Spec.MaxDamagePerPart,energy*shot.Spec.DamagePerEnergy)*damageScale,hit.point,direction,shot.OwnerRoot.transform);Stop();return;}
                visited.Add(hp.GetInstanceID());
                // Chassis resistance is integration tuning, read from existing physical HP.
                var impact=RailgunRules.Evaluate(energy,Mathf.Clamp(hp.MaxHp*.2f,5,60),shot.Spec.DamagePerEnergy,shot.Spec.MaxDamagePerPart,visited.Count-1,shot.Spec.MaxDistinctParts,shot.Spec.MinEnergyToContinue,true);
                float requested=impact.Damage*damageScale;
                float settled=hp.ApplyDamageResolved(requested,hit.point,direction);
                float absorbed=Mathf.Max(0,requested-settled);
                energy=Mathf.Max(0,impact.RemainingEnergy-absorbed/Mathf.Max(.001f,shot.Spec.DamagePerEnergy*damageScale));
                if(!impact.Continues||settled<=0||energy<=shot.Spec.MinEnergyToContinue){Stop();return;}
            }
            energy=RailgunRules.AfterTravel(energy,move-previous,shot.Spec.EnergyLossPerMeter);travelled+=move;position+=direction*move;
            body.MovePosition(position);
            if(energy<=shot.Spec.MinEnergyToContinue||travelled>=shot.Spec.Range-.0001f)Stop();
        }
        void Stop(){gameObject.SetActive(false);Destroy(gameObject);}
    }
}
