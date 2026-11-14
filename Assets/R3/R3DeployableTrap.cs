using System.Collections.Generic;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Multiple effects remain independent; expiry of a stronger effect must
    // reveal any weaker one that still has time left.
    public sealed class R3EnemyMovementDebuff : MonoBehaviour
    {
        struct Effect {public float Factor,Until;}
        readonly List<Effect> effects=new List<Effect>();
        public void Apply(float factor,float seconds)
        {
            if(float.IsNaN(factor)||float.IsInfinity(factor)||factor<0||factor>=1||
                float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;
            effects.Add(new Effect{Factor=factor,Until=Time.time+seconds});
        }
        public float Multiplier
        {
            get
            {
                float value=1;
                for(int i=effects.Count-1;i>=0;i--)
                    if(effects[i].Until<=Time.time)effects.RemoveAt(i);else value=Mathf.Min(value,effects[i].Factor);
                return value;
            }
        }
    }
    // Sensor is a child of the physical deployable so it has its own trigger collider.
    public sealed class R3DeployableTrap : MonoBehaviour
    {
        Transform owner,root;
        R3PartKind kind;
        float radius,damage,duration,repelSpeed,damagePerSecond,fieldDuration,expiresAt,slowFactor,slowSeconds;
        bool activated,deferredTrigger;
        Collider[] damageHits;
        Dictionary<Transform,Collider> damageTargets;
        readonly List<R3ShieldDome.BlastBoundary> shieldBoundaries=new List<R3ShieldDome.BlastBoundary>();
        readonly List<R3ShieldDome.BlastBoundary> exposedShields=new List<R3ShieldDome.BlastBoundary>();
        readonly List<Collider> unblockedTargets=new List<Collider>();
        public bool Activated=>activated;
        public void Configure(Transform trap,Transform source,R3PartKind type,float triggerRadius,float blastDamage,float empDuration,float repel,float dot,float fieldSeconds,float trapSeconds,float slow,float slowDuration)
        {root=trap;owner=source;kind=type;radius=triggerRadius;damage=blastDamage;duration=empDuration;repelSpeed=repel;damagePerSecond=dot;fieldDuration=fieldSeconds;expiresAt=Time.time+trapSeconds;slowFactor=slow;slowSeconds=slowDuration;}
        bool OwnerAttackBlocked
        {
            get {var run=R1RunController.Instance;return run!=null&&run.Player!=null&&owner==run.Player.transform&&run.EmergencyRescueAttackBlocked;}
        }
        void OnTriggerStay(Collider other){if(deferredTrigger&&!activated&&!OwnerAttackBlocked)OnTriggerEnter(other);}
        void OnTriggerEnter(Collider other)
        {
            if(activated||owner==null||root==null||!RRLHitRouter.CanHit(other,owner))return;
            var enemy=other.GetComponentInParent<P12PipelineSafeEnemyTurret>();
            if(enemy==null||enemy.IsDefeated)return;
            if(OwnerAttackBlocked){deferredTrigger=true;return;}
            activated=true;deferredTrigger=false;
            if(kind==R3PartKind.DamageTrapDeployer)
            {
                damageHits=new Collider[64];damageTargets=new Dictionary<Transform,Collider>();
                ShowFieldOutline();
                expiresAt=Time.time+fieldDuration;
                return;
            }
            if(kind==R3PartKind.EmpTrapDeployer)
            {
                RRLEmpPartState.RefreshEligibility();
                var emitter=gameObject.AddComponent<EmpEmitter>();emitter.SetAutomaticSimulation(false);
                emitter.BindVehicle(owner,owner.GetInstanceID(),RRLHitRouter.Team(owner));emitter.SetOrigin(root);
                emitter.ConfigurePhysics(~0,~0,8192,512);emitter.Configure(radius*2,duration,0);
                emitter.TryPulse(out _);
            }
            else if(kind==R3PartKind.SlowTrapDeployer||kind==R3PartKind.ImmobilizeTrapDeployer)
            {
                var enemies=new HashSet<P12PipelineSafeEnemyTurret>();
                foreach(var hit in Physics.OverlapSphere(root.position,radius,~0,QueryTriggerInteraction.Ignore))
                {
                    if(!RRLHitRouter.CanHit(hit,owner))continue;
                    var target=hit.GetComponentInParent<P12PipelineSafeEnemyTurret>();
                    if(target==null||target.IsDefeated||!enemies.Add(target))continue;
                    var debuff=target.GetComponent<R3EnemyMovementDebuff>();
                    if(debuff==null)debuff=target.gameObject.AddComponent<R3EnemyMovementDebuff>();
                    debuff.Apply(slowFactor,slowSeconds);
                }
            }
            else if(kind==R3PartKind.RepelTrapDeployer)
            {
                var bodies=new HashSet<Rigidbody>();Vector3 origin=root.position;
                foreach(var hit in Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore))
                {
                    if(!RRLHitRouter.CanHit(hit,owner))continue;
                    var target=hit.GetComponentInParent<P12PipelineSafeEnemyTurret>();
                    var body=hit.attachedRigidbody;
                    if(target==null||target.IsDefeated||body==null||body.isKinematic||!bodies.Add(body))continue;
                    Vector3 away=Vector3.ProjectOnPlane(body.position-origin,Vector3.up).normalized;
                    if(away.sqrMagnitude<.01f)away=root.forward;
                    body.AddForce(away*repelSpeed,ForceMode.VelocityChange);
                }
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
                ApplyAreaDamage(targets,origin,damage,R3DamagePropagation.DamageKind.Blast);
            }
            Destroy(root.gameObject);
        }
        void ShowFieldOutline()
        {
            // The sensor cancels the deployable's non-uniform scale, so the
            // outline and the physics trigger share the same world radius.
            var outline=new GameObject("Zasięg pola obrażeń");outline.transform.SetParent(transform,false);
            var line=outline.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;
            line.positionCount=48;line.startWidth=.06f;line.endWidth=.06f;
            line.startColor=new Color(1f,.35f,.05f);line.endColor=line.startColor;
            var renderer=root.GetComponent<Renderer>();if(renderer!=null)line.sharedMaterial=renderer.sharedMaterial;
            for(int i=0;i<line.positionCount;i++)
            {
                float angle=2*Mathf.PI*i/line.positionCount;
                line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,.08f,Mathf.Sin(angle)*radius));
            }
        }
        void FixedUpdate()
        {
            if(root==null)return;
            if(Time.time>=expiresAt){Destroy(root.gameObject);return;}
            if(!activated||kind!=R3PartKind.DamageTrapDeployer||owner==null||root==null||OwnerAttackBlocked)return;
            damageTargets.Clear();Vector3 origin=root.position;int count;
            while(true)
            {
                count=Physics.OverlapSphereNonAlloc(origin,radius,damageHits,~0,QueryTriggerInteraction.Ignore);
                if(count<damageHits.Length)break;
                if(damageHits.Length>=4096){damageHits=Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore);count=damageHits.Length;break;}
                System.Array.Resize(ref damageHits,damageHits.Length*2);
            }
            for(int i=0;i<count;i++)
            {
                var hit=damageHits[i];
                if(!RRLHitRouter.CanHit(hit,owner)||hit.GetComponentInParent<P12PipelineSafeDamageable>()==null)continue;
                var target=hit.GetComponentInParent<P12PipelineSafeEnemyTurret>();
                if(target==null||target.IsDefeated)continue;
                if(!damageTargets.TryGetValue(target.transform,out var previous)||
                    (hit.ClosestPoint(origin)-origin).sqrMagnitude<(previous.ClosestPoint(origin)-origin).sqrMagnitude)
                    damageTargets[target.transform]=hit;
            }
            ApplyAreaDamage(damageTargets,origin,damagePerSecond*Time.fixedDeltaTime,R3DamagePropagation.DamageKind.Kinetic);
        }
        void ApplyAreaDamage(Dictionary<Transform,Collider> targets,Vector3 origin,float amount,R3DamagePropagation.DamageKind damageKind)
        {
            amount=R3MountedBonusEffects.ScaleOwnedDamage(owner,amount);
            R3ShieldDome.CollectExposedBlastBoundaries(origin,radius,owner,shieldBoundaries,exposedShields);unblockedTargets.Clear();
            foreach(var hit in targets.Values)
            {
                if(hit==null)continue;Vector3 point=hit.ClosestPoint(origin);
                if(!R3ShieldDome.TryFirstHit(origin,point,owner,out _,out _))unblockedTargets.Add(hit);
            }
            foreach(var hit in unblockedTargets)if(hit!=null)
                RRLHitRouter.ApplyTyped(hit,amount,hit.ClosestPoint(origin),hit.transform.position-origin,owner,damageKind);
            foreach(var boundary in exposedShields)if(boundary.Dome!=null)
                boundary.Dome.Absorb(amount,boundary.Point,(boundary.Point-origin).normalized);
        }
    }
}
