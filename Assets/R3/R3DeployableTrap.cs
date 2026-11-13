using System.Collections.Generic;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Sensor is a child of the physical deployable so it has its own trigger collider.
    public sealed class R3DeployableTrap : MonoBehaviour
    {
        Transform owner,root;
        R3PartKind kind;
        float radius,damage,duration;
        bool activated;
        public bool Activated=>activated;
        public void Configure(Transform trap,Transform source,R3PartKind type,float triggerRadius,float blastDamage,float empDuration)
        {root=trap;owner=source;kind=type;radius=triggerRadius;damage=blastDamage;duration=empDuration;}
        void OnTriggerEnter(Collider other)
        {
            if(activated||owner==null||root==null||!RRLHitRouter.CanHit(other,owner))return;
            var enemy=other.GetComponentInParent<P12PipelineSafeEnemyTurret>();
            if(enemy==null||enemy.IsDefeated)return;
            activated=true;
            if(kind==R3PartKind.EmpTrapDeployer)
            {
                RRLEmpPartState.RefreshEligibility();
                var emitter=gameObject.AddComponent<EmpEmitter>();emitter.SetAutomaticSimulation(false);
                emitter.BindVehicle(owner,owner.GetInstanceID(),RRLHitRouter.Team(owner));emitter.SetOrigin(root);
                emitter.ConfigurePhysics(~0,~0,8192,512);emitter.Configure(radius*2,duration,0);
                emitter.TryPulse(out _);
            }
            else
            {
                var targets=new Dictionary<Transform,Collider>();Vector3 origin=root.position;
                foreach(var hit in Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore))
                {
                    if(!RRLHitRouter.CanHit(hit,owner)||hit.GetComponentInParent<P12PipelineSafeDamageable>()==null)continue;
                    var target=hit.GetComponentInParent<P12PipelineSafeEnemyTurret>();
                    if(target==null||target.IsDefeated)continue;
                    if(!targets.TryGetValue(target.transform,out var previous)||
                        (hit.ClosestPoint(origin)-origin).sqrMagnitude<(previous.ClosestPoint(origin)-origin).sqrMagnitude)
                        targets[target.transform]=hit;
                }
                foreach(var hit in targets.Values)if(hit!=null)
                    RRLHitRouter.ApplyTyped(hit,damage,hit.ClosestPoint(origin),hit.transform.position-origin,owner,R3DamagePropagation.DamageKind.Blast);
            }
            Destroy(root.gameObject);
        }
    }
}
