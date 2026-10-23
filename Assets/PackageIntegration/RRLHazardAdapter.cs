using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.HAZ001;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.PackageIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLHazardAdapter : MonoBehaviour
    {
        TelegraphedHazardZone zone;
        HazardRingIndicator indicator;
        readonly Dictionary<GameObject,Collider[]> targets=new Dictionary<GameObject,Collider[]>();
        void Awake()
        {
            zone=gameObject.AddComponent<TelegraphedHazardZone>();
            zone.Configure(1.5f,2,3,3,.5f,5,~0);
            indicator=gameObject.AddComponent<HazardRingIndicator>();
            zone.DamageRequested+=Damage;
        }
        static bool InCombat()
        {
            var run=R1RunController.Instance;
            return run?.Progress?.State==R1RunState.Combat&&!run.Transitioning;
        }
        void Update()
        {
            bool active=InCombat();zone.enabled=active;indicator.enabled=active;
            // Original Update/FixedUpdate own the only clock and scan cadence.
            // TimeScale freezes a live warning/active cycle while paused.
            if(active&&!R1RunController.Instance.Paused&&zone.Phase==HazardPhase.Idle)zone.Begin();
        }
        void Damage(GameObject target,float amount)
        {
            if(!InCombat()||R1RunController.Instance.Paused||target==null||target.GetComponent<R3Debris>()!=null)return;
            var body=target.GetComponent<Rigidbody>();if(body==null)return;
            if(!targets.TryGetValue(target,out var colliders))
            {colliders=target.GetComponentsInChildren<Collider>();targets.Add(target,colliders);}
            Collider closest=null;float distance=zone.Radius*zone.Radius;Vector3 point=transform.position;
            foreach(var candidate in colliders)
            {
                if(candidate==null||candidate.isTrigger||candidate.attachedRigidbody!=body||!candidate.transform.IsChildOf(target.transform)||
                    !RRLHitRouter.CanHit(candidate,transform))continue;
                var hp=candidate.GetComponentInParent<P12PipelineSafeDamageable>();if(hp==null||hp.IsDead)continue;
                var contact=candidate.ClosestPoint(transform.position);float square=(contact-transform.position).sqrMagnitude;
                if(square>distance)continue;distance=square;closest=candidate;point=contact;
            }
            if(closest!=null)RRLHitRouter.Apply(closest,amount,point,Vector3.up,transform);
        }
        void OnDisable(){if(zone!=null)zone.ResetCycle();targets.Clear();}
        void OnDestroy(){if(zone!=null)zone.DamageRequested-=Damage;}
    }
}
